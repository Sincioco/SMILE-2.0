"""Replace collection-instanced placeholder flowers in the detailed town revision."""
from pathlib import Path
import math
import random
import sys
import bpy

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT.parent/'NerisBuildingsV1/Source'))
from geometry import Geometry, material

assert bpy.app.background and Path(bpy.data.filepath).name == 'Neris-Town-Detailed.blend'
mats = {'stone': bpy.data.materials['Pale Carved Stone'],
        'gold': bpy.data.materials['Aged Gold'],
        'soil': material('Planter Dark Earth', (.055, .033, .017), roughness=1),
        'leaf': material('Flower Jade Foliage', (.075, .20, .055), roughness=.7),
        'rose': material('Rose Petal Silk', (.68, .17, .31), roughness=.64),
        'pink': material('Blush Petal Silk', (.92, .48, .56), roughness=.64),
        'ivory': material('Ivory Petal Silk', (.91, .82, .63), roughness=.67),
        'pollen': material('Golden Pollen', (.76, .43, .04), roughness=.85)}
g = Geometry('Detailed Neris Flower Planter', mats)
rng = random.Random(9513)
g.lathe('Sculpted Stone Bowl', [(1.19,.03),(1.32,.04),(1.43,.14),
    (1.49,.30),(1.5,.42),(1.46,.49),(1.37,.49),(1.32,.43),(1.32,.24)], mat='stone',steps=96)
rim=g.lathe('Bronze Rim Inlay', [(1.485,.32),(1.497,.33),(1.50,.355),(1.487,.37)], mat='gold',steps=96)
# A rim is an open ring; the lathe helper's end caps would cover the soil.
vertices=[tuple(v.co) for v in rim.data.vertices]
faces=[tuple(f.vertices) for f in rim.data.polygons if len(f.vertices)==4]
rim.data.clear_geometry(); rim.data.from_pydata(vertices,[],faces); rim.data.update()
g.cylinder('Recessed Soil', (0,0,.29),1.33,.08,'soil',64)

def petal(name, center, angle, length, width, mat, lift):
    verts, faces = [], []
    for i in range(6):
        t=(i+.015)/5.03
        breadth=math.sin(math.pi*t)**.65*width
        for j in range(5):
            v=(j-2)/2
            r=t*length
            side=v*breadth
            verts.append((center[0]+r*math.cos(angle)-side*math.sin(angle),
                          center[1]+r*math.sin(angle)+side*math.cos(angle),
                          center[2]+lift*t+.045*math.sin(math.pi*t)+v*v*.03))
    for i in range(5):
        for j in range(4):
            k=i*5+j
            faces.append((k,k+1,k+6,k+5))
    g.mesh(name,verts,faces,mat,True)

for i in range(29):
    angle=i*2.39996
    r=1.06*math.sqrt((i+.4)/29)
    x,y=r*math.cos(angle),r*math.sin(angle)
    height=rng.uniform(.67,1.02)
    bend=rng.uniform(-.07,.07)
    g.path('Curved Flower Stem',[(x,y,.32),(x+bend,y,height*.67),(x,y,height)],.011,'leaf')
    for j in range(4):
        petal('Pointed Green Leaf',(x+bend,y,.36+j*.075),angle+j*2.4,.26,.074,'leaf',.10)
    color=['rose','pink','ivory'][i%3]
    for j in range(8):
        petal('Layered Flower Petal',(x,y,height),j*math.tau/8+angle,.19,.072,color,-.025)
    for j in range(5):
        petal('Inner Flower Petal',(x,y,height+.018),j*math.tau/5+angle+.3,.115,.046,color,.025)
    g.lathe('Pollen Heart',[(.037,0),(.047,.023),(.025,.038)],(x,y,height+.018),'pollen',12)

placements=[o for o in bpy.data.objects if o.instance_collection and o.instance_collection.name=='Flower Bed']
assert len(placements)>=6
for obj in placements:
    obj.instance_collection=g.collection
bpy.context.scene.collection.children.unlink(g.collection)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Detailed.blend'),compress=True)
print('FLOWERS',len(placements),'planters, 29 flowers each',flush=True)
