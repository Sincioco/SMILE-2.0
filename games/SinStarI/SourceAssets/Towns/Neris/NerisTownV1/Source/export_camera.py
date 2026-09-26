"""Conservative camera volumes from the saved scene, independent of walk collision.

Keep solid architecture and props; ground, water and soft vegetation are not walls.
Contained boxes are redundant. Runtime tests use padded segment/AABB clearance.
"""
from pathlib import Path
import sys
import bpy

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Source'))
from collision_bounds import solid_bounds
boxes=[]
skip=('Waterfront','Neris Detailed Tree','Town Grass','Royal Site','Connected','Castle Moat',
      'Comparison Moat','Unified','Town Garden','Town Bedrock','Comparison Terrace')
for instance in bpy.context.evaluated_depsgraph_get().object_instances:
    obj=instance.object
    if obj.type!='MESH' or obj.name.startswith(skip):continue
    # Ignore paving, thin ornaments, fountains' water, and leaf/flower clusters.
    if any(s in obj.name.lower() for s in ('leaf','leaves','grass','flower','petal','banner','flag','water')):continue
    for low,high in solid_bounds(instance):
        if high[2]<1.0 or min(high[i]-low[i] for i in range(3))<.18:continue
        box=tuple(round(v*10,2) for v in (low[0],low[2]+2.1,low[1],high[0],high[2]+2.1,high[1]))
        boxes.append(box)
# Coalesce touching solid details without filling substantial empty space (doors).
# This folds cornices/roof layers into their mass while keeping courtyard gaps.
for _ in range(3):
    merged=[]
    for b in sorted(set(boxes),key=lambda b:-(b[3]-b[0])*(b[4]-b[1])*(b[5]-b[2])):
        volume=lambda v:(v[3]-v[0])*(v[4]-v[1])*(v[5]-v[2])
        for i,a in enumerate(merged):
            if any(a[j]>b[j+3]+4 or b[j]>a[j+3]+4 for j in range(3)):continue
            union=tuple(min(a[j],b[j]) for j in range(3))+tuple(max(a[j],b[j]) for j in range(3,6))
            overlap=1
            for j in range(3):overlap*=max(0,min(a[j+3],b[j+3])-max(a[j],b[j]))
            if volume(union)<=(volume(a)+volume(b)-overlap)*1.12:
                merged[i]=union
                break
        else:merged.append(b)
    boxes=merged
boxes=sorted(set(boxes),key=lambda b:-(b[3]-b[0])*(b[4]-b[1])*(b[5]-b[2]))
kept=[]
for b in boxes:
    if not any(all(a[i]<=b[i]+.01 and a[i+3]>=b[i+3]-.01 for i in range(3)) for a in kept):kept.append(b)
assert len(kept)<=512, f'Camera volume budget exceeded: {len(kept)}'
lines=["''' Generated camera clearance volumes from the saved Blender town.",
       'Module Smile.Tools.NerisTownObstacles','','Option Explicit','',
       'Import Smile.Simple3D.CameraClearance3D As Clearance','',
       'Public Sub Populate(ByRef World As Clearance.World)','',f'    World.BoxCount = {len(kept)}','']
for i,b in enumerate(kept):
    lines.append(f'    World.Boxes[{i}] = Clearance.Box('+', '.join(f'{v:.2f}' for v in b)+')')
lines+=['','End Sub','','End Module','']
(ROOT.parents[5]/'tools/Character3DViewer/NerisTownObstacles.smile').write_text('\n'.join(lines))
print(f'CAMERA {len(kept)} conservative volumes from {len(boxes)} bounds',flush=True)
