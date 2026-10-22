---
codex: 1
project: Unstuck
code: UNS
layer: stories
status: living
updated: 2026-10-16
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# Unstuck — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| UNS-T1 | 2026-10-16 | docs | closed | Start the permanent ticket log in the workflow database | — |
| UNS-T2 | 2026-10-16 | feature | closed | Canonicalize unstuck.ps1 + the /unstuck command in this repo (global ~/.claude copies become forwarders) and add UnstuckTaskbar, a tray app with an assignable global hotkey (incl. media keys) that fires it | dotnet test: 26/26 passed; deploy.ps1 verified (C:\Apps\Unstuck\UnstuckTaskbar.exe built, deploy.bat/unstuck.ps1 unaffected); tray app launched live, single-instance mutex guard confirmed, logon scheduled task + Start Menu shortcut self-registered |
| UNS-T3 | 2026-10-16 | chore | closed | Add deploy.ps1/deploy.bat packaging to C:\Apps\Unstuck and a desktop shortcut | user request |
| UNS-T4 | 2026-10-16 | chore | closed | Add assets, commands, unstuck.ps1 | — |
| UNS-T5 | 2026-10-18 | feature | closed | Tray hotkey runs recovery off the UI thread, requires a modifier unless the key has no default system meaning, and warns in the capture dialog instead of silently refusing | — |
| UNS-T6 | 2026-10-18 | fix | closed | deploy.ps1 restarts UnstuckTaskbar after a redeploy and exits non-zero when publish fails | — |
| UNS-T7 | 2026-10-18 | chore | closed | Add project AGENTS.md pointing to the shared mindattic-agent-standard protocol | — |
| UNS-T8 | 2026-10-18 | fix | closed | Remove a duplicate UNS-T7 row accidentally hand-appended to the generated USER_STORIES.md | — |
| UNS-T9 | 2026-10-18 | chore | open | Stop registering the UnstuckTaskbar Logon task in deploy/setup (job now in MindAttic.WatchDog M.WD-T9) | scheduled-task consolidation 2026-10-18 |
| UNS-T10 | 2026-10-19 | chore | wont-do | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | — |
| UNS-T11 | 2026-10-20 | docs | open | Publish Unstuck docs to GitHub Pages via the shared Toolkit DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/Unstuck/ returns 200. Depends on MAT-T8. Also write the missing README.md first, since it becomes the site's home page. | — |
| UNS-T12 | 2026-10-20 | chore | closed | Move to MindAttic.Components.Wpf 19.0.0 (Mc->Ma rename): bump the PackageReference and confirm the build; a scan found no Mc-prefixed style/class usages of its own to rename | c19e3d0 |
| UNS-T13 | 2026-10-20 | chore | open | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | user request 2026-10-20: approved repo sweep gap-fill |
| UNS-T14 | 2026-10-21 | docs | closed | Update USER_STORIES.md | — |
| UNS-T15 | 2026-10-22 | docs | closed | Update USER_STORIES.md | — |
