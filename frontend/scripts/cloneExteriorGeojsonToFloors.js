const fs = require("fs");
const path = require("path");

const campuses = require("../src/data/campuses.js").default;
const campusKey = Object.keys(campuses)[0] || "campus";
const campusConfig = campuses[campusKey] || {};
const prefix = `${campusConfig.school || "example"}_${campusKey}`;
const floors = (campusConfig.floors || ["0"]).map(String);

const dataDir = path.join(__dirname, "..", "src", "data");
const sourcePath = path.join(dataDir, `${prefix}_${floors[0]}.json`);

const targetFloors = floors.slice(1);

function main() {
  const source = JSON.parse(fs.readFileSync(sourcePath, "utf-8"));

  for (const floor of targetFloors) {
    const cloned = {
      ...source,
      features: (source.features || []).map((feature) => ({
        ...feature,
        properties: {
          ...(feature.properties || {}),
          floor: floor
        }
      }))
    };

    const targetPath = path.join(dataDir, `${prefix}_${floor}.json`);
    fs.writeFileSync(targetPath, JSON.stringify(cloned, null, 2), "utf-8");
    console.log(`Generado: ${targetPath}`);
  }
}

main();
