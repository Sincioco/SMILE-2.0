"""Export the small moving-leaf mesh and exact authored effect/tree placements."""
from pathlib import Path
import json
import hashlib
import bpy
import sys
import math
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from static_glb import write

ROOT = Path(__file__).resolve().parents[1]
data=json.loads((ROOT/'Runtime/manifest.json').read_text())
# Imported buildings combine each lantern pair into one mesh. Find connected
# luminous components, so halos sit at the actual lamps, not midway between them.
lamps=[]
for instance in bpy.context.evaluated_depsgraph_get().object_instances:
    obj=instance.object
    if obj.type!='MESH': continue
    slots={i for i,m in enumerate(obj.data.materials) if m and m.name.startswith('Warm Lantern')}
    if not slots: continue
    links={}
    positions={}
    for face in obj.data.polygons:
        if face.material_index not in slots: continue
        keys=[tuple(round(v,5) for v in obj.data.vertices[i].co) for i in face.vertices]
        for key in keys:
            links.setdefault(key,set()).update(keys)
            positions[key]=instance.matrix_world @ Vector(key)
    while links:
        pending=[next(iter(links))]; component=[]
        while pending:
            key=pending.pop()
            if key not in links: continue
            pending.extend(links.pop(key)); component.append(positions[key])
        center=sum(component,Vector())/len(component)
        point=Vector((center.x*10,center.z*10+21,center.y*10))
        if not any((point-Vector(old)).length<3 for old in lamps):
            lamps.append([round(v,5) for v in point])
data['lamps']=lamps
mat=bpy.data.materials['Neris Young Olive Leaves'].copy()
mat.name='Falling Jade Leaf'
mat.use_backface_culling=False
mat.node_tree.nodes.get('Principled BSDF').inputs['Alpha'].default_value=.999
mesh=bpy.data.meshes.new('Falling leaf')
mesh.from_pydata([(0,-.13,0),(-.048,-.025,0),(0,0,.012),(.048,-.025,0),(0,.14,0)],[],
                  [(0,1,2),(0,2,3),(1,4,2),(3,2,4)])
mesh.update(); mesh.calc_loop_triangles()
triangles=[tuple((tuple(mesh.vertices[mesh.loops[i].vertex_index].co),tuple(mesh.corner_normals[i].vector))
                 for i in t.loops) for t in mesh.loop_triangles]
write(ROOT/'Runtime/Leaf.glb',[(mat.name,mat,triangles,12)])
data['leaf']={'file':'Leaf.glb','parts':1,'triangles':len(triangles),
              'sha256':hashlib.sha256((ROOT/'Runtime/Leaf.glb').read_bytes()).hexdigest()}
(ROOT/'Runtime/manifest.json').write_text(json.dumps(data,indent=2)+'\n')
lines=["''' Generated from the accepted Blender town. Regenerate with Source/export_layout.py.",
       'Module Smile.Tools.NerisTownLayout','','Option Explicit','',
       'Import Smile.Simple3D.Precision3D As P','',
       f'Public Const TREE_COUNT = {len(data["trees"])}',f'Public Const LAMP_COUNT = {len(data["lamps"])}','',
       'Public Type TreePlacement','    Position As P.Vector3','    Scale As P.Vector3',
       '    Yaw As Double','    Variant As Number','End Type','',
       'Public Function TreeAt(Index As Number) As TreePlacement','',
       '    Dim Result As TreePlacement','','    Select Case Index']
def vector(values): return 'P.Vector('+', '.join(f'{x:.5f}' for x in values)+')'
for i,t in enumerate(data['trees']):
    lines += [f'        Case {i}',f'            Result.Position = {vector(t["position"])}',
              f'            Result.Scale = {vector(t["scale"])}',f'            Result.Yaw = {t["yaw"]:.5f}',
              f'            Result.Variant = {t["variant"]}']
lines += ['    End Select','','    Return Result','','End Function','',
          'Public Function LampAt(Index As Number) As P.Vector3','','    Dim Result As P.Vector3','',
          '    Select Case Index']
for i,p in enumerate(data['lamps']):
    lines += [f'        Case {i}',f'            Result = {vector(p)}']
lines += ['    End Select','','    Return Result','','End Function','',
          'Public Function PavingHeight(X As Double, Z As Double) As Double','']
layout=json.loads((ROOT/'expansion-layout.json').read_text())
for x,y,w,d in layout['paving']:
    left,right=math.floor(x-w/2)*10,math.ceil(x+w/2)*10
    front,back=math.floor(y-d/2)*10,math.ceil(y+d/2)*10
    lines += [f'    If (X >= {left}.0 And X <= {right}.0 And',
              f'        Z >= {front}.0 And Z <= {back}.0) Then',
              '        Return 23.12','    End If','']
lines += ['    Return 20.9','','End Function','','End Module','']
(ROOT.parents[5]/'tools/Character3DViewer/NerisTownLayout.smile').write_text('\n'.join(lines))
print('LAYOUT',len(data['trees']),'trees',len(data['lamps']),'lamps, fixed reusable leaf mesh',flush=True)
