using System.IO;
using System.Security;
using NAudio.Wave;
using SpeakText.App.Models;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

namespace SpeakText.App.Services;

public sealed class WindowsSpeechService : IDisposable
{
    public const double MinSupportedSpeedMultiplier = 0.35;
    public const double MaxSupportedSpeedMultiplier = 3.00;

    private readonly SemaphoreSlim _speakLock = new(1, 1);
    private readonly object _stateGate = new();

    private CancellationTokenSource? _playbackCts;
    private WaveOutEvent? _activeOutput;
    private bool _isSpeaking;

    public Task SpeakAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        return SpeakAsync([request], cancellationToken);
    }

    public async Task SpeakAsync(IReadOnlyList<SpeechRequest> requests, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (requests.Count == 0)
        {
            return;
        }

        await _speakLock.WaitAsync(cancellationToken);
        CancellationTokenSource? linkedCts = null;

        try
        {
            CancelAndStopCurrentPlayback();

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            SetCurrentPlayback(linkedCts, null, isSpeaking: true);

            foreach (var request in requests)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    continue;
                }

                await PlayRequestAsync(request, linkedCts.Token);
            }
        }
        finally
        {
            lock (_stateGate)
            {
                if (ReferenceEquals(_playbackCts, linkedCts))
                {
                    _playbackCts?.Dispose();
                    _playbackCts = null;
                    _activeOutput = null;
                    _isSpeaking = false;
                }
            }

            _speakLock.Release();
        }
    }

    public Task StopAsync()
    {
        CancelAndStopCurrentPlayback();
        return Task.CompletedTask;
    }

    public Task<bool> IsSpeakingAsync()
    {
        lock (_stateGate)
        {
            return Task.FromResult(_isSpeaking);
        }
    }

    public void Dispose()
    {
        CancelAndStopCurrentPlayback();
        _speakLock.Dispose();
    }

    private async Task PlayRequestAsync(SpeechRequest request, CancellationToken cancellationToken)
    {
        using var synthesizer = new SpeechSynthesizer();

        var resolvedVoice = VoiceCatalogService.ResolveWindowsVoice(
            request.LanguageProfile.WindowsVoiceName,
            request.LanguageProfile.LanguageCode);
        if (resolvedVoice is not null)
        {
            synthesizer.Voice = resolvedVoice.VoiceInformation;
        }

        synthesizer.Options.SpeakingRate = Math.Clamp(
            request.SpeedMultiplier,
            MinSupportedSpeedMultiplier,
            MaxSupportedSpeedMultiplier);

        using var synthesisStream = await SynthesizeAsync(synthesizer, request, cancellationToken);
        var waveBytes = await ReadWaveBytesAsync(synthesisStream, cancellationToken);
        await PlayWaveAsync(waveBytes, cancellationToken);
    }

    private async Task<SpeechSynthesisStream> SynthesizeAsync(
        SpeechSynthesizer synthesizer,
        SpeechRequest request,
        CancellationToken cancellationToken)
    {
        var useSsml = request.Pitch != 0;
        var operation = useSsml
            ? synthesizer.SynthesizeSsmlToStreamAsync(BuildSsml(request))
            : synthesizer.SynthesizeTextToStreamAsync(request.Text);

        cancellationToken.ThrowIfCancellationRequested();
        return await operation;
    }

    private async Task PlayWaveAsync(byte[] waveBytes, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(waveBytes, writable: false);
        using var waveReader = new WaveFileReader(memoryStream);
        using var output = new WaveOutEvent();

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        output.PlaybackStopped += (_, args) =>
        {
            if (args.Exception is not null)
            {
                completionSource.TrySetException(args.Exception);
            }
            else
            {
                completionSource.TrySetResult();
            }
        };

        output.Init(waveReader);
        SetCurrentOutput(output);

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                output.Stop();
            }
            catch
            {
            }
        });

        output.Play();
        await completionSource.Task;
        cancellationToken.ThrowIfCancellationRequested();

        ClearCurrentOutput(output);
    }

    private void CancelAndStopCurrentPlayback()
    {
        CancellationTokenSource? cts;
        WaveOutEvent? output;

        lock (_stateGate)
        {
            cts = _playbackCts;
            output = _activeOutput;
            _isSpeaking = false;
        }

        try
        {
            cts?.Cancel();
        }
        catch
        {
        }

        try
        {
            output?.Stop();
        }
        catch
        {
        }
    }

    private void SetCurrentPlayback(CancellationTokenSource? playbackCts, WaveOutEvent? output, bool isSpeaking)
    {
        lock (_stateGate)
        {
            _playbackCts = playbackCts;
            _activeOutput = output;
            _isSpeaking = isSpeaking;
        }
    }

    private void SetCurrentOutput(WaveOutEvent output)
    {
        lock (_stateGate)
        {
            _activeOutput = output;
            _isSpeaking = true;
        }
    }

    private void ClearCurrentOutput(WaveOutEvent output)
    {
        lock (_stateGate)
        {
            if (ReferenceEquals(_activeOutput, output))
            {
                _activeOutput = null;
            }
        }
    }

    private static async Task<byte[]> ReadWaveBytesAsync(SpeechSynthesisStream stream, CancellationToken cancellationToken)
    {
        stream.Seek(0);

        var size = checked((uint)stream.Size);
        using var inputStream = stream.GetInputStreamAt(0);
        using var reader = new DataReader(inputStream);

        await reader.LoadAsync(size);
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = new byte[size];
        reader.ReadBytes(bytes);
        reader.DetachStream();
        return bytes;
    }

    private static string BuildSsml(SpeechRequest request)
    {
        var escapedText = SecurityElement.Escape(request.Text) ?? string.Empty;
        var languageCode = string.IsNullOrWhiteSpace(request.LanguageProfile.LanguageCode)
            ? "en-US"
            : request.LanguageProfile.LanguageCode;
        var normalizedPitch = Math.Clamp(request.Pitch * 5, -50, 50);

        return $"""
            <speak version="1.0" xml:lang="{languageCode}" xmlns="http://www.w3.org/2001/10/synthesis">
              <prosody pitch="{normalizedPitch:+#;-#;0}%">{escapedText}</prosody>
            </speak>
            """;
    }
}
