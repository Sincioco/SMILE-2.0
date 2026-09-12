"""Assemble Mira2 using absolute Mixamo joint poses, then normalize OBJ/FBX units.
Run in the prepared Mira2 Blender scene containing Mira2.Rig and Mira2.SkinnedBody.
"""
import bpy,json
from pathlib import Path
root=Path(r'D:\AI\Mira3D\Mira2'); rig=bpy.data.objects['Mira2.Rig']; body=bpy.data.objects['Mira2.SkinnedBody'];scene=bpy.context.scene
clips={'Idle':'Standing Idle 02.fbx','Walk':'Standing Walk Forward.fbx','Run':'Standing Run Forward.fbx','Attack':'Standing 1H Magic Attack 01.fbx','Defend':'Standing Block Start.fbx','HealParty':'Standing 2H Cast Spell 01.fbx','Hit':'Standing React Small From Front.fbx','Death':'Standing React Death Backward.fbx'}
report=[]
for name,file in clips.items():
 if name in bpy.data.actions: bpy.data.actions[name].name='DiscardedDirect.'+name
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(root/'ProMagic'/file))
 imported=[o for o in bpy.data.objects if o not in before];source=next(o for o in imported if o.type=='ARMATURE');source_action=source.animation_data.action
 source.hide_set(True);start,end=map(int,source_action.frame_range)
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action;conversion=rig.matrix_world.inverted()@source.matrix_world;maximum_error=0.0
 for frame in range(start,end+1):
  scene.frame_set(frame);bpy.context.view_layer.update();desired={p.name:conversion@p.matrix.copy() for p in source.pose.bones}
  for p in rig.pose.bones:
   kwargs={}
   if p.parent: kwargs={'parent_matrix':desired[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local}
   p.matrix_basis=p.bone.convert_local_to_pose(desired[p.name],p.bone.matrix_local,invert=True,**kwargs);p.rotation_mode='QUATERNION'
   for prop in ('location','rotation_quaternion','scale'): p.keyframe_insert(data_path=prop,frame=frame)
  bpy.context.view_layer.update();maximum_error=max(maximum_error,max((p.matrix.translation-desired[p.name].translation).length for p in rig.pose.bones))
 report.append({'clip':name,'source':file,'frames':end-start+1,'maximumLocalJointError':maximum_error})
 for o in imported:bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.actions.remove(source_action)
rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1)
# OBJ has no units: Mixamo returned meter-valued coordinates labelled centimeters.
# Bring the whole rig back to meters, then bake its residual object transform.
rig.scale*=100;bpy.context.view_layer.update();body_world=body.matrix_world.copy();body.parent=None;body.matrix_world=body_world
scale=rig.scale.x;bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
for action in bpy.data.actions:
 if action.name not in set(clips)|{'HealOne'}: continue
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:
     if curve.data_path.endswith('.location'):
      for k in curve.keyframe_points:k.co.y*=scale;k.handle_left.y*=scale;k.handle_right.y*=scale
      for s in curve.sampled_points:s.co.y*=scale
scene.frame_set(2);scene.frame_set(1);body.parent=rig;body.parent_type='OBJECT';body.matrix_world=body_world;bpy.context.view_layer.update()
for o in list(scene.objects):
 if o not in (body,rig):bpy.data.objects.remove(o,do_unlink=True)
for a in list(bpy.data.actions):
 if a.name not in set(clips)|{'HealOne'}:bpy.data.actions.remove(a)
for a in bpy.data.actions:a.use_fake_user=True
bpy.data.orphans_purge(do_local_ids=True,do_linked_ids=False,do_recursive=True)
rig.hide_set(False);body.hide_set(False);scene.frame_start=1;scene.frame_end=int(bpy.data.actions['Idle'].frame_range[1]);scene.render.engine='BLENDER_EEVEE'
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira2-assembled.blend'))
(root/'mira2-retarget-report.json').write_text(json.dumps({'unitMultiplier':100,'clips':report},indent=2))
result={'retarget':report,'rigScale':list(rig.scale),'bodyHeight':body.dimensions.z}
