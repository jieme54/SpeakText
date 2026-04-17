using SpeakText.App.Models;

namespace SpeakText.App.Services;

public static class UiTextCatalog
{
    public static UiTextSet Get(string uiLanguageCode)
    {
        return LanguageCodeHelper.NormalizeUiLanguageCode(uiLanguageCode) switch
        {
            "fr" => UiTextSet.CreateFrench(),
            _ => UiTextSet.CreateEnglish(),
        };
    }

    public static IReadOnlyList<EngineOption> GetInterfaceLanguageOptions()
    {
        return
        [
            new EngineOption("fr", "Fran\u00e7ais"),
            new EngineOption("en", "English"),
            new EngineOption("de", "German"),
            new EngineOption("es", "Spanish"),
            new EngineOption("pt", "Portuguese"),
            new EngineOption("el", "Greek"),
            new EngineOption("it", "Italian"),
            new EngineOption("hi", "Hindi"),
            new EngineOption("ja", "Japanese"),
            new EngineOption("zh", "Chinese"),
        ];
    }
}

public sealed class UiTextSet
{
    private string LanguageCode { get; init; } = "en";

    public string SettingsTooltip { get; private init; } = string.Empty;
    public string MinimizeTooltip { get; private init; } = string.Empty;
    public string CloseTooltip { get; private init; } = string.Empty;
    public string WindowsVoicesBadge { get; private init; } = string.Empty;
    public string AutomaticLanguageOption { get; private init; } = string.Empty;
    public string PlaySelectionButton { get; private init; } = string.Empty;
    public string StopPlaybackButton { get; private init; } = string.Empty;
    public string ReadyStatus { get; private init; } = string.Empty;
    public string RecoveringSelectionStatus { get; private init; } = string.Empty;
    public string NoSelectionDetectedStatus { get; private init; } = string.Empty;
    public string PlaybackStoppedStatus { get; private init; } = string.Empty;
    public string PlaybackFinishedStatus { get; private init; } = string.Empty;
    public string PlaybackStartedStatus { get; private init; } = string.Empty;
    public string OpenOverlayMenu { get; private init; } = string.Empty;
    public string PlayTrayMenu { get; private init; } = string.Empty;
    public string StopTrayMenu { get; private init; } = string.Empty;
    public string TogglePlaybackMenu { get; private init; } = string.Empty;
    public string SettingsMenu { get; private init; } = string.Empty;
    public string QuitMenu { get; private init; } = string.Empty;
    public string SettingsWindowTitle { get; private init; } = string.Empty;
    public string SettingsTitle { get; private init; } = string.Empty;
    public string SettingsIntro { get; private init; } = string.Empty;
    public string InterfaceSectionTitle { get; private init; } = string.Empty;
    public string InterfaceLanguageLabel { get; private init; } = string.Empty;
    public string GlobalPlaybackTitle { get; private init; } = string.Empty;
    public string HotkeyLabel { get; private init; } = string.Empty;
    public string CaptureButton { get; private init; } = string.Empty;
    public string AlwaysOnTopLabel { get; private init; } = string.Empty;
    public string VoiceProfileTitle { get; private init; } = string.Empty;
    public string ProfileLanguageLabel { get; private init; } = string.Empty;
    public string WindowsVoiceLabel { get; private init; } = string.Empty;
    public string SpeedLabel { get; private init; } = string.Empty;
    public string PitchLabel { get; private init; } = string.Empty;
    public string TestVoiceButton { get; private init; } = string.Empty;
    public string OpenWindowsVoicesTooltip { get; private init; } = string.Empty;
    public string SaveButton { get; private init; } = string.Empty;
    public string CancelButton { get; private init; } = string.Empty;

    public string LaunchOnWindowsStartupLabel =>
        LanguageCode == "fr"
            ? "D\u00e9marrer en arri\u00e8re-plan au d\u00e9marrage de Windows"
            : "Launch in background when Windows starts";

    public string StartupRegistrationFailed =>
        LanguageCode == "fr"
            ? "Impossible de mettre \u00e0 jour l'option de d\u00e9marrage de Windows."
            : "Unable to update the Windows startup option.";

    public string HotkeyRegistrationFailed =>
        LanguageCode == "fr"
            ? "Impossible d'enregistrer le raccourci global."
            : "Unable to register the global shortcut.";

