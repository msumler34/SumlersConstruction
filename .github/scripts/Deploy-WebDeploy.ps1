<#
.SYNOPSIS
    Deploys the published site to SmarterASP.NET with Web Deploy, without touching App_Data.

.DESCRIPTION
    App_Data on the server holds everything the owner edits in the admin page: content, photos, messages,
    settings and backups. The skip rules below make Web Deploy leave it alone, so a deploy can never
    overwrite or delete it. The logs folder is skipped for the same reason.

    Connection values come from the WEBDEPLOY_* environment variables set by the workflow.
#>
param(
    [Parameter(Mandatory)]
    [string] $PublishDirectory
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

$env:DEPLOY_SOURCE = (Resolve-Path -LiteralPath $PublishDirectory).Path

# --% passes the rest of the line to msdeploy unchanged, with only %VAR% expanded. That keeps the
# password's symbols (commas, pipes, quotes) from being reinterpreted by PowerShell.
& $msdeploy --% -verb:sync -source:contentPath="%DEPLOY_SOURCE%" -dest:contentPath="%WEBDEPLOY_SITE%",computerName="%WEBDEPLOY_URL%",userName="%WEBDEPLOY_USERNAME%",password="%WEBDEPLOY_PASSWORD%",authType="Basic" -enableRule:AppOffline -skip:objectName=dirPath,absolutePath="\\App_Data$" -skip:objectName=filePath,absolutePath="\\App_Data\\" -skip:objectName=dirPath,absolutePath="\\logs$" -skip:objectName=filePath,absolutePath="\\logs\\" -retryAttempts:3 -retryInterval:3000

if ($LASTEXITCODE -ne 0) {
    throw "Web Deploy failed with exit code $LASTEXITCODE."
}
Write-Host 'Deploy finished.'
