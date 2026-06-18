#requires -Version 7.4

Describe 'New-MindNovaPullRequest' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'New-MindNovaPullRequest.ps1'

        function New-PrJson {
            param([int]$Id = 101, [string]$Description, [string]$Url = '[PLACEHOLDER]/[PLACEHOLDER]/_git/Application.MindNova/pullrequest/101')
            return ([ordered]@{ pullRequestId = $Id; url = $Url; description = $Description } | ConvertTo-Json)
        }

        function Get-DescriptionLines {
            param([string[]]$AzArgs)
            $idx = [array]::IndexOf($AzArgs, '--description')
            if ($idx -lt 0) { return @() }
            return $AzArgs[($idx + 1)..($AzArgs.Length - 1)]
        }

        function Invoke-Helper {
            param([hashtable]$Parameters)
            return (& $script:ScriptUnderTest @Parameters)
        }
    }

    It 'splits a multi-line description into separate --description values' {
        $description = "## Summary`n`n* one`n* two"
        $state = @{ CreateArgs = $null }
        $invoker = {
            param($azArgs)
            if ($azArgs[2] -eq 'create') { $state.CreateArgs = $azArgs }
            return ([ordered]@{ pullRequestId = 101; url = 'u'; description = "## Summary`n`n* one`n* two" } | ConvertTo-Json)
        }.GetNewClosure()

        Invoke-Helper -Parameters @{
            Title = 'T'; Description = $description; Repository = 'Application.MindNova'
            SourceBranch = 'AZURE-1'; AzInvoker = $invoker
        } | Out-Null

        $lines = Get-DescriptionLines -AzArgs $state.CreateArgs
        $lines | Should -Be @('## Summary', '', '* one', '* two')
    }

    It 'strips leaked here-string delimiter lines (@) from the start and end of the description' {
        $description = "@`n## Summary`n`n* one`n@"
        $state = @{ CreateArgs = $null }
        $invoker = {
            param($azArgs)
            if ($azArgs[2] -eq 'create') { $state.CreateArgs = $azArgs }
            return ([ordered]@{ pullRequestId = 101; url = 'u'; description = "## Summary`n`n* one" } | ConvertTo-Json)
        }.GetNewClosure()

        Invoke-Helper -Parameters @{
            Title = 'T'; Description = $description; Repository = 'Application.MindNova'
            SourceBranch = 'AZURE-1'; AzInvoker = $invoker
        } | Out-Null

        $lines = Get-DescriptionLines -AzArgs $state.CreateArgs
        $lines | Should -Be @('## Summary', '', '* one')
    }

    It 'throws on a description line that begins with a dash and never calls az' {
        $state = @{ CreateCalled = $false }
        $invoker = {
            param($azArgs)
            if ($azArgs[2] -eq 'create') { $state.CreateCalled = $true }
            return ([ordered]@{ pullRequestId = 1; url = 'u'; description = 'x' } | ConvertTo-Json)
        }.GetNewClosure()

        { Invoke-Helper -Parameters @{
                Title = 'T'; Description = "## Summary`n- bad bullet"; Repository = 'R'
                SourceBranch = 'AZURE-1'; AzInvoker = $invoker
            } } | Should -Throw '*option flag*'

        $state.CreateCalled | Should -BeFalse
    }

    It 'refuses an empty or whitespace-only description' {
        $invoker = { param($azArgs) return '{}' }
        { Invoke-Helper -Parameters @{
                Title = 'T'; Description = "   `n  "; Repository = 'R'
                SourceBranch = 'AZURE-1'; AzInvoker = $invoker
            } } | Should -Throw '*empty or whitespace-only*'
    }

    It 'succeeds and returns the PR when the read-back is intact' {
        $full = "## Summary`n`nbody line one`nbody line two"
        $invoker = {
            param($azArgs)
            return ([ordered]@{ pullRequestId = 202; url = 'https://pr/202'; description = "## Summary`n`nbody line one`nbody line two" } | ConvertTo-Json)
        }.GetNewClosure()

        $pr = Invoke-Helper -Parameters @{
            Title = 'T'; Description = $full; Repository = 'R'
            SourceBranch = 'AZURE-1'; AzInvoker = $invoker
        }

        $pr.pullRequestId | Should -Be 202
        $pr.description | Should -Be $full
    }

    It 'accepts a read-back a few characters shorter than expected from benign normalization' {
        $full = "## Summary`n`nDescription body content AB"
        $state = @{ UpdateCalled = $false }
        $invoker = {
            param($azArgs)
            if ($azArgs[2] -eq 'update') { $state.UpdateCalled = $true }
            return ([ordered]@{ pullRequestId = 505; url = 'u'; description = "## Summary`n`nDescription body content" } | ConvertTo-Json)
        }.GetNewClosure()

        $pr = Invoke-Helper -Parameters @{
            Title = 'T'; Description = $full; Repository = 'R'
            SourceBranch = 'AZURE-1'; AzInvoker = $invoker
        }

        $pr.pullRequestId | Should -Be 505
        $state.UpdateCalled | Should -BeFalse
    }

    It 'issues one corrective update when the first read-back is truncated, then succeeds' {
        $full = "## Summary`n`nfull body content here"
        $state = @{ ShowCount = 0; UpdateCalled = $false }
        $invoker = {
            param($azArgs)
            $verb = $azArgs[2]
            if ($verb -eq 'create') {
                return ([ordered]@{ pullRequestId = 303; url = 'u'; description = '## Summary' } | ConvertTo-Json)
            }
            if ($verb -eq 'update') {
                $state.UpdateCalled = $true
                return ([ordered]@{ pullRequestId = 303; url = 'u'; description = 'ack' } | ConvertTo-Json)
            }
            $state.ShowCount++
            $desc = if ($state.ShowCount -eq 1) { '## Summary' } else { "## Summary`n`nfull body content here" }
            return ([ordered]@{ pullRequestId = 303; url = 'u'; description = $desc } | ConvertTo-Json)
        }.GetNewClosure()

        $pr = Invoke-Helper -Parameters @{
            Title = 'T'; Description = $full; Repository = 'R'
            SourceBranch = 'AZURE-1'; AzInvoker = $invoker
        }

        $state.UpdateCalled | Should -BeTrue
        $pr.description | Should -Be $full
    }

    It 'throws when the description is still truncated after a corrective update' {
        $full = "## Summary`n`na much longer body that should survive"
        $invoker = {
            param($azArgs)
            $verb = $azArgs[2]
            if ($verb -eq 'create') {
                return ([ordered]@{ pullRequestId = 404; url = 'u'; description = '## Summary' } | ConvertTo-Json)
            }
            if ($verb -eq 'update') {
                return ([ordered]@{ pullRequestId = 404; url = 'u'; description = 'ack' } | ConvertTo-Json)
            }
            return ([ordered]@{ pullRequestId = 404; url = 'u'; description = '## Summary' } | ConvertTo-Json)
        }.GetNewClosure()

        { Invoke-Helper -Parameters @{
                Title = 'T'; Description = $full; Repository = 'R'
                SourceBranch = 'AZURE-1'; AzInvoker = $invoker
            } } | Should -Throw '*truncated after a corrective update*'
    }
}
