# Inventory

## Purpose

Manage equipment assets, their assignment to locations, and imported inventory.

## Entities

- ImportedInventoryItem: imported or manually created inventory.
- InventoryAliasRule: rules to map textual locations to buildings/rooms.
- SyncedEquipment: historical/synced equipment by building/room.

## ImportedInventoryItem Fields

- SerialNumber: priority identifier.
- InferredCategory: normalized category (configurable, SPEC 08).
- InferredStatus: operational status.
- AssignedBuildingExternalId: assigned building.
- AssignedRoomExternalId: assigned room.
- AssignedFloor: assigned floor.
- DeliveryFormPdfFileName: associated PDF, if any.
- MatchedBuildingExternalId / MatchedRoomExternalId: automatic reconciliation suggestions.
- AssignmentUpdatedAtUtc: last manual assignment date.

## Flows

- Excel import with configurable category mapping.
- Manual create/edit/delete.
- Assignment to building/room/floor.
- Reconciliation of inventory against locations.
- PDF attachment per equipment.

## Rules

- The backend is the priority data source for inventory.
- Sensitive inventory mutations are audited.
- No client-specific normalization (SPEC 08).
