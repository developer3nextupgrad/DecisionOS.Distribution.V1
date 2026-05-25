<#
.SYNOPSIS
  One-command run for DecisionOS Distribution V1 (database setup via setup-database.ps1, then web UI).

.DESCRIPTION
  - Frees the configured web port (optional)
  - Delegates to scripts\setup-database.ps1 for: build (when migrate/import run), EF migrations, Import CLI
  - Starts the web application on the chosen port

  For database-only work (no web server), use .\scripts\setup-database.ps1 directly.

.PARAMETER WebPort
  Port for the web UI (default: 5276).

.PARAMETER DbHost
.PARAMETER DbPort
.PARAMETER DbName
.PARAMETER DbUser
.PARAMETER DbPassword
  PostgreSQL connection parameters. Used to build ConnectionStrings__DecisionOs.

.PARAMETER ClientId
.PARAMETER PeriodEnd
  Import tenant and week. PeriodEnd format: YYYY-MM-DD.

.PARAMETER KpiCsvPath
.PARAMETER DriversCsvPath
  CSV paths to import.

.PARAMETER SkipMigrate
  Passed to setup-database.ps1 — skip `dotnet ef database update`.

.PARAMETER SkipImport
  Passed to setup-database.ps1 — skip Import CLI.

.PARAMETER NoFreePort
  Do not attempt to stop processes listening on WebPort.

.PARAMETER OpenBrowser
  Open the dashboard URL after starting the web app.

.EXAMPLE
  # Default: postgres/postgres, decisionos DB, port 5276
  .\scripts\run.ps1 -OpenBrowser

.EXAMPLE
  # Run on another port and import a different week
  .\scripts\run.ps1 -WebPort 6001 -PeriodEnd 2026-03-07 -OpenBrowser
#>

[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int]$WebPort = 5276,

    [string]$DbHost = "localhost",
    [ValidateRange(1, 65535)]
    [int]$DbPort = 5432,
    [string]$DbName = "decisionos",
    [string]$DbUser = "postgres",
    [string]$DbPassword = "postgres",

    [string]$ClientId = "DIST-001",
    [string]$PeriodEnd = "2026-02-28",

    [string]$KpiCsvPath = ".\\samples\\kpi_snapshots_upload.csv",
    [string]$DriversCsvPath = ".\\samples\\driver_values_upload.csv",

    [switch]$SkipMigrate,
    [switch]$SkipImport,
    [switch]$NoFreePort,
    [switch]$OpenBrowser
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$setupScript = Join-Path $PSScriptRoot "setup-database.ps1"

Push-Location $repoRoot
try {
    $connectionString = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"
    $env:ConnectionStrings__DecisionOs = $connectionString

    # Force Kestrel to bind to a single, predictable HTTP port.
    $env:ASPNETCORE_URLS = "http://localhost:$WebPort"

    Write-Host "Using DB connection: $connectionString"
    Write-Host "Web URL: http://localhost:$WebPort"

    if (-not $NoFreePort) {
        Write-Host "Freeing port $WebPort (if in use)..."
        & ".\\scripts\\free-port.ps1" -Port $WebPort -Force | Out-Host
    }

    $skipAllDb = $SkipMigrate -and $SkipImport
    if (-not $skipAllDb) {
        $setupParams = @{
            DbHost          = $DbHost
            DbPort          = $DbPort
            DbName          = $DbName
            DbUser          = $DbUser
            DbPassword      = $DbPassword
            ClientId        = $ClientId
            PeriodEnd       = $PeriodEnd
            KpiCsvPath      = $KpiCsvPath
            DriversCsvPath  = $DriversCsvPath
        }
        if ($SkipMigrate) { $setupParams.SkipMigrate = $true }
        if ($SkipImport) { $setupParams.SkipImport = $true }

        Write-Host "Running database setup ( scripts\setup-database.ps1 )..."
        & $setupScript @setupParams
    }
    else {
        Write-Host "Skipping database migrate/import (SkipMigrate and SkipImport). Building solution..."
        dotnet build ".\\DecisionOS.Distribution.V1.sln"
    }

    if ($OpenBrowser) {
        Start-Process "http://localhost:$WebPort"
    }

    Write-Host "Starting web app (Ctrl+C to stop)..."
    dotnet run --no-launch-profile --project ".\\src\\DecisionOS.Distribution.Web"
}
finally {
    Pop-Location
}
