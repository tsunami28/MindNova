#Requires -Version 7
<#
.SYNOPSIS
    Enforces constitution clause C05: log via _logger.LogMindNova(...), not standard ILogger extensions.

.DESCRIPTION
    Scans application C# under MindNova/src/ for calls to the standard ILogger extension methods
    (LogInformation/LogWarning/LogError/LogDebug/LogTrace/LogCritical). MindNova code must log
    through LogMindNova(...) so every entry carries the [App][Class][Method] envelope and structured
    scope. LogMindNova(...) itself is allowed. Exits non-zero (fails the build) when any banned call
    is found.

    One file is allow-listed because it DEFINES the logging helpers rather than violates the rule:
    Libraries.MindNova/Utilities/Logging/ILoggerExtensions.cs. Test projects (MindNova/tests/) are not
    scanned because they only ever live under MindNova/src/ here.

.PARAMETER RepoRoot
    Repository root that contains MindNova/src/. Defaults to two levels above this script.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

$allowList = @(
    'MindNova/src/Libraries.MindNova/Utilities/Logging/ILoggerExtensions.cs'
)

$pattern = '\.Log(Information|Warning|Error|Debug|Trace|Critical)\s*\('

$srcDir = Join-Path $RepoRoot 'MindNova' 'src'
$targets = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
if (Test-Path $srcDir) { Get-ChildItem -Path $srcDir -Recurse -Filter *.cs -File | ForEach-Object { $targets.Add($_) } }

$violations = [System.Collections.Generic.List[object]]::new()
foreach ($file in $targets) {
    $rel = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/')
    if ($allowList -contains $rel) { continue }
    $n = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $n++
        if ($line -match $pattern) {
            $violations.Add([pscustomobject]@{ File = $rel; Line = $n; Text = $line.Trim() })
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "C05 violation: log via _logger.LogMindNova(...), not the standard ILogger extension methods." -ForegroundColor Red
    Write-Host "Replace LogInformation/LogWarning/LogError/LogDebug/LogTrace/LogCritical with LogMindNova (see docs/constitution.md, clause C05)." -ForegroundColor Red
    foreach ($v in $violations) { Write-Host ("  {0}:{1}  {2}" -f $v.File, $v.Line, $v.Text) }
    Write-Host ("Found {0} banned ILogger call(s)." -f $violations.Count) -ForegroundColor Red
    exit 1
}

Write-Host "C05 OK: no banned ILogger extension calls under MindNova/src/."
exit 0
