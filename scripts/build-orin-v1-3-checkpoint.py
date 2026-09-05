"""Build Orin's viewer candidate from his Tripo skin and retained Mixamo motions.

Run with Blender --background --python. Originals are never overwritten.
The body retains its supplied vertices, UVs and weights; only derived poses and
rigid equipment are authored here. No Arin wrist or equipment offsets are used.
"""
from pathlib import Path
import json
import math
import shutil
import bpy
import bmesh
import numpy as np
from mathutils import Matrix, Vector, Quaternion

REPO = Path(__file__).resolve().parents[1]
ROOT = REPO / 'games/SinStarI/SourceAssets/Characters/Tank/OrinV13'
ARIN = REPO / 'games/SinStarI/SourceAssets/Characters/Paladin/ArinV57'


def imported(path):
    before = set(bpy.data.objects)
    if path.suffix.lower() == '.fbx':
        bpy.ops.import_scene.fbx(filepath=str(path))
    else:
        bpy.ops.import_scene.gltf(filepath=str(path))
    return [o for o in bpy.data.objects if o not in before]


def assign(rig, action):
    rig.animation_data_create()
    rig.animation_data.action = action
    if action and action.slots:
        rig.animation_data.action_slot = action.slots[0]


def reset_pose(rig):
    for bone in rig.pose.bones:
        bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()


def rotation(rig, bone, posed=False):
    matrix = rig.pose.bones[bone].matrix if posed else rig.data.bones[bone].matrix_local
    return (rig.matrix_world @ matrix).to_quaternion()


def render_preview(rig, action, frame, path):
    assign(rig, action)
    bpy.context.scene.frame_set(frame)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
objects = imported(ROOT / 'orin-v1.3.original.glb')
rig = next(o for o in objects if o.type == 'ARMATURE')
rig.name = 'Orin'
taunt = rig.animation_data.action
taunt.name = 'Angry'
taunt.use_fake_user = True
assign(rig, None)
reset_pose(rig)
body = [o for o in objects if o.type == 'MESH' and o.name != 'Icosphere']
for o in objects:
    if o.name == 'Icosphere':
        bpy.data.objects.remove(o, do_unlink=True)
# The source contains zero-area triangles that strict runtime cooking rejects.
# Remove only those zero-area faces in the derivative; preserve all skin data.
for obj in body:
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(mesh, dist=1.0e-10, edges=list(mesh.edges))
    mesh.to_mesh(obj.data)
    mesh.free()
# Keep one body part and two rigid equipment parts, independently editable.
bpy.ops.object.select_all(action='DESELECT')
for o in body:
    o.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
body = bpy.context.object
body.name = '02_Body'
report = {'source': 'orin-v1.3.original.glb', 'bodyVertices': len(body.data.vertices),
          'bodyWeightPolicy': 'Preserved supplied Tripo skin weights', 'actions': []}

mapping = {'Hip': 'Hips', 'Waist': 'Spine', 'Spine01': 'Spine1',
           'Spine02': 'Spine2', 'NeckTwist01': 'Neck', 'Head': 'Head'}
for side, mxside in [('L', 'Left'), ('R', 'Right')]:
    for target, source in [('Clavicle', 'Shoulder'), ('Upperarm', 'Arm'),
                           ('Forearm', 'ForeArm'), ('Hand', 'Hand'),
                           ('Thigh', 'UpLeg'), ('Calf', 'Leg'),
                           ('Foot', 'Foot'), ('ToeBase', 'ToeBase')]:
        mapping[f'{side}_{target}'] = mxside + source
