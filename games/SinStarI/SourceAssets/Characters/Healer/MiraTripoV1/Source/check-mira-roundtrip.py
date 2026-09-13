"""Check the actual exported GLB against its authored poses, including the repaired arm."""
import bpy
import hashlib
import json
import math
from pathlib import Path
from mathutils import Vector

PACKAGE = Path(__file__).resolve().parent.parent
MODEL = PACKAGE / 'mira-animation-checkpoint.glb'
JOINTS = ['mixamorig:RightArm', 'mixamorig:RightForeArm', 'mixamorig:RightHand', 'MiraStaff']


def sample(rig, action, frame):
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    result = {}
    graph = bpy.context.evaluated_depsgraph_get()
    for name in ['Mira.Body', 'Mira.Staff']:
        obj = bpy.data.objects[name]
        points = [obj.matrix_world @ vertex.co for vertex in obj.evaluated_get(graph).data.vertices]
        result[name] = [min(p[a] for p in points) for a in range(3)] + [max(p[a] for p in points) for a in range(3)]
    result['joints'] = {name: list((rig.matrix_world @ rig.pose.bones[name].matrix).translation) for name in JOINTS}
    return result


bpy.ops.wm.open_mainfile(filepath=str(PACKAGE / 'Blender/mira-rigged-animation-checkpoint.blend'))
rig = bpy.data.objects['Mira.Rig']
held = ['Idle', 'Walk', 'Attack', 'Defend', 'HealOne', 'HealParty', 'Hit']
wrist_rest = (rig.data.bones[JOINTS[1]].matrix_local.inverted() @ rig.data.bones[JOINTS[2]].matrix_local).to_quaternion()
arm_checks = []
for name in held:
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    maximum_wrist_error = 0.0
    maximum_grip_error = 0.0
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        forearm = rig.pose.bones[JOINTS[1]].matrix
        hand = rig.pose.bones[JOINTS[2]].matrix
        relative = (forearm.inverted() @ hand).to_quaternion()
        angle = relative.rotation_difference(wrist_rest).angle
        angle = min(angle, 2 * math.pi - angle)
        maximum_wrist_error = max(maximum_wrist_error, math.degrees(angle))
        maximum_grip_error = max(maximum_grip_error, (rig.pose.bones['MiraStaff'].matrix.translation - hand @ Vector((0,.031,.013))).length)
    assert maximum_wrist_error < .1 and maximum_grip_error < .0001, (name, maximum_wrist_error, maximum_grip_error)
    arm_checks.append({'clip':name,'maximumWristDeviationFromNeutralDegrees':maximum_wrist_error,'maximumStaffGripErrorMeters':maximum_grip_error})
expected = []
for action in list(bpy.data.actions):
    start, end = map(int, action.frame_range)
    for frame in sorted(set([start, (start + end) // 2, end])):
        expected.append({'clip': action.name, 'frame': frame, 'sample': sample(rig, action, frame)})

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.render.fps = 30
bpy.ops.import_scene.gltf(filepath=str(MODEL))
rig = next(obj for obj in bpy.data.objects if obj.type == 'ARMATURE')
for track in rig.animation_data.nla_tracks:
    track.mute = True
results = []
for row in expected:
    action = next(action for action in bpy.data.actions if action.name == row['clip'] or action.name.startswith(row['clip'] + '_'))
    # Compare relative clip time; Blender can preserve the exported start-frame offset.
    actual = sample(rig, action, int(action.frame_range[0]) + row['frame'] - 1)
    bounds_error = max(abs(a-b) for name in ['Mira.Body', 'Mira.Staff'] for a,b in zip(actual[name], row['sample'][name]))
    joint_error = max((Vector(actual['joints'][name]) - Vector(row['sample']['joints'][name])).length for name in JOINTS)
    assert bounds_error < .00005 and joint_error < .00005, (row['clip'], row['frame'], bounds_error, joint_error)
    results.append({'clip':row['clip'], 'frame':row['frame']-1, 'maximumBoundsErrorMeters':bounds_error, 'maximumArmStaffJointErrorMeters':joint_error})
report = {'modelSha256':hashlib.sha256(MODEL.read_bytes()).hexdigest(), 'heldArmChecks':arm_checks, 'samples':results, 'acceptedToleranceMeters':.00005}
(PACKAGE/'Source/mira-roundtrip-checkpoint.json').write_text(json.dumps(report, indent=2))
print('MIRA_ROUNDTRIP_PASSED', json.dumps(report), flush=True)
