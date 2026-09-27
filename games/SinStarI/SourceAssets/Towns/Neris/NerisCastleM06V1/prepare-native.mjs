// Mirror bounded native chunks derived from the full portable M06 r002 GLB.
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const source = path.dirname(fileURLToPath(import.meta.url));
const repository = path.resolve(source, '../../../../../..');
const destination = path.join(repository, 'tools/Character3DViewer/BuildAssets/Neris/CastleV1');
const layout = JSON.parse(fs.readFileSync(path.join(source, 'Native/native-layout.json')));
if (layout.native_chunks.length !== 8 || layout.total_static_parts !== 39)
  throw Error('Native castle layout changed; review NerisCastlePreview.');
const inputs = layout.native_chunks.map(chunk => ({
  from: 'Native/' + chunk.file, file: chunk.file, sha256: chunk.sha256, parts: chunk.parts
}));
inputs.push({from: 'drawbridge.glb', file: 'drawbridge.glb',
  sha256: 'f07d7cac8e5f8574ce16a1ca3f679b0babf1f272e43a9cab4d049637e15cbea0',
  parts: ['NC.Export.Bridge.BridgeTimber', 'NC.Export.Bridge.DarkIron', 'NC.Export.Bridge.RoyalGold']});
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
console.log('Prepared Neris Castle M06 r002: 8 static chunks, 39 static parts, 3 hinged parts.');
