namespace UnstuckTaskbar.Settings;

/// <summary>Persisted per-machine preferences. Serialized by <see cref="SettingsService"/>.</summary>
public sealed class TaskbarSettings
{
    /// <summary>The assigned hotkey, in <see cref="Hotkeys.HotkeyGesture"/>'s settings-string form, or null if unset.</summary>
    public string? HotkeyGesture { get; set; }

    public bool StartWithWindows { get; set; } = true;
}
