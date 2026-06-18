#requires -Version 7.4

Describe 'Check-DocEmDashes' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Check-DocEmDashes.ps1'

        function Invoke-Gate {
            param([Parameter(Mandatory)][string]$RepoRoot)
            $out = & $script:ScriptUnderTest -RepoRoot $RepoRoot *>&1 | Out-String
            return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
        }
    }

    BeforeEach {
        $script:Root = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path (Join-Path $script:Root 'docs') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $script:Root 'docs-functional') -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $script:Root 'AGENTS.md') -Value "# Agents`nClean content with a spaced hyphen - like this.`n" -NoNewline
    }

    It 'passes when no em-dash is present (exit 0)' {
        Set-Content -LiteralPath (Join-Path $script:Root 'docs/page.md') -Value "# Page`nPlain prose, commas, and parentheses only.`n" -NoNewline
        $r = Invoke-Gate -RepoRoot $script:Root
        $r.ExitCode | Should -Be 0
        $r.Output | Should -Match 'Em-dash OK'
    }

    It 'fails when a doc contains an em-dash (exit 1)' {
        $emDash = [char]0x2014
        Set-Content -LiteralPath (Join-Path $script:Root 'docs/page.md') -Value "# Page`nThis sentence $emDash has an em-dash.`n" -NoNewline
        $r = Invoke-Gate -RepoRoot $script:Root
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'em-dash'
    }

    It 'fails when AGENTS.md contains an em-dash (exit 1)' {
        $emDash = [char]0x2014
        Set-Content -LiteralPath (Join-Path $script:Root 'AGENTS.md') -Value "# Agents`nBroken $emDash here.`n" -NoNewline
        $r = Invoke-Gate -RepoRoot $script:Root
        $r.ExitCode | Should -Be 1
    }

    It 'fails when docs-functional/ contains an em-dash (exit 1)' {
        $emDash = [char]0x2014
        Set-Content -LiteralPath (Join-Path $script:Root 'docs/page.md') -Value "# Page`nClean prose only.`n" -NoNewline
        Set-Content -LiteralPath (Join-Path $script:Root 'docs-functional/behaviour.md') -Value "# Behaviour`nThis $emDash is functional.`n" -NoNewline
        $r = Invoke-Gate -RepoRoot $script:Root
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'em-dash'
    }

    It 'reports the count when more than one em-dash line is found (exit 1)' {
        $emDash = [char]0x2014
        Set-Content -LiteralPath (Join-Path $script:Root 'docs/page.md') -Value "# Page`nFirst $emDash offending line.`nSecond $emDash offending line.`n" -NoNewline
        $r = Invoke-Gate -RepoRoot $script:Root
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'Found 2 line\(s\) containing an em-dash\.'
    }

    It 'scans .markdown files as well as .md (exit 1)' {
        $emDash = [char]0x2014
        Set-Content -LiteralPath (Join-Path $script:Root 'docs/page.md') -Value "# Page`nClean prose only.`n" -NoNewline
        Set-Content -LiteralPath (Join-Path $script:Root 'docs/legacy.markdown') -Value "# Legacy`nThis $emDash is in a .markdown file.`n" -NoNewline
        $r = Invoke-Gate -RepoRoot $script:Root
        $r.ExitCode | Should -Be 1
        $r.Output | Should -Match 'em-dash'
    }

    It 'does not flag a spaced hyphen or hyphen-minus (exit 0)' {
        Set-Content -LiteralPath (Join-Path $script:Root 'docs/page.md') -Value "# Page`nThis uses a spaced hyphen - and a hyphen-minus, not an em-dash.`n" -NoNewline
        $r = Invoke-Gate -RepoRoot $script:Root
        $r.ExitCode | Should -Be 0
        $r.Output | Should -Match 'Em-dash OK'
    }
}
