// Checked disposable native mirrors; authored Blender exports remain canonical.
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const root = path.dirname(fileURLToPath(import.meta.url));
const destination = path.resolve(root, '../../../../../../tools/Character3DViewer/BuildAssets/Neris/Horizon');
const report = JSON.parse(fs.readFileSync(path.join(root,'asset-manifest.json')));
const checksums = JSON.parse(fs.readFileSync(path.join(root,'checksums.json')));
for (const family of ['Horizon','Transport','Royal','Cargo']) {
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
  if (family === 'Horizon') {
    // Regression: subpixel runway paint must use mip filtering, not thin overlay meshes.
    const runway = doc.materials.find(m => m.name === 'GW Filtered Runway Surface');
    const texture = doc.textures?.[runway?.pbrMetallicRoughness.baseColorTexture?.index];
    if (!texture || doc.samplers[texture.sampler].minFilter !== 9987)
      throw Error('Runway paint lost its mipmapped surface texture.');
    const image = doc.images[texture.source], view = doc.bufferViews[image.bufferView];
    const offset = 28 + bytes.readUInt32LE(12) + view.byteOffset;
    if (bytes.readUInt32BE(offset+16) !== 4096 || bytes.readUInt32BE(offset+20) !== 640)
      throw Error('Runway paint atlas resolution changed.');
  }
  if (family !== 'Horizon') {
    // Regression: smoothing the replacement hull must retain the native -Z nose.
    // The cooker reflects GLB Z; elevated cockpit vertices must therefore be +Z.
    const cockpit = doc.meshes.find(m => m.name === 'GW Glass Dark').primitives[0];
    const accessor = doc.accessors[cockpit.attributes.POSITION];
    const offset = 28 + bytes.readUInt32LE(12) + doc.bufferViews[accessor.bufferView].byteOffset;
    const scale = family === 'Royal' ? .82 : family === 'Cargo' ? 1.3 : 1;
    const forward = [];
    for (let i=0; i<accessor.count; i++) {
      if (bytes.readFloatLE(offset+i*12+4)>18*scale) forward.push(bytes.readFloatLE(offset+i*12+8));
    }
    if (!forward.length || forward.reduce((a,b)=>a+b,0)/forward.length<=0) throw Error(`${family} nose axis changed.`);
  }
}
fs.mkdirSync(destination,{recursive:true});
for(const family of ['Horizon','Transport','Royal','Cargo']) fs.copyFileSync(path.join(root,`Native/${family}.glb`),path.join(destination,`${family}.glb`));
fs.copyFileSync(path.join(root,'Horizon.sm3d.json'),path.join(destination,'Horizon.sm3d.json'));
console.log(`Prepared Neris Horizon ${report.revision}: 4 models, ${Object.values(report.parts).reduce((a,b)=>a+b,0)} parts, ${Object.values(report.triangles).reduce((a,b)=>a+b,0)} triangles, including the cargo ship.`);
