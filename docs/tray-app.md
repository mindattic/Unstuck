# Tray app

`UnstuckTaskbar.exe` is a small app that runs in the notification area. It runs the recovery when you press a
global hotkey or double-click its icon, so you don't need a working mouse or an open terminal. It has no window of
its own.

## Install

**One click:** download **[Install-Unstuck.bat](https://github.com/mindattic/Unstuck/releases/latest/download/Install-Unstuck.bat)**
and double-click it. It installs into `C:\Apps\Unstuck` (no admin rights, no .NET needed), adds an *Unstuck* desktop
shortcut and Start menu folder, makes the tray app start when you sign in, and starts it. Run it again to update.

From PowerShell, the same thing in one line:

```powershell
irm https://raw.githubusercontent.com/mindattic/Unstuck/main/setup/Install-Unstuck.ps1 | iex
```

To uninstall, use **Start menu → Unstuck → Uninstall Unstuck**.

**From source** (developers): run `install.bat` in the repo root. It needs the .NET 10 SDK and the MindAttic
packages, builds the tray app into `C:\Apps\Unstuck` and writes `C:\Apps\Unstuck\run.bat`, a launcher menu with
*Deploy & Run* and *Run*. Running it again rebuilds only what changed; `install.bat -Force` rebuilds everything.

Only one copy of the tray app runs at a time. With MindAttic.Scheduler installed, its `UnstuckTaskbar` job starts
the app at sign-in instead of the Startup-folder shortcut. Older versions registered their own
`\MindAttic\UnstuckTaskbar\Logon` task; the app now deletes it when it starts, so the two don't compete.

## The hotkey: Ctrl+Alt+Break

**Ctrl+Alt+Break** runs the recovery from anywhere, in any app, even when the mouse is dead. It's built in and always
on; there's nothing to set up. Break shares a key with Pause, usually at the top right of the keyboard. On a laptop
without one, it's often <kbd>Fn</kbd> plus another key; check your keyboard's labels.

If another app is holding Ctrl+Alt+Break when Unstuck starts, a balloon says so. Unstuck keeps retrying every
30 seconds and takes the shortcut as soon as it's free.

### An extra hotkey (optional)

Right-click the tray icon, choose **Settings…**, and set **Extra hotkey** to add a second shortcut, for example
`Control+Shift+F12`, or a media key alone such as `MediaPlayPause`. A key used on its own must have no other job (a
media or volume key, or F13–F24); anything else needs at least one modifier. Changes apply as soon as they're saved.

Without MindAttic Vault, edit `%LOCALAPPDATA%\MindAttic\UnstuckTaskbar\settings.json`:

```json
{
  "hotkeyGesture": "Control+Shift+F12"
}
```

## The menu

| Item | Does |
|---|---|
| *Hotkey: …* | Shows the active hotkeys: Ctrl+Alt+Break, plus the extra one if set. |
| **Run Unstuck Now** | Runs the default recovery. Double-clicking the icon does the same. |
| Run With → Check Only | Reports what it would do and changes nothing. |
| Run With → Restart Explorer / Restart PowerToys / Close Overlay Apps | Runs the recovery with that option added. |
| Run With → Everything | Runs with all three options. |
| Show Last Report | Opens the full report of the latest run. |
| Show History | Opens the log of earlier runs. |
| Settings… | Opens the extra-hotkey setting. |
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
| Start at sign-in | Task Scheduler → `\MindAttic\Scheduler\UnstuckTaskbar` (managed by MindAttic.Scheduler) |

## Uninstall

Use **Start menu → Unstuck → Uninstall Unstuck**. It stops the tray app and removes `C:\Apps\Unstuck` and every
shortcut the installer made. Settings and reports in `%LOCALAPPDATA%\MindAttic` are kept. If MindAttic.Scheduler
runs the app, also remove its `UnstuckTaskbar` job from its `jobs.json`.
