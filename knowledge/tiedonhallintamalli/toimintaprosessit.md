# Toimintaprosessit (5 §)

A **toimintaprosessi** is the organised sequence of tasks by which the authority produces a service or performs a statutory duty.

## Record in the model (minimum)

- Name of the process
- **Vastaava viranomainen** (responsible authority / unit)
- Purpose of the process (why it exists)
- Links to **other processes** (upstream/downstream, shared steps)

## Modelling practice (Finnish KA / JHS 179)

Start architecture work **from processes**, then attach stores and systems. Municipal examples (e.g. Hämeenkyrö) keep process, store, system, security, and archives in one maintained picture.

## ArchiMate mapping (EaGPT)

- Process → `BusinessProcess` (name = process name)
- Responsible authority → `BusinessRole` or `BusinessActor`, association to the process
- Purpose → `Goal` or notes / tagged value `kasittelytarkoitus`
- Process-to-process → `Triggering` or `Flow`
- Process uses store → `Access` (Read/Write) from process to `DataObject`
- Process uses system → `Serving` from `ApplicationComponent` to process (or Realization if the app implements the process)

Do not model a legal paragraph as an ArchiMate type. Put Finlex references in notes.
