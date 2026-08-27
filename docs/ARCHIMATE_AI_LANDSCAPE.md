# ArchiMate AI landscape — features EaGPT can borrow

Survey of public ArchiMate + LLM / MCP projects (August 2026) and what we took into EaGPT. EaGPT stays an **in-EA Ollama chat pane**, not a second MCP server: Sparx Japan already ships a free EA MCP add-in.

## Projects

| Project | Host | Notable features |
| --- | --- | --- |
| [fanievh/archi-mcp-server](https://github.com/fanievh/archi-mcp-server) | Archi plugin, MCP HTTP in-process (~69 tools, 14 resources) | Structured **search/query** instead of dumping the whole model; **relationship traversal**; **viewpoint recipe** resources; **spec-legal create-relationship** with suggested corrections; ELK auto-layout; human **approval gate**; batch ops; undo |
| [JesseLeresche/archi-mcp-server](https://github.com/JesseLeresche/archi-mcp-server) | Archi MCP HTTP | `validate_model` with alternatives; `get_element_analysis`; type change while preserving links; dry-run delete |
| [thijs-hakkenberg/archimate-mcp](https://github.com/thijs-hakkenberg/archimate-mcp) | coArchi2 `model.archimate` files | Full **relationship matrix**; layer-specific tools; **impact analysis**; export **Mermaid / SVG / PNG / Markdown / HTML**; **NDJSON audit log**; Open Exchange import/export |
| [byrondelgado/mcp-archimate](https://github.com/byrondelgado/mcp-archimate) | Headless, pyArchimate | **Refuse invalid relationships** and attach alternatives; auto-layout / layer bands; quality / TOGAF readiness reports; guided load→edit→validate→export |
| Sparx Japan MCP Server | EA add-in (not OSS) | Chat in Claude / VS Code, not in EA; `-enableEdit`; official STDIO MCP. **Do not duplicate as EaGPT’s primary UI** |
| ArchiGPT ([fideocam/Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin)) | Archi + Ollama | JSON mutation protocol EaGPT already follows |
| [ThomasRohde/archi-scripts](https://github.com/ThomasRohde/archi-scripts) | JArchi + AI | Expand from selected element; generate from description; image → metamodel; Dagre layout |

## What we did **not** copy

- Replacing the in-EA Ollama pane with MCP-only (Sparx Japan already exists).
- Exposing ~70 generic tools. Small local models (`llama3.2`) do better with **one JSON protocol** plus **deterministic context** computed in C#.
- Binding an HTTP MCP server to the LAN without the existing `OllamaEndpoint` SSRF controls.

## Adopted in EaGPT (this change)

These run in `EaGpt.Core` so they are testable on Linux and apply before any EA write:

1. **Relationship legality** — aspect/layer checks (ArchiMate 3.2-inspired, not the full official matrix). Illegal LLM links are rejected with suggested types.
2. **MODEL SUMMARY + IMPACT ANALYSIS** — counts by layer and a 1-hop walk from the current selection (or named elements on an “impact / depends” question).
3. **Mermaid neighborhood** — compact 1-hop graph of the selection or the open diagram, so truncated XML does not hide neighbors.
4. **Viewpoint recipes** — always list the six named views; inject the matching recipe (business, application, technology, motivation, implementation, tiedonhallinta).
5. **Layer-banded layout** — if a new diagram stacks every node at one coordinate, spread nodes by ArchiMate layer.
6. **Mutation preview** — chat shows what will be added/removed; delete confirmation includes the same preview.
7. **Audit log** — `%AppData%\EaGpt\audit.ndjson` (counts + truncated prompt, no model dump).

## Sensible later work (not built)

- Optional localhost MCP facade over the same Core operations (Cursor/Claude *in addition to* the pane).
- Full official ArchiMate relationship matrix (pyArchimate-style) instead of heuristics.
- ELK / Dagre routing and connection bendpoints.
- Export sidecar (SVG/PNG/Markdown deck) of the current diagram.
- Multi-turn chat that keeps the last digest in session.
- Undo of the last applied mutation.
