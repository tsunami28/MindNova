#Requires -Version 7
<#
.SYNOPSIS
    Best-effort enforcement of constitution clause C04: validate and EscapeKqlString user
    input before interpolating it into Azure Resource Graph (ARG) KQL.

.DESCRIPTION
    Scans application C# under MindNova/src/ for string literals that look like ARG KQL (they
    contain KQL pipe operators such as "| where" / "| project" / "| extend") and interpolate a
    bare local/parameter that has NOT been routed through EscapeKqlString. Such a value can
    inject KQL if upstream validation is bypassed, which is exactly what C04 forbids
    ("escape values with EscapeKqlString ... even when upstream validation exists").

    HOW IT DECIDES (the heuristic, deliberately conservative):
      1. A line is considered "KQL-looking" when it (or the multi-line string literal it
         belongs to) contains a KQL pipe operator. A small rolling window of recent lines is
         tracked so interpolations on a continuation line of a raw/interpolated string still
         count as inside KQL.
      2. Within a KQL-looking line, every interpolation hole is inspected:
           - `$"...{x}..."`  (regular interpolated string)
           - `$$"""...{{x}}..."""` (raw interpolated string; holes use double braces)
      3. A hole is FLAGGED only when its inner expression is a "bare value" that is NOT
         demonstrably safe. Safe (NOT flagged) means any of:
           - the expression calls EscapeKqlString(...) directly;
           - it references a local assigned from EscapeKqlString(...) earlier in the same file
             (e.g. `var safeSubscriptionId = EscapeKqlString(subscriptionId);`);
           - it is a compile-time constant / literal-ish identifier: a `const`/`static readonly`
             field, PascalCase identifier (convention: constants/fields like VirtualWanName),
             or member access (anything containing a '.', e.g. `_options.Name`, `resourceId`
             rendered via ToString, `properties.x`);
           - it is a known validated/typed shape: a `ResourceIdentifier` or any identifier whose
             name ends in `ResourceId`/`Id` (ARM resource ids are validated by construction),
             a `Guid`, or an enum-ish call.
         A hole is therefore flagged essentially only for a lower-camelCase *plain string
         variable* (e.g. `subscriptionId`, `purpose`, `id`) interpolated straight into KQL with
         no EscapeKqlString and no validating type.

    LIMITS (read before trusting a clean run):
      - This is a TEXTUAL heuristic, not a type checker. It cannot see whether a plain string
        parameter was validated by the caller, nor whether a "safe-looking" name is actually
        safe. It will MISS injection if the unsafe value arrives via a member access, a helper
        that is not named EscapeKqlString, or a string built up across statements.
      - It can produce FALSE POSITIVES when a lower-camelCase string is in fact a constant, an
        already-validated value, or interpolated into a non-KQL string that merely contains a
        pipe-like substring.
      - It only understands EscapeKqlString by name. A differently named escaper (or one in
        another file) is not recognised.
      - Scope is ARG KQL under MindNova/src/. Log Analytics / App Insights KQL (e.g.
        QueryWorkspaceAsync, `app(...).traces`) is a different sink and is not the target of
        C04; such files may still contain unescaped interpolation that this gate ignores.

    Because of these limits this script is intended as an ADVISORY signal that points a human
    reviewer at suspicious interpolations, not as a sound proof of C04 compliance. See the
    return guidance in the task / docs/constitution.md (clause C04).

.PARAMETER RepoRoot
    Repository root that contains MindNova/src/. Defaults to two levels above this script.

.EXAMPLE
    pwsh ./MindNova/tools/Check-KqlEscaping.ps1
    Scans the repo and exits non-zero if any suspicious unescaped KQL interpolation is found.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
)

$ErrorActionPreference = 'Stop'

# Files that DEFINE / legitimately discuss EscapeKqlString or are otherwise exempt.
$allowList = @()

# KQL pipe operators that mark a line (or its enclosing string literal) as KQL-looking.
$kqlOperatorPattern = '\|\s*(where|project(-away|-rename|-keep|-reorder)?|extend|summarize|mv-expand|mvexpand|join|order\s+by|sort\s+by|parse|parse-where|take|limit|distinct|count|union|make-series|top|sample|render|search|evaluate|lookup|partition|serialize)\b'

# ARG table roots. A string literal is only treated as ARG KQL (C04 scope) when it both uses a
# KQL operator AND opens from one of these tables. This is what keeps the gate on Azure Resource
# Graph and OFF Log Analytics / App Insights KQL (which starts from Heartbeat, traces, app(...),
# etc.) - a different sink that C04 does not govern. Every ARG query in this repo starts here.
$argTableRootPattern = '(?im)^\s*(resources|resourcecontainers|resourcechanges|dnsresources|securityresources|advisorresources|policyresources|guestconfigurationresources|patchassessmentresources|healthresources|extendedlocationresources)\b'

