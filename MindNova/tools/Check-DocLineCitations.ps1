#Requires -Version 7
<#
.SYNOPSIS
    Enforces constitution clause C02: documentation must not cite source by line number.

.DESCRIPTION
    Scans AGENTS.md and docs/ for source line-number citations (file.ext:NN or "line NN"),
    which rot on the next edit and silently mislead. Reference code by file path and symbol
    name instead. Exits non-zero (fails the build) when any citation is found.

    Two files are allow-listed because they DESCRIBE the rule rather than violate it:
    docs/constitution.md and docs/adrs/README.md.

.PARAMETER RepoRoot
    Repository root that contains AGENTS.md and docs/. Defaults to two levels above this script.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

$allowList = @(
    'docs/constitution.md',
    'docs/adrs/README.md'
)

$patterns = @(
    '\.(?:cs|ts|tsx|razor|bicep|yml|yaml|json|ps1|js|css|sln|csproj):\d+',  # file.ext:NN
    '\b[Ll]ines?\s+\d+'                                                      # line NN / lines NN-MM
)

$targets = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$agents = Join-Path $RepoRoot 'AGENTS.md'
if (Test-Path $agents) { $targets.Add((Get-Item $agents)) }
$docsDir = Join-Path $RepoRoot 'docs'
if (Test-Path $docsDir) { Get-ChildItem -Path $docsDir -Recurse -Filter *.md -File | ForEach-Object { $targets.Add($_) } }

$violations = [System.Collections.Generic.List[object]]::new()
foreach ($file in $targets) {
    $rel = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/')
    if ($allowList -contains $rel) { continue }
    $n = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $n++
        foreach ($p in $patterns) {
            if ($line -match $p) {
                $violations.Add([pscustomobject]@{ File = $rel; Line = $n; Text = $line.Trim() })
                break
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "C02 violation: documentation must not cite source line numbers." -ForegroundColor Red
    Write-Host "Reference code by file path and symbol name instead (see docs/constitution.md, clause C02)." -ForegroundColor Red
    foreach ($v in $violations) { Write-Host ("  {0}:{1}  {2}" -f $v.File, $v.Line, $v.Text) }
    Write-Host ("Found {0} line-number citation(s)." -f $violations.Count) -ForegroundColor Red
    exit 1
}

Write-Host "C02 OK: no source line-number citations in AGENTS.md or docs/."
exit 0
