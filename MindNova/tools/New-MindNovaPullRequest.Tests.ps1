#requires -Version 7.4

Describe 'New-MindNovaPullRequest' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'New-MindNovaPullRequest.ps1'

        function Invoke-Helper {
            param([hashtable]$Parameters)
            return (& $script:ScriptUnderTest @Parameters)
        }
    }

    It 'creates a PR and returns the read-back object on success' {
        $description = "## Summary`n`nbody line one`nbody line two"
        $invoker = {
            param($call)
            if ($call.Command -eq 'create') {
                return 'https://github.com/owner/repo/pull/42'
            }
            return ('{"number":42,"url":"https://github.com/owner/repo/pull/42","body":"## Summary\n\nbody line one\nbody line two"}')
        }.GetNewClosure()

        $pr = Invoke-Helper -Parameters @{
            Title = 'T'; Description = $description
            SourceBranch = 'MN-10'; GhInvoker = $invoker
        }

        $pr.number | Should -Be 42
        $pr.body | Should -BeLike '*body line*'
    }

    It 'refuses an empty or whitespace-only description' {
        $invoker = { param($call) return '{}' }
        { Invoke-Helper -Parameters @{
                Title = 'T'; Description = "   `n  "
                SourceBranch = 'MN-10'; GhInvoker = $invoker
            } } | Should -Throw '*empty or whitespace-only*'
    }

    It 'throws when PR number cannot be extracted from gh output' {
        $invoker = {
            param($call)
            if ($call.Command -eq 'create') { return 'unexpected output' }
            return '{}'
        }.GetNewClosure()

        { Invoke-Helper -Parameters @{
                Title = 'T'; Description = '## Body'
                SourceBranch = 'MN-10'; GhInvoker = $invoker
            } } | Should -Throw '*Could not extract PR number*'
    }

    It 'throws when the read-back body is empty' {
        $invoker = {
            param($call)
            if ($call.Command -eq 'create') {
                return 'https://github.com/owner/repo/pull/99'
            }
            return '{"number":99,"url":"https://github.com/owner/repo/pull/99","body":""}'
        }.GetNewClosure()

        { Invoke-Helper -Parameters @{
                Title = 'T'; Description = '## Real body'
                SourceBranch = 'MN-10'; GhInvoker = $invoker
            } } | Should -Throw '*description is empty on read-back*'
    }

    It 'passes --head when SourceBranch is provided' {
        $state = @{ CreateArgs = $null }
        $invoker = {
            param($call)
            if ($call.Command -eq 'create') {
                $state.CreateArgs = $call.Args
                return 'https://github.com/owner/repo/pull/7'
            }
            return '{"number":7,"url":"https://github.com/owner/repo/pull/7","body":"## OK"}'
        }.GetNewClosure()

        Invoke-Helper -Parameters @{
            Title = 'T'; Description = '## OK'
            SourceBranch = 'feature-branch'; GhInvoker = $invoker
        } | Out-Null

        $state.CreateArgs | Should -Contain '--head'
        $state.CreateArgs | Should -Contain 'feature-branch'
    }
}
