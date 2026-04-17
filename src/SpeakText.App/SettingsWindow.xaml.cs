using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SpeakText.App.Models;
using SpeakText.App.Services;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SpeakText.App;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _workingCopy;
    private readonly SpeechCoordinator _previewCoordinator = new();
    private HotkeySettings _capturedHotkey;
    private UiTextSet _uiTexts;
    private IReadOnlyList<EngineOption> _profileLanguageOptions = [];
    private IReadOnlyList<EngineOption> _voiceOptions = [];
    private string _selectedProfileLanguageCode = string.Empty;
    private bool _isCapturingHotkey;
    private bool _isUpdatingControls;

    public SettingsWindow(AppSettings currentSettings)
    {
        InitializeComponent();
        _workingCopy = currentSettings.DeepClone();
        _workingCopy.EnsureDefaults();
        _capturedHotkey = _workingCopy.Hotkey.DeepClone();
        _uiTexts = UiTextCatalog.Get(_workingCopy.UiLanguageCode);
        Loaded += SettingsWindow_Loaded;
        PreviewKeyDown += SettingsWindow_PreviewKeyDown;
        Closing += SettingsWindow_Closing;
    }

    public AppSettings? SavedSettings { get; private set; }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _isUpdatingControls = true;

        var interfaceLanguageOptions = VoiceCatalogService.GetInterfaceLanguageOptions(_workingCopy.UiLanguageCode);
        _workingCopy.UiLanguageCode = VoiceCatalogService.NormalizeAvailableInterfaceLanguageCode(
            _workingCopy.UiLanguageCode,
            interfaceLanguageOptions);
        _uiTexts = UiTextCatalog.Get(_workingCopy.UiLanguageCode);
        ConfigureOptionComboBox(InterfaceLanguageComboBox, interfaceLanguageOptions, _workingCopy.UiLanguageCode);
        HotkeyTextBox.Text = _capturedHotkey.ToDisplayString();
        AlwaysOnTopCheckBox.IsChecked = _workingCopy.AlwaysOnTop;
        LaunchOnWindowsStartupCheckBox.IsChecked = _workingCopy.LaunchOnWindowsStartup;

        ApplyLocalizedText();
        RefreshProfileLanguageOptions(preserveSelectedLanguage: false);

        _isUpdatingControls = false;
        RefreshSliderTexts();
        RefreshInfoText();
    }

    private void SettingsWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _previewCoordinator.Dispose();
    }

    private CultureInfo CurrentUiCulture => LanguageCodeHelper.GetUiCulture(GetSelectedUiLanguageCode());

    private void CaptureHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = true;
        HotkeyTextBox.Text = _uiTexts.CapturePrompt;
        HotkeyHintTextBlock.Text = _uiTexts.CaptureNeedsModifier;
    }

    private void SettingsWindow_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (!_isCapturingHotkey)
        {
            return;
        }

        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            _isCapturingHotkey = false;
            HotkeyTextBox.Text = _capturedHotkey.ToDisplayString();
            HotkeyHintTextBlock.Text = _uiTexts.CaptureCancelled;
            return;
        }

        if (IsModifierOnlyKey(key))
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            HotkeyHintTextBlock.Text = _uiTexts.CaptureNeedsModifier;
            return;
        }

        _capturedHotkey = HotkeySettings.From(modifiers, key);
        _isCapturingHotkey = false;
        HotkeyTextBox.Text = _capturedHotkey.ToDisplayString();
        HotkeyHintTextBlock.Text = _uiTexts.CaptureCompleted;
    }

    private void InterfaceLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _isUpdatingControls)
        {
            return;
        }

        SaveCurrentProfileToWorkingCopy();
        _workingCopy.UiLanguageCode = GetSelectedUiLanguageCode();
        _uiTexts = UiTextCatalog.Get(_workingCopy.UiLanguageCode);
        ApplyLocalizedText();
        RefreshProfileLanguageOptions(preserveSelectedLanguage: true);
        RefreshSliderTexts();
        RefreshInfoText();
    }

    private void ProfileLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _isUpdatingControls)
        {
            return;
        }

        SaveCurrentProfileToWorkingCopy();

        if (ProfileLanguageComboBox.SelectedValue is string languageCode)
        {
            LoadProfileIntoEditor(languageCode);
        }
    }

    private void RateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RefreshSliderTexts();
        RefreshInfoText();
    }

    private void PitchSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RefreshSliderTexts();
    }

    private async void PreviewVoiceButton_Click(object sender, RoutedEventArgs e)
    {
        await PreviewSelectedLanguageAsync();
    }

    private void OpenWindowsVoicesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:speech")
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            System.Windows.MessageBox.Show(
                this,
                _uiTexts.WindowsSettingsLaunchFailed,
                "SpeakText",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async Task PreviewSelectedLanguageAsync()
    {
        VoiceCatalogService.InstalledWindowsVoice? resolvedVoice = null;

        try
        {
            SaveCurrentProfileToWorkingCopy();
            var previewSettings = BuildWorkingCopyFromForm();
            var profile = previewSettings.GetLanguageProfile(_selectedProfileLanguageCode);
            resolvedVoice = VoiceCatalogService.ResolveWindowsVoice(
                profile.WindowsVoiceName,
                profile.LanguageCode);
            var basePreviewText = string.IsNullOrWhiteSpace(profile.PreviewText)
                ? LanguageCodeHelper.GetDefaultPreviewText(profile.LanguageCode)
                : profile.PreviewText;
            var resolvedVoiceName = VoiceCatalogService.GetSpokenVoiceName(
                resolvedVoice?.DisplayName ?? GetSelectedVoiceOptionLabel());
            var previewText = LanguageCodeHelper.BuildVoicePreviewText(
                profile.LanguageCode,
                basePreviewText,
                resolvedVoiceName);

            var request = new SpeechRequest
            {
                Text = previewText,
                SpeedMultiplier = profile.SpeedMultiplier,
                Pitch = profile.Pitch,
                LanguageProfile = profile,
            };

            var status = await _previewCoordinator.SpeakAsync(request, previewSettings, CancellationToken.None);
            ProfileInfoTextBox.Text = _uiTexts.BuildVoiceTestStatusSafe(status, resolvedVoiceName);
        }
        catch (Exception ex)
        {
            var resolvedVoiceName = VoiceCatalogService.GetSpokenVoiceName(
                resolvedVoice?.DisplayName ?? GetSelectedVoiceOptionLabel());
            ProfileInfoTextBox.Text = _uiTexts.BuildVoiceTestStatusSafe(ex.Message, resolvedVoiceName);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SavedSettings = BuildWorkingCopyFromForm();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private AppSettings BuildWorkingCopyFromForm()
    {
        SaveCurrentProfileToWorkingCopy();

        var settings = _workingCopy.DeepClone();
        settings.Hotkey = _capturedHotkey.DeepClone();
        settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? true;
        settings.LaunchOnWindowsStartup = LaunchOnWindowsStartupCheckBox.IsChecked ?? false;
        settings.UiLanguageCode = GetSelectedUiLanguageCode();
        settings.EnsureLanguageProfiles(_profileLanguageOptions.Select(option => option.Id));
        settings.EnsureDefaults();
        return settings;
    }

    private void ApplyLocalizedText()
    {
        Title = _uiTexts.SettingsWindowTitle;
        SettingsTitleTextBlock.Text = _uiTexts.SettingsTitle;
        SettingsIntroTextBlock.Text = _uiTexts.SettingsIntro;
        InterfaceSectionTitleTextBlock.Text = _uiTexts.InterfaceSectionTitle;
        InterfaceLanguageLabelTextBlock.Text = _uiTexts.InterfaceLanguageLabel;
        GlobalPlaybackTitleTextBlock.Text = _uiTexts.GlobalPlaybackTitle;
        HotkeyLabelTextBlock.Text = _uiTexts.HotkeyLabel;
        CaptureHotkeyButton.Content = _uiTexts.CaptureButton;
        HotkeyHintTextBlock.Text = _uiTexts.BuildDefaultHotkeyHint(HotkeySettings.CreateDefault().ToDisplayString());
        AlwaysOnTopCheckBox.Content = _uiTexts.AlwaysOnTopLabel;
        LaunchOnWindowsStartupCheckBox.Content = _uiTexts.LaunchOnWindowsStartupLabel;
        VoiceProfileTitleTextBlock.Text = _uiTexts.VoiceProfileTitle;
        ProfileLanguageLabelTextBlock.Text = _uiTexts.ProfileLanguageLabel;
        WindowsVoiceLabelTextBlock.Text = _uiTexts.WindowsVoiceLabel;
        SpeedLabelTextBlock.Text = _uiTexts.SpeedLabel;
        PitchLabelTextBlock.Text = _uiTexts.PitchLabel;
        PreviewVoiceButton.Content = _uiTexts.TestVoiceButton;
        OpenWindowsVoicesButton.ToolTip = _uiTexts.OpenWindowsVoicesTooltip;
        SaveButton.Content = _uiTexts.SaveButton;
        CancelButton.Content = _uiTexts.CancelButton;
    }

    private void RefreshProfileLanguageOptions(bool preserveSelectedLanguage)
    {
        _isUpdatingControls = true;

        _profileLanguageOptions = VoiceCatalogService.GetInstalledLanguageOptions(GetSelectedUiLanguageCode());
        _workingCopy.EnsureLanguageProfiles(_profileLanguageOptions.Select(option => option.Id));

        var targetLanguageCode = preserveSelectedLanguage
            ? _selectedProfileLanguageCode
            : _workingCopy.ActiveLanguageCode;

        if (!_profileLanguageOptions.Any(option => string.Equals(option.Id, targetLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            targetLanguageCode = _profileLanguageOptions.First().Id;
        }

        ConfigureOptionComboBox(ProfileLanguageComboBox, _profileLanguageOptions, targetLanguageCode);
        LoadProfileIntoEditor(targetLanguageCode);

        _isUpdatingControls = false;
    }

    private void LoadProfileIntoEditor(string languageCode)
    {
        _selectedProfileLanguageCode = LanguageCodeHelper.NormalizeLanguageCode(languageCode);
        var profile = _workingCopy.GetLanguageProfile(_selectedProfileLanguageCode);

        _isUpdatingControls = true;

        _voiceOptions = VoiceCatalogService.GetWindowsVoiceOptions(_selectedProfileLanguageCode, GetSelectedUiLanguageCode());
        ConfigureOptionComboBox(WindowsVoiceComboBox, _voiceOptions, profile.WindowsVoiceName);
        RateSlider.Value = profile.SpeedMultiplier;
        PitchSlider.Value = profile.Pitch;

        _isUpdatingControls = false;
        RefreshSliderTexts();
        RefreshInfoText();
    }

    private void SaveCurrentProfileToWorkingCopy()
    {
        if (string.IsNullOrWhiteSpace(_selectedProfileLanguageCode))
        {
            return;
        }

        var profile = _workingCopy.GetLanguageProfile(_selectedProfileLanguageCode);
        profile.WindowsVoiceName = (WindowsVoiceComboBox.SelectedValue as string ?? string.Empty).Trim();
        profile.SpeedMultiplier = NormalizeSpeedMultiplier(RateSlider.Value);
        profile.Pitch = (int)PitchSlider.Value;
    }

    private void RefreshSliderTexts()
    {
        if (!IsLoaded)
        {
            return;
        }

        RateValueTextBlock.Text = FormatSpeedMultiplier(RateSlider.Value);
        PitchValueTextBlock.Text = ((int)PitchSlider.Value).ToString(CurrentUiCulture);
    }

    private void RefreshInfoText()
    {
        if (!IsLoaded || string.IsNullOrWhiteSpace(_selectedProfileLanguageCode))
        {
            return;
        }

        var languageName = VoiceCatalogService.GetLanguageDisplayName(_selectedProfileLanguageCode, GetSelectedUiLanguageCode());
        ProfileInfoTextBox.Text = _uiTexts.BuildVoiceProfileInfo(languageName, FormatSpeedMultiplier(RateSlider.Value));
    }

    private string GetSelectedUiLanguageCode()
    {
        return LanguageCodeHelper.NormalizeUiLanguageCode(InterfaceLanguageComboBox.SelectedValue as string ?? _workingCopy.UiLanguageCode);
    }

    private static void ConfigureOptionComboBox(WpfComboBox comboBox, IReadOnlyList<EngineOption> options, string selectedId)
    {
        var normalizedSelectedId = selectedId.Trim();
        var normalizedLookupKey = VoiceCatalogService.NormalizeVoiceLookupKey(normalizedSelectedId);
        var items = options.ToList();
        var resolvedSelectedId = normalizedSelectedId;

        if (!string.IsNullOrWhiteSpace(normalizedSelectedId)
            && items.All(option => !string.Equals(option.Id, normalizedSelectedId, StringComparison.OrdinalIgnoreCase)))
        {
            var matchedOption = items.FirstOrDefault(option =>
                string.Equals(VoiceCatalogService.NormalizeVoiceLookupKey(option.Id), normalizedLookupKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(option.Id, normalizedSelectedId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(option.Label, normalizedSelectedId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(VoiceCatalogService.NormalizeVoiceLookupKey(option.Label), normalizedLookupKey, StringComparison.OrdinalIgnoreCase)
                || option.Label.Contains(normalizedSelectedId, StringComparison.OrdinalIgnoreCase));

            if (matchedOption is not null)
            {
                resolvedSelectedId = matchedOption.Id;
            }
        }

        comboBox.ItemsSource = items;
        comboBox.DisplayMemberPath = nameof(EngineOption.Label);
        comboBox.SelectedValuePath = nameof(EngineOption.Id);
        comboBox.SelectedValue = resolvedSelectedId;

        if (comboBox.SelectedIndex < 0 && items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static bool IsModifierOnlyKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;
    }

    private static double NormalizeSpeedMultiplier(double value)
    {
        var roundedValue = Math.Round(value / LanguageProfile.SpeedStep, MidpointRounding.AwayFromZero) * LanguageProfile.SpeedStep;
        return Math.Clamp(roundedValue, LanguageProfile.MinSpeedMultiplier, LanguageProfile.MaxSpeedMultiplier);
    }

    private string FormatSpeedMultiplier(double value)
    {
        var normalizedValue = NormalizeSpeedMultiplier(value);
        return $"x{normalizedValue.ToString("0.00", CurrentUiCulture)}";
    }

    private string GetSelectedVoiceOptionLabel()
    {
        if (WindowsVoiceComboBox.SelectedItem is EngineOption selectedOption
            && !string.IsNullOrWhiteSpace(selectedOption.Label))
        {
            return selectedOption.Label;
        }

        return _voiceOptions.FirstOrDefault(option => string.IsNullOrWhiteSpace(option.Id))?.Label
            ?? string.Empty;
    }
}