    public string HotkeyConflictStatus =>
        LanguageCode == "fr"
            ? "Le nouveau raccourci est d\u00e9j\u00e0 pris. L'ancien a \u00e9t\u00e9 conserv\u00e9."
            : "The new shortcut is already in use. The previous one was kept.";

    public string WindowsSettingsLaunchFailed =>
        LanguageCode == "fr"
            ? "Impossible d'ouvrir les param\u00e8tres Windows des voix."
            : "Unable to open the Windows voice settings.";

    public string CapturePrompt =>
        LanguageCode == "fr"
            ? "Appuie sur la combinaison..."
            : "Press the shortcut...";

    public string CaptureCancelled =>
        LanguageCode == "fr"
            ? "Capture annul\u00e9e."
            : "Capture canceled.";

    public string CaptureCompleted =>
        LanguageCode == "fr"
            ? "Raccourci captur\u00e9."
            : "Shortcut captured.";

    public string CaptureNeedsModifier =>
        LanguageCode == "fr"
            ? "Ajoute au moins Ctrl ou Alt."
            : "Add at least Ctrl or Alt.";

    public string BuildShortcutHint(string hotkey)
    {
        return $"{GetShortcutPrefix()}{hotkey}";
    }

    public string BuildDefaultHotkeyHint(string hotkey)
    {
        return LanguageCode == "fr"
            ? $"Conseil : le raccourci par d\u00e9faut est {hotkey}."
            : $"Tip: the default shortcut is {hotkey}.";
    }

    public string BuildHotkeyUnavailableStatus(string hotkey)
    {
        return LanguageCode == "fr"
            ? $"Le raccourci initial \u00e9tait indisponible. Utilisation de {hotkey}."
            : $"The initial shortcut was unavailable. Using {hotkey}.";
    }

    public string BuildVoiceProfileInfo(string languageName, string currentSpeed)
    {
        return LanguageCode == "fr"
            ? $"Voix Windows active pour le profil {languageName}. x1,00 correspond \u00e0 la vitesse native de la voix. La plage disponible dans SpeakText va de x0,35 \u00e0 x3,00. R\u00e9glage actuel : {currentSpeed}."
            : $"Windows voice active for {languageName}. x1.00 matches the native voice speed. The available range in SpeakText goes from x0.35 to x3.00. Current setting: {currentSpeed}.";
    }

    public string BuildVoiceTestStatusSafe(string generalStatus, string voiceName)
    {
        if (string.IsNullOrWhiteSpace(voiceName))
        {
            return generalStatus;
        }

        var voiceLine = LanguageCode == "fr"
            ? $"Voix utilis\u00e9e : {voiceName}."
            : $"Voice used: {voiceName}.";

        return string.IsNullOrWhiteSpace(generalStatus)
            ? voiceLine
            : $"{generalStatus}{Environment.NewLine}{voiceLine}";
    }

    public string BuildAutomaticPlaybackStartedStatus(string languageName)
    {
        return LanguageCode == "fr"
            ? $"Mode automatique : {languageName}. Lecture lanc\u00e9e via Voix Windows."
            : $"Automatic mode: {languageName}. Playback started with Windows Voices.";
    }

    public string BuildAutomaticMixedPlaybackStartedStatus(string languageNames)
    {
        return LanguageCode == "fr"
            ? $"Mode automatique : langues mixtes ({languageNames}). Lecture lanc\u00e9e via Voix Windows."
            : $"Automatic mode: mixed languages ({languageNames}). Playback started with Windows Voices.";
    }

