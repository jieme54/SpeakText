using System.IO;
using System.Text.Json;
using SpeakText.App.Models;

namespace SpeakText.App.Services;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
    };

    public SettingsService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpeakText");

        Directory.CreateDirectory(root);
        SettingsPath = Path.Combine(root, "settings.json");
    }

    public string SettingsPath { get; }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions);
                if (loaded is not null)
                {
                    loaded.EnsureDefaults();
                    Save(loaded);
                    return loaded;
                }
            }
        }
        catch
        {
        }

        var defaults = AppSettings.CreateDefault();
        Save(defaults);
        return defaults;
    }

    public void Save(AppSettings settings)
    {
        settings.EnsureDefaults();
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
