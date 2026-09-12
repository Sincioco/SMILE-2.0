"""Export the independent Mira2 comparison and record actual body contacts."""
import bpy, bmesh, json, hashlib
from pathlib import Path
from mathutils import Vector
package=Path(r'D:\SMILE 2.0\games\SinStarI\SourceAssets\Characters\Healer\MiraV2')
rig=bpy.data.objects['Mira2.Rig']; body=bpy.data.objects['Mira2.SkinnedBody']; staff=bpy.data.objects['Mira2.Staff']; scene=bpy.context.scene
def body_bounds():
    bpy.context.view_layer.update()
    points=[body.matrix_world @ v.co for v in body.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]
    return {'minimumY':min(p.z for p in points),'maximumY':max(p.z for p in points)}
rig.data.pose_position='REST'; bind=body_bounds(); rig.data.pose_position='POSE'
contacts=[]
for action in bpy.data.actions:
    rig.animation_data.action=action; rig.animation_data.action_slot=action.slots[0]
    start,end=map(int,action.frame_range); samples=[]
    for frame in sorted(set((start,(start+end)//2,end))):
        scene.frame_set(frame); samples.append({'frame':frame-start,**body_bounds()})
    contacts.append({'clip':action.name,'samples':samples})
topology=[]
for obj in (body,staff):
    mesh=bmesh.new(); mesh.from_mesh(obj.data)
    topology.append({'part':obj.name,'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'boundaryEdges':sum(e.is_boundary for e in mesh.edges),'nonManifoldEdges':sum(not e.is_manifold for e in mesh.edges)})
    mesh.free()
rig.animation_data.action=bpy.data.actions['Idle']; rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0]; scene.frame_set(1)
scene.frame_start=1; scene.frame_end=121
for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_location=Vector((0,0,1.02));area.spaces.active.region_3d.view_distance=3.2
bpy.ops.object.select_all(action='DESELECT')
for obj in (rig,body,staff): obj.select_set(True)
bpy.context.view_layer.objects.active=rig
for action in bpy.data.actions: action.use_fake_user=True
bpy.data.orphans_purge(do_local_ids=True,do_linked_ids=False,do_recursive=True)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(package/'Blender'/'mira2-rigged-animation-checkpoint.blend'))
model=package/'mira2-animation-checkpoint.glb'
bpy.ops.export_scene.gltf(filepath=str(model),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS',export_skins=True,export_influence_nb=4,export_all_influences=False,export_tangents=True,export_yup=True)
report={'modelSha256':hashlib.sha256(model.read_bytes()).hexdigest(),'units':'meters; Blender Z reported as SM3D/glTF Y','bindBody':bind,'clips':contacts,'topology':topology,'acceptance':'Comparison checkpoint only; dynamic floor/cloth contact and Party/VFX validation remain open.'}
(package/'Source'/'mira2-grounding-checkpoint.json').write_text(json.dumps(report,indent=2))
result=report
