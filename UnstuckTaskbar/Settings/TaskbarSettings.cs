using MindAttic.Vault.Settings.Manifest;

namespace UnstuckTaskbar.Settings;

/// <summary>
/// Per-machine preferences, edited in MindAttic.Vault.Desktop (tray menu → Settings…) and kept in
/// <c>%LocalAppData%\MindAttic\UnstuckTaskbar\settings.json</c>: a hotkey and "start with Windows"
/// belong to this PC, not the roaming profile.
/// </summary>
[SettingsApp(SettingsService.AppId, "Unstuck Taskbar", Local = true, Icon = "wrench",
    Description = "Tray app that runs /unstuck from a global hotkey.")]
public sealed class TaskbarSettings
{
    /// <summary>The assigned hotkey, in <see cref="Hotkeys.HotkeyGesture"/>'s settings-string form, or null if unset.</summary>
    [Setting("Recovery hotkey", Group = "Hotkey", Order = 1,
        Help = "Global shortcut that runs /unstuck, written as modifiers and a key: \"Control+Alt+U\", \"Control+Shift+F12\", or a media key alone such as \"MediaPlayPause\". Leave empty for none.")]
    public string? HotkeyGesture { get; set; }

    [Setting("Start with Windows", Group = "Startup", Order = 1,
        Help = "Start Unstuck Taskbar when you sign in to Windows.")]
    public bool StartWithWindows { get; set; } = true;
}
