using SpeakText.App.Models;

namespace SpeakText.App.Services;

public sealed class SpeechRequest
{
    public string Text { get; init; } = string.Empty;

    public double SpeedMultiplier { get; init; } = LanguageProfile.DefaultSpeedMultiplier;

    public int Pitch { get; init; }

    public LanguageProfile LanguageProfile { get; init; } = new();
}
