# StartServices.ps1 - starts the Windows services listed in
# {deploy}\config.json (written by setup.exe). Charlotte first, then
# the router, then Collector.
#
# Run elevated: powershell -ExecutionPolicy Bypass -File StartServices.ps1

$cfg = Join-Path $PSScriptRoot "..\config.json"

Write-Host ""
Write-Host "============================================"
Write-Host "  Starting Services"
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

foreach ($name in @("Charlotte", "CharlottesWeb", "Collector")) {
    if ($registered -notcontains $name) { continue }
    Write-Host "Starting $name service..."
    Start-Service -Name $name -ErrorAction SilentlyContinue
    if ((Get-Service -Name $name).Status -eq "Running") {
        Write-Host "  $name is running."
    } else {
        Write-Host "  Failed to start $name."
    }
    Write-Host ""
}

Write-Host "Done!"
Write-Host ""
