"""Retain Blender's repeated flower collection as one native model plus placements."""
import hashlib
import math
from collections import defaultdict
import bpy
from static_glb import write


def export(output, depsgraph):
    placements=sorted([o for o in bpy.data.objects if o.instance_collection and
                       o.instance_collection.name=='Detailed Neris Flower Planter'],key=lambda o:o.name)
    assert placements, 'Expected the accepted detailed flower template.'
    collection=placements[0].instance_collection
    groups=defaultdict(list)
    materials={}
    excluded={o.name for o in collection.all_objects if o.type in {'MESH','CURVE','FONT'}}
    for source in collection.all_objects:
        if source.name not in excluded:continue
        obj=source.evaluated_get(depsgraph)
        mesh=obj.to_mesh();mesh.calc_loop_triangles()
        matrix=obj.matrix_world
        normal=matrix.to_3x3().inverted().transposed()
        for triangle in mesh.loop_triangles:
            material=mesh.materials[triangle.material_index]
            materials[material.name]=material
            groups[material.name].append(tuple((tuple(matrix@mesh.vertices[mesh.loops[i].vertex_index].co),
                                                tuple((normal@mesh.corner_normals[i].vector).normalized()))
                                               for i in triangle.loops))
        obj.to_mesh_clear()
    parts=[(name,materials[name],triangles,len({v for t in triangles for v in t}))
           for name,triangles in groups.items()]
    assert len(parts)<=16 and sum(p[3] for p in parts)<=131072
    assert sum(len(p[2]) for p in parts)<=131072
    filename='Flowers.glb'
    write(output/filename,parts)
    result={'file':filename,'parts':len(parts),'triangles':sum(len(p[2]) for p in parts),
            'sha256':hashlib.sha256((output/filename).read_bytes()).hexdigest(),'placements':[]}
    for obj in placements:
        position,rotation,scale=obj.matrix_world.decompose()
        result['placements'].append({'position':[position.x*10,position.z*10+21,position.y*10],
            'scale':[scale.x*1000,scale.z*1000,scale.y*1000],
            'yaw':-rotation.to_euler().z*180/math.pi})
    print('FLOWER INSTANCES',len(placements),'share',len(parts),'parts',flush=True)
    return result,excluded
