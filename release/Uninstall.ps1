#Requires -Version 5.1
param(
    [switch]$X86
)

$ErrorActionPreference = "Stop"
$progId = "EaGpt.AddIn.EaGptAddIn"
$clsid = "{B7C4A1E2-3F58-4D9A-9C2B-8E1D6A0F4B31}"

function Remove-KeyIfPresent([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

# Per-user COM (do not call regasm /unregister — that writes HKLM and needs admin).
Remove-KeyIfPresent "HKCU:\Software\Classes\CLSID\$clsid"
Remove-KeyIfPresent "HKCU:\Software\Classes\Wow6432Node\CLSID\$clsid"
Remove-KeyIfPresent "HKCU:\Software\Classes\$progId"

foreach ($name in @("EAAddins", "EAAddins64")) {
    Remove-KeyIfPresent "HKCU:\Software\Sparx Systems\$name\EaGPT"
}

Write-Host "EaGPT unregistered for this Windows user. Restart Enterprise Architect."
