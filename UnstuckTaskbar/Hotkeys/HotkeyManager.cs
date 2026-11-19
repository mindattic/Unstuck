using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace UnstuckTaskbar.Hotkeys;

/// <summary>
/// Registers the global hotkeys via <c>RegisterHotKey</c>/<c>WM_HOTKEY</c> on a message-only window (no visible
/// UI, no low-level keyboard hook):
/// <list type="bullet">
/// <item><b>Ctrl+Alt+Break</b>, built in and always on (<see cref="RegisterBuiltIn"/>). If another app holds it,
/// registration is retried every <see cref="RetryInterval"/> until it is free.</item>
/// <item>One optional extra gesture from the settings (<see cref="RegisterCurrent"/>); assigning a new one
/// unregisters the old.</item>
/// </list>
/// Break needs two registrations: with Ctrl held, Windows reports the Pause/Break key as <c>VK_CANCEL</c> (0x03)
/// instead of <c>VK_PAUSE</c> (0x13), and which one arrives depends on the keyboard, so both are registered.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    // HWND_MESSAGE: a message-only window needs no WndClass/visible surface at all.
    private static readonly nint HwndMessage = -3;
    private const int UserId = 1, UserCancelId = 2, BuiltInId = 3, BuiltInCancelId = 4;
    private const uint VkCancel = 0x03;
    public static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

    private readonly HwndSource source;
    private readonly HashSet<int> registered = [];
    private DispatcherTimer? retry;
    private bool builtInFailureReported;

    public event Action? HotkeyPressed;

    /// <summary>Raised when a gesture can't be registered (another app owns it). For the built-in chord, only once
    /// per outage; it keeps being retried.</summary>
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

    /// <summary>True while Ctrl+Alt+Break is registered.</summary>
    public bool BuiltInActive => registered.Contains(BuiltInId) || registered.Contains(BuiltInCancelId);

    /// <summary>Registers Ctrl+Alt+Break; if it's taken, keeps retrying in the background.</summary>
    public bool RegisterBuiltIn()
    {
        if (BuiltInActive) return true;
        if (Register(BuiltInId, BuiltInCancelId, HotkeyGesture.BuiltIn))
        {
            retry?.Stop();
            builtInFailureReported = false;
            return true;
        }
        if (!builtInFailureReported)
        {
            builtInFailureReported = true;
            RegistrationFailed?.Invoke(HotkeyGesture.BuiltIn);
        }
        if (retry is null)
        {
            retry = new DispatcherTimer { Interval = RetryInterval };
            retry.Tick += (_, _) => RegisterBuiltIn();
        }
        retry.Start();
        return false;
    }

    /// <summary>Registers the extra hotkey from the settings. The built-in chord itself needs no second registration.</summary>
    public bool RegisterCurrent(HotkeyGesture gesture)
    {
        Unregister();
        if (gesture == HotkeyGesture.BuiltIn) return true;
        var ok = Register(UserId, UserCancelId, gesture);
        if (!ok) RegistrationFailed?.Invoke(gesture);
        return ok;
    }

    /// <summary>Removes the extra hotkey; Ctrl+Alt+Break stays.</summary>
    public void Unregister()
    {
        Release(UserId);
        Release(UserCancelId);
    }

    private bool Register(int id, int cancelId, HotkeyGesture gesture)
    {
        var (modifiers, vk) = gesture.ToWin32();
        var ok = Win32Interop.RegisterHotKey(source.Handle, id, modifiers | Win32Interop.MOD_NOREPEAT, vk);
        if (ok) registered.Add(id);
        if (gesture.Key == Key.Pause && gesture.Modifiers.HasFlag(ModifierKeys.Control)
            && Win32Interop.RegisterHotKey(source.Handle, cancelId, modifiers | Win32Interop.MOD_NOREPEAT, VkCancel))
        {
            registered.Add(cancelId);
            ok = true;
        }
        return ok;
    }

    private void Release(int id)
    {
        if (registered.Remove(id)) Win32Interop.UnregisterHotKey(source.Handle, id);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == Win32Interop.WM_HOTKEY && registered.Contains(wParam.ToInt32()))
        {
            handled = true;
            HotkeyPressed?.Invoke();
        }
        return nint.Zero;
    }

    public void Dispose()
    {
        retry?.Stop();
        foreach (var id in registered.ToList()) Release(id);
        source.RemoveHook(WndProc);
        source.Dispose();
    }
}
