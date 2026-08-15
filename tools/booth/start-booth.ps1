# Launches the photobooth cabin stack natively: the published API (with the Sparkbooth watcher)
# plus `cloudflared` so it's reachable at https://api.somospix.com. This is the production
# counterpart to `docker compose up` - no Docker involved, because the watcher needs to see the
# real Windows filesystem where Sparkbooth saves captures.
#
# One-time setup (see tools/booth/README.md for the full walkthrough):
#   1. Publish the API and copy the output next to this script into .\api\
#      (dotnet publish src/PixDynamicGallery.Api -c Release -o tools/booth/api)
#   2. Install `cloudflared` and make sure it's on PATH (or edit $CloudflaredPath below).
#   3. Copy env.production.example to .env.production and fill in the real secrets.
#
# Usage: double-click start-booth.cmd (recommended - forces the execution policy bypass
# regardless of what "Run with PowerShell" or the machine's policy would otherwise do),
# or from a terminal:
#   powershell -ExecutionPolicy Bypass -File tools\booth\start-booth.ps1

param(
    [string]$ApiPath = (Join-Path $PSScriptRoot "api"),
    [string]$EnvFile = (Join-Path $PSScriptRoot ".env.production"),
    [string]$CloudflaredPath = "cloudflared"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $EnvFile)) {
    throw "Missing $EnvFile - copy env.production.example to .env.production and fill in the real values first."
}

Write-Host "Loading environment from $EnvFile..."
Get-Content $EnvFile | ForEach-Object {
    $line = $_.Trim()
    if ($line -eq "" -or $line.StartsWith("#")) { return }
    $separatorIndex = $line.IndexOf("=")
    if ($separatorIndex -lt 1) { return }
    $key = $line.Substring(0, $separatorIndex).Trim()
    $value = $line.Substring($separatorIndex + 1).Trim()
    Set-Item -Path "Env:$key" -Value $value
}

$apiDll = Join-Path $ApiPath "PixDynamicGallery.Api.dll"
if (-not (Test-Path $apiDll)) {
    throw "API build not found at $apiDll - publish it there first (see tools/booth/README.md)."
}

$tunnelToken = $env:CLOUDFLARE_TUNNEL_TOKEN
if (-not $tunnelToken) {
    throw "CLOUDFLARE_TUNNEL_TOKEN is not set in $EnvFile."
}

Write-Host "Starting API (dotnet $apiDll)..."
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList @($apiDll) -WorkingDirectory $ApiPath -PassThru -WindowStyle Normal

Write-Host "Starting cloudflared tunnel..."
$tunnelProcess = Start-Process -FilePath $CloudflaredPath -ArgumentList @("tunnel", "run", "--token", $tunnelToken) -PassThru -WindowStyle Normal

Write-Host ""
Write-Host "Booth stack running:"
Write-Host "  API         -> PID $($apiProcess.Id), listening on $env:ASPNETCORE_URLS"
Write-Host "  cloudflared -> PID $($tunnelProcess.Id), public at https://api.somospix.com"
Write-Host ""
Write-Host "Two windows opened, one per process. Closing this launcher window does NOT stop them -"
Write-Host "close/Ctrl+C each of those two windows to stop the stack."
