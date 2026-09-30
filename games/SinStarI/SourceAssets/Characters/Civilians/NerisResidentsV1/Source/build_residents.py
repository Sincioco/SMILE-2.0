"""Original, self-contained Neris civilian meshes and in-place skeletal motion.

No downloaded art or animation. glTF coordinates are metres, Y up, facing +Z.
Every sampled pose is grounded against its own skinned feet, never Arin's offsets.
"""
import hashlib
import json
import math
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parent.parent
TAU = math.tau
CAST = [
    ('Tessa', 'f', False, (0.09, .34, .37), (.64, .38, .23), (.10, .045, .025)),
    ('Maren', 'f', False, (.43, .19, .34), (.90, .66, .48), (.23, .065, .025)),
    ('Ilan', 'm', False, (.15, .23, .41), (.47, .25, .14), (.025, .018, .015)),
    ('Bram', 'm', False, (.35, .24, .12), (.76, .51, .32), (.38, .30, .22)),
    ('Sera', 'f', False, (.20, .36, .18), (.56, .34, .23), (.035, .026, .020)),
    ('Pip', 'm', True, (.62, .29, .08), (.89, .65, .45), (.30, .13, .045)),
    ('Nia', 'f', True, (.24, .30, .54), (.50, .29, .19), (.045, .027, .018)),
    ('Tobin', 'm', True, (.16, .41, .32), (.73, .48, .31), (.10, .060, .033)),
]


