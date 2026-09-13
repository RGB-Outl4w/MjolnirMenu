<#
.SYNOPSIS
  Build MjolnirMenu (Release) and copy it into Valheim's BepInEx/plugins folder.
#>
param(
    [string]$ValheimDir = $(if ($env:VALHEIM_DIR) { $env:VALHEIM_DIR } else { "D:\SteamLibrary\steamapps\common\Valheim" }),
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "src\MjolnirMenu"
$plugins = Join-Path $ValheimDir "BepInEx\plugins"

if (-not (Test-Path $plugins)) {
    Write-Error "BepInEx not installed in '$ValheimDir'. Run scripts\install-bepinex.ps1 first."
}

dotnet build $proj -c $Configuration -p:ValheimDir="$ValheimDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$dest = Join-Path $plugins "MjolnirMenu"
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $proj "bin\$Configuration\net462\MjolnirMenu.dll") $dest -Force
Copy-Item (Join-Path $proj "bin\$Configuration\net462\MjolnirMenu.pdb") $dest -Force -ErrorAction SilentlyContinue

Write-Host "Deployed to $dest" -ForegroundColor Green
