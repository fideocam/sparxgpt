#Requires -Version 5.1
param(
    [switch]$X86,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $root "src\EaGpt.AddIn\EaGpt.AddIn.csproj"

Write-Host "Building EaGPT add-in..."
$platform = "x64"
if ($X86) { $platform = "x86" }

dotnet build $csproj -c $Configuration -p:PlatformTarget=$platform
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$dll = Join-Path $root "src\EaGpt.AddIn\bin\$Configuration\net48\EaGpt.AddIn.dll"
if (-not (Test-Path $dll)) { throw "DLL not found: $dll" }

$framework = if ($X86) { "Framework" } else { "Framework64" }
$regasm = Join-Path $env:WINDIR "Microsoft.NET\$framework\v4.0.30319\regasm.exe"
if (-not (Test-Path $regasm)) { throw "regasm not found: $regasm" }

Write-Host "Registering COM ($regasm)..."
& $regasm $dll /codebase /tlb
if ($LASTEXITCODE -ne 0) { throw "regasm failed" }

$regPath = "HKCU:\Software\Sparx Systems\EAAddins\EaGPT"
New-Item -Path $regPath -Force | Out-Null
Set-ItemProperty -Path $regPath -Name "(default)" -Value "EaGpt.AddIn.EaGptAddIn"

Write-Host "Registered HKCU\Software\Sparx Systems\EAAddins\EaGPT = EaGpt.AddIn.EaGptAddIn"
Write-Host "Restart Enterprise Architect, then use EaGPT -> Show EaGPT View."
