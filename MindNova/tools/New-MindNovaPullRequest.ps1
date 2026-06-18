#requires -Version 7.4

<#
.SYNOPSIS
Canonical, verified path for creating a GitHub pull request with a full, intact description.

.DESCRIPTION
Creates a PR via `gh pr create`, reads it back, and throws on an empty description. The gh CLI
handles multi-line bodies reliably, so this script is intentionally lean: validate input, create,
verify read-back.

The gh invocation is injectable via -GhInvoker so the validation and read-back logic are
unit-testable without creating a real pull request (see New-MindNovaPullRequest.Tests.ps1).

.PARAMETER Title
PR title (single line).

.PARAMETER Description
PR description (body). May be multi-line.

.PARAMETER SourceBranch
Source branch (the work branch). Defaults to the current branch.

.PARAMETER TargetBranch
Target branch. Defaults to main.

.PARAMETER GhInvoker
Test seam. A scriptblock that receives an argument hashtable with keys Command (create/view) and
Args (string[]) and returns gh stdout. When omitted, the real `gh` CLI is used. Tests pass a fake
to exercise the flow offline.

.EXAMPLE
./MindNova/tools/New-MindNovaPullRequest.ps1 -Title "Fix truncated PR descriptions" -Description @'
## Summary

Make PR creation pass the description reliably and verify the read-back.

## Change

* Add a tested helper.
* Update the guidance.
'@

.OUTPUTS
On success, the read-back PR object (number, url, body). Throws on empty description or gh failure.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Title,

    [Parameter(Mandatory = $true)]
    [string]$Description,

    [Parameter()]
    [string]$SourceBranch,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$TargetBranch = 'main',

    [Parameter()]
    [scriptblock]$GhInvoker
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Gh {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [scriptblock]$Invoker
    )

    if ($Invoker) {
        return (& $Invoker @{ Command = $Command; Args = $Arguments })
    }

    $output = & gh @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "gh $Command failed (exit $LASTEXITCODE): $output"
    }
    return $output
}

# --- Validate ---
$trimmed = $Description.Trim()
if ([string]::IsNullOrWhiteSpace($trimmed)) {
    throw "PR description is empty or whitespace-only. A PR must have a meaningful description."
}

# --- Create ---
$createArgs = @('pr', 'create', '--title', $Title, '--body', $Description, '--base', $TargetBranch)
if ($SourceBranch) {
    $createArgs += '--head'
    $createArgs += $SourceBranch
}

$createOutput = Invoke-Gh -Command 'create' -Arguments $createArgs -Invoker $GhInvoker

# gh pr create returns the PR URL on success; extract the number
$prNumber = $null
if ($createOutput -match '/pull/(\d+)') {
    $prNumber = $Matches[1]
}
elseif ($createOutput -match '"number":\s*(\d+)') {
    $prNumber = $Matches[1]
}
else {
    throw "Could not extract PR number from gh output: $createOutput"
}

# --- Read back and verify ---
$viewArgs = @('pr', 'view', $prNumber, '--json', 'number,url,body')
$viewOutput = Invoke-Gh -Command 'view' -Arguments $viewArgs -Invoker $GhInvoker
$pr = $viewOutput | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($pr.body)) {
    throw "PR #$prNumber was created but the description is empty on read-back. Manual intervention required."
}

return $pr
