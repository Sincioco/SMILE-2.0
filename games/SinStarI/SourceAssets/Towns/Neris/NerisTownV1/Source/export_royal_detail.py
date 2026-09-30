"""Append replacement Royal Castle parts without changing saved template identities."""
import hashlib
import json
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Source'))
from royal_castle_detail import apply, FINE_MASONRY
from export_catalog import material_key, split_parts
from static_glb import write
from catalog_native import generate

FLOAT3 = struct.Struct('<3f')


def collect(objects, inverse):
    groups = {}
    graph = bpy.context.evaluated_depsgraph_get()
    for source in objects:
        if source.type not in {'MESH','CURVE','FONT','SURFACE'}:
            continue
        obj = source.evaluated_get(graph)
        matrix = inverse @ obj.matrix_world
        mesh = obj.to_mesh()
        if not mesh:
            continue
        mesh.calc_loop_triangles()
        normal_matrix = matrix.to_3x3().inverted().transposed()
        points = [FLOAT3.unpack(FLOAT3.pack(*(matrix@v.co))) for v in mesh.vertices]
        normals = [FLOAT3.unpack(FLOAT3.pack(*(normal_matrix@n.vector).normalized())) for n in mesh.corner_normals]
        for triangle in mesh.loop_triangles:
            mat = mesh.materials[triangle.material_index]
            corners = tuple((points[mesh.loops[i].vertex_index],normals[i]) for i in triangle.loops)
            a,b,c = [Vector(v[0]) for v in corners]
            if (b-a).cross(c-a).length_squared > 1e-12:
                groups.setdefault(material_key(mat),[mat,[]])[1].append(corners)
        obj.to_mesh_clear()
    for mat, triangles in groups.values():
        unique = {}
        for tri in triangles:
            unique.setdefault(min(tri,tri[1:]+tri[:1],tri[2:]+tri[:2]),tri)
        triangles[:] = unique.values()
    return split_parts(groups)


def run():
    path=ROOT/'Authoring/catalog.json'
    catalog=json.loads(path.read_text())
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/catalog['blend_source']))
    castle=apply()
    assert not any(o.name.startswith(FINE_MASONRY) for o in castle.children_recursive)
    assert any(o.name.startswith('Fortress Carved Course') for o in castle.children_recursive)
    assert any(o.name.startswith('Royal Portal Archivolt') for o in castle.children_recursive)
    assert any(o.name.startswith('Royal Cypress Leaf Sprays') for o in castle.children_recursive)
    parts=collect([o for o in castle.children_recursive if not o.get('neris_door_leaf')],castle.matrix_world.inverted())
    assert len(parts)<=32, len(parts)
    batches=[]
    for part in sorted(parts,key=lambda p:p[3],reverse=True):
        batch=next((b for b in batches if len(b)<16 and sum(p[3] for p in b)+part[3]<=125000
                    and sum(len(p[2]) for p in b)+len(part[2])<=130000),None)
        if batch is None:
            batch=[]
            batches.append(batch)
        batch.append(part)
    assert len(batches)+30+14<=64, (len(batches),sum(p[3] for p in parts))
    catalog['chunks']=catalog['chunks'][:30]
    template=catalog['templates'][13]
    assert template['label']=='Royal Castle'
    template['parts']=[]
    for index,batch in enumerate(batches,30):
        filename=f'Templates/Catalog-{index:02}.glb'
        write(ROOT/'Authoring'/filename,batch)
        catalog['chunks'].append({'file':filename,'parts':len(batch),'triangles':sum(len(p[2]) for p in batch),
            'sha256':hashlib.sha256((ROOT/'Authoring'/filename).read_bytes()).hexdigest()})
        template['parts'].extend([index,p] for p in range(len(batch)))
    marker=next(o for o in castle.children if o.get('neris_entrance'))
    normalized=Matrix.Diagonal((1/marker['width'],1/.7,1/marker['height'],1)) @ marker.matrix_world.inverted()
    door=[]
    for side in (-1,1):
        side_parts=collect([o for o in marker.children if o.get('neris_leaf_side')==side],normalized)
        assert len(side_parts)==2,len(side_parts)
        door.extend(sorted(side_parts,key=lambda p: 'Gold' in p[0]))
    write(ROOT/'Runtime/Royal-Door-Leaves.glb',door)
    catalog['royal_detail_revision']=3
    path.write_text(json.dumps(catalog,indent=2)+'\n')
    generate(catalog)
    print('ROYAL EXPORT',len(parts),'parts,',len(batches),'chunks,',sum(p[3] for p in parts),'vertices',flush=True)


if __name__=='__main__':
    run()
