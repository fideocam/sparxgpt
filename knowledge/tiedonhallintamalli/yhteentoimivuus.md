# Yhteentoimivuus (interoperability)

Tiedonhallintalaki lists **yhteentoimivuus** both as a purpose of the tiedonhallintamalli and as a theme of **muutosvaikutusten arviointi**.

## National direction

- **VM tiedonhallintakartta** — public map of how government information management is organised
- **DVV Yhteentoimivuusalusta** — shared semantic vocabularies and data models so stores and interfaces mean the same thing
- Prefer **tekninen rajapinta** and agreed schemas over emailed files

## Modelling

- Shared semantics → notes on `DataObject` / `ApplicationInterface` (vocabulary name), not a new ArchiMate layer
- Cross-authority use → `Serving`/`Flow` through a named interface; mark **yhteinen tietovaranto** in the store name or notes
- When adding a system, show **which existing interfaces** it must consume (once-only / no duplicate collection)
