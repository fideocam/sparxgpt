# Build EaGPT on Windows (Visual Studio Code or Visual Studio)

This guide is for **building** the Sparx EA add-in on a Windows PC. **Visual Studio Code is enough**; you do not need the full Visual Studio IDE. Building does not install the add-in into Enterprise Architect. After a successful build, follow [INSTALL_SPARX.md](INSTALL_SPARX.md).

Linux and macOS cannot produce a usable EA add-in. EA loads a Windows COM DLL.

## Visual Studio Code (recommended if that is what you have)

### 1. Install the .NET 8 SDK

VS Code does not include a C# compiler. Install the SDK, then **close and reopen** the terminal (and VS Code).

In **PowerShell** (Run as your normal user):

```powershell
winget install Microsoft.DotNet.SDK.8
```

Or download **.NET 8 SDK** (not the Runtime-only installer) from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0).

Check:

```powershell
dotnet --version
```

You should see `8.0.x` or later. If the command is not found, the SDK is missing or the terminal was opened before install — restart VS Code.

The add-in targets **.NET Framework 4.8**. The project already pulls `Microsoft.NETFramework.ReferenceAssemblies.net48` from NuGet, so a separate 4.8 targeting pack is usually not required for `dotnet build`. If the add-in project still fails to load, install [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48).

### 2. Open the repo root (this is why the IDE finds no code)

**File → Open Folder…** and choose the folder that contains **`EaGpt.sln`** (the cloned `sparxgpt` root). Do not open a parent directory, `src` alone, or `knowledge`.

