# EaGPT — ArchiGPT for Sparx Enterprise Architect

In-EA chat pane that talks to a **local Ollama** LLM so you can analyze and change **ArchiMate** models with natural language.

This is the Sparx EA counterpart of [ArchiGPT](https://github.com/fideocam/Archi-LLM-plugin): same JSON mutation protocol and Ollama-first workflow, implemented as a Windows COM add-in (EA’s Automation API + ArchiMate 3 MDG), not an Eclipse plugin.

Similar commercial/official EA products already exist (OneRAI, Sparx Japan MCP, built-in AI Chat). None of them is an open-source in-EA Ollama add-in. See [docs/EXISTING_ALTERNATIVES.md](docs/EXISTING_ALTERNATIVES.md).

## What you can do

- Ask for analysis of the model, a diagram, or a selected element (including a deterministic **impact** walk and a compact **Mermaid** neighborhood of the selection)
- Add ArchiMate elements and relationships (illegal relationship types are rejected with suggestions)
- Create a new diagram; collapsed LLM coordinates are spread by ArchiMate layer
- Remove from the current diagram only, or from the model
- Remove a diagram (elements stay in the model)

Default Ollama endpoint: `http://localhost:11434`, model `llama3.2`. To use Ollama on **another machine on the LAN**, type its address in the EaGPT window (for example `http://192.168.1.10:11434` or `192.168.1.10`). That host must listen on the network (`OLLAMA_HOST=0.0.0.0`).

Ideas taken from public ArchiMate MCP / AI projects (query tools, legality, viewpoint recipes, Mermaid, audit log) are listed in [docs/ARCHIMATE_AI_LANDSCAPE.md](docs/ARCHIMATE_AI_LANDSCAPE.md). EaGPT remains an in-EA Ollama pane, not an MCP server.

## Build and install

Windows + Sparx EA required. Two separate guides:

- **[Build with Visual Studio](docs/BUILD_WINDOWS.md)** — workloads, open `EaGpt.sln`, Release DLL
- **[Install in Sparx EA](docs/INSTALL_SPARX.md)** — `regasm`, EA add-in key, first use, uninstall
- **[RAG / company knowledge](docs/RAG_OLLAMA.md)** — principles, CMDB, ArchiMate examples, tiedonhallintamalli with Ollama

If you already have Visual Studio and `dotnet` on the EA machine:

```powershell
.\scripts\install.ps1
```

Then in EA: **EaGPT → Show EaGPT View**.

## Repository layout

- `src/EaGpt.Core` — Ollama client, system prompt, JSON parser/validator, ArchiMate type map (testable on Linux)
- `src/EaGpt.AddIn` — .NET Framework 4.8 COM add-in, WinForms chat, EA Automation importer
- `tests/EaGpt.Core.Tests` — unit tests for the protocol layer
- `knowledge/` — template pack (principles, CMDB extract, ArchiMate examples, tiedonhallintamalli)
- `docs/` — install, security, RAG, and [ArchiMate AI landscape](docs/ARCHIMATE_AI_LANDSCAPE.md)
- `scripts/install.ps1` / `uninstall.ps1`

## Tests

Core protocol tests (parser, validator, relationship legality, impact/Mermaid context, Ollama URL policy, settings, JSON escaping) run on Linux:

```bash
dotnet test tests/EaGpt.Core.Tests/EaGpt.Core.Tests.csproj
```

The WinForms COM add-in needs Windows + EA; it is not executed in this environment.

## Security

EaGPT talks only to a user-configured Ollama origin, validates LLM JSON before touching EA, and asks before deleting from the model. Details, residual risks (prompt injection, LAN SSRF), and metadata-URL blocking: [docs/SECURITY.md](docs/SECURITY.md).

## License

MIT. Prompt/JSON ideas follow ArchiGPT; this is a new C# codebase, not a port of the Eclipse plugin.

**Attribution:** ArchiGPT / [Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin) by Raino Annala.
