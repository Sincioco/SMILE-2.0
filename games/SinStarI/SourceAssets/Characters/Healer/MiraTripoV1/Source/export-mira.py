"""Validate and export the new Tripo Mira into its canonical package."""
import bpy
import bmesh
import hashlib
import json
import math
import shutil
from pathlib import Path
from mathutils import Quaternion, Vector

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-tripo-grounded-work.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['Mira.Rig']
body = bpy.data.objects['Mira.Body']
staff = bpy.data.objects['Mira.Staff']
# Grounding baked the cape to quaternions; discard the superseded Euler draft.
for action in bpy.data.actions:
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in list(bag.fcurves):
                    if curve.data_path == 'pose.bones["MiraCape"].rotation_euler':
                        bag.fcurves.remove(curve)
topology = []
for obj in [body, staff]:
    corrected_mesh_records = obj.data.validate(verbose=True, clean_customdata=False)
    for uv in list(obj.data.uv_layers):
        if uv.name != 'UVMap':
            obj.data.uv_layers.remove(uv)
    obj.data.uv_layers.active_index = 0
    obj.data.uv_layers.active.active_render = True
    uv_data = obj.data.uv_layers.active.data
    repaired_uv_faces = 0
    for polygon in obj.data.polygons:
        loops = list(polygon.loop_indices)
        a,b,c = [uv_data[index].uv.copy() for index in loops]
        area = abs((b-a).x*(c-a).y - (b-a).y*(c-a).x)
        if area < 1e-13:
            center = (a+b+c)/3
            for index,offset in zip(loops,((-.00012,-.00012),(.00012,-.00012),(0,.00012))):
                uv_data[index].uv = center + Vector(offset)
            repaired_uv_faces += 1
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000001)
    row = {'part': obj.name, 'triangles': sum(len(p.vertices) - 2 for p in obj.data.polygons),
           'correctedMeshRecords': corrected_mesh_records,
           'boundaryEdges': sum(edge.is_boundary for edge in bm.edges),
           'nonManifoldEdges': sum(not edge.is_manifold for edge in bm.edges),
           # Use the meter-scale threshold established by this revision's reducer.
           'degenerateAreaThresholdSquareMeters': 1e-12,
           'degenerateTriangles': sum(face.calc_area() < 1e-12 for face in bm.faces),
           'microTriangles': sum(face.calc_area() < 1e-10 for face in bm.faces)}
    bm.free()
    assert row['boundaryEdges'] == row['nonManifoldEdges'] == row['degenerateTriangles'] == 0, row
    obj.data.calc_tangents(uvmap='UVMap')
    row['invalidTangents'] = sum(loop.tangent.length < .5 for loop in obj.data.loops)
    row['repairedCollapsedUvFaces'] = repaired_uv_faces
    row['repairedImportedCornerNormals'] = row['invalidTangents']
    if row['invalidTangents']:
        normals = [loop.normal.copy() for loop in obj.data.loops]
        for polygon in obj.data.polygons:
            for index in polygon.loop_indices:
                if obj.data.loops[index].tangent.length < .5:
                    normals[index] = polygon.normal.copy()
        obj.data.normals_split_custom_set(normals)
        obj.data.calc_tangents(uvmap='UVMap')
        row['invalidTangents'] = sum(loop.tangent.length < .5 for loop in obj.data.loops)
    assert row['invalidTangents'] == 0, row
    topology.append(row)
assert sum(row['triangles'] for row in topology) <= 20000
assert max(len(vertex.groups) for vertex in body.data.vertices) <= 4
assert len(bpy.data.actions) == 9
assert all(not bone.constraints for bone in rig.pose.bones)


def bounds():
    bpy.context.view_layer.update()
    graph = bpy.context.evaluated_depsgraph_get()
    points = [body.matrix_world @ vertex.co for vertex in body.evaluated_get(graph).data.vertices]
    equipment = [staff.matrix_world @ vertex.co for vertex in staff.evaluated_get(graph).data.vertices]
    return {'minimumY': min(point.z for point in points), 'maximumY': max(point.z for point in points),
            'staffMinimumY': min(point.z for point in equipment)}


rig.data.pose_position = 'REST'
bind = bounds()
assert bind['staffMinimumY'] >= bind['minimumY'], bind
rig.data.pose_position = 'POSE'
contacts = []
for action in bpy.data.actions:
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    start, end = map(int, action.frame_range)
    samples = []
    for frame in range(start, end + 1):
        scene.frame_set(frame)
        sample = {'frame': frame - start, **bounds()}
        assert sample['minimumY'] >= -.0001, (action.name, sample)
        assert sample['staffMinimumY'] >= -.0001, (action.name, sample)
        samples.append(sample)
    contacts.append({'clip': action.name, 'framesChecked': len(samples),
                     'minimumBodyY': min(row['minimumY'] for row in samples),
                     'minimumStaffY': min(row['staffMinimumY'] for row in samples),
                     'samples': [samples[0], samples[(len(samples) - 1) // 2], samples[-1]]})
idle = next(row for row in contacts if row['clip'] == 'Idle')
assert abs(bind['minimumY'] - idle['samples'][0]['minimumY']) < .000001
rig.animation_data.action = bpy.data.actions['Idle']
rig.animation_data.action_slot = bpy.data.actions['Idle'].slots[0]
scene.frame_set(1)
scene.frame_start = 1
scene.frame_end = 341
scene.name = 'Mira — Tripo Healer'
bpy.ops.object.select_all(action='DESELECT')
for obj in [rig, body, staff]:
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
for window in bpy.context.window_manager.windows:
    for area in window.screen.areas:
        if area.type == 'VIEW_3D':
            space = area.spaces.active
            space.shading.type = 'MATERIAL'
            space.overlay.show_overlays = False
            space.region_3d.view_rotation = Quaternion((1, 0, 0), math.pi / 2)
            space.region_3d.view_location = Vector((0, 0, .55))
            space.region_3d.view_distance = 1.45
            space.region_3d.view_perspective = 'ORTHO'
for action in bpy.data.actions:
    action.use_fake_user = True
bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=False, do_recursive=True)
bpy.ops.file.pack_all()
blend = PACKAGE / 'Blender/mira-rigged-animation-checkpoint.blend'
model = PACKAGE / 'mira-animation-checkpoint.glb'
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
bpy.ops.export_scene.gltf(filepath=str(model), export_format='GLB', use_selection=True,
                         export_animations=True, export_animation_mode='ACTIONS',
                         export_skins=True, export_influence_nb=4, export_all_influences=False,
                         export_tangents=True, export_yup=True)
for name in ['face-repair.json', 'surface-repair.json', 'staff-reduction.json', 'animation-assembly.json', 'equipment-fit.json', 'grounding-repair.json']:
    shutil.copy2(STAGE / name, PACKAGE / 'Source' / name)
report = {'modelSha256': hashlib.sha256(model.read_bytes()).hexdigest(),
          'units': 'meters; Blender Z reported as glTF/SM3D Y', 'bindBody': bind,
          'clips': contacts, 'topology': topology, 'rigBones': len(rig.data.bones),
          'bodyTextureSize': [4096, 4096], 'staffTextureSize': [2048, 2048], 'bodyMaximumInfluences': max(len(v.groups) for v in body.data.vertices),
          'acceptance': 'Tripo Mira export; native validation must be recorded in README.md'}
(PACKAGE / 'Source/mira-grounding-checkpoint.json').write_text(json.dumps(report, indent=2))
print('MIRA_EXPORT_READY', json.dumps(report), flush=True)
