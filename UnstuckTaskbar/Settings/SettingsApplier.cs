using UnstuckTaskbar.Hotkeys;

namespace UnstuckTaskbar.Settings;

/// <summary>
/// Puts <see cref="TaskbarSettings"/> into effect: registers the hotkey, applies "start with Windows",
/// refreshes the tooltip. Only what changed since the last apply is touched, so a save in Vault that
/// edits one field doesn't re-run the other.
/// </summary>
public sealed class SettingsApplier
{
    private readonly Action<HotkeyGesture?> setHotkey;
    private readonly Action<bool> setStartWithWindows;
    private readonly Action refreshTooltip;
    private TaskbarSettings? applied;

    public SettingsApplier(Action<HotkeyGesture?> setHotkey, Action<bool> setStartWithWindows, Action refreshTooltip)
    {
        this.setHotkey = setHotkey;
        this.setStartWithWindows = setStartWithWindows;
        this.refreshTooltip = refreshTooltip;
    }

    public void Apply(TaskbarSettings settings)
    {
        if (applied is null || applied.HotkeyGesture != settings.HotkeyGesture)
            setHotkey(HotkeyGesture.TryParse(settings.HotkeyGesture, out var gesture) ? gesture : null);
        if (applied is null || applied.StartWithWindows != settings.StartWithWindows)
            setStartWithWindows(settings.StartWithWindows);
        refreshTooltip();
        applied = new TaskbarSettings { HotkeyGesture = settings.HotkeyGesture, StartWithWindows = settings.StartWithWindows };
    }
}
