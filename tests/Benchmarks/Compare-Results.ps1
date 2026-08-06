<#
.SYNOPSIS
    Compares two stored benchmark runs and reports the mean-time and allocation deltas.

.DESCRIPTION
    Reads the *-report-full.json files produced by BenchmarkDotNet under
    BenchmarkResults/<label>/results and matches benchmarks across the two runs by their full name
    plus parameters. Benchmarks present in only one run are listed separately, so a renamed or newly
    added benchmark is never silently dropped from the comparison.

    Mean times come from Statistics.Mean (nanoseconds); allocations from
    Memory.BytesAllocatedPerOperation.

.PARAMETER Baseline
    Label of the earlier run, i.e. the directory name under BenchmarkResults.

.PARAMETER Candidate
    Label of the later run to compare against the baseline.

.PARAMETER Filter
    Optional wildcard applied to the benchmark full name, e.g. '*Item07*'.

.PARAMETER SignificanceThreshold
    Relative change below which a difference is reported as "same". Defaults to 0.05 (5%), which is
    roughly the noise floor for a quiet machine. Raise it on a busy or virtualized host.

.PARAMETER CsvPath
    Optional path to also write the comparison as CSV.

.EXAMPLE
    ./Compare-Results.ps1 -Baseline baseline -Candidate after-item01

.EXAMPLE
    ./Compare-Results.ps1 -Baseline baseline -Candidate after-item01 -Filter '*Item01*' -CsvPath delta.csv
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Baseline,
    [Parameter(Mandatory = $true)][string]$Candidate,
    [string]$Filter = '*',
    [double]$SignificanceThreshold = 0.05,
    [string]$CsvPath
)

$ErrorActionPreference = 'Stop'

$resultsRoot = if ($env:TLUMACH_BENCH_RESULTS) { $env:TLUMACH_BENCH_RESULTS }
               else { Join-Path $PSScriptRoot 'BenchmarkResults' }

function Read-Run {
    param([string]$Label)

    $dir = Join-Path $resultsRoot $Label
    if (-not (Test-Path $dir)) {
        $available = if (Test-Path $resultsRoot) {
            (Get-ChildItem -Path $resultsRoot -Directory | Select-Object -ExpandProperty Name) -join ', '
        } else { '(none)' }
        throw "No run labelled '$Label' under '$resultsRoot'. Available: $available"
    }

    $files = Get-ChildItem -Path $dir -Recurse -Filter '*-report-full.json' -ErrorAction SilentlyContinue
    if (-not $files) { throw "Run '$Label' contains no *-report-full.json files. Did the run complete?" }

    $map = @{}
    foreach ($file in $files) {
        $json = Get-Content -Raw -LiteralPath $file.FullName | ConvertFrom-Json
        foreach ($b in $json.Benchmarks) {
            # Parameters are part of the identity: [Params] produces one entry per combination.
            $key = if ([string]::IsNullOrEmpty($b.Parameters)) { $b.FullName } else { "$($b.FullName) [$($b.Parameters)]" }
            $map[$key] = [pscustomobject]@{
                Key         = $key
                DisplayName = $b.MethodTitle
                Type        = $b.Type
                Parameters  = $b.Parameters
                MeanNs      = [double]$b.Statistics.Mean
                StdDevNs    = [double]$b.Statistics.StandardDeviation
                AllocBytes  = if ($null -ne $b.Memory) { [double]$b.Memory.BytesAllocatedPerOperation } else { [double]::NaN }
            }
        }
    }

    return $map
}

$invariant = [System.Globalization.CultureInfo]::InvariantCulture

function Format-Ratio {
    param([double]$Ratio)

    if ([double]::IsNaN($Ratio) -or [double]::IsInfinity($Ratio)) { return 'n/a' }
    return ([string]::Format($script:invariant, '{0,6:0.00}x', $Ratio))
}

function Format-Number {
    param([double]$Value, [string]$Format)

    return ([string]::Format($script:invariant, $Format, $Value))
}

function Get-Verdict {
    param([double]$Ratio, [double]$Threshold)

    if ([double]::IsNaN($Ratio)) { return 'n/a' }
    if ($Ratio -lt (1 - $Threshold)) { return 'FASTER' }
    if ($Ratio -gt (1 + $Threshold)) { return 'SLOWER' }
    return 'same'
}

$baselineRun  = Read-Run -Label $Baseline
$candidateRun = Read-Run -Label $Candidate

