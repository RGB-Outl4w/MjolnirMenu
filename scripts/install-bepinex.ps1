<#
.SYNOPSIS
  Installs BepInExPack_Valheim (denikson) into the Valheim folder.

.DESCRIPTION
  Downloads the Thunderstore package zip, extracts the BepInExPack_Valheim payload
  (winhttp.dll, doorstop_config.ini, BepInEx/) into the game directory.
  Nothing is overwritten unless you pass -Force. Asks before downloading.

.PARAMETER ValheimDir
  Game folder. Defaults to $env:VALHEIM_DIR or the Steam D: library.

.PARAMETER Version
  Package version on Thunderstore (default 5.4.2202).
#>
param(
    [string]$ValheimDir = $(if ($env:VALHEIM_DIR) { $env:VALHEIM_DIR } else { "D:\SteamLibrary\steamapps\common\Valheim" }),
    [string]$Version = "5.4.2202",
    [switch]$Force,
    [switch]$Yes
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path (Join-Path $ValheimDir "valheim.exe"))) {
    Write-Error "valheim.exe not found in '$ValheimDir'. Pass -ValheimDir or set VALHEIM_DIR."
}

if ((Test-Path (Join-Path $ValheimDir "BepInEx")) -and -not $Force) {
    Write-Host "BepInEx already present in $ValheimDir (use -Force to reinstall)." -ForegroundColor Yellow
    exit 0
}

$url = "https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/$Version/"
Write-Host "Will download: $url"
Write-Host "Install to:    $ValheimDir"
if (-not $Yes) {
    $answer = Read-Host "Continue? [y/N]"
    if ($answer -notmatch '^[Yy]') { Write-Host "Aborted."; exit 1 }
}

$tmp = Join-Path $env:TEMP "BepInExPack_Valheim_$Version"
$zip = "$tmp.zip"
if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }

Write-Host "Downloading..."
Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
Expand-Archive -Path $zip -DestinationPath $tmp -Force

$payload = Join-Path $tmp "BepInExPack_Valheim"
if (-not (Test-Path $payload)) { Write-Error "Unexpected zip layout: $payload missing" }

Write-Host "Copying into game folder..."
Copy-Item -Path (Join-Path $payload "*") -Destination $ValheimDir -Recurse -Force

Remove-Item -Recurse -Force $tmp
Remove-Item -Force $zip

Write-Host "Done. Launch Valheim once so BepInEx creates its config/plugins folders." -ForegroundColor Green
Write-Host "Then: dotnet build src\MjolnirMenu -c Release   (auto-deploys to BepInEx\plugins\MjolnirMenu)"
