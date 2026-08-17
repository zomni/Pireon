# Frontend Architecture

## Stack

- Vanilla JavaScript
- Leaflet (map)
- Leaflet.draw and custom editors (geometry)
- Fuse + custom scoring (search)
- Webpack (bundler)
- Portable static build (`create_dist.js`)

## Structure

src/
  assets/     Icons, favicon, building SVGs.
  components/ UI components and map tools.
  data/       Campus config, GeoJSON, search index, static backups.
  lib/        Vendored dependencies (Leaflet, Leaflet.draw, Fuse, jQuery).
  styles/     CSS for map, search, buttons and layout.
  utils/      Data loading, navigation, cookies, static backups.
  views/      Leaflet initialization, popups, feature rendering.

## Modes

- With backend: prioritizes updated data from the API.
- Without backend: uses local JSON and static backups so the map is not empty.

## Campus Config

`src/data/campuses.js` is the canonical template configuration (SPEC 03). Data paths, search index and catalog derive from it.

## Key Modules

- `views/map.js` — Leaflet instance, bounds, tile layer, location tracking.
- `views/featureDisplay.js` — building popup experience.
- `components/autocompleteSearchBox.js` — search.
- `components/routePlanner.js` — route between buildings.
- `components/sessionModeBadge.js` — session state and admin visibility.

## Rules

- Keep backend as priority data source when available.
- Keep local/static fallback for no-API use.
- Do not duplicate loose controls; reuse existing panels.
- Only one admin tool active at a time.
