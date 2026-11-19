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
    /// <summary>Ctrl+Alt+Break: always registered, whatever the settings say (UNS-T32).</summary>
    public static readonly HotkeyGesture BuiltIn = new(ModifierKeys.Control | ModifierKeys.Alt, Key.Pause);

    /// <summary>Ctrl+Alt+U: a second permanent hotkey, registered alongside <see cref="BuiltIn"/> regardless of
    /// settings (UNS-T35) - independent of the one extra hotkey the user can still set themselves
    /// (<c>TaskbarSettings.HotkeyGesture</c>).</summary>
    public static readonly HotkeyGesture BuiltInExtra = new(ModifierKeys.Control | ModifierKeys.Alt, Key.U);

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

    /// <summary>A key alone is only valid for keys with no default system meaning, e.g. a media key —
    /// a bare letter or F5 would be swallowed system-wide.</summary>
    public bool IsValid => Key is not (Key.None or Key.System) && (Modifiers != ModifierKeys.None || IsStandaloneKey(Key));

    private static bool IsStandaloneKey(Key key) =>
        key is Key.MediaPlayPause or Key.MediaNextTrack or Key.MediaPreviousTrack or Key.MediaStop
            or Key.VolumeMute or Key.VolumeUp or Key.VolumeDown
            or >= Key.F13 and <= Key.F24;

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
        Key.Pause => "Break",
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

        // Enum.TryParse also accepts numbers ("1" would be Key value 1, Cancel; "9999" an undefined key). A lone digit
        // means that digit key (WPF names them D0-D9); any other number is rejected.
        var keyName = segments[^1];
        if (keyName.Equals("Break", StringComparison.OrdinalIgnoreCase)) keyName = nameof(Key.Pause);
        if (keyName.Length == 1 && char.IsAsciiDigit(keyName[0])) keyName = "D" + keyName;
        else if (keyName.All(char.IsAsciiDigit)) return false;
        if (!Enum.TryParse<Key>(keyName, ignoreCase: true, out var key) || key == Key.None || !Enum.IsDefined(key)) return false;

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
