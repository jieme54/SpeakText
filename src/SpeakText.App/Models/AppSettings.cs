using System.Text.Json;
using SpeakText.App.Services;

namespace SpeakText.App.Models;

public sealed class AppSettings
{
    public string ActiveLanguageCode { get; set; } = "fr";

    public string UiLanguageCode { get; set; } = "fr";

    public HotkeySettings Hotkey { get; set; } = HotkeySettings.CreateDefault();

    public bool AlwaysOnTop { get; set; } = true;

    public bool LaunchOnWindowsStartup { get; set; }

    public Dictionary<string, LanguageProfile> LanguageProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static AppSettings CreateDefault()
    {
        var settings = new AppSettings();
        settings.EnsureDefaults();
        return settings;
    }

    public void EnsureDefaults()
    {
        LanguageProfiles ??= new Dictionary<string, LanguageProfile>(StringComparer.OrdinalIgnoreCase);
        Hotkey ??= HotkeySettings.CreateDefault();
        UiLanguageCode = LanguageCodeHelper.NormalizeUiLanguageCode(UiLanguageCode);

        NormalizeLanguageProfiles();

        if (LanguageProfiles.Count == 0)
        {
            EnsureLanguageProfile("fr");
            EnsureLanguageProfile("en");
        }
        else
        {
            foreach (var languageCode in LanguageProfiles.Keys.ToArray())
            {
                EnsureLanguageProfile(languageCode);
            }
        }

        ActiveLanguageCode = LanguageCodeHelper.NormalizeLanguageCode(ActiveLanguageCode);
        if (LanguageCodeHelper.IsAutomaticLanguageCode(ActiveLanguageCode))
        {
            ActiveLanguageCode = LanguageCodeHelper.AutomaticLanguageCode;
        }
        else if (string.IsNullOrWhiteSpace(ActiveLanguageCode) || !LanguageProfiles.ContainsKey(ActiveLanguageCode))
        {
            ActiveLanguageCode = LanguageProfiles.Keys
                .OrderBy(static code => code, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? "fr";
        }
    }

    public void EnsureLanguageProfiles(IEnumerable<string> languageCodes)
    {
        foreach (var languageCode in languageCodes)
        {
            EnsureLanguageProfile(languageCode);
        }

        EnsureDefaults();
    }

    public LanguageProfile GetLanguageProfile(string languageCode)
    {
        EnsureLanguageProfile(languageCode);
        return LanguageProfiles[LanguageCodeHelper.NormalizeLanguageCode(languageCode)];
    }

    public AppSettings DeepClone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();
    }

    public void EnsureLanguageProfile(string languageCode)
    {
        var normalizedCode = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return;
        }

        if (!LanguageProfiles.TryGetValue(normalizedCode, out var profile))
        {
            profile = LanguageProfile.CreateDefault(normalizedCode);
            LanguageProfiles[normalizedCode] = profile;
        }

        EnsureLanguageDefaults(profile, LanguageProfile.CreateDefault(normalizedCode));
    }

    private void NormalizeLanguageProfiles()
    {
        var normalizedProfiles = new Dictionary<string, LanguageProfile>(StringComparer.OrdinalIgnoreCase);

        foreach (var (dictionaryKey, profile) in LanguageProfiles)
        {
            var normalizedCode = LanguageCodeHelper.NormalizeLanguageCode(profile.LanguageCode);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                normalizedCode = LanguageCodeHelper.NormalizeLanguageCode(dictionaryKey);
            }

            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                continue;
            }

            profile.LanguageCode = normalizedCode;
            if (!normalizedProfiles.ContainsKey(normalizedCode))
            {
                normalizedProfiles[normalizedCode] = profile;
            }
        }

        LanguageProfiles = normalizedProfiles;
    }

    private static void EnsureLanguageDefaults(LanguageProfile profile, LanguageProfile defaults)
    {
        if (string.IsNullOrWhiteSpace(profile.LanguageCode))
        {
            profile.LanguageCode = defaults.LanguageCode;
        }

        if (string.IsNullOrWhiteSpace(profile.DisplayName)
            || string.Equals(profile.DisplayName, "Francais", StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile.DisplayName, "Français", StringComparison.OrdinalIgnoreCase))
        {
            profile.DisplayName = defaults.DisplayName;
        }

        profile.WindowsVoiceName ??= string.Empty;
        profile.SpeedMultiplier = NormalizeSpeedMultiplier(profile, defaults);

        if (string.IsNullOrWhiteSpace(profile.PreviewText)
            || string.Equals(profile.PreviewText, "Bonjour, ici SpeakText. Ceci est un test rapide de la voix francaise.", StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile.PreviewText, "Hello, this is SpeakText. This is a quick English voice preview.", StringComparison.OrdinalIgnoreCase))
        {
            profile.PreviewText = defaults.PreviewText;
        }
    }

    private static double NormalizeSpeedMultiplier(LanguageProfile profile, LanguageProfile defaults)
    {
        if (profile.SpeedMultiplier > 0)
        {
            return Math.Clamp(profile.SpeedMultiplier, LanguageProfile.MinSpeedMultiplier, LanguageProfile.MaxSpeedMultiplier);
        }

        if (profile.Rate != 0)
        {
            var migratedMultiplier = 1.0 + (profile.Rate * 0.20);
            return Math.Clamp(migratedMultiplier, LanguageProfile.MinSpeedMultiplier, LanguageProfile.MaxSpeedMultiplier);
        }

        return defaults.SpeedMultiplier;
    }
}
