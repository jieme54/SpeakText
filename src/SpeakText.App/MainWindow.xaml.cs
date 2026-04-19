using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SpeakText.App.Models;
using SpeakText.App.Services;
using Forms = System.Windows.Forms;

namespace SpeakText.App;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly WindowsStartupService _startupService = new();
    private readonly GlobalHotkeyService _hotkeyService = new();
    private readonly SelectionCaptureService _selectionCaptureService = new();
    private readonly SpeechCoordinator _speechCoordinator = new();
    private readonly DispatcherTimer _foregroundWatcher;
    private readonly DispatcherTimer _playbackWatcher;

    private System.Drawing.Icon? _appIcon;
    private Forms.NotifyIcon? _notifyIcon;
    private AppSettings _settings;
    private UiTextSet _uiTexts;
    private IReadOnlyList<EngineOption> _installedLanguageOptions = [];
    private IntPtr _windowHandle;
    private CancellationTokenSource? _playbackCts;
    private bool _isShuttingDown;
    private bool _isPlaybackActive;
    private bool _isRefreshingPlaybackState;
    private readonly bool _startHiddenInTray;
    private bool _isTrayMenuOpen;

    public MainWindow(bool startHiddenInTray = false)
    {
        _startHiddenInTray = startHiddenInTray;
        _settings = _settingsService.Load();
        _uiTexts = UiTextCatalog.Get(_settings.UiLanguageCode);

        if (_startHiddenInTray)
        {
            ShowInTaskbar = false;
            ShowActivated = false;
            Opacity = 0;
        }

        InitializeComponent();

        _foregroundWatcher = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        _foregroundWatcher.Tick += ForegroundWatcher_Tick;

        _playbackWatcher = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350),
        };
        _playbackWatcher.Tick += PlaybackWatcher_Tick;

        Loaded += MainWindow_Loaded;
        SourceInitialized += MainWindow_SourceInitialized;
        Closing += MainWindow_Closing;

        InitializeUi();
    }

    private void InitializeUi()
    {
        EnsureInstalledLanguageProfiles();
        ApplyLocalizedText();
        RefreshLanguageOptions();

        Topmost = _settings.AlwaysOnTop;
        SetPlaybackState(false);
        SetStatus(string.Empty);
        TrySyncStartupRegistration(showErrors: false);
        _settingsService.Save(_settings);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Top + 20;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        _hotkeyService.Attach(this);
        _hotkeyService.Pressed += HotkeyService_Pressed;

        CreateTrayIcon();
        RegisterConfiguredHotkey();
        _foregroundWatcher.Start();

        if (_startHiddenInTray)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Hide();
                Opacity = 1;
                ShowActivated = true;
                RefreshTrayMenu();
            }, DispatcherPriority.ApplicationIdle);
        }
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _foregroundWatcher.Stop();
        _playbackWatcher.Stop();
        _hotkeyService.Dispose();
        await StopPlaybackAsync(updateStatus: false);
        _speechCoordinator.Dispose();

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        if (_appIcon is not null)
        {
            _appIcon.Dispose();
            _appIcon = null;
        }

        if (!_isShuttingDown)
        {
            _isShuttingDown = true;
        }
    }

    private void EnsureInstalledLanguageProfiles()
    {
        var interfaceLanguageOptions = VoiceCatalogService.GetInterfaceLanguageOptions(_settings.UiLanguageCode);
        _settings.UiLanguageCode = VoiceCatalogService.NormalizeAvailableInterfaceLanguageCode(
            _settings.UiLanguageCode,
            interfaceLanguageOptions);
        _installedLanguageOptions = VoiceCatalogService.GetInstalledLanguageOptions(_settings.UiLanguageCode);
        _settings.EnsureLanguageProfiles(_installedLanguageOptions.Select(option => option.Id));

        if (!LanguageCodeHelper.IsAutomaticLanguageCode(_settings.ActiveLanguageCode)
            && !_installedLanguageOptions.Any(option => string.Equals(option.Id, _settings.ActiveLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            _settings.ActiveLanguageCode = _installedLanguageOptions.First().Id;
        }

        _uiTexts = UiTextCatalog.Get(_settings.UiLanguageCode);
    }

    private void ApplyLocalizedText()
    {
        SettingsButton.ToolTip = _uiTexts.SettingsTooltip;
        MinimizeButton.ToolTip = _uiTexts.MinimizeTooltip;
        CloseButton.ToolTip = _uiTexts.CloseTooltip;
        ShortcutHintTextBlock.Text = _uiTexts.BuildShortcutHint(_settings.Hotkey.ToDisplayString());
        SetPlaybackState(_isPlaybackActive);
        RefreshLanguageUi();
        RefreshTrayMenu();
    }

    private async void HotkeyService_Pressed(object? sender, EventArgs e)
    {
        if (_isPlaybackActive)
        {
            await StopPlaybackAsync();
            return;
        }

        await SpeakSelectedTextAsync(delayForHotkeyRelease: true);
    }

    private void ForegroundWatcher_Tick(object? sender, EventArgs e)
    {
        _selectionCaptureService.RememberForegroundWindow(IsOwnedWindow);
    }

    private async void PlaybackWatcher_Tick(object? sender, EventArgs e)
    {
        if (_isRefreshingPlaybackState || !_isPlaybackActive)
        {
            return;
        }

        _isRefreshingPlaybackState = true;

        try
        {
            var isSpeaking = await _speechCoordinator.IsSpeakingAsync();
            if (!isSpeaking && _isPlaybackActive)
            {
                _playbackWatcher.Stop();
                _playbackCts?.Dispose();
                _playbackCts = null;
                SetPlaybackState(false);
                SetStatus(_uiTexts.PlaybackFinishedStatus);
            }
        }
        catch
        {
        }
        finally
        {
            _isRefreshingPlaybackState = false;
        }
    }

    private bool IsOwnedWindow(IntPtr handle)
    {
        return handle == _windowHandle;
    }

    private void CreateTrayIcon()
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        _appIcon ??= LoadApplicationIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "SpeakText",
            Visible = true,
            Icon = _appIcon ?? System.Drawing.SystemIcons.Application,
        };

        RefreshTrayMenu();
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;
    }

    private static System.Drawing.Icon? LoadApplicationIcon()
    {
        try
        {
            var resourceInfo = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/SpeakTextApp.ico"));
            if (resourceInfo is null)
            {
                return null;
            }

            using var stream = resourceInfo.Stream;
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;

            using var icon = new System.Drawing.Icon(buffer);
            return (System.Drawing.Icon)icon.Clone();
        }
        catch
        {
            return null;
        }
    }

    private void RefreshTrayMenu()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.ContextMenuStrip?.Dispose();

        var menu = new Forms.ContextMenuStrip();
        if (_isPlaybackActive)
        {
            menu.Items.Add(
                _uiTexts.StopTrayMenu,
                null,
                (_, _) => Dispatcher.InvokeAsync(async () => await StopPlaybackAsync()));
            menu.Items.Add(new Forms.ToolStripSeparator());
        }

        if (!IsOverlayVisibleInUi())
        {
            menu.Items.Add(_uiTexts.OpenOverlayMenu, null, (_, _) => Dispatcher.Invoke(RestoreFromTray));
        }

        menu.Items.Add(_uiTexts.QuitMenu, null, (_, _) => Dispatcher.Invoke(ShutdownApplication));
        menu.Closed += (_, _) =>
        {
            _isTrayMenuOpen = false;
            if (_windowHandle != IntPtr.Zero)
            {
                NativeMethods.PostMessage(_windowHandle, NativeMethods.WmNull, IntPtr.Zero, IntPtr.Zero);
            }
        };
        _notifyIcon.ContextMenuStrip = menu;
    }

    private void NotifyIcon_MouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button != Forms.MouseButtons.Left || _notifyIcon?.ContextMenuStrip is null)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            if (_isTrayMenuOpen)
            {
                _notifyIcon.ContextMenuStrip.Close();
                return;
            }

            RefreshTrayMenu();
            if (_windowHandle != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(_windowHandle);
            }

            _isTrayMenuOpen = true;
            _notifyIcon.ContextMenuStrip.Show(Forms.Cursor.Position);
        });
    }

    private bool IsOverlayVisibleInUi()
    {
        return IsVisible && ShowInTaskbar;
    }

    private void RegisterConfiguredHotkey()
    {
        ShortcutHintTextBlock.Text = _uiTexts.BuildShortcutHint(_settings.Hotkey.ToDisplayString());
        if (_hotkeyService.Register(_settings.Hotkey))
        {
            return;
        }

        var fallbackCandidates = new[]
        {
            HotkeySettings.CreateDefault(),
            new HotkeySettings { Ctrl = true, Alt = true, Shift = true, Key = nameof(Key.F11) },
        };

        foreach (var candidate in fallbackCandidates)
        {
            if (candidate.ToDisplayString() == _settings.Hotkey.ToDisplayString())
            {
                continue;
            }

            if (_hotkeyService.Register(candidate))
            {
                _settings.Hotkey = candidate.DeepClone();
                _settingsService.Save(_settings);
                ShortcutHintTextBlock.Text = _uiTexts.BuildShortcutHint(_settings.Hotkey.ToDisplayString());
                SetStatus(_uiTexts.BuildHotkeyUnavailableStatus(_settings.Hotkey.ToDisplayString()));
                return;
            }
        }

        SetStatus(_uiTexts.HotkeyRegistrationFailed);
    }

    private void RefreshLanguageOptions()
    {
        var overlayOptions = VoiceCatalogService.GetOverlayLanguageOptions(_settings.UiLanguageCode);
        LanguageComboBox.ItemsSource = overlayOptions;
        LanguageComboBox.DisplayMemberPath = nameof(EngineOption.Label);
        LanguageComboBox.SelectedValuePath = nameof(EngineOption.Id);
        LanguageComboBox.SelectedValue = _settings.ActiveLanguageCode;
        RefreshLanguageUi();
    }

    private void RefreshLanguageUi()
    {
        var selectedLanguageCode = GetSelectedOverlayLanguageCode();
        if (LanguageCodeHelper.IsAutomaticLanguageCode(selectedLanguageCode))
        {
            ModelBadgeTextBlock.Text = _uiTexts.WindowsVoicesBadge;
        }
        else
        {
            var profile = _settings.GetLanguageProfile(selectedLanguageCode);
            ModelBadgeTextBlock.Text = VoiceCatalogService.GetOverlayVoiceBadgeText(
                profile.LanguageCode,
                profile.WindowsVoiceName,
                _settings.UiLanguageCode);
        }

        ShortcutHintTextBlock.Text = _uiTexts.BuildShortcutHint(_settings.Hotkey.ToDisplayString());
    }

    private string GetSelectedOverlayLanguageCode()
    {
        var selectedCode = LanguageComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedCode))
        {
            selectedCode = _settings.ActiveLanguageCode;
        }

        return LanguageCodeHelper.IsAutomaticLanguageCode(selectedCode)
            ? LanguageCodeHelper.AutomaticLanguageCode
            : LanguageCodeHelper.NormalizeLanguageCode(selectedCode);
    }

    private LanguageProfile ResolvePlaybackLanguageProfile(string text, out string resolvedLanguageCode, out bool usedAutomaticMode)
    {
        var selectedCode = GetSelectedOverlayLanguageCode();
        usedAutomaticMode = LanguageCodeHelper.IsAutomaticLanguageCode(selectedCode);

        if (usedAutomaticMode)
        {
            resolvedLanguageCode = LanguageDetectionService.DetectBestLanguageCode(
                text,
                _installedLanguageOptions.Select(option => option.Id),
                GetAutomaticFallbackLanguageCode());
        }
        else
        {
            resolvedLanguageCode = selectedCode;
        }

        if (string.IsNullOrWhiteSpace(resolvedLanguageCode))
        {
            resolvedLanguageCode = _installedLanguageOptions.FirstOrDefault()?.Id ?? "fr";
        }

        return _settings.GetLanguageProfile(resolvedLanguageCode);
    }

    private AutomaticSpeechPlan BuildAutomaticSpeechPlan(string text)
    {
        var plan = AutomaticSpeechPlanner.BuildPlan(
            text,
            _settings,
            _installedLanguageOptions,
            GetAutomaticFallbackLanguageCode());
        return plan;
    }

    private string GetAutomaticFallbackLanguageCode()
    {
        var osLanguageCode = LanguageCodeHelper.NormalizeLanguageCode(CultureInfo.CurrentUICulture.Name);
        if (_installedLanguageOptions.Any(option => string.Equals(option.Id, osLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            return osLanguageCode;
        }

        var uiLanguageCode = LanguageCodeHelper.NormalizeUiLanguageCode(_settings.UiLanguageCode);
        if (_installedLanguageOptions.Any(option => string.Equals(option.Id, uiLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            return uiLanguageCode;
        }

        return _installedLanguageOptions.FirstOrDefault()?.Id ?? "fr";
    }

    private void SetStatus(string message)
    {
        var hasMessage = !string.IsNullOrWhiteSpace(message);
        StatusTextBlock.Text = hasMessage ? message : string.Empty;
        StatusTextBlock.Visibility = hasMessage ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task TogglePlaybackAsync(bool delayForHotkeyRelease)
    {
        if (_isPlaybackActive)
        {
            await StopPlaybackAsync();
            return;
        }

        await SpeakSelectedTextAsync(delayForHotkeyRelease);
    }

    private async Task SpeakSelectedTextAsync(bool delayForHotkeyRelease)
    {
        try
        {
            _playbackCts?.Dispose();
            _playbackCts = new CancellationTokenSource();
            var cancellationToken = _playbackCts.Token;

            SetPlaybackState(true);
            SetStatus(_uiTexts.RecoveringSelectionStatus);

            if (delayForHotkeyRelease)
            {
                await Task.Delay(260, cancellationToken);
            }

            var text = await _selectionCaptureService.CaptureSelectedTextAsync(IsOwnedWindow, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                SetPlaybackState(false);
                SetStatus(_uiTexts.NoSelectionDetectedStatus);
                return;
            }

            string status;
            if (string.Equals(GetSelectedOverlayLanguageCode(), LanguageCodeHelper.AutomaticLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                var plan = BuildAutomaticSpeechPlan(text);
                if (plan.LanguagesInOrder.Count > 1)
                {
                    var mixedLanguageNames = string.Join(
                        " + ",
                        plan.LanguagesInOrder.Select(code => VoiceCatalogService.GetLanguageDisplayName(code, _settings.UiLanguageCode)));
                    status = _uiTexts.BuildAutomaticMixedPlaybackStartedStatus(mixedLanguageNames);
                }
                else
                {
                    var resolvedLanguageName = VoiceCatalogService.GetLanguageDisplayName(plan.DominantLanguageCode, _settings.UiLanguageCode);
                    status = _uiTexts.BuildAutomaticPlaybackStartedStatus(resolvedLanguageName);
                }

                SetStatus(status);
                await _speechCoordinator.SpeakAsync(plan.Requests, _settings, cancellationToken);
            }
            else
            {
                var profile = ResolvePlaybackLanguageProfile(text, out _, out _);
                var request = new SpeechRequest
                {
                    Text = text,
                    SpeedMultiplier = profile.SpeedMultiplier,
                    Pitch = profile.Pitch,
                    LanguageProfile = profile,
                };

                status = _uiTexts.PlaybackStartedStatus;
                SetStatus(status);
                await _speechCoordinator.SpeakAsync(request, _settings, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            SetPlaybackState(false);
            SetStatus(_uiTexts.PlaybackFinishedStatus);
        }
        catch (OperationCanceledException)
        {
            SetPlaybackState(false);
            SetStatus(_uiTexts.PlaybackStoppedStatus);
        }
        catch (Exception ex)
        {
            SetPlaybackState(false);
            SetStatus(ex.Message);
        }
    }

    private async Task StopPlaybackAsync(bool updateStatus = true)
    {
        try
        {
            _playbackCts?.Cancel();
            _playbackWatcher.Stop();
            await _speechCoordinator.StopAsync();
        }
        catch
        {
        }
        finally
        {
            _playbackCts?.Dispose();
            _playbackCts = null;
            SetPlaybackState(false);

            if (updateStatus)
            {
                SetStatus(_uiTexts.PlaybackStoppedStatus);
            }
        }
    }

    private void SetPlaybackState(bool isActive)
    {
        _isPlaybackActive = isActive;
        PlayPauseButton.Content = isActive ? _uiTexts.StopPlaybackButton : _uiTexts.PlaySelectionButton;
        RefreshTrayMenu();

        if (isActive)
        {
            PlayPauseButton.Background = CreateBrush("#FFEAF2FF");
            PlayPauseButton.BorderBrush = CreateBrush("#FFC9DAFF");
            PlayPauseButton.BorderThickness = new Thickness(1);
            PlayPauseButton.Foreground = CreateBrush("#FF1E66F5");
        }
        else
        {
            PlayPauseButton.Background = CreateBrush("#FF1E66F5");
            PlayPauseButton.BorderThickness = new Thickness(0);
            PlayPauseButton.Foreground = System.Windows.Media.Brushes.White;
        }
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        await TogglePlaybackAsync(delayForHotkeyRelease: false);
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (LanguageComboBox.SelectedValue is string languageCode)
        {
            _settings.ActiveLanguageCode = LanguageCodeHelper.IsAutomaticLanguageCode(languageCode)
                ? LanguageCodeHelper.AutomaticLanguageCode
                : LanguageCodeHelper.NormalizeLanguageCode(languageCode);
            _settingsService.Save(_settings);
            RefreshLanguageUi();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void OpenSettings()
    {
        RestoreFromTray();

        var dialog = new SettingsWindow(_settings)
        {
            Owner = this,
        };

        if (dialog.ShowDialog() == true && dialog.SavedSettings is not null)
        {
            ApplySettings(dialog.SavedSettings);
        }
    }

    private void ApplySettings(AppSettings updatedSettings)
    {
        var previousSettings = _settings.DeepClone();
        _settings = updatedSettings.DeepClone();
        _settings.EnsureDefaults();
        EnsureInstalledLanguageProfiles();

        Topmost = _settings.AlwaysOnTop;
        ApplyLocalizedText();
        RefreshLanguageOptions();

        if (!_hotkeyService.Register(_settings.Hotkey))
        {
            _settings.Hotkey = previousSettings.Hotkey.DeepClone();
            _hotkeyService.Register(_settings.Hotkey);
            SetStatus(_uiTexts.HotkeyConflictStatus);
        }
        else
        {
            SetStatus(string.Empty);
        }

        TrySyncStartupRegistration(showErrors: true, previousLaunchOnWindowsStartup: previousSettings.LaunchOnWindowsStartup);
        RefreshLanguageUi();
        _settingsService.Save(_settings);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        MinimizeToTray();
    }

    private void MinimizeToTray()
    {
        ShowInTaskbar = false;
        Hide();
        RefreshTrayMenu();
    }

    private void RestoreFromTray()
    {
        Opacity = 1;
        ShowActivated = true;
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
        RefreshTrayMenu();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ShutdownApplication();
    }

    private void ShutdownApplication()
    {
        _isShuttingDown = true;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void TrySyncStartupRegistration(bool showErrors, bool? previousLaunchOnWindowsStartup = null)
    {
        try
        {
            _startupService.ApplyRegistration(_settings.LaunchOnWindowsStartup);
        }
        catch (Exception ex)
        {
            if (previousLaunchOnWindowsStartup.HasValue)
            {
                _settings.LaunchOnWindowsStartup = previousLaunchOnWindowsStartup.Value;

                try
                {
                    _startupService.ApplyRegistration(_settings.LaunchOnWindowsStartup);
                }
                catch
                {
                }
            }

            if (showErrors)
            {
                System.Windows.MessageBox.Show(
                    this,
                    $"{_uiTexts.StartupRegistrationFailed}{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "SpeakText",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
