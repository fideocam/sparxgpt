# Tiedonhallintamalli (Finnish public-sector information-management model)

This pack is a **modelling aid**, not legal advice. The authority that is a *tiedonhallintayksikkö* must interpret Act 906/2019 with its lawyers and the current *suositus* from Tiedonhallintalautakunta.

## What it is

Under **laki julkisen hallinnon tiedonhallinnasta (906/2019) 5 §**, each information-management unit must maintain a **tiedonhallintamalli**: a structured description of how the organisation processes information in its tasks. The model is used to plan services and case handling, plan access rights, **reduce duplicate collection of the same data**, improve **interoperability**, and plan **information security**.

The model is **maintained continuously**. Before a material reform of operations or a new system go-live, the unit must assess impacts on information security, data disclosure, case management, publicity/confidentiality, and interoperability (**muutosvaikutusten arviointi**).

## Minimum content (5 §)

1. **Toimintaprosessit** — name, responsible authority, purpose, links to other processes.
2. **Tietovarannot** — name; links to processes and systems; either a GDPR Art. 30 processing record **or** (when no record is required) responsible authority, purpose, main data groups, disclosure targets, and retention.
3. **Tietoaineistot** — archive transfer (method and location) **or** destruction.
4. **Tietojärjestelmät** — name, responsible authority, purpose, **interfaces to other systems and how data is transferred**.

## Related public description

**Asiakirjajulkisuuskuvaus** (tiedonhallintalaki 28 §) is a **shorter public extract** of how documents can be requested — not a substitute for the full model. See VM 2020:22.

## Method, not a second law

**JHS 179** (kokonaisarkkitehtuuri, four views, ArchiMate) is still used as a **method** by DVV and many municipalities. It is **not** a current statutory obligation by itself. Map 5 § objects onto ArchiMate; do not invent extra element types for GDPR articles.

## Sources to cite in model notes (URLs, not full text)

- Finlex: https://www.finlex.fi/fi/lainsaadanto/saadoskokoelma/2019/906
- Tiedonhallintalautakunta / VM 2024:22 *Suositus tiedonhallintamallista*
- DVV: JHS 179 still applied *soveltuvin osin*; ArchiMate 3.2 example case
- Interoperability map: https://www.suomi.fi/company/support-and-assistance-for-companies-and-operators/information-management-map (VM tiedonhallintakartta)

When generating ArchiMate, cover processes, stores, systems, and interfaces with named *vastaava viranomainen* roles. Prefer **tekninen rajapinta** between authorities over ad-hoc file exchange.
