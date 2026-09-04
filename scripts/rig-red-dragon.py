"""Build a bounded preview rig and original actions for Sin Star I's Red Dragon.

Run in Blender 5.2: blender --background --python scripts/rig-red-dragon.py
The accepted static mesh, UVs and texture are preserved in a new revision.
"""
import bpy
import hashlib
import json
import math
import shutil
from pathlib import Path
from mathutils import Vector, Quaternion

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'games/SinStarI/SourceAssets/Bosses/RedDragon'
PACKAGE = SOURCE / 'RedDragonV11'
PACKAGE.mkdir(exist_ok=True)
for name in ['RedDragonV1.0.original.glb', 'RedDragonV1.0.static.glb', 'cyber-dragon-red-all-poses-preview.png']:
    shutil.copy2(SOURCE / name, PACKAGE / name)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(PACKAGE / 'RedDragonV1.0.static.glb'))
meshes = sorted([o for o in bpy.context.scene.objects if o.type == 'MESH'], key=lambda o: o.name)
original_triangles = sum(len(f.vertices) - 2 for o in meshes for f in o.data.polygons)

# Work at twice the static source scale. Actor scale 25,000 preserves the
# old static prop's world size while staying within Character3D's actor bound.
for obj in meshes:
    for vertex in obj.data.vertices:
        vertex.co = (obj.matrix_world @ vertex.co) * 2
    obj.matrix_world.identity()

bpy.ops.object.armature_add()
rig = bpy.context.object
rig.name = 'RedDragonRig'
rig.data.name = 'RedDragonSkeleton'
rig.show_in_front = True
bpy.ops.object.mode_set(mode='EDIT')
for bone in list(rig.data.edit_bones):
    rig.data.edit_bones.remove(bone)

specs = {}


def bone(name, head, tail, parent=None):
    b = rig.data.edit_bones.new(name)
    b.head = Vector(head) * 2
    b.tail = Vector(tail) * 2
    if parent:
        b.parent = rig.data.edit_bones[parent]
    specs[name] = {'head': list(b.head), 'tail': list(b.tail), 'parent': parent}


bone('Root', (0, -.17, .03), (0, -.17, .105))
bone('Spine', (0, -.14, .11), (0, -.25, .18), 'Root')
bone('Chest', (0, -.25, .18), (0, -.29, .24), 'Spine')
bone('Neck', (0, -.29, .24), (0, -.345, .293), 'Chest')
bone('Head', (0, -.345, .293), (0, -.401, .31), 'Neck')
bone('Jaw', (0, -.355, .285), (0, -.399, .271), 'Head')
tail = [(0, -.09, .13), (.06, .02, .08), (.12, .12, .047), (.175, .25, .032), (.2, .39, .055)]
for i in range(4):
    bone(f'Tail{i+1}', tail[i], tail[i+1], 'Root' if i == 0 else f'Tail{i}')
for side, sign in [('L', -1), ('R', 1)]:
    bone(f'WingRoot{side}', (sign*.035, -.278, .244), (sign*.205, -.282, .244), 'Chest')
    bone(f'WingArm{side}', (sign*.205, -.282, .244), (sign*.211, -.17, .342), f'WingRoot{side}')
    bone(f'WingTip{side}', (sign*.211, -.17, .342), (sign*.47, .15, .25), f'WingArm{side}')
    bone(f'FrontLeg{side}', (sign*.04, -.264, .165), (sign*.043, -.255, .065), 'Root')
    bone(f'FrontFoot{side}', (sign*.043, -.255, .065), (sign*.045, -.325, .016), f'FrontLeg{side}')
    bone(f'HindLeg{side}', (sign*.035, -.137, .16), (sign*.043, -.103, .065), 'Root')
    bone(f'HindFoot{side}', (sign*.043, -.103, .065), (sign*.043, -.1, .008), f'HindLeg{side}')
bpy.ops.object.mode_set(mode='OBJECT')


def blend(a, b, value):
    t = max(0., min(1., value))
    t = t*t*(3-2*t)
    return {a: 1-t, b: t}


wing_parts = {4, 5, 6, 7, 11, 13, 22, 24, 25, 31, 35, 37, 38, 40, 41, 45, 46, 47, 49, 50, 52, 53, 55, 57, 58, 59}
front_parts = {9, 10, 14, 16, 17, 23, 28, 33, 36}
hind_parts = {3, 26, 29, 32, 34, 39}
tail_parts = {1, 8, 12, 44, 51, 60, 63}