$rows = @()
foreach ($key in ($baselineRun.Keys | Sort-Object)) {
    if ($key -notlike $Filter) { continue }
    if (-not $candidateRun.ContainsKey($key)) { continue }

    $b = $baselineRun[$key]
    $c = $candidateRun[$key]

    $timeRatio  = if ($b.MeanNs -gt 0) { $c.MeanNs / $b.MeanNs } else { [double]::NaN }
    $allocRatio = if ($b.AllocBytes -gt 0) { $c.AllocBytes / $b.AllocBytes }
                  elseif ($c.AllocBytes -eq 0) { 1.0 }
                  else { [double]::NaN }

    $rows += [pscustomobject]@{
        Benchmark      = $key -replace '^Tlumach\.Benchmarks\.', ''
        BaselineNs     = [math]::Round($b.MeanNs, 2)
        CandidateNs    = [math]::Round($c.MeanNs, 2)
        TimeRatio      = $timeRatio
        TimeVerdict    = Get-Verdict -Ratio $timeRatio -Threshold $SignificanceThreshold
        BaselineBytes  = $b.AllocBytes
        CandidateBytes = $c.AllocBytes
        AllocRatio     = $allocRatio
        AllocVerdict   = Get-Verdict -Ratio $allocRatio -Threshold $SignificanceThreshold
    }
}

$onlyBaseline  = @($baselineRun.Keys  | Where-Object { $_ -like $Filter -and -not $candidateRun.ContainsKey($_) } | Sort-Object)
$onlyCandidate = @($candidateRun.Keys | Where-Object { $_ -like $Filter -and -not $baselineRun.ContainsKey($_) }  | Sort-Object)

Write-Host ''
Write-Host "Baseline : $Baseline" -ForegroundColor Cyan
Write-Host "Candidate: $Candidate" -ForegroundColor Cyan
Write-Host "Threshold: $([math]::Round($SignificanceThreshold * 100, 1))% - changes smaller than this are reported as 'same'." -ForegroundColor DarkGray
Write-Host ''

if ($rows.Count -eq 0) {
    Write-Host 'No benchmarks matched in both runs.' -ForegroundColor Yellow
}
else {
    $rows |
        Select-Object Benchmark,
            @{ n = 'Base ns';   e = { Format-Number $_.BaselineNs     '{0,12:N2}' } },
            @{ n = 'Cand ns';   e = { Format-Number $_.CandidateNs    '{0,12:N2}' } },
            @{ n = 'Time';      e = { Format-Ratio  $_.TimeRatio } },
            @{ n = 'Time?';     e = { $_.TimeVerdict } },
            @{ n = 'Base B';    e = { Format-Number $_.BaselineBytes  '{0,9:N0}' } },
            @{ n = 'Cand B';    e = { Format-Number $_.CandidateBytes '{0,9:N0}' } },
            @{ n = 'Alloc';     e = { Format-Ratio  $_.AllocRatio } },
            @{ n = 'Alloc?';    e = { $_.AllocVerdict } } |
        Format-Table -AutoSize

    $faster = @($rows | Where-Object { $_.TimeVerdict -eq 'FASTER' }).Count
    $slower = @($rows | Where-Object { $_.TimeVerdict -eq 'SLOWER' }).Count
    $allocDown = @($rows | Where-Object { $_.AllocVerdict -eq 'FASTER' }).Count

    Write-Host "Summary: $faster faster, $slower slower, $($rows.Count - $faster - $slower) unchanged; $allocDown allocate less." -ForegroundColor Green
    if ($slower -gt 0) {
        Write-Host 'Regressions:' -ForegroundColor Red
        $rows | Where-Object { $_.TimeVerdict -eq 'SLOWER' } | ForEach-Object {
            Write-Host ("  {0} ({1})" -f $_.Benchmark, (Format-Ratio $_.TimeRatio)) -ForegroundColor Red
        }
    }
}

if ($onlyBaseline.Count -gt 0) {
    Write-Host ''
    Write-Host "Present only in the baseline ($($onlyBaseline.Count)):" -ForegroundColor Yellow
    $onlyBaseline | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

if ($onlyCandidate.Count -gt 0) {
    Write-Host ''
    Write-Host "Present only in the candidate ($($onlyCandidate.Count)):" -ForegroundColor Yellow
    $onlyCandidate | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

if ($CsvPath) {
    $rows | Export-Csv -LiteralPath $CsvPath -NoTypeInformation
    Write-Host ''
    Write-Host "CSV written to $CsvPath"
}