mapping = {t: 'mixamorig:' + s for t, s in mapping.items()}
actions = [taunt]
manifest = json.loads((ARIN / 'arin-v5.7-animation-set.json').read_text())
for entry in manifest['animations']:
    if entry['name'] == 'Idle':
        source_path = ROOT / 'Animations/orin-v1.3-mixamo-sword-and-shield-idle-with-skin.fbx'
        source_mapping = {name: name for name in mapping}
    else:
        source_path = ROOT / 'Animations' / entry['file'].replace('arin-v5.7-', 'orin-motion-')
        shutil.copy2(ARIN / entry['file'], source_path)
        source_mapping = mapping
    source_objects = imported(source_path)
    source = next(o for o in source_objects if o.type == 'ARMATURE')
    source_action = source.animation_data.action
    first, last = (int(round(v)) for v in source_action.frame_range)
    fps = bpy.context.scene.render.fps / bpy.context.scene.render.fps_base
    corrections = {}
    for target_name, source_name in source_mapping.items():
        target_q = rotation(rig, target_name)
        source_q = rotation(source, source_name)
        # Match neutral limb directions while preserving Orin's bone roll.
        # This accounts for his supplied A-pose versus Mixamo's T-pose.
        if any(part in target_name for part in ('Upperarm', 'Forearm', 'Hand')):
            delta = (target_q @ Vector((0, 1, 0))).rotation_difference(
                source_q @ Vector((0, 1, 0)))
            target_q = delta @ target_q
        corrections[target_name] = source_q.inverted() @ target_q
    source_height = (source.matrix_world @ source.data.bones[source_mapping['Hip']].head_local).z
    target_height = (rig.matrix_world @ rig.data.bones['Hip'].head_local).z
    unit_scale = target_height / max(0.001, source_height)
    action = bpy.data.actions.new(entry['name'])
    action.use_fake_user = True
    assign(rig, action)
    bpy.context.scene.frame_set(first)
    initial_hip = (source.matrix_world @ source.pose.bones[source_mapping['Hip']].matrix).translation.copy()
    for frame in range(first, last + 1):
        bpy.context.scene.frame_set(frame)
        reset_pose(rig)
        for target_bone in rig.pose.bones:
            name = target_bone.name
            if name not in source_mapping:
                continue
            source_name = source_mapping[name]
            desired_q = rotation(source, source_name, True) @ corrections[name]
            head = (rig.matrix_world @ target_bone.matrix).translation
            if name == 'Hip':
                current = (source.matrix_world @ source.pose.bones[source_name].matrix).translation
                head.z += (current.z - initial_hip.z) * unit_scale
            target_bone.matrix = rig.matrix_world.inverted() @ Matrix.LocRotScale(
                head, desired_q, Vector((1, 1, 1)))
            bpy.context.view_layer.update()
        output_frame = 1 + (frame - first) * 30 / fps
        for bone in rig.pose.bones:
            bone.rotation_mode = 'QUATERNION'
            bone.keyframe_insert('location', frame=output_frame, group=bone.name)
            bone.keyframe_insert('rotation_quaternion', frame=output_frame, group=bone.name)
            bone.keyframe_insert('scale', frame=output_frame, group=bone.name)
    report['actions'].append({'name': action.name, 'file': 'Animations/' + source_path.name,
                              'sourceFrames': [first, last], 'sourceFps': fps})
    actions.append(action)
    for o in source_objects:
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.actions.remove(source_action)
    print('ORIN_ACTION', action.name, flush=True)

# Hold a consistent guard during combat and locomotion. Sample Orin's own
# retargeted poses, rather than transferring any saved Arin correction values.
def hold_chain(names, reference_name, frame, target_names):
    reference = next(a for a in actions if a.name == reference_name)
    assign(rig, reference)
    bpy.context.scene.frame_set(frame)
    values = {}
    for name in names:
        b = rig.pose.bones[name]
        values[f'pose.bones["{name}"].location'] = tuple(b.location)
        values[f'pose.bones["{name}"].rotation_quaternion'] = tuple(b.rotation_quaternion)
    for action in actions:
        if action.name not in target_names:
            continue
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        if curve.data_path not in values:
                            continue
                        value = values[curve.data_path][curve.array_index]
                        for key in curve.keyframe_points:
                            key.co.y = value
                            key.handle_left.y = value
                            key.handle_right.y = value

