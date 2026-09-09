"""Bake a reversible, rest-pose-relative Vrax motion trial. Run with Blender 5.2.

Only this package's Private outputs are written. The accepted Dragon and Vrax
packages, geometry, weights and bone lengths are never changed.
"""
import bpy
import hashlib
import json
import math
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector

PACKAGE = Path(__file__).resolve().parent
BOSSES = PACKAGE.parents[1]
ORIGINAL = PACKAGE.parent / 'RedDragonV11'
SOURCE = BOSSES / 'Vrax/VraxV1/Private/Vrax-v1-animation-source.blend'
OUTPUT = PACKAGE / 'Private'
OUTPUT.mkdir(exist_ok=True)
CLIPS = ['Idle', 'Walk', 'Run', 'Hit', 'Attack', 'Attack2', 'Attack3',
         'Attack4', 'Attack5', 'Attack6']

# Desired global orientation changes are measured against Vrax's Idle frame 1.
# His bone names use the opposite L/R sign to the Dragon builder; map by measured
# X position. Do not copy source translations, lengths or absolute rest poses.
MAPPING = {
    'Root': ('pelvis', .55, 22), 'Spine': ('spine2', .72, 30),
    'Chest': ('spine4', .80, 35), 'Neck': ('neck1', .85, 40),
    'Head': ('head', .90, 45), 'Jaw': ('jaw_joint1', .8, 25),
    'Tail1': ('tail1', .55, 25), 'Tail2': ('tail3', .55, 30),
    'Tail3': ('tail4', .55, 35), 'Tail4': ('tail6', .55, 40),
}
for target, source in [('L', 'r'), ('R', 'l')]:
    MAPPING.update({
        'WingRoot'+target: ('Claw_B_'+source+'2', .40, 28),
        'WingArm'+target: ('Claw_F_'+source+'4', .35, 30),
        'WingTip'+target: ('Claw_B_'+source+'7', .30, 28),
        'FrontLeg'+target: ('upperarm_'+source, .60, 38),
        'FrontFoot'+target: ('hand_'+source, .50, 35),
        'HindLeg'+target: ('thing_'+source, .65, 38),
        'HindFoot'+target: ('foot_'+source+'1', .50, 35),
    })

def assign(rig, action):
    rig.animation_data_create()
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]

def body_points(body):
    evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    points = [evaluated.matrix_world @ v.co for v in mesh.vertices]
    evaluated.to_mesh_clear()
    return points

def geometry_hash(body):
    data = {'vertices': [list(v.co) for v in body.data.vertices],
            'polygons': [list(p.vertices) for p in body.data.polygons],
            'weights': [[(g.group, g.weight) for g in v.groups] for v in body.data.vertices],
            'uv': [[list(p.uv) for p in layer.data] for layer in body.data.uv_layers]}
    return hashlib.sha256(json.dumps(data).encode()).hexdigest()

bpy.ops.wm.open_mainfile(filepath=str(ORIGINAL / 'red-dragon-v1.1-rig.blend'))
scene = bpy.context.scene
dragon = bpy.data.objects['RedDragonRig']
body = bpy.data.objects['RedDragonBody']
original_actions = list(bpy.data.actions)
original_hash = geometry_hash(body)
original_lengths = {b.name: b.length for b in dragon.data.bones}
for action in original_actions:
    action.use_fake_user = True
for track in dragon.animation_data.nla_tracks:
    track.mute = True

with bpy.data.libraries.load(str(SOURCE), link=False) as (src, dst):
    dst.objects = ['Armature']
    dst.actions = list(CLIPS)
source = dst.objects[0]
scene.collection.objects.link(source)
source.hide_render = True
source_actions = dict(zip(CLIPS, dst.actions))
for track in source.animation_data.nla_tracks:
    track.mute = True
assign(source, source_actions['Idle'])
scene.frame_set(1)
bpy.context.view_layer.update()
reference = {name: (source.matrix_world @ source.pose.bones[name].matrix).to_quaternion()
             for name, _, _ in MAPPING.values()}
rest = {b.name: b.matrix_local.to_quaternion() for b in dragon.data.bones}
rest_local = {b.name: (b.parent.matrix_local.inverted() @ b.matrix_local).to_quaternion()
              if b.parent else rest[b.name] for b in dragon.data.bones}
