// Mirror bounded native chunks derived from the full portable M06 r006 GLB.
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const source = path.dirname(fileURLToPath(import.meta.url));
const repository = path.resolve(source, '../../../../../..');
const destination = path.join(repository, 'tools/Character3DViewer/BuildAssets/Neris/CastleV2');
const layout = JSON.parse(fs.readFileSync(path.join(source, 'Native/native-layout.json')));
if (layout.native_chunks.length !== 12 || layout.total_static_parts !== 85)
  throw Error('Native castle layout changed; review NerisCastlePreview.');
const inputs = layout.native_chunks.map(chunk => ({
  from: 'Native/' + chunk.file, file: chunk.file, sha256: chunk.sha256, parts: chunk.parts
}));
inputs.push(...[
  {
    "from": "drawbridge.glb",
    "file": "drawbridge.glb",
    "sha256": "b46fc6f28cb70b11fe25a5106f90e43ed301f96daf1935606bfaaaf260bb4a98",
    "parts": [
      "NC.Export.Bridge.BridgeTimber",
      "NC.Export.Bridge.CrystalBlue",
      "NC.Export.Bridge.DarkIron",
      "NC.Export.Bridge.IvoryStone",
      "NC.Export.Bridge.PolishedBrass",
      "NC.Export.Bridge.RoyalDoor",
      "NC.Export.Bridge.RoyalGold"
    ]
  },
  {
    "from": "door-west.glb",
    "file": "door-west.glb",
    "sha256": "784057dceeef1e495f0ec1ff38fa0b04e96457cec251fc72a72fa37349b8b099",
    "parts": [
      "NC.Export.DoorWest.PolishedBrass",
      "NC.Export.DoorWest.RoyalDoor",
      "NC.Export.DoorWest.RoyalGold"
    ]
  },
  {
    "from": "door-east.glb",
    "file": "door-east.glb",
    "sha256": "eb746c53b8d77976b9c526b1573e24fa53b31abcb66ad7f0db1c1d9a16c74699",
    "parts": [
      "NC.Export.DoorEast.PolishedBrass",
      "NC.Export.DoorEast.RoyalDoor",
      "NC.Export.DoorEast.RoyalGold"
    ]
  }
]);
for (const input of inputs) {
  const bytes = fs.readFileSync(path.join(source, input.from));
  if (createHash('sha256').update(bytes).digest('hex') !== input.sha256)
    throw Error('Export changed; review native contract: ' + input.file);
  const json = JSON.parse(bytes.toString('utf8', 20, 20 + bytes.readUInt32LE(12)));
  if (JSON.stringify(json.meshes.map(m => m.name)) !== JSON.stringify(input.parts))
    throw Error('Part layout changed: ' + input.file);
  let vertices = 0, indices = 0;
  for (const mesh of json.meshes) for (const primitive of mesh.primitives) {
    const count = json.accessors[primitive.attributes.POSITION].count;
    const indexCount = json.accessors[primitive.indices].count;
    if (count > 65535 || indexCount > 196608 || primitive.attributes.TEXCOORD_0 === undefined)
      throw Error('Native primitive contract failed: ' + mesh.name);
    vertices += count;
    indices += indexCount;
  }
  if (vertices > 131072 || indices > 393216 || json.meshes.length > 16)
    throw Error('Native model budget exceeded: ' + input.file);
}
const texture = fs.readFileSync(path.join(source, 'Native', layout.texture.file));
if (createHash('sha256').update(texture).digest('hex') !== layout.texture.sha256)
  throw Error('Native masonry texture changed.');
fs.mkdirSync(destination, { recursive: true });
fs.writeFileSync(path.join(destination, layout.texture.file), texture);
for (const input of inputs)
  fs.copyFileSync(path.join(source, input.from), path.join(destination, input.file));
fs.copyFileSync(path.join(source, 'Castle.sm3d.json'), path.join(destination, 'Castle.sm3d.json'));
console.log('Prepared Neris Castle M06 r006: 12 static chunks, 85 static parts, 7 bridge parts and 6 door parts.');
