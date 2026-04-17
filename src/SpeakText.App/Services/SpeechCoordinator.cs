using SpeakText.App.Models;

namespace SpeakText.App.Services;

public sealed class SpeechCoordinator : IDisposable
{
    private readonly WindowsSpeechService _windowsSpeechService = new();

    public async Task<string> SpeakAsync(SpeechRequest request, AppSettings settings, CancellationToken cancellationToken)
    {
        await StopAsync();
        await _windowsSpeechService.SpeakAsync(request, cancellationToken);
        return UiTextCatalog.Get(settings.UiLanguageCode).PlaybackStartedStatus;
    }

    public async Task<string> SpeakAsync(IReadOnlyList<SpeechRequest> requests, AppSettings settings, CancellationToken cancellationToken)
    {
        await StopAsync();
        await _windowsSpeechService.SpeakAsync(requests, cancellationToken);
        return UiTextCatalog.Get(settings.UiLanguageCode).PlaybackStartedStatus;
    }

    public Task StopAsync()
    {
        return _windowsSpeechService.StopAsync();
    }

    public Task<bool> IsSpeakingAsync()
    {
        return _windowsSpeechService.IsSpeakingAsync();
    }

    public void Dispose()
    {
        _windowsSpeechService.Dispose();
    }
}
