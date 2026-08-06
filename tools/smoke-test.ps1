<#
.SYNOPSIS
  Smoke test end-to-end del backend: crea un evento, sube una foto de prueba por el mismo
  pipeline que usa el watcher, y valida que quede consultable.

.USAGE
  # con la API corriendo (docker compose up, o dotnet run) en http://localhost:8080
  ./tools/smoke-test.ps1
  ./tools/smoke-test.ps1 -ApiUrl http://localhost:5219   # si corres con `dotnet run` (perfil http)
#>
param(
    [string]$ApiUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

Write-Step "Comprobando que la API responde en $ApiUrl ..."
try {
    Invoke-WebRequest -Uri "$ApiUrl/swagger/v1/swagger.json" -UseBasicParsing | Out-Null
    Write-Host "API arriba." -ForegroundColor Green
} catch {
    Write-Host "No se pudo contactar $ApiUrl. ¿Está corriendo el contenedor/proceso?" -ForegroundColor Red
    throw
}

Write-Step "Creando un evento de prueba..."
$slug = "smoke-test-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
$eventBody = @{
    name            = "Smoke Test Event"
    slug            = $slug
    watchFolderPath = "C:\SparkboothPhotos\SmokeTest"
    guestBaseUrl    = "http://localhost:4200"
} | ConvertTo-Json

$event = Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/events" -ContentType "application/json" -Body $eventBody
Write-Host "Evento creado: Id=$($event.id) Slug=$($event.slug)" -ForegroundColor Green

Write-Step "Subiendo tools/sample.jpg vía el endpoint manual (mismo pipeline que el watcher)..."
$samplePath = Join-Path $PSScriptRoot "sample.jpg"
if (-not (Test-Path $samplePath)) { throw "No se encontró $samplePath" }

# Invoke-RestMethod -Form solo existe en PowerShell 7+; en Windows PowerShell 5.1 hay que
# construir el multipart/form-data a mano con HttpClient.
Add-Type -AssemblyName System.Net.Http
$httpClient = [System.Net.Http.HttpClient]::new()
try {
    $fileBytes = [System.IO.File]::ReadAllBytes($samplePath)
    $fileContent = [System.Net.Http.ByteArrayContent]::new($fileBytes)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/jpeg")

    $multipart = [System.Net.Http.MultipartFormDataContent]::new()
    $multipart.Add($fileContent, "file", "sample.jpg")

    $response = $httpClient.PostAsync("$ApiUrl/api/events/$($event.id)/photos", $multipart).GetAwaiter().GetResult()
    $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    if (-not $response.IsSuccessStatusCode) {
        throw "Upload falló con status $($response.StatusCode): $responseBody"
    }

    $photo = $responseBody | ConvertFrom-Json
} finally {
    $httpClient.Dispose()
}

Write-Host "Foto subida: Id=$($photo.id) Status=$($photo.status) Url=$($photo.url)" -ForegroundColor Green

Write-Step "Verificando que aparece en el feed del muro..."
$wall = Invoke-RestMethod -Uri "$ApiUrl/api/events/$($event.id)/photos"
if ($wall.items | Where-Object { $_.id -eq $photo.id }) {
    Write-Host "OK: la foto aparece en /api/events/$($event.id)/photos" -ForegroundColor Green
} else {
    Write-Host "La foto no aparece todavía en el feed (revisa Status arriba)." -ForegroundColor Yellow
}

Write-Step "Verificando que la URL subida a Storage es accesible..."
try {
    $head = Invoke-WebRequest -Uri $photo.url -Method Head -UseBasicParsing
    Write-Host "OK ($($head.StatusCode)): $($photo.url)" -ForegroundColor Green
} catch {
    Write-Host "No se pudo acceder a $($photo.url) directamente (normal si usas S3 real con PublicRead=false)." -ForegroundColor Yellow
}

Write-Host "`nListo. Usa este Id de evento en tools/signalr-test-client.html para ver el próximo upload en vivo:" -ForegroundColor Cyan
Write-Host $event.id -ForegroundColor White
