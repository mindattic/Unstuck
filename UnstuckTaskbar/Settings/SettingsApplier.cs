using UnstuckTaskbar.Hotkeys;

namespace UnstuckTaskbar.Settings;

/// <summary>
/// Puts <see cref="TaskbarSettings"/> into effect: registers the hotkey and refreshes the tooltip. The hotkey is
/// only re-registered when it actually changed, so an unrelated save in Vault doesn't drop and re-take it.
/// </summary>
public sealed class SettingsApplier
{
    private readonly Action<HotkeyGesture?> setHotkey;
    private readonly Action refreshTooltip;
    private TaskbarSettings? applied;

    public SettingsApplier(Action<HotkeyGesture?> setHotkey, Action refreshTooltip)
    {
        this.setHotkey = setHotkey;
        this.refreshTooltip = refreshTooltip;
    }

    public void Apply(TaskbarSettings settings)
    {
        if (applied is null || applied.HotkeyGesture != settings.HotkeyGesture)
            setHotkey(HotkeyGesture.TryParse(settings.HotkeyGesture, out var gesture) ? gesture : null);
        refreshTooltip();
        applied = new TaskbarSettings { HotkeyGesture = settings.HotkeyGesture };
    }
}
