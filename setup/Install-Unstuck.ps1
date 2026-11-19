<#
.SYNOPSIS
    One-click installer for Unstuck: downloads the latest release and sets it up. No .NET, no build, no admin.

.DESCRIPTION
    1. Downloads Unstuck.zip from the latest GitHub release (or uses -ZipPath) and unpacks it into C:\Apps\Unstuck:
       unstuck.ps1 + unstuck.bat (the recovery) and UnstuckTaskbar.exe (the tray app, self-contained).
    2. Adds an "Unstuck" desktop shortcut (runs the recovery) and a Start menu folder.
    3. Starts the tray app at sign-in through a Startup-folder shortcut (skipped when MindAttic.Scheduler already
       runs it), and starts it now. Its default hotkey is Ctrl+Alt+Break.

    Run it again to update. -Uninstall removes everything it added.

    One-liner (PowerShell):
        irm https://raw.githubusercontent.com/mindattic/Unstuck/main/setup/Install-Unstuck.ps1 | iex

.PARAMETER Uninstall
    Remove Unstuck: stop the tray app, delete C:\Apps\Unstuck and every shortcut this installer made.

.PARAMETER ZipPath
    Install from a local Unstuck.zip instead of downloading the latest release.

.PARAMETER NoTray
    Install only the recovery script and its shortcut; no tray app, no sign-in start.
#>
[CmdletBinding()]
param(
    [switch]$Uninstall,
    [string]$ZipPath,
    [switch]$NoTray
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest is many times slower with the progress bar on.

$Repo       = 'mindattic/Unstuck'
$InstallDir = 'C:\Apps\Unstuck'
$Exe        = Join-Path $InstallDir 'UnstuckTaskbar.exe'
$Desktop    = [Environment]::GetFolderPath('Desktop')
$Programs   = Join-Path ([Environment]::GetFolderPath('Programs')) 'Unstuck'
$StartupLnk = Join-Path ([Environment]::GetFolderPath('Startup')) 'Unstuck Tray.lnk'
$DesktopLnk = Join-Path $Desktop 'Unstuck.lnk'

function Say([string]$text, [string]$color = 'Gray') { Write-Host "  $text" -ForegroundColor $color }

function New-Shortcut([string]$path, [string]$target, [string]$description, [string]$arguments = '') {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($path)
    $link.TargetPath = $target
    $link.Arguments = $arguments
    $link.WorkingDirectory = $InstallDir
    $link.Description = $description
    $icon = Join-Path $InstallDir 'logo.ico'
    if (Test-Path $icon) { $link.IconLocation = "$icon,0" }
    $link.Save()
}

function Stop-Tray {
    # Only this desktop session's copy; another signed-in user's tray app is theirs.
    $mine = (Get-Process -Id $PID).SessionId
    Get-Process UnstuckTaskbar -ErrorAction SilentlyContinue | Where-Object SessionId -eq $mine | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

Write-Host ''
Write-Host '  Unstuck' -ForegroundColor Cyan

if ($Uninstall) {
    Stop-Tray
    foreach ($p in $StartupLnk, $DesktopLnk, (Join-Path ([Environment]::GetFolderPath('Programs')) 'MindAttic\UnstuckTaskbar.lnk')) {
        if (Test-Path $p) { Remove-Item $p -Force }
    }
    if (Test-Path $Programs) { Remove-Item $Programs -Recurse -Force }
    if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }
    Say 'Uninstalled. (Settings and reports in %LOCALAPPDATA%\MindAttic were kept.)' Green
    return
}

# ---- Get the release ----
$work = Join-Path ([IO.Path]::GetTempPath()) ("unstuck-setup-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    if (-not $ZipPath) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $ZipPath = Join-Path $work 'Unstuck.zip'
        $url = "https://github.com/$Repo/releases/latest/download/Unstuck.zip"
        Say "Downloading $url"
        Invoke-WebRequest -Uri $url -OutFile $ZipPath -UseBasicParsing
    }
    $unpacked = Join-Path $work 'files'
    Expand-Archive -LiteralPath $ZipPath -DestinationPath $unpacked -Force
    if (-not (Test-Path (Join-Path $unpacked 'unstuck.ps1'))) { throw "$ZipPath doesn't look like an Unstuck release (no unstuck.ps1 inside)." }

    # ---- Install ----
    Stop-Tray   # the running exe is locked
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    Get-ChildItem -LiteralPath $unpacked -File | Where-Object { -not $NoTray -or $_.Name -ne 'UnstuckTaskbar.exe' } |
        Copy-Item -Destination $InstallDir -Force
    Get-ChildItem -LiteralPath $InstallDir -File | Unblock-File
    Say "Installed to $InstallDir" Green
} finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}

# ---- Shortcuts ----
$bat = Join-Path $InstallDir 'unstuck.bat'
New-Shortcut $DesktopLnk $bat 'Unstuck - recover a desktop that stopped responding to clicks'
New-Shortcut (Join-Path $Programs 'Unstuck.lnk') $bat 'Unstuck - recover a desktop that stopped responding to clicks'
New-Shortcut (Join-Path $Programs 'Unstuck (check only).lnk') $bat 'Unstuck - report only, change nothing' 'check'
New-Shortcut (Join-Path $Programs 'Uninstall Unstuck.lnk') 'powershell.exe' 'Remove Unstuck' `
    "-NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $InstallDir 'Install-Unstuck.ps1')`" -Uninstall"
Say 'Added the Unstuck desktop shortcut and Start menu folder.' Green

# ---- Tray app ----
if (-not $NoTray -and (Test-Path $Exe)) {
    $scheduled = Get-ScheduledTask -TaskPath '\MindAttic\Scheduler\' -TaskName 'UnstuckTaskbar' -ErrorAction SilentlyContinue
    if ($scheduled) {
        Say 'MindAttic.Scheduler already starts the tray app at sign-in.'
    } else {
        New-Shortcut $StartupLnk $Exe 'Unstuck tray icon and hotkey'
        Say 'The tray app will start when you sign in.' Green
    }
    Start-Process -FilePath $Exe -WorkingDirectory $InstallDir
    Say 'Tray app started. Press Ctrl+Alt+Break any time to unstick the desktop.' Green
}

Write-Host ''
Say 'Done. Clicks dead? Press Ctrl+Alt+Break, or double-click "Unstuck" on the desktop.' Cyan
Write-Host ''
