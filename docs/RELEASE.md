---
layer: release
status: living
---
# Unstuck — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
A Windows recovery tool for a desktop that stops responding to clicks: `unstuck.ps1` releases stuck input, kills
hung or runaway processes and restarts a hung Explorer, with a tray app and two global hotkeys to trigger it.

## 2. The milestone: Unstuck v3, on R: {#milestone}
- **Milestone:** MS-75, "Unstuck v3 released, deployed only from R:"
- **Published means:** `Unstuck-Setup-v3.exe` on GitHub Releases (published 2026-11-19), and the owner's own
  copy at `R:\MindAttic\Apps\Unstuck\{Debug,Release}`.
- **The natural stopping point:** v3 is released and does its job. The only loose end is the R: cutover: code
  and docs still name `C:\Apps\Unstuck`, and the old install folder still exists. Once those are gone, Unstuck
  is done.

## 3. Done when (acceptance checks)
1. build — the UnstuckTaskbar solution builds clean on main.
2. exists — `R:\MindAttic\Apps\Unstuck\Release\UnstuckTaskbar.exe`, and `C:\Apps\Unstuck` does not exist.
3. signoff — the owner presses Ctrl+Alt+U and the recovery report appears.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | UNS-T40 R: cutover | No C: path or folder left | UNS-T41 Repoint C: paths (990), UNS-T42 Remove the C: install (980) |

One epic: v3 is shipped; the cutover is the only remaining system.

## 5. Out of scope (not before this milestone)
- New recovery steps, new overlay/hook apps, code signing, a v4 release.
- Generic sweeps: exhaustive bughunts.

## 6. Current verified state
- v1–v3 on GitHub Releases (latest v3, 2026-11-19); Inno Setup per-user installer (ee55680, UNS-T36).
- Ctrl+Alt+Break and Ctrl+Alt+U hotkeys (c3727f3, UNS-T35); tray hotkey capture (3c4b943, UNS-T34).
- Shared run menu; Debug and Release exes on R: (1322fbb, MAT-T303).

## 7. Decision record
- 2026-11-27 — charter drafted from README/docs/code/tickets; Legion vote: Approve, unanimous among voters that answered (plurality quorum reached), no dissent (9 of 9).
- Triage: 3 open → done 0, duplicate 0, wont-do 1, kept 2 (UNS-T38 reworded: run menu landed in 1322fbb, C: paths remain; UNS-T39), new 0.
