<#
.SYNOPSIS
  Apply EF Core migrations and load data via the Import CLI (no web server).

.DESCRIPTION
  Use this when you only want the database prepared:
  - Runs `dotnet ef database update` (all pending migrations).
  - Runs DecisionOS.Distribution.Import with your tenant, week, and CSV paths.

  KPI definitions, driver catalog, snapshots, drivers, alerts, and weekly focus
  are created/updated by the Import project.

  scripts\run.ps1 calls this script for all migrate/import/build (unless both
  SkipMigrate and SkipImport are set).

  ASP.NET Identity (roles and SeedAdmin user) runs when you start the Web
  project the first time after migrations — e.g. run.ps1 or
  dotnet run --project src\DecisionOS.Distribution.Web.

.PARAMETER SkipBuild
  Do not run `dotnet build` before migrate/import.

.PARAMETER SkipMigrate
  Skip `dotnet ef database update`.

.PARAMETER SkipImport
  Skip the Import CLI (migrations only).

  Other parameters match scripts\run.ps1 for database and CSV paths.

.EXAMPLE
  .\scripts\setup-database.ps1

.EXAMPLE
  .\scripts\setup-database.ps1 -PeriodEnd 2026-03-07 -SkipBuild
#>

[CmdletBinding()]
param(
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

    [switch]$SkipBuild,
    [switch]$SkipMigrate,
    [switch]$SkipImport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot
try {
    $connectionString = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"
    $env:ConnectionStrings__DecisionOs = $connectionString

    Write-Host "ConnectionStrings__DecisionOs -> Host=$DbHost;...;Database=$DbName;Username=$DbUser;Password=****"

    if (-not $SkipBuild) {
        Write-Host "Building solution..."
        dotnet build ".\\DecisionOS.Distribution.V1.sln"
    }

    if (-not $SkipMigrate) {
        Write-Host "Applying EF Core migrations..."
        dotnet ef database update `
            --project ".\\src\\DecisionOS.Distribution.Infrastructure" `
            --startup-project ".\\src\\DecisionOS.Distribution.Web"
    }

    if (-not $SkipImport) {
        if (-not (Test-Path $KpiCsvPath)) {
            throw "KPI CSV not found: $KpiCsvPath"
        }
        if (-not (Test-Path $DriversCsvPath)) {
            throw "Driver CSV not found: $DriversCsvPath"
        }

        Write-Host "Importing KPI and driver data..."
        dotnet run --project ".\\src\\DecisionOS.Distribution.Import" -- $ClientId $PeriodEnd $KpiCsvPath $DriversCsvPath
    }

    Write-Host ""
    Write-Host "Database setup finished."
    if (-not $SkipMigrate) {
        Write-Host "Identity roles/admin: start the Web project once to run SeedAdmin (see appsettings.Development.json)."
    }
}
finally {
    Pop-Location
}
