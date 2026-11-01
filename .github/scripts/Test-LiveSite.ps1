<#
.SYNOPSIS
    Checks the deployed site answers: the home page loads and /health reports the storage can be written.

.DESCRIPTION
    The site can take a minute to start after a deploy, so this retries for up to two minutes.
    Email reporting "Degraded" is fine (no mail server set yet); storage must be Healthy.
#>
param(
    [Parameter(Mandatory)]
    [string] $SiteUrl
)

$ErrorActionPreference = 'Stop'
$SiteUrl = $SiteUrl.TrimEnd('/')
$deadline = (Get-Date).AddMinutes(2)
$lastError = ''

while ((Get-Date) -lt $deadline) {
    try {
        $homePage = Invoke-WebRequest -Uri "$SiteUrl/" -TimeoutSec 30 -SkipHttpErrorCheck
        $health = Invoke-WebRequest -Uri "$SiteUrl/health" -TimeoutSec 30 -SkipHttpErrorCheck
        $storage = ($health.Content | ConvertFrom-Json).checks.storage.status
        if ($homePage.StatusCode -eq 200 -and $storage -eq 'Healthy') {
            Write-Host "The site is up. Health: $($health.Content)"
            exit 0
        }
        $lastError = "Home page: $($homePage.StatusCode). Health: $($health.StatusCode) $($health.Content)"
    }
    catch {
        $lastError = $_.Exception.Message
    }
    Start-Sleep -Seconds 10
}

throw "The site did not come up within two minutes. Last result: $lastError"
