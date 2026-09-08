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
$shareInstall = Join-Path $PSScriptRoot "share\Install.ps1"
if (-not (Test-Path $install) -and (Test-Path $shareInstall)) {
    Copy-Item $shareInstall $install -Force
}
if (-not (Test-Path $install)) {
    throw "Install script not found: $install"
}

if ($X86) { & $install -X86 } else { & $install }
