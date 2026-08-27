# RAG with Ollama and EaGPT

Ollama **runs the model**. It does not search your documents by itself. RAG (retrieval-augmented generation) is a **layer next to Ollama**: chunk company files → embed → store → on each question retrieve a few passages → **paste them into the prompt**.

EaGPT already sends the open ArchiMate digest. Extra company knowledge (principles, CMDB excerpts, example viewpoints, tiedonhallintamalli) must be **retrieved and injected**, or the model will guess.

Two complementary pieces:

1. **A knowledge pack EaGPT can read** (works today, no extra server).
2. **A real vector RAG service on the same LAN as Ollama** (better recall on large CMDB / document sets).

## What to put in the corpus

Keep each file **short, factual, and named by topic**. Prefer Markdown. Do not dump a whole CMDB or a 200-page PDF into one blob.

| Collection | Typical files | What the model should learn |
| --- | --- | --- |
| **Architecture principles** | `principles/naming.md`, `principles/integration.md`, `principles/security.md` | Naming, reuse, “no direct DB access”, cloud vs on-prem, who owns a capability |
| **CMDB** | `cmdb/applications.md` or small CSVs of **name, type, owner, environment** | Existing applications, servers, and relations so new diagrams **reuse names** instead of inventing duplicates |
| **Example ArchiMate** | `examples/business-layer.md`, `examples/application-layer.md`, `examples/technology-deployment.md`, `examples/motivation.md` | Required viewpoints, typical element sets, “a deployment view uses Node, Device, CommunicationNetwork, Assignment/Serving” |
| **Tiedonhallintamalli** | `tiedonhallintamalli/tietovarannot.md`, `kasittelytarkoitukset.md`, `vastuut.md`, `rajapinnat.md` | Finnish information-management model: data stores, purposes of processing, owners, systems, interfaces — so generated views match how the organisation documents information (tiedonhallintalaki-style structure, not a legal opinion) |

**CMDB rule:** export a **curated extract** (applications + technical services + owners), not every CI. Refresh the extract on a schedule.

**Example diagrams:** a screenshot alone is useless to Ollama. Store a **text companion**: viewpoint name, element types, and a short “good vs bad” JSON or bullet list that matches EaGPT’s CHANGES format.

Templates live in [`knowledge/`](../knowledge/) in this repo. Copy them to the live folder below and replace the placeholders.

## 1. EaGPT knowledge folder (built in)

On each **Ask**, EaGPT searches `.md` / `.txt` / `.csv` under the knowledge directory (keyword overlap with your question) and inserts a capped **COMPANY KNOWLEDGE** block after the model XML.

Default path:

```
%AppData%\EaGpt\knowledge\
```

Suggested layout:

```
%AppData%\EaGpt\knowledge\
  principles\
  cmdb\
  examples\
  tiedonhallintamalli\
```

Copy from the repo:

```powershell
xcopy /E /I knowledge %AppData%\EaGpt\knowledge
```

A shared LAN folder is fine (set in `%AppData%\EaGpt\settings.ini`):

```ini
KnowledgeFolder=\\fileserver\architecture\eagpt-knowledge
KnowledgeMaxChars=8000
```

Caps: about 8 files, 8000 characters total (configurable). That is enough for **principles + the matching example viewpoint + a slice of tiedonhallintamalli**, not an entire CMDB.

Check the **Debug** tab: if knowledge was used, you will see `--- COMPANY KNOWLEDGE` in the user message.

## 2. Vector RAG on the Ollama machine (recommended when the corpus grows)

Run this **on the same host as Ollama** (or another LAN box). Embeddings stay in the estate.

### Models

```powershell
ollama pull llama3.2
ollama pull nomic-embed-text
```

`nomic-embed-text` is for `/api/embed`. Chat still uses `llama3.2` (or a larger instruct model) in EaGPT.

Ollama must listen on the LAN if EaGPT or the RAG UI is on another PC: `OLLAMA_HOST=0.0.0.0` (see [INSTALL_SPARX.md](INSTALL_SPARX.md)).

### Pipeline

```
documents → split into 300–800 token chunks (keep headings)
          → POST http://ollama:11434/api/embed  { "model": "nomic-embed-text", "input": "..." }
          → store vector + text + source path in a vector DB
          → on Ask: embed the user question → top 5 chunks → text to the LLM
```

The LLM still only sees **those chunks + EaGPT’s ArchiMate XML**. Retrieval quality matters more than model size.

### Turnkey (fastest)

Pick one; both talk to local Ollama:

- **[Open WebUI](https://github.com/open-webui/open-webui)** — Documents / knowledge collections, RAG onto an Ollama model. Good for **building and testing** the corpus (upload principles, tiedonhallintamalli, CMDB export). Chat here is **not** inside EA; use it to curate files, then copy the same Markdown into EaGPT’s knowledge folder (or a future retrieve API).
- **[AnythingLLM](https://anythingllm.com)** — Workspaces per topic (e.g. one workspace “tiedonhallinta”, one “ArchiMate examples”). Same idea: local embeddings + Ollama.

Use the UI to **debug retrieval** (“which passages came back for ‘deployment view’?”). Then keep the winning Markdown in `%AppData%\EaGpt\knowledge` so **EaGPT** injects it.

### DIY (same Ollama, your database)

A small Python (or Node) service on the Ollama box:

1. Chunk files from `\\fileserver\architecture\...`
2. Call `POST /api/embed` with `nomic-embed-text`
3. Store in [Qdrant](https://qdrant.tech), [Chroma](https://www.trychroma.com), or SQLite + sqlite-vec
4. `POST /retrieve { "query": "...", "k": 5 }` → `{ "chunks": [ { "source": "...", "text": "..." } ] }`

EaGPT today reads the **folder**, not this HTTP API. Until an HTTP retriever is wired in, write retrieve results back to the knowledge folder or keep the service for Open WebUI only.

## Prompt behaviour

The system prompt tells the model:

- Existing **ids** come only from the ArchiMate XML.
- Knowledge is for **rules, naming, required viewpoints, and tiedonhallintamalli structure**.
- CMDB names may be reused as **element names** when you ask to model those CIs; they are not EA GUIDs.

If you ask “create a technology deployment diagram for order capture”, retrieval should surface `examples/technology-deployment.md` plus relevant CMDB rows and a principle about environments (dev/test/prod).

## Limits

- Keyword retrieval in EaGPT is **not** semantic search. Name files after the viewpoint (`technology-deployment.md`) so questions hit them.
- Large PDFs: convert to Markdown (one topic per file) before indexing.
- Secrets: do not put credentials or personal data in the knowledge pack. CMDB extracts should be **architecture-relevant**, not HR dumps.
- Context window: raising `KnowledgeMaxChars` too high will crowd out the model XML. Prefer better files over a bigger cap.
