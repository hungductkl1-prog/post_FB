<#
  analyze-grid-log.ps1
  Parses logs/account_grid_perf.log (+ optional grid_metrics.log) produced with
  ACCOUNT_GRID_DIAG=1 and prints the benchmark report:
    - TOP 10 operations by UI time (from [UiProfiler])
    - LoadScope / Bind breakdowns (from [LoadScopeBreakdown] / [BindBreakdown])
    - Memory consumers peak (from [MemoryStats])
    - GC summary (from [GC])
    - Account retention (from [AccountRetention])

  Usage:
    powershell -ExecutionPolicy Bypass -File tools\analyze-grid-log.ps1 -Path logs\account_grid_perf.log
#>
param(
    [Parameter(Mandatory = $true)] [string]$Path
)

if (-not (Test-Path $Path)) { Write-Error "Log file not found: $Path"; exit 1 }
$lines = Get-Content $Path

function Strip-Ts([string]$line) {
    # Drop the leading "HH:mm:ss.fff " timestamp Emit prepends.
    return ($line -replace '^\d{2}:\d{2}:\d{2}\.\d{3}\s+', '')
}

# ── [UiProfiler] Top rows: "#N name | calls/s | avg | max | total | %" ──
$ui = @{}
foreach ($raw in $lines) {
    $l = Strip-Ts $raw
    if ($l -match '^\[UiProfiler\]\s+#\d+\s+(.+?)\s+\|\s+([\d\.]+)\s+\|\s+([\d\.]+)\s+\|\s+([\d\.]+)\s+\|\s+([\d\.]+)\s+\|\s+([\d\.]+)%') {
        $name = $matches[1].Trim()
        $row = [pscustomobject]@{
            Operation  = $name
            CallsPerSec= [double]$matches[2]
            AvgMs      = [double]$matches[3]
            MaxMs      = [double]$matches[4]
            TotalMsSec = [double]$matches[5]
            PctUI      = [double]$matches[6]
        }
        # Keep the window with the largest Total ms/s for each operation (worst case).
        if (-not $ui.ContainsKey($name) -or $ui[$name].TotalMsSec -lt $row.TotalMsSec) {
            $ui[$name] = $row
        }
    }
}

