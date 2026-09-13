"""Bake Mira1's own staff hold, free-hand casts, cape hinge and floor contacts.

Run after retexture-mira1.py using Blender 5.2 --background --python.
The original selected comparison remains immutable. No runtime IK is required.
"""
import bpy
import bmesh
import importlib.util
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector
from mathutils.kdtree import KDTree

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\Mira1Selected')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira1-retextured.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['Mira.Rig']
body = bpy.data.objects['Mira.SkinnedBody']
staff = bpy.data.objects['Mira.Staff']
rig.data.pose_position = 'POSE'
spec = importlib.util.spec_from_file_location('mira_staff', Path(__file__).with_name('repair-mira1-staff.py'))
sys.dont_write_bytecode = True
staff_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(staff_module)
staff_report = staff_module.repair_staff(staff)


def select_action(name):
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    return action


def at(frame):
    scene.frame_set(frame)
    bpy.context.view_layer.update()


def write_pose(bone, matrix, frame, parent_matrix=None):
    kwargs = {}
    if bone.parent:
        kwargs = {'parent_matrix': parent_matrix if parent_matrix is not None else bone.parent.matrix,
                  'parent_matrix_local': bone.parent.bone.matrix_local}
    bone.matrix_basis = bone.bone.convert_local_to_pose(matrix, bone.bone.matrix_local, invert=True, **kwargs)
    bone.rotation_mode = 'QUATERNION'
    for prop in ['location', 'rotation_quaternion', 'scale']:
        bone.keyframe_insert(data_path=prop, frame=frame)


select_action('Idle')
at(1)
hand_name = 'mixamorig:RightHand'
arm_names = ['mixamorig:RightArm', 'mixamorig:RightForeArm', hand_name]
idle_hand = rig.pose.bones[hand_name].matrix.copy()
carry_offset = idle_hand.translation - rig.pose.bones[arm_names[0]].head
minimum_hand = idle_hand.translation.z - .04
reflection = Matrix.Diagonal((-1, 1, 1, 1))
for name in ['Attack', 'HealOne']:
    action = select_action(name)
    samples = []
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        at(frame)
        desired = {}
        for bone in rig.pose.bones:
            other_name = bone.name.replace('Left', 'TEMP').replace('Right', 'Left').replace('TEMP', 'Right')
            other = rig.pose.bones[other_name]
            desired[bone.name] = reflection @ other.matrix @ other.bone.matrix_local.inverted() @ reflection @ bone.bone.matrix_local
        samples.append((frame, desired))
    for frame, desired in samples:
        at(frame)
        for bone in rig.pose.bones:
            write_pose(bone, desired[bone.name], frame, desired.get(bone.parent.name) if bone.parent else None)
print('FREE_HAND_CASTS_READY', flush=True)

target = bpy.data.objects.new('Temporary Mira1 Staff Hold', None)
pole = bpy.data.objects.new('Temporary Mira1 Elbow Pole', None)
scene.collection.objects.link(target)
scene.collection.objects.link(pole)
for name in ['Attack', 'HealOne', 'HealParty', 'Walk', 'Run', 'Defend', 'Hit']:
    action = select_action(name)
    forearm = rig.pose.bones[arm_names[1]]
    constraint = forearm.constraints.new('IK')
    constraint.target = target
    constraint.pole_target = pole
    constraint.chain_count = 2
    constraint.use_stretch = False
    samples = []
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        at(frame)
        shoulder = rig.pose.bones[arm_names[0]].head.copy()
        point = shoulder + carry_offset
        point.z = max(minimum_hand, point.z)
        target.location = rig.matrix_world @ point
        pole.location = rig.matrix_world @ (shoulder + Vector((-.42, .25, -.18)))
        bpy.context.view_layer.update()
        pose = {name: rig.pose.bones[name].matrix.copy() for name in arm_names}
        vertical = idle_hand.copy()
        vertical.translation = pose[hand_name].translation
        pose[hand_name] = vertical
        samples.append((frame, pose))
    forearm.constraints.remove(constraint)
    for frame, pose in samples:
        at(frame)
        for bone_name in arm_names:
            bone = rig.pose.bones[bone_name]
            write_pose(bone, pose[bone_name], frame, pose.get(bone.parent.name))
