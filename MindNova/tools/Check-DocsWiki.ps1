#Requires -Version 7
<#
.SYNOPSIS
    Validates wiki integrity for the docs/ and docs-functional/ layers (orphans and broken relative links).

.DESCRIPTION
    Enforces two of the docs-wiki lint rules from docs/ai-sdlc/docs-wiki.md over the two wiki layers,
    docs/ (technical) and docs-functional/ (functional), across their .md and .markdown pages:

      1. Broken relative links - every inline markdown link [text](target) whose target is a relative
         path to a local file must resolve to an existing file. External links (http://, https://,
         mailto:) and pure #anchor links are ignored; for a path#anchor target the #anchor is stripped
         and only the path is validated. The target is resolved relative to the linking page's folder.

      2. Orphans - a page is an orphan when it is BOTH (a) not linked from its layer's index file
         (docs/index.md or docs-functional/index.md) AND (b) not linked by any other wiki page via a
         relative link. Entry-point files (any index.md and any README.md) are exempt. If a layer's
         index file is absent, part (a) is treated as "not indexed" for that layer and a note is
         printed; part (b) still applies.

    This checker deliberately does NOT verify index drift (that is Build-DocsIndex.ps1 in -Check mode)
    nor artifact-graph front-matter (that is Check-ArtifactGraph.ps1). Exits non-zero on any violation.

.PARAMETER RepoRoot
    Repository root containing docs/ and docs-functional/. Defaults to two levels above this script.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

$layers = @('docs', 'docs-functional')
$linkPattern = '\[[^\]]*\]\(\s*([^)\s]+)\s*\)'

function Get-RelPath([string]$root, [string]$full) {
    return [IO.Path]::GetRelativePath($root, $full).Replace('\', '/')
}

function Test-IsExternalOrAnchor([string]$target) {
    if ($target -match '^(?i:https?://|mailto:)') { return $true }
    if ($target.StartsWith('#')) { return $true }
    return $false
}

function Get-LinkTargets([string]$text) {
    $targets = [System.Collections.Generic.List[string]]::new()
    foreach ($m in [regex]::Matches($text, $linkPattern)) {
        $targets.Add($m.Groups[1].Value)
    }
    return $targets
}

function Resolve-LinkPath([string]$pageDir, [string]$target) {
    $pathPart = ($target -split '#', 2)[0]
    if ([string]::IsNullOrWhiteSpace($pathPart)) { return $null }
    $decoded = [uri]::UnescapeDataString($pathPart)
    $combined = [IO.Path]::Combine($pageDir, $decoded)
    return [IO.Path]::GetFullPath($combined)
}

function Remove-Code([string]$text) {
    # Drop fenced code blocks and inline code spans so links shown as examples are not scanned as links.
    $noFence = [regex]::Replace($text, '(?ms)^[ \t]*(`{3,}|~{3,}).*?^[ \t]*\1[ \t]*$', '')
    return [regex]::Replace($noFence, '`[^`\r\n]+`', '')
}

$pages = [System.Collections.Generic.List[object]]::new()
foreach ($layer in $layers) {
    $layerRoot = Join-Path $RepoRoot $layer
    if (-not (Test-Path $layerRoot)) { continue }
    foreach ($file in (Get-ChildItem -Path $layerRoot -Recurse -Include *.md, *.markdown -File)) {
        $pages.Add([pscustomobject]@{
            Layer    = $layer
            FullPath = $file.FullName
            Dir      = $file.DirectoryName
            Rel      = Get-RelPath $RepoRoot $file.FullName
            Name     = $file.Name
            Text     = Remove-Code (Get-Content -LiteralPath $file.FullName -Raw)
        })
    }
}

$violations = [System.Collections.Generic.List[string]]::new()
$notes = [System.Collections.Generic.List[string]]::new()

$linkedTargets = [System.Collections.Generic.HashSet[string]]::new()
$indexedTargets = @{}
foreach ($layer in $layers) { $indexedTargets[$layer] = [System.Collections.Generic.HashSet[string]]::new() }

foreach ($page in $pages) {
    foreach ($target in (Get-LinkTargets $page.Text)) {
        if (Test-IsExternalOrAnchor $target) { continue }

        $resolved = Resolve-LinkPath $page.Dir $target
        if ($null -eq $resolved) { continue }

        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            $violations.Add(("{0} : broken relative link -> '{1}' (does not resolve to a file)" -f $page.Rel, $target))
            continue
        }

        $resolvedKey = (Get-RelPath $RepoRoot $resolved).ToLowerInvariant()
        $isIndex = ($page.Name -ieq 'index.md')
        if ($isIndex) {
            [void]$indexedTargets[$page.Layer].Add($resolvedKey)
        }
        else {
            [void]$linkedTargets.Add($resolvedKey)
        }
    }
}

$indexPresent = @{}
foreach ($layer in $layers) {
    $layerRoot = Join-Path $RepoRoot $layer
    if (-not (Test-Path $layerRoot)) { continue }
    $indexPath = Join-Path $layerRoot 'index.md'
    $indexPresent[$layer] = Test-Path -LiteralPath $indexPath -PathType Leaf
    if (-not $indexPresent[$layer]) {
        $notes.Add(("note: {0}/index.md is absent - skipping the index check for this layer (orphans judged by inbound links only)" -f $layer))
    }
}

foreach ($page in $pages) {
    if (($page.Name -ieq 'index.md') -or ($page.Name -ieq 'readme.md') -or ($page.Name -ilike '*template*')) { continue }

    $key = $page.Rel.ToLowerInvariant()
    $isIndexed = $indexedTargets[$page.Layer].Contains($key)
    $isLinked = $linkedTargets.Contains($key)

    if ((-not $isIndexed) -and (-not $isLinked)) {
        $violations.Add(("{0} : orphan - not listed in {1}/index.md and not linked by any other wiki page" -f $page.Rel, $page.Layer))
    }
}

foreach ($note in $notes) { Write-Host "  $note" -ForegroundColor Yellow }

if ($violations.Count -gt 0) {
    Write-Host "Docs-wiki violations (see docs/ai-sdlc/docs-wiki.md):" -ForegroundColor Red
    foreach ($v in ($violations | Sort-Object)) { Write-Host "  $v" }
    Write-Host ("Checked {0} wiki page(s); {1} violation(s)." -f $pages.Count, $violations.Count) -ForegroundColor Red
    exit 1
}

Write-Host ("Docs-wiki OK: {0} wiki page(s) validated, no orphans or broken relative links." -f $pages.Count)
exit 0
