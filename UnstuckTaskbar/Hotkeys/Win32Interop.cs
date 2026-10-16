using System.Runtime.InteropServices;

namespace UnstuckTaskbar.Hotkeys;

/// <summary>Raw Win32 global-hotkey P/Invoke. See <see cref="HotkeyManager"/> for the WM_HOTKEY pump.</summary>
internal static class Win32Interop
{
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    /// <summary>Without this, Windows auto-repeats WM_HOTKEY while a non-media key is held down.</summary>
    public const uint MOD_NOREPEAT = 0x4000;

    public const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(nint hWnd, int id);
}
