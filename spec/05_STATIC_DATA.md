# Static Data Layer & Identifier Scheme

## Purpose

Make the frontend static JSON files optional and independent of client content, and generalize the building identifier scheme.

## Current State

- `src/data/cs_sotero_{-1..5}.json` — per-floor GeoJSON.
- `src/data/cs_sotero_search.json` — search index.
- `src/data/sotero_buildings_catalog.json`, `sotero_buildings_manual_data.json` (client buildings), `sotero_buildings_backend_backup.json`.
- `src/data/interiors/SR-BLD-*` — building interiors.
- `src/data/walking_routes_backup.json`, `network_telemetry_backup.json`.
- Building ID regex `/^SR-BLD-\d+$/` in `soteroSearchMetadata.js:12` and `scripts/syncSoteroFloorsFromSearch.js:20`.
- 10 data-regeneration scripts hardcode filenames and ID patterns.

## Required Changes

- Turn static JSON files into optional template assets.
- The fallback chain (API → localStorage → static JSON) must tolerate missing static files (empty map).
- Remove the `SR-BLD-\d+` regex; treat building IDs as opaque strings.
- Parameterize the data-regeneration scripts from the campus configuration (SPEC 03).

## Rules

- The application must work with no static data present.
- The backend remains the priority data source when available.

## Acceptance Criteria

- Deleting all static JSON files leaves a functional empty map.
- A building with an arbitrary ID (any format) renders, searches and opens correctly.
- Regeneration scripts produce files named from campus configuration.

## Decisiones de implementación

- Los respaldos estáticos se conservan como mecanismo (modo sin API), pero sin contenido de cliente por defecto.
- El identificador de edificio deja de validarse por patrón; el backend ya lo trata como string opaco.
