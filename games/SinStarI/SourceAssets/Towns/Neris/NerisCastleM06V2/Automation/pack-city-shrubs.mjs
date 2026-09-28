// Use spare capacity in existing models so the optional Tripo castle still fits.
import fs from 'node:fs';
import {createHash} from 'node:crypto';
const root='D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring';
const check='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/City-Hall-r001';
const catalog=JSON.parse(fs.readFileSync(root+'/catalog.json'));
function read(file){const raw=fs.readFileSync(file),n=raw.readUInt32LE(12);return {doc:JSON.parse(raw.subarray(20,20+n)),bin:raw.subarray(28+n)};}
const moves=new Map(),outputs=[];
for(const [chunk,sourceChunk,parts] of [[0,29,[11,12,13,14,15]],[29,30,[0,1,2,3,4]]]){
 const source=read(root+'/Templates/Catalog-'+sourceChunk+'.glb');
 const item=catalog.chunks[chunk],file=root+'/'+item.file,{doc,bin}=read(file),buffers=[bin];let length=bin.length;
 if(chunk===29){
  if(doc.nodes.some(n=>Object.keys(n).some(k=>!['mesh','name'].includes(k))))throw Error('Unexpected transformed catalog node');
  doc.meshes=doc.meshes.slice(0,11);doc.nodes=doc.nodes.filter(n=>n.mesh<11);doc.scenes[0].nodes=doc.nodes.map((_,i)=>i);
 }
 doc.extensionsUsed=[...new Set([...(doc.extensionsUsed||[]),...(source.doc.extensionsUsed||[])])];
 for(const part of parts){
  const mesh=structuredClone(source.doc.meshes[part]),primitive=mesh.primitives[0];
  const references=[['indices',primitive.indices],...Object.entries(primitive.attributes)];
  for(const [name,index] of references){
   const accessor=structuredClone(source.doc.accessors[index]),view=structuredClone(source.doc.bufferViews[accessor.bufferView]);
   const bytes=source.bin.subarray(view.byteOffset||0,(view.byteOffset||0)+view.byteLength);
   const padding=Buffer.alloc((4-length%4)%4);buffers.push(padding,bytes);length+=padding.length;
   view.byteOffset=length;view.buffer=0;length+=bytes.length;
   accessor.bufferView=doc.bufferViews.length;doc.bufferViews.push(view);
   if(name==='indices')primitive.indices=doc.accessors.length;else primitive.attributes[name]=doc.accessors.length;
   doc.accessors.push(accessor);
  }
  const material=structuredClone(source.doc.materials[primitive.material]);
  if(JSON.stringify(material).includes('Texture'))throw Error('Unexpected textured shrub material');
  primitive.material=doc.materials.length;doc.materials.push(material);
  moves.set(sourceChunk+':'+part,[chunk,doc.meshes.length]);
  doc.nodes.push({name:mesh.name,mesh:doc.meshes.length});doc.scenes[0].nodes.push(doc.nodes.length-1);doc.meshes.push(mesh);
 }
 const vertices=doc.meshes.reduce((n,m)=>n+doc.accessors[m.primitives[0].attributes.POSITION].count,0);
 const triangles=doc.meshes.reduce((n,m)=>n+doc.accessors[m.primitives[0].indices].count/3,0);
 if(vertices>131072||triangles>130000||doc.meshes.length>16)throw Error('Native model capacity exceeded');
 buffers.push(Buffer.alloc((4-length%4)%4));const binary=Buffer.concat(buffers);doc.buffers[0].byteLength=binary.length;
 const text=Buffer.from(JSON.stringify(doc)),json=Buffer.alloc(Math.ceil(text.length/4)*4,32);text.copy(json);
 const head=Buffer.alloc(20);head.writeUInt32LE(0x46546c67);head.writeUInt32LE(2,4);head.writeUInt32LE(28+json.length+binary.length,8);head.writeUInt32LE(json.length,12);head.writeUInt32LE(0x4e4f534a,16);
 const bh=Buffer.alloc(8);bh.writeUInt32LE(binary.length);bh.writeUInt32LE(0x004e4942,4);
 const bytes=Buffer.concat([head,json,bh,binary]);item.sha256=createHash('sha256').update(bytes).digest('hex');item.parts=doc.meshes.length;item.triangles=triangles;
 outputs.push({file,bytes,vertices,triangles,parts:doc.meshes.length});
}
for(const template of catalog.templates)template.parts=template.parts.map(p=>moves.get(p.join(':'))||p);
if(catalog.chunks.length!==31)throw Error('Unexpected catalog revision');
catalog.chunks.pop();
for(const output of outputs)fs.writeFileSync(output.file,output.bytes);
fs.writeFileSync(root+'/catalog.json',JSON.stringify(catalog,null,2)+'\n');
fs.unlinkSync(root+'/Templates/Catalog-30.glb');
fs.writeFileSync(check+'/packing.json',JSON.stringify({modelCount:30,additionalModels:0,moves:[...moves],outputs:outputs.map(({bytes,...o})=>o)},null,2));
console.log('PASS shrubs packed into existing models; no extra model slots.');
