---
description: Recover a Windows desktop that stopped responding to clicks - release stuck input, kill hung/runaway processes, restart a hung Explorer.
argument-hint: "[check | hooks | overlays | explorer]"
allowed-tools: PowerShell, Bash
---

The user's desktop has stopped responding to clicks (the keyboard still reaches this terminal).
Act immediately - no questions, no preamble.

Run the recovery script with the PowerShell tool, mapping the argument (`$ARGUMENTS`) to switches:

- no argument: `& "D:\Projects\MindAttic\Unstuck\unstuck.ps1"`
- `check`: add `-Check` (report only, change nothing)
- `hooks`: add `-Hooks` (also restart PowerToys, whose FancyZones / Always On Top / mouse utilities hook the mouse)
- `overlays`: add `-Overlays` (also close overlay apps that can swallow clicks: Overwolf, Steam/NVIDIA/Xbox Game Bar overlays, Discord hook)
- `explorer`: add `-Explorer` (force-restart Explorer)

Arguments can be combined, e.g. `/unstuck overlays explorer`.

What the script does: cancels any window's stuck mouse capture, releases stuck mouse buttons and
keys (each Shift/Ctrl/Alt/Win side separately, any other key Windows reports held, and Sticky Keys
latches), lists the keys held before and after, releases all mouse buttons, sends click cycles only for a left button Windows
still reports stuck down (never a blind click, and never the right button: three blind right-clicks once pasted the
clipboard into the terminal prompt), and nudges the cursor so hover state (the i-beam) recomputes; kills windowed apps still
"Not Responding" after a 2-second re-check, any process over 40% of RAM (or the biggest when free
RAM is under 10%), and any process over 70% CPU; restarts DisplayFusion's desktop app if it's
running (its global mouse hook is a known cause of every click dying while the keyboard works -
the user has seen exiting it fix this); restarts Explorer if it's hung or missing. It
never kills system processes, Explorer (except restarting it), Claude, terminals or shells.

Then report in a few lines: what was killed or restarted, the keys still held, and the RAM / top-process numbers.
If it prints STUCK GAMEPAD INPUT, lead with that: a gamepad button whose release never arrived (seen 2026-10-16:
the left trigger, which made Shift bring up a blue 1-2-3 overlay) can't be cleared by injected input - tell the user
to press and release that button on the controller, or switch the controller off. If a keyboard key is still held
after the release, tell them to press and release that key. If nothing was found and clicks are still dead, suggest
the next step in this order: `/unstuck hooks` (PowerToys), then
`/unstuck overlays` (if the script listed running overlay apps), then `/unstuck explorer`, then Ctrl+Shift+Esc (Task
Manager) or Ctrl+Alt+Del to break a full-screen app's input capture.
