#Requires -Version 5.1
param(
    [switch]$X86,
    [ValidateSet("release", "stable")]
    [string]$Channel = "release",
    [switch]$SkipBuild,
    [switch]$PromoteToStable
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$build = Join-Path $PSScriptRoot "build.ps1"
$dll = Join-Path $root "$Channel\EaGpt.AddIn.dll"

if (-not $SkipBuild) {
    $buildArgs = @{ Configuration = "Release" }
    if ($X86) { $buildArgs.X86 = $true }
    if ($PromoteToStable -or $Channel -eq "stable") { $buildArgs.PromoteToStable = $true }
    & $build @buildArgs
}

if (-not (Test-Path $dll)) {
    throw "Add-in not found: $dll. Run .\scripts\build.ps1$(if ($Channel -eq 'stable') { ' -PromoteToStable' }) first, or install.ps1 without -SkipBuild."
}

$install = Join-Path $root "$Channel\Install.ps1"
if (Test-Path $install) {
    if ($X86) { & $install -X86 } else { & $install }
} else {
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
}
