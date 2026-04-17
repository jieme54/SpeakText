using SpeakText.App.Services;

namespace SpeakText.App.Models;

public sealed class LanguageProfile
{
    public const double MinSpeedMultiplier = 0.35;
    public const double MaxSpeedMultiplier = 3.00;
    public const double SpeedStep = 0.05;
    public const double DefaultSpeedMultiplier = 1.00;

    public string LanguageCode { get; set; } = "fr";

    public string DisplayName { get; set; } = "Français";

    public string WindowsVoiceName { get; set; } = string.Empty;

    public double SpeedMultiplier { get; set; } = DefaultSpeedMultiplier;

    // Kept for migrating settings written by older builds.
    public int Rate { get; set; }

    public int Pitch { get; set; }

    public string PreviewText { get; set; } = string.Empty;

    public string EngineDisplayName => "Windows Voices";

    public static LanguageProfile CreateDefault(string languageCode)
    {
        var normalizedCode = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
        return new LanguageProfile
        {
            LanguageCode = normalizedCode,
            DisplayName = LanguageCodeHelper.GetLanguageDisplayName(normalizedCode, "fr"),
            WindowsVoiceName = string.Empty,
            SpeedMultiplier = DefaultSpeedMultiplier,
            Rate = 0,
            Pitch = 0,
            PreviewText = LanguageCodeHelper.GetDefaultPreviewText(normalizedCode),
        };
    }

    public static LanguageProfile CreateFrenchDefault()
    {
        return CreateDefault("fr");
    }

    public static LanguageProfile CreateEnglishDefault()
    {
        return CreateDefault("en");
    }
}
