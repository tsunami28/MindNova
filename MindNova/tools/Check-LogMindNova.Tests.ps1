#requires -Version 7.4

Describe 'Check-LogMindNova' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Check-LogMindNova.ps1'
        function Invoke-Gate {
            param([Parameter(Mandatory)][string]$RepoRoot)
            $out = & $script:ScriptUnderTest -RepoRoot $RepoRoot *>&1 | Out-String
            return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
        }
        function New-CsFile {
            param(
                [Parameter(Mandatory)][string]$RepoRoot,
                [Parameter(Mandatory)][string]$RelativePath,
                [Parameter(Mandatory)][string]$Content
            )
            $full = Join-Path -Path $RepoRoot -ChildPath $RelativePath
            $dir = Split-Path -Path $full -Parent
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Set-Content -LiteralPath $full -Value $Content -Encoding UTF8
        }
    }
    BeforeEach {
        $script:Root = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path (Join-Path -Path $script:Root -ChildPath 'MindNova/src') -Force | Out-Null
    }

    It 'passes clean source that logs via LogMindNova (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Api.MindNova/Foo.cs' -Content @'
namespace Api.MindNova
{
    public class Foo
    {
        public void Bar()
        {
            _logger.LogMindNova("hello from Foo.Bar");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C05 OK'
    }

    It 'passes source with no logging at all (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova/Plain.cs' -Content @'
namespace Libraries.MindNova
{
    public class Plain
    {
        public int Add(int a, int b) => a + b;
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C05 OK'
    }

    It 'passes when MindNova/src exists but contains no .cs files (exit 0)' {
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C05 OK'
    }

    It 'fails on a forbidden LogInformation call (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Api.MindNova/Bad.cs' -Content @'
namespace Api.MindNova
{
    public class Bad
    {
        public void Run()
        {
            _logger.LogInformation("this is banned");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C05 violation'
    }

    It 'fails on a forbidden LogError call (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.Workflow/Worse.cs' -Content @'
namespace Libraries.Workflow
{
    public class Worse
    {
        public void Run()
        {
            _logger.LogError("also banned");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C05 violation'
    }

    It 'fails on a forbidden LogWarning call (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Api.MindNova/Warn.cs' -Content @'
namespace Api.MindNova
{
    public class Warn
    {
        public void Run()
        {
            _logger.LogWarning("banned warning");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C05 violation'
    }

    It 'fails on a forbidden LogDebug call (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Api.MindNova/Dbg.cs' -Content @'
namespace Api.MindNova
{
    public class Dbg
    {
        public void Run()
        {
            _logger.LogDebug("banned debug");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C05 violation'
    }

    It 'fails on a forbidden LogTrace call (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Api.MindNova/Trc.cs' -Content @'
namespace Api.MindNova
{
    public class Trc
    {
        public void Run()
        {
            _logger.LogTrace("banned trace");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C05 violation'
    }

    It 'fails on a forbidden LogCritical call (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Api.MindNova/Crit.cs' -Content @'
namespace Api.MindNova
{
    public class Crit
    {
        public void Run()
        {
            _logger.LogCritical("banned critical");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C05 violation'
    }

    It 'does not flag the allow-listed ILoggerExtensions.cs that defines the helpers (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova/Utilities/Logging/ILoggerExtensions.cs' -Content @'
namespace Libraries.MindNova.Utilities.Logging
{
    public static class ILoggerExtensions
    {
        public static void LogMindNova(this ILogger logger, string message)
        {
            logger.LogInformation(message);
            logger.LogWarning(message);
            logger.LogError(message);
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C05 OK'
    }

    It 'reports each violation as file:line and a total count' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Api.MindNova/Multi.cs' -Content @'
namespace Api.MindNova
{
    public class Multi
    {
        public void Run()
        {
            _logger.LogInformation("one");
            _logger.LogError("two");
        }
    }
}
'@
        $result = Invoke-Gate -RepoRoot $script:Root
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'Api\.MindNova/Multi\.cs:7'
        $result.Output | Should -Match 'Api\.MindNova/Multi\.cs:8'
        $result.Output | Should -Match 'Found 2 banned ILogger call\(s\)\.'
    }
}
