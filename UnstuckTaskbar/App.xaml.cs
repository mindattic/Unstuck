using System.Windows;
using MindAttic.Vault.Credentials;
using MindAttic.Vault.Settings.Manifest;
using UnstuckTaskbar.Hotkeys;
using UnstuckTaskbar.Recovery;
using UnstuckTaskbar.Settings;
using UnstuckTaskbar.Tray;

namespace UnstuckTaskbar;

/// <summary>Tray-only app: no main window, ever. See the UnstuckTaskbar plan for the full design.</summary>
public partial class App : Application
{
    private const string InstanceMutexName = @"Local\UnstuckTaskbar.Instance";
    private Mutex? instanceMutex;
    private HotkeyManager? hotkeyManager;
    private TrayIconService? trayIcon;
    private SettingsService? settingsService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        if (SettingsManifestCommand.TryRun<TaskbarSettings>(e.Args))
        {
            Shutdown();
            return;
        }

        instanceMutex = new Mutex(initiallyOwned: true, InstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            // Another instance already owns the tray icon and hotkey; this one exits quietly.
            Shutdown();
            return;
        }

        KeyImporter.ImportAppScopedFor(SettingsService.AppId);

        var exePath = Environment.ProcessPath!;
        settingsService = new SettingsService();
        var current = settingsService.LoadOrSeed();
        var runner = new UnstuckRunner();

        hotkeyManager = new HotkeyManager();
        trayIcon = new TrayIconService(settingsService, hotkeyManager, runner, exePath);
        hotkeyManager.HotkeyPressed += trayIcon.RunNow;

        var applier = new SettingsApplier(
            gesture => { if (gesture is { } g) hotkeyManager.RegisterCurrent(g); else hotkeyManager.Unregister(); },
            trayIcon.ApplyStartWithWindows,
            trayIcon.UpdateTooltip);
        applier.Apply(current);

        // Edits saved in MindAttic.Vault.Desktop take effect here without a restart.
        settingsService.Changed += changed => Dispatcher.BeginInvoke(() => applier.Apply(changed));
        settingsService.Watch();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        settingsService?.Dispose();
        trayIcon?.Dispose();
        hotkeyManager?.Dispose();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
