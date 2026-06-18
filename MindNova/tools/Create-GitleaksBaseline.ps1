<#
.SYNOPSIS
    Create a gitleaks baseline file for the current repository.
.DESCRIPTION
    This script creates a gitleaks baseline file for the current repository using the specified gitleaks configuration file.
    The script clones a fresh copy of the repository to a temporary location, runs gitleaks to generate the baseline file, and then cleans up the temporary files.
.NOTES
    This script requires Git and Gitleaks to be installed and available in the system PATH.
.LINK
    https://gitleaks.io/
    https://github.com/gitleaks/gitleaks
    https://github.com/JoostVoskuil/azure-devops-gitleaks
    https://github.com/secure-software-engineering/gitleaks

.EXAMPLE
    MindNova\tools\Create-GitleaksBaseline.ps1 
    Uses the default gitleaks configuration file from Microsoft Security Devops rules and the original gitleaks rules. 
    (https://raw.githubusercontent.com/JoostVoskuil/azure-devops-gitleaks/refs/heads/main/task/v3/configs/GitleaksUdmCombo.toml)
.EXAMPLE
    MindNova\tools\Create-GitleaksBaseline.ps1 -gitleaksConfiguration 'UDMSecretChecksv8'
    Uses the gitleaks configuration file that contains only the Microsoft Security Devops rules (CSCAN*)
.EXAMPLE
    MindNova\tools\Create-GitleaksBaseline.ps1 -gitleaksConfiguration 'GitleaksOriginal'
    Uses the gitleaks configuration file that contains only the original gitleaks rules in the master branch.
.EXAMPLE
    MindNova\tools\Create-GitleaksBaseline.ps1 -gitleaksconfigurationUrl 'https://raw.githubusercontent.com/gitleaks/gitleaks/refs/tags/v8.28.0/config/gitleaks.toml'
    Uses the gitleaks configuration file from the specified URL. In this example it uses the tag v8.28.0 from the original gitleaks repository.
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param (
    # Name of the baseline file to create in the current directory
    [Parameter()]
    [string]
    $BaselineFileName = 'gitleaks-baseline.json',


    # Gitleaks configuration to use, default is a combination of Microsoft Security Devops rules and original gitleaks rules
    [ValidateSet("GitleaksUdmCombo", "UDMSecretChecksv8", "GitleaksOriginal")]
    [Parameter(ParameterSetName = 'Default')]
    [string]
    $gitleaksConfiguration = 'GitleaksUdmCombo',

    # URL of the gitleaks configuration file to use. If specified, this overrides the gitleaksConfiguration parameter.
    # [ValidatePattern('^https?://.*\.toml$')]
    # [Parameter(ParameterSetName = 'CustomUrl', Mandatory = $true)]
    # [string]
    # $gitleaksconfigurationUrl, # = 'https://raw.githubusercontent.com/JoostVoskuil/azure-devops-gitleaks/refs/heads/main/task/v3/configs/GitleaksUdmCombo.toml',

    # Temporary directory for cloning the repository
    [Parameter()]
    [string]
    $tempDrive = 'd:\temp\',

    # Name of the temporary directory for cloning the repository
    [Parameter()]
    [string]
    $tempDirectoryName = 'gitleaksbaseline'

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

if (!(Test-Path $tempDrive)) {
    New-Item -Path $tempDrive -ItemType Directory
}


$remoteUrl = git config --get remote.origin.url
Write-Host "Remote URL: $remoteUrl"

$currentDirectory = Get-Location

$gitleaksBaselinePath = "$currentDirectory/$BaselineFileName"
$gitleakconfigurationFile = 'gitleaks.toml'

Write-Host "Current directory: $currentDirectory"
Write-Host "Downloading $gitleakconfigurationFile config file"
Invoke-WebRequest -Uri $gitleaksconfigurationUrl -Method get  -OutFile $gitleakconfigurationFile

if (!(Test-Path $gitleakconfigurationFile)) {
    Write-Error "Failed to download gitleaks configuration file from $gitleaksconfigurationUrl"
    exit 1
}

Push-Location
Write-Host "Create temp directory for cloning MindNova repo"
$tempDirectory = "$tempDrive\$tempDirectoryName"
if (Test-Path $tempDirectory) {
    Remove-Item -Path $tempDirectory -Recurse -Force
}   
New-Item -Path $tempDirectory -ItemType Directory

try {
    
    
    Write-Host " Change to temp directory: $tempDirectory"
    Set-Location -Path $tempDirectory

    Write-Host "Clone fresh copy of MindNova repo to temp directory"
    git clone $remoteUrl

    Write-Host "Running gitleaks to create baseline file: $gitleaksBaselinePath"
    gitleaks git $currentDirectory --config "$currentDirectory/$gitleakconfigurationFile" --report-format=json --redact --report-path "$gitleaksBaselinePath" --exit-code=99
}
catch {
    Write-Error "An error occurred: $_"
    exit 1
}
finally {
    Pop-Location
}


Write-Host "Remove temp directory and config file"
if (Test-Path $tempDirectory) {
    Remove-Item -Path $tempDirectory -Recurse -Force -ProgressAction SilentlyContinue
}  
if(Test-Path $gitleakconfigurationFile) {
    Remove-Item -Path $gitleakconfigurationFile -Force -ProgressAction SilentlyContinue
}

Write-Host "Sort gitleaks baseline file on Fingerprint"
$baseline = get-content $gitleaksBaselinePath | convertfrom-json
$baseline | sort-object -property Fingerprint | convertto-json | out-file $gitleaksBaselinePath -Force

Write-Host "Gitleaks baseline file created at: $gitleaksBaselinePath"
