import { validateZoomRange, MIN_ZOOM, MAX_ZOOM } from "../utils/viewportRules.js";

describe("viewportRules", () => {
  test("allows a valid zoom range", () => {
    expect(validateZoomRange(12, 19)).toBeNull();
    expect(validateZoomRange(MIN_ZOOM, MAX_ZOOM)).toBeNull();
  });

  test("rejects non-integer values", () => {
    expect(validateZoomRange(1.5, 19)).toMatch(/entero/);
    expect(validateZoomRange(12, NaN)).toMatch(/entero/);
  });

  test("rejects values outside the allowed range", () => {
    expect(validateZoomRange(-1, 19)).toMatch(/minimo/);
    expect(validateZoomRange(0, 22)).toMatch(/maximo/);
  });

  test("rejects a minimum greater than the maximum", () => {
    expect(validateZoomRange(19, 12)).toMatch(/mayor o igual/);
  });
});
