#requires -Version 7.4

Describe 'Check-Coverage' {
    BeforeAll {
        $script:ScriptUnderTest = Join-Path -Path $PSScriptRoot -ChildPath 'Check-Coverage.ps1'

        function Invoke-Gate {
            param([hashtable]$GateArgs)
            $out = & $script:ScriptUnderTest @GateArgs *>&1 | Out-String
            return [pscustomobject]@{ Output = $out; ExitCode = $LASTEXITCODE }
        }

        function New-OpenCoverReport {
            param(
                [Parameter(Mandatory)][string]$Path,
                [Parameter(Mandatory)][int]$NumSequencePoints,
                [Parameter(Mandatory)][int]$VisitedSequencePoints
            )
            $xml = @"
<?xml version="1.0" encoding="utf-8"?>
<CoverageSession>
  <Summary numSequencePoints="$NumSequencePoints" visitedSequencePoints="$VisitedSequencePoints" />
  <Modules>
    <Module>
      <ModuleName>Libraries.Workflow</ModuleName>
      <Summary numSequencePoints="$NumSequencePoints" visitedSequencePoints="$VisitedSequencePoints" />
    </Module>
  </Modules>
</CoverageSession>
"@
            Set-Content -LiteralPath $Path -Value $xml -Encoding utf8
        }

        function New-OpenCoverReportRaw {
            param(
                [Parameter(Mandatory)][string]$Path,
                [Parameter(Mandatory)][string]$Xml
            )
            Set-Content -LiteralPath $Path -Value $Xml -Encoding utf8
        }
    }

    BeforeEach {
        $script:Root = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Root -Force | Out-Null
        $script:CoverageFile = Join-Path -Path $script:Root -ChildPath 'coverage.opencover.xml'
    }

    It 'passes when coverage meets the threshold (exit 0)' {
        New-OpenCoverReport -Path $script:CoverageFile -NumSequencePoints 100 -VisitedSequencePoints 95
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C09 OK'
    }

    It 'fails when coverage is below the threshold (exit 1)' {
        New-OpenCoverReport -Path $script:CoverageFile -NumSequencePoints 100 -VisitedSequencePoints 40
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'C09 coverage gate FAILED'
    }

    It 'fails when the coverage file is missing (exit 1)' {
        $result = Invoke-Gate -GateArgs @{ CoverageFile = (Join-Path $script:Root 'absent.xml'); Threshold = 80 }
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'coverage file not found'
    }

    It 'fails when the report is not an OpenCover document (exit 1)' {
        Set-Content -LiteralPath $script:CoverageFile -Value '<NotCoverage />' -Encoding utf8
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'no <CoverageSession> root'
    }

    It 'fails when the XML is malformed and cannot be parsed (exit 1)' {
        New-OpenCoverReportRaw -Path $script:CoverageFile -Xml '<CoverageSession><Summary numSequencePoints="100" '
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'failed to parse'
    }

    It 'fails when overall coverage is unreadable because numSequencePoints is zero (exit 1)' {
        $xml = @'
<?xml version="1.0" encoding="utf-8"?>
<CoverageSession>
  <Summary numSequencePoints="0" visitedSequencePoints="0" />
  <Modules>
    <Module>
      <ModuleName>Libraries.Workflow</ModuleName>
      <Summary numSequencePoints="0" visitedSequencePoints="0" />
    </Module>
  </Modules>
</CoverageSession>
'@
        New-OpenCoverReportRaw -Path $script:CoverageFile -Xml $xml
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'could not read overall line coverage'
    }

    It 'passes using the sequenceCoverage fallback when count attributes are absent (exit 0)' {
        $xml = @'
<?xml version="1.0" encoding="utf-8"?>
<CoverageSession>
  <Summary sequenceCoverage="91.5" />
  <Modules>
    <Module>
      <ModuleName>Libraries.Workflow</ModuleName>
      <Summary sequenceCoverage="91.5" />
    </Module>
  </Modules>
</CoverageSession>
'@
        New-OpenCoverReportRaw -Path $script:CoverageFile -Xml $xml
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C09 OK'
    }

    It 'renders n/a for a module whose summary is unreadable while overall is readable (exit 0)' {
        $xml = @'
<?xml version="1.0" encoding="utf-8"?>
<CoverageSession>
  <Summary numSequencePoints="100" visitedSequencePoints="95" />
  <Modules>
    <Module>
      <ModuleName>Libraries.Workflow</ModuleName>
      <Summary numSequencePoints="0" visitedSequencePoints="0" />
    </Module>
  </Modules>
</CoverageSession>
'@
        New-OpenCoverReportRaw -Path $script:CoverageFile -Xml $xml
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'n/a'
        $result.Output | Should -Match 'C09 OK'
    }

    It 'passes when coverage is exactly at the threshold because the check is strict less-than (exit 0)' {
        New-OpenCoverReport -Path $script:CoverageFile -NumSequencePoints 100 -VisitedSequencePoints 80
        $result = Invoke-Gate -GateArgs @{ CoverageFile = $script:CoverageFile; Threshold = 80 }
        $result.ExitCode | Should -Be 0
        $result.Output | Should -Match 'C09 OK'
    }
}
