"""Transfer the existing Mixamo weights to the closed Mira2 surface in bind pose."""
import bpy,json,math,bmesh
from pathlib import Path
root=Path(r'D:\AI\Mira3D\Mira2');rig=bpy.data.objects['Mira2.Rig'];old=bpy.data.objects['Mira2.SkinnedBody'];scene=bpy.context.scene
for name in ('Mira2.Repair.High','Mira2.Repair.Low'):
    if name in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
with bpy.data.libraries.load(str(root/'mira2-repaired-baked-body.blend'),link=False) as (source,target): target.objects=['Mira2.Repair.Low']
low=target.objects[0];scene.collection.objects.link(low);low.hide_set(False);low.hide_render=False
with bpy.data.libraries.load(str(root/'mira2-idle-staff.blend'),link=False) as (source,target): target.meshes=[old.data.name]
weight_source=old.copy();weight_source.data=target.meshes[0];weight_source.name='Mira2.WeightTransferSource';scene.collection.objects.link(weight_source)
world=old.matrix_world.copy();weight_source.parent=None;weight_source.matrix_world=world;weight_source.modifiers.clear();weight_source.hide_set(False)
rig.data.pose_position='REST';bpy.context.view_layer.update()
for group in old.vertex_groups: low.vertex_groups.new(name=group.name)
bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
modifier=low.modifiers.new('Own Mixamo Bind Weights','DATA_TRANSFER');modifier.object=weight_source;modifier.use_vert_data=True;modifier.data_types_verts={'VGROUP_WEIGHTS'};modifier.vert_mapping='POLYINTERP_NEAREST';modifier.layers_vgroup_select_src='ALL';modifier.layers_vgroup_select_dst='NAME'
bpy.ops.object.modifier_apply(modifier=modifier.name)
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4);bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)
unweighted=sum(not any(g.weight>0 for g in v.groups) for v in low.data.vertices)
if unweighted: raise RuntimeError(f'{unweighted} vertices missing skin weights')
hand=rig.data.bones['mixamorig:RightHand'];group=low.vertex_groups[hand.name].index;local=hand.matrix_local.inverted() @ low.matrix_world;back=local.inverted();curled=0
for v in low.data.vertices:
    if not any(g.group==group and g.weight>.5 for g in v.groups): continue
    p=local @ v.co
    if p.y<=.075: continue
    theta=(p.y-.075)/.035;radius=.035-p.z;p.y=.075+radius*math.sin(theta);p.z=.035-radius*math.cos(theta);v.co=back @ p;curled+=1
low.data.update();world=low.matrix_world.copy();low.parent=rig;low.matrix_world=world
modifier=low.modifiers.new('Mira2 Own Rig','ARMATURE');modifier.object=rig
bpy.data.objects.remove(weight_source,do_unlink=True);bpy.data.objects.remove(old,do_unlink=True);low.name='Mira2.SkinnedBody';low.data.name='Mira2.ClosedBody'
rig.data.pose_position='POSE';rig.hide_set(False);rig.hide_render=False;bpy.data.objects['Mira2.Staff'].hide_set(False);bpy.data.objects['Mira2.Staff'].hide_render=False
scene.frame_set(1);bpy.context.view_layer.update()
result={'bodyTriangles':sum(len(p.vertices)-2 for p in low.data.polygons),'unweightedVertices':unweighted,'curledFingerVertices':curled,'maxInfluences':max(len(v.groups) for v in low.data.vertices)}
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira2-closed-rigged.blend'))
(root/'mira2-weight-transfer-report.json').write_text(json.dumps(result,indent=2))
