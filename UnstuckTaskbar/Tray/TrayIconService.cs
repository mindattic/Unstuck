using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using MindAttic.Vault.Launch;
using UnstuckTaskbar.Hotkeys;
using UnstuckTaskbar.Recovery;
using UnstuckTaskbar.Settings;
using UnstuckTaskbar.Startup;
using Forms = System.Windows.Forms;

namespace UnstuckTaskbar.Tray;

/// <summary>
/// The whole UI: a tray icon with a tooltip, a context menu, and a balloon tip after each recovery
/// run. Modeled directly on MindAttic.Orchestrator's own <c>Tray.cs</c> (same
/// <c>Forms.NotifyIcon</c> + <c>ContextMenuStrip</c>-rebuilt-on-open approach), minus the dashboard
/// window this app doesn't have.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon icon;
    private readonly SettingsService settings;
    private readonly HotkeyManager hotkeyManager;
    private readonly UnstuckRunner runner;
    private readonly string exePath;

    public TrayIconService(SettingsService settings, HotkeyManager hotkeyManager, UnstuckRunner runner, string exePath)
    {
        this.settings = settings;
        this.hotkeyManager = hotkeyManager;
        this.runner = runner;
        this.exePath = exePath;

        icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath) ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip(),
        };
        icon.ContextMenuStrip!.Opening += (_, _) => BuildMenu();
        icon.DoubleClick += (_, _) => RunNow();
        UpdateTooltip();

        hotkeyManager.RegistrationFailed += gesture =>
            ShowBalloon("Hotkey unavailable", $"{gesture.ToDisplayString()} is already used by another app.");
    }

    public void UpdateTooltip()
    {
        var current = settings.Load();
        var hotkeyText = HotkeyGesture.TryParse(current.HotkeyGesture, out var gesture)
            ? gesture.ToDisplayString()
            : "no hotkey set";
        var text = $"UnstuckTaskbar - {hotkeyText} runs /unstuck";
        icon.Text = text.Length > 127 ? text[..127] : text;
    }

    public void ShowBalloon(string title, string text) =>
        icon.ShowBalloonTip(4000, title, text, Forms.ToolTipIcon.Info);

    /// <summary>Runs the recovery in the background and shows its summary; a no-op during the cooldown.</summary>
    public async void RunNow()
    {
        var result = await runner.RunAsync();
        if (result is not null) ShowBalloon("Unstuck", result.Summary);
    }

    /// <summary>Applies "Start with Windows"; a failure (e.g. task creation blocked by policy) is shown, not thrown.</summary>
    public void ApplyStartWithWindows(bool enabled)
    {
        try { StartupRegistrar.Apply(enabled, exePath); }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or COMException)
        {
            ShowBalloon("Start with Windows failed", ex.Message);
        }
    }

    private void BuildMenu()
    {
        var menu = icon.ContextMenuStrip!;
        var old = menu.Items.Cast<Forms.ToolStripItem>().ToList();
        menu.Items.Clear();
        foreach (var item in old) item.Dispose(); // Clear() doesn't dispose; rebuilt fresh on every open.

        menu.Items.Add("Run Unstuck Now", null, (_, _) => RunNow());
        menu.Items.Add("Settings...", null, (_, _) => OpenSettings());

        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());
    }

    /// <summary>Settings are edited in MindAttic.Vault.Desktop; saves come back through <see cref="SettingsService.Changed"/>.</summary>
    public void OpenSettings()
    {
        if (!VaultSettings.Open(SettingsService.AppId))
            ShowBalloon("MindAttic Vault isn't installed", $"Install MindAttic Vault to edit these settings, or edit {settings.FilePath}.");
    }

    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
    }
}