combat_names = [a.name for a in actions if a.name != 'Angry']
hold_chain(['L_Clavicle', 'L_Upperarm', 'L_Forearm', 'L_Hand'],
           'Idle', 1, combat_names)
hold_chain(['R_Clavicle', 'R_Upperarm', 'R_Forearm', 'R_Hand'],
           'Idle', 1, ['Walk', 'Run', 'BlockImpact', 'Hit', 'Defend'])
hold_chain(['R_Hand'], 'Idle', 1, combat_names)

def aim_bone(name, target):
    bone = rig.pose.bones[name]
    head = (rig.matrix_world @ bone.matrix).translation
    current = (rig.matrix_world @ bone.matrix).to_quaternion()
    current_y = current @ Vector((0, 1, 0))
    desired = (target - head).normalized()
    result = current_y.rotation_difference(desired) @ current
    bone.matrix = rig.matrix_world.inverted() @ Matrix.LocRotScale(
        head, result, Vector((1, 1, 1)))
    bpy.context.view_layer.update()


def key_orin_arm(action, side, attack):
    assign(rig, action)
    first, last = (int(round(v)) for v in action.frame_range)
    duration = max(1, last - first)
    for frame in range(first, last + 1):
        bpy.context.scene.frame_set(frame)
        chest = (rig.matrix_world @ rig.pose.bones['Spine02'].matrix).translation
        amount = math.sin(math.pi * (frame - first) / duration) if attack else 0
        sign = 1 if side == 'L' else -1
        elbow = chest + Vector((sign * 0.22, -0.08 - 0.14 * amount, -0.10 + 0.14 * amount))
        hand = chest + Vector((sign * 0.30, -0.13 - 0.35 * amount, -0.23 + 0.25 * amount))
        aim_bone(side + '_Upperarm', elbow)
        aim_bone(side + '_Forearm', hand)
        hammer_direction = Vector((0, -amount, -1 + 0.55 * amount)).normalized()
        aim_bone(side + '_Hand', hand + hammer_direction)
        for name in (side + '_Upperarm', side + '_Forearm', side + '_Hand'):
            bone = rig.pose.bones[name]
            bone.rotation_mode = 'QUATERNION'
            bone.keyframe_insert('location', frame=frame, group=bone.name)
            bone.keyframe_insert('rotation_quaternion', frame=frame, group=bone.name)
            bone.keyframe_insert('scale', frame=frame, group=bone.name)


for action in actions:
    if action.name == 'Angry':
        continue
    key_orin_arm(action, 'L', False)
    key_orin_arm(action, 'R', action.name in ('SwordAttack', 'SwordAttack2'))

# GLB needs one translation track at the root and rotation tracks elsewhere.
# Remove identity-scale and redundant child-location curves before export.
for action in actions:
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in list(bag.fcurves):
                    if curve.data_path.endswith('.scale'):
                        bag.fcurves.remove(curve)
                    elif (curve.data_path.endswith('.location') and
                          'pose.bones["Hip"]' not in curve.data_path):
                        bag.fcurves.remove(curve)

# Equipment comes from Sin's named original, fitted once to Orin's own hand
# bind spaces. It cannot deform or inherit unrelated limb weights.
idle = next(a for a in actions if a.name == 'Idle')
assign(rig, idle)
bpy.context.scene.frame_set(1)
reference_hands = {name: (rig.matrix_world @ rig.pose.bones[name].matrix).copy()
                   for name in ('L_Hand', 'R_Hand')}
