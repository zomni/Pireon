// Sin SVG de edificios por ahora.
// Los edificios se renderizan desde el GeoJSON derivado de la configuración del campus
// (SPEC 03): archivos `${school}_${campus}_${floor}.json`.

import { getPrimaryCampusKey } from "../utils/campusConfig.js";

const campusKey = getPrimaryCampusKey();

export const campusBuildings = {
  [campusKey]: {},
};

export const latlngBuildings = {
  [campusKey]: {},
};
