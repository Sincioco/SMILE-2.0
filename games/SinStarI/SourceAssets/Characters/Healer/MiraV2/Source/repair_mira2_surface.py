"""Close generated surface before low-poly reduction; retain original textured source."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
root=Path(r'D:\AI\Mira3D\Mira2')
for name in ('Mira2.Repair.High','Mira2.Repair.Low'):
    if name in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
with bpy.data.libraries.load(str(root/'mira2-bake-setup.blend'),link=False) as (source,target):
    target.objects=['Mira2.High.Source']
high=target.objects[0];bpy.context.scene.collection.objects.link(high);high.name='Mira2.Repair.High'
high.hide_set(False);high.hide_render=False
low=high.copy();low.data=high.data.copy();low.name='Mira2.Repair.Low';bpy.context.scene.collection.objects.link(low)
bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
mesh=bmesh.new();mesh.from_mesh(low.data);bmesh.ops.remove_doubles(mesh,verts=list(mesh.verts),dist=.000001);mesh.to_mesh(low.data);mesh.free()
modifier=low.modifiers.new('Give Thin Cloth A Closed Volume','SOLIDIFY');modifier.thickness=.006;modifier.offset=0;modifier.use_rim=True
bpy.ops.object.modifier_apply(modifier=modifier.name)
modifier=low.modifiers.new('Close Surface Before Reduction','REMESH');modifier.mode='VOXEL';modifier.voxel_size=.0025;modifier.use_smooth_shade=True
bpy.ops.object.modifier_apply(modifier=modifier.name)
mesh=bmesh.new();mesh.from_mesh(low.data);remaining=set(mesh.verts);remove=[]
while remaining:
    stack=[remaining.pop()];component=[]
    while stack:
        v=stack.pop();component.append(v)
        for edge in v.link_edges:
            other=edge.other_vert(v)
            if other in remaining: remaining.remove(other);stack.append(other)
    if max(max(v.co[i] for v in component)-min(v.co[i] for v in component) for i in range(3))<.008: remove.extend(component)
if remove: bmesh.ops.delete(mesh,geom=remove,context='VERTS')
mesh.to_mesh(low.data);mesh.free()
triangles=sum(len(p.vertices)-2 for p in low.data.polygons)
group=low.vertex_groups.new(name='Reduction Weights')
for v in low.data.vertices:
    p=low.matrix_world @ v.co;weight=.35 if p.z>1.46 else (.3 if abs(p.x)>.55 and p.z>1.1 else 1)
    group.add([v.index],weight,'REPLACE')
modifier=low.modifiers.new('Game Budget','DECIMATE');modifier.ratio=18100/triangles;modifier.vertex_group=group.name;modifier.vertex_group_factor=1
bpy.ops.object.modifier_apply(modifier=modifier.name)
low.vertex_groups.clear()
mesh=bmesh.new();mesh.from_mesh(low.data);bmesh.ops.recalc_face_normals(mesh,faces=list(mesh.faces));topology={'vertices':len(mesh.verts),'boundaryEdges':sum(e.is_boundary for e in mesh.edges),'nonManifoldEdges':sum(not e.is_manifold for e in mesh.edges),'triangles':sum(len(f.verts)-2 for f in mesh.faces)};mesh.to_mesh(low.data);mesh.free()
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1519,island_margin=.003,area_weight=.2);bpy.ops.object.mode_set(mode='OBJECT')
material=bpy.data.materials.new('Mira2.RepairedBaked');material.use_nodes=True;low.data.materials.clear();low.data.materials.append(material)
image=bpy.data.images.new('Mira2.Repaired.BaseColor',width=4096,height=4096,alpha=False)
target=material.node_tree.nodes.new('ShaderNodeTexImage');target.name='BakeTarget';target.image=image;material.node_tree.nodes.active=target
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.025;scene.render.bake.max_ray_distance=.05;scene.render.bake.margin=12
for obj in scene.objects:
    obj.hide_set(obj not in (low,high));obj.hide_render=obj not in (low,high)
high.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira2-repair-bake-setup.blend'))
(root/'mira2-repair-topology.json').write_text(json.dumps(topology,indent=2))
result=topology