assign(rig, None)
reset_pose(rig)
gear_objects = imported(ROOT / 'orin-v1.0-equipment-source.glb')
gear_rig = next(o for o in gear_objects if o.type == 'ARMATURE')
assign(gear_rig, None)
reset_pose(gear_rig)
equipment = []
for source_name, bone_name, output_name in [('Shield', 'L_Hand', '00_Shield'),
                                           ('Weapon', 'R_Hand', '01_Weapon')]:
    obj = next(o for o in gear_objects if o.name == source_name)
    source_hand = gear_rig.matrix_world @ gear_rig.data.bones[bone_name].matrix_local
    target_hand = rig.matrix_world @ rig.data.bones[bone_name].matrix_local
    reference_hand = reference_hands[bone_name]
    target_anchor = reference_hand @ Vector((0, rig.data.bones[bone_name].length * 0.3, 0))
    source_points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    source_array = np.array([tuple(point) for point in source_points])
    if source_name == 'Weapon':
        centered = source_array - source_array.mean(axis=0)
        eigenvalues, axes = np.linalg.eigh(centered.T @ centered)
        axis = axes[:, np.argmax(eigenvalues)]
        projection = centered @ axis
        radial = np.linalg.norm(centered - np.outer(projection, axis), axis=1)
        low_spread = radial[projection < np.percentile(projection, 15)].mean()
        high_spread = radial[projection > np.percentile(projection, 85)].mean()
        if low_spread < high_spread:
            butt = source_array[np.argmin(projection)]
            toward_head = Vector(tuple(axis))
        else:
            butt = source_array[np.argmax(projection)]
            toward_head = -Vector(tuple(axis))
        turn = toward_head.normalized().rotation_difference(Vector((0, 0, -1)))
        fitted_world = (Matrix.Translation(target_anchor) @ turn.to_matrix().to_4x4() @
                        Matrix.Translation(-Vector(tuple(butt))) @ obj.matrix_world)
    else:
        center = Vector(tuple((source_array.min(axis=0) + source_array.max(axis=0)) / 2))
        desired_center = target_anchor + Vector((0, 0, -0.20))
        fitted_world = (Matrix.Translation(desired_center - center) @ obj.matrix_world)
    transform = target_hand @ reference_hand.inverted() @ fitted_world
    obj.data.transform(transform)
    obj.parent = rig
    obj.matrix_parent_inverse = rig.matrix_world.inverted()
    obj.matrix_world = Matrix.Identity(4)
    obj.modifiers.clear()
    obj.vertex_groups.clear()
    obj.vertex_groups.new(name=bone_name).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    modifier = obj.modifiers.new('Rigid Hand Attachment', 'ARMATURE')
    modifier.object = rig
    obj.name = output_name
    material = obj.data.materials[0].copy()
    material.name = 'Orin' + source_name
    obj.data.materials.clear()
    obj.data.materials.append(material)
    equipment.append(obj)
for o in gear_objects:
    if o not in equipment:
        bpy.data.objects.remove(o, do_unlink=True)

for obj in [body] + equipment:
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(mesh, dist=1.0e-7, edges=list(mesh.edges))
    tiny_faces = [face for face in mesh.faces if face.calc_area() < 1.0e-6]
    if tiny_faces:
        bmesh.ops.delete(mesh, geom=tiny_faces, context='FACES_ONLY')
    mesh.to_mesh(obj.data)
    mesh.free()

scene = bpy.context.scene
scene.render.fps = 30
scene.render.fps_base = 1
# Tripo GLB imported at 24 fps; retain its authored seconds at output 30 Hz.
for layer in taunt.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            for curve in bag.fcurves:
                for key in curve.keyframe_points:
                    key.co.x *= 30 / 24
                    key.handle_left.x *= 30 / 24
                    key.handle_right.x *= 30 / 24
scene.frame_start = 1
scene.frame_end = max(math.ceil(a.frame_range[1]) for a in actions)
idle = next(a for a in actions if a.name == 'Idle')
assign(rig, idle)
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for o in [rig, body] + equipment:
    o.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.gltf(filepath=str(ROOT / 'orin-v1.3-animation-checkpoint.glb'),
    export_format='GLB', use_selection=True, export_materials='EXPORT',
    export_animations=True, export_animation_mode='ACTIONS', export_merge_animation='ACTION',
    export_anim_single_armature=True, export_armature_object_remove=True,
    export_rest_position_armature=True, export_reset_pose_bones=False,
    export_optimize_animation_keep_anim_armature=False, export_extra_animations=False,
    export_skins=True)
