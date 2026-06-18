#requires -Version 7.4

Describe 'Check-KqlEscaping' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Check-KqlEscaping.ps1'

        function Invoke-Gate {
            param([Parameter(Mandatory)][string]$RepoRoot)
            $out = & $script:ScriptUnderTest -RepoRoot $RepoRoot *>&1 | Out-String
            return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
        }

        function New-CsFile {
            param(
                [Parameter(Mandatory)][string]$Root,
                [Parameter(Mandatory)][string]$Name,
                [Parameter(Mandatory)][string]$Content
            )
            $srcDir = Join-Path -Path $Root -ChildPath 'MindNova/src'
            New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
            $path = Join-Path -Path $srcDir -ChildPath $Name
            Set-Content -LiteralPath $path -Value $Content -Encoding utf8
            return $path
        }
    }

    BeforeEach {
        $script:Root = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Root -Force | Out-Null
    }

    It 'passes when there are no .cs files at all (exit 0)' {
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C04 OK'
    }

    It 'passes safe KQL usage routed through EscapeKqlString (exit 0)' {
        $content = @'
public class SafeQuery
{
    public string Build(string subscriptionId)
    {
        var safe = EscapeKqlString(subscriptionId);
        var query = $$"""
            resources
            | where subscriptionId =~ '{{safe}}'
            | project id, name
            """;
        return query;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'SafeQuery.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C04 OK'
    }

    It 'fails on unescaped bare interpolation into ARG KQL (exit 1)' {
        $content = @'
public class UnsafeQuery
{
    public string Build(string subscriptionId)
    {
        var query = $$"""
            resources
            | where subscriptionId =~ '{{subscriptionId}}'
            | project id, name
            """;
        return query;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'UnsafeQuery.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C04'
        $result.Output | Should -Match 'EscapeKqlString'
    }

    It 'passes when EscapeKqlString is called directly inside the interpolation hole (exit 0)' {
        $content = @'
public class InlineEscapeQuery
{
    public string Build(string subscriptionId)
    {
        var query = $$"""
            resources
            | where subscriptionId =~ '{{EscapeKqlString(subscriptionId)}}'
            | project id, name
            """;
        return query;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'InlineEscapeQuery.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C04 OK'
    }

    It 'ignores KQL that is not rooted at an ARG table (Log Analytics, exit 0)' {
        $content = @'
public class LogAnalyticsQuery
{
    public string Build(string subscriptionId)
    {
        var query = $$"""
            traces
            | where subscriptionId =~ '{{subscriptionId}}'
            | project timestamp, message
            """;
        return query;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'LogAnalyticsQuery.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C04 OK'
    }

    It 'does not flag interpolation in a non-KQL string that merely contains a pipe (exit 0)' {
        $content = @'
public class PipeText
{
    public string Build(string purpose)
    {
        var label = $$"""
            display | {{purpose}} | summary text
            """;
        return label;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'PipeText.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C04 OK'
    }

    It 'does not flag a PascalCase constant interpolated into ARG KQL (exit 0)' {
        $content = @'
public class ConstQuery
{
    public string Build()
    {
        var query = $$"""
            resources
            | where name =~ '{{VirtualWanName}}'
            | project id, name
            """;
        return query;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'ConstQuery.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C04 OK'
    }

    It 'does not flag a member-access expression interpolated into ARG KQL (exit 0)' {
        $content = @'
public class MemberAccessQuery
{
    public string Build()
    {
        var query = $$"""
            resources
            | where name =~ '{{_options.Name}}'
            | project id, name
            """;
        return query;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'MemberAccessQuery.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C04 OK'
    }

    It 'emits the suspicious-count summary and violation guidance on a violation (exit 1)' {
        $content = @'
public class CountQuery
{
    public string Build(string purpose)
    {
        var query = $$"""
            resources
            | where purpose =~ '{{purpose}}'
            | project id, name
            """;
        return query;
    }
}
'@
        New-CsFile -Root $script:Root -Name 'CountQuery.cs' -Content $content

        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'possible unescaped user input interpolated into ARG KQL'
        $result.Output | Should -Match 'Route the value through EscapeKqlString'
        $result.Output | Should -Match 'Found 1 suspicious KQL interpolation'
    }
}
