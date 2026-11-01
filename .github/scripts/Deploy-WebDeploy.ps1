<#
.SYNOPSIS
    Deploys the published site to SmarterASP.NET with Web Deploy, without touching App_Data.

.DESCRIPTION
    App_Data on the server holds everything the owner edits in the admin page: content, photos, messages,
    settings and backups. The skip rules below make Web Deploy leave it alone, so a deploy can never
    overwrite or delete it. The logs folder is skipped for the same reason.

    Connection values come from the WEBDEPLOY_* environment variables set by the workflow.
    With -WhatIf, Web Deploy connects, signs in and lists what it would change, but changes nothing.
#>
param(
    [Parameter(Mandatory)]
    [string] $PublishDirectory,

    [switch] $WhatIf
)

$ErrorActionPreference = 'Stop'

foreach ($name in 'WEBDEPLOY_URL', 'WEBDEPLOY_SITE', 'WEBDEPLOY_USERNAME', 'WEBDEPLOY_PASSWORD') {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "$name is not set. The password goes in the WEBDEPLOY_PASSWORD secret under Settings > Secrets and variables > Actions."
    }
}

$msdeploy = 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe'
if (-not (Test-Path -LiteralPath $msdeploy)) {
    Write-Host 'Web Deploy is not installed on this runner. Installing it.'
    choco install webdeploy --yes --no-progress | Out-Host
    if (-not (Test-Path -LiteralPath $msdeploy)) { throw "Web Deploy did not install to $msdeploy." }
}

$source = (Resolve-Path -LiteralPath $PublishDirectory).Path
$dest = @(
    "contentPath=""$env:WEBDEPLOY_SITE"""
    "computerName=""$env:WEBDEPLOY_URL"""
    "userName=""$env:WEBDEPLOY_USERNAME"""
    "password=""$env:WEBDEPLOY_PASSWORD"""
    'authType="Basic"'
) -join ','

$arguments = @(
    '-verb:sync'
    "-source:contentPath=""$source"""
    "-dest:$dest"
    '-enableRule:AppOffline'
    '-skip:objectName=dirPath,absolutePath="\\App_Data$"'
    '-skip:objectName=filePath,absolutePath="\\App_Data\\"'
    '-skip:objectName=dirPath,absolutePath="\\logs$"'
    '-skip:objectName=filePath,absolutePath="\\logs\\"'
    '-retryAttempts:3'
    '-retryInterval:3000'
)
if ($WhatIf) { $arguments += '-whatif' }

# msdeploy reads its raw command line, and PowerShell re-quotes arguments that contain quotes.
# Starting it through ProcessStartInfo passes this exact string, so quotes and password symbols arrive unchanged.
$startInfo = [System.Diagnostics.ProcessStartInfo]::new($msdeploy, ($arguments -join ' '))
$startInfo.UseShellExecute = $false
$process = [System.Diagnostics.Process]::Start($startInfo)
$process.WaitForExit()

if ($process.ExitCode -ne 0) {
    throw "Web Deploy failed with exit code $($process.ExitCode)."
}
Write-Host ($WhatIf ? 'Dry run finished: Web Deploy connected and listed the changes above. Nothing was changed.' : 'Deploy finished.')
