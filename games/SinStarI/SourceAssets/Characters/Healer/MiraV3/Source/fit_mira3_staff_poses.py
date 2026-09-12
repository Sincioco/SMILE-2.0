"""Bake a supported two-bone hold so the long staff stays clear of the floor."""
import bpy,json
from pathlib import Path
from mathutils import Matrix,Vector
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-casting-rigged.blend'))
rig=bpy.data.objects['Mira3.Rig'];scene=bpy.context.scene
rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1);bpy.context.view_layer.update()
minimum_hand=rig.pose.bones['mixamorig:RightHand'].matrix.translation.z-.04
vertical=Matrix(((0,0,1,0),(0,-1,0,0),(1,0,0,0),(0,0,0,1)))
target=bpy.data.objects.new('Temporary Staff Hold Target',None);pole=bpy.data.objects.new('Temporary Staff Hold Pole',None)
scene.collection.objects.link(target);scene.collection.objects.link(pole)
report=[]
for name in ['Attack','HealOne','HealParty','Walk','Run','Defend','Hit']:
    action=bpy.data.actions[name];rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0]
    forearm=rig.pose.bones['mixamorig:RightForeArm'];constraint=forearm.constraints.new('IK');constraint.target=target;constraint.pole_target=pole;constraint.chain_count=2;constraint.use_stretch=False
    poses=[];start,end=map(int,action.frame_range)
    for frame in range(start,end+1):
        scene.frame_set(frame);bpy.context.view_layer.update();shoulder=rig.pose.bones['mixamorig:RightArm'].head.copy()
        target.location=rig.matrix_world@Vector((shoulder.x-.16,shoulder.y-.10,max(minimum_hand,shoulder.z-.30)))
        pole.location=rig.matrix_world@Vector((shoulder.x-.42,shoulder.y+.25,shoulder.z-.18));bpy.context.view_layer.update()
        pose={bone:rig.pose.bones[bone].matrix.copy() for bone in ['mixamorig:RightArm','mixamorig:RightForeArm','mixamorig:RightHand']}
        hand=vertical.copy();hand.translation=pose['mixamorig:RightHand'].translation;pose['mixamorig:RightHand']=hand;poses.append((frame,pose))
    forearm.constraints.remove(constraint)
    for frame,pose in poses:
        scene.frame_set(frame)
        for bone_name in ['mixamorig:RightArm','mixamorig:RightForeArm','mixamorig:RightHand']:
            bone=rig.pose.bones[bone_name];parent=pose.get(bone.parent.name,bone.parent.matrix)
            bone.matrix_basis=bone.bone.convert_local_to_pose(pose[bone_name],bone.bone.matrix_local,invert=True,parent_matrix=parent,parent_matrix_local=bone.parent.bone.matrix_local)
            for prop in ['location','rotation_quaternion','scale']:bone.keyframe_insert(data_path=prop,frame=frame)
    report.append({'clip':name,'frames':len(poses),'staffHold':'Baked two-bone right arm, vertical palm, floor clearance'})
for obj in [target,pole]:bpy.data.objects.remove(obj,do_unlink=True)
# Lower the fallen staff into the arena plane as the death animation settles.
action=bpy.data.actions['Death'];rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0];start,end=map(int,action.frame_range)
horizontal=Matrix(((1,0,0),(0,0,-1),(0,1,0))).to_quaternion();poses=[]
for frame in range(start,end+1):
    scene.frame_set(frame);bpy.context.view_layer.update();bone=rig.pose.bones['mixamorig:RightHand'];matrix=bone.matrix.copy();t=min(1,max(0,(frame-(end-55))/30));t=t*t*(3-2*t)
    rotation=matrix.to_quaternion().slerp(horizontal,t).to_matrix().to_4x4();rotation.translation=matrix.translation;poses.append((frame,rotation))
for frame,matrix in poses:
    scene.frame_set(frame);bone=rig.pose.bones['mixamorig:RightHand'];basis=bone.bone.convert_local_to_pose(matrix,bone.bone.matrix_local,invert=True,parent_matrix=bone.parent.matrix,parent_matrix_local=bone.parent.bone.matrix_local)
    bone.rotation_quaternion=basis.to_quaternion();bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
report.append({'clip':'Death','staffHold':'Blend to a horizontal staff as the pose settles'})
(root/'mira3-staff-animation-repair.json').write_text(json.dumps({'minimumHandHeight':minimum_hand,'clips':report},indent=2),encoding='utf-8')
rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-equipped-poses.blend'));print('STAFF_POSES_READY',flush=True)
