param(
    [string]$Root = $PSScriptRoot,
    [string]$FeHttp  = "7784",
    [string]$FeHttps = "7783",
    [string]$BeHttp  = "7780",
    [string]$BeHttps = "7781",
    # Host path to the Collector content folder (created by caller if
    # missing). Storage paths in appsettings.json get rewritten to
    # $StoragePrefix when set (container mount point), else $ContentRoot.
    [string]$ContentRoot   = "",
    [string]$StoragePrefix = ""
)

$ErrorActionPreference = 'Stop'

# --- Backend: appsettings.json ----------------------------------------
# "Urls" is what Program.cs reads to bind Kestrel; Auth.Domain and the
# JWT issuer/audience must match the URL users open in the browser.
$app = Join-Path $Root 'Collector.Web.Server\appsettings.json'
if (Test-Path $app) {
    $j = Get-Content $app -Raw | ConvertFrom-Json
    $j | Add-Member -NotePropertyName Urls `
        -NotePropertyValue "http://0.0.0.0:$BeHttp;https://0.0.0.0:$BeHttps" -Force
    if ($j.Auth) {
        $j.Auth.Domain = "https://localhost:$FeHttps"
        if ($j.Auth.JWT) {
            $j.Auth.JWT.ValidIssuer   = "https://localhost:$FeHttps"
            $j.Auth.JWT.ValidAudience = "https://localhost:$FeHttps"
        }
    }
    if ($ContentRoot -ne "" -or $StoragePrefix -ne "") {
        $prefix = if ($StoragePrefix -ne "") { $StoragePrefix } `
                  else { $ContentRoot -replace '/', '\' }
        $prefix = $prefix.TrimEnd('/', '\')
        $sep = if ($prefix.StartsWith('/')) { '/' } else { '\' }
        if ($j.Storage) {
            $j.Storage.Articles = "$prefix${sep}articles$sep"
            $j.Storage.Journal  = "$prefix${sep}journals$sep"
            Write-Host "  Storage.Articles = $($j.Storage.Articles)"
            Write-Host "  Storage.Journal  = $($j.Storage.Journal)"
        }
    }
    [IO.File]::WriteAllText($app, ($j | ConvertTo-Json -Depth 32))
    Write-Host "Updated $app"
    Write-Host "  Urls = http://0.0.0.0:$BeHttp;https://0.0.0.0:$BeHttps"
    Write-Host "  Auth.Domain / JWT issuer = https://localhost:$FeHttps"
}
else {
    Write-Host "WARNING: $app not found - backend ports not updated"
}

# --- Built client bundle (wwwroot) -------------------------------------
# apiBasePath() in endpoints.js hard-codes backend ports 7780/7781 and the
# built bundle carries them literally - patch the minified assets in place.
$www = Join-Path $Root 'Collector.Web.Server\wwwroot'
if (Test-Path $www) {
    $count = 0
    Get-ChildItem $www -Recurse -Include *.js -File | ForEach-Object {
        $t = Get-Content $_.FullName -Raw
        $n = $t -replace '7781', $BeHttps -replace '7780', $BeHttp
        if ($n -ne $t) { [IO.File]::WriteAllText($_.FullName, $n); $count++ }
    }
    Write-Host "Patched backend ports in $count wwwroot JS asset(s)"
}
else {
    Write-Host "No wwwroot at $www - skipping bundle patch"
}

# --- vite.config.js (front-end dev ports + proxy target) ---------------
# The React client isn't part of deploy.bat's output, but update it
# wherever it's found (deployed tree or a sibling checkout).
$found = $false
foreach ($rel in @('Collector.Web.Client\vite.config.js')) {
    $f = Join-Path $Root $rel
    if (Test-Path $f) {
        $t = Get-Content $f -Raw
        $t = $t -replace '7784', $FeHttp -replace '7783', $FeHttps `
               -replace '7781', $BeHttps -replace '7780', $BeHttp
        [IO.File]::WriteAllText($f, $t)
        Write-Host "Updated $f"
        $found = $true
    }
}
if (-not $found) {
    Write-Host "vite.config.js not found under $Root - skipping front-end dev config"
}
