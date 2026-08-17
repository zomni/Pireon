# Domain Overview

The platform is domain-independent.

The core hierarchy is:

Campus
└── Building
    └── Floor
        └── Room

Supporting entities:

- Equipment (inventory item)
- Category (configurable inventory classification)
- PointOfInterest (map marker)
- WalkingRouteNode / WalkingRouteEdge (walkable network)
- NetworkTelemetrySnapshot / NetworkTelemetryObservation
- ImportedInventoryItem (imported inventory)
- SyncedBuilding / SyncedRoom (backend-managed locations)
- BuildingGeometryOverride (edited or moved polygons)
- ManualBuilding (created from the map)

Subsystems:

- Inventory and reconciliation
- Network telemetry
- Equipment delivery forms
- Administrative map editing
- Audit and backups

The template starts empty: campuses, buildings, floors, rooms, categories, points of interest and assets are created after installation.
