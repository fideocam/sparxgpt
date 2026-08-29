# Install EaGPT in Sparx Enterprise Architect

This guide is for **registering and using** EaGPT inside Sparx EA on Windows. To compile the DLL first, see [BUILD_WINDOWS.md](BUILD_WINDOWS.md).

The add-in is a .NET Framework 4.8 COM object. Register it on the **same Windows machine and Windows user account** that runs EA.

## What you need

- Windows 10 or 11
- [Sparx Enterprise Architect](https://sparxsystems.com) 15 or later (64-bit recommended)
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) (already on current Windows 10/11)
- [Ollama](https://ollama.com) installed, with a model available, for example:

  ```powershell
  ollama pull llama3.2
  ```

- A built add-in in **`release\`** (from [BUILD_WINDOWS.md](BUILD_WINDOWS.md) / `.\scripts\build.ps1`), or the promoted **`stable\`** drop. Recipients of a copied folder only need that folder’s `Install.ps1` — no SDK.

Enable the **ArchiMate 3** MDG in EA: **Specialize → Manage Technologies** (or **Configure → Manage Technologies** on some EA versions) and tick **ArchiMate 3**.

## 1. Close Enterprise Architect

Exit EA completely before registering. COM registration cannot replace a DLL that EA still has loaded.

## 2. Register the add-in

Open **PowerShell** as the **same Windows user who starts EA** (normal user is enough; you do not need an Administrator prompt for HKCU registration).

```powershell
cd path\to\sparxgpt
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\install.ps1
```

The script:

1. Runs `scripts\build.ps1` (Release → `release\`) if `dotnet` is available
2. Runs 64-bit `regasm.exe /codebase` on `release\EaGpt.AddIn.dll`
3. Writes the EA add-in key:

   `HKCU\Software\Sparx Systems\EAAddins\EaGPT` = `EaGpt.AddIn.EaGptAddIn`

Register a promoted user drop: `.\scripts\install.ps1 -Channel stable` (build it first with `.\scripts\build.ps1 -PromoteToStable`).

You should see a line that the add-in is registered, then: restart EA and use **EaGPT → Show EaGPT View**.

### 32-bit EA

If About EA shows a 32-bit build, register with:

```powershell
.\scripts\install.ps1 -X86
```

The DLL must have been built as x86 (see [BUILD_WINDOWS.md](BUILD_WINDOWS.md)).

### Already built, no Visual Studio on this PC

Copy the **`release\`** or **`stable\`** folder (or unzip `EaGPT.zip`) to a path that will not change, then:

```powershell
cd C:\EaGPT\release
Set-ExecutionPolicy -Scope Process Bypass
.\Install.ps1
```

That registers COM from the DLLs in the same folder. Do not move the folder after install without running `Install.ps1` again.

To skip compiling on a machine that already has `release\`:

```powershell
.\scripts\install.ps1 -SkipBuild
```

## 3. Enable it in EA

1. Start Enterprise Architect.
2. Open any project (or create a test project).
3. **Specialize → Manage Add-Ins** (menu names vary slightly by EA version: **Extend → Manage Add-Ins** on some builds).
4. Confirm **EaGPT** is listed and **Enabled**.
5. Menu **EaGPT → Show EaGPT View**.

A window titled **EaGPT** should open (Ollama URL, model list, chat).

If the **EaGPT** menu is missing:

- Confirm the registry value exists (step 2).
- Confirm 64-bit vs 32-bit match (EA bitness, `regasm`, DLL `PlatformTarget`).
- Look at EA’s add-in list for a load error.
- Rebuild Release and run `install.ps1` again after closing EA.

## 4. First use

1. Start **Ollama** (`ollama serve`) or **LM Studio** (Start server on port 1234).
2. In the EaGPT window, the **LLM** field is the server URL:
   - Ollama on this PC: leave `http://localhost:11434`.
   - LM Studio on this PC: `http://localhost:1234` (path `/v1` is stripped; EaGPT talks to `/v1/models` and `/v1/chat/completions`).
   - **Another computer on the LAN:** `http://192.168.1.10:11434` or just `192.168.1.10` (port 11434 is added if you omit it). That machine must bind Ollama to the network, for example:

     ```powershell
     $env:OLLAMA_HOST = "0.0.0.0"
     ollama serve
     ```

     Allow TCP 11434 through the Windows Firewall on the Ollama host. HTTPS reverse proxies without a port stay on 443.
3. Click **Refresh list** and pick a model (for example `llama3.2`).
4. Click **Test** — you should see that the LLM is reachable.
5. Open or select an ArchiMate package/diagram.
6. Type a request (or pick an example prompt) and click **Ask EaGPT**, for example:
   - `Add a Business Actor called Customer`
   - `Describe this diagram`
   - `Audit this model for quality issues`
   - `What depends on the selected element?`
   - `Find application components named CRM`
   - `Create a new diagram showing order fulfilment`

Selection in the Project Browser or on the open diagram is sent as context.

Deletes from the **model** (elements, relationships, whole diagrams) ask for confirmation. Remove-from-diagram-only does not.

Settings are stored in `%AppData%\EaGpt\settings.ini`.

Optional **company knowledge** (principles, CMDB extract, example viewpoints, tiedonhallintamalli): copy the repo `knowledge\` folder to `%AppData%\EaGpt\knowledge\` (or set `KnowledgeFolder` in settings.ini). See [RAG_OLLAMA.md](RAG_OLLAMA.md).

## 5. Uninstall

Close EA, then:

```powershell
cd path\to\sparxgpt
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\uninstall.ps1
```

For 32-bit EA: `.\scripts\uninstall.ps1 -X86`.

Restart EA. The **EaGPT** menu should be gone.

## 6. Troubleshooting

| Symptom | What to check |
| --- | --- |
| No EaGPT menu | Registry key, add-in enabled, EA restarted, 32/64-bit match. |
| “Retrieving the COM class factory failed” | `regasm /codebase` was not run, or the DLL moved. Re-run `install.ps1`. |
| BadImageFormatException | 32-bit EA with an x64 DLL (or the reverse). Rebuild and `install.ps1 -X86` if needed. |
| Test cannot reach Ollama | Ollama is running; URL is `http://localhost:11434` or a LAN address; on a LAN host set `OLLAMA_HOST=0.0.0.0` and open firewall TCP 11434. Try `ollama list` on the server. |
| Model list empty | Pull a model (`ollama pull llama3.2`), then **Refresh list**. |
| Changes not applied | Reply may be analysis (plain text). For adds, the model must return JSON. ArchiMate 3 MDG must be enabled. Open a package as the create target. |
| Elements have the wrong type | Enable ArchiMate 3 MDG; types are `ArchiMate3::ArchiMate_…`. |
| Access denied on `regasm` | You do not need Administrator for HKCU. If `regasm` itself fails, run PowerShell normally (not redirected to a protected folder). |

More on URLs, confirmation prompts, and residual LLM risks: [SECURITY.md](SECURITY.md).
