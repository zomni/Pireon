import { resolveFloorButtonId } from "../utils/floorButtons.js";

describe("resolveFloorButtonId", () => {
  test("maps a remote floor value to its button id", () => {
    expect(resolveFloorButtonId("0", ["0", "1"])).toBe("b0");
    expect(resolveFloorButtonId("1", ["0", "1"])).toBe("b1");
  });

  test("keeps a template-style button id untouched", () => {
    expect(resolveFloorButtonId("b1", ["0", "1"])).toBe("b1");
  });

  test("falls back to the first floor button when the floor is not listed", () => {
    expect(resolveFloorButtonId("7", ["0", "1"])).toBe("b0");
  });

  test("falls back to b0 when there is no default floor", () => {
    expect(resolveFloorButtonId("", ["0", "1"])).toBe("b0");
    expect(resolveFloorButtonId(undefined, ["0", "1"])).toBe("b0");
  });

  test("falls back to b0 when there are no floors", () => {
    expect(resolveFloorButtonId("0", [])).toBe("b0");
  });
});
