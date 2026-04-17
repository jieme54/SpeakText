using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SpeakText.App;

public partial class App : System.Windows.Application
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SpeakText",
        "crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        var startInBackground = e.Args.Any(arg => string.Equals(arg, "--background", StringComparison.OrdinalIgnoreCase));
        var mainWindow = new MainWindow(startInBackground);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception.ToString());
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.ExceptionObject?.ToString() ?? "Unknown unhandled exception");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception.ToString());
    }

    private static void WriteCrashLog(string content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath) ?? AppContext.BaseDirectory);
            File.WriteAllText(CrashLogPath, $"[{DateTime.Now:O}]{Environment.NewLine}{content}");
        }
        catch
        {
        }
    }
}
