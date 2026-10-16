using System.Windows;
using UnstuckTaskbar.Hotkeys;
using UnstuckTaskbar.Recovery;
using UnstuckTaskbar.Settings;
using UnstuckTaskbar.Startup;
using UnstuckTaskbar.Tray;

namespace UnstuckTaskbar;

/// <summary>Tray-only app: no main window, ever. See the UnstuckTaskbar plan for the full design.</summary>
public partial class App : Application
{
    private const string InstanceMutexName = @"Local\UnstuckTaskbar.Instance";
    private Mutex? instanceMutex;
    private HotkeyManager? hotkeyManager;
    private TrayIconService? trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        instanceMutex = new Mutex(initiallyOwned: true, InstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            // Another instance already owns the tray icon and hotkey; this one exits quietly.
            Shutdown();
            return;
        }

        var exePath = Environment.ProcessPath!;
        var settingsService = new SettingsService();
        var current = settingsService.Load();
        var runner = new UnstuckRunner();

        hotkeyManager = new HotkeyManager();
        trayIcon = new TrayIconService(settingsService, hotkeyManager, runner, exePath);

        hotkeyManager.HotkeyPressed += () =>
        {
            var result = runner.Run();
            if (result is not null) trayIcon.ShowBalloon("Unstuck", result.Summary);
        };

        if (HotkeyGesture.TryParse(current.HotkeyGesture, out var gesture))
            hotkeyManager.RegisterCurrent(gesture);

        StartupRegistrar.Apply(current.StartWithWindows, exePath);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        trayIcon?.Dispose();
        hotkeyManager?.Dispose();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
