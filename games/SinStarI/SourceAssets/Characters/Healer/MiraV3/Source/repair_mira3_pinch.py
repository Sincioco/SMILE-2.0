import bpy,bmesh,json
from pathlib import Path
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-low-setup.blend'))
obj=bpy.data.objects['Mira3.Body'];bm=bmesh.new();bm.from_mesh(obj.data)
bad=[e for e in bm.edges if not e.is_manifold]
print('BAD_EDGES',[(len(e.link_faces),[list(v.co) for v in e.verts]) for e in bad],flush=True)
bm.verts.ensure_lookup_table();bm.faces.ensure_lookup_table()
coords=[tuple(v.co) for v in bm.verts];replacement={}
for v in set(v for edge in bad for v in edge.verts):
    todo=set(v.link_faces);fans=[]
    while todo:
        todo2=[todo.pop()];fan=[]
        while todo2:
            face=todo2.pop();fan.append(face)
            for e in face.edges:
                if v in e.verts and e.is_manifold:
                    for adjacent in e.link_faces:
                        if adjacent in todo:todo.remove(adjacent);todo2.append(adjacent)
        fans.append(fan)
    print('VERTEX_FANS',v.index,[len(f) for f in fans],flush=True)
    for fan in fans[1:]:
        new_index=len(coords);coords.append(tuple(v.co))
        for f in fan:replacement[(v.index,f.index)]=new_index
faces=[[replacement.get((v.index,f.index),v.index) for v in f.verts] for f in bm.faces]
bm.free();new=bpy.data.meshes.new('Mira3.Closed.Body');new.from_pydata(coords,[],faces);new.update();obj.data=new
bm=bmesh.new();bm.from_mesh(new)
after={'vertices':len(bm.verts),'triangles':len(bm.faces),'boundaryEdges':sum(e.is_boundary for e in bm.edges),'nonManifoldEdges':sum(not e.is_manifold for e in bm.edges)}
bm.free();print('REPAIRED',after,flush=True)
if after['nonManifoldEdges']:raise RuntimeError('Body must be closed before baking')
for p in new.polygons:p.use_smooth=True
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1519,island_margin=.003,area_weight=.2);bpy.ops.object.mode_set(mode='OBJECT')
report=json.loads((root/'mesh-topology.json').read_text(encoding='utf-8'));report['low']=after;report['pinchedEdgeRepair']='Separate closed face fans sharing a single voxel edge.'
(root/'mesh-topology.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-low-setup.blend'))
