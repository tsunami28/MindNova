#Requires -Version 7
<#
.SYNOPSIS
    Runs the AI-SDLC / constitution gate scripts locally, the same ones the code-analysis pipeline
    runs, and prints a single pass/fail summary.

.DESCRIPTION
    A developer convenience: one command to check a change against every gate before pushing, instead
    of waiting for CI. Blocking gates (structure, docs, house-style) fail this run on a violation;
    advisory gates (the ones the pipeline reports as warnings while the codebase clears pre-existing
    debt) are reported but never fail the local run, matching CI behaviour.

    The coverage gate (C09) needs a merged OpenCover report and is skipped unless you pass
    -CoverageFile. Run the test suite with coverage first if you want it included.

.PARAMETER RepoRoot
    Repository root. Defaults to two levels above this script.

.PARAMETER CoverageFile
    Optional path to a merged OpenCover XML to run the C09 coverage gate against.

.PARAMETER IncludeToolTests
    Also run the Pester tests for the gate scripts (Invoke-Pester on this tools folder).

.EXAMPLE
    pwsh MindNova/tools/Invoke-LocalGates.ps1

.EXAMPLE
    pwsh MindNova/tools/Invoke-LocalGates.ps1 -IncludeToolTests
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path,
    [string]$CoverageFile,
    [switch]$IncludeToolTests,
    [switch]$BlockingOnly
)

$ErrorActionPreference = 'Stop'
$tools = $PSScriptRoot

$gates = @(
    @{ Name = 'C02 no source line-number citations'; Script = 'Check-DocLineCitations.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Blocking' }
    @{ Name = 'AI-SDLC artifact-graph links resolve'; Script = 'Check-ArtifactGraph.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Blocking' }
    @{ Name = 'C10 docs wiki: no orphans or broken links'; Script = 'Check-DocsWiki.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Blocking' }
    @{ Name = 'C10 docs index in sync'; Script = 'Build-DocsIndex.ps1'; Args = @{ Check = $true; RepoRoot = $RepoRoot }; Mode = 'Blocking' }
    @{ Name = 'C11 no em-dash characters'; Script = 'Check-DocEmDashes.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Blocking' }
    @{ Name = 'AI-SDLC skill hygiene'; Script = 'Check-SkillHygiene.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Blocking' }
    @{ Name = 'Story-trait coverage (fails only on malformed key)'; Script = 'Check-StoryTraits.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Blocking' }
    @{ Name = 'C05 log via LogMindNova'; Script = 'Check-LogMindNova.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Advisory' }
    @{ Name = 'C04 ARG KQL inputs escaped'; Script = 'Check-KqlEscaping.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Advisory' }
    @{ Name = 'C06 PascalCase JSON wire names'; Script = 'Check-ApiPascalCase.ps1'; Args = @{ RepoRoot = $RepoRoot }; Mode = 'Advisory' }
)

if ($CoverageFile) {
    $gates += @{ Name = 'C09 line coverage >= 80%'; Script = 'Check-Coverage.ps1'; Args = @{ CoverageFile = $CoverageFile; Threshold = 80; RepoRoot = $RepoRoot }; Mode = 'Advisory' }
}

if ($BlockingOnly) {
    $gates = @($gates | Where-Object { $_.Mode -eq 'Blocking' })
}

$results = [System.Collections.Generic.List[object]]::new()
foreach ($g in $gates) {
    $path = Join-Path $tools $g.Script
    Write-Host ""
    Write-Host ("===== {0} ({1}) =====" -f $g.Name, $g.Mode) -ForegroundColor Cyan
    $gateArgs = $g.Args
    & $path @gateArgs
    $code = $LASTEXITCODE
    $status = if ($code -eq 0) { 'PASS' } elseif ($g.Mode -eq 'Advisory') { 'ADVISORY' } else { 'FAIL' }
    $results.Add([pscustomobject]@{ Gate = $g.Name; Mode = $g.Mode; Status = $status })
}

$toolTestsFailed = $false
if ($IncludeToolTests) {
    Write-Host ""
    Write-Host "===== Gate-script Pester tests =====" -ForegroundColor Cyan
    $pester = Invoke-Pester -Path $tools -PassThru -Output Detailed
    if ($pester.FailedCount -gt 0) { $toolTestsFailed = $true }
    $results.Add([pscustomobject]@{ Gate = "Gate-script tests ($($pester.PassedCount)/$($pester.TotalCount))"; Mode = 'Blocking'; Status = $(if ($toolTestsFailed) { 'FAIL' } else { 'PASS' }) })
}

Write-Host ""
Write-Host "===== Summary =====" -ForegroundColor Cyan
$results | Format-Table -AutoSize | Out-String | Write-Host

$blockingFailures = @($results | Where-Object { $_.Status -eq 'FAIL' })
if ($blockingFailures.Count -gt 0) {
    Write-Host ("{0} blocking gate(s) failed." -f $blockingFailures.Count) -ForegroundColor Red
    exit 1
}

$advisories = @($results | Where-Object { $_.Status -eq 'ADVISORY' })
if ($advisories.Count -gt 0) {
    Write-Host ("All blocking gates pass. {0} advisory gate(s) reported issues (not blocking)." -f $advisories.Count) -ForegroundColor Yellow
}
else {
    Write-Host "All gates pass." -ForegroundColor Green
}
exit 0
