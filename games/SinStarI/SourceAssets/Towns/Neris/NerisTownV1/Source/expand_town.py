"""Extend the accepted detailed town; preserve both earlier Blender revisions.

Run in background Blender with Neris-Town-Detailed.blend loaded.
"""
from pathlib import Path
import sys
import json
import math
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
assert bpy.app.background and Path(bpy.data.filepath).name=='Neris-Town-Detailed.blend'
sys.path[:0]=[str(ROOT/'Source'),str(ROOT.parent/'NerisBuildingsV1/Source')]
from expansion_architecture import Architecture, cypress, planter, flag
from residential_expansion import create_homes
from royal_district import create_castle, create_headquarters
from props import instance

# All new road/yard rectangles share one grid. Their union has no coplanar
# intersections, so crossings cannot reintroduce the reported tile flicker.
paved=[]
def paving(g,name,x,y,width,depth,z=.13):
    paved.append((x,y,width,depth))

def finish_paving(g):
    cells=set()
    for x,y,w,d in paved:
        for i in range(math.floor(x-w/2),math.ceil(x+w/2)):
            for j in range(math.floor(y-d/2),math.ceil(y+d/2)):
                cells.add((i,j))
    verts=[]; faces=[]; mortar=[]
    for x,y in sorted(cells):
        n=len(verts)
        verts.extend([(x+.025,y+.025,.212),(x+.975,y+.025,.212),
                      (x+.975,y+.975,.212),(x+.025,y+.975,.212)])
        faces.append((n,n+1,n+2,n+3))
        mortar.extend([(x,y,.19),(x+1,y,.19),(x+1,y+1,.19),(x,y+1,.19)])
    g.mesh('Unified Expanded Road Mortar',mortar,faces,'paving')
    obj=g.mesh('Unified Expanded Slate Tiles',verts,faces,'pavinglight')
    obj.data.materials.append(mats['paving'])
    for face in obj.data.polygons: face.material_index=int(g.rng.random()<.08)

names={'stone':'Ivory Limestone','trim':'Pale Carved Stone','gold':'Aged Gold',
       'brightgold':'Polished Gold Edge','roof':'Teal Enamel Roof','roofalt':'Teal Enamel Alternate',
       'cloth':'Neris Teal Banner','dark':'Recess Shadow','iron':'Patinated Iron','wood':'Dark Walnut',
       'glass':'Amber Window Glass','lamp':'Warm Lantern','crystal':'Cyan Relay Crystal',
       'crystaledge':'Crystal Bright Facets','leaf':'Moss Green Leaves','leaflight':'Fresh Green Leaves',
       'paving':'Town Paving','pavinglight':'Town Pavinglight','grass':'Town Grass',
       'water':'Town Water','foundation':'Town Foundation'}
mats={key:bpy.data.materials[name] for key,name in names.items()}
for key,rgb in [('paving',(.10,.14,.18)),('pavinglight',(.17,.225,.27))]:
    mat=mats[key]
    mat.diffuse_color=(*rgb,1)
    mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1)
    tint=mat.node_tree.nodes.get('Neris Texture Tint')
    if tint: tint.inputs[1].default_value=(*rgb,1)

# Make the existing ground continuous across the extension; remove former dividing walls.
for obj in list(bpy.data.objects):
    if obj.name.startswith(('Low Boundary Wall','Boundary Coping','North Boundary Wall')):
        bpy.data.objects.remove(obj,do_unlink=True)
for name,z,dz in [('Town Bedrock Plinth',-1.1,2),('Town Garden Ground',-.16,.3)]:
    obj=bpy.data.objects[name]
    obj.dimensions=(270,302,dz)
    obj.location=(0,31,z)
tower=bpy.data.objects['Neris-Communication-Tower Placement']
tower.location=(15,75,.16)
# Relocate the nearest old garden pavilion to leave breathing room west of City Hall.
old_pavilion=next(o for o in bpy.data.objects if o.name.startswith('Home 06'))
old_pavilion.location=(-24,-54,.2)

site=Architecture('05 Expanded District Streets',mats)
for x in [-132.5,132.5]:
    site.box('Outer Town Boundary',(x,31,.5),(.6,297,1),'stone',.02)
for y in [-117.5,179.5]:
    site.box('Outer Town Boundary',(0,y,.5),(265,.6,1),'stone',.02)
roads=[(0,60,255,9),(0,-59,255,7),(0,-91,179,6),
       (-59,8,6,107),(59,9,6,108),(-96,2,6,112),(91,2,6,112),
       (0,-70,8,49),(0,46,8,24),(-48,73,10,25),(66,75,11,30),
       (-92,-46,75,6),(-92,45,75,6),(92,-45,75,6),(92,45,75,6),
       (0,-114,174,5),(-80,-86,5,55),(80,-86,5,55)]
for i,(x,y,w,d) in enumerate(roads): paving(site,f'Expanded Street {i:02}',x,y,w,d,z=.13)
paving(site,'Open City Hall Civic Plaza',0,7,34,19,z=.13)
paving(site,'Tower Court',15,75,21,20,z=.13)

homes=create_homes(mats)
placements=[]
district=Architecture('06 Residential Neighborhoods',mats)
tree_templates=sorted([c for c in bpy.data.collections if c.name.startswith('Neris Detailed Tree ')],key=lambda c:c.name)
trees=Architecture('07 New Neighborhood Trees',mats)
tree_index=28

