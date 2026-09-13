"""Fit Mira's own Mixamo clips to the preserved skinned bind pose and PBR material."""
import bpy
import json
import shutil
from pathlib import Path
from mathutils import Matrix, Vector

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
PACK = STAGE / 'MixamoPack'
CLIPS = {'Idle':'Standing Idle 03.fbx', 'Walk':'Standing Walk Forward.fbx',
         'Run':'Standing Run Forward.fbx', 'Attack':'Standing 1H Magic Attack 01.fbx',
         'Defend':'Standing Block Start.fbx', 'HealOne':'standing 1H cast spell 01.fbx',
         'HealParty':'Standing 2H Cast Spell 01.fbx', 'Hit':'Standing React Small From Front.fbx',
         'Death':'Standing React Death Backward.fbx'}
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-tripo-mixamo-rig.blend'))
scene = bpy.context.scene
scene.render.fps = 30
rig = bpy.data.objects['Mira.Rig']
body = bpy.data.objects['Mira.Body.Mixamo']
body.name = 'Mira.Body'
rig.animation_data_clear()
rig.data.pose_position = 'REST'
bpy.ops.object.select_all(action='DESELECT')
for obj in (rig, body):
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
rig.data.pose_position = 'POSE'
for bone in rig.pose.bones:
    bone.matrix_basis.identity()
for action in list(bpy.data.actions):
    bpy.data.actions.remove(action)
with bpy.data.libraries.load(str(STAGE / 'mira-tripo-baked-work.blend')) as (available, loaded):
    loaded.materials = ['Mira.Tripo.4K.PBR']
body.data.materials.clear()
body.data.materials.append(loaded.materials[0])
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.vertex_group_limit_total(limit=4)
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
report = []
for name, filename in CLIPS.items():
    source_path = PACK / filename
    shutil.copy2(source_path, PACKAGE / 'Source' / 'Mixamo' / filename)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(source_path))
    imported = set(bpy.data.objects) - before
    source = next(o for o in imported if o.type == 'ARMATURE')
    source_action = source.animation_data.action
    start, end = map(int, source_action.frame_range)
    conversion = rig.matrix_world.inverted() @ source.matrix_world
    rig.animation_data_create()
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data.action = action
    maximum_error = 0.0
    for frame in range(start, end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        desired = {}
        for p in source.pose.bones:
            matrix = conversion @ p.matrix
            # Convert joint positions to meters without shrinking bone basis axes.
            desired[p.name] = Matrix.LocRotScale(matrix.translation, matrix.to_quaternion(),
                                                Vector((1, 1, 1)))
        for p in rig.pose.bones:
            kwargs = {}
            if p.parent:
                kwargs = {'parent_matrix':desired[p.parent.name],
                          'parent_matrix_local':p.parent.bone.matrix_local}
            p.matrix_basis = p.bone.convert_local_to_pose(desired[p.name], p.bone.matrix_local,
                                                         invert=True, **kwargs)
            p.rotation_mode = 'QUATERNION'
            for prop in ('location','rotation_quaternion','scale'):
                p.keyframe_insert(data_path=prop, frame=frame-start+1)
        bpy.context.view_layer.update()
        maximum_error = max(maximum_error, max((p.matrix.translation-desired[p.name].translation).length
                                               for p in rig.pose.bones))
    report.append({'clip':name, 'source':filename, 'samples':end-start+1,
                   'maxJointErrorMeters':maximum_error})
    assert maximum_error < .00001, report[-1]
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.actions.remove(source_action)
    print('MIRA_CLIP_READY '+json.dumps(report[-1]),flush=True)
rig.animation_data.action = bpy.data.actions['Idle']
rig.animation_data.action_slot = bpy.data.actions['Idle'].slots[0]
scene.frame_set(1)
scene.frame_start = 1
scene.frame_end = int(bpy.data.actions['Idle'].frame_range[1])
(STAGE / 'animation-assembly.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-tripo-animation-work.blend'),compress=True)
print('MIRA_ANIMATION_ASSEMBLED',flush=True)
