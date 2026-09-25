"""Keep both castle candidates in the editable town, connected by the royal avenue."""
from pathlib import Path
import json
import sys
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
assert bpy.app.background and Path(bpy.data.filepath).name=='Neris-Town-Expanded.blend'
assert 'royalRebuild' not in json.loads((ROOT/'expansion-layout.json').read_text()), \
    'The royal rebuild owns the current larger site. Use refine_castle.py instead.'
sys.path[:0]=[str(ROOT/'Source'),str(ROOT.parent/'NerisBuildingsV1/Source')]
from expansion_architecture import Architecture
from align_paving import apply as pave
from detail_grass import apply as grass
from tree_variation import apply as vary_trees
from paving_plan import subtract, COMPARISON_MOAT

for obj in list(bpy.data.objects):
    if not obj.library and obj.name.startswith(('Neris Tripo Castle','Comparison Terrace','Comparison Boundary','Comparison Moat','Comparison Bridge')):
        bpy.data.objects.remove(obj,do_unlink=True)
bpy.data.orphans_purge(do_recursive=True)
castle=ROOT.parent/'TripoCastleV1/Blend/Neris-Castle-Cleaned.blend'
with bpy.data.libraries.load(str(castle),link=True) as (source,target):
    target.collections=['Neris Tripo Castle Architecture']
obj=bpy.data.objects.new('Neris Tripo Castle Comparison',None)
obj.instance_type='COLLECTION';obj.instance_collection=target.collections[0]
bpy.context.scene.collection.objects.link(obj)
target.collections[0].library.filepath=bpy.path.relpath(str(castle))
# Ground the entrance deck, not the low stray/back vertices of the Tripo export.
obj.location=(-228,176,.212-.19*170);obj.scale=(170,170,170)
obj['Comparison']='Tripo candidate; accepted procedural castle remains to the east'
mats={k:bpy.data.materials[n] for k,n in [('foundation','Town Foundation'),('grass','Town Grass'),('stone','Ivory Limestone'),
    ('water','Royal Deep Blue Water'),('bed','Royal Deep Blue Moat Bed')]}
site=Architecture('08 Castle Comparison',mats)
# Extend only the missing western land; no coplanar overlap with the old ground.
for a,b,c,d in subtract([(-327,52,-135,283),(-135,182,-131,283)],COMPARISON_MOAT):
    site.box('Comparison Terrace Base',((a+c)/2,(b+d)/2,-1.1),(c-a,d-b,2),'foundation',0)
    site.box('Comparison Terrace Lawn',((a+c)/2,(b+d)/2,-.16),(c-a,d-b,.3),'grass',0)
for a,b,c,d in COMPARISON_MOAT:
    site.box('Comparison Moat Lining',((a+c)/2,(b+d)/2,-.65),(c-a,d-b,.2),'bed',0)
    site.box('Comparison Moat Water',((a+c)/2,(b+d)/2,.04),(c-a,d-b,.05),'water',0)
for x,depth in [(-317.3,198),(-138.7,198),(-305.3,174),(-150.7,174)]:
    site.box('Comparison Moat Bank',(x,176,.18),(.5,depth,.36),'stone',0)
for y in [76.7,89.3,262.7,275.3]:
    segments=[(-317,-236),(-220,-139)] if y<100 else [(-317,-139)]
    for a,c in segments:site.box('Comparison Moat Bank',((a+c)/2,y,.18),(c-a,.5,.36),'stone',0)
# A supported crossing meets the imported entrance deck at the paving height.
site.box('Comparison Bridge Deck',(-228,85,.07),(16,26,.24),'stone',0)
for x in [-235.7,-220.3]:
    site.box('Comparison Bridge Rail',(x,83,1.05),(.55,22,1.7),'stone',.03)
    for y in [78,88]:site.box('Comparison Bridge Pier',(x,y,-.21),(1.2,1.2,.85),'stone',0)
for old in list(bpy.data.objects):
    if old.name.startswith('Outer Town Boundary') and abs(old.location.x+132.5)<.1:
        bpy.data.objects.remove(old,do_unlink=True)
site.box('Comparison Boundary West',(-326.5,167.5,.5),(.6,231,1),'stone',0)
site.box('Comparison Boundary North',(-231,282.5,.5),(192,.6,1),'stone',0)
site.box('Comparison Boundary East',(-131.3,232,.5),(.6,100,1),'stone',0)
layout=json.loads((ROOT/'expansion-layout.json').read_text())
layout['bounds']=[-327,-120,135,283]
layout['comparisonCastle']={'position':[-228,176],'scale':170,'groundAnchor':.19,'candidate':'Tripo','source':'../TripoCastleV1/Blend/Neris-Castle-Cleaned.blend'}
layout['comparisonWater']=[[(a+c)/2,(b+d)/2,c-a,d-b] for a,b,c,d in COMPARISON_MOAT]
(ROOT/'expansion-layout.json').write_text(json.dumps(layout,indent=2)+'\n')
pave();vary_trees();grass()
scene=bpy.context.scene
for name,position,aim,power in [
        ('Tripo Castle Spotlight',(-250,150,150),(-228,176,35),180000),
        ('Royal Castle Spotlight',(-68,88,105),(-48,127,25),90000),
        ('Military Headquarters Spotlight',(66,90,80),(66,125,12),70000)]:
    light=bpy.data.objects.get(name)
    if light is None:
        light=bpy.data.objects.new(name,bpy.data.lights.new(name,'SPOT'))
        scene.collection.objects.link(light)
    light.location=position
    light.rotation_euler=(Vector(aim)-light.location).to_track_quat('-Z','Y').to_euler()
    light.data.energy=power;light.data.color=(1.0,.91,.8)
    light.data.spot_size=2.094395;light.data.spot_blend=.5;light.data.shadow_soft_size=6
scene['Neris Bounds']='-327,-120 to 135,283; western comparison terrace begins at Y52'
for name,pos,look,ortho in [('Castle Comparison',(-174,-90,215),(-151,174,37),False),
                           ('Castle Entrance',(-262,39,10),(-228,106,7),False),
                           ('Connected Streets Plan',(-96,81.5,580),(-96,81.5,0),True),
                           ('Canal Repair',(41,-53,48),(0,-7,0),False)]:
    cam=bpy.data.objects.get(name)
    if cam is None:
        cam=bpy.data.objects.new(name,bpy.data.cameras.new(name));scene.collection.objects.link(cam)
    cam.location=pos;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.clip_end=3000;cam.data.lens=40
    if ortho:cam.data.type='ORTHO';cam.data.ortho_scale=480
scene.camera=bpy.data.objects['Castle Comparison']
assert any(i.object.type=='MESH' and i.object.name.startswith('Neris Tripo Castle')
           for i in bpy.context.evaluated_depsgraph_get().object_instances), 'Castle library instance missing'
for image in bpy.data.images:
    if image.source=='FILE' and not image.library and not image.packed_file:image.pack()
bpy.data.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Expanded.blend'),compress=True)
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
for name in ['Castle Comparison','Castle Entrance','Connected Streets Plan','Canal Repair']:
    scene.camera=bpy.data.objects[name];scene.render.filepath=str(ROOT/'Previews'/(name.replace(' ','-')+'.png'))
    bpy.ops.render.render(write_still=True)
