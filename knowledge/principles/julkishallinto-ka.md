# Finnish public-sector architecture principles (modelling)

Use these when the repository or user prompt is julkishallinto / kunta / hyvinvointialue / virasto.

1. **Lawful basis first** — every process and store has a purpose tied to a task; put the purpose in notes (`kasittelytarkoitus`).
2. **Once-only (yhden kerran)** — do not create a new *tietovaranto* if a national or existing municipal store already holds the data; connect via **tekninen rajapinta**.
3. **Four views (JHS 179 method)** — *toiminta, tieto, tietojärjestelmä, teknologia*. 5 § lives mainly in the first three; add technology only when asked.
4. **ArchiMate 3.x** — DVV example architectures use ArchiMate 3.2; stay on standard types.
5. **Named interfaces** — viranomainen-to-viranomainen exchange is an `ApplicationInterface`, not a shared Excel.
6. **Owner on every 5 § object** — `vastaava viranomainen` as `BusinessRole`.
7. **Reuse before buy/build** — prefer existing components from the model/CMDB extract.
8. **Security as constraints** — CIA/VAHTI themes as `Constraint`, not as shadow processes.
9. **Public extract vs full model** — *asiakirjajulkisuuskuvaus* is public and thinner than the tiedonhallintamalli.
10. **Change assessment** — new systems get a muutosvaikutus note covering security, disclosure, asianhallinta, julkisuus, interoperability.

Sources: tiedonhallintalaki 906/2019 5 § and 2 §; VM 2024:22; JHS 179 / DVV KA; VM tiedonhallintakartta.
