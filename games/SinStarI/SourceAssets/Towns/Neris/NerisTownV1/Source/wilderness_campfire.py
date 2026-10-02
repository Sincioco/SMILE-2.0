"""Reusable stone-ring campfire, shared by native catalog and Blender export.

Standard-library geometry. Append template 39 without changing existing IDs or
the document fingerprint. Dimensions are metres, Y up; four bounded mesh parts.
"""
import hashlib
import json
import math
from pathlib import Path
import struct

COLORS = ((.22,.23,.23), (.13,.055,.018), (1,.18,.012), (1,.72,.12))
ROOT = Path(__file__).resolve().parents[1]


def geometry():
    groups = [[], [], [], []]

    def shape(band, center, scale, flame=False):
        rings, sides = 8, 12
        vertices=[]
        for row in range(rings+1):
            t=row/rings
            radius=math.sin(math.pi*t) if not flame else math.sin(math.pi*t)**.7*(1-t)*1.6
            for col in range(sides):
                angle=col*math.tau/sides
                vertices.append((center[0]+scale[0]*radius*math.cos(angle)+(.2*t*t if flame else 0),
                    center[1]+scale[1]*t, center[2]+scale[2]*radius*math.sin(angle)))
        for row in range(rings):
            for col in range(sides):
                a=row*sides+col; b=row*sides+(col+1)%sides
                for ids in ((a,a+sides,b),(b,a+sides,b+sides)):
                    points=[vertices[i] for i in ids]
                    u=[points[1][i]-points[0][i] for i in range(3)]
                    v=[points[2][i]-points[0][i] for i in range(3)]
                    n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
                    length=math.sqrt(sum(k*k for k in n))
                    if length > .000001:
                        groups[band].append((points,[k/length for k in n]))

    for i in range(9):
        angle=i*math.tau/9
        shape(0,(.8*math.cos(angle),0,.8*math.sin(angle)),(.28,.3,.23))
    shape(1,(0,.12,-.18),(.76,.28,.16))
    shape(1,(.08,.22,0),(.18,.25,.72))
    for x,z,h in ((-.25,0,1.05),(.18,.14,1.45),(.18,-.24,.9)):
        shape(2,(x,.3,z),(.32,h,.28),True)
        shape(3,(x,.32,z),(.18,h*.68,.15),True)
    return groups


def build():
    folder=ROOT/'Authoring'
    catalog=json.loads((folder/'catalog.json').read_text(encoding='utf-8'))
    assert len(catalog['templates']) in (39,40) and len(catalog['chunks']) in (37,38)
    binary=bytearray()
    gltf=dict(asset=dict(version='2.0',generator='SMILE Wilderness Campfire'),scene=0,
        scenes=[dict(nodes=list(range(4)))],nodes=[],meshes=[],materials=[],accessors=[],bufferViews=[])

    def accessor(values,width,component=5126):
        raw=struct.pack('<'+('f' if component==5126 else 'I')*len(values),*values)
        view=len(gltf['bufferViews'])
        gltf['bufferViews'].append(dict(buffer=0,byteOffset=len(binary),byteLength=len(raw)))
        binary.extend(raw)
        index=len(gltf['accessors'])
        gltf['accessors'].append(dict(bufferView=view,componentType=component,
            count=len(values)//width,type={1:'SCALAR',2:'VEC2',3:'VEC3'}[width]))
        return index

    for band,triangles in enumerate(geometry()):
        positions=[v for points,_ in triangles for p in points for v in p]
        normals=[v for _,n in triangles for _ in range(3) for v in n]
        uvs=[v for points,_ in triangles for p in points for v in (p[0],p[2])]
        p=accessor(positions,3)
        gltf['accessors'][p].update(min=[min(positions[i::3]) for i in range(3)],
                                  max=[max(positions[i::3]) for i in range(3)])
        material=dict(name='Campfire '+str(band),doubleSided=False,
            pbrMetallicRoughness=dict(baseColorFactor=list(COLORS[band])+[1],metallicFactor=0,roughnessFactor=.95))
        if band>=2:
            material['emissiveFactor']=list(COLORS[band])
        gltf['materials'].append(material)
        gltf['meshes'].append(dict(name='Campfire '+str(band),primitives=[dict(mode=4,material=band,
            attributes=dict(POSITION=p,NORMAL=accessor(normals,3),TEXCOORD_0=accessor(uvs,2)),
            indices=accessor(list(range(len(positions)//3)),1,5125))]))
        gltf['nodes'].append(dict(name='Campfire '+str(band),mesh=band))
    gltf['buffers']=[dict(byteLength=len(binary))]
    encoded=json.dumps(gltf,separators=(',',':')).encode()
    encoded+=b' '*(-len(encoded)%4)
    raw=struct.pack('<III',0x46546c67,2,28+len(encoded)+len(binary))
    raw+=struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary
    (folder/'Catalog-37.glb').write_bytes(raw)
    catalog['chunks']=catalog['chunks'][:37]+[dict(file='Catalog-37.glb',parts=4,sha256=hashlib.sha256(raw).hexdigest())]
    catalog['templates']=catalog['templates'][:39]+[dict(id=39,label='Campfire',source='wilderness:campfire',
        category='decoration',bounds=[[-1.1,-1.1,0],[1.1,1.1,1.85]],parts=[[37,i] for i in range(4)],
        solids=[[-.95,-.95,.95,.95,.35]],camera=[],steps=[],floors=[],doors=[],water=[])]
    (folder/'catalog.json').write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')


if __name__=='__main__':
    build()