    public static UiTextSet CreateEnglish()
    {
        return new UiTextSet
        {
            LanguageCode = "en",
            SettingsTooltip = "Settings",
            MinimizeTooltip = "Minimize to the notification area",
            CloseTooltip = "Close",
            WindowsVoicesBadge = "Windows Voices",
            AutomaticLanguageOption = "Automatic",
            PlaySelectionButton = "Read Selection",
            StopPlaybackButton = "Stop Playback",
            ReadyStatus = "Ready to read the selection.",
            RecoveringSelectionStatus = "Capturing the selection...",
            NoSelectionDetectedStatus = "No selected text was detected.",
            PlaybackStoppedStatus = "Playback stopped.",
            PlaybackFinishedStatus = "Playback finished.",
            PlaybackStartedStatus = "Playback started with Windows Voices.",
            OpenOverlayMenu = "Open",
            PlayTrayMenu = "Read",
            StopTrayMenu = "Stop",
            TogglePlaybackMenu = "Read / stop",
            SettingsMenu = "Settings",
            QuitMenu = "Close",
            SettingsWindowTitle = "SpeakText Settings",
            SettingsTitle = "Settings",
            SettingsIntro = "SpeakText uses local Windows voices. The app can be shown in multiple languages, and each language installed on the PC keeps its own voice, speed, and pitch.",
            InterfaceSectionTitle = "Interface",
            InterfaceLanguageLabel = "Application language",
            GlobalPlaybackTitle = "Global playback",
            HotkeyLabel = "Global shortcut",
            CaptureButton = "Capture",
            AlwaysOnTopLabel = "Keep the overlay always on top",
            VoiceProfileTitle = "Voice profile",
            ProfileLanguageLabel = "Language to configure",
            WindowsVoiceLabel = "Windows voice",
            SpeedLabel = "Speed",
            PitchLabel = "Pitch",
            TestVoiceButton = "Test voice",
            OpenWindowsVoicesTooltip = "Open Windows voice settings",
            SaveButton = "Save",
            CancelButton = "Cancel",
        };
    }

    public static UiTextSet CreateFrench()
    {
        return new UiTextSet
        {
            LanguageCode = "fr",
            SettingsTooltip = "Param\u00e8tres",
            MinimizeTooltip = "R\u00e9duire vers la zone de notification",
            CloseTooltip = "Fermer",
            WindowsVoicesBadge = "Voix Windows",
            AutomaticLanguageOption = "Automatique",
            PlaySelectionButton = "Lire la s\u00e9lection",
            StopPlaybackButton = "Arr\u00eater la lecture",
            ReadyStatus = "Pr\u00eat \u00e0 lire la s\u00e9lection.",
            RecoveringSelectionStatus = "R\u00e9cup\u00e9ration de la s\u00e9lection...",
            NoSelectionDetectedStatus = "Aucun texte s\u00e9lectionn\u00e9 d\u00e9tect\u00e9.",
            PlaybackStoppedStatus = "Lecture arr\u00eat\u00e9e.",
            PlaybackFinishedStatus = "Lecture termin\u00e9e.",
            PlaybackStartedStatus = "Lecture lanc\u00e9e via Voix Windows.",
            OpenOverlayMenu = "Ouvrir",
            PlayTrayMenu = "Lire",
            StopTrayMenu = "Arr\u00eat",
            TogglePlaybackMenu = "Lire / arr\u00eater",
            SettingsMenu = "Param\u00e8tres",
            QuitMenu = "Fermer",
            SettingsWindowTitle = "Param\u00e8tres SpeakText",
            SettingsTitle = "Param\u00e8tres",
            SettingsIntro = "SpeakText utilise les voix locales de Windows. L'application peut \u00eatre affich\u00e9e dans plusieurs langues, et chaque langue install\u00e9e sur le PC garde sa propre voix, sa propre vitesse et sa propre tonalit\u00e9.",
            InterfaceSectionTitle = "Interface",
            InterfaceLanguageLabel = "Langue de l'application",
            GlobalPlaybackTitle = "Lecture globale",
            HotkeyLabel = "Raccourci global",
            CaptureButton = "Capturer",
            AlwaysOnTopLabel = "Garder l'overlay toujours au-dessus",
            VoiceProfileTitle = "Profil vocal",
            ProfileLanguageLabel = "Langue \u00e0 configurer",
            WindowsVoiceLabel = "Voix Windows",
            SpeedLabel = "Vitesse",
            PitchLabel = "Tonalit\u00e9",
            TestVoiceButton = "Tester la voix",
            OpenWindowsVoicesTooltip = "Ouvrir les param\u00e8tres Windows des voix",
            SaveButton = "Enregistrer",
            CancelButton = "Annuler",
        };
    }

    private string GetShortcutPrefix()
    {
        return LanguageCode == "fr" ? "Raccourci : " : "Shortcut: ";
    }
}
