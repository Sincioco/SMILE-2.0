import bpy,bmesh,json,hashlib,math
from pathlib import Path
from mathutils import Vector,Quaternion
root=Path(r'D:\AI\Mira3D\Mira3')
package=Path(r'D:\SMILE 2.0\games\SinStarI\SourceAssets\Characters\Healer\MiraV3')
for folder in ['Blender','Source','Animations','Reference','Previews']:(package/folder).mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-grounded-rigged.blend'))
rig=bpy.data.objects['Mira3.Rig'];body=bpy.data.objects['Mira3.SkinnedBody'];staff=bpy.data.objects['Mira3.Staff'];scene=bpy.context.scene
repair=[]
for obj in [body,staff]:
    moved={}
    for iteration in range(20):
        bad=0
        for face in obj.data.polygons:
            ids=list(face.vertices);points=[obj.data.vertices[i].co.copy() for i in ids]
            cross=(points[1]-points[0]).cross(points[2]-points[0])
            if cross.length>1.015e-6:continue
            bad+=1
            edge=max([(0,1,2),(1,2,0),(2,0,1)],key=lambda e:(points[e[1]]-points[e[0]]).length)
            a,b,c=[points[i] for i in edge];axis=(b-a).normalized();foot=a+axis*(c-a).dot(axis);altitude=c-foot
            if altitude.length<1e-12:raise RuntimeError('Collapsed source triangle requires separate topology repair')
            target=foot+altitude.normalized()*(1.08e-6/(b-a).length);vertex=ids[edge[2]]
            moved.setdefault(vertex,obj.data.vertices[vertex].co.copy());obj.data.vertices[vertex].co=target
        if not bad:break
    else:raise RuntimeError(f'Triangle repair did not converge on {obj.name}')
    obj.data.update();obj.data.calc_tangents(uvmap=obj.data.uv_layers.active.name)
    badloops={loop.index for loop in obj.data.loops if loop.tangent.length<.5}
    repaired_faces=[]
    if badloops:
        for face in obj.data.polygons:
            if any(loop in badloops for loop in face.loop_indices):
                face.use_smooth=False;obj.data.uv_layers.active.data[face.loop_indices[-1]].uv.x+=.0000004;repaired_faces.append(face.index)
        obj.data.update();obj.data.calc_tangents(uvmap=obj.data.uv_layers.active.name)
    zeros=sum(loop.tangent.length<.5 for loop in obj.data.loops)
    if zeros:raise RuntimeError(f'{obj.name}: {zeros} invalid tangents')
    repair.append({'part':obj.name,'movedVertices':len(moved),'maximumVertexShiftMeters':max([(obj.data.vertices[i].co-p).length for i,p in moved.items()] or [0]),'flatTangentFaces':repaired_faces,'zeroTangents':zeros})

def bounds():
    bpy.context.view_layer.update();points=[body.matrix_world@v.co for v in body.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]
    staff_points=[staff.matrix_world@v.co for v in staff.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]
    return {'minimumY':min(p.z for p in points),'maximumY':max(p.z for p in points),'staffMinimumY':min(p.z for p in staff_points)}
rig.data.pose_position='REST';bind=bounds();rig.data.pose_position='POSE';contacts=[]
for action in bpy.data.actions:
    rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0];start,end=map(int,action.frame_range);samples=[]
    for frame in sorted({start,(start+end)//2,end}):scene.frame_set(frame);samples.append({'frame':frame-start,**bounds()})
    contacts.append({'clip':action.name,'samples':samples})
topology=[]
for obj in [body,staff]:
    bm=bmesh.new();bm.from_mesh(obj.data);row={'part':obj.name,'triangles':len(bm.faces),'boundaryEdges':sum(e.is_boundary for e in bm.edges),'nonManifoldEdges':sum(not e.is_manifold for e in bm.edges)};bm.free()
    if row['boundaryEdges'] or row['nonManifoldEdges']:raise RuntimeError('Exported source surface must be closed')
    topology.append(row)
rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1);scene.frame_start=1;scene.frame_end=121
bpy.ops.object.select_all(action='DESELECT')
for obj in [rig,body,staff]:obj.select_set(True)
bpy.context.view_layer.objects.active=rig
for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.shading.type='MATERIAL';space.overlay.show_overlays=False;space.region_3d.view_rotation=Quaternion((1,0,0),math.pi/2);space.region_3d.view_location=Vector((0,0,1.03));space.region_3d.view_distance=2.45;space.region_3d.view_perspective='ORTHO'
for action in bpy.data.actions:action.use_fake_user=True
bpy.data.orphans_purge(do_local_ids=True,do_linked_ids=False,do_recursive=True);bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(package/'Blender'/'mira3-rigged-animation-checkpoint.blend'))
model=package/'mira3-animation-checkpoint.glb'
bpy.ops.export_scene.gltf(filepath=str(model),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS',export_skins=True,export_influence_nb=4,export_all_influences=False,export_tangents=True,export_yup=True)
report={'modelSha256':hashlib.sha256(model.read_bytes()).hexdigest(),'units':'meters; Blender Z reported as glTF/SM3D Y','bindBody':bind,'clips':contacts,'topology':topology,'exportRepair':repair,'acceptance':'Comparison checkpoint; native checks are recorded in README.md.'}
(package/'Source'/'mira3-grounding-checkpoint.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('EXPORT_READY',json.dumps(report),flush=True)
