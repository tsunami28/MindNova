#Requires -Version 7
<#
.SYNOPSIS
    Validates the AI-SDLC artifact graph (docs/ai-sdlc/artifact-graph.md).

.DESCRIPTION
    Every artifact under docs/, docs-functional/, or specs/ that declares `story:` in YAML front-matter has "opted
    in" to the graph. For those, this checker verifies:
      1. `story` is a valid work-item key (e.g. AZURE-1234).
      2. `phase` is present and one of the known phases.
      3. Each `relates.*` repo path resolves to a file; each URL is well-formed.
    Artifacts without front-matter (or without `story:`) are ignored, so adoption is incremental.
    Exits non-zero (fails the build) on any violation.

.PARAMETER RepoRoot
    Repository root containing docs/, docs-functional/, and specs/. Defaults to two levels above this script.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

$phases = @('discovery', 'elaboration', 'create', 'mature', 'governance')
$storyPattern = '^[A-Z][A-Z0-9]+-\d+$'

$roots = @('docs', 'docs-functional', 'specs') | ForEach-Object { Join-Path $RepoRoot $_ } | Where-Object { Test-Path $_ }
$files = @()
if ($roots) { $files = Get-ChildItem -Path $roots -Recurse -Include *.md, *.markdown -File }

$violations = [System.Collections.Generic.List[string]]::new()
$checked = 0

function Get-FrontMatterValue([string]$fm, [string]$key) {
    $m = [regex]::Match($fm, "(?m)^\s*$key`:\s*(.+?)\s*$")
    if ($m.Success) { return $m.Groups[1].Value.Trim().Trim('"').Trim("'") }
    return $null
}

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $fmMatch = [regex]::Match($text, '\A\s*---\r?\n(.*?)\r?\n---\r?\n', 'Singleline')
    if (-not $fmMatch.Success) { continue }
    $fm = $fmMatch.Groups[1].Value

    $story = Get-FrontMatterValue $fm 'story'
    if (-not $story) { continue }   # not opted into the graph

    $checked++
    $rel = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/')

    if ($story -cnotmatch $storyPattern) {
        $violations.Add("$rel : story '$story' is not a valid work-item key (e.g. AZURE-1234).")
    }

    $phase = Get-FrontMatterValue $fm 'phase'
    if (-not $phase) {
        $violations.Add("$rel : declares 'story' but is missing 'phase'.")
    }
    elseif ($phase.ToLower() -notin $phases) {
        $violations.Add("$rel : phase '$phase' is not one of: $($phases -join ', ').")
    }

    $relBlock = [regex]::Match($fm, '(?ms)^\s*relates:\s*\r?\n(.*)$')
    if ($relBlock.Success) {
        foreach ($line in ($relBlock.Groups[1].Value -split '\r?\n')) {
            if ($line -match '^\s+([A-Za-z0-9_-]+):\s*(.+?)\s*$') {
                $k = $Matches[1]; $v = $Matches[2].Trim().Trim('"').Trim("'")
                if ($v -match '^https?://') { continue }       # external URL: presence only
                if (-not (Test-Path (Join-Path $RepoRoot $v))) {
                    $violations.Add("$rel : relates.$k -> '$v' does not resolve to a file in the repo.")
                }
            }
            elseif ($line.Trim() -ne '') { break }              # end of the relates block
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Artifact-graph violations (see docs/ai-sdlc/artifact-graph.md):" -ForegroundColor Red
    foreach ($v in $violations) { Write-Host "  $v" }
    Write-Host ("Checked {0} graph artifact(s); {1} violation(s)." -f $checked, $violations.Count) -ForegroundColor Red
    exit 1
}

Write-Host ("Artifact graph OK: {0} graph artifact(s) validated, no violations." -f $checked)
exit 0
