import fs from 'node:fs';
import { runBlender } from './blender-background.mjs';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle/';
const output=root+'native-integration/Neris-Town-Royal-Castle-r001.blend';
if(fs.existsSync(output))throw Error('Use a fresh town revision.');
const document=JSON.parse(fs.readFileSync(root+'native-integration/accepted-town-document.json','utf8'));
const before={columns:document.columns,rows:document.rows,xs:document.xs,zs:document.zs,cells:document.cells};
document.items=document.items.filter(i=>i.template!==6);
const xs=[...before.xs],zs=[...before.zs];
while(xs[0]>-5100)xs.unshift(xs[0]-20);
while(zs.at(-1)<4010)zs.push(zs.at(-1)+20);
const shift=xs.indexOf(before.xs[0]);
const columns=xs.length-1,rows=zs.length-1,cells=[];
const inside=(x,z,a,b,c,d)=>x>=a&&x<b&&z>=c&&z<d;
for(let row=0;row<rows;row++)for(let col=0;col<columns;col++) {
  const x=(xs[col]+xs[col+1])/2,z=(zs[row]+zs[row+1])/2;
  const old=col-shift;
  let kind=row<before.rows&&old>=0&&old<before.columns?before.cells[row*before.columns+old]:1;
  if(kind===0||x<-2460)kind=1;
  if(inside(x,z,-5000,-2280,1000,4010))kind=1;
  // One moat around the new island, with a full land margin beyond it.
  if(inside(x,z,-4780,-2540,1100,3780))kind=2;
  // Paving around the moat and a connected town approach, at normal road height.
  if(inside(x,z,-4960,-4880,900,3980)||inside(x,z,-2460,-2280,900,3980)||
     inside(x,z,-4960,-2280,3880,3980)||inside(x,z,-4960,-2280,820,920)||
     inside(x,z,-3760,-3560,900,1100)||inside(x,z,-3660,-2300,820,1000))kind=3;
  cells.push(kind);
}
Object.assign(document,{name:'Neris Town',dirty:true,columns,rows,xs,zs,cells});
const documentPath=root+'native-integration/relocated-town-document-r001.json';
fs.writeFileSync(documentPath,JSON.stringify(document));
fs.mkdirSync(root+'checkpoints/Town-r001',{recursive:true});
await runBlender('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Blend/Neris Town.blend',String.raw`
import bpy,json,sys,math,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
target=Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle/native-integration/Neris-Town-Royal-Castle-r001.blend')
assert not target.exists()
s=bpy.context.scene
doc=json.loads(Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle/native-integration/relocated-town-document-r001.json').read_text())
old=[o for o in s.objects if o.get('town_template')==6]
assert len(old)==1
for anchor in old:
    for o in list(anchor.children_recursive):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.objects.remove(anchor,do_unlink=True)
surface=bpy.data.objects['Town Editable Surface'];bpy.data.objects.remove(surface,do_unlink=True)
sys.path.insert(0,'D:/SMILE 2.0/tools/Character3DViewer')
from town_blender_save import terrain
from town_document_codec import encode
terrain(doc)
snapshot=bpy.data.texts['Town Editor Document.json'];snapshot.clear();snapshot.write(json.dumps(doc))
catalog=json.loads(Path('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/catalog.json').read_text())
s['town_editor_document_sha256']=hashlib.sha256(encode(doc,catalog)).hexdigest()
s['town_editor_items']=len(doc['items'])
# Append owned architecture only; lighting/cameras remain the town's environment.
source='D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M06-r005.blend'
names=['NC.Site','NC.Fortifications','NC.Gatehouse','NC.Bridge','NC.Palace','NC.Courtyard']
with bpy.data.libraries.load(source,link=False) as (available,loaded):
    assert all(name in available.collections for name in names)
    loaded.collections=names
castle=bpy.data.collections.new('Neris Royal Castle — Revised M06');s.collection.children.link(castle)
for c in loaded.collections:castle.children.link(c)
# Preserve only the top island floor in this assembled town. Original source stays intact.
site=bpy.data.collections['NC.Site'];vertices=[];faces=[]
material=bpy.data.materials['NC.MAT.IvoryStone']
for o in list(site.objects):
    if o.type=='MESH' and '.Foundation.' in o.name:
        for p in o.data.polygons:
            points=[o.matrix_world@o.data.vertices[i].co for i in p.vertices]
            if all(abs(v.z)<.0001 for v in points):
                n=len(vertices);vertices.extend(tuple(v)for v in points);faces.append(tuple(range(n,n+len(points))))
    bpy.data.objects.remove(o,do_unlink=True)
mesh=bpy.data.meshes.new('NC.Site.DeckFloor.Mesh');mesh.from_pydata(vertices,[],faces);mesh.materials.append(material)
floor=bpy.data.objects.new('NC.Site.DeckFloor',mesh);site.objects.link(floor);floor['owner']='site'
anchor=bpy.data.objects.new('Neris New Royal Castle Assembly',None);castle.objects.link(anchor)
anchor.location=(-366,244,.212);anchor.scale=(1.7,1.7,1.7)
anchor['asset_source']=source;anchor['native_origin']=[-3660,23.12,2440];anchor['native_scale']=17
for c in loaded.collections:
    for o in c.objects:
        if o.parent is None:o.parent=anchor
s['castle_relocation']='M06-r005 at former Tripo precinct; Old Castle source and palette preserved'
record=bpy.data.texts.new('New Royal Castle Placement.json');record.write(json.dumps({'source':source,'origin':[-3660,23.12,2440],'scale':17,'old_castle_instance_removed':39,'moat':[-4780,-2540,1100,3780],'bridge_landing':[-3660,1012],'native_sync_pending':True},indent=2))
bpy.context.view_layer.update()
# Keep the two versioned scenes inspectable with a useful overview camera.
d=bpy.data.cameras.new('Town Royal Castle Overview');camera=bpy.data.objects.new(d.name,d);s.collection.objects.link(camera)
camera.location=(-870,-940,1040);target_point=Vector((-117.5,22.5,20))
camera.rotation_euler=(target_point-camera.location).to_track_quat('-Z','Y').to_euler()
d.type='ORTHO';d.ortho_scale=1140;d.clip_end=5000;s.camera=camera
s.render.resolution_x=1600;s.render.resolution_y=1100;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/Town-r001/town-relocated.png'
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            v=area.spaces.active;v.clip_end=5000;v.region_3d.view_location=target_point
            v.region_3d.view_rotation=camera.rotation_euler.to_quaternion();v.region_3d.view_distance=980
            v.region_3d.view_perspective='ORTHO';v.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
bpy.ops.render.render(write_still=True)
print('NC_RESULT '+json.dumps({'file':str(target),'castle_origin':list(anchor.location),'remaining_tripo_placements':len([o for o in s.objects if o.get('town_template')==6]),'grid':[doc['columns'],doc['rows']]}))
`,root+'checkpoints/Town-r001/town-build.log');
