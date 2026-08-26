# Existing Sparx EA AI plugins vs ArchiGPT

Evaluated before writing EaGPT. ArchiGPT is an in-Archi chat pane that talks to local Ollama and applies structured ArchiMate create/update/delete operations.

## Comparison

| Product | In-EA chat | Local Ollama | Create/edit model | Open source | Notes |
| --- | --- | --- | --- | --- | --- |
| **ArchiGPT** (Archi) | Yes | Yes | Yes | MIT | Baseline we are matching |
| **OneRAI** | Yes | Yes (also LM Studio / cloud) | Yes | No | Closest product shape. Commercial; trial ended 31 Aug 2026 |
| **Sparx Japan MCP Server** | No (chat in Claude / VS Code) | Via an MCP client | Opt-in (`-enableEdit`) | Free add-in | Official, Windows-only, STDIO MCP. No HTTP (EULA). EA 17.2 incompatible |
| **EA built-in AI Chat** | Yes | No (OpenAI / Gemini) | Limited | No | Corporate edition+. `#diagram#` JSON context. Mostly Q&A |
| **AI Assist** (Genie) | Yes | Vendor stack | Yes | No | Commercial in-EA authoring |
| **AI Power Tools** | No (MCP) | No | Yes | No | Paid local MCP (~USD 40/mo) |
| **Kernaro** | Web | Vendor stack | Yes | No | Enterprise product |

## Decision

None of the EA options is an open-source ArchiGPT clone: **in-EA pane + Ollama-first + structured ArchiMate mutations + MIT**.

EaGPT fills that gap. It is a Windows COM add-in, not an MCP server (Sparx Japan already ships a free MCP add-in).
