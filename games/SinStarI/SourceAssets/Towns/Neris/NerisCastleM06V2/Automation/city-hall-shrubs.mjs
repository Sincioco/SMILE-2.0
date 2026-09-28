// Reuse Royal Court foliage in the four front City Hall planters; keep item/member IDs.
import fs from 'node:fs';
import {createHash} from 'node:crypto';
import {runBlender} from './blender-background.mjs';
const root='D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1';
const work='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const checks=work+'/checkpoints/City-Hall-r001';
fs.mkdirSync(checks,{recursive:true});
await runBlender(root+'/Authoring/Catalog-r002.blend',String.raw`
import bpy,json,sys,math,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
root=Path('${root}');check=Path('${checks}')
sys.path.insert(0,str(root/'Source'))
from static_glb import write
city=bpy.data.objects['Neris-City-Hall Editable'];inverse=city.matrix_world.inverted()
catalog=json.loads((root/'Authoring/catalog.json').read_text())
old=[bpy.data.objects['Clipped Cypress.%03d'%i] for first in [40,45,60,65] for i in range(first,first+5)]
assert all(o in city.children_recursive for o in old)
removed=[]
for obj in old:
 mesh=obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh();mesh.calc_loop_triangles()
 matrix=inverse@obj.matrix_world
 for t in mesh.loop_triangles:
  vertices=[matrix@mesh.vertices[i].co for i in t.vertices]
  if (vertices[1]-vertices[0]).cross(vertices[2]-vertices[0]).length_squared>1e-12:
   removed.append([[v.x,v.z,-v.y] for v in vertices])
source='${work}/source/neris-castle-M06-r006.blend'
with bpy.data.libraries.load(source,link=False) as (src,dst):
 prefix='NC.Courtyard.Planter.West.2.Cypress.'
 dst.objects=[n for n in src.objects if n.startswith(prefix)]
for obj in dst.objects:bpy.context.scene.collection.objects.link(obj)
bpy.context.view_layer.update()
source_groups={}
for obj in dst.objects:
 evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
 group=source_groups.setdefault(mesh.materials[0].name,{'material':bpy.data.materials[mesh.materials[0].name],'vertices':[],'faces':[]})
 start=len(group['vertices']);group['vertices'].extend(obj.matrix_world@v.co for v in mesh.vertices)
 group['faces'].extend(tuple(start+i for i in p.vertices) for p in mesh.polygons)
 evaluated.to_mesh_clear()
points=[p for g in source_groups.values() for p in g['vertices']]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=(lo+hi)*.5
soil=bpy.data.materials.new('City Hall Planter Soil');soil.use_nodes=True
soil.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.07,.04,.018,1)
soil.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.95
names=sorted(source_groups)
assert len(names)==4
for first,(x,y) in zip([40,45,60,65],[(-9.1,-7.7),(-5,-8),(9.1,-7.7),(5,-8)]):
 for i,name in enumerate(names):
  group=source_groups[name];mesh=bpy.data.meshes.new('City Hall Royal Court Shrub '+str(first)+' '+name)
  vertices=[(x+(p.x-center.x)*1.05/(hi.x-lo.x),y+(p.y-center.y)*1.05/(hi.y-lo.y),1.20+(p.z-lo.z)*1.75/(hi.z-lo.z)) for p in group['vertices']]
  mesh.from_pydata(vertices,[],group['faces']);mesh.materials.append(group['material']);mesh.update()
  for polygon in mesh.polygons:polygon.use_smooth=True
  obj=bpy.data.objects['Clipped Cypress.%03d'%(first+i)];obj.data=mesh;obj.modifiers.clear();obj.matrix_world=city.matrix_world
  obj['royal_court_shrub_source']=prefix;obj['foliage_height']=1.75
 mesh=bpy.data.meshes.new('City Hall Planter Soil '+str(first))
 vertices=[(x+math.cos(i*math.tau/32)*.50,y+math.sin(i*math.tau/32)*.50,1.19) for i in range(32)]
 mesh.from_pydata(vertices,[],[tuple(range(32))]);mesh.materials.append(soil)
 obj=bpy.data.objects['Clipped Cypress.%03d'%(first+4)];obj.data=mesh;obj.modifiers.clear();obj.matrix_world=city.matrix_world
 obj['royal_court_shrub_source']='soil beneath reused foliage'
for obj in dst.objects:bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.view_layer.update()
# Export only replacement geometry, preserving all existing model/part identities.
groups={}
for obj in old:
 evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
 matrix=inverse@obj.matrix_world;normal=matrix.to_3x3().inverted().transposed()
 group=groups.setdefault(mesh.materials[0].name,[bpy.data.materials[mesh.materials[0].name],[]])
 for t in mesh.loop_triangles:
  corners=tuple((tuple(matrix@mesh.vertices[mesh.loops[i].vertex_index].co),tuple((normal@mesh.corner_normals[i].vector).normalized())) for i in t.loops)
  a,b,c=[Vector(p[0]) for p in corners]
  if (b-a).cross(c-a).length_squared>1e-12:group[1].append(corners)
 evaluated.to_mesh_clear()
batch=[(name,mat,tri,len(set(c for t in tri for c in t))) for name,(mat,tri) in sorted(groups.items())]
chunk=len(catalog['chunks']);output=check/('Catalog-%02d.glb'%chunk)
write(output,batch)
assert len(batch)==5
target=root/'Authoring/Catalog-r003.blend';assert not target.exists()
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
report={'removed':removed,'chunk':chunk,'parts':len(batch),'addedTriangles':sum(len(t) for _,_,t,_ in batch),'vertices':sum(n for _,_,_,n in batch),'source_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'objects':[o.name for o in old],'source':source,'height':1.75,'width':1.05}
(check/'replacement.json').write_text(json.dumps(report))
# Saved source stays fully visible. Only this preview isolates City Hall.
keep=set(city.children_recursive)|{city}
for obj in bpy.context.scene.objects:obj.hide_render=obj not in keep
scene=bpy.context.scene
world=bpy.data.worlds.new('City Hall Preview');world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.32,.38,.44,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6;scene.world=world
ld=bpy.data.lights.new('City Hall Preview Sun','SUN');ld.energy=2
light=bpy.data.objects.new(ld.name,ld);scene.collection.objects.link(light);light.rotation_euler=(.5,-.4,-.7)
cd=bpy.data.cameras.new('City Hall Shrubs Preview');camera=bpy.data.objects.new(cd.name,cd);scene.collection.objects.link(camera)
aim=city.matrix_world@Vector((7.1,-7.9,1.6));camera.location=city.matrix_world@Vector((8,-17,7))
camera.rotation_euler=(aim-camera.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=15;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1280;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.filepath=str(check/'city-hall-shrubs.png');bpy.ops.render.render(write_still=True)
print('PASS City Hall shrubs',report['addedTriangles'],'triangles',report['vertices'],'vertices')
`,checks+'/build-fixed.log');
const report=JSON.parse(fs.readFileSync(checks+'/replacement.json'));
const catalog=JSON.parse(fs.readFileSync(root+'/Authoring/catalog.json'));
const city=catalog.templates.find(t=>t.label==='City Hall');
const key=tri=>tri.map(v=>v.map(n=>Math.round(n*10000)||0).join(',')).sort().join(';');
const targets=new Set(report.removed.map(key)), matched=new Set();
const changes=[];
for(const chunk of new Set(city.parts.map(p=>p[0]))) {
 const item=catalog.chunks[chunk],file=root+'/Authoring/'+item.file;
 const raw=fs.readFileSync(file),jl=raw.readUInt32LE(12),doc=JSON.parse(raw.subarray(20,20+jl)),bin=raw.subarray(28+jl);
 let removed=0;
 for(const [,part] of city.parts.filter(p=>p[0]===chunk)) {
  const prim=doc.meshes[part].primitives[0],pos=doc.accessors[prim.attributes.POSITION],pv=doc.bufferViews[pos.bufferView],idx=doc.accessors[prim.indices],iv=doc.bufferViews[idx.bufferView];
  if(idx.componentType!==5125)throw Error('Unexpected index format');
  const offset=(iv.byteOffset||0)+(idx.byteOffset||0),kept=[];
  for(let i=0;i<idx.count;i+=3){
   const ids=[0,1,2].map(j=>bin.readUInt32LE(offset+4*(i+j)));
   const tri=ids.map(id=>[0,1,2].map(j=>bin.readFloatLE((pv.byteOffset||0)+(pos.byteOffset||0)+id*(pv.byteStride||12)+j*4)));
   const k=key(tri);if(targets.has(k)){removed++;matched.add(k);}else kept.push(...ids);
  }
  if(kept.length!==idx.count){if(!kept.length)throw Error('Stable part became empty');kept.forEach((v,i)=>bin.writeUInt32LE(v,offset+4*i));idx.count=kept.length;}
 }
 if(removed){
  let json=Buffer.from(JSON.stringify(doc));json=Buffer.concat([json,Buffer.alloc((4-json.length%4)%4,32)]);
  const head=Buffer.alloc(20);head.writeUInt32LE(0x46546c67);head.writeUInt32LE(2,4);head.writeUInt32LE(28+json.length+bin.length,8);head.writeUInt32LE(json.length,12);head.writeUInt32LE(0x4e4f534a,16);
  const bh=Buffer.alloc(8);bh.writeUInt32LE(bin.length);bh.writeUInt32LE(0x004e4942,4);
  const output=Buffer.concat([head,json,bh,bin]);fs.writeFileSync(file,output);
  item.sha256=createHash('sha256').update(output).digest('hex');item.triangles-=removed;changes.push({file:item.file,removedTriangles:removed});
 }
}
if(matched.size!==targets.size)throw Error('Unmatched City Hall foliage triangles');
const filename=`Templates/Catalog-${String(report.chunk).padStart(2,'0')}.glb`;
const raw=fs.readFileSync(checks+'/'+filename.split('/')[1]);fs.writeFileSync(root+'/Authoring/'+filename,raw,{flag:'wx'});
catalog.chunks.push({file:filename,parts:report.parts,triangles:report.addedTriangles,sha256:createHash('sha256').update(raw).digest('hex')});
for(let i=0;i<report.parts;i++)city.parts.push([report.chunk,i]);
catalog.blend_source='Authoring/Catalog-r003.blend';catalog.source_sha256=report.source_sha256;
fs.writeFileSync(root+'/Authoring/catalog.json',JSON.stringify(catalog,null,2)+'\n');
fs.writeFileSync(checks+'/checks.json',JSON.stringify({planters:4,changedObjects:report.objects,source:report.source,height:report.height,width:report.width,matchedOldTriangles:matched.size,changes,newChunk:filename,triangles:report.addedTriangles,vertices:report.vertices,stableDocumentFingerprint:catalog.document_fingerprint},null,2));
console.log('PASS Published four City Hall shrubs; preserved catalog/member identities.');
