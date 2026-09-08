#Requires -Version 5.1
<#
.SYNOPSIS
  Register the EaGPT add-in from this folder (no Visual Studio / SDK required).

  COM is written under HKCU\Software\Classes so a normal (non-admin) user
  can install. Machine-wide regasm /codebase is not used.
#>
param(
    [switch]$X86
)

$ErrorActionPreference = "Stop"
$dir = $PSScriptRoot
$dll = Join-Path $dir "EaGpt.AddIn.dll"
$core = Join-Path $dir "EaGpt.Core.dll"
$progId = "EaGpt.AddIn.EaGptAddIn"
$clsid = "{B7C4A1E2-3F58-4D9A-9C2B-8E1D6A0F4B31}"
$netCategory = "{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}"

if (-not (Test-Path $dll)) {
    throw "EaGpt.AddIn.dll not found in $dir. On a build machine run .\scripts\build.ps1, then copy this whole folder."
}
if (-not (Test-Path $core)) {
    throw "EaGpt.Core.dll not found in $dir. Keep it next to EaGpt.AddIn.dll."
}

function Get-ClassesClsidPath {
    param([switch]$IsX86, [string]$Guid)
    if ($IsX86) {
        return "HKCU:\Software\Classes\Wow6432Node\CLSID\$Guid"
    }
    return "HKCU:\Software\Classes\CLSID\$Guid"
}

function Ensure-Key([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -Path $Path -Force | Out-Null
    }
}

function Set-DefaultValue([string]$Path, [string]$Value) {
    Ensure-Key $Path
    Set-ItemProperty -LiteralPath $Path -Name "(default)" -Value $Value
}

function Register-PerUserCom {
    param(
        [Parameter(Mandatory = $true)][string]$DllPath,
        [switch]$IsX86
    )

    $full = [IO.Path]::GetFullPath($DllPath)
    $codeBase = ([Uri]$full).AbsoluteUri
    $assembly = "EaGpt.AddIn, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    $version = "1.0.0.0"
    $runtime = "v4.0.30319"
    try {
        $an = [Reflection.AssemblyName]::GetAssemblyName($full)
        if ($an) {
            $assembly = $an.FullName
            $version = $an.Version.ToString()
            if ($an.Version.Revision -lt 0) {
                $version = "{0}.{1}.{2}.0" -f $an.Version.Major, $an.Version.Minor, [Math]::Max($an.Version.Build, 0)
            }
        }
    } catch {
        # Keep the csproj defaults if metadata cannot be read.
    }

    $clsidPath = Get-ClassesClsidPath -IsX86:$IsX86 -Guid $clsid
    $inproc = Join-Path $clsidPath "InprocServer32"
    $inprocVer = Join-Path $inproc $version
    $progIdKey = Join-Path $clsidPath "ProgId"
    $cat = Join-Path $clsidPath "Implemented Categories\$netCategory"
    $progRoot = "HKCU:\Software\Classes\$progId"

    Set-DefaultValue $progRoot $progId
    Set-DefaultValue (Join-Path $progRoot "CLSID") $clsid

    Set-DefaultValue $clsidPath $progId
    Set-DefaultValue $inproc "mscoree.dll"
    Set-ItemProperty -LiteralPath $inproc -Name "ThreadingModel" -Value "Both"
    Set-ItemProperty -LiteralPath $inproc -Name "Class" -Value $progId
    Set-ItemProperty -LiteralPath $inproc -Name "Assembly" -Value $assembly
    Set-ItemProperty -LiteralPath $inproc -Name "RuntimeVersion" -Value $runtime
    Set-ItemProperty -LiteralPath $inproc -Name "CodeBase" -Value $codeBase

    Set-DefaultValue $inprocVer ""
    Set-ItemProperty -LiteralPath $inprocVer -Name "Class" -Value $progId
    Set-ItemProperty -LiteralPath $inprocVer -Name "Assembly" -Value $assembly
    Set-ItemProperty -LiteralPath $inprocVer -Name "RuntimeVersion" -Value $runtime
    Set-ItemProperty -LiteralPath $inprocVer -Name "CodeBase" -Value $codeBase

    Set-DefaultValue $progIdKey $progId
    Ensure-Key $cat

    Write-Host "Registered per-user COM ($clsidPath)"
    Write-Host "  $full"
}

function Register-EaAddin {
    $regPath = "HKCU:\Software\Sparx Systems\EAAddins\EaGPT"
    Set-DefaultValue $regPath $progId
    Write-Host "Registered HKCU\Software\Sparx Systems\EAAddins\EaGPT = $progId"
}

$bitness = if ($X86) { "32-bit" } else { "64-bit" }
Write-Host "Registering EaGPT for the current Windows user ($bitness COM, no admin regasm)..."
Register-PerUserCom -DllPath $dll -IsX86:$X86
Register-EaAddin

Write-Host "Leave this folder in place (COM CodeBase records the path)."
Write-Host "Restart Enterprise Architect, then use EaGPT -> Show EaGPT View."
