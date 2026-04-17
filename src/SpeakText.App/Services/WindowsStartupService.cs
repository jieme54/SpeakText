using Microsoft.Win32;

namespace SpeakText.App.Services;

public sealed class WindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SpeakText";

    public void ApplyRegistration(bool launchOnWindowsStartup)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Unable to access the Windows startup registry key.");

        if (!launchOnWindowsStartup)
        {
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("Unable to determine the current executable path.");
        }

        var command = $"\"{executablePath}\" --background";
        runKey.SetValue(ValueName, command, RegistryValueKind.String);
    }
}
