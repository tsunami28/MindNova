#requires -Version 7.4

Describe 'Check-ArtifactGraph' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Check-ArtifactGraph.ps1'

        function Invoke-Gate {
            param([Parameter(Mandatory)][string]$RepoRoot)
            $out = & $script:ScriptUnderTest -RepoRoot $RepoRoot *>&1 | Out-String
            return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
        }

        function New-Artifact {
            param(
                [Parameter(Mandatory)][string]$RepoRoot,
                [Parameter(Mandatory)][string]$RelativePath,
                [Parameter(Mandatory)][string]$FrontMatter
            )
            $full = Join-Path -Path $RepoRoot -ChildPath $RelativePath
            $dir = Split-Path -Path $full -Parent
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Set-Content -LiteralPath $full -Value $FrontMatter -NoNewline
            return $full
        }
    }

    BeforeEach {
        $script:Root = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path (Join-Path $script:Root 'docs') -Force | Out-Null
    }

    It 'passes a clean fixture (exit 0)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/target.md' -FrontMatter @'
---
story: AZURE-1234
phase: discovery
---

# Target
'@
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/source.md' -FrontMatter @'
---
story: AZURE-1234
phase: create
relates:
  spec: docs/target.md
  ticket: https://[PLACEHOLDER].atlassian.net/browse/AZURE-1234
---

# Source
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'Artifact graph OK:'
    }

    It 'ignores artifacts without story front-matter (exit 0)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/plain.md' -FrontMatter @'
---
title: No story here
phase: discovery
---

# Plain
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'Artifact graph OK:'
    }

    It 'fails on an invalid work-item key (exit 1)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/badstory.md' -FrontMatter @'
---
story: not-a-key
phase: discovery
---

# Bad story
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'is not a valid work-item key'
    }

    It 'fails when phase is missing (exit 1)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/nophase.md' -FrontMatter @'
---
story: AZURE-1234
---

# No phase
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match "missing 'phase'"
    }

    It 'fails on an unknown phase (exit 1)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/badphase.md' -FrontMatter @'
---
story: AZURE-1234
phase: unknownphase
---

# Bad phase
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'is not one of:'
    }

    It 'fails on an unresolved relates path (exit 1)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/badrelates.md' -FrontMatter @'
---
story: AZURE-1234
phase: create
relates:
  spec: docs/does-not-exist.md
---

# Bad relates
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'does not resolve to a file in the repo'
    }

    It 'passes when no graph artifacts exist at all (exit 0)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/notes.md' -FrontMatter @'
# Just a heading, no front-matter at all
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'Artifact graph OK:'
    }

    It 'rejects a lowercase story key case-sensitively (exit 1)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/lowerkey.md' -FrontMatter @'
---
story: azure-1234
phase: discovery
---

# Lowercase key
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'is not a valid work-item key'
    }

    It 'passes when a relates repo path resolves (exit 0)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/relatestarget.md' -FrontMatter @'
---
story: AZURE-1234
phase: discovery
---

# Relates target
'@
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/relatessource.md' -FrontMatter @'
---
story: AZURE-1234
phase: create
relates:
  spec: docs/relatestarget.md
---

# Relates source
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'Artifact graph OK:'
    }

    It 'treats an external URL relates value as presence-only (exit 0)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/urlrelates.md' -FrontMatter @'
---
story: AZURE-1234
phase: create
relates:
  ticket: https://[PLACEHOLDER].atlassian.net/browse/AZURE-1234
---

# URL relates
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'Artifact graph OK:'
    }

    It 'reports multiple violations together (exit 1)' {
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/multi1.md' -FrontMatter @'
---
story: not-a-key
phase: discovery
---

# First bad
'@
        New-Artifact -RepoRoot $script:Root -RelativePath 'docs/multi2.md' -FrontMatter @'
---
story: AZURE-1234
phase: unknownphase
---

# Second bad
'@

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'is not a valid work-item key'
        $result.Output | Should -Match 'is not one of:'
        $result.Output | Should -Match '2 violation\(s\)\.'
    }
}
