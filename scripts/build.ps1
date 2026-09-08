#Requires -Version 5.1
<#
.SYNOPSIS
  Build the EaGPT add-in and stage a shareable drop in release\ (optionally stable\).

.EXAMPLE
  .\scripts\build.ps1
  .\scripts\build.ps1 -PromoteToStable
#>
param(
    [switch]$X86,
    [string]$Configuration = "Release",
    [switch]$PromoteToStable,
    [switch]$SkipCompile
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $root "src\EaGpt.AddIn\EaGpt.AddIn.csproj"
$shareScripts = Join-Path $PSScriptRoot "share"
$releaseDir = Join-Path $root "release"
$stableDir = Join-Path $root "stable"
$platform = "x64"
if ($X86) { $platform = "x86" }

function Get-GitCommit([string]$repo) {
    try {
        $out = & git -C $repo rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $out) { return [string]$out.Trim() }
    } catch { }
    return "unknown"
}

function Stage-ShareFolder {
    param(
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$BinDir,
        [Parameter(Mandatory = $true)][string]$Channel
    )

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    $required = @("EaGpt.AddIn.dll", "EaGpt.Core.dll")
    foreach ($name in $required) {
        $src = Join-Path $BinDir $name
        if (-not (Test-Path $src)) { throw "Missing $src" }
        Copy-Item $src (Join-Path $Destination $name) -Force
    }

    Copy-Item (Join-Path $shareScripts "Install.ps1") (Join-Path $Destination "Install.ps1") -Force
    Copy-Item (Join-Path $shareScripts "Uninstall.ps1") (Join-Path $Destination "Uninstall.ps1") -Force

    $version = "1.0.0"
    $verNode = Select-Xml -Path $csproj -XPath "//Version" | Select-Object -First 1
    if ($verNode) { $version = $verNode.Node.InnerText.Trim() }

    $stamp = @(
        "EaGPT $version"
        "Channel: $Channel"
        "Configuration: $Configuration"
        "Platform: $platform"
        "BuiltUtc: $([DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))"
        "Commit: $(Get-GitCommit $root)"
        "Files: EaGpt.AddIn.dll, EaGpt.Core.dll"
        "Share this whole folder (or EaGPT.zip). Recipients run .\Install.ps1 — no .NET SDK required."
    ) -join [Environment]::NewLine
    Set-Content -Path (Join-Path $Destination "VERSION.txt") -Value $stamp -Encoding UTF8

    $zip = Join-Path $Destination "EaGPT.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    $stage = Join-Path ([IO.Path]::GetTempPath()) ("eagpt-share-" + $Channel + "-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    try {
        Copy-Item (Join-Path $Destination "EaGpt.AddIn.dll") $stage
        Copy-Item (Join-Path $Destination "EaGpt.Core.dll") $stage
        Copy-Item (Join-Path $Destination "Install.ps1") $stage
        Copy-Item (Join-Path $Destination "Uninstall.ps1") $stage
        Copy-Item (Join-Path $Destination "VERSION.txt") $stage
        $readme = Join-Path $Destination "README.md"
        if (Test-Path $readme) { Copy-Item $readme $stage }
        Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -Force
    } finally {
        Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Staged $Channel -> $Destination"
    Write-Host "  Zip: $zip"
}

if (-not $SkipCompile) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet not found. Install the .NET 8 SDK, or copy an existing release\ folder instead."
    }
    if ($Configuration -ne "Release") {
        Write-Warning "Share folders are meant for Release. Building Configuration=$Configuration anyway."
    }
    Write-Host "Building EaGPT add-in ($Configuration, $platform)..."
    & dotnet build $csproj -c $Configuration -p:PlatformTarget=$platform
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

$binDir = Join-Path $root "src\EaGpt.AddIn\bin\$Configuration\net48"
if (-not (Test-Path (Join-Path $binDir "EaGpt.AddIn.dll"))) {
    throw "Add-in DLL not found: $binDir\EaGpt.AddIn.dll"
}

Stage-ShareFolder -Destination $releaseDir -BinDir $binDir -Channel "release"

if ($PromoteToStable) {
    Stage-ShareFolder -Destination $stableDir -BinDir $binDir -Channel "stable"
}

Write-Host "Done. Share the release\ folder (or release\EaGPT.zip). Promote a user drop with -PromoteToStable."
