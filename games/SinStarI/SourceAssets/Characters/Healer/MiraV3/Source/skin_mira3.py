import bpy,json
from pathlib import Path
from mathutils.kdtree import KDTree
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-assembled.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['Mira3.Rig'];source=bpy.data.objects['Mira3.SkinnedBody']
rig.data.pose_position='REST';bpy.context.view_layer.update()
with bpy.data.libraries.load(str(root/'mira3-unrigged-baked.blend'),link=False) as (src,dst):dst.objects=['Mira3.Body']
body=dst.objects[0];scene.collection.objects.link(body);body.hide_set(False);body.hide_render=False
tree=KDTree(len(source.data.vertices))
for v in source.data.vertices:tree.insert(source.matrix_world@v.co,v.index)
tree.balance()
for group in source.vertex_groups:body.vertex_groups.new(name=group.name)
maximum=0;unweighted=0
for v in body.data.vertices:
    point,index,distance=tree.find(body.matrix_world@v.co);maximum=max(maximum,distance)
    weights=sorted([(g.group,g.weight) for g in source.data.vertices[index].groups if g.weight>0],key=lambda g:-g[1])[:4]
    total=sum(w for _,w in weights)
    if total==0:unweighted+=1
    for group,weight in weights:body.vertex_groups[group].add([v.index],weight/total,'REPLACE')
if maximum>.002 or unweighted:raise RuntimeError(f'Own-rig alignment failed: {maximum} m, {unweighted} unweighted')
world=body.matrix_world.copy();body.parent=rig;body.matrix_world=world
modifier=body.modifiers.new('Mira3 Own Mixamo Rig','ARMATURE');modifier.object=rig
bpy.data.objects.remove(source,do_unlink=True);body.name='Mira3.SkinnedBody'
hand=rig.data.bones['mixamorig:RightHand'];local=hand.matrix_local.inverted()@rig.matrix_world.inverted()@body.matrix_world
handgroup=body.vertex_groups[hand.name].index
points=[local@v.co for v in body.data.vertices if any(g.group==handgroup and g.weight>.5 for g in v.groups)]
report={'maximumWeightTransferDistanceMeters':maximum,'unweightedVertices':unweighted,'maxInfluences':max(len(v.groups) for v in body.data.vertices),'handBounds':[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)],'handRestMatrix':[list(r) for r in hand.matrix_local]}
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1);bpy.context.view_layer.update()
(root/'mira3-skin-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-skinned.blend'));print('SKIN_READY',json.dumps(report),flush=True)