# An interpolation expression is treated as SAFE (not flagged) when it matches any of these.
# Deliberately narrow: we only exempt things that are clearly not a bare unescaped string
# variable. We do NOT exempt by an `Id`/`Identifier` name suffix, because a `string`
# parameter named subscriptionId/resourceId looks identical to a validated one textually, and
# C04 requires EscapeKqlString at the call site regardless of the value's apparent type.
$safeExpressionPatterns = @(
    'EscapeKqlString',          # escaped at the call site, e.g. {EscapeKqlString(x)} / {{EscapeKqlString(x)}}
    '\.',                       # member access: _options.Name, peering.id, x.ToString() (not a bare local)
    '\bnameof\s*\(',            # nameof(...) renders a compile-time literal
    '[Gg]uid\b',                # explicit Guid in the expression
    '[Ee]num\b'                 # explicit enum parse/value in the expression
)

# PascalCase opener => const / static readonly field convention (e.g. VirtualWanName). Checked
# case-SENSITIVELY on purpose: PowerShell -match is case-insensitive, which would wrongly treat
# a lower-camelCase string variable (subscriptionId, id) as a constant and defeat the gate.
$pascalCaseConstPattern = '^[A-Z][A-Za-z0-9_]*$'

function Test-IsKqlOperatorLine {
    param([string]$Line)
    return [bool]([regex]::IsMatch($Line, $kqlOperatorPattern, 'IgnoreCase'))
}

function Get-InterpolationHoles {
    # Returns the inner expressions of interpolation holes on a single line for a string literal
    # of the given interpolation ARITY. C# sets the hole delimiter by the number of leading '$':
    #   $"...{x}..."        -> arity 1, hole is a single brace pair {x}
    #   $$"""...{{x}}..."""  -> arity 2, hole is a double brace pair {{x}}  (single {n} is literal)
    # Matching the arity is what keeps KQL regex quantifiers like {36} or {1,3} (literal text in a
    # $$""" raw string) from being mistaken for interpolation. A non-interpolated literal (arity 0)
    # has no holes.
    param(
        [string]$Line,
        [int]$Arity
    )

    $holes = [System.Collections.Generic.List[string]]::new()
    if ($Arity -le 0) { return $holes }

    $open = ('{' * $Arity)
    $close = ('}' * $Arity)
    # Match exactly $Arity braces: not preceded/followed by another brace, inner has no braces.
    $pattern = "(?<!\{)$([regex]::Escape($open))(?!\{)\s*([^{}]+?)\s*(?<!\})$([regex]::Escape($close))(?!\})"
    foreach ($m in [regex]::Matches($Line, $pattern)) {
        $holes.Add($m.Groups[1].Value)
    }

    return $holes
}

function Get-StringLiteralArity {
    # Returns the interpolation arity of a string opener on this line: the count of leading '$'
    # before the opening quote ($"->1, $$"""->2). Returns -1 when no '$'-prefixed opener is on
    # the line, which includes a $-less literal (const string q = """...""") - correct, since such
    # a literal has no interpolation holes and the callers treat arity <= 0 as "no holes".
    param([string]$Line)

    $m = [regex]::Match($Line, '(\$+)\s*@?"')
    if ($m.Success) { return $m.Groups[1].Value.Length }
    return -1
}

function Test-IsSafeExpression {
    param(
        [string]$Expression,
        [System.Collections.Generic.HashSet[string]]$SafeLocals
    )

    $expr = $Expression.Trim()

    # A local previously assigned from EscapeKqlString(...) is safe.
    if ($SafeLocals.Contains($expr)) { return $true }

    foreach ($p in $safeExpressionPatterns) {
        if ($expr -match $p) { return $true }
    }

    # Case-sensitive: a PascalCase bare identifier is a constant/field, not a string variable.
    if ($expr -cmatch $pascalCaseConstPattern) { return $true }

    return $false
}

$srcDir = Join-Path $RepoRoot 'MindNova' 'src'
$targets = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
if (Test-Path $srcDir) {
    Get-ChildItem -Path $srcDir -Recurse -Filter *.cs -File | ForEach-Object { $targets.Add($_) }
}

$violations = [System.Collections.Generic.List[object]]::new()

