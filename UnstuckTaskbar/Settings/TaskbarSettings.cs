using MindAttic.Vault.Settings.Manifest;

namespace UnstuckTaskbar.Settings;

/// <summary>
/// Per-machine preferences, edited in MindAttic.Vault.Desktop (tray menu → Settings…) and kept in
/// <c>%LocalAppData%\MindAttic\UnstuckTaskbar\settings.json</c>: a hotkey belongs to this PC, not the roaming
/// profile. (Starting at sign-in used to be a setting here too; MindAttic.Scheduler owns that now, UNS-T9. An old
/// file's <c>startWithWindows</c> key is simply ignored.)
/// </summary>
[SettingsApp(SettingsService.AppId, "Unstuck", Local = true, Icon = "wrench",
    Description = "Tray app that runs /unstuck from a global hotkey.")]
public sealed class TaskbarSettings
{
    /// <summary>The assigned hotkey, in <see cref="Hotkeys.HotkeyGesture"/>'s settings-string form, or null if unset.</summary>
    [Setting("Recovery hotkey", Group = "Hotkey", Order = 1,
        Help = "Global shortcut that runs /unstuck, written as modifiers and a key: \"Control+Alt+U\", \"Control+Shift+F12\", or a media key alone such as \"MediaPlayPause\". Leave empty for none.")]
    public string? HotkeyGesture { get; set; }
}
