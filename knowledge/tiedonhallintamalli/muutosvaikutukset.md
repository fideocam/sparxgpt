# Muutosvaikutusten arviointi

Before a **material change** to operations or taking a **new system into use**, tiedonhallintalaki requires an assessment of impacts on:

- Information security (*tietoturva*)
- Disclosure of data (*tietojen luovuttaminen*)
- Case management (*asianhallinta*)
- Publicity and confidentiality (*julkisuus ja salassapito*)
- Interoperability (*yhteentoimivuus*)

## Modelling practice

When the user describes a new system or process, generate:

1. The new 5 § objects (process, store, system, interfaces)
2. `Flow` / `Serving` / `Access` from **existing** objects that change
3. A `Constraint` or notes block `muutosvaikutusten arviointi` listing the five themes (short bullets, not a legal memo)

VAHTI / CIA (saatavuus, eheys, luottamuksellisuus) and later NIS2 overlay are **security planning** inputs — model as `Constraint`/`Principle`, not as extra ArchiMate layers.
