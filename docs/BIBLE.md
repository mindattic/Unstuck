---
layer: bible
status: living
---
# Unstuck — Project Bible

> Single source of truth for what Unstuck IS, is NOT, and the rules that keep it coherent.
> README says how to install and run it; this says how to think about it. The finish line lives in
> `docs/RELEASE.md`.

## 1. The one sentence {#UNS-§1}
Get a Windows desktop working again when clicks stop doing anything, in about three seconds, without a restart.

## 2. The product promise {#UNS-§2}
- One keystroke from any app (Ctrl+Alt+Break or Ctrl+Alt+U) runs the recovery.
- It releases stuck input, kills hung or runaway apps, restarts a hung Explorer, and restarts DisplayFusion.
- It says what it did and what it could not fix, and keeps the report.
- No admin rights and no .NET needed to install.

## 3. What it is NOT {#UNS-§3}
- Not a system cleaner or optimizer.
- Not allowed to touch Windows system processes, terminals, shells, editors, Claude, Task Manager or its own
  launcher; Explorer is only ever restarted.
- Not acting outside the user's own desktop session.

## 4. Architecture canon {#UNS-§4}
```
 hotkey / tray menu ─► UnstuckTaskbar.exe (tray app) ─► unstuck.ps1 (the recovery)
 unstuck.bat ─────────────────────────────────────────►      │
 /unstuck command ────────────────────────────────────►      ▼
                                       %LOCALAPPDATA%\MindAttic\Unstuck\last-run.txt, history.log
```
### 4.1 Projects and files
- `unstuck.ps1` — the recovery itself; `unstuck.bat` — double-clickable wrapper.
- `UnstuckTaskbar` — tray app (WPF + WinForms tray icon) with global hotkeys; `UnstuckTaskbar.Tests` — NUnit tests.
- `setup/Unstuck.iss` — Inno Setup per-user installer (`Unstuck-Setup-v<N>.exe`).
- `run.ps1` — shared run menu deploying to `R:\MindAttic\Apps\Unstuck`.
### 4.2 The nouns
Stuck input, hung app, runaway process, shell, mouse hook, report.
### 4.3 The verbs
Release, kill, restart, report; `check` changes nothing.

## 5. The Laws {#UNS-§5}
Inherits `MindAttic.HouseRules.md` by reference.
1. Never kill a protected process (list in §3). {#UNS-LAW-1}
2. Never click the right button; click left only when it is still reported down after release. {#UNS-LAW-2}
3. "Not responding" is re-checked after 2 seconds before a kill. {#UNS-LAW-3}
4. `check` mode reports and changes nothing. {#UNS-LAW-4}
5. Releases are whole numbers: tag `v<N>`. {#UNS-LAW-5}

## 6. Verified state {#UNS-§6}
v1–v3 on GitHub Releases (latest v3, 2026-11-19); both hotkeys; installer; deployed on R: (1322fbb). Some code
and docs still name `C:\Apps\Unstuck`.

## 7. Active frontier {#UNS-§7}
`docs/RELEASE.md` — milestone MS-75 "Unstuck v3 on R:".

## 8. Quality bar {#UNS-§8}
A new recovery step must honour the protected list, show up in the report and in `check` mode, and be described
in `docs/how-it-works.md`.

## 9. Glossary {#UNS-§9}
- **Stuck input** — a key or button whose release never arrived, or a window holding mouse capture.
- **Runaway** — over 40% of RAM (or the biggest when under 5% free), or over 70% CPU.
