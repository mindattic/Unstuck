# Tray app

`UnstuckTaskbar.exe` is a small app that runs in the notification area. It runs the recovery when you press a
global hotkey or double-click its icon, so you don't need a working mouse or an open terminal. It has no window of
its own.

## Install

Download **`Unstuck-Setup-v<N>.exe`** from the [latest release](https://github.com/mindattic/Unstuck/releases/latest)
and run it. It installs per user into `%LOCALAPPDATA%\Programs\Unstuck` (no admin rights, no .NET needed), adds
*Unstuck*, *Unstuck (check only)* and *Unstuck Tray* to the Start menu and an *Unstuck* desktop shortcut, makes the
tray app start when you sign in, and starts it. Run a newer installer over the old one to update.

**From source** (developers): run `install.bat` in the repo root. It needs the .NET 10 SDK and the MindAttic
packages, builds the tray app into `C:\Apps\Unstuck` and writes `C:\Apps\Unstuck\run.bat`, a launcher menu with
*Deploy & Run* and *Run*. Running it again rebuilds only what changed; `install.bat -Force` rebuilds everything.

Only one copy of the tray app runs at a time. With MindAttic.Scheduler installed, its `UnstuckTaskbar` job starts
the app at sign-in, and the installer doesn't add its own sign-in entry. Older versions registered their own
`\MindAttic\UnstuckTaskbar\Logon` task; the app now deletes it when it starts, so the two don't compete.

## The hotkeys: Ctrl+Alt+Break and Ctrl+Alt+U

**Ctrl+Alt+Break** and **Ctrl+Alt+U** both run the recovery from anywhere, in any app, even when the mouse is dead.
Both are built in and always on; there's nothing to set up. Break shares a key with Pause, usually at the top right
of the keyboard. On a laptop without one, it's often <kbd>Fn</kbd> plus another key; check your keyboard's labels.

If another app is holding either one when Unstuck starts, a balloon says so. Unstuck keeps retrying every
30 seconds and takes the shortcut as soon as it's free.

### An extra hotkey (optional)

Right-click the tray icon and choose **Set Alternate Hotkey…**: press the key combination you want, and it's active
immediately - no restart, no gesture string to type. **Clear Alternate Hotkey** removes it again (Ctrl+Alt+Break and
Ctrl+Alt+U keep working either way).

The same setting can still be edited in MindAttic Vault's Settings page (right-click the tray icon → **Settings…**):
set **Extra hotkey** to a gesture string, for example `Control+Shift+F12`, or a media key alone such as
`MediaPlayPause`. A key used on its own must have no other job (a media or volume key, or F13–F24); anything else
needs at least one modifier. Either way of setting it applies as soon as it's saved - they write the same file.

Without MindAttic Vault, edit `%LOCALAPPDATA%\MindAttic\UnstuckTaskbar\settings.json`:

```json
{
  "hotkeyGesture": "Control+Shift+F12"
}
```

## The menu

| Item | Does |
|---|---|
| *Hotkey: …* | Shows the active hotkeys: Ctrl+Alt+Break and Ctrl+Alt+U, plus the extra one if set. |
| **Run Unstuck Now** | Runs the default recovery. Double-clicking the icon does the same. |
| Run With → Check Only | Reports what it would do and changes nothing. |
| Run With → Restart Explorer / Restart PowerToys / Close Overlay Apps | Runs the recovery with that option added. |
| Run With → Everything | Runs with all three options. |
| Show Last Report | Opens the full report of the latest run. |
| Show History | Opens the log of earlier runs. |
| Settings… | Opens the extra-hotkey setting in MindAttic Vault. |
| Set Alternate Hotkey… | Captures a key combination for the extra hotkey, right from the menu. |
| Clear Alternate Hotkey | Removes the extra hotkey (grayed out when none is set). |
| Exit | Closes the tray app and frees the hotkeys. |

After each run, a balloon sums up what was done. **Click the balloon to open the full report.** A warning balloon
means something needs you, for example a gamepad button or key that is still held.

Runs can't overlap: pressing the hotkey repeatedly while a run is in progress, or within 2 seconds of the last one,
does nothing. A run that takes longer than 90 seconds is stopped.

## Where things are

| What | Where |
|---|---|
| App and installed script | `%LOCALAPPDATA%\Programs\Unstuck\` (installer) or `C:\Apps\Unstuck\` (from source) |
| Settings | `%LOCALAPPDATA%\MindAttic\UnstuckTaskbar\settings.json` |
| Reports | `%LOCALAPPDATA%\MindAttic\Unstuck\last-run.txt` and `history.log` |
| Start at sign-in | The `Unstuck` value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, or with MindAttic.Scheduler, Task Scheduler → `\MindAttic\Scheduler\UnstuckTaskbar` |

## Uninstall

Use **Windows Settings → Apps → Unstuck → Uninstall**. It stops the tray app and removes the install folder, its
shortcuts and its sign-in entry. Settings and reports in `%LOCALAPPDATA%\MindAttic` are kept. If MindAttic.Scheduler
runs the app, also remove its `UnstuckTaskbar` job from its `jobs.json`.
