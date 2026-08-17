import {
  getPrimaryCampusKey,
  getDataFileNames,
  getCatalogFileName,
  getBackupFileName,
} from "../utils/campusConfig.js";

describe("campusConfig", () => {
  test("derives the primary campus key from campuses.js", () => {
    expect(getPrimaryCampusKey()).toBe("campus");
  });

  test("derives data file names from school + campus key", () => {
    const names = getDataFileNames();
    expect(names.search).toBe("data/example_campus_search.json");
    expect(names.floor("0")).toBe("data/example_campus_0.json");
    expect(names.floor("b1")).toBe("data/example_campus_b1.json");
  });

  test("derives catalog and backup file names from campus key", () => {
    expect(getCatalogFileName()).toBe("data/campus_buildings_catalog.json");
    expect(getBackupFileName()).toBe("data/campus_buildings_backend_backup.json");
  });
});
