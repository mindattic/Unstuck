using System.Windows.Interop;

namespace UnstuckTaskbar.Hotkeys;

/// <summary>
/// Registers one global hotkey via <c>RegisterHotKey</c>/<c>WM_HOTKEY</c> on a message-only window
/// (no visible UI, no low-level keyboard hook). Media/volume keys register the same way as any
/// other key, with zero modifiers. Only one gesture is ever registered at a time; assigning a new
/// one unregisters the old.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    // HWND_MESSAGE: a message-only window needs no WndClass/visible surface at all.
    private static readonly nint HwndMessage = -3;
    private const int HotkeyId = 1;

    private readonly HwndSource source;
    private bool registered;

    public event Action? HotkeyPressed;

    /// <summary>Raised when <see cref="RegisterCurrent"/> fails (e.g. the gesture is already owned by another app).</summary>
    public event Action<HotkeyGesture>? RegistrationFailed;

    public HotkeyManager()
    {
        var parameters = new HwndSourceParameters("UnstuckTaskbarHotkeySink")
        {
            ParentWindow = HwndMessage,
            WindowStyle = 0,
        };
        source = new HwndSource(parameters);
        source.AddHook(WndProc);
    }

    public bool RegisterCurrent(HotkeyGesture gesture)
    {
        Unregister();
        var (modifiers, vk) = gesture.ToWin32();
        registered = Win32Interop.RegisterHotKey(source.Handle, HotkeyId, modifiers | Win32Interop.MOD_NOREPEAT, vk);
        if (!registered) RegistrationFailed?.Invoke(gesture);
        return registered;
    }

    public void Unregister()
    {
        if (!registered) return;
        Win32Interop.UnregisterHotKey(source.Handle, HotkeyId);
        registered = false;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == Win32Interop.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke();
        }
        return nint.Zero;
    }

    public void Dispose()
    {
        Unregister();
        source.RemoveHook(WndProc);
        source.Dispose();
    }
}
