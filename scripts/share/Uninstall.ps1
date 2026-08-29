#Requires -Version 5.1
param(
    [switch]$X86
)

$ErrorActionPreference = "Stop"
$dir = $PSScriptRoot
$dll = Join-Path $dir "EaGpt.AddIn.dll"

$framework = if ($X86) { "Framework" } else { "Framework64" }
$regasm = Join-Path $env:WINDIR "Microsoft.NET\$framework\v4.0.30319\regasm.exe"
if ((Test-Path $regasm) -and (Test-Path $dll)) {
    & $regasm $dll /unregister
}

$regPath = "HKCU:\Software\Sparx Systems\EAAddins\EaGPT"
if (Test-Path $regPath) {
    Remove-Item -Path $regPath -Recurse -Force
}

Write-Host "EaGPT unregistered. Restart Enterprise Architect."
