"""Measure skinned body minimum Y at 30 Hz; defaults to the Orin checkpoint.

The viewer's relative grounding correction is first Idle minimum minus the
sampled clip minimum. Equipment is excluded so the shield cannot lift Orin.
This never changes the model, animation source, or saved pose corrections.
"""
import argparse, json, struct, hashlib
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parents[1]/'games/SinStarI/SourceAssets/Characters/Tank/OrinV13'
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--model', type=Path, default=root/'orin-v1.3-animation-checkpoint.glb')
parser.add_argument('--output', type=Path, default=root/'Calibration/orin-v1.3-grounding.json')
parser.add_argument('--exclude', nargs='+', default=['00_Shield','01_Weapon'])
parser.add_argument('--clips', nargs='+', default=['Idle','Defend','Hit','Death'])
args=parser.parse_args()
raw=args.model.read_bytes()
length=struct.unpack_from('<I',raw,12)[0]; doc=json.loads(raw[20:20+length]); binary=raw[28+length:]
def accessor(index):
    a=doc['accessors'][index]; v=doc['bufferViews'][a['bufferView']]
    size={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']]
    dt=np.dtype({5126:'<f4',5123:'<u2',5121:'u1',5125:'<u4'}[a['componentType']])
    return np.ndarray((a['count'],size),dtype=dt,buffer=binary,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',size*dt.itemsize),dt.itemsize)).copy()
def matrix(t,q,s):
    x,y,z,w=q/np.linalg.norm(q); m=np.eye(4)
    m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)], [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)], [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])@np.diag(s)
    m[:3,3]=t; return m
parents={c:i for i,n in enumerate(doc['nodes']) for c in n.get('children',[])}
meshes=[]
for n in doc['nodes']:
    if 'mesh' not in n or 'skin' not in n or n.get('name') in args.exclude: continue
    skin=doc['skins'][n['skin']]; ibm=accessor(skin['inverseBindMatrices']).reshape(-1,4,4).transpose(0,2,1)
    for p in doc['meshes'][n['mesh']]['primitives']:
        a=p['attributes']; verts=accessor(a['POSITION']); verts=np.column_stack((verts,np.ones(len(verts))))
        meshes.append((verts, accessor(a['JOINTS_0']),accessor(a['WEIGHTS_0']),skin['joints'],ibm))
result={}
for anim in doc['animations']:
    name=anim['name']
    samplers=[(accessor(s['input'])[:,0],accessor(s['output'])) for s in anim['samplers']]
    duration=max(float(s[0][-1]) for s in samplers); rows=[]
    times_to_sample=np.linspace(0,duration,round(duration*30)+1) if name in args.clips else [0]
    for time in times_to_sample:
        vals=[{p:np.array(n.get(p,d),float) for p,d in [('translation',[0,0,0]),('rotation',[0,0,0,1]),('scale',[1,1,1])]} for n in doc['nodes']]
        for c in anim['channels']:
            times,outputs=samplers[c['sampler']]; hi=min(len(times)-1,max(1,np.searchsorted(times,time))); lo=hi-1
            f=np.clip((time-times[lo])/max(1e-9,times[hi]-times[lo]),0,1); a,b=outputs[lo],outputs[hi]; path=c['target']['path']
            if path=='rotation' and np.dot(a,b)<0:b=-b
            vals[c['target']['node']][path]=a*(1-f)+b*f
        cache={}
        def world(i):
            if i not in cache:
                n=doc['nodes'][i]; v=vals[i]; m=matrix(v['translation'],v['rotation'],v['scale'])
                if 'matrix' in n: m=np.array(n['matrix']).reshape(4,4).T
                cache[i]=world(parents[i])@m if i in parents else m
            return cache[i]
        minima=[]
        for verts,joints,weights,nodes,ibm in meshes:
            ms=np.array([world(n) for n in nodes])@ibm
            ys=np.sum(np.einsum('vkij,vj->vki',ms[joints],verts)[:,:,1]*weights,axis=1)
            minima.append(float(ys.min()))
        rows.append(round(min(minima)*1000))
    result[name]=rows
    print(name,rows)
report={'modelSha256':hashlib.sha256(raw).hexdigest(), 'sampleRate':30,
    'fullClips':args.clips, 'frameZeroCheckedForEveryClip':True, 'excludedMeshes':args.exclude,
    'unit':'one thousandth of model unit', 'idleReferenceMinimumY1000':result['Idle'][0],
    'bindMinimumY1000':round(min(float(mesh[0][:,1].min()) for mesh in meshes)*1000),
    'minimumY1000':result}
args.output.write_text(json.dumps(report,indent=2)+'\n')
