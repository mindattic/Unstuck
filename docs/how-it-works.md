# How it works

`unstuck.ps1` runs five steps, in this order, and then prints a report. With `check`, it decides everything the
same way but changes nothing, and each action in the report starts with *WOULD*.

## 1. Stuck input

Most "clicks do nothing, keyboard works" cases come from input that Windows thinks is still in progress.

- **Mouse capture.** A window that started a drag and never received the button-up keeps *capture*, so every
  click goes to it. Unstuck sends that window `WM_CANCELMODE`, the documented way to cancel it, and releases
  capture.
- **Mouse buttons.** It sends a release for the left, right and middle buttons. Releases are harmless. If Windows
  still reports the left button down, it sends a few down/up cycles for that button only. It never clicks the
  right button, because three blind right-clicks once pasted the clipboard into a terminal three times.
- **Modifier keys.** Windows tracks left and right Shift, Ctrl, Alt and Win separately, so it releases each side
  with its own scan code and extended-key flag. A generic "Shift up" can leave the real key down. Before that it
  sends an unassigned dummy key, so releasing Win or Alt on its own can't open Start or a menu bar.
- **Any other held key** that Windows reports gets a release too.
- **Sticky Keys.** A latched or locked modifier isn't cleared by a key-up, so Unstuck turns Sticky Keys off and
  back on if it's enabled.
- **Hover state.** It moves the cursor one pixel and back, so the pointer shape updates (for example, the
  text cursor over a text box).

### Gamepad buttons

Controllers report buttons as virtual keys too (VK `0xC3` to `0xDA`). A trigger whose release never arrived can
make Windows behave oddly; on 2026-10-16, Shift brought up a blue 1-2-3 overlay. Injected input can't release a
gamepad key. Only the controller can. When this happens the report says **STUCK GAMEPAD INPUT**, names the
button and the connected controllers, and tells you to press and release it or switch the controller off.

## 2. Runaway processes

It takes a 2-second snapshot of every process, then picks:

| Condition | Why |
|---|---|
| A window *Not Responding* at both the start and the end of the 2 seconds | A brief hiccup recovers within that time; a hung app doesn't. |
| One process using **over 40% of RAM** | Memory pressure makes everything, input included, page and stall. |
| The biggest process when **less than 5% of RAM is free** | The same pressure when no single process is the obvious cause. |
| One process using **over 70% of the whole CPU** across the sample | A pegged machine drops and delays input. |

### Never touched

- Windows itself: `csrss`, `dwm`, `winlogon`, `lsass`, `svchost`, Defender, the Start menu and search hosts,
  `LogonUI`, UAC (`consent`), and the rest of the system list.
- Explorer, which is only ever restarted, never just killed.
- Terminals, shells and editors: Windows Terminal, conhost, PowerShell, cmd, bash, VS Code, Visual Studio, `node`
  and Claude.
- Task Manager and the Unstuck tray app.
- **Everything that launched this run**, all the way up the parent chain, so the app hosting your terminal
  survives even if it's the biggest process.
- Anything in another user's session or in session 0 (services), even when run as admin.

## 3. Explorer

Explorer draws the taskbar, Start and the desktop. If it's hung or isn't running, Unstuck restarts it, and the
`explorer` option restarts it in any case. If Windows doesn't bring it back by itself within a second, Unstuck
starts it.

## 4. Global mouse hooks

Apps that hook the mouse system-wide can swallow every click if they stall.

- **DisplayFusion**: its desktop app is always restarted from the same path when it's running. Exiting it has
  fixed dead clicks before. Its Windows service can't hook the desktop and needs admin to stop, so it's left alone.
- **PowerToys** (with `hooks`): FancyZones, Always On Top and the mouse utilities all hook the mouse. Unstuck
  stops every PowerToys process and relaunches only the main runner, which starts its own modules again.

## 5. Overlays (with `overlays`)

Game and chat overlays can sit invisibly on top of everything. Unstuck closes Overwolf, the Steam overlay
(`GameOverlayUI`), NVIDIA Overlay, Xbox Game Bar and Discord's hook helper. If you don't use `overlays`, the report
still lists any of them that are running, so you know whether to try it.

## The report

```text
RAM: 11840 MB free of 32581 MB
Top memory:
  ...
Top CPU (2 s sample):
  ...
Keys held before: Left Shift
Keys still held:  none
Actions:
  release stuck mouse capture/buttons and modifier keys
  restart DisplayFusion (its mouse hook can swallow every click)
No runaway process found.
```

It's printed, and saved to `%LOCALAPPDATA%\MindAttic\Unstuck\last-run.txt`. Each run is also appended to
`history.log` in the same folder; when that file grows past 1 MB, the older half is dropped. Use `nolog` to skip
saving.

Exit codes: `0` when the run finished (whatever it found), `2` for an unknown option.
