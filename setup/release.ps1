#Requires -Version 5.1
<#
.SYNOPSIS
    Releases the next whole-number version of Unstuck (v2, v3, v4...) with its installer on GitHub Releases.

.DESCRIPTION
    1. Picks the version: the csproj's current major if it was never tagged, otherwise one past the highest vN tag.
    2. If that's a bump, sets <Version> in UnstuckTaskbar.csproj to N.0.0 and commits "Release vN (<Ticket>)".
    3. Builds Unstuck-Setup-vN.exe (setup\build.ps1).
    4. Tags vN, pushes main and the tag, and creates the GitHub release with the installer attached.

    Builds run here rather than in GitHub Actions because the MindAttic packages restore from C:\LocalNuGet.

.PARAMETER Ticket
    The ticket ID for the version-bump commit (the commit-msg hook requires one), e.g. UNS-T40.

.PARAMETER Dry
    Show what would happen and change nothing.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File setup\release.ps1 -Ticket UNS-T40
#>
[CmdletBinding()]
param(
    [string]$Ticket,
    [switch]$Dry
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root 'UnstuckTaskbar\UnstuckTaskbar.csproj'
$repo = 'mindattic/Unstuck'

function Invoke-Git {
    # git reports progress on stderr; under 'Stop', Windows PowerShell would treat that as a failure.
    $ErrorActionPreference = 'Continue'
    $o = & git.exe -C $root @args 2>&1 | ForEach-Object { "$_" }
    if ($LASTEXITCODE -ne 0) { throw "git $args failed: $o" }
    $o
}

if (Invoke-Git status --porcelain) { throw 'Commit or stash your changes first.' }
$branch = Invoke-Git rev-parse --abbrev-ref HEAD
if ($branch -ne 'main') { throw "Release from main (you are on $branch)." }
try { Invoke-Git fetch --tags --quiet | Out-Null } catch { Write-Host '  Offline: using local tags.' -ForegroundColor Yellow }

$current = [int](([xml](Get-Content $proj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Split('.')[0]
$released = @(Invoke-Git tag --list 'v*' | Where-Object { $_ -match '^v(\d+)$' } | ForEach-Object { [int]$_.Substring(1) })
$n = if (($released -notcontains $current) -and -not ($released | Where-Object { $_ -ge $current })) { $current }
     else { [Math]::Max($current, ($released | Measure-Object -Maximum).Maximum) + 1 }
$tag = "v$n"
Write-Host "  Releasing Unstuck $tag$(if ($Dry) { ' (dry run)' })" -ForegroundColor Cyan
if ($Dry) { return }

if ($n -ne $current) {
    if (-not $Ticket) { throw "Releasing $tag bumps the version; pass -Ticket <ID> for the commit." }
    $text = [IO.File]::ReadAllText($proj)
    $text = [regex]::Replace($text, '<Version>[^<]*</Version>', "<Version>$n.0.0</Version>")
    [IO.File]::WriteAllText($proj, $text)
    Invoke-Git commit -am "Release $tag ($Ticket)" | Out-Null
}

& (Join-Path $PSScriptRoot 'build.ps1')
$setup = Join-Path $root "artifacts\installer\Unstuck-Setup-$tag.exe"

Invoke-Git tag -a $tag -m "Unstuck $tag" | Out-Null
Invoke-Git push origin HEAD | Out-Null
Invoke-Git push origin $tag | Out-Null

# The active gh account may not be able to write to mindattic/; prefer the mindattic login when it's there.
if (-not $env:GH_TOKEN) {
    $ErrorActionPreference = 'Continue'
    $t = gh auth token -u mindattic 2>$null
    if ($LASTEXITCODE -eq 0 -and $t) { $env:GH_TOKEN = $t }
    $ErrorActionPreference = 'Stop'
}
$notes = @"
**Install:** download **Unstuck-Setup-$tag.exe** below and run it. No admin rights and no .NET needed. If SmartScreen warns about an unrecognized app, choose *More info* then *Run anyway*.

Then, whenever clicks stop working, press **Ctrl+Alt+Break** (or **Ctrl+Alt+U**).

**Update:** run a newer installer over the old one. **Uninstall:** Windows Settings > Apps > Unstuck.

Docs: https://mindattic.github.io/Unstuck/
"@
gh release create $tag $setup --repo $repo --title "Unstuck $tag" --notes $notes --latest
if ($LASTEXITCODE -ne 0) { throw 'gh release create failed' }
Write-Host "  Published https://github.com/$repo/releases/tag/$tag" -ForegroundColor Green
