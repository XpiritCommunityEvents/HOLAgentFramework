#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Update central NuGet package versions while staying on the SDK's major version.
.DESCRIPTION
  Dry run by default; -Apply rewrites only Version attributes in Directory.Packages.props.
  Rules:
   1. SDK major comes from global.json (sdk.version).
   2. SDK-aligned prefixes (package-policy.json) and any package already on the SDK major
      stay on the SDK major.
   3. Other packages may move to any newer major.
   4. Stable current -> stable candidates only. Prerelease current -> newer prerelease/stable.
   5. Packages in "pinned" are never changed.
#>
param(
    [string]$Repo = '.',
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$policy = Get-Content (Join-Path $PSScriptRoot 'package-policy.json') -Raw | ConvertFrom-Json
$root = (Resolve-Path $Repo).Path
$sdk = (Get-Content (Join-Path $root 'global.json') -Raw | ConvertFrom-Json).sdk.version
$sdkMajor = [int]($sdk.Split('.')[0])
$propsPath = Join-Path $root 'Directory.Packages.props'
$aligned = @($policy.sdkAlignedPrefixes)
$pinned = @{}
$policy.pinned.PSObject.Properties | ForEach-Object { $pinned[$_.Name] = $_.Value }

function ConvertTo-Ver([string]$v) {
    $core, $pre = ($v -split '\+')[0] -split '-', 2
    $n = @($core.Split('.') | ForEach-Object { [int]$_ })
    while ($n.Count -lt 4) { $n += 0 }
    [pscustomobject]@{ Nums = $n; Pre = $pre }
}

# Returns <0, 0, >0 like a comparer; stable > prerelease of same core.
function Compare-Ver([string]$a, [string]$b) {
    $x = ConvertTo-Ver $a; $y = ConvertTo-Ver $b
    for ($i = 0; $i -lt 4; $i++) {
        if ($x.Nums[$i] -ne $y.Nums[$i]) { return $x.Nums[$i] - $y.Nums[$i] }
    }
    if (-not $x.Pre -and -not $y.Pre) { return 0 }
    if (-not $x.Pre) { return 1 }
    if (-not $y.Pre) { return -1 }
    $p = $x.Pre.Split('.'); $q = $y.Pre.Split('.')
    for ($i = 0; $i -lt [Math]::Max($p.Count, $q.Count); $i++) {
        if ($i -ge $p.Count) { return -1 }
        if ($i -ge $q.Count) { return 1 }
        $pn = $p[$i] -match '^\d+$'; $qn = $q[$i] -match '^\d+$'
        if ($pn -and $qn) { $d = [long]$p[$i] - [long]$q[$i]; if ($d -ne 0) { return [Math]::Sign($d) } }
        elseif ($pn) { return -1 } elseif ($qn) { return 1 }
        else { $d = [string]::CompareOrdinal($p[$i], $q[$i]); if ($d -ne 0) { return [Math]::Sign($d) } }
    }
    0
}

function Get-NuGetVersions([string]$pkg) {
    try {
        (Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$($pkg.ToLowerInvariant())/index.json").versions
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) { return @() }
        throw
    }
}

$text = Get-Content $propsPath -Raw
$pattern = '(<PackageVersion\s+Include="(?<pkg>[^"]+)"[^>]*?\sVersion=")(?<ver>[^"]+)(")'
$changes = [System.Collections.Generic.List[object]]::new()
$skipped = [System.Collections.Generic.List[object]]::new()

$newText = [regex]::Replace($text, $pattern, {
    param($m)
    $pkg = $m.Groups['pkg'].Value; $cur = $m.Groups['ver'].Value
    if ($cur -like '*$(*') { $skipped.Add("$pkg $cur (MSBuild property)"); return $m.Value }
    if ($pinned.ContainsKey($pkg)) { $skipped.Add("$pkg $cur (pinned: $($pinned[$pkg]))"); return $m.Value }
    $curV = ConvertTo-Ver $cur
    $keepMajor = ($curV.Nums[0] -eq $sdkMajor) -or [bool]($aligned | Where-Object { $pkg.StartsWith($_) })
    $best = $null
    foreach ($v in (Get-NuGetVersions $pkg)) {
        $nv = ConvertTo-Ver $v
        if ($keepMajor -and $nv.Nums[0] -ne $sdkMajor) { continue }
        if ($nv.Pre -and -not $curV.Pre) { continue }
        if ((Compare-Ver $v $cur) -le 0) { continue }
        if (-not $best -or (Compare-Ver $v $best) -gt 0) { $best = $v }
    }
    if (-not $best) { return $m.Value }
    $changes.Add([pscustomobject]@{ Package = $pkg; Old = $cur; New = $best })
    $m.Groups[1].Value + $best + $m.Groups[2].Value
})

if ($Apply -and $changes.Count) { Set-Content $propsPath $newText -NoNewline }

Write-Host "SDK $sdk (major $sdkMajor); $(if ($Apply) { 'APPLIED' } else { 'DRY RUN' })"
if ($changes.Count) { $changes | ForEach-Object { Write-Host "  $($_.Package): $($_.Old) -> $($_.New)" } }
else { Write-Host '  no updates' }
$skipped | ForEach-Object { Write-Host "  skipped $_" }
