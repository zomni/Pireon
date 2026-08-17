// Configuración del campus (SPEC 03).
// La configuración canónica de los sitios/campus la provee siteConfig
// (sesión del backend con fallback a campuses.js); de aquí derivan los
// nombres de archivos de datos, el índice de búsqueda y el catálogo de edificios.

import { getSite, getPrimaryCampusKey } from "../config/siteConfig.js";

export { getPrimaryCampusKey };

const getSchool = (campusKey = getPrimaryCampusKey()) => getSite(campusKey)?.school || "tmpl";

export const getDataFileNames = (campusKey = getPrimaryCampusKey()) => {
  const school = getSchool(campusKey);
  const prefix = `${school}_${campusKey}`;
  return {
    search: `data/${prefix}_search.json`,
    floor: (floor) => `data/${prefix}_${floor}.json`,
  };
};

export const getCatalogFileName = (campusKey = getPrimaryCampusKey()) =>
  `data/${campusKey}_buildings_catalog.json`;

export const getBackupFileName = (campusKey = getPrimaryCampusKey()) =>
  `data/${campusKey}_buildings_backend_backup.json`;
