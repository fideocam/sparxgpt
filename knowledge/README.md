# EaGPT knowledge pack (copy to `%AppData%\EaGpt\knowledge`)

This folder is a **template** for company RAG context. Copy it, then replace placeholders with your organisation’s text.

EaGPT retrieves a few matching `.md` / `.txt` / `.csv` files on each Ask and adds them as COMPANY KNOWLEDGE. Viewpoint recipes (business / application / technology / motivation / implementation / tiedonhallinta) are also injected from code when the question matches; keep the example files in sync. See [docs/RAG_OLLAMA.md](../docs/RAG_OLLAMA.md).

Suggested live location:

```
%AppData%\EaGpt\knowledge\
```

## Default pack (Finnish public-sector modelling aid)

The bundled files follow **tiedonhallintalaki 906/2019 5 §** and common KA practice (JHS 179 as method, VM 2024:22). They are **not** legal advice.

| Folder | Files | Use when |
| --- | --- | --- |
| `tiedonhallintamalli/` | `overview.md`, `toimintaprosessit.md`, `tietovarannot.md`, `kasittelytarkoitukset.md`, `tietoaineistot.md`, `tietojarjestelmat.md`, `rajapinnat.md`, `vastuut.md`, `muutosvaikutukset.md`, `asiakirjajulkisuuskuvaus.md`, `yhteentoimivuus.md` | User asks for tiedonhallintamalli / 5 § objects |
| `principles/` | `architecture-principles.md`, `julkishallinto-ka.md` | Naming, reuse, once-only, rajapinta-first |
| `examples/` | ArchiMate viewpoint notes + `tiedonhallintamalli-archimate.md` | Required element sets |
| `cmdb/` | `extract.md`, `julkishallinto-inventory.md` | Reuse real names; replace the fictional inventory |

Keep each file short: retrieval is keyword overlap, about 8 chunks / 8000 characters per Ask.
