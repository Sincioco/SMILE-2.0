"""Export saved Blender site geometry for native water and walk collision."""
from pathlib import Path
import json
import sys
import bpy
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Source'))
from collision_bounds import solid_bounds
layout=json.loads((ROOT/'expansion-layout.json').read_text())
solids=[]
for inst in bpy.context.evaluated_depsgraph_get().object_instances:
    obj=inst.object
    if obj.type!='MESH' or obj.get('neris_door_leaf'):continue
    if obj.name.startswith(('Waterfront','Neris Detailed Tree','Connected','Neris Lawn')):continue
    if any(s in obj.name.lower() for s in ('leaf','leaves','grass','flower','petal','water','step','stair','threshold')):continue
    # Ground-level solid masses; tall roofs and detail above heads aren't barriers.
    for low,high in solid_bounds(inst):
        if low[2]>2.4 or high[2]<.65 or min(high[i]-low[i] for i in range(2))<.15:continue
        solids.append(tuple(round(v,4) for v in (low[0],low[1],high[0],high[1])))
kept=[]
for r in sorted(set(solids),key=lambda r:-(r[2]-r[0])*(r[3]-r[1])):
    if not any(a<=r[0] and b<=r[1] and c>=r[2] and d>=r[3] for a,b,c,d in kept):kept.append(r)
layout['solidRectangles']=kept
(ROOT/'Runtime/site.json').write_text(json.dumps(layout,indent=2)+'\n')
lines=["''' Generated from the saved Blender waterfront. No runtime ownership.",
       'Module Smile.Tools.NerisTownSite','','Option Explicit','',
       'Public Type Region','    X0 As Double','    Z0 As Double','    X1 As Double','    Z1 As Double','End Type','']
for name,value in zip(['WEST','SOUTH','EAST','NORTH'],layout['bounds']):lines.append(f'Public Const {name} = {value*10}')
lines+=['', 'Private Function Box(X0 As Double, Z0 As Double, X1 As Double, Z1 As Double) As Region',
        '', '    Dim Result As Region','','    Result.X0 = X0','    Result.Z0 = Z0','    Result.X1 = X1','    Result.Z1 = Z1',
        '', '    Return Result','','End Function','']
for name,items in [('Water',layout['waterRectangles']),('Bridge',layout['bridges']),('Land',layout['land']),('Solid',kept)]:
    lines += [f'Public Const {name.upper()}_COUNT = {len(items)}','',
              f'Public Function {name}At(Index As Number) As Region','','    Dim Result As Region','','    Select Case Index']
    for i,r in enumerate(items):lines += [f'        Case {i}',f"            Result = Box({', '.join(f'{v*10:.3f}' for v in r)})"]
    lines += ['    End Select','','    Return Result','','End Function','']
lines += ['Public Function Contains(Item As Region, X As Double, Z As Double, Margin As Double) As Boolean',
          '', '    Dim Result As Boolean','',
          '    Result = (X >= Item.X0 - Margin And X <= Item.X1 + Margin And',
          '        Z >= Item.Z0 - Margin And Z <= Item.Z1 + Margin)','',
          '    Return Result','','End Function','','End Module','']
(ROOT.parents[5]/'tools/Character3DViewer/NerisTownSite.smile').write_text('\n'.join(lines))
print('SITE',len(kept),'ground solids;',len(layout['waterRectangles']),'water rectangles',flush=True)
