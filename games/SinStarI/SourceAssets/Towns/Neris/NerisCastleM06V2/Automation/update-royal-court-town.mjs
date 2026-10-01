// Build a new town revision from the last normally saved Viewer document.
import fs from 'node:fs';
import path from 'node:path';
import {execFileSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const saveRoot = execFileSync('pwsh', ['-NoProfile', '-File',
    fileURLToPath(new URL('../../../../../../../scripts/get-smile-data-root.ps1', import.meta.url))],
    {encoding:'utf8'}).trim();
import {createHash} from 'node:crypto';
import {runBlender} from './blender-background.mjs';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const hash=value=>createHash('sha256').update(value).digest('hex');
const data=path.join(saveRoot,hash('smile.tools.character3d-viewer'),'Data');
const live=fs.readFileSync(path.join(data,hash('TownEditor.PermanentNeris')+'.bin'));
const check=root+'/checkpoints/Town-r003';
fs.mkdirSync(check,{recursive:true});
fs.writeFileSync(check+'/before-road.town',live,{flag:'wx'});
fs.writeFileSync(check+'/before-road.sha256',hash(live),{flag:'wx'});
await runBlender('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/Catalog-r002.blend',String.raw`
import bpy,sys,json,hashlib,math
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,'D:/SMILE 2.0/tools/Character3DViewer')
from town_blender_save import populate_items,terrain,lighting
from town_document_codec import decode,encode,unwrap
root=Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle')
check=root/'checkpoints/Town-r003'
target=root/'native-integration/Neris-Town-Royal-Castle-r003.blend'
assert not target.exists()
catalog=json.loads(Path('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/catalog.json').read_text())
doc=decode(unwrap((check/'before-road.town').read_bytes()),catalog)
doc.pop('payload')
previous=json.loads(json.dumps(doc))
changed=0
for row in range(doc['rows']):
    z=(doc['zs'][row]+doc['zs'][row+1])*.5
    for col in range(doc['columns']):
        x=(doc['xs'][col]+doc['xs'][col+1])*.5
        if -4960<=x<-2280 and 820<=z<1012:
            index=row*doc['columns']+col
            if doc['cells'][index]!=3:
                changed+=1;doc['cells'][index]=3
assert changed>0 and 1012 in doc['zs']
assert doc['items']==previous['items'] and doc['sun']==previous['sun']
assert not any(i['template']==6 for i in doc['items'])
doc['dirty']=False
assert decode(encode(doc,catalog),catalog)['cells']==doc['cells']
(root/'native-integration/relocated-town-document-r003.json').write_text(json.dumps(doc))
populate_items(doc,catalog);terrain(doc);lighting(doc)
s=bpy.context.scene
s['town_editor_name']=doc['name'];s['town_editor_items']=len(doc['items'])
s['town_editor_document_sha256']=hashlib.sha256(encode(doc,catalog)).hexdigest()
snapshot=bpy.data.texts.get('Town Editor Document.json') or bpy.data.texts.new('Town Editor Document.json')
snapshot.clear();snapshot.write(json.dumps(doc))
source=str(root/'source/neris-castle-M06-r006.blend')
names=['NC.Site','NC.Fortifications','NC.Gatehouse','NC.Bridge','NC.Palace','NC.Courtyard']
with bpy.data.libraries.load(source,link=False) as (available,loaded):
    assert all(n in available.collections for n in names)
    loaded.collections=names
castle=bpy.data.collections.new('Royal Court — M06-r006');s.collection.children.link(castle)
for collection in loaded.collections:castle.children.link(collection)
site=bpy.data.collections['NC.Site'];vertices=[];faces=[]
for obj in list(site.objects):
    if obj.type=='MESH' and '.Foundation.' in obj.name:
        for polygon in obj.data.polygons:
            points=[obj.matrix_world@obj.data.vertices[i].co for i in polygon.vertices]
            if all(abs(v.z)<.0001 for v in points):
                start=len(vertices);vertices.extend(tuple(v) for v in points)
                faces.append(tuple(range(start,start+len(points))))
    bpy.data.objects.remove(obj,do_unlink=True)
mesh=bpy.data.meshes.new('NC.Site.DeckFloor.Mesh');mesh.from_pydata(vertices,[],faces)
mesh.materials.append(bpy.data.materials['NC.MAT.IvoryStone'])
floor=bpy.data.objects.new('NC.Site.DeckFloor',mesh);site.objects.link(floor);floor['owner']='site'
anchor=bpy.data.objects.new('Royal Court Assembly',None);castle.objects.link(anchor)
anchor.location=(-366,244,.212);anchor.scale=(1.7,1.7,1.7)
anchor['asset_source']=source;anchor['native_origin']=[-3660,23.12,2440];anchor['native_scale']=17
for collection in loaded.collections:
    for obj in collection.objects:
        if obj.parent is None:obj.parent=anchor
s['castle_relocation']='Royal Court at former Tripo precinct; M06-r006 flush gate facade'
record=bpy.data.texts.new('Royal Court Placement.json')
record.write(json.dumps({'source':source,'name':'Royal Court','origin':[-3660,23.12,2440],
    'scale':17,'moat':[-4780,-2540,1012,3780],'bridge_landing':[-3660,1012],
    'front_road':[-4960,-2280,820,1012]},indent=2))
bpy.context.view_layer.update()
assert not any(o.get('town_member','').startswith('Sweeping Royal Garden Stair') for o in s.objects)
assert not any(o.get('town_template')==6 for o in s.objects)
camera_data=bpy.data.cameras.new('Royal Court Town Overview')
camera=bpy.data.objects.new(camera_data.name,camera_data);s.collection.objects.link(camera)
camera.location=(-870,-940,1040);aim=Vector((-117.5,22.5,20))
camera.rotation_euler=(aim-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.type='ORTHO';camera_data.ortho_scale=1140;camera_data.clip_end=5000;s.camera=camera
s.render.engine='CYCLES';s.cycles.samples=16
s.render.resolution_x=1280;s.render.resolution_y=900;s.render.resolution_percentage=100
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            view=area.spaces.active;view.clip_end=5000;view.region_3d.view_location=aim
            view.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
            view.region_3d.view_distance=980;view.region_3d.view_perspective='ORTHO'
            view.overlay.show_overlays=False;view.shading.type='MATERIAL'
            view.shading.use_scene_lights=False;view.shading.use_scene_world=False
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
# Daylight inspection render; preserve the saved user's lighting document.
day=dict(doc);day['sun']=[255,237,214,260,38,207,54,1,65];lighting(day)
s.render.filepath=str(check/'town-day.png');bpy.ops.render.render(write_still=True)
(check/'town-checks.json').write_text(json.dumps({'changed_road_cells':changed,
    'preserved_items':len(doc['items']),'preserved_sun':doc['sun'],'tripo_instances':0,
    'bridge_landing_z':1012,'royal_side_stairs':0,'file':str(target)},indent=2))
print('PASS Royal Court town revision: road cells',changed,'items',len(doc['items']),'sun',doc['sun'])
`,check+'/town-build.log');
