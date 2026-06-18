#requires -Version 7.4

<#
.SYNOPSIS
Canonical, verified path for creating an Azure DevOps pull request with a full, intact description.

.DESCRIPTION
`az repos pr create` / `az repos pr update` do not reliably accept a single multi-line string for
`--description`: a newline-containing string is truncated at the first newline (a real incident left a
PR with only "## Summary"), and any line beginning with "-" can be parsed by az as an option flag.

This script removes both traps and proves the result:
  1. It splits the description into an array of one-line strings and passes those to az, so az receives
     multiple values and joins them with newlines.
  2. It rejects any description line that begins with "-" (use "*" for markdown bullets) so no line is
     parseable as a flag, and it refuses an empty/whitespace-only description outright.
  3. After create it reads the PR back and compares the non-whitespace content length against a small
     tolerance (Azure DevOps can normalize a few characters on store, so an exact match is too strict);
     on truncation it issues one corrective `az repos pr update` and re-reads. If the description is
     still truncated it throws. The read-back is the proof - the call is never assumed to have worked.

The az invocation is injectable via -AzInvoker so the splitting, flag-guard, and truncation logic are
unit-testable without creating a real pull request (see New-MindNovaPullRequest.Tests.ps1).

.PARAMETER Title
PR title (single line).

.PARAMETER Description
PR description. May be multi-line; pass it as one string and let this script split it safely.

.PARAMETER Repository
Target repository name (e.g. Application.MindNova).

.PARAMETER SourceBranch
Source branch (the work branch). A bare name or a refs/heads/... ref both work.

.PARAMETER TargetBranch
Target branch. Defaults to main.

.PARAMETER Organization
Azure DevOps organization URL. Defaults to [PLACEHOLDER].

.PARAMETER Project
Azure DevOps project. Defaults to [PLACEHOLDER].

.PARAMETER AzInvoker
Test seam. A scriptblock that receives the az argument array (string[]) and returns az stdout. When
omitted, the real `az` CLI is used. Tests pass a fake to exercise the flow offline.

.EXAMPLE
./MindNova/tools/New-MindNovaPullRequest.ps1 -Title "Fix truncated PR descriptions" -Repository Application.MindNova -SourceBranch AZURE-1234 -Description @'
## Summary

Make PR creation pass the description as a line array and verify the read-back.

## Change

* Add a tested helper.
* Update the guidance.
'@

.OUTPUTS
On success, the read-back PR object (PullRequestId, Url, description). Throws on truncation or az failure.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Title,

    [Parameter(Mandatory = $true)]
    [string]$Description,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SourceBranch,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$TargetBranch = 'main',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$Organization = '[PLACEHOLDER]',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$Project = '[PLACEHOLDER]',

    [Parameter()]
    [scriptblock]$AzInvoker
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-PrDescriptionLine {
    param([string]$Description)

    if ($null -eq $Description) {
        return [string[]]@()
    }

    $normalized = $Description -replace "`r`n", "`n" -replace "`r", "`n"
    $lines = [System.Collections.Generic.List[string]]@($normalized -split "`n")

    # A line that is solely a here-string delimiter (@, @', or '@) is never valid
    # description content. It leaks in when the description is passed as a PowerShell
    # here-string from a shell that does not parse @'...'@ (e.g. bash), so the @
    # delimiters arrive as literal first and last lines. Strip them and warn rather
    # than publishing a PR description that starts and ends with a stray @.
    $isDelimiter = { param([string]$Line) $Line.Trim() -in @('@', "@'", "'@") }

    $stripped = $false
    while ($lines.Count -gt 0 -and (& $isDelimiter $lines[0])) {
        $lines.RemoveAt(0)
        $stripped = $true
    }
    while ($lines.Count -gt 0 -and (& $isDelimiter $lines[$lines.Count - 1])) {
        $lines.RemoveAt($lines.Count - 1)
        $stripped = $true
    }

    if ($stripped) {
        Write-Warning "Stripped a leaked here-string delimiter line (@, @', or '@) from the PR description. If you call this script from bash, do not wrap the description in @'...'@; pass it as a normal quoted string, or call the script from PowerShell."
    }

    return [string[]]@($lines)
}

function Assert-PrDescriptionFlagSafe {
    param([string[]]$Lines)

    foreach ($line in $Lines) {
        if ($line -match '^-') {
            throw "PR description contains a line that starts with '-', which az can parse as an option flag and corrupt the call. Use '*' for markdown bullets. Offending line: '$line'."
        }
    }
}

function Get-PrDescriptionContentLength {
    param([string]$Text)

    if ($null -eq $Text) {
        return 0
    }

    return ($Text -replace '\s', '').Length
}

