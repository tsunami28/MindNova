#Requires -Version 7
<#
.SYNOPSIS
    Enable this repository's git hooks (the pre-push gate) for the current clone.

.DESCRIPTION
    Git does not install hooks automatically on clone, so each developer enables them once. This
    points core.hooksPath at the committed .githooks/ directory, which contains a pre-push hook that
    runs the blocking CA gates (docs, skills, graph, house-style) before every push - from any tool,
    because it is a git hook, not an agent feature.

    Run once per clone:  pwsh MindNova/tools/Install-GitHooks.ps1
    Bypass a single push: git push --no-verify
    Disable again:        git config --unset core.hooksPath

.PARAMETER RepoRoot
    Repository root. Defaults to two levels above this script.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

Push-Location $RepoRoot
try {
    if (-not (Test-Path (Join-Path $RepoRoot '.githooks/pre-push'))) {
        Write-Host "No .githooks/pre-push found under $RepoRoot; nothing to enable." -ForegroundColor Red
        exit 1
    }

    git config core.hooksPath .githooks
    Write-Host "Enabled git hooks: core.hooksPath = .githooks"

    if (Get-Command pwsh -ErrorAction SilentlyContinue) {
        Write-Host "PowerShell 7 (pwsh) found; the pre-push gate will run on push." -ForegroundColor Green
    }
    else {
        Write-Host "PowerShell 7 (pwsh) is not on PATH; the pre-push hook will warn and allow pushes until you install it." -ForegroundColor Yellow
    }

    Write-Host "Bypass a single push with: git push --no-verify"
    Write-Host "Disable again with:        git config --unset core.hooksPath"
}
finally {
    Pop-Location
}
exit 0