class Figure:
    def __init__(self, name, colors):
        self.name, self.colors = name, colors
        self.joints, self.parts = [], [[] for _ in colors]

    def joint(self, name, point, parent=-1):
        self.joints.append((name, point, parent))
        return len(self.joints) - 1

    def egg(self, material, joint, center, size, rings=8, slices=12):
        """Smooth ellipsoid; one rigid skin influence keeps simple limbs predictable."""
        vertices, normals, indices = [], [], []
        for r in range(rings + 1):
            a = math.pi * r / rings
            for s in range(slices + 1):
                b = TAU * s / slices
                unit = (math.sin(a)*math.cos(b), math.cos(a), math.sin(a)*math.sin(b))
                vertices.append(tuple(center[i]+unit[i]*size[i] for i in range(3)))
                n = [unit[i]/size[i] for i in range(3)]
                length = math.sqrt(sum(v*v for v in n))
                normals.append(tuple(v/length for v in n))
        for r in range(rings):
            for s in range(slices):
                a = r*(slices+1)+s
                b = a+slices+1
                if r > 0:
                    indices.extend((a, a+1, b))
                if r < rings-1:
                    indices.extend((a+1, b+1, b))
        self.parts[material].append((joint, vertices, normals, indices))

    def skirt(self, joint, width):
        vertices, normals, indices = [], [], []
        for y,r in [(1.035,width*.85),(.80,width*1.20),(.67,width*1.30)]:
            for i in range(25):
                a=TAU*i/24
                vertices.append((r*math.cos(a),y,r*.67*math.sin(a)))
                normals.append((math.cos(a),.12,math.sin(a)))
        for row in range(2):
            for i in range(24):
                a=row*25+i
                indices.extend((a,a+1,a+25,a+1,a+26,a+25))
        self.parts[1].append((joint,vertices,normals,indices))

    def save(self, dog=False):
        blob = bytearray()
        views, accessors = [], []

        def accessor(values, kind, form='f', bounds=False):
            flat = [x for row in values for x in row]
            while len(blob) % 4:
                blob.append(0)
            offset = len(blob)
            blob.extend(struct.pack('<'+form*len(flat), *flat))
            views.append({'buffer': 0, 'byteOffset': offset, 'byteLength': len(blob)-offset})
            item = {'bufferView': len(views)-1, 'componentType': {'f':5126,'H':5123,'I':5125}[form],
                    'count': len(values), 'type': kind}
            if bounds:
                item['min'] = [min(row[i] for row in values) for i in range(len(values[0]))]
                item['max'] = [max(row[i] for row in values) for i in range(len(values[0]))]
            accessors.append(item)
            return len(accessors)-1

        all_vertices = []
        positions, normals, joints, weights, indices, uvs = [], [], [], [], [], []
        for mat, parts in enumerate(self.parts):
            if not parts:
                continue
            for joint, verts, norms, faces in parts:
                indices.extend((i+len(positions),) for i in faces)
                positions.extend(verts)
                normals.extend(norms)
                joints.extend([(joint,0,0,0)]*len(verts))
                weights.extend([(1.,0.,0.,0.)]*len(verts))
                uvs.extend([((mat+.5)/len(self.colors),.5)]*len(verts))
                all_vertices.extend((joint, p) for p in verts)
        primitives = [{'attributes': {'POSITION':accessor(positions,'VEC3',bounds=True),
            'NORMAL':accessor(normals,'VEC3'), 'JOINTS_0':accessor(joints,'VEC4','H'),
            'TEXCOORD_0':accessor(uvs,'VEC2'),
            'TANGENT':accessor([tangent(n) for n in normals],'VEC4'),
            'WEIGHTS_0':accessor(weights,'VEC4')},
            'indices':accessor(indices,'SCALAR','I'), 'material':0}]
        nodes, inverse = [], []
        for name, point, parent in self.joints:
            origin = (0,0,0) if parent < 0 else self.joints[parent][1]
            nodes.append({'name':name,'translation':[point[i]-origin[i] for i in range(3)]})
            inverse.append((1,0,0,0,0,1,0,0,0,0,1,0,-point[0],-point[1],-point[2],1))
        for i, (_, _, parent) in enumerate(self.joints):
            if parent >= 0:
                nodes[parent].setdefault('children',[]).append(i)
        mesh_node = len(nodes)
        nodes.append({'name':self.name,'mesh':0,'skin':0})
        animations, grounding = [], {}
        for clip, seconds in [('Idle',3.0),('Walk',1.2),('Run',.72)]:
            frames = round(seconds*30)+1
            times = [(seconds*i/(frames-1),) for i in range(frames)]
            time_acc = accessor(times,'SCALAR',bounds=True)
            rotations = [[] for _ in self.joints]
            lifts = []
            minima = []
            for (t,) in times:
                phase = TAU*t/seconds
                angles = pose(self.joints, clip, phase, dog)
                world = []
                for i, (_, p, parent) in enumerate(self.joints):
                    angle = angles[i]
                    rotations[i].append((math.sin(angle/2),0,0,math.cos(angle/2)))
                    if parent < 0:
                        world.append((p,angle))
                    else:
                        pp, pa = world[parent]
                        rest = self.joints[parent][1]
                        delta = (p[0]-rest[0],p[1]-rest[1],p[2]-rest[2])
                        moved = rx(delta,pa)
                        world.append((tuple(pp[k]+moved[k] for k in range(3)),pa+angle))
                low = min(world[j][0][1]+rx(tuple(p[k]-self.joints[j][1][k] for k in range(3)),world[j][1])[1]
                          for j,p in all_vertices)
                lifts.append((0.,-low,0.))
                minima.append(low + lifts[-1][1])
            samplers, channels = [], []
            for i, values in enumerate(rotations):
                samplers.append({'input':time_acc,'output':accessor(values,'VEC4'),'interpolation':'LINEAR'})
                channels.append({'sampler':len(samplers)-1,'target':{'node':i,'path':'rotation'}})
            samplers.append({'input':time_acc,'output':accessor(lifts,'VEC3'),'interpolation':'LINEAR'})
            channels.append({'sampler':len(samplers)-1,'target':{'node':0,'path':'translation'}})
            animations.append({'name':clip,'samplers':samplers,'channels':channels})
            grounding[clip] = {'frame0MinimumY':minima[0], 'minimumY':min(minima), 'maximumY':max(minima), 'samples':frames}
        inverse_acc = accessor(inverse,'MAT4')
        while len(blob)%4: blob.append(0)
        palette=palette_png(self.colors)
        doc = {'asset':{'version':'2.0','generator':'SMILE original Neris Residents V1'},
            'scene':0,'scenes':[{'nodes':[0,mesh_node]}], 'nodes':nodes,
            'meshes':[{'name':self.name,'primitives':primitives}],
            'skins':[{'joints':list(range(len(self.joints))),'inverseBindMatrices':inverse_acc,'skeleton':0}],
            'materials':[{'name':'Resident Palette','pbrMetallicRoughness':{
                'baseColorTexture':{'index':0},'metallicFactor':.0,'roughnessFactor':.78}}],
            'textures':[{'sampler':0,'source':0}],
            'samplers':[{'magFilter':9729,'minFilter':9987,'wrapS':10497,'wrapT':10497}],
            'images':[{'uri':f'{self.name}-palette.png'}],
            'animations':animations,'buffers':[{'byteLength':len(blob)}],
            'bufferViews':views,'accessors':accessors}
        encoded = json.dumps(doc,separators=(',',':')).encode()
        encoded += b' '*((-len(encoded))%4)
        blob.extend(b'\0'*((-len(blob))%4))
        result = struct.pack('<III',0x46546c67,2,28+len(encoded)+len(blob))
        result += struct.pack('<II',len(encoded),0x4e4f534a)+encoded
        result += struct.pack('<II',len(blob),0x004e4942)+blob
        path = ROOT/'Models'/f'{self.name}.glb'
        path.parent.mkdir(parents=True,exist_ok=True)
        path.write_bytes(result)
        path.with_name(f'{self.name}-palette.png').write_bytes(palette)
        return {'name':self.name,'file':str(path.relative_to(ROOT)),
            'sha256':hashlib.sha256(result).hexdigest(),
            'paletteSha256':hashlib.sha256(palette).hexdigest(),
            'triangles':sum(len(p[3])//3 for parts in self.parts for p in parts),
            'bindMinimumY':min(p[1] for _,p in all_vertices),'clips':grounding}


def tangent(n):
    # Constant palette UVs still need a stable orthogonal tangent frame.
    t = (-n[2],0.,n[0]) if abs(n[1]) < .99 else (0.,-n[2],n[1])
    length = math.sqrt(sum(v*v for v in t))
    return (*[v/length for v in t],1.)


def palette_png(colors):
    """One material per actor preserves the shared town renderer's material budget."""
    def chunk(kind,data):
        return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data))
    def channel(v):
        return round(255*(12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055))
    row=b''.join(bytes([*(channel(v) for v in c),255])*16 for c in colors)
    return (b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',len(colors)*16,16,8,6,0,0,0))+
            chunk(b'IDAT',zlib.compress((b'\0'+row)*16))+chunk(b'IEND',b''))


