#Requires -Version 5.1
<#
.SYNOPSIS
    Builds artifacts\installer\Unstuck-Setup-v<N>.exe, the Windows installer.

.DESCRIPTION
    1. Publishes UnstuckTaskbar.exe self-contained and single-file, so the people installing it need no .NET.
    2. Stages it with unstuck.ps1, unstuck.bat and logo.ico.
    3. Compiles setup\Unstuck.iss with Inno Setup 6 (winget install JRSoftware.InnoSetup).

    N is the whole-number major of <Version> in UnstuckTaskbar.csproj (releases are v1, v2, v3...). This runs
    locally, not in CI: the MindAttic packages restore from C:\LocalNuGet, which isn't on nuget.org.
    setup\release.ps1 calls it; run it on its own to build without publishing.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File setup\build.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root 'UnstuckTaskbar\UnstuckTaskbar.csproj'
$out = Join-Path $root 'artifacts\installer'
$stage = Join-Path $out 'stage'

$version = ([xml](Get-Content $proj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "No <Version> in $proj" }
$parts = $version.Split('.')
if (@($parts | Select-Object -Skip 1 | Where-Object { $_ -ne '0' }).Count -gt 0) {
    throw "Version $version isn't a whole number. Set <Version> in $proj to N.0.0."
}
$major = $parts[0]

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

# 1. tray app
Write-Host "  Publishing UnstuckTaskbar v$major (self-contained, single file)" -ForegroundColor Cyan
dotnet publish $proj -c Release -r win-x64 --self-contained true `
    /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true `
    /p:DebugType=none -o (Join-Path $out 'publish') | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

# 2. stage
Copy-Item (Join-Path $out 'publish\UnstuckTaskbar.exe') $stage
Copy-Item (Join-Path $root 'unstuck.ps1'), (Join-Path $root 'unstuck.bat'), (Join-Path $root 'assets\logo.ico') $stage

# 3. installer
$iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 not found (winget install JRSoftware.InnoSetup)' }
& $iscc /Q "/DMajor=$major" "/DStage=$stage" "/O$out" (Join-Path $PSScriptRoot 'Unstuck.iss')
if ($LASTEXITCODE -ne 0) { throw "installer build failed ($LASTEXITCODE)" }

$setup = Join-Path $out "Unstuck-Setup-v$major.exe"
Write-Host ("  Built {0} ({1:N1} MB)" -f $setup, ((Get-Item $setup).Length / 1MB)) -ForegroundColor Green
