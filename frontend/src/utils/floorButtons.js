// Resolución del id de botón de piso (`b0`, `b1`, ...) a partir de la
// configuración del campus. Los sitios remotos guardan el piso por defecto como
// valor ("0", "1", ...) mientras que la plantilla estática usa el id del botón
// ("b1"); este helper normaliza ambos casos.

export const resolveFloorButtonId = (defaultFloor, floors) => {
  const normalized = String(defaultFloor ?? "");

  if (normalized.startsWith("b")) {
    return normalized;
  }

  const floorList = Array.isArray(floors) ? floors : [];
  const index = floorList.findIndex((floor) => String(floor) === normalized);
  return index >= 0 ? `b${index}` : "b0";
};
