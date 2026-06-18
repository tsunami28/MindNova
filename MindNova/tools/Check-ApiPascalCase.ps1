#Requires -Version 7
<#
.SYNOPSIS
    Enforces constitution clause C06: the JSON wire format is PascalCase.

.DESCRIPTION
    Two static checks, both run from the repository source - no build, no running service.

    Part A - PascalCase wire names (the C06 violation):
        Scans .cs files under MindNova/src/ for [JsonPropertyName("value")] attributes whose
        serialized value is snake_case or camelCase, i.e. NOT PascalCase. System.Text.Json
        serialises by property name (PascalCase) by default, so a [JsonPropertyName] that
        downgrades the wire name to snake_case or camelCase is the accidental slip C06 forbids.

        An allowlist covers the legitimate reasons a wire name must differ from the property:
          - CosmosDB system fields:        id, ttl, _etag, _ts
          - other leading-underscore names (_*): treated as system/document metadata
          - dotted paths (a.b.c):          external projection / partition-key style keys
          - all-lowercase single words:    deliberate matches to an external contract
                                           (Slack: ok/ts/error; OpsGenie: message/tags/source).
                                           These cannot arise from an accidental PascalCase
                                           property being serialised verbatim, so they are not
                                           treated as C06 violations.
        Everything else that is snake_case or camelCase is flagged. Files under the external-integration
        libraries (Libraries.Network, Libraries.Cmdb) are skipped entirely: their DTOs mirror third-party
        API wire names (IPAM/SOLIDserver, ServiceNow CMDB), which C06 does not govern.

    Part B - spec well-formedness:
        If specs/ contains *.openapi.yaml or *.yaml files, each must parse as YAML and every
        schema property name under components.schemas.*.properties must be PascalCase. If specs/
        is absent or empty, this part is a no-op and reports "no specs yet".

    OUT OF SCOPE - code-vs-spec drift:
        This script does NOT verify that an OpenAPI document matches the wire format the running
        API actually emits. True drift detection requires generating the OpenAPI from the build
        (Swagger/Swashbuckle) and diffing it against the committed spec; that is a separate,
        larger pipeline step. Do not assume this static check covers drift - it does not.

    Sibling of Check-DocLineCitations.ps1 (clause C02) and Check-DocEmDashes.ps1 (clause C11):
    same shape (param/help/exit codes, "file:line text" output) so the CI gate treats them alike.

.PARAMETER RepoRoot
    Repository root that contains MindNova/src/ and (optionally) specs/. Defaults to two levels
    above this script.

.EXAMPLE
    pwsh MindNova/tools/Check-ApiPascalCase.ps1
    Runs both checks against the repository and exits 0 (clean) or 1 (violations found).
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

$exactAllowList = @('id', 'ttl', '_etag', '_ts')

# External-integration libraries whose DTOs deserialize third-party APIs (IPAM/SOLIDserver, ServiceNow
# CMDB); their wire names are dictated by those APIs, not our own surface, so C06 does not apply.
# Excluded wholesale here; a future inline opt-out marker on individual DTOs would be more precise.
$externalIntegrationPaths = @('MindNova/src/Libraries.Network/', 'MindNova/src/Libraries.Cmdb/')

function Test-IsPascalCase {
    param([string]$Name)
    return $Name -cmatch '^[A-Z][A-Za-z0-9]*$'
}

function Get-WireNameClassification {
    param([string]$Name)

    if ($exactAllowList -contains $Name) { return 'allow-system' }
    if ($Name.StartsWith('_')) { return 'allow-system' }
    if ($Name.Contains('.')) { return 'allow-path' }
    if (Test-IsPascalCase $Name) { return 'pascal' }
    if ($Name -cmatch '^[a-z0-9]+$') { return 'allow-lowercase' }
    if ($Name.Contains('_')) { return 'snake_case' }
    if ($Name -cmatch '^[a-z][A-Za-z0-9]*$' -and $Name -cmatch '[A-Z]') { return 'camelCase' }
    return 'other'
}

# --- Part A: PascalCase wire names under MindNova/src/ ---

$srcDir = Join-Path $RepoRoot 'MindNova' 'src'
$attrRegex = '\[JsonPropertyName\("(?<value>[^"]*)"\)\]'

$violations = [System.Collections.Generic.List[object]]::new()
$allowed = [System.Collections.Generic.List[object]]::new()

