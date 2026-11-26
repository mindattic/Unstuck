#Requires -Version 5.1
<#
.SYNOPSIS
    Unstuck's run menu: 1) Redeploy  2) Run Debug  3) Run Release  (the MindAttic standard, MAT-T279).

.DESCRIPTION
    The installer, not the recovery: unstuck.ps1 is the recovery itself, and unstuck.bat runs it with no install.
    run.bat beside this file (Write-RunBat) opens the same menu on a double-click; install.bat runs 1) Redeploy.
    Everything else lives in MindAttic.Agency\shared\deploy-lib.ps1's Invoke-AppRun:
      1) Redeploy     build UnstuckTaskbar and deploy Debug and Release to R:\MindAttic\Apps\Unstuck\{Debug,Release},
                      each with the payload beside the exe: unstuck.ps1, unstuck.bat, logo.ico and commands\
      2) Run Debug    the dev/test tray app, in this console
      3) Run Release  the standalone tray app, started the way a double-click would start it
    Picks combine: 13 is Deploy & Run (redeploy, then run the installed Release copy).

    A redeploy stops the tray app running from the folder it replaces. A Release tray app that was running in this
    desktop session is started again afterwards, so the hotkey isn't dead until the next logon; Release also runs
    UnstuckTaskbar.exe --write-settings-manifest, so MindAttic Vault shows this app's tab.

    Finding deploy-lib.ps1, first match wins:
      1. $env:MINDATTIC_DEPLOY_LIB, the full path of a deploy-lib.ps1 (a dev copy, or a workspace elsewhere)
      2. walking up from this folder, at each ancestor <dir>:
           <dir>\MindAttic.Agency\shared\deploy-lib.ps1           (the canonical copy, current with main)
           <dir>\mindattic-agent-standard\shared\deploy-lib.ps1   (the workspace-root mirror; lags until
                                                                    sync-workspace-copy.ps1 runs)
      3. the same two under D:\Projects\MindAttic (repos in another workspace root, e.g. D:\Projects\GunGreenEyes)

.EXAMPLE
    .\run.ps1                    the menu
    .\run.ps1 -Redeploy          build and deploy both configs (install.bat)
    .\run.ps1 -RunRelease        Run: the installed tray app, no build
    .\run.ps1 -Redeploy -RunRelease             Deploy & Run
    .\run.ps1 -Redeploy -DryRun  print what would be built and launched, do nothing
#>
param(
    [switch]$Redeploy,
    [switch]$RunDebug,
    [switch]$RunRelease,
    [switch]$DryRun,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$AppArgs = @()
)
$ErrorActionPreference = 'Stop'

$trayState = @{ Restart = $false }
$Override = @{
    Exe       = 'UnstuckTaskbar.exe'
    Plan      = @{ Kind = 'dotnet'; Projects = @("$PSScriptRoot\UnstuckTaskbar\UnstuckTaskbar.csproj") }
    PreBuild  = {
        param($Config, $Out)
        if ($Config -ne 'Release') { return }
        $target = $Out -replace '\.staging$', ''
        $mySession = (Get-Process -Id $PID).SessionId
        $trayState.Restart = [bool](Get-Process 'UnstuckTaskbar' -ErrorAction SilentlyContinue |
            Where-Object { $_.SessionId -eq $mySession -and $_.Path -and $_.Path.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase) })
    }
    PostBuild = {
        param($Config, $Out)
        Copy-Item -LiteralPath "$PSScriptRoot\unstuck.ps1", "$PSScriptRoot\unstuck.bat", "$PSScriptRoot\assets\logo.ico" -Destination $Out -Force
        Copy-Item -LiteralPath "$PSScriptRoot\commands" -Destination $Out -Recurse -Force
        $exe = Join-Path $Out 'UnstuckTaskbar.exe'
        if ($Config -ne 'Release' -or -not (Test-Path -LiteralPath $exe)) { return }
        # Seeds first-run defaults and writes settings.manifest.json so MindAttic Vault shows this app's tab.
        & $exe --write-settings-manifest | Out-Null
        if ($trayState.Restart) { Start-Process -FilePath $exe -WorkingDirectory $Out }
    }
}

function Find-DeployLib {
    if ($env:MINDATTIC_DEPLOY_LIB -and (Test-Path -LiteralPath $env:MINDATTIC_DEPLOY_LIB)) { return $env:MINDATTIC_DEPLOY_LIB }
    $roots = @()
    $d = $PSScriptRoot
    while ($d) { $roots += $d; $d = Split-Path -Parent $d }
    $roots += 'D:\Projects\MindAttic'
    foreach ($r in $roots) {
        foreach ($rel in 'MindAttic.Agency\shared\deploy-lib.ps1', 'mindattic-agent-standard\shared\deploy-lib.ps1') {
            $p = Join-Path $r $rel
            if (Test-Path -LiteralPath $p) { return $p }
        }
    }
    throw 'deploy-lib.ps1 not found: clone MindAttic.Agency beside this repo, or set MINDATTIC_DEPLOY_LIB to its path.'
}

. (Find-DeployLib)
Invoke-AppRun -Repo $PSScriptRoot -Redeploy:$Redeploy -RunDebug:$RunDebug -RunRelease:$RunRelease -DryRun:$DryRun `
    -AppArgs $AppArgs -Override $Override
