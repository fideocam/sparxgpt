# Tietovarannot (5 §)

A **tietovaranto** is a set of data that is processed for a defined purpose (register, case store, statistics store, shared national store, etc.). **Yhteinen tietovaranto** is a store used by several authorities (e.g. national base registers).

## Record in the model (minimum)

- Name of the store
- Links to **toimintaprosessit** that use it
- Links to **tietojärjestelmät** that implement or access it
- **Either** a reference to the organisation’s **GDPR Art. 30** record of processing activities
- **Or**, if Art. 30 does not apply: responsible authority, purpose, **main data groups** (*tietoryhmät*), **disclosure targets** (*luovutuskohteet*), and **retention** (*säilytysaika*)

## Once-only / yhden kerran

Tiedonhallintalaki and EU once-only practice (Tallinn / SDGR) push authorities to **reuse existing stores** instead of collecting the same personal or business data again. Prefer Serving/Access to an existing `DataObject` over a new duplicate store.

## ArchiMate mapping

- Store → `DataObject` (and `Grouping` if several logical stores sit in one platform)
- Data groups → nested `DataObject` or attributes/notes (`tietoryhma`)
- Retention / destruction → `Constraint` or notes (`sailytys`, `hävitys`) — not a new element type
- Process/system links → `Access` / `Serving`

Name stores after the **information**, not the vendor product (`väestötietovaranto`, not `X-järjestelmän kanta`).
