const fs = require("fs");
const path = require("path");

const campuses = require("../src/data/campuses.js").default;
const campusKey = Object.keys(campuses)[0] || "campus";
const campusConfig = campuses[campusKey] || {};
const prefix = `${campusConfig.school || "example"}_${campusKey}`;
const baseFloor = (campusConfig.floors || ["0"])[0];

const dataDir = path.join(__dirname, "..", "src", "data");
const geojsonPath = path.join(dataDir, `${prefix}_${baseFloor}.json`);
const outputPath = path.join(dataDir, `${campusKey}_buildings_catalog.json`);

function main() {
  const raw = fs.readFileSync(geojsonPath, "utf-8");
  const geojson = JSON.parse(raw);

  const buildings = (geojson.features || []).map((feature) => {
    const p = feature.properties || {};

    return {
      id: p.id || "",
      slug: p.slug || "",
      displayName: p.name || "",
      shortName: p.id || "",
      realName: "",
      type: "unknown",
      floors: [],
      hasInteriorMap: false,
      hasInventory: false,
      responsibleArea: "",
      notes: "",
      sourceId: p.sourceId || "",
      centroid: p.centroid || null
    };
  });

  const result = { buildings };

  fs.writeFileSync(outputPath, JSON.stringify(result, null, 2), "utf-8");

  console.log(`Catálogo generado en: ${outputPath}`);
  console.log(`Total de edificios: ${buildings.length}`);
}

main();
