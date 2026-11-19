# Troubleshooting

## The mouse is dead: how do I even start it?

The keyboard almost always still works:

- **Tray app installed:** press your hotkey.
- **Otherwise:** press <kbd>Win</kbd>+<kbd>R</kbd>, type the full path to `unstuck.bat` (for example
  `D:\Projects\MindAttic\Unstuck\unstuck.bat`) and press <kbd>Enter</kbd>. If it's installed, `C:\Apps\Unstuck\run.bat`
  works too; press <kbd>2</kbd> for *Run*.
- **In Claude Code:** type `/unstuck`.

## Unstuck ran, found nothing, and clicks are still dead

Escalate one step at a time:

1. `unstuck.bat hooks` restarts PowerToys, whose mouse hooks are a common cause.
2. `unstuck.bat overlays` closes game and chat overlays. Try it especially if the report listed some as running.
3. `unstuck.bat explorer` force-restarts Explorer.
4. <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Esc</kbd> opens Task Manager, and <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Del</kbd>
   opens the security screen. Both break a full-screen app's hold on input. Press <kbd>Esc</kbd> to go back.
5. Sign out and back in. If that's needed, note what was running and open an issue.

## "STUCK GAMEPAD INPUT"

A controller button's release never reached Windows. Software can't clear it. Press and release the button the
report names on the controller, or press each one if you aren't sure which controller it is. If that doesn't help,
switch the controller off or unplug it.

## "Keys still held: …" after the run

A physical key may really be stuck, or its release got lost. Press and release that key once. If it's listed
again on the next run, check the keyboard itself.

## The pointer shows the wrong shape

For example, there's no text cursor over a text box. Unstuck moves the cursor a pixel to fix this. If it stays
wrong, move the mouse over a different window and back.

## Something I needed was closed

Look at the report (*Show Last Report* in the tray menu, or `%LOCALAPPDATA%\MindAttic\Unstuck\history.log`) to see
why it was chosen: not responding, its share of RAM, or its CPU use. Run `unstuck.bat check` first next time to
preview what would happen.

## Tray app problems

| Symptom | Fix |
|---|---|
| "Hotkey unavailable" balloon | Another app owns that shortcut. Pick a different one in Settings. |
| The hotkey does nothing | Make sure it's set (the first line of the tray menu shows it) and the tray icon is running. |
| "unstuck.ps1 not found" | Run `install.bat` again; the installed script is missing from `C:\Apps\Unstuck\`. |
| "MindAttic Vault isn't installed" | Edit `%LOCALAPPDATA%\MindAttic\UnstuckTaskbar\settings.json` instead (see [Tray app](tray-app.md)). |
| The installer says the tray app is running in another session | Another signed-in user is running it and it locks the exe. Exit it there, then run `install.bat` again. |

## "Running scripts is disabled on this system"

Use `unstuck.bat`. It starts PowerShell with `-ExecutionPolicy Bypass` for this one script and doesn't change your
system policy.
