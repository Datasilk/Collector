# UnregisterUpdater.ps1 - stops and removes the Collector Updater service.
# The service may be registered under a name other than CollectorUpdater
# (the display name is "Collector Updater"), so every service carrying
# that display name is removed as well.
#
# Run elevated: powershell -ExecutionPolicy Bypass -File UnregisterUpdater.ps1

Write-Host ""
Write-Host "============================================"
Write-Host "  Unregistering Collector Updater"
Write-Host "============================================"
Write-Host ""

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "NOTE: Not running as Administrator - service control requires elevated rights."
    Write-Host ""
}

function Remove-CollectorService([string]$name) {
    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "$name service is not registered - skipping."
        Write-Host ""
        return
    }
    Write-Host "Stopping $name service..."
    Stop-Service -Name $name -Force -ErrorAction SilentlyContinue
    Write-Host "Removing $name service..."
    sc.exe delete $name | Out-Null
    if ($LASTEXITCODE -eq 0) { Write-Host "  $name service removed." }
    else { Write-Host "  Failed to remove $name service." }
    Write-Host ""
}

# canonical name plus every service registered under another name but
# with the "Collector Updater" display name
$names = @("CollectorUpdater") +
    @(Get-Service -DisplayName "Collector Updater" -ErrorAction SilentlyContinue |
        ForEach-Object { $_.Name })
$names | Select-Object -Unique | ForEach-Object { Remove-CollectorService $_ }

Write-Host "Done!"
Write-Host ""
