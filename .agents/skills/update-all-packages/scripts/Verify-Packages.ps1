#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Verify package updates: build every project, run Module 5 tests, audit NuGet and npm.
  Exit code is non-zero if any build or test fails.
#>
param([string]$Repo = '.')
Set-Location $Repo
$fail = $false
$skip = '[\\/](node_modules|apm_modules|bin|obj)[\\/]'
$projects = Get-ChildItem -Recurse -Filter *.csproj | Where-Object { $_.FullName -notmatch $skip } | Sort-Object FullName

Write-Host "== SDK =="; dotnet --version

Write-Host "== Build every project =="
foreach ($p in $projects) {
    $out = dotnet build $p.FullName --nologo -v q -clp:ErrorsOnly 2>&1
    if ($LASTEXITCODE -eq 0) { Write-Host "OK    $($p.FullName)" }
    else {
        Write-Host "FAIL  $($p.FullName)"; $fail = $true
        $out | Select-String 'error ' | Select-Object -Unique -First 5 | ForEach-Object { Write-Host "  $_" }
    }
}

Write-Host "== Module 5 tests =="
$m5 = 'exercises/module5/completed/GloboTicket.slnx'
if (Test-Path $m5) { dotnet test $m5 --nologo -v q; if ($LASTEXITCODE -ne 0) { $fail = $true } }

Write-Host "== NuGet vulnerabilities / deprecations =="
foreach ($flag in 'vulnerable', 'deprecated') {
    foreach ($p in $projects) {
        $hits = dotnet list $p.FullName package "--$flag" --include-transitive 2>$null | Select-String '^\s+>'
        if ($hits) { Write-Host "[$flag] $($p.FullName)"; $hits | ForEach-Object { Write-Host $_.Line } }
    }
}

Write-Host "== npm audit (informational) =="
$npm = 'exercises/module5/completed'
if (Test-Path (Join-Path $npm 'package.json')) {
    Push-Location $npm; npm audit --omit=dev 2>&1 | Select-Object -Last 15; Pop-Location
}

exit ($fail ? 1 : 0)
