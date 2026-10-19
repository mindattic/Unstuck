---
codex: 1
project: Unstuck
code: UNS
layer: stories
status: living
updated: 2026-10-16
---
<!-- GENERATED from MindAttic.Workflow/data/workflow.db by `workflow db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the workflow CLI. -->

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