for obj in [target, pole]:
    bpy.data.objects.remove(obj, do_unlink=True)
print('STAFF_HOLD_READY', flush=True)

# The added cape is a closed component, independent of seam vertex numbering.
bm = bmesh.new()
bm.from_mesh(body.data)
bm.verts.ensure_lookup_table()
todo = set(bm.verts)
cape_indices = None
while todo:
    first = todo.pop()
    component, queue = {first}, [first]
    while queue:
        vertex = queue.pop()
        for edge in vertex.link_edges:
            other = edge.other_vert(vertex)
            if other in todo:
                todo.remove(other)
                component.add(other)
                queue.append(other)
    if len(component) == 90 and min(v.co.y for v in component) > .11:
        cape_indices = {v.index for v in component}
bm.free()
assert cape_indices is not None, 'Expected the preserved closed waist cape'
tree = KDTree(len(body.data.vertices) - len(cape_indices))
for vertex in body.data.vertices:
    if vertex.index not in cape_indices:
        tree.insert(vertex.co, vertex.index)
tree.balance()
for index in cape_indices:
    vertex = body.data.vertices[index]
    top = min(1, max(0, (vertex.co.z - .90) / .14))
    vertex.co.y -= .045 * top
    vertex.co.z += .035 * top

bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
cape_bone = rig.data.edit_bones.new('MiraCape')
cape_bone.head = (0, .108, 1.075)
cape_bone.tail = (0, .108, .575)
cape_bone.parent = rig.data.edit_bones['mixamorig:Hips']
bpy.ops.object.mode_set(mode='OBJECT')
cape_group = body.vertex_groups.new(name='MiraCape')
for index in cape_indices:
    vertex = body.data.vertices[index]
    cape_weight = min(1, max(0, (1.075 - vertex.co.z) / .23))
    near = tree.find_n(vertex.co, 4)
    weights = {}
    total = sum(1 / max(distance, .002) for co, i, distance in near)
    for co, i, distance in near:
        fraction = (1 / max(distance, .002)) / total
        for group in body.data.vertices[i].groups:
            weights[group.group] = weights.get(group.group, 0) + group.weight * fraction * (1 - cape_weight)
    weights[cape_group.index] = cape_weight
    weights = dict(sorted(weights.items(), key=lambda item: -item[1])[:4])
    total = sum(weights.values())
    for group in body.vertex_groups:
        group.remove([index])
    for group, weight in weights.items():
        if weight > .000001:
            body.vertex_groups[group].add([index], weight / total, 'REPLACE')
body.data.update()


def body_minimum(exclude_cape=False):
    mesh = body.evaluated_get(bpy.context.evaluated_depsgraph_get()).data
    return min((body.matrix_world @ vertex.co).z for vertex in mesh.vertices
               if not exclude_cape or vertex.index not in cape_indices)


rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
bind_minimum = body_minimum()
common = -bind_minimum
rig.location.z += common
rig.data.pose_position = 'POSE'
hips = rig.pose.bones['mixamorig:Hips']
local_up = hips.bone.matrix_local.to_3x3().inverted() @ rig.matrix_world.to_3x3().inverted() @ Vector((0, 0, 1))
grounding = []
for action in bpy.data.actions:
    select_action(action.name)
    samples = []
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        at(frame)
        minimum = body_minimum(True)
        correction = -minimum if action.name in ['Idle', 'Walk'] else max(0, -minimum)
        samples.append((frame, hips.location.copy(), correction, minimum))
    for frame, location, correction, minimum in samples:
        at(frame)
        hips.location = location + local_up * correction
        hips.keyframe_insert(data_path='location', frame=frame)
    grounding.append({'clip': action.name, 'maximumLift': max(s[2] for s in samples),
                      'frame0Before': samples[0][3], 'frames': len(samples)})
print('BODY_GROUNDED', flush=True)