Then install the prompted extensions (or **Extensions**: **C# Dev Kit** and **C#**). Reload the window if asked. The Solution Explorer should list **EaGpt.Core**, **EaGpt.AddIn**, and **EaGpt.Core.Tests**.

### 3. Trigger the build

Any of these:

| Action | How |
| --- | --- |
| Default build | `Ctrl+Shift+B` (Run Build Task) — builds the **Release** add-in |
| Command Palette | `Ctrl+Shift+P` → **Tasks: Run Build Task** |
| Terminal | see commands below |

```powershell
cd path\to\sparxgpt
dotnet restore EaGpt.sln
dotnet build src\EaGpt.AddIn\EaGpt.AddIn.csproj -c Release
```

Output DLL:

```
src\EaGpt.AddIn\bin\Release\net48\EaGpt.AddIn.dll
```

If VS Code says there is **no build task**, the `.vscode` folder from this repo is missing — open the GitHub clone, not a copy of only the Markdown files.

### Typical “IDE does not find code”

| Symptom | Cause |
| --- | --- |
| No projects / “no build task” | Folder is not the repo root, or `.vscode` / `EaGpt.sln` is not in that folder |
| Projects fail to load / SDK not found | `.NET 8 SDK` not installed, or VS Code not restarted after `winget` |
| C# files have no IntelliSense | **C# Dev Kit** extension missing; install it and reload |
| `dotnet` works in PowerShell but not in VS Code | Restart VS Code so it picks up PATH |

## What you will produce

| Output | Path |
| --- | --- |
| Add-in DLL (this is what EA loads) | `src\EaGpt.AddIn\bin\Release\net48\EaGpt.AddIn.dll` |
| Core library (copied next to the DLL) | `src\EaGpt.AddIn\bin\Release\net48\EaGpt.Core.dll` |

You do **not** need Sparx’s `Interop.EA.dll`. The add-in talks to EA through late-bound COM.

## Visual Studio 2022 (optional)

Use **Visual Studio 2022** (Community is enough) or newer if you prefer that IDE instead of VS Code.

1. Open **Visual Studio Installer**.
2. Modify your installation and enable the workload **.NET desktop development**.
3. On the right, under Individual components, confirm these are selected:
   - **.NET Framework 4.8 targeting pack**
   - **.NET Framework 4.8 SDK**
   - **.NET 8.0 Runtime** (or SDK) — needed to restore SDK-style projects and to run unit tests
4. Install / Modify, then restart the PC if the installer asks.

The **.NET 8 SDK** is also available separately from [dotnet.microsoft.com](https://dotnet.microsoft.com/download) if Visual Studio did not add it.

Check from **Developer PowerShell for VS 2022**:

```powershell
dotnet --version
```

You should see `8.0.x` or later.

### Get the source

Clone or unzip the repository so you have a folder that contains `EaGpt.sln`.

```powershell
git clone https://github.com/fideocam/sparxgpt.git
cd sparxgpt
```

### Open the solution

1. Start Visual Studio.
2. **File → Open → Project/Solution…**
3. Open `EaGpt.sln` at the repo root.

You should see three projects:

| Project | Role |
| --- | --- |
| **EaGpt.Core** | Parser, Ollama client, ArchiMate types (netstandard2.0) |
| **EaGpt.AddIn** | WinForms COM add-in for EA (net48, x64) |
| **EaGpt.Core.Tests** | Unit tests (net8.0) |

If NuGet restore fails on first open: **Tools → NuGet Package Manager → Package Manager Settings** and allow nuget.org. Then right-click the solution → **Restore NuGet Packages**.

### Build Release (recommended)

EA should load the Release DLL unless you are debugging.

1. Set the configuration dropdown (toolbar) to **Release**, platform **Any CPU**.
   - `EaGpt.AddIn` still compiles as **x64** (`PlatformTarget` in the csproj). That matches 64-bit EA.
2. **Build → Rebuild Solution** (or press `Ctrl+Shift+B` after a Clean).
3. Confirm the Output window ends with **Build succeeded**.

The add-in DLL is:

```
src\EaGpt.AddIn\bin\Release\net48\EaGpt.AddIn.dll
```

Copy that whole `net48` folder if you move the build to another machine. Keep `EaGpt.AddIn.dll` and `EaGpt.Core.dll` together.

### Command line (same result)

From **Developer PowerShell for VS 2022**, in the repo root:

```powershell
dotnet restore EaGpt.sln
dotnet build src\EaGpt.AddIn\EaGpt.AddIn.csproj -c Release
```

### Run the unit tests (optional)

These tests do not start EA. They cover the JSON protocol, type map, and Ollama URL checks.

In Visual Studio: **Test → Run All Tests**.

Or:

```powershell
dotnet test tests\EaGpt.Core.Tests\EaGpt.Core.Tests.csproj
```

## 32-bit Enterprise Architect

Only if **Help → About EA** says you are on 32-bit EA.

In Visual Studio:

1. Open `src\EaGpt.AddIn\EaGpt.AddIn.csproj`.
2. Change `<PlatformTarget>x64</PlatformTarget>` to `<PlatformTarget>x86</PlatformTarget>`.
3. Rebuild Release.

Or from Developer PowerShell:

```powershell
dotnet build src\EaGpt.AddIn\EaGpt.AddIn.csproj -c Release -p:PlatformTarget=x86
```

Then use `.\scripts\install.ps1 -X86` as described in [INSTALL_SPARX.md](INSTALL_SPARX.md).

## Typical build problems

| Symptom | What to do |
| --- | --- |
| Project will not load / “SDK not found” | Install the **.NET 8 SDK** (`winget install Microsoft.DotNet.SDK.8`), restart VS Code / Visual Studio, then reopen the folder that contains `EaGpt.sln`. |
| `net48` targeting pack missing | Visual Studio Installer → Individual components → **.NET Framework 4.8 targeting pack**. |
| WinForms / `System.Windows.Forms` errors | Enable **.NET desktop development**. `EaGpt.AddIn` sets `UseWindowsForms`. |
| Restore cannot reach nuget.org | Check proxy/firewall; restore `Microsoft.NETFramework.ReferenceAssemblies.net48`. |
| Built DLL is in `bin\Debug` | Switch configuration to **Release** and rebuild before installing into EA. |

Building does **not** register the add-in. Continue with [INSTALL_SPARX.md](INSTALL_SPARX.md).
