// Alternate front/back relay dishes with the unchanged left/right crystal poles.
import fs from 'node:fs';
import {createHash} from 'node:crypto';
import {runBlender} from './blender-background.mjs';
const town='D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1';
const work='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const check=work+'/checkpoints/Tower-r001';fs.mkdirSync(check,{recursive:true});
await runBlender(town+'/Authoring/Catalog-r003.blend',String.raw`
import bpy,json,hashlib,math,sys
from pathlib import Path
from mathutils import Vector,Matrix
from types import SimpleNamespace
sys.path.insert(0,'${town}/Source')
from catalog_features import collect,finish
root=Path('${town}');check=Path('${check}')
tower=bpy.data.objects['Neris-Communication-Tower Editable'];inverse=tower.matrix_world.inverted()
parts=[o for o in tower.children_recursive if 'Dish' in o.name]
assert len(parts)==16
triangles=[];changes=[]
for obj in parts:
 points=[inverse@obj.matrix_world@Vector(v) for v in obj.bound_box]
 sign=1 if sum(v.x for v in points)>0 else -1
 start=2.575 if obj.name.startswith('Dish Mount Arm') else 2.675
 mount=obj.name.startswith(('Dish Mount Arm','Dish Lower Brace'))
 offset=Matrix.Translation(Vector((0,-1.65,0)))
 if mount:
  offset[1][0]=-.7*sign/(4.65-start);offset[1][3]= -1.65+.7*4.65/(4.65-start)
 transform=Matrix.Rotation(math.pi/2,4,'Z')@offset
 basis=Matrix(((1,0,0,0),(0,0,1,0),(0,-1,0,0),(0,0,0,1)))
 native=basis@transform@basis.inverted();normal=native.to_3x3().inverted().transposed()
 evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
 matrix=inverse@obj.matrix_world
 for t in mesh.loop_triangles:
  vertices=[matrix@mesh.vertices[i].co for i in t.vertices]
  if (vertices[1]-vertices[0]).cross(vertices[2]-vertices[0]).length_squared>1e-12:
   triangles.append({'positions':[[v.x,v.z,-v.y] for v in vertices],'transform':[list(row) for row in native],'normal':[list(row) for row in normal]})
 evaluated.to_mesh_clear()
 obj.data=obj.data.copy();obj.data.transform(transform@inverse@obj.matrix_world)
 obj.matrix_world=tower.matrix_world.copy()
 changes.append({'name':obj.name,'local_matrix':[n for row in (inverse@obj.matrix_world) for n in row]})
bpy.context.view_layer.update()
clearances=[]
for suffix in ['', '.001']:
 dish=bpy.data.objects['Parabolic Relay Dish'+suffix];rim=bpy.data.objects['Dish Gold Rim'+suffix]
 maximum=max(abs((inverse@o.matrix_world@Vector(v)).x) for o in [dish,rim] for v in o.bound_box)
 # Dishes face front/back; crystal poles remain on the left/right.
 clearances.append(4.467-maximum)
assert min(clearances)>2.7
owned=set(tower.children_recursive)
corners=[inverse@o.matrix_world@Vector(v) for o in owned if o.type in {'MESH','CURVE'} for v in o.bound_box]
bounds=[[min(v[i] for v in corners) for i in range(3)],[max(v[i] for v in corners) for i in range(3)]]
features={'camera':[],'steps':[],'solids':[],'water':[]}
for instance in bpy.context.evaluated_depsgraph_get().object_instances:
 if instance.object.original in owned:collect(instance,SimpleNamespace(matrix=tower.matrix_world,label='Comm Tower'),features)
camera=finish(features)['camera']
target=root/'Authoring/Catalog-r004.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
(check/'replacement.json').write_text(json.dumps({'triangles':triangles,'changes':changes,'clearance':clearances,'bounds':bounds,'camera':camera,'source_sha256':hashlib.sha256(target.read_bytes()).hexdigest()}))
keep=set(tower.children_recursive)|{tower}
for obj in bpy.context.scene.objects:obj.hide_render=obj not in keep
scene=bpy.context.scene;world=bpy.data.worlds.new('Tower Review');world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.28,.34,.4,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65;scene.world=world
ld=bpy.data.lights.new('Tower Review Sun','SUN');ld.energy=2
light=bpy.data.objects.new(ld.name,ld);scene.collection.objects.link(light);light.rotation_euler=(.5,-.4,-.7)
cd=bpy.data.cameras.new('Tower Dishes Review');camera=bpy.data.objects.new(cd.name,cd);scene.collection.objects.link(camera)
aim=tower.matrix_world@Vector((0,0,18));camera.location=tower.matrix_world@Vector((17,-31,31))
camera.rotation_euler=(aim-camera.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=72;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1280;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.filepath=str(check/'tower-dishes.png');bpy.ops.render.render(write_still=True)
print('PASS both dishes clear crystal poles by',clearances,'with attached extended brackets')
`,check+'/alternating-build.log');
const report=JSON.parse(fs.readFileSync(check+'/replacement.json'));
const key=tri=>tri.map(v=>v.map(n=>Math.round(n*10000)||0).join(',')).sort().join(';');
const targets=new Map(report.triangles.map(t=>[key(t.positions),t])),matched=new Set();
const catalog=JSON.parse(fs.readFileSync(town+'/Authoring/catalog.json'));
const tower=catalog.templates.find(t=>t.label==='Comm Tower'),changes=[];
for(const chunk of new Set(tower.parts.map(p=>p[0]))){
 const item=catalog.chunks[chunk],file=town+'/Authoring/'+item.file,raw=fs.readFileSync(file),jl=raw.readUInt32LE(12),doc=JSON.parse(raw.subarray(20,20+jl)),bin=raw.subarray(28+jl);
 let count=0;
 for(const [,part] of tower.parts.filter(p=>p[0]===chunk)){
  const prim=doc.meshes[part].primitives[0],pos=doc.accessors[prim.attributes.POSITION],pv=doc.bufferViews[pos.bufferView],idx=doc.accessors[prim.indices],iv=doc.bufferViews[idx.bufferView];
  const io=(iv.byteOffset||0)+(idx.byteOffset||0),po=(pv.byteOffset||0)+(pos.byteOffset||0),stride=pv.byteStride||12;
  const transformed=new Map(),untouched=new Set();
  for(let i=0;i<idx.count;i+=3){
   const ids=[0,1,2].map(j=>bin.readUInt32LE(io+4*(i+j))),tri=ids.map(id=>[0,1,2].map(j=>bin.readFloatLE(po+id*stride+j*4)));
   const k=key(tri),t=targets.get(k);
   if(t){count++;matched.add(k);for(const id of ids)transformed.set(id,t);}else for(const id of ids)untouched.add(id);
  }
  for(const [id,t] of transformed){
   if(untouched.has(id))throw Error('Moved vertices shared with stationary geometry');
   const point=[0,1,2].map(j=>bin.readFloatLE(po+id*stride+j*4));
   for(let row=0;row<3;row++)bin.writeFloatLE(t.transform[row][3]+point.reduce((sum,n,j)=>sum+n*t.transform[row][j],0),po+id*stride+row*4);
   for(const semantic of ['NORMAL','TANGENT'])if(prim.attributes[semantic]!==undefined){
    const a=doc.accessors[prim.attributes[semantic]],v=doc.bufferViews[a.bufferView],at=(v.byteOffset||0)+(a.byteOffset||0)+id*(v.byteStride||(semantic==='TANGENT'?16:12));
    const direction=[0,1,2].map(j=>bin.readFloatLE(at+j*4)),matrix=semantic==='NORMAL'?t.normal:t.transform;
    const moved=[0,1,2].map(row=>direction.reduce((sum,n,j)=>sum+n*matrix[row][j],0)),length=Math.hypot(...moved);
    moved.forEach((n,j)=>bin.writeFloatLE(n/length,at+j*4));
   }
  }
  if(transformed.size){
   pos.min=[Infinity,Infinity,Infinity];pos.max=[-Infinity,-Infinity,-Infinity];
   for(let id=0;id<pos.count;id++)for(let j=0;j<3;j++){
    const value=bin.readFloatLE(po+id*stride+j*4);pos.min[j]=Math.min(pos.min[j],value);pos.max[j]=Math.max(pos.max[j],value);
   }
  }
 }
 if(count){
  const json=Buffer.from(JSON.stringify(doc)),padded=Buffer.alloc(Math.ceil(json.length/4)*4,32);json.copy(padded);
  const header=Buffer.alloc(20);header.writeUInt32LE(0x46546c67);header.writeUInt32LE(2,4);header.writeUInt32LE(28+padded.length+bin.length,8);header.writeUInt32LE(padded.length,12);header.writeUInt32LE(0x4e4f534a,16);
  const bh=Buffer.alloc(8);bh.writeUInt32LE(bin.length);bh.writeUInt32LE(0x004e4942,4);
  const output=Buffer.concat([header,padded,bh,bin]);fs.writeFileSync(file,output);item.sha256=createHash('sha256').update(output).digest('hex');changes.push({file:item.file,movedTriangles:count});
 }
}
if(matched.size!==targets.size)throw Error(`Missing dish triangles ${targets.size-matched.size}`);
catalog.blend_source='Authoring/Catalog-r004.blend';catalog.source_sha256=report.source_sha256;
tower.bounds=report.bounds;tower.camera=report.camera;
fs.writeFileSync(town+'/Authoring/catalog.json',JSON.stringify(catalog,null,2)+'\n');
fs.writeFileSync(check+'/checks.json',JSON.stringify({rotationDegrees:90,clearance:report.clearance,parts:report.changes.map(o=>o.name),triangles:matched.size,changes,modelsAdded:0},null,2));
console.log('PASS native dishes moved with unchanged part/model counts.');
