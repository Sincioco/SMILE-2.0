"""Put one-hand casts in the free left hand while the right hand keeps the staff."""
import bpy,json
from pathlib import Path
from mathutils import Matrix
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-staff-rigged.blend'))
rig=bpy.data.objects['Mira3.Rig'];scene=bpy.context.scene
if rig.get('Mira3CastingMirrored'):raise RuntimeError('Use the pre-mirror staff checkpoint')
exec(compile((root/'close_mira3_staff.py').read_text(encoding='utf-8'),'close_mira3_staff.py','exec'))
rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1);bpy.context.view_layer.update()
arm_names=['mixamorig:RightShoulder','mixamorig:RightArm','mixamorig:RightForeArm']
arm_basis={name:rig.pose.bones[name].matrix_basis.copy() for name in arm_names}
reflection=Matrix.Diagonal((-1,1,1,1));report=[]
for name in ['Attack','HealOne']:
    action=bpy.data.actions[name];rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0];start,end=map(int,action.frame_range);samples=[]
    for frame in range(start,end+1):
        scene.frame_set(frame);bpy.context.view_layer.update();desired={}
        for bone in rig.pose.bones:
            other_name=bone.name.replace('Left','TEMP').replace('Right','Left').replace('TEMP','Right')
            other=rig.pose.bones[other_name]
            desired[bone.name]=reflection@other.matrix@other.bone.matrix_local.inverted()@reflection@bone.bone.matrix_local
        samples.append((frame,desired))
    for frame,desired in samples:
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            parent={'parent_matrix':desired[bone.parent.name],'parent_matrix_local':bone.parent.bone.matrix_local} if bone.parent else {}
            bone.matrix_basis=bone.bone.convert_local_to_pose(desired[bone.name],bone.bone.matrix_local,invert=True,**parent);bone.rotation_mode='QUATERNION'
            for prop in ['location','rotation_quaternion','scale']:bone.keyframe_insert(data_path=prop,frame=frame)
        for arm_name in arm_names:
            bone=rig.pose.bones[arm_name];bone.matrix_basis=arm_basis[arm_name]
            for prop in ['location','rotation_quaternion','scale']:bone.keyframe_insert(data_path=prop,frame=frame)
        bpy.context.view_layer.update();hand=rig.pose.bones['mixamorig:RightHand']
        hand_pose=Matrix(((0,0,1,0),(0,-1,0,0),(1,0,0,0),(0,0,0,1)));hand_pose.translation=hand.matrix.translation
        basis=hand.bone.convert_local_to_pose(hand_pose,hand.bone.matrix_local,invert=True,parent_matrix=hand.parent.matrix,parent_matrix_local=hand.parent.bone.matrix_local)
        hand.rotation_quaternion=basis.to_quaternion();hand.keyframe_insert(data_path='rotation_quaternion',frame=frame)
    report.append({'clip':name,'frames':end-start+1,'castingHand':'Left','staffArm':'Neutral local pose following the torso; vertical right palm'})
rig['Mira3CastingMirrored']=True;rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1)
(root/'mira3-casting-hand-repair.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-casting-rigged.blend'));print('LEFT_HAND_CASTS_READY',flush=True)
