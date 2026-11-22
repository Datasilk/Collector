# StopServices.ps1 - stops the Windows services listed in
# {deploy}\config.json (written by setup.exe). Collector first, then
# the router, then Charlotte.
#
# Run elevated: powershell -ExecutionPolicy Bypass -File StopServices.ps1

$cfg = Join-Path $PSScriptRoot "..\config.json"

Write-Host ""
Write-Host "============================================"
Write-Host "  Stopping Services"
Write-Host "============================================"
Write-Host ""

if (-not (Test-Path $cfg)) {
    Write-Host "config.json not found - run setup.exe first."
    Write-Host ""
    exit 1
}

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "NOTE: Not running as Administrator - service control requires elevated rights."
    Write-Host ""
}

$registered = @(Get-Content $cfg -Raw | ConvertFrom-Json)

foreach ($name in @("Collector", "CharlottesWeb", "Charlotte")) {
    if ($registered -notcontains $name) { continue }
    Write-Host "Stopping $name service..."
    Stop-Service -Name $name -Force -ErrorAction SilentlyContinue
    Write-Host ""
}

Write-Host "Done!"
Write-Host ""
