using System.Windows;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;
using Clipboard = System.Windows.Clipboard;
using TextDataFormat = System.Windows.TextDataFormat;

namespace SpeakText.App.Services;

public sealed class SelectionCaptureService
{
    private IntPtr _lastExternalWindow;

    public void RememberForegroundWindow(Func<IntPtr, bool> isOwnedWindow)
    {
        var current = NativeMethods.GetForegroundWindow();
        if (current != IntPtr.Zero && !isOwnedWindow(current))
        {
            _lastExternalWindow = current;
        }
    }

    public async Task<string?> CaptureSelectedTextAsync(Func<IntPtr, bool> isOwnedWindow, CancellationToken cancellationToken)
    {
        var targetWindow = ResolveTargetWindow(isOwnedWindow);
        if (targetWindow == IntPtr.Zero || !NativeMethods.IsWindow(targetWindow))
        {
            return null;
        }

        var previousForeground = NativeMethods.GetForegroundWindow();
        var targetAlreadyFocused = previousForeground == targetWindow;
        var snapshot = ClipboardSnapshot.Capture();
        var sequenceBefore = NativeMethods.GetClipboardSequenceNumber();

        try
        {
            if (!targetAlreadyFocused)
            {
                if (NativeMethods.IsIconic(targetWindow))
                {
                    NativeMethods.ShowWindow(targetWindow, NativeMethods.SwRestore);
                }

                NativeMethods.SetForegroundWindow(targetWindow);
                await Task.Delay(120, cancellationToken);
            }

            Forms.SendKeys.SendWait("^c");

            var sequenceChanged = false;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                await Task.Delay(60, cancellationToken);
                if (NativeMethods.GetClipboardSequenceNumber() != sequenceBefore)
                {
                    sequenceChanged = true;
                    break;
                }
            }

            if (!sequenceChanged || !Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                return null;
            }

            var text = Clipboard.GetText(TextDataFormat.UnicodeText);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        finally
        {
            snapshot.Restore();

            if (!targetAlreadyFocused &&
                previousForeground != IntPtr.Zero &&
                NativeMethods.IsWindow(previousForeground) &&
                !isOwnedWindow(previousForeground))
            {
                NativeMethods.SetForegroundWindow(previousForeground);
            }
        }
    }

    private IntPtr ResolveTargetWindow(Func<IntPtr, bool> isOwnedWindow)
    {
        var current = NativeMethods.GetForegroundWindow();
        if (current != IntPtr.Zero && !isOwnedWindow(current))
        {
            return current;
        }

        return _lastExternalWindow;
    }

    private sealed class ClipboardSnapshot
    {
        private const int ClipboardRetryCount = 6;
        private const int ClipboardRetryDelayMs = 40;

        private ClipboardSnapshot(System.Windows.DataObject? dataObject, bool hadClipboardData)
        {
            DataObject = dataObject;
            HadClipboardData = hadClipboardData;
        }

        public System.Windows.DataObject? DataObject { get; }

        public bool HadClipboardData { get; }

        public static ClipboardSnapshot Capture()
        {
            try
            {
                var source = TryGetClipboardDataObject();
                if (source is null)
                {
                    return new ClipboardSnapshot(null, false);
                }

                var clone = new System.Windows.DataObject();
                foreach (var format in source.GetFormats())
                {
                    try
                    {
                        var data = source.GetData(format, true);
                        if (data is not null)
                        {
                            clone.SetData(format, data);
                        }
                    }
                    catch
                    {
                    }
                }

                return new ClipboardSnapshot(clone, true);
            }
            catch
            {
                return new ClipboardSnapshot(null, false);
            }
        }

        public void Restore()
        {
            try
            {
                if (!HadClipboardData)
                {
                    return;
                }

                if (DataObject is not null)
                {
                    TryRestoreClipboardDataObject(DataObject);
                }
            }
            catch
            {
            }
        }

        private static System.Windows.IDataObject? TryGetClipboardDataObject()
        {
            for (var attempt = 0; attempt < ClipboardRetryCount; attempt++)
            {
                try
                {
                    return Clipboard.GetDataObject();
                }
                catch (COMException)
                {
                    Thread.Sleep(ClipboardRetryDelayMs);
                }
            }

            return null;
        }

        private static void TryRestoreClipboardDataObject(System.Windows.IDataObject dataObject)
        {
            for (var attempt = 0; attempt < ClipboardRetryCount; attempt++)
            {
                try
                {
                    // Do not persist the data through OleFlushClipboard; this path was crashing in ole32.dll.
                    Clipboard.SetDataObject(dataObject, false);
                    return;
                }
                catch (COMException)
                {
                    Thread.Sleep(ClipboardRetryDelayMs);
                }
            }
        }
    }
}
