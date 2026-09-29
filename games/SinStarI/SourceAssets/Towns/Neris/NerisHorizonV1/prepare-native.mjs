// Checked disposable native mirrors; authored Blender exports remain canonical.
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const root = path.dirname(fileURLToPath(import.meta.url));
const destination = path.resolve(root, '../../../../../../tools/Character3DViewer/BuildAssets/Neris/Horizon');
const report = JSON.parse(fs.readFileSync(path.join(root,'asset-manifest.json')));
const checksums = JSON.parse(fs.readFileSync(path.join(root,'checksums.json')));
for (const family of ['Horizon','Transport','Royal']) {
  const file = `Native/${family}.glb`, bytes = fs.readFileSync(path.join(root,file));
  if (createHash('sha256').update(bytes).digest('hex') !== checksums[file]) throw Error(`Changed asset: ${file}`);
  const doc = JSON.parse(bytes.toString('utf8',20,20+bytes.readUInt32LE(12)));
  let vertices=0, indices=0;
  if (doc.meshes.length !== report.parts[family] || doc.meshes.length>16) throw Error('Part contract failed.');
  for (const mesh of doc.meshes) for (const p of mesh.primitives) {
    const v=doc.accessors[p.attributes.POSITION].count, i=doc.accessors[p.indices].count;
    if (v>65535 || i>196608 || p.attributes.TEXCOORD_0===undefined || p.attributes.TANGENT===undefined) throw Error('Geometry contract failed.');
    vertices+=v; indices+=i;
  }
  if (vertices>131072 || indices>393216) throw Error('Model exceeds native budget.');
}
fs.mkdirSync(destination,{recursive:true});
for(const family of ['Horizon','Transport','Royal']) fs.copyFileSync(path.join(root,`Native/${family}.glb`),path.join(destination,`${family}.glb`));
fs.copyFileSync(path.join(root,'Horizon.sm3d.json'),path.join(destination,'Horizon.sm3d.json'));
console.log(`Prepared Neris Horizon ${report.revision}: 3 models, ${Object.values(report.parts).reduce((a,b)=>a+b,0)} parts, ${Object.values(report.triangles).reduce((a,b)=>a+b,0)} triangles, including both ships.`);
