// Validate and mirror Blender's native preview exports; no source geometry is rewritten.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const source = path.dirname(fileURLToPath(import.meta.url));
const repository = path.resolve(source, '../../../../../..');
const destination = path.join(repository, 'tools/Character3DViewer/BuildAssets/Neris/M01');
for (const [file, parts] of [['castle.glb', 4], ['drawbridge.glb', 1]]) {
  const bytes = fs.readFileSync(path.join(source, file));
  if (bytes.toString('ascii', 0, 4) !== 'glTF') throw Error('Invalid GLB: ' + file);
  const json = JSON.parse(bytes.toString('utf8', 20, 20 + bytes.readUInt32LE(12)));
  if (json.meshes.length !== parts) throw Error('Unexpected part layout: ' + file);
  if (file === 'castle.glb' && json.meshes[2].name !== 'NC.Export.castle.NC.MAT.Moat')
    throw Error('Moat part changed; review the native water exclusion.');
  for (const mesh of json.meshes) for (const primitive of mesh.primitives)
    if (primitive.attributes.TEXCOORD_0 === undefined) throw Error('Native preview requires UVs.');
}
fs.mkdirSync(destination, { recursive: true });
for (const file of ['castle.glb', 'drawbridge.glb', 'Castle.sm3d.json'])
  fs.copyFileSync(path.join(source, file), path.join(destination, file));
console.log('Prepared provisional Neris Castle M01 r003 inputs.');
