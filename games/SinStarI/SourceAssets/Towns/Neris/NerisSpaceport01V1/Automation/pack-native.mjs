// Preserve every portable triangle; consolidate static parts for native pool limits.
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = process.argv[2] || path.join(root, 'NSP01-preview.glb');
const destination = process.argv[3] || path.join(root, 'Native');
if (fs.existsSync(destination)) throw Error('Use a new revision; Native already exists.');
const bytes = fs.readFileSync(source), length = bytes.readUInt32LE(12);
const original = JSON.parse(bytes.toString('utf8', 20, 20 + length));
const binary = bytes.subarray(28 + length);
const hash = b => createHash('sha256').update(b).digest('hex');
function accessor(index) {
  const a = original.accessors[index], v = original.bufferViews[a.bufferView];
  const width = {SCALAR:1,VEC2:2,VEC3:3,VEC4:4}[a.type];
  const size = {5123:2,5125:4,5126:4}[a.componentType];
  if (!width || !size || a.sparse) throw Error('Unsupported accessor.');
  return Array.from({length:a.count}, (_,i) => Array.from({length:width}, (_,c) => {
    const offset = (v.byteOffset||0)+(a.byteOffset||0)+i*(v.byteStride||width*size)+c*size;
    return a.componentType===5126 ? binary.readFloatLE(offset) : size===2 ? binary.readUInt16LE(offset) : binary.readUInt32LE(offset);
  }));
}
const parts = [], groups = new Map();
let sourceTriangles = 0;
const minimum = [Infinity,Infinity,Infinity], maximum = [-Infinity,-Infinity,-Infinity];
function newPart(material) {
  const part = {material,positions:[],normals:[],uvs:[],indices:[],lookup:new Map(),sources:new Set()};
  parts.push(part); groups.set(material,part); return part;
}
function walk(index, parentTranslation) {
  const node = original.nodes[index];
  // The live entrance reuses Royal Court's already loaded, hinged door models.
  if (node.extras?.kind === 'entrance-door') return;
  if (node.matrix || node.rotation || node.scale || node.skin) throw Error('Unexpected transform: '+node.name);
  const translation = parentTranslation.map((n,i)=>n+(node.translation?.[i]||0));
  if (node.mesh !== undefined) for (const primitive of original.meshes[node.mesh].primitives) {
    const positions = accessor(primitive.attributes.POSITION).map(p=>p.map((v,i)=>Math.fround(v+translation[i])));
    const normals = accessor(primitive.attributes.NORMAL), indices = accessor(primitive.indices).flat();
    const sourceUv = primitive.attributes.TEXCOORD_0 === undefined ? null : accessor(primitive.attributes.TEXCOORD_0);
    if ((primitive.mode ?? 4)!==4 || indices.length%3) throw Error('Expected triangles.');
    for (let i=0;i<indices.length;i+=3) {
      const ids=indices.slice(i,i+3), p=ids.map(j=>positions[j]);
      const ab=p[1].map((v,c)=>v-p[0][c]), ac=p[2].map((v,c)=>v-p[0][c]);
      const normal=[ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0]];
      if (Math.hypot(...normal)<1e-12) throw Error('Zero triangle.');
      const axis=normal.map(Math.abs).indexOf(Math.max(...normal.map(Math.abs)));
      const axes=axis===0?[1,2]:axis===1?[0,2]:[0,1];
      const values=ids.map(j=>({p:positions[j],n:normals[j],uv:sourceUv ? sourceUv[j] : axes.map(c=>Math.fround(positions[j][c]/8))}));
      const keys=values.map(v=>[...v.p,...v.n,...v.uv,axis].join(','));
      let part=groups.get(primitive.material)||newPart(primitive.material);
      const needed=keys.filter(k=>!part.lookup.has(k)).length;
      if (part.positions.length+needed>60000 || part.indices.length+3>196608) part=newPart(primitive.material);
      part.sources.add(node.name);
      for (let c=0;c<3;c++) {
        if (!part.lookup.has(keys[c])) {
          part.lookup.set(keys[c],part.positions.length);
          part.positions.push(values[c].p); part.normals.push(values[c].n); part.uvs.push(values[c].uv);
          values[c].p.forEach((v,k)=>{minimum[k]=Math.min(minimum[k],v);maximum[k]=Math.max(maximum[k],v);});
        }
        part.indices.push(part.lookup.get(keys[c]));
      }
      sourceTriangles++;
    }
  }
  for (const child of node.children||[]) walk(child,translation);
}
for (const root of original.scenes[original.scene||0].nodes) walk(root,[0,0,0]);
const bins=[];
for(const part of [...parts].sort((a,b)=>b.positions.length-a.positions.length)) {
  let bin=bins.find(b=>b.vertices+part.positions.length<=131072 && b.indices+part.indices.length<=393216 && b.parts.length<16);
  if(!bin){bin={vertices:0,indices:0,parts:[]};bins.push(bin);}
  bin.parts.push(part);bin.vertices+=part.positions.length;bin.indices+=part.indices.length;
}
fs.mkdirSync(destination);
const manifest={source_sha256:hash(bytes),triangles:sourceTriangles,parts:parts.length,models:bins.length,
  gltf_bounds:{min:minimum,max:maximum},native_chunks:[],method:'Exact triangles and normals, baked translations, material consolidation, planar UVs and explicit orthonormal tangents for untextured PBR; no simplification.'};
