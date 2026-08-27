# Example: map one service to 5 § (ArchiMate)

User intent: “Kuvata lupapalvelun tiedonhallinta.”

Minimum diagram objects:

- `BusinessRole` **Lupaviranomainen** (vastaava)
- `BusinessProcess` **Luvan käsittely** — purpose in notes
- `DataObject` **Luparekisteri** (tietovaranto) — tietoryhmät and retention in notes
- `ApplicationComponent` **Asianhallinta** — purpose: case handling
- `ApplicationInterface` **Lupatietojen tekninen rajapinta**
- `ApplicationComponent` **Kansallinen rekisteri** (reuse, once-only)
- Relationships: Serving (Asianhallinta → process), Access (process → Luparekisteri), Serving (rajapinta → Asianhallinta), Flow or Serving from national register
- `Constraint` **Säilytys ja hävitys** on the store
- Notes on Asianhallinta: muutosvaikutusten arviointi if this is a new go-live

Viewpoint: layered business / application; technology omitted unless asked.
