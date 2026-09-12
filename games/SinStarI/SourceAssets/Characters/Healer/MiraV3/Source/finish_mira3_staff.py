"""Fit the reusable Mira staff design to Mira3's own hand and own skin."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-skinned.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['Mira3.Rig'];body=bpy.data.objects['Mira3.SkinnedBody']
old_idle=rig.animation_data.action;old_idle.name='Discarded Combat Idle'
motion=json.loads((root/'mira3-neutral-idle-rotations.json').read_text(encoding='utf-8'))['frames']
bpy.data.actions.remove(old_idle)
staff_material=bpy.data.materials.new('Mira3.Staff.Combined');staff_material.use_nodes=True
staff_shader=staff_material.node_tree.nodes.get('Principled BSDF');staff_shader.inputs['Metallic'].default_value=.5;staff_shader.inputs['Roughness'].default_value=.34
palette=staff_material.node_tree.nodes.new('ShaderNodeTexImage');palette.image=bpy.data.images.load(str(root/'mira3-staff-palette.png'));palette.image.pack()
staff_material.node_tree.links.new(palette.outputs['Color'],staff_shader.inputs['Base Color'])
action=bpy.data.actions.new('Idle');action.use_fake_user=True;rig.animation_data.action=action
for frame,rotations in enumerate(motion,1):
    scene.frame_set(frame);desired={}
    for p in rig.pose.bones:
        matrix=Quaternion(rotations[p.name]).to_matrix().to_4x4()
        matrix.translation=(desired[p.parent.name]@p.parent.bone.matrix_local.inverted()@p.bone.matrix_local.translation) if p.parent else p.bone.matrix_local.translation
        desired[p.name]=matrix
        parent={'parent_matrix':desired[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
        p.matrix_basis=p.bone.convert_local_to_pose(matrix,p.bone.matrix_local,invert=True,**parent);p.rotation_mode='QUATERNION'
        for prop in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=prop,frame=frame)
hand=rig.pose.bones['mixamorig:RightHand'];rest=hand.bone.matrix_local
local=rest.inverted()@rig.matrix_world.inverted()@body.matrix_world;back=local.inverted();group=body.vertex_groups[hand.name].index;curled=0
for v in body.data.vertices:
    if not any(g.group==group and g.weight>.5 for g in v.groups):continue
    p=local@v.co
    if p.y<=.073:continue
    theta=(p.y-.073)/.035;radius=.035-p.z;p.y=.073+radius*math.sin(theta);p.z=.035-radius*math.cos(theta);v.co=back@p;curled+=1
body.data.update()
# A consistent palm orientation keeps the staff vertical through the neutral loop.
rotation=Matrix(((0,0,1),(0,-1,0),(1,0,0))).to_4x4()
for frame in range(1,122):
    scene.frame_set(frame);bpy.context.view_layer.update();desired=rotation.copy();desired.translation=hand.matrix.translation
    basis=hand.bone.convert_local_to_pose(desired,rest,invert=True,parent_matrix=hand.parent.matrix,parent_matrix_local=hand.parent.bone.matrix_local)
    hand.rotation_quaternion=basis.to_quaternion();hand.keyframe_insert(data_path='rotation_quaternion',frame=frame)
scene.frame_set(1);bpy.context.view_layer.update()
placeholder=bpy.data.meshes.new('Staff Palette Holder');placeholder.materials.append(staff_material)
old=bpy.data.objects.new('Mira3.Staff',placeholder);scene.collection.objects.link(old)
staff_code=(root/'close_mira3_staff.py').read_text(encoding='utf-8')
exec(compile(staff_code,'close_mira3_staff.py','exec'))
(root/'mira3-grip-report.json').write_text(json.dumps({'curledVertices':curled,'gripBone':'mixamorig:RightHand','gripLocal':[0,.073,.035],'curlRadius':.035,'equippedTriangles':19652},indent=2),encoding='utf-8')
for a in bpy.data.actions:a.use_fake_user=True
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-staff-rigged.blend'));print('STAFF_READY',curled,flush=True)
