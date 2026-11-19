using System.ComponentModel;
using System.Diagnostics;
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
        // Clicking a run's balloon opens its full report.
        icon.BalloonTipClicked += (_, _) => { if (icon.Tag is true) OpenFile(UnstuckRunner.LastReportPath); };
        UpdateTooltip();

        hotkeyManager.RegistrationFailed += gesture =>
            ShowBalloon("Hotkey unavailable", $"{gesture.ToDisplayString()} is already used by another app. Pick another one in Settings.", Forms.ToolTipIcon.Warning);
    }

    private string CurrentHotkeyText() =>
        HotkeyGesture.TryParse(settings.Load().HotkeyGesture, out var gesture) ? gesture.ToDisplayString() : "no hotkey set";

    public void UpdateTooltip() => SetTooltip($"Unstuck - {CurrentHotkeyText()} runs recovery");

    private void SetTooltip(string text) => icon.Text = text.Length > 127 ? text[..127] : text;

    public void ShowBalloon(string title, string text, Forms.ToolTipIcon kind = Forms.ToolTipIcon.Info, bool opensReport = false)
    {
        icon.Tag = opensReport;
        icon.ShowBalloonTip(5000, title, text, kind);
    }

    /// <summary>Runs the plain recovery (what the hotkey and a double-click do).</summary>
    public void RunNow() => Run(RecoveryOptions.None);

    /// <summary>Runs the recovery in the background and shows its summary; a no-op during the cooldown.</summary>
    public async void Run(RecoveryOptions options)
    {
        // async void: an escaped exception would take the whole tray app (and the hotkey) down with it.
        try
        {
            if (runner.IsRunning) return;
            SetTooltip("Unstuck - running recovery...");
            var result = await runner.RunAsync(options);
            UpdateTooltip();
            if (result is null) return;
            var title = options.HasFlag(RecoveryOptions.Check) ? "Unstuck (check only)" : "Unstuck";
            ShowBalloon(title, result.Summary + (File.Exists(UnstuckRunner.LastReportPath) ? "\nClick for the full report." : ""),
                result.NeedsAttention ? Forms.ToolTipIcon.Warning : Forms.ToolTipIcon.Info, opensReport: true);
        }
        catch (Exception ex)
        {
            UpdateTooltip();
            ShowBalloon("Unstuck failed", ex.Message, Forms.ToolTipIcon.Error);
        }
    }

    /// <summary>Removes the legacy logon task (MindAttic.Scheduler starts the app now) and keeps the Start Menu
    /// shortcut current; a failure is shown, not thrown.</summary>
    public void PrepareStartup()
    {
        try { StartupRegistrar.Apply(exePath); }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or COMException)
        {
            ShowBalloon("Start Menu shortcut failed", ex.Message, Forms.ToolTipIcon.Warning);
        }
    }

    private void BuildMenu()
    {
        var menu = icon.ContextMenuStrip!;
        var old = menu.Items.Cast<Forms.ToolStripItem>().ToList();
        menu.Items.Clear();
        foreach (var item in old) item.Dispose(); // Clear() doesn't dispose; rebuilt fresh on every open.

        menu.Items.Add(new Forms.ToolStripLabel($"Hotkey: {CurrentHotkeyText()}") { ForeColor = System.Drawing.SystemColors.GrayText });
        menu.Items.Add(new Forms.ToolStripSeparator());

        var runNow = new Forms.ToolStripMenuItem("Run Unstuck Now", null, (_, _) => RunNow()) { Enabled = !runner.IsRunning };
        runNow.Font = new System.Drawing.Font(runNow.Font, System.Drawing.FontStyle.Bold); // the double-click action
        menu.Items.Add(runNow);

        var runWith = new Forms.ToolStripMenuItem("Run With") { Enabled = !runner.IsRunning };
        runWith.DropDownItems.Add("Check Only (change nothing)", null, (_, _) => Run(RecoveryOptions.Check));
        runWith.DropDownItems.Add(new Forms.ToolStripSeparator());
        runWith.DropDownItems.Add("Restart Explorer", null, (_, _) => Run(RecoveryOptions.Explorer));
        runWith.DropDownItems.Add("Restart PowerToys (mouse hooks)", null, (_, _) => Run(RecoveryOptions.Hooks));
        runWith.DropDownItems.Add("Close Overlay Apps", null, (_, _) => Run(RecoveryOptions.Overlays));
        runWith.DropDownItems.Add(new Forms.ToolStripSeparator());
        runWith.DropDownItems.Add("Everything", null, (_, _) => Run(RecoveryOptions.Explorer | RecoveryOptions.Hooks | RecoveryOptions.Overlays));
        menu.Items.Add(runWith);

        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Show Last Report", null, (_, _) => OpenFile(UnstuckRunner.LastReportPath))
            { Enabled = File.Exists(UnstuckRunner.LastReportPath) });
        menu.Items.Add(new Forms.ToolStripMenuItem("Show History", null, (_, _) => OpenFile(UnstuckRunner.HistoryPath))
            { Enabled = File.Exists(UnstuckRunner.HistoryPath) });
        menu.Items.Add("Settings...", null, (_, _) => OpenSettings());

        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());
    }

    private void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            ShowBalloon("No report yet", $"Nothing has been saved to {path} yet.");
            return;
        }
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Win32Exception)
        {
            // No app associated with .txt/.log: Notepad is always there.
            try { Process.Start("notepad.exe", $"\"{path}\""); }
            catch (Win32Exception ex) { ShowBalloon("Couldn't open the report", ex.Message, Forms.ToolTipIcon.Warning); }
        }
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
