"""Reuse the existing Studio tree geometry/materials in movable city assemblies."""
from functools import lru_cache
import json
import math
from pathlib import Path
import bpy


@lru_cache(maxsize=2)
def prototype(variant):
    name='Neris Detailed Tree '+str(1+variant%2)
    collection=bpy.data.collections.get(name)
    if collection is None:
        base=Path(__file__).resolve().parents[2]/'NerisTownV1'
        catalog=json.loads((base/'Authoring/catalog.json').read_text())
        with bpy.data.libraries.load(str(base/catalog['blend_source']),link=False) as (source,target):
            if name not in source.collections:
                raise ValueError('The canonical Studio tree is missing: '+name)
            target.collections=[name]
        collection=target.collections[0]
    collection.use_fake_user=True
    result=[]
    for obj in collection.all_objects:
        if obj.type!='MESH':
            continue
        for face in obj.data.polygons:
            points=[tuple(obj.matrix_world @ obj.data.vertices[i].co) for i in face.vertices]
            result.append((points,obj.data.materials[face.material_index]))
    return result


def append(mesh,x,y,z,scale=1.8,variant=0,angle=0):
    ca,sa=math.cos(angle),math.sin(angle)
    for points,material in prototype(variant):
        mesh.polygon([(x+scale*(a*ca-b*sa),y+scale*(a*sa+b*ca),z+scale*c)
                      for a,b,c in points],material)
