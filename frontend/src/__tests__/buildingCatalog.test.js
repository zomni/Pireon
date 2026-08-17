import { computeAllowedBuildingIdsForFloor } from "../utils/buildingCatalog.js";

describe("computeAllowedBuildingIdsForFloor", () => {
  test("returns null when there is no catalog so features are not filtered", () => {
    expect(computeAllowedBuildingIdsForFloor([], 0)).toBeNull();
    expect(computeAllowedBuildingIdsForFloor(null, 0)).toBeNull();
  });

  test("allows buildings that list the requested floor", () => {
    const buildings = [
      { id: "a", floors: [0, 1] },
      { id: "b", floors: [1, 2] },
    ];
    expect(computeAllowedBuildingIdsForFloor(buildings, 0)).toEqual(new Set(["a"]));
    expect(computeAllowedBuildingIdsForFloor(buildings, 1)).toEqual(new Set(["a", "b"]));
    expect(computeAllowedBuildingIdsForFloor(buildings, 2)).toEqual(new Set(["b"]));
  });

  test("only allows buildings without floors on floor 0", () => {
    const buildings = [
      { id: "a", floors: [] },
      { id: "b", floors: [1] },
    ];
    expect(computeAllowedBuildingIdsForFloor(buildings, 0)).toEqual(new Set(["a"]));
    expect(computeAllowedBuildingIdsForFloor(buildings, 1)).toEqual(new Set(["b"]));
  });
});
