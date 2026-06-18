#Requires -Version 7
<#
.SYNOPSIS
    Layer 0 of the skill eval harness: static hygiene gate for the AI-SDLC skills.

.DESCRIPTION
    Validates every skill under .claude/skills/ against the structural and house-style rules the
    skill ecosystem relies on, so a skill cannot silently drift. For each skill folder containing a
    SKILL.md it checks:

      - name=folder           the frontmatter `name:` matches the folder name (how skills are invoked).
      - trigger-rich desc     the description carries triggering language ("Use when/whenever/after/...").
      - persona line          a persona / "Usable by" / "Invoked as" line is present.
      - Next-steps section     a `## Next steps` section is present.
      - no em-dash            no U+2014 (house style, same rule as Check-DocEmDashes.ps1).
      - no line-citations     no `file.ext:NN` or "line NN" source citations (clause C02 patterns,
                              same as Check-DocLineCitations.ps1).
      - cross-refs resolve    every `/<phase>-<step>` reference points at a real skill folder.

    This is the deterministic, CI-gateable layer; the routing (Layer 1) and value (Layer 2) layers
    are subagent-driven and documented in the governance-validate-skills skill, which calls this
    script for Layer 0. Exits non-zero (fails the build) when any skill violates a rule.

.PARAMETER RepoRoot
    Repository root that contains .claude/skills/. Defaults to two levels above this script.

.PARAMETER SkillsRoot
    The skills directory to scan. Defaults to <RepoRoot>/.claude/skills. Override for testing.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path,
    [string]$SkillsRoot
)

$ErrorActionPreference = 'Stop'

if (-not $SkillsRoot) { $SkillsRoot = Join-Path $RepoRoot '.claude' 'skills' }

$emDash = [char]0x2014
$phasePrefixes = 'create|discovery|elaboration|governance|mature|run'
$lineCitationPatterns = @(
    '\.(?:cs|ts|tsx|razor|bicep|yml|yaml|json|ps1|js|css|sln|csproj):\d+',
    '\b[Ll]ines?\s+\d+'
)

if (-not (Test-Path $SkillsRoot)) {
    Write-Host "Skill hygiene: skills directory not found at $SkillsRoot." -ForegroundColor Red
    exit 1
}

$skillDirs = Get-ChildItem -Path $SkillsRoot -Directory | Sort-Object Name
$skillNames = $skillDirs.Name

$violations = [System.Collections.Generic.List[object]]::new()
function Add-Violation { param($Skill, $Rule, $Detail) $violations.Add([pscustomobject]@{ Skill = $Skill; Rule = $Rule; Detail = $Detail }) }

foreach ($dir in $skillDirs) {
    $name = $dir.Name
    $path = Join-Path $dir.FullName 'SKILL.md'
    if (-not (Test-Path $path)) { Add-Violation $name 'skill-md' 'no SKILL.md in folder'; continue }
    $text = Get-Content -LiteralPath $path -Raw

    $fmName = if ($text -match '(?m)^name:\s*(\S+)') { $matches[1] } else { '' }
    if ($fmName -ne $name) { Add-Violation $name 'name=folder' "frontmatter name '$fmName' does not match folder" }

    if ($text -notmatch 'Use (when|whenever|after|once|during|this)') { Add-Violation $name 'trigger-rich-desc' 'description lacks triggering language (Use when/whenever/after/...)' }
    if ($text -notmatch 'persona|Usable by|Invoked as') { Add-Violation $name 'persona-line' 'no persona / Usable by / Invoked as line' }
    if ($text -notmatch '(?m)^##\s*Next steps') { Add-Violation $name 'next-steps' 'no "## Next steps" section' }
    if ($text.Contains($emDash)) { Add-Violation $name 'em-dash' 'contains an em-dash (U+2014)' }

    foreach ($p in $lineCitationPatterns) {
        if ($text -match $p) { Add-Violation $name 'line-citation' "matches source line-citation pattern ($p)"; break }
    }

    $badRefs = [System.Collections.Generic.List[string]]::new()
    foreach ($m in [regex]::Matches($text, "/($phasePrefixes)-[a-z-]+")) {
        $tok = $m.Value.TrimStart('/')
        if ($skillNames -notcontains $tok -and -not $badRefs.Contains($tok)) { $badRefs.Add($tok) }
    }
    if ($badRefs.Count -gt 0) { Add-Violation $name 'cross-ref' ("unresolved skill reference(s): " + ($badRefs -join ', ')) }
}

if ($violations.Count -gt 0) {
    Write-Host "Skill hygiene violation: one or more skills under .claude/skills/ break a structure or house-style rule." -ForegroundColor Red
    foreach ($v in $violations) { Write-Host ("  {0}  [{1}]  {2}" -f $v.Skill, $v.Rule, $v.Detail) }
    Write-Host ("Found {0} violation(s) across {1} skill(s)." -f $violations.Count, $skillDirs.Count) -ForegroundColor Red
    exit 1
}

Write-Host ("Skill hygiene OK: {0} skill(s) pass (name=folder, trigger-rich description, persona line, Next-steps, no em-dash, no line-citations, cross-refs resolve)." -f $skillDirs.Count)
exit 0
