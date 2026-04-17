using System.Text.Json;
using System.Windows.Input;

namespace SpeakText.App.Models;

public sealed class HotkeySettings
{
    public bool Ctrl { get; set; } = true;

    public bool Alt { get; set; } = true;

    public bool Shift { get; set; } = true;

    public bool Win { get; set; }

    public string Key { get; set; } = nameof(System.Windows.Input.Key.F12);

    public static HotkeySettings CreateDefault()
    {
        return new HotkeySettings
        {
            Ctrl = true,
            Alt = true,
            Shift = true,
            Win = false,
            Key = nameof(System.Windows.Input.Key.F12),
        };
    }

    public static HotkeySettings From(ModifierKeys modifiers, System.Windows.Input.Key key)
    {
        return new HotkeySettings
        {
            Ctrl = modifiers.HasFlag(ModifierKeys.Control),
            Alt = modifiers.HasFlag(ModifierKeys.Alt),
            Shift = modifiers.HasFlag(ModifierKeys.Shift),
            Win = modifiers.HasFlag(ModifierKeys.Windows),
            Key = key.ToString(),
        };
    }

    public ModifierKeys ToModifierKeys()
    {
        var modifiers = ModifierKeys.None;

        if (Ctrl)
        {
            modifiers |= ModifierKeys.Control;
        }

        if (Alt)
        {
            modifiers |= ModifierKeys.Alt;
        }

        if (Shift)
        {
            modifiers |= ModifierKeys.Shift;
        }

        if (Win)
        {
            modifiers |= ModifierKeys.Windows;
        }

        return modifiers;
    }

    public System.Windows.Input.Key ToKey()
    {
        return Enum.TryParse<System.Windows.Input.Key>(Key, true, out var parsed)
            ? parsed
            : System.Windows.Input.Key.F12;
    }

    public string ToDisplayString()
    {
        var parts = new List<string>();

        if (Ctrl)
        {
            parts.Add("Ctrl");
        }

        if (Alt)
        {
            parts.Add("Alt");
        }

        if (Shift)
        {
            parts.Add("Shift");
        }

        if (Win)
        {
            parts.Add("Win");
        }

        parts.Add(ToKey().ToString());
        return string.Join(" + ", parts);
    }

    public HotkeySettings DeepClone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<HotkeySettings>(json) ?? CreateDefault();
    }
}
