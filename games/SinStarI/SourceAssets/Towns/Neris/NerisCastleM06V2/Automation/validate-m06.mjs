// Focused offline GLB structure/geometry checks. This is not the Khronos validator.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const directory=fs.existsSync(path.join(root,'neris-castle.glb')) ? root :
 path.join(root,'exports',process.argv[2] || 'M06-r002');
function read(file){
 const bytes=fs.readFileSync(file);
 if(bytes.toString('ascii',0,4)!=='glTF'||bytes.readUInt32LE(4)!==2||bytes.readUInt32LE(8)!==bytes.length)throw Error('Invalid GLB header '+file);
 const length=bytes.readUInt32LE(12),json=JSON.parse(bytes.toString('utf8',20,20+length)),start=20+length;
 if(bytes.toString('ascii',start+4,start+8)!=='BIN\0')throw Error('Embedded BIN expected');
 return {bytes,json,bin:bytes.subarray(start+8)};
}
function accessor(g,id){
 const a=g.json.accessors[id],view=g.json.bufferViews[a.bufferView],components={SCALAR:1,VEC2:2,VEC3:3,VEC4:4}[a.type];
 const sizes={5126:4,5125:4,5123:2,5121:1},size=sizes[a.componentType],stride=view.byteStride||components*size;
 if(!components||!size||a.sparse)throw Error('Unexpected accessor encoding');
 const offset=(view.byteOffset||0)+(a.byteOffset||0);
 if(offset+(a.count-1)*stride+components*size>g.bin.length)throw Error('Accessor out of bounds');
 const values=[];
 for(let i=0;i<a.count;i++){
  const row=[];for(let c=0;c<components;c++){
   const p=offset+i*stride+c*size;
   row.push(a.componentType===5126?g.bin.readFloatLE(p):a.componentType===5125?g.bin.readUInt32LE(p):a.componentType===5123?g.bin.readUInt16LE(p):g.bin[p]);
  }values.push(row);
 }
 if(values.flat().some(v=>!Number.isFinite(v)))throw Error('Non-finite accessor');
 return values;
}
const checks=[];
for(const file of ['neris-castle.glb','castle.glb','drawbridge.glb','door-west.glb','door-east.glb']){
 const g=read(path.join(directory,file));let triangles=0,degenerateUV=0;
 for(const mesh of g.json.meshes)for(const primitive of mesh.primitives){
  if((primitive.mode??4)!==4)throw Error('Triangle list expected');
  const pos=accessor(g,primitive.attributes.POSITION),normal=accessor(g,primitive.attributes.NORMAL),uv=accessor(g,primitive.attributes.TEXCOORD_0),indices=accessor(g,primitive.indices).flat();
  if(pos.length>65535)throw Error('Native primitive exceeds 65,535 vertices: '+mesh.name);
  if(pos.length!==normal.length||pos.length!==uv.length||indices.length%3)throw Error('Attribute mismatch');
  if(indices.some(v=>v>=pos.length))throw Error('Index exceeds vertices');
  for(let i=0;i<indices.length;i+=3){
   const [a,b,c]=indices.slice(i,i+3).map(n=>uv[n]);
   const area=(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]);
   if(Math.abs(area)<1e-13)degenerateUV++;
  }
  triangles+=indices.length/3;
 }
 for(const b of g.json.buffers)if(b.uri)throw Error('Unexpected external buffer');
 for(const i of g.json.images||[])if(i.uri||i.mimeType!=='image/png')throw Error('Embedded PNG expected');
 if(degenerateUV)throw Error(file+': '+degenerateUV+' degenerate UV triangles');
 if(JSON.stringify(g.json).match(/[A-Z]:[\\/]|Users[\\/]|auth|token|password/i))throw Error('Unexpected local path or sensitive metadata');
 checks.push({file,bytes:g.bytes.length,sha256:crypto.createHash('sha256').update(g.bytes).digest('hex'),meshes:g.json.meshes.length,materials:g.json.materials.length,triangles,degenerateUV,embeddedImages:(g.json.images||[]).length});
}
const probe=read(path.join(root,'checkpoints/M06-r001/axis-probe.glb'));
const marker=probe.json.nodes.find(n=>n.name==='NC.AxisProbe');
if(JSON.stringify(marker.translation)!=='[3,11,-7]')throw Error('Axis marker mismatch');
const full=read(path.join(directory,'neris-castle.glb'));
const pivot=full.json.nodes.find(n=>n.name==='NC.Export.Bridge.Pivot');
if(JSON.stringify(pivot.translation)!=='[0,-0.25,64]'||pivot.children.length!==7)throw Error('Bridge pivot contract changed');
const report={checks,asymmetricMarker:{blender:[3,7,11],gltf:marker.translation,passed:true},pivot:{gltf:pivot.translation,movingParts:7},khronosValidator:'not run: no existing offline validator located; no installation or npm used'};
fs.writeFileSync(path.join(directory,'offline-validation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
