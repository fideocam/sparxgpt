# EaGPT — ArchiGPT for Sparx Enterprise Architect

In-EA chat pane that talks to a **local Ollama** LLM so you can analyze and change **ArchiMate** models with natural language.

This is the Sparx EA counterpart of [ArchiGPT](https://github.com/fideocam/Archi-LLM-plugin): same JSON mutation protocol and Ollama-first workflow, implemented as a Windows COM add-in (EA’s Automation API + ArchiMate 3 MDG), not an Eclipse plugin.

Similar commercial/official EA products already exist (OneRAI, Sparx Japan MCP, built-in AI Chat). None of them is an open-source in-EA Ollama add-in. See [docs/EXISTING_ALTERNATIVES.md](docs/EXISTING_ALTERNATIVES.md).

## What you can do

- Ask for analysis of the model, a diagram, or a selected element
- Add ArchiMate elements and relationships
- Create a new diagram with layout
- Remove from the current diagram only, or from the model
- Remove a diagram (elements stay in the model)

Default Ollama endpoint: `http://localhost:11434`, model `llama3.2`.

## Install

Windows + EA required. See [docs/WINDOWS_INSTALL.md](docs/WINDOWS_INSTALL.md).

```powershell
.\scripts\install.ps1
```

Then in EA: **EaGPT → Show EaGPT View**.

## Repository layout

- `src/EaGpt.Core` — Ollama client, system prompt, JSON parser/validator, ArchiMate type map (testable on Linux)
- `src/EaGpt.AddIn` — .NET Framework 4.8 COM add-in, WinForms chat, EA Automation importer
- `tests/EaGpt.Core.Tests` — unit tests for the protocol layer
- `scripts/install.ps1` / `uninstall.ps1`

## License

MIT. Prompt/JSON ideas follow ArchiGPT; this is a new C# codebase, not a port of the Eclipse plugin.

**Attribution:** ArchiGPT / [Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin) by Raino Annala.
