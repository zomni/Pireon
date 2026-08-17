// Cálculo del conjunto de ids de edificios permitidos por piso según el
// catálogo de edificios del campus. Si no hay catálogo (campus nuevo sin datos)
// se devuelve `null` para indicar "permitir todos", evitando que un plano
// GeoJSON subido sea filtrado por completo.

export const computeAllowedBuildingIdsForFloor = (buildings, floorNumber) => {
  if (!Array.isArray(buildings) || buildings.length === 0) {
    return null;
  }

  const allowedIds = new Set();

  for (const building of buildings) {
    const floors = Array.isArray(building.floors) ? building.floors : [];

    if (floors.length > 0) {
      if (floors.includes(Number(floorNumber))) {
        allowedIds.add(building.id);
      }
    } else if (Number(floorNumber) === 0) {
      allowedIds.add(building.id);
    }
  }

  return allowedIds;
};
