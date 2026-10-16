using System.Windows.Input;

namespace UnstuckTaskbar.Hotkeys;

/// <summary>
/// A hotkey: zero or more modifiers plus one key, including WPF's extended media/volume keys
/// (<see cref="Key.MediaPlayPause"/>, <see cref="Key.MediaNextTrack"/>, etc. — those can be
/// registered with no modifiers at all). Converts to/from the string stored in settings.json and
/// to the Win32 (modifiers, virtual-key) pair <see cref="Win32Interop.RegisterHotKey"/> needs.
/// </summary>
public readonly record struct HotkeyGesture(ModifierKeys Modifiers, Key Key)
{
    private static readonly (ModifierKeys Flag, string Token)[] ModifierOrder =
    [
        (ModifierKeys.Control, "Control"),
        (ModifierKeys.Alt, "Alt"),
        (ModifierKeys.Shift, "Shift"),
        (ModifierKeys.Windows, "Windows"),
    ];

    private static readonly (ModifierKeys Flag, string Label)[] DisplayOrder =
    [
        (ModifierKeys.Control, "Ctrl"),
        (ModifierKeys.Alt, "Alt"),
        (ModifierKeys.Shift, "Shift"),
        (ModifierKeys.Windows, "Win"),
    ];

    /// <summary>A key alone is only valid for keys with no default system meaning, e.g. a media key.</summary>
    public bool IsValid => Key != Key.None && Key != Key.System;

    /// <summary>The stable form persisted to settings.json, e.g. "Control+Alt+U" or "MediaPlayPause".</summary>
    public string ToSettingsString()
    {
        var parts = new List<string>();
        foreach (var (flag, token) in ModifierOrder)
            if (Modifiers.HasFlag(flag)) parts.Add(token);
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    /// <summary>The human-friendly form shown in the tray menu/dialog, e.g. "Ctrl+Alt+U" or "Media Play/Pause".</summary>
    public string ToDisplayString()
    {
        var parts = new List<string>();
        foreach (var (flag, label) in DisplayOrder)
            if (Modifiers.HasFlag(flag)) parts.Add(label);
        parts.Add(DisplayKeyName(Key));
        return string.Join("+", parts);
    }

    private static string DisplayKeyName(Key key) => key switch
    {
        Key.MediaPlayPause => "Media Play/Pause",
        Key.MediaNextTrack => "Media Next",
        Key.MediaPreviousTrack => "Media Previous",
        Key.MediaStop => "Media Stop",
        Key.VolumeMute => "Volume Mute",
        Key.VolumeUp => "Volume Up",
        Key.VolumeDown => "Volume Down",
        _ => key.ToString(),
    };

    public static bool TryParse(string? value, out HotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var segments = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0) return false;

        var modifiers = ModifierKeys.None;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var matched = false;
            foreach (var (flag, token) in ModifierOrder)
            {
                if (!string.Equals(segments[i], token, StringComparison.OrdinalIgnoreCase)) continue;
                modifiers |= flag;
                matched = true;
                break;
            }
            if (!matched) return false;
        }

        if (!Enum.TryParse<Key>(segments[^1], ignoreCase: true, out var key) || key == Key.None) return false;

        gesture = new HotkeyGesture(modifiers, key);
        return gesture.IsValid;
    }

    /// <summary>The Win32 pair <c>RegisterHotKey</c> needs: fsModifiers and the virtual-key code.</summary>
    public (uint Modifiers, uint VirtualKey) ToWin32()
    {
        uint mod = 0;
        if (Modifiers.HasFlag(ModifierKeys.Alt)) mod |= Win32Interop.MOD_ALT;
        if (Modifiers.HasFlag(ModifierKeys.Control)) mod |= Win32Interop.MOD_CONTROL;
        if (Modifiers.HasFlag(ModifierKeys.Shift)) mod |= Win32Interop.MOD_SHIFT;
        if (Modifiers.HasFlag(ModifierKeys.Windows)) mod |= Win32Interop.MOD_WIN;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(Key);
        return (mod, vk);
    }
}
