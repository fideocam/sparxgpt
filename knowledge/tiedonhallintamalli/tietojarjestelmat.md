# Tietojärjestelmät (5 §)

A **tietojärjestelmä** is an information system used to process data in official tasks.

## Record in the model (minimum)

- Name
- **Vastaava viranomainen**
- Purpose
- **Interfaces to other systems** and **transfer methods** (see `rajapinnat.md`)

## Finnish KA practice

JHS 179 **tietojärjestelmänäkymä** (application view) sits between information view and technology view. DVV still uses JHS 179 *soveltuvin osin* with **ArchiMate 3.x**. Technology (servers, cloud) belongs in the technology view — not as a substitute for the 5 § system list.

## ArchiMate mapping

- System → `ApplicationComponent`
- Responsible authority → `BusinessRole` associated to the component
- Purpose → notes or `Goal`
- Platform/hosting → `Node` / `SystemSoftware` **only** if the user asked for technology view
- Do not duplicate the same system as both a process and a component

Vendor product names may appear as the component name if that is how the organisation’s CMDB is kept; still attach a clear purpose and interfaces.
