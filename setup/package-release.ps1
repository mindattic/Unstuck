#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the one-click release (Unstuck.zip + Install-Unstuck.bat) and, with -Publish, puts it on GitHub Releases.

.DESCRIPTION
    The release has to be built on a machine that can restore the MindAttic packages (C:\LocalNuGet: they aren't
    on nuget.org), so this runs locally, not in CI. UnstuckTaskbar.exe is published self-contained and single-file,
    so the people installing it need no .NET runtime.

    Unstuck.zip holds: unstuck.ps1, unstuck.bat, UnstuckTaskbar.exe, logo.ico, Install-Unstuck.ps1 (kept for
    uninstall) and README.txt. Output goes to artifacts\release\.

.PARAMETER Publish
    Create the GitHub release v<Version> (from UnstuckTaskbar.csproj) with both files attached.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File setup\package-release.ps1 -Publish
#>
param([switch]$Publish)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root 'UnstuckTaskbar\UnstuckTaskbar.csproj'
$out = Join-Path $root 'artifacts\release'
$stage = Join-Path $out 'stage'
$version = ([xml](Get-Content $proj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "No <Version> in $proj" }

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Write-Host "  Publishing UnstuckTaskbar $version (self-contained, single file)" -ForegroundColor Cyan
dotnet publish $proj -c Release -r win-x64 --self-contained true `
    /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true `
    /p:DebugType=none -o (Join-Path $out 'publish') | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Copy-Item (Join-Path $out 'publish\UnstuckTaskbar.exe') $stage
Copy-Item (Join-Path $root 'unstuck.ps1'), (Join-Path $root 'unstuck.bat'), (Join-Path $PSScriptRoot 'Install-Unstuck.ps1') $stage
Copy-Item (Join-Path $root 'assets\logo.ico') $stage
@"
Unstuck $version - recover a Windows desktop that stopped responding to clicks.

  unstuck.bat            run the recovery now (double-click). Options: check, explorer, hooks, overlays, all
  UnstuckTaskbar.exe     tray icon; Ctrl+Alt+Break runs the recovery from anywhere
  Install-Unstuck.ps1    installer / uninstaller (-Uninstall)

Docs: https://mindattic.github.io/Unstuck/
"@ | Set-Content (Join-Path $stage 'README.txt') -Encoding ASCII

$zip = Join-Path $out 'Unstuck.zip'
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Copy-Item (Join-Path $PSScriptRoot 'Install-Unstuck.bat') $out
Write-Host "  Built $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)" -ForegroundColor Green

if ($Publish) {
    $tag = "v$version"
    $notes = @"
**One-click install:** download **Install-Unstuck.bat** below and double-click it. It installs into ``C:\Apps\Unstuck`` (no admin, no .NET needed), adds an *Unstuck* desktop shortcut and starts the tray app: press **Ctrl+Alt+Break** whenever clicks stop working.

Or in PowerShell: ``irm https://raw.githubusercontent.com/mindattic/Unstuck/main/setup/Install-Unstuck.ps1 | iex``

Docs: https://mindattic.github.io/Unstuck/
"@
    # Windows PowerShell turns a native command's stderr into a terminating error under 'Stop'; "release not found"
    # is an expected answer here, not a failure.
    $ErrorActionPreference = 'Continue'
    gh release view $tag --repo mindattic/Unstuck 2>&1 | Out-Null
    $exists = $LASTEXITCODE -eq 0
    if ($exists) {
        gh release upload $tag $zip (Join-Path $out 'Install-Unstuck.bat') --repo mindattic/Unstuck --clobber
    } else {
        gh release create $tag $zip (Join-Path $out 'Install-Unstuck.bat') --repo mindattic/Unstuck --title "Unstuck $version" --notes $notes --latest
    }
    if ($LASTEXITCODE -ne 0) { throw 'gh release failed' }
    Write-Host "  Published https://github.com/mindattic/Unstuck/releases/tag/$tag" -ForegroundColor Green
}
