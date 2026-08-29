# EaGPT — stable (user) drop

This folder is the **last promoted** shareable add-in. Use it when you want a known-good build for other people, while `release\` can keep moving with every compile.

Fill it from the repo root (needs the .NET 8 SDK on the build PC):

```powershell
.\scripts\build.ps1 -PromoteToStable
```

Then copy or zip **this whole folder** (or send `EaGPT.zip`). Recipients do **not** need Visual Studio.

## Install (on the EA PC)

1. Copy this folder to a **stable path** (for example `C:\EaGPT\stable`). Do not move it after install.
2. Close Enterprise Architect.
3. PowerShell (same Windows user who starts EA):

   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass
   .\Install.ps1
   ```

4. Restart EA → **EaGPT → Show EaGPT View**.

Uninstall: `.\Uninstall.ps1` (close EA first). 32-bit EA: `.\Install.ps1 -X86`.
