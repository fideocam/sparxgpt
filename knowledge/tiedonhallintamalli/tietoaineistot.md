# Tietoaineistot, arkistointi ja hävitys (5 §)

A **tietoaineisto** is a body of documents or data with a defined lifecycle. The tiedonhallintamalli must state, for relevant materials, **how and where they are transferred to archives** or **how they are destroyed**.

## Record in the model

- Link the aineisto to its **tietovaranto** and **toimintaprosessi**
- Archive: transfer **method** and **location** (e.g. SÄHKE, Kansallisarkisto, municipal archive)
- Or **destruction** rule and responsible role
- Align with **asianhallinta** (case management) and publicity/confidentiality classification

## ArchiMate mapping

- Aineisto → `DataObject` or `BusinessObject` (document series); keep the name stable
- Archive location → `Location` or notes; transfer → `Flow` to an archive `ApplicationComponent` or role
- Destruction / retention → `Constraint`

Do not hide archive decisions only inside a system name. 5 § wants the **information lifecycle**, not only the application catalogue.