def tree(x,y,scale=1):
    global tree_index
    obj=instance(tree_templates[tree_index%2],trees.collection,f'Garden Tree {tree_index:03}',
                 (x,y,.03),tree_index*1.7,scale)
    tree_index+=1
    return obj

def home(style,x,y,lotw,lotd):
    instance(homes[style],district.collection,f'{style} Residence {len(placements)+1:02}',(x,y,.20))
    dims={'Large':(17,15),'Medium':(11.2,12),'Small':(9.3,9.5)}[style]
    placements.append({'style':style,'x':x,'y':y,'width':dims[0],'depth':dims[1],
                       'lotWidth':lotw,'lotDepth':lotd})
    paving(site,'Residential Front Walk',x,y-dims[1]/2-2.6,3,5.5,z=.13)
    if style=='Large':
        # Broad lawns, four shade trees, ornamental shrubs and a private garden walk.
        paving(site,'Estate Garden Walk',x,y-11,lotw-3,2.5,z=.13)
        for dx in [-lotw/2+3,lotw/2-3]:
            for dy in [-lotd/2+4,lotd/2-4]: tree(x+dx,y+dy,.96)
        for dx in [-10,10]:
            planter(site,x+dx,y-9,.15,1.2)
    elif style=='Medium':
        paving(site,'Family Home Court',x,y-7,lotw-3,3.8,z=.13)
        tree(x+lotw/2-2,y+lotd/2-3,.73)
    else:
        paving(site,'Workers Shared Court',x,y-6,lotw-1,3.0,z=.13)

for x in [-115,-77]:
    for y in [-23,22]: home('Large',x,y,34,40)
for x in [75,111]:
    for y in [-28,1,30]: home('Medium',x,y,26,25)
for x in [-66,-42,-18,18,42,66]:
    for y in [-77,-103]: home('Small',x,y,22,18)
for x,y in [(-86,-65),(86,-65),(-86,-108),(86,-108)]: tree(x,y,.70)
for x in [-121,-95,-70,-23,17,45,90,119]: tree(x,54,.92)
for x in [-15,15]:
    for y in [-1,12]: planter(site,x,y,.13,1.5)

castle=create_castle(mats)
military=create_headquarters(mats)
# Four non-overlapping moat bands surround the castle island and pass under its bridge.
water=[(-48,88,94,8),(-48,165,94,8),(-90.5,126.5,9,69),(-5.5,126.5,9,69)]
for i,(x,y,w,d) in enumerate(water):
    site.box('Castle Moat Lining',(x,y,-.19),(w+.4,d+.4,.2),'dark',0)
    site.box('Castle Moat Water',(x,y,.04),(w,d,.05),'water',0)
for x in [-96,0]:
    site.box('Moat Outer Bank',(x,126.5,.23),(.5,86,.45),'trim',.02)
for y in [83.7,169.3]:
    # Split front bank at the bridge.
    if y<100:
        for x in [-74.5,-21.5]: site.box('Moat Bank',(x,y,.23),(42.5,.5,.45),'trim',.02)
    else: site.box('Moat Bank',(-48,y,.23),(96,.5,.45),'trim',.02)
for x in [-107,10,116]:
    for y in [97,120,146,171]: tree(x,y,1.13)
finish_paving(site)

scene=bpy.context.scene
scene['Neris Expansion']='Royal precinct, three residential neighborhoods, open civic plaza'
scene['Neris Bounds']='-135,-120 to 135,182 meters'
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.render.resolution_x=1920; scene.render.resolution_y=1080
scene.render.resolution_percentage=75
# Preserve existing cameras, add wide and district-focused views.
for name,loc,target,lens in [
    ('Expansion Overview',(277,-370,315),(0,28,10),40),
    ('Royal District',(123,-39,143),(-4,124,17),46),
    ('Rich Neighborhood',(-158,-81,56),(-95,2,5),43),
    ('Middle Neighborhood',(172,-85,57),(94,6,4),45),
    ('Workers Neighborhood',(85,-168,45),(0,-88,4),48),
    ('City Hall Approach',(3,-37,19),(0,26,8),46)]:
    data=bpy.data.cameras.new(name); obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj); obj.location=loc
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    data.lens=lens; data.clip_end=3000
scene.camera=bpy.data.objects['Expansion Overview']
scene.timeline_markers.clear()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.clip_end=3000
            area.spaces.active.region_3d.view_distance=350
            area.spaces.active.region_3d.view_location=(0,35,0)
scene.world.color=(.15,.15,.15)
for image in bpy.data.images:
    if image.source=='FILE' and not image.packed_file: image.pack()
bpy.context.view_layer.update()
layout={'bounds':[-135,-120,135,182],'homes':placements,'roads':roads,'paving':paved,'water':water,
        'tower':[15,75],'cityHall':[0,26],'castle':[-48,127],'military':[66,125],
        'treeCount':len([o for o in bpy.data.objects if o.name.startswith('Garden Tree ')])}
(ROOT/'expansion-layout.json').write_text(json.dumps(layout,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Expanded.blend'),compress=True)
scene.render.filepath=str(ROOT/'Previews/Neris-Expanded-Overview.png')
bpy.ops.render.render(write_still=True)
print('EXPANDED NERIS',len(placements),'new homes, castle, headquarters,',layout['treeCount'],'trees',flush=True)
