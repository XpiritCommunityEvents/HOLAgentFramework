#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Update the Aspire CLI, the Aspire packages, and the Aspire agent skill files (non-interactive, cross-platform).
  Runs: aspire update --self, aspire update, aspire agent init (standard/.agents location only).
  Pinned packages from package-policy.json are restored if `aspire update` bumps them.
#>
param([string]$Repo = (Join-Path $PSScriptRoot '../../../..'))
$ErrorActionPreference = 'Stop'
if (-not (Get-Command aspire -ErrorAction SilentlyContinue)) {
    Write-Host 'aspire CLI not found; skipping Aspire update.'; exit 0
}
Set-Location $Repo

$propsPath = Join-Path (Get-Location) 'Directory.Packages.props'
$policy = Get-Content (Join-Path $PSScriptRoot 'package-policy.json') -Raw | ConvertFrom-Json
$pinned = @($policy.pinned.PSObject.Properties.Name)

function Get-PinnedVersions {
    $xml = Get-Content $propsPath -Raw
    $map = @{}
    foreach ($id in $pinned) {
        $m = [regex]::Match($xml, '<PackageVersion\s+Include="' + [regex]::Escape($id) + '"\s+Version="([^"]+)"')
        if ($m.Success) { $map[$id] = $m.Groups[1].Value }
    }
    $map
}

function Invoke-Aspire {
    Write-Host "== aspire $($args -join ' ') =="
    & aspire @args
    if ($LASTEXITCODE -ne 0) { Write-Error "aspire $($args -join ' ') failed (exit $LASTEXITCODE)"; exit $LASTEXITCODE }
}

$before = Get-PinnedVersions

# Flags avoid the interactive channel/confirmation prompts, which fail in non-interactive terminals.
Invoke-Aspire update --self --channel stable --yes --non-interactive
Invoke-Aspire update --channel stable --yes --non-interactive

# `aspire update` ignores our pins (e.g. preview-only DevUI); restore them.
$xml = Get-Content $propsPath -Raw
foreach ($id in $before.Keys) {
    $pattern = '(<PackageVersion\s+Include="' + [regex]::Escape($id) + '"\s+Version=")[^"]+(")'
    $new = [regex]::Replace($xml, $pattern, { param($m) $m.Groups[1].Value + $before[$id] + $m.Groups[2].Value })
    if ($new -ne $xml) { Write-Host "Restored pinned $id -> $($before[$id])"; $xml = $new }
}
Set-Content $propsPath -Value $xml -NoNewline

# Only the standard (.agents) location; 'all' would also create .github/.claude/.opencode skill folders.
Invoke-Aspire agent init --workspace-root . --skill-locations standard --skills all --non-interactive