def weights(part, point):
    x, y, z = point / 2
    side = 'R' if x >= 0 else 'L'
    ax = abs(x)
    if part in wing_parts:
        if ax < .185:
            return blend('Chest', f'WingRoot{side}', (ax-.018)/.065)
        return blend(f'WingArm{side}', f'WingTip{side}', (ax-.205)/.13)
    if part in front_parts:
        return blend(f'FrontFoot{side}', f'FrontLeg{side}', (z-.045)/.045)
    if part in hind_parts:
        return blend(f'HindFoot{side}', f'HindLeg{side}', (z-.04)/.055)
    if part in tail_parts:
        stations = [-.06, .065, .185, .33]
        if y <= stations[0]: return blend('Root', 'Tail1', (y+.13)/.07)
        for i in range(3):
            if y < stations[i+1]:
                return blend(f'Tail{i+1}', f'Tail{i+2}', (y-stations[i])/(stations[i+1]-stations[i]))
        return {'Tail4': 1}
    if y < -.337 and z > .24:
        if part == 0 and z < .29:
            return blend('Head', 'Jaw', (.294-z)/.014)
        return {'Head': 1}
    if z > .255:
        return blend('Neck', 'Head', (-y-.32)/.03)
    if z > .195:
        return blend('Chest', 'Neck', (z-.215)/.06)
    return blend('Spine', 'Chest', (-y-.17)/.12)


for obj in meshes:
    part = int(obj.name.rsplit('_', 1)[1])
    groups = {name: obj.vertex_groups.new(name=name) for name in specs}
    for vertex in obj.data.vertices:
        influences = weights(part, vertex.co)
        assert abs(sum(influences.values()) - 1) < 1e-6
        for name, weight in influences.items():
            if weight > 0:
                groups[name].add([vertex.index], weight, 'REPLACE')
    modifier = obj.modifiers.new('Dragon Deformation', 'ARMATURE')
    modifier.object = rig
    obj.parent = rig

bpy.ops.object.select_all(action='DESELECT')
for obj in meshes: obj.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
body = bpy.context.object
body.name = 'RedDragonBody'
assert sum(len(f.vertices)-2 for f in body.data.polygons) == original_triangles

scene = bpy.context.scene
scene.render.fps = 30
rig.animation_data_create()
actions = []


def rotate(name, axis, degrees):
    b = rig.pose.bones[name]
    rest = rig.data.bones[name].matrix_local.to_quaternion()
    b.rotation_quaternion = rest.inverted() @ Quaternion(axis, math.radians(degrees)) @ rest


for name, count in [('Idle', 121), ('Roar', 91), ('FireBreath', 121), ('ClawStrike', 67), ('Hit', 25)]:
    action = bpy.data.actions.new(name)
    rig.animation_data.action = action
    actions.append(action)
    for frame in range(1, count+1, 3):
        t = (frame-1)/(count-1)
        wave = math.sin(t*math.tau)
        swell = math.sin(math.pi*t)**2
        for b in rig.pose.bones:
            b.rotation_mode = 'QUATERNION'
            b.rotation_quaternion = Quaternion()
            b.location = (0, 0, 0)
            b.scale = (1, 1, 1)
        rotate('Chest', (1,0,0), 1.2*wave)
        rotate('Neck', (1,0,0), 1.4*wave)
        rotate('Head', (0,0,1), 2*wave)
        for side, sign in [('L', -1), ('R', 1)]:
            rotate(f'WingRoot{side}', (0,1,0), sign * (4*wave + (10*swell if name in ['Roar', 'FireBreath'] else 0)))
            rotate(f'WingTip{side}', (0,1,0), sign * 3 * math.sin(t*math.tau+.4))
        for i in range(4):
            rotate(f'Tail{i+1}', (0,0,1), math.sin(t*math.tau-i*.4) * (2+i*.7))
        if name in ['Roar', 'FireBreath']:
            mouth = swell if name == 'Roar' else min(1, max(0,(t-.12)/.12)) * min(1,max(0,(.92-t)/.16))
            rotate('Jaw', (1,0,0), 18*mouth)
            rotate('Neck', (1,0,0), -4*swell if name == 'Roar' else 4*swell)
            rotate('Head', (0,0,1), 4*wave if name == 'FireBreath' else 0)
        if name == 'ClawStrike':
            strike = math.sin(math.pi*t)**3
            rotate('FrontLegR', (1,0,0), -35*strike)
            rotate('FrontFootR', (1,0,0), 12*strike)
            rotate('Neck', (0,0,1), -7*strike)
        if name == 'Hit':
            rotate('Chest', (1,0,0), -7*swell)
            rotate('Head', (1,0,0), 6*swell)
        for b in rig.pose.bones:
            b.keyframe_insert('rotation_quaternion', frame=frame, group=b.name)
    action.use_fake_user = True