for(const [index,bin] of bins.entries()) {
  const usedMaterials=[...new Set(bin.parts.map(p=>p.material))];
  const json={asset:{version:'2.0',generator:'NSP01 native packing'},scene:0,scenes:[{nodes:[]}],nodes:[],meshes:[],materials:usedMaterials.map(i=>original.materials[i]),accessors:[],bufferViews:[],buffers:[],extensionsUsed:original.extensionsUsed};
  const slices=[];let offset=0;
  if (original.textures) json.textures = original.textures;
  if (original.samplers) json.samplers = original.samplers;
  if (original.images) json.images = original.images.map(im => {
    if (im.bufferView === undefined) throw Error('Only self-contained images are allowed.');
    const view=original.bufferViews[im.bufferView], data=binary.subarray(view.byteOffset||0,(view.byteOffset||0)+view.byteLength);
    const index=json.bufferViews.length;json.bufferViews.push({buffer:0,byteOffset:offset,byteLength:data.length});
    slices.push(data);offset+=data.length;
    return {...im,bufferView:index};
  });
  function add(values,type,componentType) {
    const components={SCALAR:1,VEC2:2,VEC3:3,VEC4:4}[type],flat=values.flat(),size=componentType===5123?2:4;
    const data=Buffer.alloc(flat.length*size);
    flat.forEach((v,i)=>componentType===5126?data.writeFloatLE(v,i*size):data.writeUInt16LE(v,i*size));
    const align=Math.ceil(offset/4)*4;slices.push(Buffer.alloc(align-offset));offset=align;
    const view=json.bufferViews.length;json.bufferViews.push({buffer:0,byteOffset:offset,byteLength:data.length});slices.push(data);offset+=data.length;
    const accessor={bufferView:view,componentType,count:flat.length/components,type};
    if(type==='VEC3') {accessor.min=[0,1,2].map(c=>values.reduce((m,v)=>Math.min(m,v[c]),Infinity));accessor.max=[0,1,2].map(c=>values.reduce((m,v)=>Math.max(m,v[c]),-Infinity));}
    json.accessors.push(accessor);return json.accessors.length-1;
  }
  for (const [partIndex,part] of bin.parts.entries()) {
    const name=`NSP01.Native.${index}.${partIndex}.${original.materials[part.material].name.split('.').pop()}`;
    // There are no normal maps. Supply a stable orthonormal basis rather than
    // accumulating incompatible projection derivatives at smooth trim corners.
    const tangents=part.normals.map(normal=>{
      const length=Math.hypot(...normal), n=normal.map(v=>v/length);
      const axis=n.map(Math.abs).indexOf(Math.min(...n.map(Math.abs)));
      const t=n.map((v,c)=>(c===axis?1:0)-v*n[axis]), magnitude=Math.hypot(...t);
      return [...t.map(v=>v/magnitude),1];
    });
    const attributes={POSITION:add(part.positions,'VEC3',5126),NORMAL:add(part.normals,'VEC3',5126),TEXCOORD_0:add(part.uvs,'VEC2',5126),TANGENT:add(tangents,'VEC4',5126)};
    const indices=add(part.indices,'SCALAR',5123);
    json.scenes[0].nodes.push(json.nodes.length);json.nodes.push({name,mesh:json.meshes.length});
    json.meshes.push({name,primitives:[{attributes,indices,material:usedMaterials.indexOf(part.material)}]});
  }
  slices.push(Buffer.alloc(Math.ceil(offset/4)*4-offset));const data=Buffer.concat(slices);json.buffers=[{byteLength:data.length}];
  const encoded=Buffer.from(JSON.stringify(json)),padded=Buffer.alloc(Math.ceil(encoded.length/4)*4,32);encoded.copy(padded);
  const header=Buffer.alloc(20);header.write('glTF');header.writeUInt32LE(2,4);header.writeUInt32LE(28+padded.length+data.length,8);header.writeUInt32LE(padded.length,12);header.write('JSON',16);
  const bh=Buffer.alloc(8);bh.writeUInt32LE(data.length);bh.write('BIN\0',4);
  const output=Buffer.concat([header,padded,bh,data]),file=`Spaceport-${String(index).padStart(2,'0')}.glb`;
  fs.writeFileSync(path.join(destination,file),output);
  manifest.native_chunks.push({file,sha256:hash(output),vertices:bin.vertices,triangles:bin.indices/3,parts:json.meshes.map(m=>m.name),source_nodes:bin.parts.map(p=>[...p.sources])});
}
if(sourceTriangles<=0 || parts.length>24 || bins.length>6) throw Error('Asset contract failed.');
fs.writeFileSync(path.join(destination,'native-layout.json'),JSON.stringify(manifest,null,2)+'\n');
console.log(JSON.stringify({...manifest,native_chunks:manifest.native_chunks.map(({source_nodes,...c})=>c)},null,2));