def rx(p, angle):
    c,s = math.cos(angle),math.sin(angle)
    return p[0], p[1]*c-p[2]*s, p[1]*s+p[2]*c


def pose(joints, clip, phase, dog):
    values = []
    stride = math.sin(phase)
    for name, _, _ in joints:
        value = 0.
        side = -1 if name.endswith('R') else 1
        if clip == 'Idle':
            if name == 'Chest': value = .018*stride
            if name == 'Head': value = -.025*stride
            if name.startswith('Arm'): value = .025*stride
        else:
            amplitude = .42 if clip == 'Walk' else .75
            if name.startswith('Leg'): value = side*amplitude*stride
            if name.startswith('Knee'): value = -max(0., side*stride)*amplitude*1.3
            if name.startswith('Arm'): value = -side*amplitude*.75*stride
            if name.startswith('Elbow'): value = -.12 - (.4 if clip == 'Run' else .1)*(1-side*stride)
            if dog and name.startswith('Fore'): value = -side*amplitude*stride
        if name == 'Tail': value = .20*math.sin(phase*2)
        values.append(value)
    return values


def human(record):
    name, gender, child, shirt, skin, hair = record
    colors = [skin,shirt,(.79,.73,.57),(.08,.10,.14),hair,(.035,.026,.025),(.92,.88,.76),(.63,.43,.16)]
    f = Figure(name,colors)
    f.joint('Root',(0,0,0))
    hips = f.joint('Hips',(0,.88,0),0)
    chest = f.joint('Chest',(0,1.20,0),hips)
    head = f.joint('Head',(0,1.50,0),chest)
    width = .18 if gender == 'f' else .205
    f.egg(1,chest,(0,1.235,0),(width,.265,.115))
    f.egg(1,hips,(0,.955,0),(width*.92,.16,.115))
    f.egg(2,chest,(0,1.255,.075),(width*.73,.21,.06))
    f.egg(7,hips,(0,1.035,.01),(width*.96,.026,.123))
    f.egg(7,hips,(0,1.035,.132),(.035,.032,.018))
    f.egg(0,head,(0,1.495,0),(.064,.10,.065))
    f.egg(0,head,(0,1.667,.01),(.105,.142,.096),12,16)
    f.egg(0,head,(0,1.603,.051),(.078,.069,.068),10,16)
    f.egg(4,head,(0,1.755,-.015),(.110,.060,.102),10,16)
    f.egg(4,head,(0,1.678,-.066),(.107,.117,.043),10,16)
    for side in (-1,1):
        f.egg(0,head,(side*.107,1.662,.003),(.022,.040,.024))
        f.egg(6,head,(side*.041,1.687,.097),(.023,.012,.007))
        f.egg(5,head,(side*.041,1.687,.104),(.010,.011,.004))
        f.egg(4,head,(side*.041,1.711,.096),(.027,.005,.009))
        f.egg(1,chest,(side*.060,1.445,.073),(.051,.036,.026))
    f.egg(0,head,(0,1.659,.112),(.019,.036,.028))
    f.egg(4,head,(0,1.616,.105),(.029,.005,.005))
    if gender == 'f':
        f.egg(4,head,(0,1.655,-.109),(.080,.088,.070))
        f.egg(7,head,(0,1.745,.022),(.112,.012,.097))
        for side in (-1,1):
            f.egg(4,head,(side*.099,1.63,-.025),(.035,.13,.070))
        f.skirt(hips,width)
    elif name == 'Bram':
        f.egg(4,head,(0,1.578,.067),(.075,.048,.049))
    for side,suffix in [(-1,'L'),(1,'R')]:
        arm = f.joint('Arm'+suffix,(side*.22,1.39,0),chest)
        elbow = f.joint('Elbow'+suffix,(side*.245,1.13,.015),arm)
        leg = f.joint('Leg'+suffix,(side*.092,.89,0),hips)
        knee = f.joint('Knee'+suffix,(side*.092,.47,0),leg)
        f.egg(1,arm,(side*.195,1.389,0),(.09,.078,.079))
        f.egg(1,arm,(side*.235,1.265,.005),(.070,.160,.070))
        f.egg(1,elbow,(side*.245,1.13,.015),(.047,.055,.048))
        f.egg(1,elbow,(side*.245,1.03,.02),(.049,.130,.049))
        f.egg(2,elbow,(side*.245,.945,.02),(.055,.030,.054))
        f.egg(0,elbow,(side*.245,.876,.023),(.043,.064,.030))
        f.egg(3,leg,(side*.092,.69,0),(.078,.234,.086))
        f.egg(3,knee,(side*.092,.47,0),(.061,.064,.069))
        f.egg(3,knee,(side*.092,.28,0),(.055,.220,.067))
        f.egg(5,knee,(side*.092,.112,.025),(.066,.105,.082))
        f.egg(5,knee,(side*.092,.048,.072),(.068,.048,.130))
    if child:
        # Shorter limbs, fuller head: a distinct child silhouette, not merely a tint.
        for parts in f.parts:
            for _,verts,_,_ in parts:
                for i,p in enumerate(verts):
                    verts[i] = (p[0]*.75,p[1]*.69,p[2]*.78)
        f.joints = [(n,(p[0]*.75,p[1]*.69,p[2]*.78),par) for n,p,par in f.joints]
    return f


