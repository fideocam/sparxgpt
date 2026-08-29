#Requires -Version 5.1
param(
    [switch]$X86,
    [ValidateSet("release", "stable", "auto")]
    [string]$Channel = "auto"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Find-AddInDll {
    $candidates = @()
    if ($Channel -eq "auto") {
        $candidates = @(
            (Join-Path $root "release\EaGpt.AddIn.dll"),
            (Join-Path $root "stable\EaGpt.AddIn.dll"),
            (Join-Path $root "src\EaGpt.AddIn\bin\Release\net48\EaGpt.AddIn.dll"),
            (Join-Path $root "src\EaGpt.AddIn\bin\Debug\net48\EaGpt.AddIn.dll")
        )
    } else {
        $candidates = @(Join-Path $root "$Channel\EaGpt.AddIn.dll")
    }
    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }
    return $null
}

$dll = Find-AddInDll
$framework = if ($X86) { "Framework" } else { "Framework64" }
$regasm = Join-Path $env:WINDIR "Microsoft.NET\$framework\v4.0.30319\regasm.exe"
if ((Test-Path $regasm) -and $dll) {
    & $regasm $dll /unregister
}

$regPath = "HKCU:\Software\Sparx Systems\EAAddins\EaGPT"
if (Test-Path $regPath) {
    Remove-Item -Path $regPath -Recurse -Force
}

Write-Host "EaGPT unregistered. Restart Enterprise Architect."
