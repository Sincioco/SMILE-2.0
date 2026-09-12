"""Bake Mixamo absolute bone poses into Mira's original-pose bind rig.
The no-skin export uses a T-pose rest rig; direct Action assignment is invalid.
A matching With Skin reference confirms equal animated global joint transforms.
"""
import bpy, json
from pathlib import Path
from mathutils import Matrix
root=Path('D:/AI/Mira3D/Mixamo')
rig=bpy.data.objects['Mira.Rig'];scene=bpy.context.scene
clips={'Walk':'ProMagic/Standing Walk Forward.fbx','Run':'ProMagic/Standing Run Forward.fbx','Attack':'ProMagic/Standing 1H Magic Attack 01.fbx','Defend':'ProMagic/Standing Block Start.fbx','HealOne':'Mira-HealOne-Mirrored.fbx','HealParty':'ProMagic/Standing 2H Cast Spell 01.fbx','Hit':'ProMagic/Standing React Small From Front.fbx','Death':'ProMagic/Standing React Death Backward.fbx'}
report=[]
for name,file in clips.items():
    if name in bpy.data.actions:bpy.data.actions[name].name='RejectedDirectAssignment.'+name
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(root/file))
    imported=[o for o in bpy.data.objects if o not in before]
    source=next(o for o in imported if o.type=='ARMATURE')
    source.hide_set(True)
    source_action=source.animation_data.action
    start,end=map(int,source_action.frame_range)
    action=bpy.data.actions.new(name);action.use_fake_user=True
    rig.animation_data.action=action
    conversion=rig.matrix_world.inverted()@source.matrix_world
    maximum_error=0.0
    for frame in range(start,end+1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        desired={p.name:conversion@p.matrix.copy() for p in source.pose.bones}
        for p in rig.pose.bones:
            kwargs={}
            if p.parent:
                kwargs={'parent_matrix':desired[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local}
            p.matrix_basis=p.bone.convert_local_to_pose(desired[p.name],p.bone.matrix_local,invert=True,**kwargs)
            p.rotation_mode='QUATERNION'
            for prop in ('location','rotation_quaternion','scale'):
                p.keyframe_insert(data_path=prop,frame=frame)
        bpy.context.view_layer.update()
        maximum_error=max(maximum_error,max((p.matrix.translation-desired[p.name].translation).length for p in rig.pose.bones)*.01)
    report.append({'name':name,'file':file,'frames':end-start+1,'maxWorldJointErrorMeters':maximum_error})
    for o in imported:bpy.data.objects.remove(o,do_unlink=True)
    print(name,'retargeted',end-start+1,'frames; max joint error',maximum_error)
rig.animation_data.action=bpy.data.actions['HealOne'];rig.animation_data.action_slot=bpy.data.actions['HealOne'].slots[0]
scene.frame_set(21)
for o in scene.objects:
    o.hide_set(o.name not in ['Mira.Rig','Mira.SkinnedBody','Mira.Staff'])
bpy.ops.wm.save_as_mainfile(filepath='D:/AI/Mira3D/mira-retargeted-animation-set.blend')
Path('D:/AI/Mira3D/mira-retarget-report.json').write_text(json.dumps(report,indent=2))
result={'retargeting':report}
