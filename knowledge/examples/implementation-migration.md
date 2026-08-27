# Example: implementation and migration viewpoint

When the user asks for a **roadmap**, **migration**, **plateau**, or **work package** view:

Typical elements: WorkPackage, Deliverable, ImplementationEvent, Plateau, Gap, plus the core elements a plateau represents.

Typical relationships: RealizationRelationship (work package or plateau → architecture), TriggeringRelationship for events, AssociationRelationship to gaps.

Name plateaus after states the organisation already uses (e.g. current / target). Reuse existing architecture ids from the XML instead of cloning the whole landscape.
