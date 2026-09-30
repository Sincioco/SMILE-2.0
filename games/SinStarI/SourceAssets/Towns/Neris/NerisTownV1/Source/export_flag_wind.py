"""Attach a small looping wind skin to existing HQ flag vertices without changing catalog slots.
Run in Blender against the immutable Catalog.blend; original catalog and document identity stay intact.
"""
import json, math, struct, sys
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Source'))
from catalog_scene import assemblies

def key(p): return tuple(round(v,3) for v in p)

def run():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Authoring/Catalog.blend'))
    hq=next(a for a in assemblies() if a.label=='Military HQ')
    names={'Royal Neris Flag','Flag Border','Flag Border.001','Flag Gold Border','Flag Gold Border.001','Neris Four Point Star.105'}
    selected=set()
    graph=bpy.context.evaluated_depsgraph_get()
    for name in names:
        obj=bpy.data.objects[name].evaluated_get(graph)
        mesh=obj.to_mesh()
        matrix=hq.matrix.inverted() @ obj.matrix_world
        for v in mesh.vertices:
            p=matrix @ v.co
            selected.add(key((p.x,p.z,-p.y)))
        obj.to_mesh_clear()
    low=min(p[0] for p in selected); high=max(p[0] for p in selected)
    output=ROOT/'Authoring/Wind'
    output.mkdir(exist_ok=True)
    changed=[]
    for source in sorted((ROOT/'Authoring/Templates').glob('Catalog-*.glb')):
        raw=source.read_bytes(); size=struct.unpack_from('<I',raw,12)[0]
        doc=json.loads(raw[20:20+size]); blob=bytearray(raw[28+size:])
        def values(index):
            a=doc['accessors'][index]; v=doc['bufferViews'][a['bufferView']]
            offset=v.get('byteOffset',0)+a.get('byteOffset',0)
            n={'VEC3':3,'VEC4':4,'SCALAR':1}[a['type']]
            return [struct.unpack_from('<'+'f'*n,blob,offset+i*v.get('byteStride',4*n)) for i in range(a['count'])]
        def add(data,kind,component=5126):
            dimensions={'SCALAR':1,'VEC3':3,'VEC4':4,'MAT4':16}[kind]
            blob.extend(b'\0'*(-len(blob)%4)); start=len(blob)
            blob.extend(struct.pack('<'+('f' if component==5126 else 'H')*len(data),*data))
            view=len(doc['bufferViews']); doc['bufferViews'].append({'buffer':0,'byteOffset':start,'byteLength':len(blob)-start})
            i=len(doc['accessors']);doc['accessors'].append({'bufferView':view,'componentType':component,'count':len(data)//dimensions,'type':kind})
            return i
        hits=0
        for node in doc['nodes'][:]:
            if 'mesh' not in node:continue
            for prim in doc['meshes'][node['mesh']]['primitives']:
                points=values(prim['attributes']['POSITION'])
                flags=[key(p) in selected for p in points]
                # A production animated model uses one skin on every part;
                # static vertices remain rigidly attached to the identity root.
                joints=[]; weights=[]
                for p,flag in zip(points,flags):
                    if flag:
                        u=max(0,min(1,(p[0]-low)/(high-low)))*7
                        i=min(6,int(u)); f=u-i
                        joints.extend([i+1,i+2,0,0]);weights.extend([1-f,f,0,0]);hits+=1
                    else:
                        joints.extend([0,0,0,0]);weights.extend([1,0,0,0])
                prim['attributes']['JOINTS_0']=add(joints,'VEC4',5123)
                prim['attributes']['WEIGHTS_0']=add(weights,'VEC4')
                node['skin']=0
        if not hits:continue
        for index,node in enumerate(doc['nodes']):
            node['name']=f"{node.get('name','Catalog')} Part {index}"
        first=len(doc['nodes'])
        bones=list(range(first,first+9))
        doc['nodes'].extend({'name':f'HQ Wind {i}','translation':[0,0,0]} for i in range(9))
        doc['nodes'][first]['children']=bones[1:]
        doc['scenes'][0]['nodes'].append(first)
        identity=[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]
        doc['skins']=[{'joints':bones,'inverseBindMatrices':add(identity*9,'MAT4')}]
        times=[i/8 for i in range(33)]
        input_id=add(times,'SCALAR');doc['accessors'][input_id].update(min=[0],max=[4])
        animation={'name':'Gentle Wind','samplers':[],'channels':[]}
        for i in range(1,9):
            u=(i-1)/7
            offsets=[]
            for t in times:offsets.extend([0,0,.18*u*math.sin(t*math.tau/4-u*math.tau)])
            output_id=add(offsets,'VEC3')
            animation['samplers'].append({'input':input_id,'output':output_id,'interpolation':'LINEAR'})
            animation['channels'].append({'sampler':i-1,'target':{'node':first+i,'path':'translation'}})
        doc['animations']=[animation]
        blob.extend(b'\0'*(-len(blob)%4));doc['buffers'][0]['byteLength']=len(blob)
        encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*(-len(encoded)%4)
        (output/source.name).write_bytes(struct.pack('<III',0x46546c67,2,28+len(encoded)+len(blob))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(blob),0x004e4942)+blob)
        changed.append({'file':source.name,'flag_vertices':hits})
    assert changed and sum(x['flag_vertices'] for x in changed)>100
    (output/'wind.json').write_text(json.dumps(changed,indent=2)+'\n')
    print('WIND',changed)

if __name__=='__main__':run()