actions = []
evidence = []
for name in CLIPS:
    source_action = source_actions[name]
    assign(source, source_action)
    count = int(round(source_action.frame_range[1]))
    action = bpy.data.actions.new('Vrax_'+name)
    action.use_fake_user = True
    assign(dragon, action)
    actions.append(action)
    samples = []
    peak_lift = 0.0
    peak_motion = 0.0
    for frame in range(1, count+1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        desired = {}
        for bone in dragon.pose.bones:
            src_name, gain, limit = MAPPING[bone.name]
            current = (source.matrix_world @ source.pose.bones[src_name].matrix).to_quaternion()
            delta = current @ reference[src_name].inverted()
            if delta.w < 0:
                delta.negate()
            axis, angle = delta.to_axis_angle()
            angle = min(angle * gain, math.radians(limit))
            desired[bone.name] = Quaternion(axis, angle) @ rest[bone.name]
            parent_rotation = desired[bone.parent.name] if bone.parent else Quaternion()
            bone.rotation_mode = 'QUATERNION'
            bone.rotation_quaternion = rest_local[bone.name].inverted() @ parent_rotation.inverted() @ desired[bone.name]
            bone.location = (0, 0, 0)
            bone.scale = (1, 1, 1)
            peak_motion = max(peak_motion, angle)
        bpy.context.view_layer.update()
        points = body_points(body)
        lift = max(0.0, -min(p.z for p in points))
        peak_lift = max(peak_lift, lift)
        # One shared world-floor translation, not independent per-limb stretching.
        dragon.pose.bones['Root'].location = rest['Root'].inverted() @ Vector((0, 0, lift))
        for bone in dragon.pose.bones:
            bone.keyframe_insert('rotation_quaternion', frame=frame, group=bone.name)
        dragon.pose.bones['Root'].keyframe_insert('location', frame=frame, group='Root')
        bpy.context.view_layer.update()
        points = body_points(body)
        minimum = [min(p[i] for p in points) for i in range(3)]
        maximum = [max(p[i] for p in points) for i in range(3)]
        assert all(math.isfinite(c) for p in points for c in p), (name, frame, 'nonfinite')
        assert minimum[2] >= -0.0001, (name, frame, minimum[2])
        assert max(maximum[i]-minimum[i] for i in range(3)) < 3.0, (name, frame, 'bounds')
        if frame in {1, count//2, count}:
            samples.append({'frame': frame, 'minimum': minimum, 'maximum': maximum})
    assert peak_motion > .001, (name, 'motion missing')
    evidence.append({'clip': action.name, 'source': name, 'frames': count,
                     'fps': 30, 'maximumFloorLift': peak_lift,
                     'maximumMappedMotionDegrees': math.degrees(peak_motion), 'samples': samples})
    print('RETARGET', name, count, 'frames; peak floor lift', round(peak_lift, 5), flush=True)

assert geometry_hash(body) == original_hash
assert all(abs(b.length-original_lengths[b.name]) < 1e-7 for b in dragon.data.bones)
# Source motion stays in the private source package; export only target actions.
bpy.data.objects.remove(source, do_unlink=True)
for action in source_actions.values():
    if action not in original_actions:
        bpy.data.actions.remove(action)
for track in list(dragon.animation_data.nla_tracks):
    dragon.animation_data.nla_tracks.remove(track)
assign(dragon, actions[4])
scene.render.fps = 30
scene.frame_start = 1
scene.frame_end = int(actions[4].frame_range[1])
scene.frame_set(12)
bpy.ops.object.select_all(action='DESELECT')
dragon.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = dragon
output = OUTPUT / 'red-dragon-vrax-trial.glb'
bpy.ops.export_scene.gltf(filepath=str(output), export_format='GLB', use_selection=True,
    export_materials='EXPORT', export_animations=True, export_animation_mode='ACTIONS',
    export_merge_animation='ACTION', export_anim_single_armature=True,
    export_armature_object_remove=True, export_rest_position_armature=True,
    export_reset_pose_bones=False, export_optimize_animation_keep_anim_armature=False,
    export_extra_animations=False, export_skins=True)
descriptor = json.loads((ORIGINAL/'RedDragonV11.sm3d.json').read_text())
descriptor['clips'].update({a.name: {'loop': a.name in ['Vrax_Idle','Vrax_Walk','Vrax_Run']} for a in actions})
(PACKAGE/'RedDragonV12VraxTrial.sm3d.json').write_text(json.dumps(descriptor,indent=2)+'\n')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_distance=2.5
            area.spaces.active.region_3d.view_location=(0,-.2,.3)
            area.spaces.active.region_3d.view_rotation=Vector((1.1,-1.8,1.0)).to_track_quat('Z','Y')
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/'red-dragon-vrax-trial.blend'),compress=True)
report = {'status': 'Experimental adaptation, original Dragon unchanged',
          'sourceSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
          'originalDragonSha256': hashlib.sha256((ORIGINAL/'red-dragon-v1.1-animated.glb').read_bytes()).hexdigest(),
          'outputSha256': hashlib.sha256(output.read_bytes()).hexdigest(),
          'geometrySkinUVUnchanged': True, 'boneLengthsUnchanged': True,
          'mapping': MAPPING, 'clips': evidence,
          'limits': '24-bone preview rig: shared wing/hand chains; no independent fingers or IK foot planting. Ten selected Vrax clips, not a complete source-animation conversion.'}
(PACKAGE/'retarget-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print('TRIAL COMPLETE', output, flush=True)
