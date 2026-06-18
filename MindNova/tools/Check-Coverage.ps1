#Requires -Version 7
<#
.SYNOPSIS
    Enforces constitution clause C09: changed code ships with tests, target >= 80% line coverage.

.DESCRIPTION
    Reads an OpenCover-format coverage XML (the merged report the code-analysis pipeline writes to
    $(Agent.TempDirectory)/coverage.opencover.xml via dotnet-coverage) and compares overall line
    coverage against a threshold. Prints overall and per-module coverage, then exits non-zero (fails
    the build) when overall coverage is below the threshold.

    Coverage is read from the root <CoverageSession>/<Summary>: line coverage is computed from
    numSequencePoints / visitedSequencePoints (the authoritative counts). If those counts are absent
    the sequenceCoverage attribute is used as a fallback. Per-module figures come from each
    <Module>/<Summary>.

    SCOPING CAVEAT: "changed projects" scoping (clause C09 targets changed projects specifically) is
    NOT implemented here. This script measures overall and per-module coverage for the whole merged
    report; it does not diff against a base branch to isolate changed projects. Treat the per-module
    breakdown as the closest available approximation and scope by eye if needed.

    ALTERNATIVE PATH: SonarCloud's quality gate is a cleaner enforcement mechanism and is already
    partly wired into code-analysis-pipeline.yml (SonarCloudPrepare consumes the same
    coverage.opencover.xml via sonar.cs.vscoveragexml.reportsPaths; SonarCloudPublish posts the QG).
    Configuring a "coverage on new code >= 80%" condition on the SonarCloud quality gate enforces the
    changed-code intent of C09 without a hand-rolled diff and is the recommended long-term route. This
    script exists as a self-contained, dependency-free gate for pipelines that do not gate on Sonar.

    Sibling of Check-DocLineCitations.ps1 (C02) and Check-DocEmDashes.ps1 (C11): same house style, one
    rule per script, run locally and (deferred) in CI.

.PARAMETER CoverageFile
    Path to an OpenCover-format coverage XML. In CI this is
    $(Agent.TempDirectory)/coverage.opencover.xml produced by the "Merge code coverage files" step.

.PARAMETER Threshold
    Minimum acceptable overall line coverage percent. Defaults to 80 (the C09 target). Set this to the
    current measured baseline while real coverage is below 80% to gate against regression without
    failing the build on day one (see the recommendation in the .DESCRIPTION).

.PARAMETER RepoRoot
    Optional repository root, used only to render the coverage file path relative to the repo in
    output. Does not affect the pass/fail decision. Defaults to two levels above this script.

.EXAMPLE
    ./Check-Coverage.ps1 -CoverageFile "$(Agent.TempDirectory)/coverage.opencover.xml" -Threshold 80

.EXAMPLE
    ./Check-Coverage.ps1 -CoverageFile ./coverage.opencover.xml -Threshold 42
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$CoverageFile,

    [double]$Threshold = 80,

    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

function Get-LineCoveragePercent {
    param([System.Xml.XmlElement]$Summary)

    if ($null -eq $Summary) { return $null }

    $numAttr = $Summary.GetAttribute('numSequencePoints')
    $visitedAttr = $Summary.GetAttribute('visitedSequencePoints')

    $num = 0
    $visited = 0
    $haveCounts = [int]::TryParse($numAttr, [ref]$num) -and [int]::TryParse($visitedAttr, [ref]$visited)

    if ($haveCounts -and $num -gt 0) {
        return [math]::Round(($visited / $num) * 100, 2)
    }

    if ($haveCounts -and $num -eq 0) {
        return $null
    }

    $seqCov = 0.0
    if ([double]::TryParse($Summary.GetAttribute('sequenceCoverage'), [ref]$seqCov)) {
        return [math]::Round($seqCov, 2)
    }

    return $null
}

if (-not (Test-Path -LiteralPath $CoverageFile)) {
    Write-Host "C09 coverage gate: coverage file not found at '$CoverageFile'." -ForegroundColor Red
    Write-Host "A missing coverage report means tests or coverage collection did not run; treating as a failure." -ForegroundColor Red
    Write-Host "Expected the merged OpenCover XML from the 'Merge code coverage files' step (code-analysis-pipeline.yml)." -ForegroundColor Red
    exit 1
}

try {
    [xml]$doc = Get-Content -LiteralPath $CoverageFile -Raw
}
catch {
    Write-Host "C09 coverage gate: failed to parse '$CoverageFile' as XML: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$session = $doc.CoverageSession
if ($null -eq $session) {
    Write-Host "C09 coverage gate: '$CoverageFile' has no <CoverageSession> root; not an OpenCover report." -ForegroundColor Red
    exit 1
}

$overall = Get-LineCoveragePercent -Summary $session.Summary
if ($null -eq $overall) {
    Write-Host "C09 coverage gate: could not read overall line coverage from <CoverageSession>/<Summary> in '$CoverageFile'." -ForegroundColor Red
    Write-Host "Expected numSequencePoints / visitedSequencePoints (or a sequenceCoverage attribute)." -ForegroundColor Red
    exit 1
}

$relFile = $CoverageFile
try {
    $resolved = (Resolve-Path -LiteralPath $CoverageFile).Path
    $relFile = [IO.Path]::GetRelativePath($RepoRoot, $resolved).Replace('\', '/')
}
catch { }

Write-Host "C09 coverage gate (overall line coverage, OpenCover): $relFile"

$modules = @($session.Modules.Module)
if ($modules.Count -gt 0) {
    Write-Host "Per-module line coverage:"
    foreach ($m in $modules) {
        if ($null -eq $m) { continue }
        $name = $m.ModuleName
        if ([string]::IsNullOrWhiteSpace($name)) { $name = $m.FullName }
        $modCov = Get-LineCoveragePercent -Summary $m.Summary
        if ($null -eq $modCov) {
            Write-Host ("  {0,7}  {1}" -f 'n/a', $name)
        }
        else {
            Write-Host ("  {0,6:N2}%  {1}" -f $modCov, $name)
        }
    }
}

Write-Host ("Overall line coverage: {0:N2}% (required >= {1:N2}%)." -f $overall, $Threshold)

if ($overall -lt $Threshold) {
    Write-Host ("C09 coverage gate FAILED: {0:N2}% is below the required {1:N2}%." -f $overall, $Threshold) -ForegroundColor Red
    Write-Host "Changed code must ship with tests (constitution clause C09). Add tests to raise line coverage." -ForegroundColor Red
    exit 1
}

Write-Host ("C09 OK: overall line coverage {0:N2}% meets the {1:N2}% threshold." -f $overall, $Threshold) -ForegroundColor Green
exit 0
