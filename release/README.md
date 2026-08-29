# EaGPT — current release drop

This folder is the **shareable add-in** produced by `.\scripts\build.ps1`. Zip or copy the whole folder. Recipients do **not** need Visual Studio or the .NET SDK.

| File | Role |
| --- | --- |
| `EaGpt.AddIn.dll` | COM add-in EA loads |
| `EaGpt.Core.dll` | Must stay next to the add-in |
| `Install.ps1` | Register for the current Windows user |
| `Uninstall.ps1` | Unregister |
| `VERSION.txt` | Build stamp (written by the build script) |
| `EaGPT.zip` | Same files, for email / file share |

DLLs appear after a Release build. They are not stored in git.

## Install (on the EA PC)

1. Copy this folder to a **stable path** (for example `C:\EaGPT\release`). COM records that path — do not move it afterwards without installing again.
2. Close Enterprise Architect.
3. PowerShell (same Windows user who starts EA):

   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass
   .\Install.ps1
   ```

4. Restart EA → **EaGPT → Show EaGPT View**.

Needs 64-bit EA and .NET Framework 4.8 (already on current Windows). For 32-bit EA: `.\Install.ps1 -X86` and a matching x86 build.

Optional knowledge pack: copy repo `knowledge\` to `%AppData%\EaGpt\knowledge\`.

## How this folder is filled

On a machine with the .NET 8 SDK, from the repo root:

```powershell
.\scripts\build.ps1
```

That compiles Release and refreshes `release\`. To snapshot the same bits as the user drop:

```powershell
.\scripts\build.ps1 -PromoteToStable
```

That also writes `stable\` (see `stable\README.md`).
