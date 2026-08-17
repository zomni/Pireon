export const MIN_ZOOM = 0;
export const MAX_ZOOM = 21;

export const validateZoomRange = (minZoom, maxZoom) => {
  if (!Number.isInteger(minZoom) || !Number.isInteger(maxZoom)) {
    return "El zoom debe ser un numero entero.";
  }
  if (minZoom < MIN_ZOOM || minZoom > MAX_ZOOM) {
    return `El zoom minimo debe estar entre ${MIN_ZOOM} y ${MAX_ZOOM}.`;
  }
  if (maxZoom < MIN_ZOOM || maxZoom > MAX_ZOOM) {
    return `El zoom maximo debe estar entre ${MIN_ZOOM} y ${MAX_ZOOM}.`;
  }
  if (minZoom > maxZoom) {
    return "El zoom maximo debe ser mayor o igual al minimo.";
  }
  return null;
};
