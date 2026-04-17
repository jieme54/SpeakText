using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SpeakText.App.Models;

namespace SpeakText.App.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x534B;

    private HwndSource? _source;
    private IntPtr _handle;
    private bool _registered;

    public event EventHandler? Pressed;

    public void Attach(Window window)
    {
        _handle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);
    }

    public bool Register(HotkeySettings hotkey)
    {
        if (_handle == IntPtr.Zero)
        {
            return false;
        }

        Unregister();

        var modifiers = GetNativeModifiers(hotkey.ToModifierKeys()) | NativeMethods.ModNoRepeat;
        var virtualKey = KeyInterop.VirtualKeyFromKey(hotkey.ToKey());

        _registered = NativeMethods.RegisterHotKey(_handle, HotkeyId, modifiers, virtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered && _handle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_handle, HotkeyId);
            _registered = false;
        }
    }

    public void Dispose()
    {
        Unregister();

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private static uint GetNativeModifiers(ModifierKeys modifiers)
    {
        var native = 0u;

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            native |= NativeMethods.ModAlt;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            native |= NativeMethods.ModControl;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            native |= NativeMethods.ModShift;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            native |= NativeMethods.ModWin;
        }

        return native;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }
}

