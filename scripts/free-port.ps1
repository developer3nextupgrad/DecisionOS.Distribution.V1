<#
.SYNOPSIS
  Stops the Decision OS web host and frees local TCP ports.

.DESCRIPTION
  1) Stops Decision OS hosts: apphost process DecisionOS.Distribution.Web, DecisionOS.Distribution.Web.exe,
     and dotnet.exe when the command line references this web project.
  2) For each requested port, stops any process still LISTENING on that port.

  Use when a dev server is stuck or you need ports back before re-running.

.PARAMETER Port
  One or more TCP ports to free. Default: 5276 and 7153 (matches launchSettings.json http/https).

.PARAMETER Force
  Stop processes with -Force (stronger termination).

.PARAMETER SkipStopApp
  Do not stop Decision OS dotnet hosts; only free listeners on the given ports.

.EXAMPLE
  .\scripts\free-port.ps1

.EXAMPLE
  .\scripts\free-port.ps1 -Port 6001 -Force

.EXAMPLE
  .\scripts\free-port.ps1 -SkipStopApp -Port 5276
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateCount(1, 64)]
    [ValidateRange(1, 65535)]
    [int[]]$Port = @(5276, 7153),

    [switch]$Force,

    [switch]$SkipStopApp
)

Set-StrictMode -Version Latest

function Stop-DecisionOSWebHosts {
    param([switch]$UseForce)

    # PID -> human-readable reason (de-duped)
    $toStop = @{ }

    function Add-StopTarget {
        param([int]$ProcId, [string]$Reason)
        if ($ProcId -le 0) { return }
        if (-not $toStop.ContainsKey($ProcId)) {
            $toStop[$ProcId] = $Reason
        }
    }

    # Published apphost: process name DecisionOS.Distribution.Web
    foreach ($pp in Get-Process -Name 'DecisionOS.Distribution.Web' -ErrorAction SilentlyContinue) {
        Add-StopTarget -ProcId $pp.Id -Reason "DecisionOS.Distribution.Web host"
    }

    # CIM: apphost .exe + dotnet.exe when command line references this project
    try {
        $cim = @(
            @(Get-CimInstance Win32_Process -Filter "Name = 'DecisionOS.Distribution.Web.exe'" -ErrorAction SilentlyContinue),
            @(Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue)
        ) | ForEach-Object { $_ } | Where-Object { $_ }

        foreach ($p in $cim) {
            $cmd = [string]$p.CommandLine
            if ($p.Name -ieq 'DecisionOS.Distribution.Web.exe') {
                Add-StopTarget -ProcId ([int]$p.ProcessId) -Reason "DecisionOS.Distribution.Web.exe"
                continue
            }
            if ($cmd -match 'DecisionOS\.Distribution\.Web') {
                Add-StopTarget -ProcId ([int]$p.ProcessId) -Reason "dotnet host for Distribution.Web"
            }
        }
    }
    catch {
        Write-Warning "Process query (CIM) failed: $($_.Exception.Message). Relying on process name + port cleanup."
    }

    $stopped = 0
    foreach ($entry in $toStop.GetEnumerator() | Sort-Object -Property Key) {
        $procId = [int]$entry.Key
        $reason = [string]$entry.Value

        $name = "<unknown>"
        try {
            $procInfo = Get-Process -Id $procId -ErrorAction SilentlyContinue
            if ($procInfo) { $name = $procInfo.ProcessName }
        }
        catch { }

        Write-Host "Stopping PID $procId ($name) - $reason..."
        try {
            if ($UseForce) {
                Stop-Process -Id $procId -Force -ErrorAction Stop
            }
            else {
                Stop-Process -Id $procId -ErrorAction Stop
            }
            $stopped++
        }
        catch {
            Write-Warning "Failed to stop PID ${procId}: $($_.Exception.Message)"
        }
    }

    if ($stopped -gt 0) {
        Start-Sleep -Milliseconds 500
    }
    return $stopped
}

function Test-PortListenFree {
    param([int]$TcpPort)
    $c = Get-NetTCPConnection -LocalPort $TcpPort -State Listen -ErrorAction SilentlyContinue
    return (-not $c)
}

function Stop-ListenersOnPort {
    param(
        [int]$TcpPort,
        [switch]$UseForce
    )

    $connections = Get-NetTCPConnection -LocalPort $TcpPort -State Listen -ErrorAction SilentlyContinue
    if (-not $connections) {
        Write-Host "Port $TcpPort is already free."
        return $true
    }

    $procIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($processId in $procIds) {
        if (-not $processId) { continue }

        $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
        $name = if ($proc) { $proc.ProcessName } else { "<unknown>" }

        Write-Host "Stopping PID $processId ($name) listening on port $TcpPort..."

        try {
            if ($UseForce) {
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

    $deadline = (Get-Date).AddSeconds(10)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        if (Test-PortListenFree -TcpPort $TcpPort) {
            Write-Host "Port $TcpPort is now free."
            return $true
        }
    }

    Write-Warning "Port $TcpPort still appears to be in use."
    return $false
}

# --- Stop Decision OS hosts first (releases Kestrel bindings)
if (-not $SkipStopApp) {
    $n = Stop-DecisionOSWebHosts -UseForce:$Force
    if ($n -gt 0) {
        Write-Host "Stopped $n Decision OS host process(es)."
    }
    else {
        Write-Host "No Decision OS host process matched (may already be stopped). Clearing listeners on ports."
    }
}

# --- Free each port
$allOk = $true
$uniquePorts = $Port | Sort-Object -Unique
foreach ($p in $uniquePorts) {
    if (-not (Stop-ListenersOnPort -TcpPort $p -UseForce:$Force)) {
        $allOk = $false
    }
}

if ($allOk) {
    exit 0
}
exit 1
