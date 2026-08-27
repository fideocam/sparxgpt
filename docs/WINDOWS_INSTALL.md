# Install EaGPT on Windows

This add-in is a .NET Framework 4.8 COM object loaded by Sparx Enterprise Architect. Register it on the same machine as EA. Linux/macOS cannot host EA add-ins.

## Requirements

- Windows 10/11, 64-bit
- Sparx Enterprise Architect 15+ (64-bit recommended)
- .NET Framework 4.8 (included with Windows 10/11)
- [Ollama](https://ollama.com) running, with a model pulled (`ollama pull llama3.2`)
- Visual Studio 2022 or the .NET 8 SDK plus .NET Framework 4.8 targeting pack (to build)

Enable the **ArchiMate 3** MDG in EA (`Specialize → Manage Technologies`).

## Build

From a Developer Command Prompt or PowerShell:

```powershell
cd path\to\EaGPT
dotnet restore
dotnet build src/EaGpt.AddIn/EaGpt.AddIn.csproj -c Release
```

The DLL is `src/EaGpt.AddIn/bin/Release/net48/EaGpt.AddIn.dll`.

## Register (current user)

Run PowerShell **as the same Windows user who runs EA** (HKCU registration):

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\install.ps1
```

The script:

1. Builds the add-in if needed
2. Runs 64-bit `regasm /codebase` on `EaGpt.AddIn.dll`
3. Writes `HKCU\Software\Sparx Systems\EAAddins\EaGPT` = `EaGpt.AddIn.EaGptAddIn`

Restart Enterprise Architect. Check **Specialize → Add-In Windows** / **Manage Add-Ins** that **EaGPT** is enabled.

## Use

1. Start Ollama (`ollama serve` or the Ollama app).
2. Open a project in EA.
3. Menu **EaGPT → Show EaGPT View**.
4. Confirm the Ollama URL (`http://localhost:11434`) and pick a model.
5. Ask in natural language, for example:
   - `Add a Business Actor called Customer`
   - `Describe this diagram`
   - `What business processes use this application?`
   - `Remove this element from the diagram`
   - `Create a new diagram showing order fulfilment`

Selection in the Project Browser or on the open diagram is sent as context.

## Uninstall

```powershell
.\scripts\uninstall.ps1
```

Then restart EA.

## 32-bit EA

If you run 32-bit EA, rebuild with `-p:PlatformTarget=x86` and use 32-bit `regasm` from `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\regasm.exe`. `install.ps1 -X86` does this.

## Testing in this repo (Linux)

Core logic (parser, validator, type map, Ollama URL policy, settings, JSON helpers) is covered by:

```bash
dotnet test tests/EaGpt.Core.Tests/EaGpt.Core.Tests.csproj
```

Live EA COM tests must run on Windows with EA installed. See [SECURITY.md](SECURITY.md) for the threat model.
