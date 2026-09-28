import fs from 'node:fs';
import {createHash} from 'node:crypto';
const work='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const repo='D:/SMILE 2.0';
const owner=repo+'/games/SinStarI/SourceAssets/Towns/Neris/NerisCastleM06V2';
const input=work+'/exports/M06-r006';
const hash=b=>createHash('sha256').update(b).digest('hex');
function sameGeometry(first,second) {
  const read=file=>{const raw=fs.readFileSync(file),n=raw.readUInt32LE(12);return {json:JSON.parse(raw.subarray(20,20+n)),bin:raw.subarray(n+28)};};
  const a=read(first),b=read(second);
  if(JSON.stringify(a.json)!==JSON.stringify(b.json)||a.bin.length!==b.bin.length)return false;
  const floats=new Set();
  for(const accessor of a.json.accessors)if(accessor.componentType===5126){
    const v=a.json.bufferViews[accessor.bufferView],count={SCALAR:1,VEC2:2,VEC3:3,VEC4:4}[accessor.type];
    for(let i=0;i<accessor.count;i++)for(let c=0;c<count;c++)floats.add((v.byteOffset||0)+(accessor.byteOffset||0)+i*(v.byteStride||count*4)+c*4);
  }
  for(let p=0;p<a.bin.length;p+=4)if(a.bin.readUInt32LE(p)!==b.bin.readUInt32LE(p))
    if(!floats.has(p)||!Number.isFinite(a.bin.readFloatLE(p))||Math.abs(a.bin.readFloatLE(p)-b.bin.readFloatLE(p))>1e-6)return false;
  return true;
}
const layout=JSON.parse(fs.readFileSync(input+'/Native/native-layout.json'));
const old=JSON.parse(fs.readFileSync(owner+'/Native/native-layout.json'));
if(layout.native_chunks.length!==12||layout.total_static_parts!==85)throw Error('Part layout changed');
const changed=[];
for(let i=0;i<12;i++) {
  if(JSON.stringify(layout.native_chunks[i].parts)!==JSON.stringify(old.native_chunks[i].parts))throw Error('Part order changed');
  const chunk=layout.native_chunks[i],file=chunk.file;
  if(chunk.parts.some(name=>name.startsWith('NC.Export.Gatehouse.')))changed.push(file);
  else {
    if(!sameGeometry(input+'/Native/'+file,owner+'/Native/'+file))throw Error('Unchanged chunk geometry differs: '+file);
    fs.copyFileSync(owner+'/Native/'+file,input+'/Native/'+file);
    layout.native_chunks[i]=old.native_chunks[i];
  }
}
for(const file of ['drawbridge.glb','door-west.glb','door-east.glb'])
  if(!sameGeometry(input+'/'+file,owner+'/'+file))throw Error('Unchanged motion owner differs: '+file);
// Blender's UV float round trip can differ by one ULP. Keep unchanged published
// owners byte-identical after validating their complete layout and float payload.
for(const file of ['drawbridge.glb','door-west.glb','door-east.glb'])fs.copyFileSync(owner+'/'+file,input+'/'+file);
fs.writeFileSync(input+'/Native/native-layout.json',JSON.stringify(layout,null,2)+'\n');
for(const file of ['castle.glb','neris-castle.glb'])fs.copyFileSync(input+'/'+file,owner+'/'+file);
for(const file of [...changed,'native-layout.json'])fs.copyFileSync(input+'/Native/'+file,owner+'/Native/'+file);
fs.copyFileSync(work+'/source/neris-castle-M06-r006.blend',owner+'/Source/neris-castle-M06-r006.blend',fs.constants.COPYFILE_EXCL);
for(const file of [owner+'/prepare-native.mjs',repo+'/tools/Character3DViewer/Character3DViewer.smileproj'])
  fs.writeFileSync(file,fs.readFileSync(file,'utf8').replaceAll('r005','r006'));
const catalogFile=repo+'/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/catalog.json';
const catalog=JSON.parse(fs.readFileSync(catalogFile));
const retired=JSON.parse(fs.readFileSync(work+'/native-integration/royal-stairs-removal.json')).removed;
for(const source of catalog.instances)if(source.template===13)source.compatible_retired_members=retired;
fs.writeFileSync(catalogFile,JSON.stringify(catalog,null,2)+'\n');
fs.writeFileSync(work+'/checkpoints/M06-r006/publish-checks.json',JSON.stringify({changedNativeChunks:changed,unchangedMovingOwners:3,staticParts:85,movingParts:13},null,2));
console.log('Updated only affected native chunks: '+changed.join(', '));
