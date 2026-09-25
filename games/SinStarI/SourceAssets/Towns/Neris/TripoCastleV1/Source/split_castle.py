"""Lossless static GLB partitioning for the existing bounded native model format.

Preserves every cleaned triangle, normal, UV and original PBR texture.
Repairs only unusable tangent vectors on degenerate UV corners.
Each model contains at most two 60k-vertex / 65k-triangle parts; no decimation.
"""
from pathlib import Path
import copy
import hashlib
import json
import struct
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Neris-Castle-Cleaned.glb'
blob=source.read_bytes()
length=struct.unpack_from('<I',blob,12)[0]
doc=json.loads(blob[20:20+length]);binary=blob[28+length:]
assert len(doc['meshes'])==1 and len(doc['meshes'][0]['primitives'])==1
primitive=doc['meshes'][0]['primitives'][0]
types={5126:np.float32,5125:np.uint32,5123:np.uint16,5121:np.uint8}
dims={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}


def read(index):
    a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']]
    count=dims[a['type']];dtype=types[a['componentType']]
    offset=v.get('byteOffset',0)+a.get('byteOffset',0)
    assert v.get('byteStride',count*np.dtype(dtype).itemsize)==count*np.dtype(dtype).itemsize
    return np.frombuffer(binary,dtype=dtype,count=a['count']*count,offset=offset).reshape(-1,count)


attributes={k:read(v) for k,v in primitive['attributes'].items()}
normal=attributes['NORMAL'].astype(np.float64)
normal/=np.linalg.norm(normal,axis=1,keepdims=True)
tangent=attributes['TANGENT'].copy()
projected=tangent[:,:3]-normal*np.sum(normal*tangent[:,:3],axis=1,keepdims=True)
invalid=np.linalg.norm(projected,axis=1)<1e-5
# At collapsed UV corners no tangent direction exists. Supply a stable orthogonal
# basis for those corners only; retain all valid Blender/MikkTSpace tangents.
axis=np.eye(3)[np.argmin(np.abs(normal[invalid]),axis=1)]
replacement=np.cross(normal[invalid],axis)
replacement/=np.linalg.norm(replacement,axis=1,keepdims=True)
tangent[invalid,:3]=replacement
attributes['TANGENT']=tangent
triangles=read(primitive['indices']).reshape(-1,3)
output=ROOT/'Runtime';output.mkdir(exist_ok=True)
images=[]
for index,img in enumerate(doc['images']):
    view=doc['bufferViews'][img['bufferView']]
    image_data=binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']]
    name='Castle-PBR-'+str(index)+('.png' if img['mimeType']=='image/png' else '.jpg')
    (output/name).write_bytes(image_data)
    images.append({'uri':name,'name':img.get('name',name)})
parts=[];start=0
while start<len(triangles):
    end=min(start+65000,len(triangles))
    used=np.unique(triangles[start:end])
    while len(used)>60000:
        end=start+max(1,int((end-start)*.9));used=np.unique(triangles[start:end])
    parts.append((start,end,used));start=end
manifest={'source_sha256':hashlib.sha256(blob).hexdigest(),'triangles':len(triangles),
          'repaired_degenerate_tangents':int(invalid.sum()),
          'parts':len(parts),'chunks':[],'placement':{'x':-228,'y':176,'scale':170,'floor':.212,'groundAnchor':.19}}
for batch_index in range(0,len(parts),2):
    batch=parts[batch_index:batch_index+2]
    result={'asset':{'version':'2.0','generator':'Sin and Codex: lossless castle partition'},
        'scene':0,'scenes':[{'nodes':[]}],'nodes':[],'meshes':[],'accessors':[],
        'bufferViews':[],'materials':doc['materials'],'images':images,
        'textures':doc['textures'],'samplers':doc.get('samplers',[])}
    for key in ('extensionsUsed','extensionsRequired'):
        if key in doc:result[key]=doc[key]
    data=bytearray()
    def accessor(values,definition):
        data.extend(b'\0'*(-len(data)%4))
        raw=values.tobytes();view=len(result['bufferViews'])
        result['bufferViews'].append({'buffer':0,'byteOffset':len(data),'byteLength':len(raw)})
        data.extend(raw)
        a={k:v for k,v in definition.items() if k not in ('bufferView','byteOffset','count','min','max')}
        a.update(bufferView=view,count=len(values))
        if a['type']=='VEC3':a.update(min=values.min(axis=0).tolist(),max=values.max(axis=0).tolist())
        index=len(result['accessors']);result['accessors'].append(a);return index
    for start,end,used in batch:
        attr={name:accessor(values[used],doc['accessors'][primitive['attributes'][name]]) for name,values in attributes.items()}
        indices=np.searchsorted(used,triangles[start:end]).astype(np.uint32).reshape(-1,1)
        index=accessor(indices,{'componentType':5125,'type':'SCALAR'})
        mesh_index=len(result['meshes']);result['meshes'].append({'primitives':[{'attributes':attr,'indices':index,'material':primitive.get('material',0),'mode':4}]})
        node=copy.deepcopy(next(n for n in doc['nodes'] if n.get('mesh')==0));node['mesh']=mesh_index
        node.pop('children',None);result['nodes'].append(node);result['scenes'][0]['nodes'].append(mesh_index)
    data.extend(b'\0'*(-len(data)%4));result['buffers']=[{'byteLength':len(data)}]
    encoded=json.dumps(result,separators=(',',':')).encode();encoded+=b' '*(-len(encoded)%4)
    name=f'Castle-{batch_index//2:02d}.glb'
    encoded_blob=struct.pack('<III',0x46546c67,2,28+len(encoded)+len(data))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(data),0x004e4942)+data
    (output/name).write_bytes(encoded_blob)
    manifest['chunks'].append({'file':name,'parts':len(batch),'triangles':sum(b-a for a,b,_ in batch),'sha256':hashlib.sha256(encoded_blob).hexdigest()})
(output/'Castle.sm3d.json').write_text('{"version":1}\n')
(output/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('CASTLE PARTITIONS',len(parts),'parts,',len(manifest['chunks']),'models,',len(triangles),'triangles')
