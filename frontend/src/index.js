// Apply instance theme before any UI renders (SPEC 01/02).
import { applyBrandingTheme } from "./config/appConfig.js";

// Load site configuration from the backend session (multi-tenant), with
// fallback to the static campuses.js template.
import { loadSites } from "./config/siteConfig.js";
import { identifiers } from "./utils/identifiers.js";

// Add the campus selector component
import "@app/campusSelector";

// Search for the feature by id/alias in URL
import "@app/findByUrl";

// Building-to-building route planner
import "@app/routePlanner";

// Backend session mode indicator
import { initSessionModeBadge } from "@app/sessionModeBadge";

// Manual building polygon editor for admins
import { initManualBuildingEditor } from "@app/manualBuildingEditor";

// Existing building geometry editor for admins
import { initBuildingGeometryEditor } from "@app/buildingGeometryEditor";

// Walking route network editor for admins
import { initWalkingRouteEditor } from "@app/walkingRouteEditor";

// Persistent walking route visibility layer
import { initWalkingRouteLayer } from "@app/walkingRouteLayer";

// Network telemetry panel and heat overlay
import { initNetworkTelemetryPanel } from "@app/networkTelemetryPanel";

// Site zoom range (min/max) editing from the map
import { initSiteViewportPanel } from "@app/siteViewportPanel";

applyBrandingTheme();

const bootstrapLoadingOverlay = document.getElementById("map-loading-overlay");
if (bootstrapLoadingOverlay) {
  bootstrapLoadingOverlay.hidden = false;
}

window.addEventListener(
  identifiers.events.mapDataRefreshed,
  () => {
    const overlay = document.getElementById("map-loading-overlay");
    if (overlay) {
      overlay.hidden = true;
    }
  },
  { once: true }
);

loadSites();

initSessionModeBadge();
initWalkingRouteLayer();
initNetworkTelemetryPanel();
initManualBuildingEditor();
initBuildingGeometryEditor();
initWalkingRouteEditor();
initSiteViewportPanel();
