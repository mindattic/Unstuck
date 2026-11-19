# Tray app

`UnstuckTaskbar.exe` is a small app that runs in the notification area. It runs the recovery when you press a
global hotkey or double-click its icon, so you don't need a working mouse or an open terminal. It has no window of
its own.

## Install

Run **`install.bat`** from the repo root, or `run.ps1` from PowerShell. You need the .NET 10 SDK. The installer:

1. copies `unstuck.ps1` to `C:\Apps\Unstuck\`,
2. builds `UnstuckTaskbar.exe` into the same folder,
3. writes `C:\Apps\Unstuck\run.bat`, a launcher menu with *Deploy & Run* and *Run*.

Running it again only rebuilds what changed; `install.bat -Force` rebuilds everything. If the tray app is running,
the installer stops it to replace the exe and starts it again afterwards.

Start it from **Start menu → MindAttic → UnstuckTaskbar**, or run `C:\Apps\Unstuck\UnstuckTaskbar.exe`. The first
time it runs, it adds that Start menu shortcut and, with *Start with Windows* on (the default), a logon task under
`\MindAttic\UnstuckTaskbar\` in Task Scheduler so it starts every time you sign in. Only one copy runs at a time.

## Set a hotkey

There's no hotkey until you choose one. Right-click the tray icon and choose **Settings…** to open the *Unstuck* tab
in MindAttic Vault, then set **Recovery hotkey**, for example:

- `Control+Alt+U`
- `Control+Shift+F12`
- `MediaPlayPause`, or another media or volume key, used on its own

A key used on its own must be one with no other job: a media or volume key, or F13–F24. Anything else needs at
least one modifier, so a plain letter or F5 can't be taken over system-wide. If another app already owns the
shortcut, a balloon says so; pick a different one. Changes apply as soon as they're saved, without a restart.

Without MindAttic Vault, edit `%LOCALAPPDATA%\MindAttic\UnstuckTaskbar\settings.json` directly:

```json
{
  "HotkeyGesture": "Control+Alt+U",
  "StartWithWindows": true
}
```

## The menu

| Item | Does |
|---|---|
| *Hotkey: …* | Shows the current hotkey. |
| **Run Unstuck Now** | Runs the default recovery. Double-clicking the icon does the same. |
| Run With → Check Only | Reports what it would do and changes nothing. |
| Run With → Restart Explorer / Restart PowerToys / Close Overlay Apps | Runs the recovery with that option added. |
| Run With → Everything | Runs with all three options. |
| Show Last Report | Opens the full report of the latest run. |
| Show History | Opens the log of earlier runs. |
| Settings… | Opens the hotkey and startup settings. |
| Exit | Closes the tray app and frees the hotkey. |

After each run, a balloon sums up what was done. **Click the balloon to open the full report.** A warning balloon
means something needs you, for example a gamepad button or key that is still held.

Runs can't overlap: pressing the hotkey repeatedly while a run is in progress, or within 2 seconds of the last one,
does nothing. A run that takes longer than 90 seconds is stopped.

## Where things are

| What | Where |
|---|---|
| App and installed script | `C:\Apps\Unstuck\` |
| Settings | `%LOCALAPPDATA%\MindAttic\UnstuckTaskbar\settings.json` |
| Reports | `%LOCALAPPDATA%\MindAttic\Unstuck\last-run.txt` and `history.log` |
| Logon task | Task Scheduler → `\MindAttic\UnstuckTaskbar\Logon` |

## Uninstall

1. Right-click the tray icon and choose **Exit**.
2. In the settings, turn off *Start with Windows*, or delete the `\MindAttic\UnstuckTaskbar\Logon` task in Task
   Scheduler.
3. Delete `C:\Apps\Unstuck\`, the *UnstuckTaskbar* Start menu shortcut and `%LOCALAPPDATA%\MindAttic\Unstuck*`.