validation = []
for action in actions:
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    samples = []
    for frame in [1, int(action.frame_range[1]/2), int(action.frame_range[1])]:
        scene.frame_set(frame)
        evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        points = [evaluated.matrix_world @ v.co for v in mesh.vertices]
        assert all(math.isfinite(c) for point in points for c in point)
        minimum = [min(v[i] for v in points) for i in range(3)]
        maximum = [max(v[i] for v in points) for i in range(3)]
        assert minimum[2] > -.025, (action.name, frame, minimum)
        assert all(maximum[i]-minimum[i] < 2.5 for i in range(3))
        samples.append({'frame': frame, 'min': minimum, 'max': maximum})
        evaluated.to_mesh_clear()
    validation.append({'clip': action.name, 'samples': samples})
(PACKAGE/'rig-validation.json').write_text(json.dumps(validation, indent=2)+'\n')

rig.animation_data.action = actions[0]
rig.animation_data.action_slot = actions[0].slots[0]
scene.frame_start = 1
scene.frame_end = 121
scene.frame_set(1)
bpy.context.view_layer.update()

# Keep the production derivative limited to the rig and unchanged mesh.
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
output = PACKAGE/'red-dragon-v1.1-animated.glb'
bpy.ops.export_scene.gltf(filepath=str(output), export_format='GLB', use_selection=True,
    export_materials='EXPORT', export_animations=True, export_animation_mode='ACTIONS',
    export_merge_animation='ACTION', export_anim_single_armature=True,
    export_armature_object_remove=True, export_rest_position_armature=True,
    export_reset_pose_bones=False, export_optimize_animation_keep_anim_armature=False,
    export_extra_animations=False, export_skins=True)

sockets = {}
for name, joint, position in [('Root','Root',(0,-.17,.03)), ('Chest','Chest',(0,-.3,.20)),
                              ('Head','Head',(0,-.38,.315)), ('Mouth','Jaw',(0,-.408,.284))]:
    point = rig.data.bones[joint].matrix_local.inverted() @ (Vector(position)*2)
    sockets[name] = {'node': joint, 'translation': list(point)}
descriptor = {'version':1,'sampleRate':30,'clips':{a.name:{'loop':a.name=='Idle'} for a in actions}, 'sockets':sockets}
(PACKAGE/'RedDragonV11.sm3d.json').write_text(json.dumps(descriptor,indent=2)+'\n')

# A ready-to-inspect Blender view; cameras/lights are added after GLB export.
for obj in bpy.context.selected_objects: obj.select_set(False)
body.select_set(True)
bpy.context.view_layer.objects.active = body
rig.show_in_front = False
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_distance=2.0
            area.spaces.active.region_3d.view_location=(0,-.2,.3)
            area.spaces.active.region_3d.view_rotation=(Vector((1.1,-1.8,1.1))-Vector((0,-.2,.3))).to_track_quat('Z','Y')
bpy.ops.wm.save_as_mainfile(filepath=str(PACKAGE/'red-dragon-v1.1-rig.blend'),compress=True)
manifest={'version':'1.1','status':'Animated preview rig','builder':'scripts/rig-red-dragon.py',
 'source':'RedDragonV1.0.static.glb','geometryChanged':False,'sourceScaleMultiplier':2,
 'runtimeScalePercent':25000,'vertices':len(body.data.vertices),'triangles':original_triangles,
 'bones':specs,'clips':list(descriptor['clips']),'sockets':list(sockets),
 'modelSha256':hashlib.sha256(output.read_bytes()).hexdigest().upper()}
(PACKAGE/'red-dragon-v1.1-package.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('DRAGON RIG',len(specs),'bones,',len(actions),'clips,',original_triangles,'unchanged triangles')
