"""Fit the preserved staff to Mira2's own hand and curl existing finger geometry.
Run once on mira2-idle-staff.blend. No added faces or borrowed body geometry.
"""
import bpy, math, json
from mathutils import Matrix, Vector
from pathlib import Path
rig=bpy.data.objects['Mira2.Rig']; body=bpy.data.objects['Mira2.SkinnedBody']; staff=bpy.data.objects['Mira2.Staff']; scene=bpy.context.scene
scene.frame_set(1); bpy.context.view_layer.update()
hand=rig.pose.bones['mixamorig:RightHand']; rest=hand.bone.matrix_local.copy()
staff_points=[staff.matrix_world @ v.co for v in staff.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]
base=min(staff_points,key=lambda p:p.z).copy()
group=body.vertex_groups[hand.name].index; local=rest.inverted() @ body.matrix_world; back=local.inverted(); changed=0
for v in body.data.vertices:
    if not any(g.group==group and g.weight>.5 for g in v.groups): continue
    p=local @ v.co
    if p.y<=.075: continue
    theta=(p.y-.075)/.035
    radius=.035-p.z
    p.y=.075+radius*math.sin(theta); p.z=.035-radius*math.cos(theta)
    v.co=back @ p; changed+=1
body.data.update()
# Idle keeps the staff vertical. The other clips preserve their authored arm poses.
rotation=Matrix(((0,0,1),(0,-1,0),(1,0,0))).to_4x4()
for frame in range(1,122,4):
    scene.frame_set(frame); bpy.context.view_layer.update()
    desired=rotation.copy(); desired.translation=hand.matrix.translation
    basis=hand.bone.convert_local_to_pose(desired,rest,invert=True,parent_matrix=hand.parent.matrix,parent_matrix_local=hand.parent.bone.matrix_local)
    hand.rotation_quaternion=basis.to_quaternion(); hand.keyframe_insert(data_path='rotation_quaternion',frame=frame)
scene.frame_set(1); bpy.context.view_layer.update()
grip=rig.matrix_world @ hand.matrix @ Vector((0,.075,.035))
translation=Vector((grip.x-base.x,grip.y-base.y,0))
inverse_deform=(rig.matrix_world @ hand.matrix @ rest.inverted()).inverted()
for v,p in zip(staff.data.vertices,staff_points): v.co=staff.matrix_world.inverted() @ inverse_deform @ (p+translation)
staff.data.update(); bpy.context.view_layer.update()
for action in bpy.data.actions: action.use_fake_user=True
bpy.ops.wm.save_as_mainfile(filepath=r'D:\AI\Mira3D\Mira2\mira2-grip-checkpoint.blend')
result={'fingerVerticesCurled':changed,'gripWorld':list(grip),'staffTranslation':list(translation),'equippedTriangles':sum(len(p.vertices)-2 for o in (body,staff) for p in o.data.polygons)}
Path(r'D:\AI\Mira3D\Mira2\mira2-grip-report.json').write_text(json.dumps(result,indent=2))
