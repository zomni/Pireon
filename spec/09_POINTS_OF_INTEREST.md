# Points of Interest Administration

## Purpose

Allow creating and managing points of interest (map markers) from the administrative UI, generically.

## Current State

- `src/components/markers.js` renders markers (language leftovers cleaned: "Copier le lien" -> "Copiar enlace", "étage" -> "piso").
- `PointOfInterest` entity, `api/points-of-interest` CRUD with audit logging and frontend admin editor implemented.

## Required Changes

Backend:

- New `PointOfInterest` entity: type, name, description, coordinates, campus/floor, active, audit fields. (DONE)
- CRUD API following existing controller patterns (`ManualBuildingsController`). (DONE)
- Audit logging on mutations. (DONE)

Frontend:

- Render points of interest from the backend reusing `markers.js`. (DONE)
- Admin management surface in the existing admin tooling. (DONE)
- No dependency on client IDs or names. (DONE)

## Rules

- Points of interest are soft-deleted.
- Coordinates are stored generically (lat/lng or plan-local), documented in SPEC 13.

## Acceptance Criteria

- An admin can create, edit and delete a point of interest from the UI.
- Points of interest render on the map and survive reload.
- Viewer and editor roles respect the existing RBAC.

## Decisiones de implementación

- La entidad sigue los patrones existentes (GUID, timestamps, soft delete, auditoría).
- `markers.js` se reutiliza; solo se le quitan los restos en francés.
- GET público con filtros `campus`, `floor` (incluye POIs sin piso asignado) e `includeInactive` (solo autenticado). Mutaciones restringidas a `admin`.
- El editor admin (`poiEditor.js`) aporta dos herramientas: agregar POI (clic en el mapa + modal) y gestionar POIs (lista con editar/eliminar).
