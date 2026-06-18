<#
.SYNOPSIS
    Runs gitleaks using an existing baseline file to report only new findings.

.DESCRIPTION
    This script runs gitleaks using a baseline file that represents a known state of secrets
    in your repository. The baseline file is useful for:
    - Suppressing known/accepted findings in existing code
    - Focusing on new secrets introduced in future commits
    - Integrating gitleaks into CI/CD pipelines without breaking builds for historical issues
    
    The script downloads a specified gitleaks configuration (TOML format), runs gitleaks against the 
    current repository, and uses the baseline JSON file you specify to suppress known findings.

.PARAMETER BaselineFileName
    Specifies the name of the baseline file to use in the current directory.
    The file must already exist.
    Default: 'gitleaks-baseline.json'

.PARAMETER gitleaksConfiguration
    Specifies which predefined gitleaks configuration to use:
    - 'GitleaksUdmCombo' (Default): Combines Microsoft Security DevOps rules with original gitleaks rules
    - 'UDMSecretChecksv8': Uses only Microsoft Security DevOps rules (CSCAN* rules)
    - 'GitleaksOriginal': Uses only the original gitleaks rules from the master branch

.NOTES
    Prerequisites:
    - Git must be installed and available in the system PATH
    - Gitleaks must be installed and available in the system PATH
    
    Installation:
    - Git: https://git-scm.com/downloads
    - Gitleaks: https://github.com/gitleaks/gitleaks#installing
    
    The script will:
    1. Validate Git and Gitleaks are available
    2. Download the specified configuration file
    3. Run gitleaks against the repository using the baseline file
    4. Report only new findings not present in the baseline
    5. Clean up temporary configuration files

.LINK
    https://gitleaks.io/

.LINK
    https://github.com/gitleaks/gitleaks

.LINK
    https://github.com/JoostVoskuil/azure-devops-gitleaks

.EXAMPLE
    MindNova\tools\Test-GitleaksBaseline.ps1
    
    Runs gitleaks using the default 'GitleaksUdmCombo' configuration and the
    default baseline file name 'gitleaks-baseline.json'.

.EXAMPLE
    MindNova\tools\Test-GitleaksBaseline.ps1 -gitleaksConfiguration 'UDMSecretChecksv8'
    
    Runs gitleaks using only Microsoft Security DevOps rules (CSCAN* rules) and the
    baseline file 'gitleaks-baseline.json'.

.EXAMPLE
    MindNova\tools\Test-GitleaksBaseline.ps1 -gitleaksConfiguration 'GitleaksOriginal'
    
    Runs gitleaks using only the original gitleaks rules from the master branch and
    the baseline file 'gitleaks-baseline.json'.

.EXAMPLE
    MindNova\tools\Test-GitleaksBaseline.ps1 -BaselineFileName 'custom-baseline.json'
    
    Runs gitleaks using a custom baseline filename instead of the default 'gitleaks-baseline.json'.
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param (
    [Parameter()]
    [string]
    $BaselineFileName = 'gitleaks-baseline.json',
    
    [ValidateSet("GitleaksUdmCombo", "UDMSecretChecksv8", "GitleaksOriginal")]
    [Parameter(ParameterSetName = 'Default')]
    [string]
    $gitleaksConfiguration = 'GitleaksUdmCombo'
)

if ($PSCmdlet.ParameterSetName -eq 'Default') {
    switch ($gitleaksConfiguration) {
        'GitleaksUdmCombo' {
            $gitleaksconfigurationUrl = 'https://raw.githubusercontent.com/JoostVoskuil/azure-devops-gitleaks/refs/heads/main/task/v3/configs/GitleaksUdmCombo.toml'
        }
        'UDMSecretChecksv8' {
            $gitleaksconfigurationUrl = 'https://raw.githubusercontent.com/JoostVoskuil/azure-devops-gitleaks/refs/heads/main/task/v3/configs/UDMSecretChecksv8.toml'
        }
        'GitleaksOriginal' {
            $gitleaksconfigurationUrl = 'https://raw.githubusercontent.com/gitleaks/gitleaks/refs/heads/master/config/gitleaks.toml'
        }
        Default {
            Write-Error "Invalid gitleaks configuration specified. Valid options are: GitleaksUdmCombo, UDMSecretChecksv8, GitleaksOriginal"
            exit 1
        }
    }

}

# Ensure git is available
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "Git is not installed or not available in the system PATH. Please install Git and try again."
    exit 1
}

if (-not (Get-Command gitleaks -ErrorAction SilentlyContinue)) {
    Write-Error "Gitleaks is not installed or not available in the system PATH. Please install Gitleaks and try again."
    exit 1
}

$gitleakconfigurationFile = 'gitleaks.toml'
$currentDirectory = Get-Location

Write-Host "Current directory: $currentDirectory"
Write-Host "Downloading $gitleakconfigurationFile config file"
Invoke-WebRequest -Uri $gitleaksconfigurationUrl -Method get  -OutFile $gitleakconfigurationFile

if (!(Test-Path $gitleakconfigurationFile)) {
    Write-Error "Failed to download gitleaks configuration file from $gitleaksconfigurationUrl"
    exit 1
}


try {
   
    Write-Host "Running gitleaks against the current repository using baseline file: $BaselineFileName"
    gitleaks detect --source="$currentDirectory" --baseline-path "$BaselineFileName" --config "$gitleakconfigurationFile" --redact --exit-code=99
}
catch {
    Write-Error "An error occurred: $_"
    exit 1
}
finally {
}

Remove-Item -Path $gitleakconfigurationFile -ErrorAction SilentlyContinue
$gitleaksBaselinePath = Join-Path -Path $currentDirectory -ChildPath $BaselineFileName

Write-Host "Gitleaks scan completed using baseline file: $gitleaksBaselinePath"
