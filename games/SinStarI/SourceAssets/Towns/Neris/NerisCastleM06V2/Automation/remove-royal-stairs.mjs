// Target only the original Royal Castle; preserve catalog identities and part slots.
import fs from 'node:fs';
import {createHash} from 'node:crypto';
import {runBlender} from './blender-background.mjs';
const root='D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1';
const work='D:/Projects/Sin-Star-I-Assets/Neris-Castle/native-integration';
const hash=b=>createHash('sha256').update(b).digest('hex');
await runBlender(root+'/Authoring/Catalog.blend',String.raw`
import bpy,json,sys,hashlib
from pathlib import Path
from mathutils import Vector
root=Path('${root}')
sys.path.insert(0,str(root/'Source'))
from catalog_scene import assemblies,instance_owner
from catalog_features import collect,finish
catalog=json.loads((root/'Authoring/catalog.json').read_text())
owner=next(a for a in assemblies() if a.label=='Royal Castle')
inverse=owner.matrix.inverted()
removed=[];triangles=[]
for name in owner.members:
    o=bpy.data.objects[name]
    points=[inverse @ o.matrix_world @ Vector(v) for v in o.bound_box]
    lo=[min(v[i] for v in points) for i in range(3)]
    hi=[max(v[i] for v in points) for i in range(3)]
    rail=o.name.startswith('Terrace Handrail.') and 24<=int(o.name.rsplit('.',1)[1])<216
    post=o.name.startswith('Carved Ivory Baluster') and lo[2]<14.4 and lo[1]>-25 and max(abs(lo[0]),abs(hi[0]))>9
    if o.name.startswith('Sweeping Royal Garden Stair') or rail or post:
        removed.append(o.name)
        evaluated=o.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
        matrix=inverse @ evaluated.matrix_world
        for t in mesh.loop_triangles:
            vertices=[matrix @ mesh.vertices[i].co for i in t.vertices]
            if (vertices[1]-vertices[0]).cross(vertices[2]-vertices[0]).length_squared>1e-12:
                triangles.append([[v.x,v.z,-v.y] for v in vertices])
        evaluated.to_mesh_clear()
assert sum(n.startswith('Sweeping Royal Garden Stair') for n in removed)==92
assert sum(n.startswith('Terrace Handrail') for n in removed)==192
for name in removed:bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
assert len([o for o in bpy.data.objects if o.name.startswith('Royal Entrance Stair')])==26
bpy.context.view_layer.update()
owner=next(a for a in assemblies() if a.label=='Royal Castle')
features={'steps':[],'solids':[],'water':[],'camera':[]}
members=set(owner.members)
for inst in bpy.context.evaluated_depsgraph_get().object_instances:
    if inst.object.original.name in members:collect(inst,owner,features)
features=finish(features)
assert len(features['steps'])==27
target=root/'Authoring/Catalog-r002.blend'
assert not target.exists()
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
report={'removed':removed,'triangles':triangles,'features':features,'source_sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
Path('${work}/royal-stairs-removal.json').write_text(json.dumps(report))
print('REMOVED',len(removed),'objects;',len(triangles),'triangles; kept 26 central steps and threshold')
`,work+'/royal-stairs-removal.log');
const report=JSON.parse(fs.readFileSync(work+'/royal-stairs-removal.json'));
const key=tri=>tri.map(v=>v.map(n=>Math.round(n*10000)||0).join(',')).sort().join(';');
const targets=new Set(report.triangles.map(key));
const matched=new Set();
const catalog=JSON.parse(fs.readFileSync(root+'/Authoring/catalog.json'));
const royal=catalog.templates.find(t=>t.label==='Royal Castle');
const changes=[];
for(const chunk of new Set(royal.parts.map(p=>p[0]))) {
  const item=catalog.chunks[chunk], file=root+'/Authoring/'+item.file;
  const raw=fs.readFileSync(file), jl=raw.readUInt32LE(12);
  const doc=JSON.parse(raw.subarray(20,20+jl));
  const bin=raw.subarray(28+jl);
  let removed=0;
  for(const [,part] of royal.parts.filter(p=>p[0]===chunk)) {
    const prim=doc.meshes[part].primitives[0];
    const pos=doc.accessors[prim.attributes.POSITION], pv=doc.bufferViews[pos.bufferView];
    const idx=doc.accessors[prim.indices], iv=doc.bufferViews[idx.bufferView];
    if(idx.componentType!==5125)throw Error('Unexpected index format');
    const offset=(iv.byteOffset||0)+(idx.byteOffset||0);
    const kept=[];
    for(let i=0;i<idx.count;i+=3){
      const ids=[0,1,2].map(j=>bin.readUInt32LE(offset+4*(i+j)));
      const tri=ids.map(id=>[0,1,2].map(j=>bin.readFloatLE((pv.byteOffset||0)+(pos.byteOffset||0)+id*(pv.byteStride||12)+j*4)));
      const k=key(tri);
      if(targets.has(k)){removed++;matched.add(k);}else kept.push(...ids);
    }
    if(kept.length!==idx.count){
      if(!kept.length)throw Error('Removal would empty a stable part slot');
      kept.forEach((v,i)=>bin.writeUInt32LE(v,offset+4*i));idx.count=kept.length;
    }
  }
  if(removed){
    let json=Buffer.from(JSON.stringify(doc));json=Buffer.concat([json,Buffer.alloc((4-json.length%4)%4,32)]);
    const header=Buffer.alloc(20);header.writeUInt32LE(0x46546c67);header.writeUInt32LE(2,4);header.writeUInt32LE(28+json.length+bin.length,8);header.writeUInt32LE(json.length,12);header.writeUInt32LE(0x4e4f534a,16);
    const bh=Buffer.alloc(8);bh.writeUInt32LE(bin.length);bh.writeUInt32LE(0x004e4942,4);
    const output=Buffer.concat([header,json,bh,bin]);
    fs.writeFileSync(file,output);item.sha256=hash(output);item.triangles-=removed;
    changes.push({file:item.file,removedTriangles:removed});
  }
}
if(matched.size!==targets.size)throw Error(`Unmatched stair triangles: ${targets.size-matched.size}/${targets.size}`);
catalog.document_fingerprint=fs.readFileSync('D:/SMILE 2.0/tools/Character3DViewer/TownCatalogData.smile','utf8').match(/FINGERPRINT = "([a-f0-9]+)"/)[1];
catalog.blend_source='Authoring/Catalog-r002.blend';catalog.source_sha256=report.source_sha256;
Object.assign(royal,report.features);
const removed=new Set(report.removed);
for(const item of catalog.instances)if(item.template===royal.id)item.members=item.members.filter(n=>!removed.has(n));
fs.writeFileSync(root+'/Authoring/catalog.json',JSON.stringify(catalog,null,2)+'\n');
fs.writeFileSync(work+'/royal-stairs-checks.json',JSON.stringify({removedObjects:report.removed.length,uniqueTriangles:targets.size,matchedTriangles:matched.size,centralSteps:26,walkSurfaces:royal.steps.length,changes},null,2));
console.log(changes);
