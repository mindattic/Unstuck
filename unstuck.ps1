# /unstuck - recover a Windows desktop that has stopped responding to clicks.
#
# 1. Releases stuck input: cancels any window's mouse capture, releases held mouse buttons and
#    Shift/Ctrl/Alt/Win keys (each side separately, plus any other key Windows reports held, and Sticky Keys
#    latches), reports which keys were held - including a gamepad button whose release never arrived, which only
#    the controller can clear - clicks only a left button still reported stuck down (never the right: a right-click pastes in a terminal), and nudges the cursor so hover state
#    (e.g. the i-beam over a text field) gets recomputed.
# 2. Kills what has gone off the rails: windowed apps still "Not Responding" after a re-check,
#    a runaway memory hog, a runaway CPU hog. System processes, Explorer, Claude, terminals and
#    shells are never killed.
# 3. Restarts Explorer if it's hung (or always, with -Explorer).
# 4. Restarts DisplayFusion's desktop app: its system-wide mouse hook is a known way for every
#    click to die while the keyboard still works. (Its Windows service can't hook the desktop and
#    needs admin to stop, so it's left alone.) With -Hooks, PowerToys - FancyZones, Always On Top
#    and the mouse utilities all hook the mouse - is restarted too.
# 5. With -Overlays, also closes overlay apps that can sit invisibly on top and swallow clicks.
#
# -Check reports only and changes nothing.
param(
    [switch]$Check,
    [switch]$Overlays,
    [switch]$Explorer,
    [switch]$Hooks
)

$ErrorActionPreference = 'Continue'
$actions = New-Object System.Collections.Generic.List[string]

# Never touched: Windows itself, the shell, and whatever is running this command.
$protected = @(
    'System', 'Idle', 'Registry', 'smss', 'csrss', 'wininit', 'winlogon', 'services', 'lsass',
    'svchost', 'dwm', 'fontdrvhost', 'Memory Compression', 'MsMpEng', 'NisSrv', 'SecurityHealthService',
    'audiodg', 'sihost', 'ctfmon', 'spoolsv', 'taskhostw', 'RuntimeBroker', 'StartMenuExperienceHost',
    'ShellExperienceHost', 'SearchHost', 'TextInputHost', 'ApplicationFrameHost', 'SystemSettings',
    'LockApp', 'explorer', 'claude', 'node', 'WindowsTerminal', 'OpenConsole', 'conhost',
    'powershell', 'pwsh', 'bash', 'cmd', 'Code', 'devenv', 'MsMpEngCP', 'WmiPrvSE', 'dllhost'
)
$overlayApps = @('Overwolf', 'OverwolfBrowser', 'GameOverlayUI', 'NVIDIA Overlay', 'GameBar', 'GameBarFTServer', 'DiscordHookHelper64', 'DiscordHookHelper')

function Act([string]$what, [scriptblock]$do) {
    if ($Check) { $actions.Add("WOULD $what"); return }
    try { & $do; $actions.Add($what) } catch { $actions.Add("FAILED to $what - $($_.Exception.Message)") }
}

