import { identifiers } from "../utils/identifiers.js";

describe("identifiers", () => {
  test("uses the configured instance prefix", () => {
    expect(identifiers.prefix).toBe("pireon");
  });

  test("scopes localStorage keys with the prefix", () => {
    expect(identifiers.storage.buildingBackup).toBe("pireon_building_backup");
    expect(identifiers.storage.walkingRoutesBackup).toBe("pireon_walking_routes_backup");
    expect(identifiers.storage.networkTelemetry).toBe("pireon_network_telemetry");
  });

  test("scopes event names with the prefix", () => {
    expect(identifiers.events.sessionChanged).toBe("pireon-session-changed");
    expect(identifiers.events.adminMapToolMode).toBe("pireon-admin-map-tool-mode");
    expect(identifiers.events.mapDataRefreshed).toBe("pireon-map-data-refreshed");
    expect(identifiers.events.sitesLoaded).toBe("pireon-sites-loaded");
    expect(identifiers.events.campusChanged).toBe("pireon-campus-changed");
  });

  test("scopes globals and window name with the prefix", () => {
    expect(identifiers.globals.adminMapToolMode).toBe("pireonAdminMapToolMode");
    expect(identifiers.windowName).toBe("pireon-dashboard");
  });
});
