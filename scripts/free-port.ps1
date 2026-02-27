<#
.SYNOPSIS
  Frees a TCP port by stopping the listening process.

.DESCRIPTION
  Finds processes that are LISTENING on a given local TCP port and stops them.
  Useful when a dev server is stuck and you need the port back.

.PARAMETER Port
  Local TCP port number to free (e.g. 5276).

.PARAMETER Force
  Stop processes with -Force.

.EXAMPLE
  .\scripts\free-port.ps1 -Port 5276 -Force
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 65535)]
    [int]$Port,

    [switch]$Force
)

$connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if (-not $connections) {
    Write-Host "Port $Port is already free."
    exit 0
}

$pids = $connections | Select-Object -ExpandProperty OwningProcess -Unique

foreach ($processId in $pids) {
    if (-not $processId) { continue }

    $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
    $name = if ($proc) { $proc.ProcessName } else { "<unknown>" }

    Write-Host "Stopping PID $processId ($name) listening on port $Port..."

    try {
        if ($Force) {
            Stop-Process -Id $processId -Force -ErrorAction Stop
        }
        else {
            Stop-Process -Id $processId -ErrorAction Stop
        }
    }
    catch {
        Write-Warning "Failed to stop PID ${processId}: $($_.Exception.Message)"
    }
}

# Wait briefly for the port to be released
$deadline = (Get-Date).AddSeconds(10)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 250
    $stillListening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if (-not $stillListening) {
        Write-Host "Port $Port is now free."
        exit 0
    }
}

Write-Warning "Port $Port still appears to be in use."
exit 1

