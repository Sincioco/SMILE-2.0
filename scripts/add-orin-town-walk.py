"""Append the matching Mixamo town Walk, retaining the nine accepted clips byte-for-byte.

Run in background Blender. Produces a candidate and audit under artifacts for review.
"""
from pathlib import Path
import importlib.util
import json
import shutil
import sys
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
sys.dont_write_bytecode = True
OUT = ROOT / 'artifacts/orin-town-walk'
OUT.mkdir(exist_ok=True)

def module(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'scripts' / file)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result

b = module('orin_builder', 'build-orin-v1-3-mixamo.py')
a = module('clip_append', 'add-party-victory-clip.py')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.render.fps=30
accepted=b.import_file(b.PACKAGE/'orin-v1.3-nine-clips.glb')
accepted_rig=next(o for o in accepted if o.type=='ARMATURE')
accepted_idle=bpy.data.actions['Idle']
a.activate(accepted_rig,accepted_idle,int(accepted_idle.frame_range[0]))
accepted_floor=a.minimum([o for o in accepted if o.type=='MESH' and o.name.startswith('02_Body')])
bpy.ops.wm.read_factory_settings(use_empty=True)
objects = b.import_file(b.IDLE_SOURCE)
rig = next(o for o in objects if o.type == 'ARMATURE')
bpy.context.scene.frame_set(1)
body = [o for o in objects if o.type == 'MESH']
idle_low = accepted_floor
source = b.ANIMATIONS / 'orin-v1.3-mixamo-town-walk.fbx'
incoming = b.import_file(source)
other = next(o for o in incoming if o.type == 'ARMATURE')
assert [x.name for x in rig.data.bones] == [x.name for x in other.data.bones]
delta = max(abs(x-y) for bone in rig.data.bones
            for row, other_row in zip(bone.matrix_local, other.data.bones[bone.name].matrix_local)
            for x,y in zip(row, other_row))
assert all((x.parent.name if x.parent else None) ==
           (other.data.bones[x.name].parent.name if other.data.bones[x.name].parent else None)
           for x in rig.data.bones)
source_action = other.animation_data.action
first,last = map(round, source_action.frame_range)
conversion = rig.matrix_world.inverted() @ other.matrix_world
# Re-uploading the skin changes FBX rest axes. Sample the actual armature-space
# poses and solve the accepted rig's local channels; copying curves or applying
# an extra rest rotation visibly twists the shoulders.
samples=[]
for frame in range(first,last+1):
    bpy.context.scene.frame_set(frame)
    samples.append({p.name: conversion @ p.matrix
                    for p in other.pose.bones})
action = bpy.data.actions.new('Walk')
b.assign_action(rig, action)
for frame,poses in enumerate(samples,1):
    for p in rig.pose.bones:
        a.write_pose(p,poses[p.name],frame,poses.get(p.parent.name) if p.parent else None)
for obj in incoming:
    bpy.data.objects.remove(obj, do_unlink=True)
for old in list(bpy.data.actions):
    if old != action: bpy.data.actions.remove(old, do_unlink=True)
b.assign_action(rig, action)
b.remove_armature_object_channels([action])
b.normalize_rig(rig, [action])
contact=[]
for frame in range(1,len(samples)+1):
    a.activate(rig,action,frame)
    low=a.minimum(body)
    pose=rig.pose.bones['Root'].matrix.copy()
    pose.translation += rig.matrix_world.inverted().to_3x3() @ Vector((0,0,idle_low-low))
    a.write_pose(rig.pose.bones['Root'],pose,frame)
    contact.append(a.minimum(body))
bpy.context.scene.render.fps = 30
bpy.context.scene.frame_start, bpy.context.scene.frame_end = map(round, action.frame_range)
bpy.context.scene.frame_set(bpy.context.scene.frame_start)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'orin-v1.3-town-walk.blend'),compress=True)
bpy.ops.object.select_all(action='DESELECT')
for obj in objects: obj.select_set(True)
bpy.context.view_layer.objects.active = rig
clip = OUT / 'Walk.glb'
bpy.ops.export_scene.gltf(filepath=str(clip), export_format='GLB', use_selection=True,
    export_animations=True, export_animation_mode='ACTIONS', export_merge_animation='ACTION',
    export_anim_single_armature=True, export_armature_object_remove=True,
    export_rest_position_armature=True, export_reset_pose_bones=False,
    export_optimize_animation_keep_anim_armature=False, export_extra_animations=False,
    export_skins=True)
candidate = OUT / 'orin-v1.3-animation-checkpoint.glb'
shutil.copyfile(b.PACKAGE / 'orin-v1.3-nine-clips.glb', candidate)
audit = a.append_animation(candidate, clip, 'Walk')
audit.update(source=source.name, maxRestMatrixDelta=delta, bones=len(rig.data.bones),
             sourceSha256=b.source_hash(source), modelSha256=b.source_hash(candidate),
             idleMinimum=idle_low, walkMinimum=min(contact), walkMaximum=max(contact),
             retarget='Sampled armature-space poses onto accepted rig, grounded walking contact')
(OUT/'walk-import.json').write_text(json.dumps(audit, indent=2)+'\n')
print('ORIN WALK', json.dumps(audit), flush=True)