if (Test-Path $srcDir) {
    $csFiles = Get-ChildItem -Path $srcDir -Recurse -Filter *.cs -File
    foreach ($file in $csFiles) {
        $rel = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/')
        $skipFile = $false
        foreach ($ex in $externalIntegrationPaths) { if ($rel.StartsWith($ex)) { $skipFile = $true; break } }
        if ($skipFile) { continue }
        $n = 0
        foreach ($line in Get-Content -LiteralPath $file.FullName) {
            $n++
            foreach ($m in [regex]::Matches($line, $attrRegex)) {
                $value = $m.Groups['value'].Value
                $class = Get-WireNameClassification $value
                $record = [pscustomobject]@{ File = $rel; Line = $n; Value = $value; Class = $class; Text = $line.Trim() }
                switch ($class) {
                    'snake_case' { $violations.Add($record) }
                    'camelCase' { $violations.Add($record) }
                    'other' { $violations.Add($record) }
                    default { $allowed.Add($record) }
                }
            }
        }
    }
}
else {
    Write-Host ("Note: source directory not found at {0}; skipping Part A." -f $srcDir) -ForegroundColor Yellow
}

# --- Part B: spec well-formedness under specs/ ---

$specsDir = Join-Path $RepoRoot 'specs'
$specViolations = [System.Collections.Generic.List[object]]::new()
$specFiles = @()
if (Test-Path $specsDir) {
    $specFiles = @(Get-ChildItem -Path $specsDir -Recurse -Include *.openapi.yaml, *.yaml, *.yml -File)
}

$yamlAvailable = $null -ne (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue)

if ($specFiles.Count -gt 0) {
    if (-not $yamlAvailable) {
        Write-Host "Spec check needs a YAML parser (ConvertFrom-Yaml, e.g. the 'powershell-yaml' module); it is not installed." -ForegroundColor Yellow
        Write-Host "Install with: Install-Module powershell-yaml -Scope CurrentUser" -ForegroundColor Yellow
        $specViolations.Add([pscustomobject]@{ File = '(specs)'; Detail = 'YAML parser unavailable; could not validate spec property names' })
    }
    else {
        foreach ($file in $specFiles) {
            $rel = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/')
            $doc = $null
            try {
                $raw = Get-Content -LiteralPath $file.FullName -Raw
                $doc = ConvertFrom-Yaml $raw
            }
            catch {
                $specViolations.Add([pscustomobject]@{ File = $rel; Detail = ("does not parse as YAML: {0}" -f $_.Exception.Message) })
                continue
            }

            $schemas = $doc.components.schemas
            if ($null -eq $schemas) { continue }
            foreach ($schemaName in $schemas.Keys) {
                $props = $schemas[$schemaName].properties
                if ($null -eq $props) { continue }
                foreach ($propName in $props.Keys) {
                    if (-not (Test-IsPascalCase $propName)) {
                        $specViolations.Add([pscustomobject]@{
                                File   = $rel
                                Detail = ("components.schemas.{0}.properties.{1} is not PascalCase" -f $schemaName, $propName)
                            })
                    }
                }
            }
        }
    }
}

# --- Report ---

$failed = $false

Write-Host "C06 - JSON wire format is PascalCase" -ForegroundColor Cyan

if ($allowed.Count -gt 0) {
    Write-Host ("Part A: {0} allowlisted [JsonPropertyName] value(s) (system field, leading-underscore, dotted path, or external lowercase contract) - not flagged:" -f $allowed.Count)
    foreach ($a in ($allowed | Sort-Object Value, File)) {
        Write-Host ("  [{0}] {1}:{2}  {3}" -f $a.Class, $a.File, $a.Line, $a.Value)
    }
}

if ($violations.Count -gt 0) {
    $failed = $true
    Write-Host "Part A FAILED: [JsonPropertyName] values that are not PascalCase (C06 violation)." -ForegroundColor Red
    Write-Host "Remove the attribute (let System.Text.Json use the PascalCase property name) or justify a genuine wire-name difference (see docs/conventions/api.md, clause C06)." -ForegroundColor Red
    foreach ($v in ($violations | Sort-Object File, Line)) {
        Write-Host ("  {0}:{1}  [{2}] {3}" -f $v.File, $v.Line, $v.Class, $v.Text)
    }
    Write-Host ("Found {0} non-PascalCase wire name(s)." -f $violations.Count) -ForegroundColor Red
}
else {
    Write-Host "Part A OK: every non-allowlisted [JsonPropertyName] under MindNova/src/ is PascalCase."
}

if ($specFiles.Count -eq 0) {
    Write-Host "Part B: no specs yet (specs/ is absent or contains no YAML); skipping spec well-formedness check."
}
elseif ($specViolations.Count -gt 0) {
    $failed = $true
    Write-Host ("Part B FAILED: {0} spec issue(s) under specs/." -f $specViolations.Count) -ForegroundColor Red
    foreach ($s in $specViolations) {
        Write-Host ("  {0}  {1}" -f $s.File, $s.Detail) -ForegroundColor Red
    }
}
else {
    Write-Host ("Part B OK: {0} spec file(s) parsed and all components.schemas.*.properties names are PascalCase." -f $specFiles.Count)
}

if ($failed) { exit 1 }

Write-Host "C06 OK: wire-name and spec checks passed."
exit 0
