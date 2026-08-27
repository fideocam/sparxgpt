# Rajapinnat: tekninen rajapinta ja katseluyhteys

Tiedonhallintalaki distinguishes **how** authorities exchange data (definitions in 2 §; technical interfaces in ch. 4, including **22 §**).

## Two transfer modes

1. **Tekninen rajapinta** — machine-to-machine interface (API, message, file drop with agreed schema). **Preferred** between authorities when disclosing data for official duties, so collection is not repeated by hand.
2. **Katseluyhteys** — view/access connection (a user in authority A looks at data in authority B’s system without a full copy). Use when a copy is unnecessary or legally tighter.

Both must appear in the tiedonhallintamalli **system** description: *which systems talk, and how*.

## Interoperability

VM **tiedonhallintakartta** and DVV **Yhteentoimivuusalusta** (semantic interoperability) are the national direction: shared vocabularies and interfaces, not one-off CSV emails.

## ArchiMate mapping

- Technical interface → `ApplicationInterface` on the providing `ApplicationComponent`
- Consumer uses interface → `Serving` (interface to consumer) and/or `Flow` of a `DataObject`
- Katseluyhteys → `Access` or `Serving` **named** `katseluyhteys` (do not model it as a silent association)
- Put protocol in notes (`REST`, `Suomi.fi-palveluväylä`, `SFTP`) — do not invent a new ArchiMate type per protocol

Never draw a direct `Access` from a foreign process into another organisation’s database without an interface element when 5 § is in scope.
