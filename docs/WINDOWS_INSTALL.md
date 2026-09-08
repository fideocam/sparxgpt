# Windows notes

EaGPT is a Windows COM add-in for Sparx Enterprise Architect.

- **Build** (VS Code or Visual Studio): [BUILD_WINDOWS.md](BUILD_WINDOWS.md) — `.\scripts\build.ps1` writes `release\` (share that folder or `EaGPT.zip`)
- **Install and use in EA**: [INSTALL_SPARX.md](INSTALL_SPARX.md)
- **Security**: [SECURITY.md](SECURITY.md)

The two guides are separate on purpose: you can compile on a developer PC, copy `release\` or `stable\` to users, then register with that folder’s `Install.ps1`.
