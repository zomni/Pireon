// Identificadores internos de la aplicación (SPEC 10).
// Todos los prefijos de storage, nombres de eventos y ventanas derivan del
// prefijo configurado en appConfig; renombrarlos no requiere tocar el resto del código.

import { appConfig } from "../config/appConfig.js";

const prefix = appConfig.prefix;

export const identifiers = {
  prefix,
  storage: {
    buildingBackup: `${prefix}_building_backup`,
    walkingRoutesBackup: `${prefix}_walking_routes_backup`,
    networkTelemetry: `${prefix}_network_telemetry`,
    walkingRoutesVisible: `${prefix}_walking_routes_visible`,
    buildingLabelsVisible: `${prefix}_building_labels_visible`,
  },
  events: {
    sessionChanged: `${prefix}-session-changed`,
    adminMapToolMode: `${prefix}-admin-map-tool-mode`,
    buildingLayerClick: `${prefix}-building-layer-click`,
    mapDataRefreshed: `${prefix}-map-data-refreshed`,
    sitesLoaded: `${prefix}-sites-loaded`,
    campusChanged: `${prefix}-campus-changed`,
  },
  globals: {
    adminMapToolMode: `${prefix}AdminMapToolMode`,
    openDashboard: `open${prefix.charAt(0).toUpperCase()}${prefix.slice(1)}Dashboard`,
  },
  windowName: `${prefix}-dashboard`,
};