foreach ($file in $targets) {
    $rel = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/')
    if ($allowList -contains $rel) { continue }

    # Locals whose assignment expression contains EscapeKqlString(...) are trusted by name. This
    # covers `var x = EscapeKqlString(v);` and a KQL fragment built with it inside a multi-line
    # ternary, e.g.  var clause = cond ? "" : $"... {EscapeKqlString(v)} ...";  (the value the
    # local carries is already escaped). The assignment expression is read across continuation
    # lines until the terminating ';' so the EscapeKqlString call is seen even when it is not on
    # the first line of the assignment.
    $safeLocals = [System.Collections.Generic.HashSet[string]]::new()
    $lines = @(Get-Content -LiteralPath $file.FullName)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $assign = [regex]::Match($lines[$i], '\b(?:var\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$')
        if (-not $assign.Success) { continue }

        $name = $assign.Groups[1].Value
        $rhs = $assign.Groups[2].Value
        $j = $i
        # Gather continuation lines until a ';' terminates the statement (bounded to avoid runaway).
        while ($rhs -notmatch ';' -and ($j - $i) -lt 12 -and ($j + 1) -lt $lines.Count) {
            $j++
            $rhs += "`n" + $lines[$j]
        }
        if ($rhs -match 'EscapeKqlString\s*\(') { [void]$safeLocals.Add($name) }
    }

    # Walk the file tracking the currently-open string literal. A multi-line raw literal ("""...)
    # is buffered until its closing """; on close we decide whether the whole block was
    # KQL-looking (any buffered line matched a KQL operator) and, if so, scan its holes at the
    # literal's interpolation arity. Single-line interpolated strings (e.g. an inline ternary
    # `| where id =~ '{id}'`) are evaluated immediately. Buffering the literal is what bounds the
    # scan to one query and stops KQL braces from leaking into unrelated code.
    $inRawLiteral = $false
    $rawArity = -1
    $rawIsKql = $false
    $rawBuffer = [System.Collections.Generic.List[object]]::new()
    $rawText = [System.Text.StringBuilder]::new()
    $n = 0

    foreach ($line in $lines) {
        $n++

        if ($inRawLiteral) {
            $rawBuffer.Add([pscustomobject]@{ Line = $n; Text = $line })
            [void]$rawText.AppendLine($line)
            if (Test-IsKqlOperatorLine $line) { $rawIsKql = $true }

            if ($line -match '"""') {
                $isArg = $rawText.ToString() -match $argTableRootPattern
                if ($rawIsKql -and $isArg) {
                    foreach ($entry in $rawBuffer) {
                        foreach ($hole in (Get-InterpolationHoles -Line $entry.Text -Arity $rawArity)) {
                            if (-not (Test-IsSafeExpression -Expression $hole -SafeLocals $safeLocals)) {
                                $violations.Add([pscustomobject]@{
                                    File = $rel; Line = $entry.Line; Expr = $hole.Trim(); Text = $entry.Text.Trim()
                                })
                            }
                        }
                    }
                }
                $inRawLiteral = $false
                $rawArity = -1
                $rawIsKql = $false
                $rawBuffer.Clear()
                [void]$rawText.Clear()
            }
            continue
        }

        # Does a raw multi-line literal OPEN on this line (""" present, not also closed later on
        # the same line)? Count """ occurrences: an odd count leaves the literal open.
        $tripleMatches = [regex]::Matches($line, '"""')
        $opensRaw = ($tripleMatches.Count % 2) -eq 1

        if ($opensRaw) {
            $inRawLiteral = $true
            $rawArity = Get-StringLiteralArity $line
            $rawIsKql = (Test-IsKqlOperatorLine $line)
            $rawBuffer = [System.Collections.Generic.List[object]]::new()
            $rawBuffer.Add([pscustomobject]@{ Line = $n; Text = $line })
            $rawText = [System.Text.StringBuilder]::new()
            [void]$rawText.AppendLine($line)
            continue
        }

        # Single-line interpolated string on this line: evaluate immediately when it is both
        # KQL-looking and rooted at an ARG table (keeps Log Analytics one-liners out of scope).
        if ((Test-IsKqlOperatorLine $line) -and ($line -match '\$') -and ($line -match $argTableRootPattern)) {
            $arity = Get-StringLiteralArity $line
            foreach ($hole in (Get-InterpolationHoles -Line $line -Arity $arity)) {
                if (-not (Test-IsSafeExpression -Expression $hole -SafeLocals $safeLocals)) {
                    $violations.Add([pscustomobject]@{
                        File = $rel; Line = $n; Expr = $hole.Trim(); Text = $line.Trim()
                    })
                }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "C04 (advisory): possible unescaped user input interpolated into ARG KQL." -ForegroundColor Yellow
    Write-Host "Route the value through EscapeKqlString(...) before interpolating, even when the caller validates it (see docs/constitution.md, clause C04)." -ForegroundColor Yellow
    foreach ($v in $violations) {
        Write-Host ("  {0}:{1}  [{{{2}}}]  {3}" -f $v.File, $v.Line, $v.Expr, $v.Text)
    }
    Write-Host ("Found {0} suspicious KQL interpolation(s). This heuristic is approximate - verify each against the source." -f $violations.Count) -ForegroundColor Yellow
    exit 1
}

Write-Host "C04 OK: no unescaped plain-string interpolations into ARG KQL detected under MindNova/src/."
exit 0
