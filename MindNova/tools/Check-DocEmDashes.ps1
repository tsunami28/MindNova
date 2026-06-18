#Requires -Version 7
<#
.SYNOPSIS
    House-style doc lint: documentation must not contain the em-dash character (U+2014).

.DESCRIPTION
    Scans AGENTS.md and the documentation wiki layers (docs/ and docs-functional/) for the em-dash
    character (U+2014). The house style uses a spaced hyphen ' - ', commas, colons, or parentheses
    instead. Em-dashes drift in from pasted or model-generated prose and are easy to miss by eye,
    so they are caught mechanically. Exits non-zero (fails the build) when any em-dash is found.

    Sibling of Check-DocLineCitations.ps1 (clause C02) and Check-DocsWiki.ps1: the same shared
    script is run locally by the governance-check-graph skill and (deferred) by the CI gate, so the
    rule lives in one place.

.PARAMETER RepoRoot
    Repository root that contains AGENTS.md, docs/, and docs-functional/. Defaults to two levels
    above this script.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

$emDash = [char]0x2014

$targets = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$agents = Join-Path $RepoRoot 'AGENTS.md'
if (Test-Path $agents) { $targets.Add((Get-Item $agents)) }
foreach ($layer in @('docs', 'docs-functional')) {
    $dir = Join-Path $RepoRoot $layer
    if (Test-Path $dir) {
        Get-ChildItem -Path $dir -Recurse -Include *.md, *.markdown -File | ForEach-Object { $targets.Add($_) }
    }
}

$violations = [System.Collections.Generic.List[object]]::new()
foreach ($file in $targets) {
    $rel = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/')
    $n = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $n++
        if ($line.Contains($emDash)) {
            $violations.Add([pscustomobject]@{ File = $rel; Line = $n; Text = $line.Trim() })
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Doc house-style violation: documentation must not contain the em-dash character (U+2014)." -ForegroundColor Red
    Write-Host "Use a spaced hyphen ' - ', commas, colons, or parentheses instead." -ForegroundColor Red
    foreach ($v in $violations) { Write-Host ("  {0}:{1}  {2}" -f $v.File, $v.Line, $v.Text) }
    Write-Host ("Found {0} line(s) containing an em-dash." -f $violations.Count) -ForegroundColor Red
    exit 1
}

Write-Host "Em-dash OK: no em-dash (U+2014) characters in AGENTS.md, docs/, or docs-functional/."
exit 0
