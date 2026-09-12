import bpy, bmesh, json, math
from pathlib import Path
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-paint-conditions.blend'))
high=bpy.data.objects['Mira3.HunyuanShape'];high.name='Mira3.High'
low=high.copy();low.data=high.data.copy();low.name='Mira3.Body'
bpy.context.scene.collection.objects.link(low)
bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
bm=bmesh.new();bm.from_mesh(low.data)
before={'vertices':len(bm.verts),'triangles':sum(len(f.verts)-2 for f in bm.faces),'boundaryEdges':sum(e.is_boundary for e in bm.edges),'nonManifoldEdges':sum(not e.is_manifold for e in bm.edges)}
print('ORIGINAL',json.dumps(before),flush=True)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
remaining=set(bm.verts);removed=[];components=[]
while remaining:
    stack=[remaining.pop()];component=[]
    while stack:
        v=stack.pop();component.append(v)
        for edge in v.link_edges:
            other=edge.other_vert(v)
            if other in remaining:remaining.remove(other);stack.append(other)
    extent=max(max(v.co[i] for v in component)-min(v.co[i] for v in component) for i in range(3))
    components.append({'vertices':len(component),'extent':extent})
    if extent<.004:removed.extend(component)
if removed:bmesh.ops.delete(bm,geom=removed,context='VERTS')
closed=all(e.is_manifold for e in bm.edges)
bm.to_mesh(low.data);bm.free()
if not closed:
    modifier=low.modifiers.new('Close Hunyuan Surface','REMESH');modifier.mode='VOXEL';modifier.voxel_size=.0014;modifier.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
triangles=sum(len(p.vertices)-2 for p in low.data.polygons)
group=low.vertex_groups.new(name='Preserve Face And Hands')
for v in low.data.vertices:
    p=v.co;weight=.2 if p.z>.33 else (.3 if abs(p.x)>.32 and p.z>.17 else 1)
    group.add([v.index],weight,'REPLACE')
modifier=low.modifiers.new('Viewer Body Budget','DECIMATE');modifier.ratio=18100/triangles;modifier.vertex_group=group.name;modifier.vertex_group_factor=1
bpy.ops.object.modifier_apply(modifier=modifier.name);low.vertex_groups.clear()
bm=bmesh.new();bm.from_mesh(low.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
after={'vertices':len(bm.verts),'triangles':len(bm.faces),'boundaryEdges':sum(e.is_boundary for e in bm.edges),'nonManifoldEdges':sum(not e.is_manifold for e in bm.edges)}
bm.to_mesh(low.data);bm.free()
for p in low.data.polygons:p.use_smooth=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1519,island_margin=.003,area_weight=.2);bpy.ops.object.mode_set(mode='OBJECT')
high.hide_render=True;high.hide_set(True)
(root/'mesh-topology.json').write_text(json.dumps({'original':before,'low':after,'removedSmallVertices':len(removed),'components':sorted(components,key=lambda c:-c['vertices'])[:15]},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-low-setup.blend'))
print('LOW_READY',json.dumps(after),flush=True)
