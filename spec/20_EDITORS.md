# Administrative Map Editors

## Purpose

Admin tools for editing the map: buildings, geometry, walking routes and points of interest.

## Unified Panel

`adminMapToolsPanel.js` hosts the admin tools:

- Add building
- Edit shape
- Move building
- Edit routes
- Delete routes
- Split vertex
- Connect building
- Undo

The panel shows only with an admin session.

## Building Editor

- Create buildings by drawing a polygon (`manualBuildingEditor.js`).
- POST to the backend and refresh the map and caches.

## Geometry Editor

- Edit the shape of an existing building (`buildingGeometryEditor.js`).
- Move a building.
- Persist the override in the backend.

## Walking Route Editor

- Create routes by clicks.
- Free drawing.
- Move, join and split vertices.
- Connect routes to building edges.
- Delete segments and undo the last action.
- Save and refresh the walking route layer.

## Points of Interest Editor

- Create, edit and delete points of interest (SPEC 09). (DONE: `poiEditor.js`)
- Rendered reusing `markers.js`. (DONE)
- Add mode: single map click places the point and opens the create form.
- Manage mode: list all POIs with edit and delete actions.

## Rules

- Only one admin tool active at a time.
- Admin tools share visual state through the unified panel.
- Changes must reflect without requiring a manual reload.
- Tools visibility syncs with the session state.
