"""Append reusable, deterministic landforms without changing accepted catalog IDs.

Standard-library GLB geometry; no downloaded art or authoring dependency.
The catalog's document fingerprint intentionally remains stable for old town saves.
"""
import json
import math
import struct
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AUTHORING = ROOT / 'Authoring'


def mountain():
    """An asymmetric eroded ridge with subsidiary peaks, within the catalog bounds."""
    size = 52
    radius, height = 25, 48
    vertices = []
    for row in range(size + 1):
        z = -1 + row * 2 / size
        for col in range(size + 1):
            x = -1 + col * 2 / size
            peaks = []
            for px,pz,sx,sz,gain in ((.18,-.12,.62,.72,1),(-.27,.18,.55,.52,.72),
                                      (.35,.36,.48,.42,.58),(-.38,-.43,.45,.38,.52)):
                d = math.hypot((x-px)/sx,(z-pz)/sz)
                peaks.append(max(0,1-d)**.75*gain)
            ridge = max(peaks)
            erosion = (.08*math.sin(x*21+z*8) + .045*math.sin(x*39-z*27)
                       + .025*math.sin(x*81+z*47))
            rim = max(0,min(1,(1-max(abs(x),abs(z)))*5))
            y = height * max(0,min(1,ridge+erosion*ridge**.35))*rim
            vertices.append((radius*x,y,radius*z))
    groups = [[],[],[]]
    for row in range(size):
        for col in range(size):
            a = row*(size+1)+col
            for ids in ((a,a+size+1,a+1),(a+1,a+size+1,a+size+2)):
                points = [vertices[i] for i in ids]
                if max(p[1] for p in points) < .03:
                    continue
                u = [points[1][i]-points[0][i] for i in range(3)]
                v = [points[2][i]-points[0][i] for i in range(3)]
                n = (u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
                length = math.sqrt(sum(k*k for k in n))
                band = min(2,int(sum(p[1] for p in points)/3/height*3))
                groups[band].append((points,[k/length for k in n]))
    return groups,radius,height


def geometry(form):
    """Layered radial formations, in glTF metres with Y up."""
    if form == 0:
        return mountain()
    rings, sectors = 18, 72
    height = (48, 32, 12, 65)[form]
    radius = (25, 24, 35, 10)[form]
    vertices = []
    for row in range(rings + 1):
        t = row / rings
        if form == 0:
            width = (1-t)**1.05
        elif form == 1:
            width = .45 + .55*(1-t)**5 if t < .9 else .45*(1-t)/.1
        elif form == 2:
            width = math.sqrt(max(0, 1-t*t))
        else:
            width = .43 + .57*(1-t)**7 if t < .84 else .43*(1-t)/.16
        for col in range(sectors):
            a = col * math.tau / sectors
            rough = 1 + (.13*math.sin(a*5+.4)+.065*math.sin(a*9-1.2))*(1-t*.65)
            x = radius * width * math.cos(a) * rough
            z = radius * width * math.sin(a) * rough
            if form == 2:
                x *= 1.5
                z *= .7
            y = height*t + .8*math.sin(a*3+t*4)*math.sin(math.pi*t)
            vertices.append((x, y, z))
    groups = [[], [], []]
    for row in range(rings):
        band = min(2, row*3//rings)
        for col in range(sectors):
            a = row*sectors+col
            b = row*sectors+(col+1)%sectors
            c, d = a+sectors, b+sectors
            for ids in ((a,c,b),(b,c,d)):
                points = [vertices[i] for i in ids]
                u = [points[1][i]-points[0][i] for i in range(3)]
                v = [points[2][i]-points[0][i] for i in range(3)]
                n = (u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
                length = math.sqrt(sum(k*k for k in n))
                if length < .000001:
                    continue
                groups[band].append((points, [k/length for k in n]))
    return groups, radius, height


def build():
    catalog = json.loads((AUTHORING/'catalog.json').read_text(encoding='utf-8'))
    assert len(catalog['templates']) in (35,39)
    assert len(catalog['chunks']) in (36,37)
    catalog['templates'] = catalog['templates'][:35]
    catalog['chunks'] = catalog['chunks'][:36]
    binary = bytearray()
    gltf = dict(asset=dict(version='2.0',generator='SMILE Luma Landforms'),scene=0,
                scenes=[dict(nodes=list(range(12)))],nodes=[],meshes=[],materials=[],
                accessors=[],bufferViews=[],buffers=[])
    def accessor(values, width, component=5126):
        blob = struct.pack('<'+('f' if component==5126 else 'I')*len(values),*values)
        view = len(gltf['bufferViews'])
        gltf['bufferViews'].append(dict(buffer=0,byteOffset=len(binary),byteLength=len(blob)))
        binary.extend(blob)
        index = len(gltf['accessors'])
        gltf['accessors'].append(dict(bufferView=view,componentType=component,
            count=len(values)//width,type={1:'SCALAR',2:'VEC2',3:'VEC3'}[width]))
        return index
    labels = ('Highland Peak','Sandstone Mesa','Wind Dune','Ancient Rock Spire')
    colors = ((.15,.18,.20),(.40,.22,.10),(.54,.34,.13),(.18,.22,.23))
    for form, label in enumerate(labels):
        groups,radius,height = geometry(form)
        for band,triangles in enumerate(groups):
            positions,normals,uvs = [],[],[]
            for points,n in triangles:
                for point in points:
                    positions.extend(point);normals.extend(n);uvs.extend((point[0]/10,point[2]/10))
            p = accessor(positions,3)
            gltf['accessors'][p].update(min=[min(positions[i::3]) for i in range(3)],
                                      max=[max(positions[i::3]) for i in range(3)])
            index = len(gltf['meshes'])
            rgb = [min(.95,c*(.88+.16*band)) for c in colors[form]]
            gltf['materials'].append(dict(name=label+' '+str(band),doubleSided=False,
                pbrMetallicRoughness=dict(baseColorFactor=rgb+[1],metallicFactor=0,roughnessFactor=.96)))
            gltf['meshes'].append(dict(name=label+' '+str(band),primitives=[dict(mode=4,material=index,
                attributes=dict(POSITION=p,NORMAL=accessor(normals,3),TEXCOORD_0=accessor(uvs,2)),
                indices=accessor(list(range(len(positions)//3)),1,5125))]))
            gltf['nodes'].append(dict(name=label+' '+str(band),mesh=index))
        extent = radius*1.22
        xextent,zextent = (extent*1.5,extent*.7) if form==2 else (extent,extent)
        catalog['templates'].append(dict(id=35+form,label=label,source='landform:'+str(form),
            category='decoration',bounds=[[-xextent,-zextent,0],[xextent,zextent,height+1]],
            parts=[[36,form*3+i] for i in range(3)],
            solids=[[-xextent,-zextent,xextent,zextent,height]],
            camera=[[-xextent,-zextent,0,xextent,zextent,height]],steps=[],floors=[],doors=[],water=[]))
    gltf['buffers']=[dict(byteLength=len(binary))]
    encoded=json.dumps(gltf,separators=(',',':')).encode()
    encoded+=b' '*(-len(encoded)%4)
    raw=struct.pack('<III',0x46546c67,2,28+len(encoded)+len(binary))
    raw+=struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary
    (AUTHORING/'Catalog-36.glb').write_bytes(raw)
    catalog['chunks'].append(dict(file='Catalog-36.glb',parts=12,
        sha256=hashlib.sha256(raw).hexdigest()))
    (AUTHORING/'catalog.json').write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')
    print('Appended 4 landforms, 12 mesh parts; existing 35 templates and fingerprint preserved.')


if __name__=='__main__':
    build()
