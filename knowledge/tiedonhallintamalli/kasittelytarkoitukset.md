# Käsittelytarkoitukset (purpose of processing)

Every **toimintaprosessi** and **tietovaranto** in the tiedonhallintamalli needs a **purpose**: the official task the processing supports. This is the modelling counterpart of GDPR *purpose limitation* and of 5 § “miksi prosessia/varantoa pidetään”.

## Record

- Short purpose statement in Finnish (or the organisation’s language)
- Link to the statutory task or service, in **notes** (Finlex citation as text, not as an ArchiMate type)
- If a GDPR Art. 30 record exists, **point to it** rather than duplicating the whole register in the architecture model

## ArchiMate mapping

- Purpose → `Goal` or `Requirement` named as the purpose, realized by the process; **or** notes / tagged value `kasittelytarkoitus` on the process and store
- Do not create one Motivation element per GDPR article number

Once-only: if the purpose is already served by a **yhteinen tietovaranto**, connect to that store instead of inventing a parallel purpose-specific copy.
