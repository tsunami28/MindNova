#requires -Version 7.4

Describe 'Check-ApiPascalCase' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Check-ApiPascalCase.ps1'
        $script:YamlAvailable = $null -ne (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue)

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

        function New-SpecFile {
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

    It 'passes PascalCase wire names (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova/Dto.cs' -Content @'
public class Dto
{
    [JsonPropertyName("CustomerName")]
    public string CustomerName { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C06 OK'
    }

    It 'passes when an allowlisted system field wire name is used (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova.Data/Document.cs' -Content @'
public class Document
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C06 OK'
    }

    It 'fails on a snake_case wire name (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova/Dto.cs' -Content @'
public class Dto
{
    [JsonPropertyName("customer_name")]
    public string CustomerName { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C06 violation'
        $result.Output | Should -Match 'customer_name'
    }

    It 'fails on a camelCase wire name (exit 1)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova/Dto.cs' -Content @'
public class Dto
{
    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C06 violation'
        $result.Output | Should -Match 'customerName'
    }

    It 'passes an exact-allowlist system field other than id (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova.Data/Document.cs' -Content @'
public class Document
{
    [JsonPropertyName("_etag")]
    public string Etag { get; set; }

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C06 OK'
    }

    It 'passes a leading-underscore wire name as system metadata (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova.Data/Document.cs' -Content @'
public class Document
{
    [JsonPropertyName("_self")]
    public string Self { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C06 OK'
    }

    It 'passes a dotted JSON path wire name (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova/Projection.cs' -Content @'
public class Projection
{
    [JsonPropertyName("customer.cloudSpace.id")]
    public string Key { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C06 OK'
    }

    It 'passes an all-lowercase external-contract wire name (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.Communication/SlackResponse.cs' -Content @'
public class SlackResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C06 OK'
    }

    It 'reports no specs yet when specs/ is absent (exit 0)' {
        New-CsFile -RepoRoot $script:Root -RelativePath 'MindNova/src/Libraries.MindNova/Dto.cs' -Content @'
public class Dto
{
    [JsonPropertyName("CustomerName")]
    public string CustomerName { get; set; }
}
'@

        $result = Invoke-Gate -RepoRoot $script:Root

        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C06 OK'
    }
}
