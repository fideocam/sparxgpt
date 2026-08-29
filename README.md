# EaGPT — ArchiGPT for Sparx Enterprise Architect

In-EA chat pane that talks to a **local Ollama** LLM so you can analyze and change **ArchiMate** models with natural language.

This is the Sparx EA counterpart of [ArchiGPT](https://github.com/fideocam/Archi-LLM-plugin): same JSON mutation protocol and Ollama-first workflow, implemented as a Windows COM add-in (EA’s Automation API + ArchiMate 3 MDG), not an Eclipse plugin.

Similar commercial/official EA products already exist (OneRAI, Sparx Japan MCP, built-in AI Chat). None of them is an open-source in-EA Ollama add-in. See [docs/EXISTING_ALTERNATIVES.md](docs/EXISTING_ALTERNATIVES.md).

## What you can do

- Ask for analysis of the model, a diagram, or a selected element (including a deterministic **impact** walk, **quality audit**, **search hits**, and a compact **Mermaid** neighborhood)
- Follow up in the same chat (last few turns are sent; **Clear chat** resets)
- Add ArchiMate elements and relationships (illegal relationship types are rejected with suggestions)
- Create a new diagram; collapsed LLM coordinates are spread by ArchiMate layer
- Remove from the current diagram only, or from the model
- Remove a diagram (elements stay in the model)

Default LLM: Ollama at `http://localhost:11434`, model `llama3.2`. **LM Studio** works at `http://localhost:1234` (OpenAI-compatible `/v1`). To use a server on **another machine on the LAN**, type its address (for example `http://192.168.1.10:11434` or `192.168.1.10`). That host must listen on the network (`OLLAMA_HOST=0.0.0.0`).

Ideas taken from public ArchiMate MCP / AI projects and from commercial EA assistants are listed in [docs/ARCHIMATE_AI_LANDSCAPE.md](docs/ARCHIMATE_AI_LANDSCAPE.md) and [docs/COMMERCIAL_ALTERNATIVES.md](docs/COMMERCIAL_ALTERNATIVES.md). EaGPT remains an in-EA local-LLM pane, not an MCP server.

## Build and install

Windows + Sparx EA required. Two separate guides:

- **[Build on Windows](docs/BUILD_WINDOWS.md)** — VS Code or Visual Studio; `.\scripts\build.ps1` fills `release\`
- **[Install in Sparx EA](docs/INSTALL_SPARX.md)** — `regasm`, EA add-in key, first use, uninstall
- **[RAG / company knowledge](docs/RAG_OLLAMA.md)** — principles, CMDB, ArchiMate examples, tiedonhallintamalli with Ollama

If you already have the .NET SDK on the EA machine:

```powershell
.\scripts\build.ps1
.\scripts\install.ps1
```

That fills **`release\`** (share that folder or `release\EaGPT.zip`). Snapshot a user drop with `.\scripts\build.ps1 -PromoteToStable` (`stable\`). Then in EA: **EaGPT → Show EaGPT View**.

## Repository layout

- `src/EaGpt.Core` — Ollama client, system prompt, JSON parser/validator, ArchiMate type map (testable on Linux)
- `src/EaGpt.AddIn` — .NET Framework 4.8 COM add-in, WinForms chat, EA Automation importer
- `tests/EaGpt.Core.Tests` — unit tests for the protocol layer
- `knowledge/` — template pack (Finnish 5 § tiedonhallintamalli, KA principles, CMDB extract, ArchiMate examples)
- `docs/` — install, security, RAG, [open-source landscape](docs/ARCHIMATE_AI_LANDSCAPE.md), [commercial alternatives](docs/COMMERCIAL_ALTERNATIVES.md)
- `release/` / `stable/` — shareable add-in drops (`build.ps1` fills them)
- `scripts/build.ps1`, `install.ps1`, `uninstall.ps1`

## Tests

Core protocol tests (parser, validator, relationship legality, impact/Mermaid context, Ollama URL policy, settings, JSON escaping) run on Linux:

```bash
dotnet test tests/EaGpt.Core.Tests/EaGpt.Core.Tests.csproj
```

The WinForms COM add-in needs Windows + EA; it is not executed in this environment.

## Security

EaGPT talks only to a user-configured LLM origin (Ollama `/api/*` or OpenAI-compatible `/v1/*`), validates LLM JSON before touching EA, and asks before deleting from the model. Details, residual risks (prompt injection, LAN SSRF), and metadata-URL blocking: [docs/SECURITY.md](docs/SECURITY.md).

## License

MIT. Prompt/JSON ideas follow ArchiGPT; this is a new C# codebase, not a port of the Eclipse plugin.

**Attribution:** ArchiGPT / [Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin) by Raino Annala.
