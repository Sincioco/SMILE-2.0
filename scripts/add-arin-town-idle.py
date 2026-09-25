"""Author a relaxed town-only pose and append it without rewriting accepted clips.

Run with background Blender. Review artifacts/temp/arin-town-idle.png and candidate
before promoting; the accepted model, descriptor and calibration are not mutated.
"""
from pathlib import Path
import importlib.util
import json
import math
import shutil
import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT=Path(__file__).resolve().parent.parent
PACKAGE=ROOT/'games/SinStarI/SourceAssets/Characters/Paladin/ArinV57'
OUT=ROOT/'artifacts/temp'
OUT.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('clip_append',ROOT/'scripts/add-party-victory-clip.py')
a=importlib.util.module_from_spec(spec); spec.loader.exec_module(a)
base=PACKAGE/'arin-v5.7-ten-clips.glb'
assert len(a.read_glb(base)[0]['animations'])==10
bpy.ops.wm.open_mainfile(filepath=str(PACKAGE/'Blender/arin-v5.7-victory.blend'))
scene=bpy.context.scene
rig=next(o for o in scene.objects if o.type=='ARMATURE')
meshes=[o for o in scene.objects if o.type=='MESH' and
        (o.parent==rig or any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers))]
body=[o for o in meshes if o.name.startswith('tripo_part')]
idle=bpy.data.actions['Idle']
a.activate(rig,idle,1)
idle_low=a.minimum(body)
rig.animation_data_clear()
for bone in rig.pose.bones: bone.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update()
bind_low=a.minimum(body)

def aim(name,direction):
    bone=rig.pose.bones['mixamorig:'+name]
    pose=bone.matrix.copy()
    current=(bone.tail-bone.head).normalized()
    rotation=current.rotation_difference(Vector(direction).normalized())
    pose=Matrix.LocRotScale(pose.translation,rotation@pose.to_quaternion(),Vector((1,1,1)))
    a.write_pose(bone,pose,1)
    bpy.context.view_layer.update()

action=bpy.data.actions.new('TownIdle'); action.use_fake_user=True
a.activate(rig,action,1)
for side,sign in [('Left',1),('Right',-1)]:
    aim(side+'Arm',(sign*.19,0,-1))
    aim(side+'ForeArm',(sign*.08,-.08,-1))
    aim(side+'Hand',(sign*.06,-.02,-1))
hips=rig.pose.bones['mixamorig:Hips']
pose=hips.matrix.copy(); pose.translation.z+=idle_low-a.minimum(body)
a.write_pose(hips,pose,1)
bpy.context.view_layer.update()
neutral={p.name:p.matrix_basis.copy() for p in rig.pose.bones}
for frame in [1,31,61]:
    for bone in rig.pose.bones:
        bone.matrix_basis=neutral[bone.name]
        bone.rotation_mode='QUATERNION'
        # Breathing stays in the torso, preserving both planted feet.
        if frame==31 and bone.name=='mixamorig:Spine2':
            bone.rotation_quaternion.rotate(Quaternion((1,0,0),math.radians(.4)))
        for prop in ('location','rotation_quaternion','scale'):
            bone.keyframe_insert(data_path=prop,frame=frame)
scene.render.fps=30; scene.frame_start=1; scene.frame_end=61
contact=[]
for frame in [1,31,61]:
    a.activate(rig,action,frame)
    contact.append(a.minimum(body))
assert max(abs(v-idle_low) for v in contact)<.0001
a.activate(rig,action,1)
bpy.ops.wm.save_as_mainfile(filepath=str(PACKAGE/'Blender/arin-v5.7-town-idle.blend'),compress=True)
for old in list(bpy.data.actions):
    if old!=action: bpy.data.actions.remove(old)
bpy.ops.object.select_all(action='DESELECT')
for obj in [rig,*meshes]: obj.select_set(True)
bpy.context.view_layer.objects.active=rig
clip=OUT/'arin-town-idle-only.glb'
bpy.ops.export_scene.gltf(filepath=str(clip),export_format='GLB',use_selection=True,
    export_animations=True,export_animation_mode='ACTIONS',export_merge_animation='ACTION',
    export_anim_single_armature=True,export_armature_object_remove=True,
    export_rest_position_armature=True,export_reset_pose_bones=False,
    export_optimize_animation_keep_anim_armature=False,export_skins=True)
candidate=OUT/'arin-town-idle-candidate.glb'; shutil.copyfile(base,candidate)
audit=a.append_animation(candidate,clip,'TownIdle')
audit.update(bindBodyMinimumY=bind_low,idleBodyMinimumY=idle_low,townBodyMinimumY=contact,
             frames=61,sampleRate=30,source='Blender/arin-v5.7-victory.blend',
             townOnly=True,standingHandsAtSides=True)
(PACKAGE/'Calibration/town-idle-import.json').write_text(json.dumps(audit,indent=2)+'\n')
# Review the unequipped pose in the actual textured model.
for obj in scene.objects:
    if obj.type=='MESH' and obj not in body: obj.hide_render=True
data=bpy.data.cameras.new('Town Idle Review'); camera=bpy.data.objects.new('Town Idle Review',data)
scene.collection.objects.link(camera); camera.location=(1.35,-2.7,1.05)
camera.rotation_euler=(Vector((.025,0,.54))-camera.location).to_track_quat('-Z','Y').to_euler()
data.type='ORTHO'; data.ortho_scale=1.35; scene.camera=camera
for loc,power,size in [((1,-2,3),220,3),((-2,-1,1.5),130,2)]:
    data=bpy.data.lights.new('Review Light','AREA'); obj=bpy.data.objects.new('Review Light',data)
    scene.collection.objects.link(obj); obj.location=loc; data.energy=power; data.shape='DISK'; data.size=size
    obj.rotation_euler=(Vector((0,0,.5))-obj.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=700; scene.render.resolution_y=850; scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'arin-town-idle.png')
bpy.ops.render.render(write_still=True)
print('TOWN_IDLE_CANDIDATE',json.dumps(audit),flush=True)
