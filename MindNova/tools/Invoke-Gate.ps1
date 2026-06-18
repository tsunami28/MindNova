#Requires -Version 7
<#
.SYNOPSIS
    Runs a constitution / AI-SDLC gate script in GitHub Actions CI with a readable result.

.DESCRIPTION
    The gate scripts (Check-*.ps1) signal "issues found" with a non-zero exit code. Run directly in a
    pwsh step that surfaces as the opaque "Process completed with exit code 1", which reads like
    the script crashed rather than the validation simply finding issues.

    This wrapper runs the gate, lets its own output (the human-readable violation list) flow to the
    log, and then translates a non-zero exit into a proper GitHub Actions annotation:

      - Advisory mode: a warning annotation (the gate does not block the workflow yet).
      - Blocking mode: an error annotation + non-zero exit (the gate fails the step).

    In advisory mode the wrapper exits 0, so the step passes despite issues. In blocking mode it
    exits 1 so the workflow step fails with a clear message. The gate script is unchanged and still
    exits non-zero on a developer machine.

    A genuine crash in the gate (a thrown, terminating error rather than a clean non-zero exit) still
    propagates and fails the step loudly, so real script bugs are not masked.

.PARAMETER Script
    Path to the gate script to run (e.g. Check-DocEmDashes.ps1).

.PARAMETER Clause
    Constitution clause id or short tag used in the message (e.g. C11, AI-SDLC).

.PARAMETER What
    Short human label used in the message (e.g. "no em-dash characters").

.PARAMETER Mode
    Advisory (default) emits a warning annotation. Blocking emits an error and fails the step.

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
        Write-Host ("::error::{0}: {1} failed (see the log above for the specific findings). This gate is blocking; fix the listed issues to pass CI." -f $Clause, $What)
        exit 1
    }
    else {
        Write-Host ("::warning::{0} advisory: {1} found issue(s) (see the log above). Not blocking yet; tracked debt to clear before this gate becomes blocking." -f $Clause, $What)
    }
}

exit 0
