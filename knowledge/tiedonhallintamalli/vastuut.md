# Vastuut (vastaava viranomainen)

Every **toimintaprosessi**, **tietovaranto** (when Art. 30 does not apply), and **tietojärjestelmä** in 5 § needs a **responsible authority**.

## Practice

- The *tiedonhallintayksikkö* **owns** the model; it cannot outsource that ownership (typical municipal reading, e.g. Hämeenkyrö).
- A supplier may operate a system; the **vastaava viranomainen** remains the authority.
- Shared stores: name the **controller** vs **processor** in notes if GDPR applies; still keep one ArchiMate role as the 5 § owner.

## ArchiMate mapping

- Authority / unit → `BusinessActor` or `BusinessRole` (`vastaava viranomainen`)
- Link with `Association` to process, store, and system
- Optional: `BusinessCollaboration` for joint services (wellbeing services county + municipality)

If the user does not name an owner, add a role `vastaava viranomainen (tarkistettava)` rather than omitting the field.
