<#
.SYNOPSIS
    Checks the deployed site answers: the home page loads and /health reports the storage can be written.

.DESCRIPTION
    The site can take a minute to start after a deploy, so this retries for up to two minutes.
    Email reporting "Degraded" is fine (no mail server set yet); storage must be Healthy.
    On failure it prints what the server sent back (status, sign-in request, start of the page),
    so a hosting error page or login prompt shows in the run log.
#>
param(
    [Parameter(Mandatory)]
    [string] $SiteUrl
)

$ErrorActionPreference = 'Stop'
$SiteUrl = $SiteUrl.TrimEnd('/')
$deadline = (Get-Date).AddMinutes(2)
$lastError = ''

function Get-StorageStatus($response) {
    try { return ($response.Content | ConvertFrom-Json).checks.storage.status }
    catch { return $null }
}

function Format-Response($response) {
    $text = ($response.Content -replace '<[^>]+>', ' ' -replace '\s+', ' ').Trim()
    if ($text.Length -gt 400) { $text = $text.Substring(0, 400) + '...' }
    $signIn = $response.Headers['WWW-Authenticate']
    $signInText = $signIn ? " Asks for sign-in: $signIn." : ''
    return "$($response.StatusCode).$signInText Server: $($response.Headers['Server']). Page: $text"
}

while ((Get-Date) -lt $deadline) {
    try {
        $homePage = Invoke-WebRequest -Uri "$SiteUrl/" -TimeoutSec 30 -SkipHttpErrorCheck
        $health = Invoke-WebRequest -Uri "$SiteUrl/health" -TimeoutSec 30 -SkipHttpErrorCheck
        if ($homePage.StatusCode -eq 200 -and (Get-StorageStatus $health) -eq 'Healthy') {
            Write-Host "The site is up. Health: $($health.Content)"
            exit 0
        }
        $lastError = "`nHome page: $(Format-Response $homePage)`nHealth: $(Format-Response $health)"
    }
    catch {
        $lastError = $_.Exception.Message
    }
    Start-Sleep -Seconds 10
}

throw "The site did not come up within two minutes. Last result: $lastError"
