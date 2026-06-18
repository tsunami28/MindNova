#Requires -Version 7
<#
.SYNOPSIS
    Runs a constitution / AI-SDLC gate script in Azure DevOps CI with a readable result.

.DESCRIPTION
    The gate scripts (Check-*.ps1) signal "issues found" with a non-zero exit code. Run directly in a
    PowerShell@2 task that surfaces in the build as the opaque "PowerShell exited with code '1'", which
    reads like the script crashed rather than the validation simply finding issues.

    This wrapper runs the gate, lets its own output (the human-readable violation list) flow to the
    log, and then translates a non-zero exit into a proper Azure DevOps annotation:

      - Advisory mode: a warning + SucceededWithIssues (the gate does not block the build yet).
      - Blocking mode: an error + Failed (the gate fails the build, but with a clean message instead
        of "PowerShell exited with code 1").

    In both modes the wrapper itself exits 0, so the task result comes from the logging command, not
    a raw exit code. The gate script is unchanged and still exits non-zero on a developer machine.

    A genuine crash in the gate (a thrown, terminating error rather than a clean non-zero exit) still
    propagates and fails the task loudly, so real script bugs are not masked.

.PARAMETER Script
    Path to the gate script to run (e.g. Check-DocEmDashes.ps1).

.PARAMETER Clause
    Constitution clause id or short tag used in the message (e.g. C11, AI-SDLC).

.PARAMETER What
    Short human label used in the message (e.g. "no em-dash characters").

.PARAMETER Mode
    Advisory (default) emits a warning and SucceededWithIssues. Blocking emits an error and Failed.

.PARAMETER GateArgs
    Remaining arguments are passed through verbatim to the gate script (e.g. -RepoRoot <path>, or
    -CoverageFile <x> -Threshold 80). Captured via ValueFromRemainingArguments so callers pass them
    as plain trailing arguments.

.EXAMPLE
    pwsh Invoke-Gate.ps1 -Script ./Check-DocEmDashes.ps1 -Clause C11 -What "no em-dash characters" -Mode Blocking -RepoRoot .

.EXAMPLE
    pwsh Invoke-Gate.ps1 -Script ./Check-LogMindNova.ps1 -Clause C05 -What "logging via LogMindNova" -Mode Advisory -RepoRoot .
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Script,
    [Parameter(Mandatory)][string]$Clause,
    [Parameter(Mandatory)][string]$What,
    [ValidateSet('Advisory', 'Blocking')][string]$Mode = 'Advisory',
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$GateArgs
)

# Parse the pass-through args ("-Name value" pairs and bare "-Switch") into a hashtable and splat
# THAT. Array-splatting $GateArgs binds positionally and mis-handles "-Name value", so a gate's
# -RepoRoot would not bind. Gate values here are paths, a number, and switches, none starting with '-'.
$splat = @{}
for ($i = 0; $i -lt $GateArgs.Count; $i++) {
    $token = $GateArgs[$i]
    if ($token -notlike '-*') { continue }
    $name = $token.TrimStart('-')
    if (($i + 1) -lt $GateArgs.Count -and ($GateArgs[$i + 1] -notlike '-*')) {
        $splat[$name] = $GateArgs[$i + 1]
        $i++
    }
    else {
        $splat[$name] = $true
    }
}

& $Script @splat
$code = $LASTEXITCODE

if ($code -ne 0) {
    if ($Mode -eq 'Blocking') {
        Write-Host ("##vso[task.logissue type=error]{0}: {1} failed (see the log above for the specific findings). This gate is blocking; fix the listed issues to pass CI." -f $Clause, $What)
        Write-Host ("##vso[task.complete result=Failed;]{0}: {1} failed" -f $Clause, $What)
    }
    else {
        Write-Host ("##vso[task.logissue type=warning]{0} advisory: {1} found issue(s) (see the log above). Not blocking yet; tracked debt to clear before this gate becomes blocking." -f $Clause, $What)
        Write-Host ("##vso[task.complete result=SucceededWithIssues;]{0} advisory: issues found" -f $Clause)
    }
}

exit 0
