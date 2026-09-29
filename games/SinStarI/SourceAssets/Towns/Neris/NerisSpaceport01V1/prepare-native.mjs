import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const root = path.dirname(fileURLToPath(import.meta.url));
const destination = path.resolve(root, '../../../../../../tools/Character3DViewer/BuildAssets/Neris/Spaceport01');
const layout = JSON.parse(fs.readFileSync(path.join(root, 'Native/native-layout.json')));
if (layout.models !== 6 || layout.parts !== 21 || layout.triangles !== 473528)
  throw Error('Review native spaceport contract.');
for (const chunk of layout.native_chunks) {
  const bytes = fs.readFileSync(path.join(root, 'Native', chunk.file));
  if (createHash('sha256').update(bytes).digest('hex') !== chunk.sha256)
    throw Error('Native spaceport checksum changed: ' + chunk.file);
  const json = JSON.parse(bytes.toString('utf8', 20, 20 + bytes.readUInt32LE(12)));
  let vertices = 0, indices = 0;
  for (const mesh of json.meshes) for (const p of mesh.primitives) {
    const v = json.accessors[p.attributes.POSITION].count, i = json.accessors[p.indices].count;
    if (v > 65535 || i > 196608 || p.attributes.TEXCOORD_0 === undefined)
      throw Error('Part budget or UV contract failed.');
    vertices += v; indices += i;
  }
  if (vertices > 131072 || indices > 393216 || json.meshes.length > 16)
    throw Error('Native model budget exceeded.');
}
fs.mkdirSync(destination, {recursive:true});
for (const chunk of layout.native_chunks)
  fs.copyFileSync(path.join(root, 'Native', chunk.file), path.join(destination, chunk.file));
fs.copyFileSync(path.join(root, 'Spaceport.sm3d.json'), path.join(destination, 'Spaceport.sm3d.json'));
console.log('Prepared Neris Spaceport r08: 6 models, 21 static parts, 473528 static triangles.');
