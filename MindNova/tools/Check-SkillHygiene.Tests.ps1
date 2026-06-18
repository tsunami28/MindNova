#requires -Version 7.4

Describe 'Check-SkillHygiene' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Check-SkillHygiene.ps1'

        function New-SkillFixture {
            param(
                [Parameter(Mandatory)][string]$Root,
                [Parameter(Mandatory)][string]$Folder,
                [string]$Name = $Folder,
                [string]$Description = 'Do a thing. Use when the user says "do the thing".',
                [string]$Body = "Invoked as ``/{0}``. Usable by any persona.`n`n## Next steps`n- none`n",
                [switch]$NoSkillMd
            )
            $dir = Join-Path $Root $Folder
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            if ($NoSkillMd) { return $dir }
            $resolvedBody = $Body -f $Folder
            $content = "---`nname: $Name`ndescription: >`n  $Description`n---`n`n# Title`n`n$resolvedBody"
            Set-Content -LiteralPath (Join-Path $dir 'SKILL.md') -Value $content -NoNewline
            return $dir
        }

        function Invoke-Hygiene {
            param([Parameter(Mandatory)][string]$SkillsRoot)
            $out = & $script:ScriptUnderTest -SkillsRoot $SkillsRoot *>&1 | Out-String
            return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
        }
    }

    BeforeEach {
        $script:Skills = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Skills | Out-Null
    }

    It 'passes a well-formed skill (exit 0)' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 0
        $r.Output | Should -Match 'Skill hygiene OK'
    }

    It 'fails when frontmatter name does not match the folder' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' -Name 'governance-other' | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'name=folder'
    }

    It 'fails when the description lacks triggering language' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' -Description 'Just does a thing with no trigger phrasing.' | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'trigger-rich-desc'
    }

    It 'fails when there is no persona line' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' -Body "Plain body.`n`n## Next steps`n- none`n" | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'persona-line'
    }

    It 'fails when the Next steps section is missing' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' -Body "Invoked as ``/governance-do-thing``. Usable by any persona.`n" | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'next-steps'
    }

    It 'fails on an em-dash' {
        $emDash = [char]0x2014
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' -Description "Do a thing $emDash quickly. Use when asked." | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'em-dash'
    }

    It 'fails on a source line-number citation' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' -Body "Invoked as ``/governance-do-thing``. Usable by any persona. See Program.cs:42.`n`n## Next steps`n- none`n" | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'line-citation'
    }

    It 'fails on an unresolved cross-reference' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-do-thing' -Body "Invoked as ``/governance-do-thing``. Usable by any persona. Then run /governance-does-not-exist.`n`n## Next steps`n- none`n" | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'cross-ref'
    }

    It 'resolves a cross-reference to a sibling skill that exists' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-alpha' | Out-Null
        New-SkillFixture -Root $script:Skills -Folder 'governance-beta' -Body "Invoked as ``/governance-beta``. Usable by any persona. Then run /governance-alpha.`n`n## Next steps`n- none`n" | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 0
    }

    It 'fails when a skill folder has no SKILL.md' {
        New-SkillFixture -Root $script:Skills -Folder 'governance-empty' -NoSkillMd | Out-Null
        $r = Invoke-Hygiene -SkillsRoot $script:Skills
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'skill-md'
    }

    It 'fails when the skills directory does not exist' {
        $missing = Join-Path $script:Skills 'nope'
        $r = Invoke-Hygiene -SkillsRoot $missing
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'not found'
    }
}
