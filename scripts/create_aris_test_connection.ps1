$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root ".env"

if (-not (Test-Path $envFile)) {
    throw "Missing .env file."
}

$line = Get-Content $envFile |
    Where-Object { $_ -match '^MSSQL_SA_PASSWORD=' } |
    Select-Object -First 1

if (-not $line) {
    throw "MSSQL_SA_PASSWORD was not found in .env."
}

$saPassword = ($line -replace '^MSSQL_SA_PASSWORD=', '').Trim().Trim('"', "'")

if ([string]::IsNullOrWhiteSpace($saPassword)) {
    throw "MSSQL_SA_PASSWORD is empty."
}

$env:ARIS_TEST_CONNECTION = "Server=localhost,1433;Database=ArisDb;User Id=sa;Password=$saPassword;TrustServerCertificate=True;Connect Timeout=30;"

$connectionPrefix = "Server=localhost,1433;Database=ArisDb;User Id=sa;Password="
$connectionSuffix = ";TrustServerCertificate=True;Connect Timeout=30;"
$env:ARIS_TEST_CONNECTION = $connectionPrefix + $saPassword + $connectionSuffix

Write-Host '$env:ARIS_TEST_CONNECTION has been assigned.' -ForegroundColor Green