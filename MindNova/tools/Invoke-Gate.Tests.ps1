#requires -Version 7.4

Describe 'Invoke-Gate' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Invoke-Gate.ps1'

        function New-FakeGate {
            param(
                [Parameter(Mandatory)][string]$Root,
                [Parameter(Mandatory)][int]$ExitCode,
                [string]$Message = 'gate ran'
            )
            $path = Join-Path $Root 'FakeGate.ps1'
            Set-Content -LiteralPath $path -Value "Write-Host '$Message'; exit $ExitCode" -NoNewline
            return $path
        }

        function Invoke-Wrapper {
            param(
                [Parameter(Mandatory)][string]$Gate,
                [Parameter(Mandatory)][string]$Mode
            )
            $out = & $script:ScriptUnderTest -Script $Gate -Clause 'C99' -What 'a test rule' -Mode $Mode *>&1 | Out-String
            return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
        }
    }

    BeforeEach {
        $script:Root = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Root -Force | Out-Null
    }

    It 'passes through a clean gate (exit 0, no annotation) in Advisory mode' {
        $gate = New-FakeGate -Root $script:Root -ExitCode 0
        $r = Invoke-Wrapper -Gate $gate -Mode 'Advisory'
        $r.ExitCode | Should -Be 0
        $r.Output | Should -Match 'gate ran'
        $r.Output | Should -Not -Match 'task.logissue'
    }

    It 'passes through a clean gate (exit 0, no annotation) in Blocking mode' {
        $gate = New-FakeGate -Root $script:Root -ExitCode 0
        $r = Invoke-Wrapper -Gate $gate -Mode 'Blocking'
        $r.ExitCode | Should -Be 0
        $r.Output | Should -Not -Match 'task.logissue'
    }

    It 'turns a non-zero gate into a warning + SucceededWithIssues in Advisory mode (wrapper exits 0)' {
        $gate = New-FakeGate -Root $script:Root -ExitCode 1
        $r = Invoke-Wrapper -Gate $gate -Mode 'Advisory'
        $r.ExitCode | Should -Be 0
        $r.Output | Should -Match '##vso\[task.logissue type=warning\]'
        $r.Output | Should -Match '##vso\[task.complete result=SucceededWithIssues;\]'
    }

    It 'turns a non-zero gate into an error + Failed in Blocking mode (wrapper exits 0)' {
        $gate = New-FakeGate -Root $script:Root -ExitCode 1
        $r = Invoke-Wrapper -Gate $gate -Mode 'Blocking'
        $r.ExitCode | Should -Be 0
        $r.Output | Should -Match '##vso\[task.logissue type=error\]'
        $r.Output | Should -Match '##vso\[task.complete result=Failed;\]'
    }

    It 'includes the clause and label in the annotation' {
        $gate = New-FakeGate -Root $script:Root -ExitCode 1
        $r = Invoke-Wrapper -Gate $gate -Mode 'Blocking'
        $r.Output | Should -Match 'C99'
        $r.Output | Should -Match 'a test rule'
    }

    It 'passes through named args and switches to the gate by name' {
        $gate = Join-Path $script:Root 'ArgGate.ps1'
        Set-Content -LiteralPath $gate -Value @'
param([string]$RepoRoot, [switch]$Check)
Write-Host "ROOT=[$RepoRoot] CHECK=[$Check]"
exit 0
'@ -NoNewline
        $out = & $script:ScriptUnderTest -Script $gate -Clause 'C99' -What 'passthrough' -Mode 'Blocking' -RepoRoot 'C:\some\path' -Check *>&1 | Out-String
        $LASTEXITCODE | Should -Be 0
        $out | Should -Match 'ROOT=\[C:\\some\\path\]'
        $out | Should -Match 'CHECK=\[True\]'
        $out | Should -Not -Match 'positional parameter cannot be found'
    }
}
