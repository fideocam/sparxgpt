#Requires -Version 5.1
param(
    [switch]$X86,
    [ValidateSet("release", "stable", "auto")]
    [string]$Channel = "auto"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Find-ChannelDir {
    $candidates = @()
    if ($Channel -eq "auto") {
        $candidates = @(
            (Join-Path $root "release"),
            (Join-Path $root "stable")
        )
    } else {
        $candidates = @(Join-Path $root $Channel)
    }
    foreach ($dir in $candidates) {
        if (Test-Path (Join-Path $dir "Uninstall.ps1")) { return $dir }
        if (Test-Path (Join-Path $dir "EaGpt.AddIn.dll")) { return $dir }
    }
    return $null
}

$dir = Find-ChannelDir
$uninstall = $null
if ($dir) {
    $uninstall = Join-Path $dir "Uninstall.ps1"
    $shareUninstall = Join-Path $PSScriptRoot "share\Uninstall.ps1"
    if (-not (Test-Path $uninstall) -and (Test-Path $shareUninstall)) {
        Copy-Item $shareUninstall $uninstall -Force
    }
}

if ($uninstall -and (Test-Path $uninstall)) {
    if ($X86) { & $uninstall -X86 } else { & $uninstall }
    return
}

# Fallback: drop the HKCU keys even if the share script is missing.
$progId = "EaGpt.AddIn.EaGptAddIn"
$clsid = "{B7C4A1E2-3F58-4D9A-9C2B-8E1D6A0F4B31}"
foreach ($path in @(
        "HKCU:\Software\Classes\CLSID\$clsid",
        "HKCU:\Software\Classes\Wow6432Node\CLSID\$clsid",
        "HKCU:\Software\Classes\$progId",
        "HKCU:\Software\Sparx Systems\EAAddins\EaGPT"
    )) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

Write-Host "EaGPT unregistered for this Windows user. Restart Enterprise Architect."
