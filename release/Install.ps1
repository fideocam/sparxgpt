#Requires -Version 5.1
<#
.SYNOPSIS
  Register the EaGPT add-in from this folder (no Visual Studio / SDK required).
#>
param(
    [switch]$X86
)

$ErrorActionPreference = "Stop"
$dir = $PSScriptRoot
$dll = Join-Path $dir "EaGpt.AddIn.dll"
$core = Join-Path $dir "EaGpt.Core.dll"

if (-not (Test-Path $dll)) {
    throw "EaGpt.AddIn.dll not found in $dir. On a build machine run .\scripts\build.ps1, then copy this whole folder."
}
if (-not (Test-Path $core)) {
    throw "EaGpt.Core.dll not found in $dir. Keep it next to EaGpt.AddIn.dll."
}

$framework = if ($X86) { "Framework" } else { "Framework64" }
$regasm = Join-Path $env:WINDIR "Microsoft.NET\$framework\v4.0.30319\regasm.exe"
if (-not (Test-Path $regasm)) {
    throw "regasm not found: $regasm"
}

Write-Host "Registering COM ($regasm)..."
Write-Host "  $dll"
& $regasm $dll /codebase /tlb
if ($LASTEXITCODE -ne 0) { throw "regasm failed" }

$regPath = "HKCU:\Software\Sparx Systems\EAAddins\EaGPT"
New-Item -Path $regPath -Force | Out-Null
Set-ItemProperty -Path $regPath -Name "(default)" -Value "EaGpt.AddIn.EaGptAddIn"

Write-Host "Registered HKCU\Software\Sparx Systems\EAAddins\EaGPT = EaGpt.AddIn.EaGptAddIn"
Write-Host "Leave this folder in place (COM /codebase records the path)."
Write-Host "Restart Enterprise Architect, then use EaGPT -> Show EaGPT View."
