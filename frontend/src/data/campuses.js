// Configuración canónica del campus (SPEC 03).
// Este archivo es la única fuente de verdad para el campus raíz de la plantilla:
// define límites, centro, pisos y el prefijo de los archivos de datos derivados.
// En una instalación nueva se puede editar o eliminar este ejemplo.
//
// Los nombres de archivos de datos derivan de school + campus key:
//   `${school}_${campus}_${floor}.json` y `${school}_${campus}_search.json`.
export default {
  campus: {
    school: "example",
    fullName: "Campus de ejemplo",
    floors: ["0", "1"],
    defaultFloor: "b1",
    center: [-33.45, -70.65],
    zoom: 16,
    bounds: [
      [-33.46, -70.66],
      [-33.44, -70.64],
    ],
  },
  sotero: {
    school: "cs",
    fullName: "Complejo Hospitalario Sótero del Río",
    floors: ["-1", "0", "1", "2", "3", "4", "5"],
    defaultFloor: "b1",
    center: [-33.576, -70.581],
    zoom: 18,
    bounds: [
      [-33.5801, -70.5832],
      [-33.5720, -70.5763],
    ],
  },
};