# Search a small hinge rotation while keeping the seam attached to nearby waist skin.
cape = rig.pose.bones['MiraCape']
pivot = cape.bone.head_local
inverse_rest = {bone.name: bone.matrix_local.inverted() for bone in rig.data.bones}
cape_vertices = [(body.data.vertices[i].co.copy(),
                  [(body.vertex_groups[g.group].name, g.weight) for g in body.data.vertices[i].groups])
                 for i in cape_indices]
cape_report = []
for action in bpy.data.actions:
    select_action(action.name)
    angles = []
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        at(frame)
        parent = hips.matrix @ inverse_rest[hips.name]
        fixed = {bone.name: bone.matrix @ inverse_rest[bone.name] for bone in rig.pose.bones}

        def candidate(degrees):
            rotation = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(degrees), 4, 'X') @ Matrix.Translation(-pivot)
            transform = parent @ rotation
            minimum = 100
            for coordinate, groups in cape_vertices:
                point = Vector((0, 0, 0))
                for name, weight in groups:
                    point += ((transform if name == 'MiraCape' else fixed[name]) @ coordinate) * weight
                minimum = min(minimum, (rig.matrix_world @ point).z)
            return minimum, transform @ cape.bone.matrix_local

        previous = angles[-1] if angles else 0
        possibilities = sorted(range(-170, 171), key=lambda degree: abs(degree) + .6 * abs(degree - previous))
        best = None
        for degrees in possibilities:
            minimum, matrix = candidate(degrees)
            if best is None or minimum > best[0]:
                best = (minimum, degrees, matrix)
            if minimum >= .004:
                best = (minimum, degrees, matrix)
                break
        assert best[0] >= -.0001, f'Cape cannot clear floor: {action.name} frame {frame}, {best[0]}'
        write_pose(cape, best[2], frame)
        angles.append(best[1])
    cape_report.append({'clip': action.name, 'minimumDegrees': min(angles), 'maximumDegrees': max(angles),
                        'maximumFrameStep': max([abs(a - b) for a, b in zip(angles, angles[1:])] or [0])})
print('CAPE_CLEARANCE_READY', flush=True)

# Use Mira1's own staff bind orientation to settle the long weapon beside her.
action = select_action('Death')
hand = rig.pose.bones[hand_name]
staff_points = [staff.matrix_world @ vertex.co for vertex in staff.data.vertices]
staff_angles = []
for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
    at(frame)
    position = hand.matrix.translation.copy()
    t = min(1, max(0, (frame - 45) / 30))
    preferred = 90 * t * t * (3 - 2 * t)
    candidates = sorted(range(0, 181, 2), key=lambda degree: abs(degree - preferred))
    for degrees in candidates:
        matrix = Matrix.Rotation(math.radians(degrees), 4, 'Y') @ idle_hand
        matrix.translation = position
        transform = rig.matrix_world @ matrix @ inverse_rest[hand_name]
        minimum = min((transform @ point).z for point in staff_points)
        if minimum >= .002:
            break
    else:
        raise RuntimeError(f'Death staff cannot clear floor at {frame}')
    write_pose(hand, matrix, frame)
    staff_angles.append(degrees)

select_action('Idle')
at(1)
for action in bpy.data.actions:
    action.use_fake_user = True
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira1-repaired.blend'))
(STAGE / 'mira1-animation-repair.json').write_text(json.dumps({
    'bindBodyMinimum': bind_minimum, 'commonActorOffset': common,
    'bodyGrounding': grounding, 'cape': cape_report, 'capeVertices': len(cape_indices),
    'capeAttachment': 'Top rows extended beneath belt, nearest waist weights, one baked hinge',
    'castingHand': 'Left', 'staff': staff_report, 'staffCarryOffset': list(carry_offset),
    'staffDeathDegrees': [min(staff_angles), max(staff_angles)],
    'policy': 'Ground body without equipment/cape; preserve Run airtime; articulate cape independently'
}, indent=2))
print('MIRA1_ANIMATION_READY', flush=True)