# ---- 1. Stuck input ----
Add-Type -Namespace Unstuck -Name Input -MemberDefinition @'
[DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, System.UIntPtr extra);
[DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, System.UIntPtr extra);
[DllImport("user32.dll")] public static extern bool ReleaseCapture();
[DllImport("user32.dll")] public static extern IntPtr GetCapture();
[DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
[DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);
[DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint type);
[DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint action, uint param, ref STICKYKEYS info, uint winIni);
[StructLayout(LayoutKind.Sequential)] public struct STICKYKEYS { public uint cbSize; public uint dwFlags; }
'@

# Every key Windows thinks is held right now, by name. A key whose release never arrived shows up here: on
# 2026-10-16 it was the gamepad's left trigger (VK 0xC9), which made Shift bring up a blue 1-2-3 overlay.
$keyNames = @{
    0x10 = 'Shift'; 0x11 = 'Ctrl'; 0x12 = 'Alt'; 0x5B = 'Left Win'; 0x5C = 'Right Win'; 0x5D = 'Menu key'
    0xA0 = 'Left Shift'; 0xA1 = 'Right Shift'; 0xA2 = 'Left Ctrl'; 0xA3 = 'Right Ctrl'; 0xA4 = 'Left Alt'; 0xA5 = 'Right Alt'
    0x08 = 'Backspace'; 0x09 = 'Tab'; 0x0D = 'Enter'; 0x1B = 'Esc'; 0x20 = 'Space'
    0xC3 = 'gamepad A'; 0xC4 = 'gamepad B'; 0xC5 = 'gamepad X'; 0xC6 = 'gamepad Y'
    0xC7 = 'gamepad right bumper (RB/R1)'; 0xC8 = 'gamepad left bumper (LB/L1)'
    0xC9 = 'gamepad left trigger (LT/L2)'; 0xCA = 'gamepad right trigger (RT/R2)'
    0xCB = 'gamepad d-pad up'; 0xCC = 'gamepad d-pad down'; 0xCD = 'gamepad d-pad left'; 0xCE = 'gamepad d-pad right'
    0xCF = 'gamepad Menu/Start'; 0xD0 = 'gamepad View/Back'
    0xD1 = 'gamepad left stick press'; 0xD2 = 'gamepad right stick press'
    0xD3 = 'gamepad left stick up'; 0xD4 = 'gamepad left stick down'; 0xD5 = 'gamepad left stick right'; 0xD6 = 'gamepad left stick left'
    0xD7 = 'gamepad right stick up'; 0xD8 = 'gamepad right stick down'; 0xD9 = 'gamepad right stick right'; 0xDA = 'gamepad right stick left'
}
function HeldKeys {
    # Mouse buttons (1-6) are handled on their own; toggle keys (Caps/Num/Scroll Lock) aren't "held".
    foreach ($vk in 7..254) {
        if ($vk -in 0x14, 0x90, 0x91) { continue }
        if ([Unstuck.Input]::GetAsyncKeyState($vk) -band 0x8000) { [pscustomobject]@{ Vk = $vk; Name = $(if ($keyNames[$vk]) { $keyNames[$vk] } elseif (($vk -ge 0x30 -and $vk -le 0x39) -or ($vk -ge 0x41 -and $vk -le 0x5A)) { [string][char]$vk } else { 'key 0x{0:X2}' -f $vk }) } }
    }
}
$heldBefore = @(HeldKeys)
Act 'release stuck mouse capture/buttons and modifier keys' {
    # If some window is still holding mouse capture - e.g. a drag that never got its button-up -
    # tell it to cancel its current mode, the documented way to break this loose.
    $cap = [Unstuck.Input]::GetCapture()
    if ($cap -ne [IntPtr]::Zero) { [Unstuck.Input]::SendMessage($cap, 0x001F, [IntPtr]::Zero, [IntPtr]::Zero) } # WM_CANCELMODE
    [void][Unstuck.Input]::ReleaseCapture()

    # Releases are harmless, so all three buttons always get one. A press is not: it clicks whatever is under the cursor,
    # and a right-click in a terminal pastes the clipboard (2026-10-16: three blind right-clicks typed the clipboard into
    # the prompt three times). So a down+up cycle - what mashing the buttons by hand did when a release alone wasn't
    # enough (2026-10-15) - is sent only for the left button, and only while Windows reports it stuck down. Never right.
    foreach ($flag in 0x0004, 0x0010, 0x0040) { [Unstuck.Input]::mouse_event($flag, 0, 0, 0, [UIntPtr]::Zero) } # left/right/middle up
    Start-Sleep -Milliseconds 50
    if ([Unstuck.Input]::GetAsyncKeyState(0x01) -band 0x8000) {
        for ($i = 0; $i -lt 3; $i++) {
            [Unstuck.Input]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 15
            [Unstuck.Input]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        }
        $actions.Add('the left button was still reported down after its release: sent down/up cycles to clear it')
    }
    [void][Unstuck.Input]::ReleaseCapture()

    # Nudge the cursor a pixel and back so Windows recomputes hover state - without this the
    # pointer can keep showing the wrong icon (no i-beam over text) even after input is unstuck.
    [Unstuck.Input]::mouse_event(0x0001, 1, 0, 0, [UIntPtr]::Zero)
    [Unstuck.Input]::mouse_event(0x0001, -1, 0, 0, [UIntPtr]::Zero)

    # Keys. Windows tracks left and right modifiers separately, so each side is released on its own, with its scan code
    # and the extended flag the right-hand and Win keys carry (a generic VK_SHIFT up with no scan code can leave the real
    # key down). A dummy key (0xE8, unassigned) goes first so a lone Win or Alt release can't open Start or a menu bar.
    [Unstuck.Input]::keybd_event(0xE8, 0, 0, [UIntPtr]::Zero); [Unstuck.Input]::keybd_event(0xE8, 0, 0x0002, [UIntPtr]::Zero)
    $extended = @(0xA3, 0xA5, 0x5B, 0x5C, 0x5D)
    foreach ($vk in 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C, 0x5D, 0x10, 0x11, 0x12) {
        $flags = 0x0002 -bor $(if ($extended -contains $vk) { 0x0001 } else { 0 })
        [Unstuck.Input]::keybd_event([byte]$vk, [byte][Unstuck.Input]::MapVirtualKey($vk, 0), [uint32]$flags, [UIntPtr]::Zero)
    }
    # Any other key reported held (not a gamepad one, which injected input can't touch) gets its release too.
    foreach ($k in $heldBefore | Where-Object { $_.Vk -lt 0xC3 -or $_.Vk -gt 0xDA }) {
        [Unstuck.Input]::keybd_event([byte]$k.Vk, [byte][Unstuck.Input]::MapVirtualKey($k.Vk, 0), 0x0002, [UIntPtr]::Zero)
    }

    # Sticky Keys latches or locks a modifier on its own (a key-up doesn't clear that); switching it off and back on does.
    $sk = New-Object Unstuck.Input+STICKYKEYS; $sk.cbSize = 8
    if ([Unstuck.Input]::SystemParametersInfo(0x003A, 8, [ref]$sk, 0) -and ($sk.dwFlags -band 1)) {
        $on = $sk.dwFlags
        $sk.dwFlags = $on -band (-bnot 1); [void][Unstuck.Input]::SystemParametersInfo(0x003B, 8, [ref]$sk, 0)
        $sk.dwFlags = $on; [void][Unstuck.Input]::SystemParametersInfo(0x003B, 8, [ref]$sk, 0)
        $actions.Add('cleared Sticky Keys latched modifiers (turned it off and back on)')
    }
}
$heldAfter = @(if ($Check) { $heldBefore } else { Start-Sleep -Milliseconds 200; HeldKeys })

# ---- Snapshot ----
$os = Get-CimInstance Win32_OperatingSystem
$totalMB = [math]::Round($os.TotalVisibleMemorySize / 1KB)
$freeMB = [math]::Round($os.FreePhysicalMemory / 1KB)
$cores = (Get-CimInstance Win32_ComputerSystem).NumberOfLogicalProcessors

$before = @{}; Get-Process | ForEach-Object { $before[$_.Id] = $_.CPU }
$hungFirst = @(Get-Process | Where-Object { $_.MainWindowHandle -ne 0 -and -not $_.Responding } | ForEach-Object Id)
Start-Sleep -Seconds 2
$procs = Get-Process
$cpuPct = @{}
foreach ($p in $procs) { if ($before.ContainsKey($p.Id) -and $p.CPU) { $cpuPct[$p.Id] = [math]::Round((($p.CPU - $before[$p.Id]) / 2) * 100 / $cores, 1) } }

function IsProtected($p) { $protected -contains $p.Name -or $p.Id -eq $PID -or $p.SessionId -eq 0 }

# ---- 2. Culprits ----
$culprits = @{}
# Still hung two seconds later (a passing hiccup isn't).
foreach ($p in $procs | Where-Object { $_.MainWindowHandle -ne 0 -and -not $_.Responding -and $hungFirst -contains $_.Id }) {
    if (-not (IsProtected $p)) { $culprits[$p.Id] = "$($p.Name) (not responding)" }
}
# Memory: one process over 40% of RAM, or the biggest when free RAM is under 10%.
$byMem = $procs | Where-Object { -not (IsProtected $_) } | Sort-Object WorkingSet64 -Descending
$top = $byMem | Select-Object -First 1
if ($top) {
    $topMB = [math]::Round($top.WorkingSet64 / 1MB)
    if ($topMB -gt $totalMB * 0.4 -or $freeMB -lt $totalMB * 0.1) { $culprits[$top.Id] = "$($top.Name) (using $topMB MB of RAM)" }
}
# CPU: over 70% of the whole machine across the sample.
foreach ($p in $procs) {
    if ($cpuPct.ContainsKey($p.Id) -and $cpuPct[$p.Id] -gt 70 -and -not (IsProtected $p)) { $culprits[$p.Id] = "$($p.Name) (using $($cpuPct[$p.Id])% CPU)" }
}

foreach ($id in $culprits.Keys) { Act "kill $($culprits[$id]) [pid $id]" { Stop-Process -Id $id -Force } }

# ---- 3. Explorer ----
# (Not named $explorer: PowerShell variables are case-insensitive, so it would be the -Explorer switch.)
$shell = Get-Process explorer -ErrorAction SilentlyContinue | Select-Object -First 1
if ($Explorer -or ($shell -and -not $shell.Responding) -or -not $shell) {
    $why = if ($Explorer) { 'as asked' } elseif (-not $shell) { 'it was not running' } else { 'it was not responding' }
    Act "restart Explorer ($why)" {
        Get-Process explorer -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep -Milliseconds 800
        if (-not (Get-Process explorer -ErrorAction SilentlyContinue)) { Start-Process explorer.exe }
    }
}

# ---- 4. Global mouse hooks: kill, then relaunch from the same path ----
function Restart-App([string[]]$names, [string]$label) {
    $running = @(Get-Process | Where-Object { $names -contains $_.Name -and $_.SessionId -ne 0 })
    if ($running.Count -eq 0) { return }
    $exe = ($running | Where-Object { $_.Path } | Select-Object -First 1).Path
    Act "restart $label (its mouse hook can swallow every click)" {
        $running | Stop-Process -Force
        Start-Sleep -Seconds 1
        if ($exe) { Start-Process $exe }
    }
}
Restart-App @('DisplayFusion', 'DisplayFusionHookApp32', 'DisplayFusionHookApp64', 'DisplayFusionCommand') 'DisplayFusion'
if ($Hooks) {
    # Only the PowerToys runner is relaunched; it starts its own modules.
    $pt = @(Get-Process | Where-Object { $_.Name -like 'PowerToys*' -and $_.SessionId -ne 0 })
    if ($pt.Count -gt 0) {
        $runner = ($pt | Where-Object { $_.Name -eq 'PowerToys' -and $_.Path } | Select-Object -First 1).Path
        Act 'restart PowerToys (FancyZones / Always On Top / mouse utilities hook the mouse)' {
            $pt | Stop-Process -Force
            Start-Sleep -Seconds 1
            if ($runner) { Start-Process $runner }
        }
    }
}

# ---- 5. Overlays ----
if ($Overlays) {
    foreach ($p in $procs | Where-Object { $overlayApps -contains $_.Name }) { Act "close overlay $($p.Name) [pid $($p.Id)]" { Stop-Process -Id $p.Id -Force } }
}

# ---- Report ----
"RAM: $freeMB MB free of $totalMB MB"
"Top memory:"
$procs | Sort-Object WorkingSet64 -Descending | Select-Object -First 6 | ForEach-Object { "  {0,-24} {1,6} MB  pid {2}" -f $_.Name, [math]::Round($_.WorkingSet64 / 1MB), $_.Id }
"Top CPU (2 s sample):"
$cpuPct.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 5 | ForEach-Object { $p = $procs | Where-Object Id -eq $_.Key | Select-Object -First 1; "  {0,-24} {1,5}%  pid {2}" -f $p.Name, $_.Value, $_.Key }
$stillHung = $procs | Where-Object { $_.MainWindowHandle -ne 0 -and -not $_.Responding } | ForEach-Object Name
if ($stillHung) { "Not responding: $($stillHung -join ', ')" }
$runningOverlays = $procs | Where-Object { $overlayApps -contains $_.Name } | ForEach-Object Name | Sort-Object -Unique
if ($runningOverlays -and -not $Overlays) { "Overlay apps running (can swallow clicks; '/unstuck overlays' closes them): $($runningOverlays -join ', ')" }
"Keys held before: " + $(if ($heldBefore) { ($heldBefore.Name) -join ', ' } else { 'none' })
"Keys still held:  " + $(if ($heldAfter) { ($heldAfter.Name) -join ', ' } else { 'none' })
$pads = @($heldAfter | Where-Object { $_.Vk -ge 0xC3 -and $_.Vk -le 0xDA })
if ($pads) {
    # Gamepad keys come from the controller's driver, not the keyboard stream: only that controller can release them.
    "STUCK GAMEPAD INPUT: Windows still thinks $(($pads.Name) -join ', ') is held - its release never arrived, and injected"
    "  input can't clear a gamepad key. Press and release it on the controller (try each one that's on), or switch the"
    "  controller off/unplug it. Connected: " + ((Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue |
        Where-Object { $_.Class -in 'XboxComposite', 'HIDClass', 'MEDIA' -and $_.FriendlyName -match 'Xbox|DualSense|DualShock|Wireless Controller|game controller' } |
        ForEach-Object FriendlyName | Sort-Object -Unique) -join ', ')
}
"Actions:"
if ($actions.Count -eq 0) { "  none" } else { $actions | ForEach-Object { "  $_" } }
if ($culprits.Count -eq 0) { "No runaway process found." }