# Resolve sockets from the exported bind spaces, matching the runtime exactly.
import struct
blob = (ROOT / 'orin-v1.3-animation-checkpoint.glb').read_bytes()
json_length = struct.unpack_from('<I', blob, 12)[0]
gltf = json.loads(blob[20:20 + json_length])
binary = blob[28 + json_length:]
world = {}
def node_matrix(index, parent):
    node = gltf['nodes'][index]
    if 'matrix' in node:
        local = np.array(node['matrix']).reshape(4, 4).T
    else:
        t = Vector(node.get('translation', (0, 0, 0)))
        q = node.get('rotation', (0, 0, 0, 1))
        local = np.array(Matrix.LocRotScale(t, Quaternion((q[3], *q[:3])),
            Vector(node.get('scale', (1, 1, 1)))))
    world[index] = parent @ local
    for child in node.get('children', []): node_matrix(child, world[index])
for node in gltf['scenes'][gltf.get('scene', 0)]['nodes']:
    node_matrix(node, np.eye(4))
by_name = {n['name']: i for i, n in enumerate(gltf['nodes'])}
def equipment_center(mesh_name, bone_name, mean=False):
    index = by_name[mesh_name]
    mesh = gltf['meshes'][gltf['nodes'][index]['mesh']]
    acc = gltf['accessors'][mesh['primitives'][0]['attributes']['POSITION']]
    view = gltf['bufferViews'][acc['bufferView']]
    offset = view.get('byteOffset', 0) + acc.get('byteOffset', 0)
    stride = view.get('byteStride', 12)
    points = np.array([struct.unpack_from('<3f', binary, offset + i * stride)
                       for i in range(acc['count'])])
    center = points.mean(axis=0) if mean else (points.min(axis=0) + points.max(axis=0)) / 2
    local = np.linalg.inv(world[by_name[bone_name]]) @ world[index] @ np.append(center, 1)
    return [round(float(v), 8) for v in local[:3]]
sockets = {name: {'node': bone} for name, bone in {
    'Root': 'Hip', 'Head': 'Head', 'Chest': 'Spine02', 'HandRight': 'R_Hand',
    'HandLeft': 'L_Hand', 'FootLeft': 'L_Foot', 'FootRight': 'R_Foot'}.items()}
sockets['SwordBase'] = {'node': 'R_Hand'}
sockets['SwordTip'] = {'node': 'R_Hand', 'translation': equipment_center('01_Weapon', 'R_Hand', True)}
sockets['ShieldCenter'] = {'node': 'L_Hand', 'translation': equipment_center('00_Shield', 'L_Hand')}
descriptor = {'version': 1, 'sampleRate': 30,
    'clips': {a.name: {'loop': a.name in ('Idle', 'Walk', 'Run', 'Defend')} for a in actions},
    'sockets': sockets}
(ROOT / 'OrinV13.sm3d.json').write_text(json.dumps(descriptor, indent=2) + '\n', encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Blender/orin-v1.3-animation-working.blend'), compress=True)
(ROOT / 'orin-v1.3-animation-set.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')

scene.render.engine = 'CYCLES'
scene.cycles.samples = 12
scene.render.resolution_x = 1100
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Studio')
scene.world.color = (0.18, 0.18, 0.18)
bpy.ops.object.camera_add(location=(0.9, -3, 0.9))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, 0, 0.52)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.7
scene.camera = camera
for loc, power in [((1, -2, 3), 180), ((-2, -1, 1), 100)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    light = bpy.context.object
    light.data.energy = power
    light.data.size = 3
    light.rotation_euler = (Vector((0, 0, 0.5)) - light.location).to_track_quat('-Z', 'Y').to_euler()
for name, frame in [('Idle', 1), ('SwordAttack', 20), ('Walk', 15), ('Angry', 45)]:
    render_preview(rig, next(a for a in actions if a.name == name), frame, ROOT / 'Previews' / (name + '.png'))
print('ORIN_CHECKPOINT_READY', flush=True)
