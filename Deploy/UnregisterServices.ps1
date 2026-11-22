# UnregisterServices.ps1 - stops and removes the Collector,
# CharlottesWeb and Charlotte Windows services.
#
# Run elevated: powershell -ExecutionPolicy Bypass -File UnregisterServices.ps1

Write-Host ""
Write-Host "============================================"
Write-Host "  Unregistering Services"
Write-Host "============================================"
Write-Host ""

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "NOTE: Not running as Administrator - service control requires elevated rights."
    Write-Host ""
}

foreach ($name in @("Collector", "CharlottesWeb", "Charlotte")) {
    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "$name service is not registered - skipping."
        Write-Host ""
        continue
    }
    Write-Host "Stopping $name service..."
    Stop-Service -Name $name -Force -ErrorAction SilentlyContinue
    Write-Host "Removing $name service..."
    sc.exe delete $name | Out-Null
    if ($LASTEXITCODE -eq 0) { Write-Host "  $name service removed." }
    else { Write-Host "  Failed to remove $name service." }
    Write-Host ""
}

Write-Host "Done!"
Write-Host ""