function Test-PrDescriptionTruncated {
    param(
        [int]$ExpectedContentLength,
        [int]$ActualContentLength,
        [int]$MinimumAbsoluteTolerance = 8,
        [double]$RelativeTolerance = 0.02
    )

    # Azure DevOps can normalize a few characters when it stores the description
    # (link/entity encoding, trimming), so an exact length match is too strict and
    # raises false positives on intact descriptions. The failure mode we actually
    # guard against is newline truncation that keeps only the first line, a
    # near-total content loss. Treat the read-back as truncated only when it falls
    # short by more than a small tolerance.
    $tolerance = [Math]::Max($MinimumAbsoluteTolerance, [Math]::Ceiling($ExpectedContentLength * $RelativeTolerance))
    return $ActualContentLength -lt ($ExpectedContentLength - $tolerance)
}

function Invoke-MindNovaAz {
    param(
        [string[]]$Arguments,
        [scriptblock]$Invoker
    )

    if ($Invoker) {
        return (& $Invoker $Arguments)
    }

    $output = & az @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed (exit $LASTEXITCODE): $output"
    }

    return ($output | Out-String)
}

function Get-PrFromJson {
    param([string]$Json)

    if ([string]::IsNullOrWhiteSpace($Json)) {
        throw "az returned no JSON to parse."
    }

    return ($Json | ConvertFrom-Json)
}

function New-MindNovaPullRequest {
    param(
        [string]$Title,
        [string]$Description,
        [string]$Repository,
        [string]$SourceBranch,
        [string]$TargetBranch,
        [string]$Organization,
        [string]$Project,
        [scriptblock]$AzInvoker
    )

    $lines = ConvertTo-PrDescriptionLine -Description $Description
    Assert-PrDescriptionFlagSafe -Lines $lines

    $expectedContentLength = Get-PrDescriptionContentLength -Text ($lines -join "`n")
    if ($expectedContentLength -eq 0) {
        throw "Refusing to create a PR with an empty or whitespace-only description."
    }

    $createArgs = @(
        'repos', 'pr', 'create',
        '--organization', $Organization,
        '--project', $Project,
        '--repository', $Repository,
        '--source-branch', $SourceBranch,
        '--target-branch', $TargetBranch,
        '--title', $Title,
        '--output', 'json',
        '--description'
    ) + $lines

    $created = Get-PrFromJson -Json (Invoke-MindNovaAz -Arguments $createArgs -Invoker $AzInvoker)
    $id = $created.pullRequestId

    $pr = Get-MindNovaPullRequestReadBack -Id $id -Organization $Organization -AzInvoker $AzInvoker
    $actualLength = Get-PrDescriptionContentLength -Text $pr.description

    if (Test-PrDescriptionTruncated -ExpectedContentLength $expectedContentLength -ActualContentLength $actualLength) {
        Write-Warning "PR $id description read back as $actualLength of $expectedContentLength content chars; issuing one corrective update."

        $updateArgs = @(
            'repos', 'pr', 'update',
            '--id', $id,
            '--organization', $Organization,
            '--output', 'json',
            '--description'
        ) + $lines

        Invoke-MindNovaAz -Arguments $updateArgs -Invoker $AzInvoker | Out-Null

        $pr = Get-MindNovaPullRequestReadBack -Id $id -Organization $Organization -AzInvoker $AzInvoker
        $actualLength = Get-PrDescriptionContentLength -Text $pr.description
    }

    if (Test-PrDescriptionTruncated -ExpectedContentLength $expectedContentLength -ActualContentLength $actualLength) {
        throw "PR $id description is truncated after a corrective update: read back $actualLength of $expectedContentLength content chars. Fix this before treating the PR as done."
    }

    Write-Host "PR $id created with verified description ($actualLength content chars). URL: $($pr.PSObject.Properties['url'].Value)"
    return $pr
}

function Get-MindNovaPullRequestReadBack {
    param(
        [object]$Id,
        [string]$Organization,
        [scriptblock]$AzInvoker
    )

    $showArgs = @(
        'repos', 'pr', 'show',
        '--id', $Id,
        '--organization', $Organization,
        '--output', 'json'
    )

    return Get-PrFromJson -Json (Invoke-MindNovaAz -Arguments $showArgs -Invoker $AzInvoker)
}

return New-MindNovaPullRequest `
    -Title $Title `
    -Description $Description `
    -Repository $Repository `
    -SourceBranch $SourceBranch `
    -TargetBranch $TargetBranch `
    -Organization $Organization `
    -Project $Project `
    -AzInvoker $AzInvoker
