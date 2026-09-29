"""Rebuild all three review scenes from their versioned documents and current landmarks."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[7]
PACKAGE=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/Character3DViewer'))
from town_document_codec import decode,unwrap
import town_blender_save as town
from town_blender_landmarks import populate
catalog=json.loads((town.TOWN/'Authoring/catalog.json').read_text())
for filename in ('Neris-Town-r009','Neris-Spaceport-r002','Horizon-Airport-r009'):
 document=decode(unwrap((PACKAGE/'Town/r009'/ (filename+'.town')).read_bytes()),catalog)
 bpy.ops.wm.open_mainfile(filepath=str(town.TOWN/catalog['blend_source']))
 town.populate_items(document,catalog);town.terrain(document);town.lighting(document);counts=populate(document)
 scene=bpy.context.scene
 scene['town_editor_name']=document['name'];scene['town_editor_items']=len(document['items'])
 snapshot=bpy.data.texts.new('Town Editor Document.json');snapshot.write(json.dumps({k:v for k,v in document.items() if k!='payload'}))
 camera_data=bpy.data.cameras.new('Linked Town Review');camera=bpy.data.objects.new('Linked Town Review',camera_data);scene.collection.objects.link(camera)
 cx=(document['xs'][0]+document['xs'][-1])/20;cy=(document['zs'][0]+document['zs'][-1])/20
 camera.location=(cx,cy-1200,1500);camera.rotation_euler=(Vector((cx,cy,0))-camera.location).to_track_quat('-Z','Y').to_euler()
 camera_data.clip_end=10000;camera_data.type='ORTHO';camera_data.ortho_scale=max((document['xs'][-1]-document['xs'][0])/10,(document['zs'][-1]-document['zs'][0])/10)*1.15
 scene.camera=camera;scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
 scene.cycles.samples=8;bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(PACKAGE/'Town/r009'/(filename+'.blend')),compress=True)
 scene.render.filepath=str(PACKAGE/'Town/r009'/(filename+'.png'));bpy.ops.render.render(write_still=True)
 assert len({o['town_identity'] for o in scene.objects if 'town_identity' in o})==len(document['items'])
 print('PASS LINKED TOWN',document['name'],len(document['items']),counts,flush=True)
