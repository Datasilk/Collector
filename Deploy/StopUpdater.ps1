# StopUpdater.ps1 - stops the Collector Updater service.
# Resolves the real service name via the "Collector Updater" display
# name in case it was registered under something other than
# CollectorUpdater.
#
# Run elevated: powershell -ExecutionPolicy Bypass -File StopUpdater.ps1

Write-Host ""
Write-Host "============================================"
Write-Host "  Stopping Collector Updater"
Write-Host "============================================"
Write-Host ""

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "NOTE: Not running as Administrator - service control requires elevated rights."
    Write-Host ""
}

$names = @("CollectorUpdater") +
    @(Get-Service -DisplayName "Collector Updater" -ErrorAction SilentlyContinue |
        ForEach-Object { $_.Name })
$found = $false
foreach ($name in ($names | Select-Object -Unique)) {
    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if (-not $svc) { continue }
    $found = $true
    Write-Host "Stopping $name service..."
    Stop-Service -Name $name -Force -ErrorAction SilentlyContinue
}
if (-not $found) { Write-Host "Collector Updater service is not registered." }

Write-Host ""
Write-Host "Done!"
Write-Host ""
