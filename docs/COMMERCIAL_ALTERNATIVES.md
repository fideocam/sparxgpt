# Commercial ArchiMate / Sparx EA AI products

What OneRAI, Kernaro, Sparx built-in chat, Sparx Japan MCP, and AI Power Tools sell — and which of those ideas belong in EaGPT. EaGPT stays MIT, in-EA, and local-LLM-first.

## Product map (August 2026)

| Product | Where you chat | Model | Writes the EA model | Price shape | Closest EaGPT overlap |
| --- | --- | --- | --- | --- | --- |
| **[OneRAI](https://1repo.pl/onerai/)** | In-EA pane | Ollama, **LM Studio**, ChatGPT, Claude, Gemini, Azure | Yes (on explicit command) | Commercial; trial ended 31 Aug 2026 | Same product shape |
| **[Kernaro Assist](https://kernaro.sparxsystems.com/kernaro-assist/)** | In-EA pane | Vendor stack | Yes | Sparx commercial (Basic / Advanced + Hub) | In-EA authoring + governance |
| **Kernaro AI Hub** | Browser | Vendor stack | No (Assist writes) | Enterprise | Stakeholder Q&A, Jira/ADO, Word/Excel |
| **[EA Model Chat](https://sparxsystems.com/enterprise_architect_user_guide/17.2/teams___collaboration/chat_model_assistant.html)** | In-EA chat | OpenAI / Gemini | Limited | Corporate edition+ | `#diagram#` / `#element#` JSON context, follow-up with `<>` |
| **[Sparx Japan MCP](https://www.sparxsystems.jp/en/MCP/)** | Claude / VS Code | Whatever the MCP client uses | Opt-in `-enableEdit` / `-enableDelete` | Free add-in | Do **not** replace EaGPT’s pane with this |
| **[AI Power Tools](https://sparxservices.com/ai-power-tools/)** | Claude Cowork/Code, Copilot | Claude (etc.) | Live COM MCP | Paid (~USD 40/mo class) | Governance, orphans, impact, spreadsheet→diagram |

## Features they advertise

**OneRAI** — 40+ EA tools, any LLM including LM Studio, semantic search over large repositories, Jira/Azure DevOps tabs, ArchiMate/BPMN/UML generation, **quality audit** with a findings report, documentation generation.

**Kernaro Assist** — NL element creation (including tagged values), diagram scaffolds, **governance / MDG conformance** (mandatory tags, stereotypes, naming), NL repository queries, Visio/image/sketch → diagram, broadcast-event agents, role-based write vs read-only.

**EA Model Chat** — Passes **Name, Notes, Type, Tagged Values** for `#element#`; diagram geometry + connectors for `#diagram#`; optional prior turn via `<>`. Cloud LLM only.

**Sparx Japan MCP** — `find_elements_by_name`, current diagram/selection, open diagram, **edit and delete gated by flags**, CSV of AI modifications (`-modifiedInfoPath`), ArchiMate creation prompts. Chat is not in EA.

**AI Power Tools** — Spreadsheet/CMDB → stereotyped diagrams, **orphan / fan-in / fan-out**, relationship discovery, conformance audits, stakeholder narratives. MCP in Claude, not an in-EA Ollama pane.

## What we took into EaGPT

| Commercial idea | EaGPT behaviour |
| --- | --- |
| Quality audit (OneRAI, Kernaro, AI Power Tools) | Deterministic **MODEL QUALITY** line on every Ask; full list on “audit the model” (orphans, not on any view, illegal existing relationships, duplicate names, high fan-in/out) |
| Find in repository (OneRAI, Japan MCP, Kernaro queries) | **SEARCH HITS** from keyword overlap; always on “find / search / which …” |
| Follow-up chat (OneRAI, EA `<>`) | Last **4 turns** (short prompt + reply, not the old XML dump). **Clear chat** resets them |
| `#element#` notes (EA Model Chat) | Selection lines include a flattened **notes:** snippet |
| LM Studio (OneRAI) | If `/api/tags` fails, use **OpenAI-compatible** `/v1/models` and `/v1/chat/completions` (default LM Studio `http://localhost:1234`) |
| Example prompts | Dropdown under the prompt box |

## What we are not copying

- Jira / Azure DevOps / Confluence / SharePoint sync (Kernaro Hub, OneRAI)
- Visio / screenshot / handwriting → model (Kernaro) — needs a vision model
- BPMN / UML / MDG authoring (EaGPT is ArchiMate 3)
- Broadcast-event agents and Hub RBAC
- Cloud API keys in the add-in (pointing the URL at `api.openai.com` would send the digest off-box; use a local proxy if you must)
- Replacing the pane with MCP-only (Japan MCP and AI Power Tools already do that)

## Practical takeaway

The commercial tools mostly sell **governance, search, conversation memory, and LLM flexibility**. Those are cheap to do in `EaGpt.Core` next to the digest. They also sell **integrations and vision**, which would pull EaGPT off the ArchiGPT / local-Ollama brief.