Write-Host "==== TOP 10 OPERATIONS BY UI TIME (worst 30s window) ===="
if ($ui.Count -eq 0) {
    Write-Host "  (no [UiProfiler] rows found - did you run with ACCOUNT_GRID_DIAG=1 and let a 30s window elapse?)"
} else {
    $rank = 1
    $ui.Values | Sort-Object TotalMsSec -Descending | Select-Object -First 10 | ForEach-Object {
        "{0,2}. {1,-28} Avg={2,8:0.000}ms  Max={3,8:0.000}ms  Calls/s={4,8:0.0}  Total={5,7:0.00}ms/s  %UI={6,5:0.0}" -f `
            $rank, $_.Operation, $_.AvgMs, $_.MaxMs, $_.CallsPerSec, $_.TotalMsSec, $_.PctUI
        $rank++
    }
}

# ── UIBusy summary ──
$busy = @()
foreach ($raw in $lines) {
    $l = Strip-Ts $raw
    if ($l -match 'UIBusy~=([\d\.]+)%.*probeDispatchAvg=([\d\.]+)ms\s+probeDispatchMax=([\d\.]+)ms') {
        $busy += [pscustomobject]@{ Busy=[double]$matches[1]; DispAvg=[double]$matches[2]; DispMax=[double]$matches[3] }
    }
}
if ($busy.Count) {
    $b = $busy | Measure-Object Busy -Maximum -Average
    $dmax = ($busy | Measure-Object DispMax -Maximum).Maximum
    Write-Host ""
    Write-Host ("UI Busy: avg={0:0.0}%  peak={1:0.0}%   ProbeDispatch peak={2:0.0}ms  (samples={3})" -f $b.Average, $b.Maximum, $dmax, $busy.Count)
}

# ── [LoadScopeBreakdown] ──
Write-Host ""
Write-Host "==== LOADSCOPE BREAKDOWN (last occurrence) ===="
$ls = ($lines | Where-Object { (Strip-Ts $_) -match '^\[LoadScopeBreakdown\]' } | Select-Object -Last 1)
if ($ls) { Write-Host ("  " + (Strip-Ts $ls)) } else { Write-Host "  (none)" }

# ── [BindBreakdown] (largest Total) ──
Write-Host ""
Write-Host "==== BIND BREAKDOWN (worst by Total) ===="
$bindWorst = $null; $bindMax = -1
foreach ($raw in $lines) {
    $l = Strip-Ts $raw
    if ($l -match '^\[BindBreakdown\].*Total=([\d\.]+)ms') {
        $t = [double]$matches[1]
        if ($t -gt $bindMax) { $bindMax = $t; $bindWorst = $l }
    }
}
if ($bindWorst) { Write-Host ("  " + $bindWorst) } else { Write-Host "  (none)" }

# ── [MemoryStats] peak ──
Write-Host ""
Write-Host "==== MEMORY CONSUMERS (peak) ===="
$mem = @()
foreach ($raw in $lines) {
    $l = Strip-Ts $raw
    if ($l -match 'ManagedHeap=(\d+)MB PrivateMemory=(\d+)MB LOH=(\d+)MB Gen0=(\d+) Gen1=(\d+) Gen2=(\d+)') {
        $mem += [pscustomobject]@{
            Managed=[int]$matches[1]; Private=[int]$matches[2]; LOH=[int]$matches[3]
            Gen0=[int]$matches[4]; Gen1=[int]$matches[5]; Gen2=[int]$matches[6]
        }
    }
}
if ($mem.Count) {
    $pm = ($mem | Measure-Object Private -Maximum).Maximum
    $mh = ($mem | Measure-Object Managed -Maximum).Maximum
    $lo = ($mem | Measure-Object LOH -Maximum).Maximum
    $g2 = ($mem | Measure-Object Gen2 -Maximum).Maximum
    $g1 = ($mem | Measure-Object Gen1 -Maximum).Maximum
    $g0 = ($mem | Measure-Object Gen0 -Maximum).Maximum
    Write-Host ("  ManagedHeap peak={0}MB  PrivateMemory peak={1}MB  LOH peak={2}MB" -f $mh, $pm, $lo)
    Write-Host ("  GC collections (absolute max): Gen0={0} Gen1={1} Gen2={2}" -f $g0, $g1, $g2)
} else { Write-Host "  (no [MemoryStats] rows)" }

# ── [GC] pause summary ──
Write-Host ""
Write-Host "==== GC PAUSE SUMMARY ===="
$pauses = @()
foreach ($raw in $lines) {
    $l = Strip-Ts $raw
    if ($l -match 'LastPause=([\d\.]+)ms PauseTime=([\d\.]+)%') {
        $pauses += [double]$matches[1]
    }
}
if ($pauses.Count) {
    $mp = ($pauses | Measure-Object -Maximum).Maximum
    $ap = ($pauses | Measure-Object -Average).Average
    Write-Host ("  Largest GC pause={0:0.0}ms  Average GC pause={1:0.0}ms  (samples={2})" -f $mp, $ap, $pauses.Count)
} else { Write-Host "  (no [GC] rows)" }

# ── [UiHandler] slow handler invocations (>= threshold) ──
Write-Host ""
Write-Host "==== SLOW UI HANDLERS ([UiHandler] >= 8ms) ===="
$uh = @{}
foreach ($raw in $lines) {
    $l = Strip-Ts $raw
    if ($l -match '^\[UiHandler\] Name=(.+?) DurationMs=([\d\.]+)') {
        $n = $matches[1]; $ms = [double]$matches[2]
        if (-not $uh.ContainsKey($n)) { $uh[$n] = [pscustomobject]@{ Name=$n; Count=0; MaxMs=0.0; SumMs=0.0 } }
        $uh[$n].Count++; $uh[$n].SumMs += $ms
        if ($ms -gt $uh[$n].MaxMs) { $uh[$n].MaxMs = $ms }
    }
}
if ($uh.Count) {
    $uh.Values | Sort-Object MaxMs -Descending | ForEach-Object {
        "{0,-30} Count={1,5}  Max={2,8:0.0}ms  Avg={3,8:0.0}ms" -f $_.Name, $_.Count, $_.MaxMs, ($_.SumMs / $_.Count)
    }
} else { Write-Host "  (no slow handlers logged - all UI handlers ran < 8ms)" }

# ── [AccountRetention] last ──
Write-Host ""
Write-Host "==== ACCOUNT RETENTION (last) ===="
$ret = ($lines | Where-Object { (Strip-Ts $_) -match '^\[AccountRetention\]' } | Select-Object -Last 1)
if ($ret) { Write-Host ("  " + (Strip-Ts $ret)) } else { Write-Host "  (none)" }
