"""Validate and export the selected Mira1 repair into its canonical package."""
import bpy
import bmesh
import hashlib
import json
import math
import shutil
from pathlib import Path
from mathutils import Quaternion, Vector

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\Mira1Selected')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira1-repaired.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['Mira.Rig']
body = bpy.data.objects['Mira.SkinnedBody']
staff = bpy.data.objects['Mira.Staff']
topology = []
for obj in [body, staff]:
    for uv in list(obj.data.uv_layers):
        if uv.name != 'UVMap':
            obj.data.uv_layers.remove(uv)
    obj.data.uv_layers.active_index = 0
    obj.data.uv_layers.active.active_render = True
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000001)
    row = {'part': obj.name, 'triangles': sum(len(p.vertices) - 2 for p in obj.data.polygons),
           'boundaryEdges': sum(edge.is_boundary for edge in bm.edges),
           'nonManifoldEdges': sum(not edge.is_manifold for edge in bm.edges),
           'degenerateTriangles': sum(face.calc_area() < 1e-10 for face in bm.faces)}
    bm.free()
    assert row['boundaryEdges'] == row['nonManifoldEdges'] == row['degenerateTriangles'] == 0, row
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
scene.frame_end = 121
scene.name = 'Mira1 — Selected Healer'
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
            space.region_3d.view_location = Vector((0, 0, 1.03))
            space.region_3d.view_distance = 2.45
            space.region_3d.view_perspective = 'ORTHO'
for action in bpy.data.actions:
    action.use_fake_user = True
bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=False, do_recursive=True)
bpy.ops.file.pack_all()
blend = PACKAGE / 'Blender/mira1-rigged-animation-checkpoint.blend'
model = PACKAGE / 'mira1-animation-checkpoint.glb'
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
bpy.ops.export_scene.gltf(filepath=str(model), export_format='GLB', use_selection=True,
                         export_animations=True, export_animation_mode='ACTIONS',
                         export_skins=True, export_influence_nb=4, export_all_influences=False,
                         export_tangents=True, export_yup=True)
for name in ['mira1-texture-repair.json', 'mira1-animation-repair.json']:
    shutil.copy2(STAGE / name, PACKAGE / 'Source' / name)
report = {'modelSha256': hashlib.sha256(model.read_bytes()).hexdigest(),
          'units': 'meters; Blender Z reported as glTF/SM3D Y', 'bindBody': bind,
          'clips': contacts, 'topology': topology, 'rigBones': len(rig.data.bones),
          'textureSize': [4096, 4096], 'bodyMaximumInfluences': max(len(v.groups) for v in body.data.vertices),
          'acceptance': 'Selected Mira1 repair; native validation recorded in README.md'}
(PACKAGE / 'Source/mira1-grounding-checkpoint.json').write_text(json.dumps(report, indent=2))
print('MIRA1_EXPORT_READY', json.dumps(report), flush=True)