def dog():
    f = Figure('Mochi',[(.42,.22,.085),(.79,.60,.36),(.035,.022,.012),(.10,.31,.37)])
    root = f.joint('Root',(0,0,0))
    body = f.joint('Chest',(0,.48,0),root)
    head = f.joint('Head',(0,.60,.36),body)
    tail = f.joint('Tail',(0,.56,-.39),body)
    f.egg(0,body,(0,.48,0),(.175,.20,.42),10,16)
    f.egg(1,body,(0,.45,.25),(.145,.17,.18))
    f.egg(0,head,(0,.66,.405),(.135,.145,.16))
    f.egg(1,head,(0,.60,.54),(.09,.07,.13))
    f.egg(2,head,(0,.627,.653),(.050,.032,.026))
    f.egg(3,head,(0,.57,.36),(.143,.025,.135))
    for side,suffix in [(-1,'L'),(1,'R')]:
        f.egg(2,head,(side*.082,.697,.516),(.016,.021,.015))
        f.egg(0,head,(side*.132,.625,.39),(.058,.133,.067))
        for front in (True,False):
            z = .27 if front else -.27
            joint = f.joint(('Fore' if front else 'Leg')+suffix,(side*.12,.47,z),body)
            f.egg(0,joint,(side*.13,.265,z),(.051,.230,.058))
            f.egg(1,joint,(side*.13,.048,z+.035),(.064,.048,.094))
    f.egg(0,tail,(0,.63,-.55),(.047,.083,.22))
    f.egg(0,tail,(0,.575,-.40),(.062,.061,.085))
    return f


if __name__ == '__main__':
    records = [human(record).save() for record in CAST]
    records.append(dog().save(True))
    (ROOT/'residents.sm3d.json').write_text(json.dumps({'version':1,'sampleRate':30,
        'clips':{name:{'loop':True} for name in ('Idle','Walk','Run')}},indent=2)+'\n')
    (ROOT/'grounding.json').write_text(json.dumps(records,indent=2)+'\n')
    print(json.dumps([{k:r[k] for k in ('name','triangles','bindMinimumY')} for r in records],indent=2))
