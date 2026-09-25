"""Rebuild only the clean castle and its royal site, preserving other town edits.

Run in background Blender with the saved Neris-Town-Expanded.blend loaded.
The open interactive Blender session is never modified or saved by this script.
"""
from pathlib import Path
import json
import sys
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
assert bpy.app.background and Path(bpy.data.filepath).name=='Neris-Town-Expanded.blend'
sys.path[:0]=[str(ROOT/'Source'),str(ROOT.parent/'NerisBuildingsV1/Source')]
from royal_district import create_castle
from expansion_architecture import Architecture
from paving_plan import subtract, COMPARISON_MOAT, ROYAL_MOAT
from align_paving import apply as pave
from detail_grass import apply as grass

names={'stone':'Ivory Limestone','trim':'Pale Carved Stone','gold':'Aged Gold',
       'brightgold':'Polished Gold Edge','roof':'Teal Enamel Roof','roofalt':'Teal Enamel Alternate',
       'cloth':'Neris Teal Banner','dark':'Recess Shadow','iron':'Patinated Iron','wood':'Dark Walnut',
       'glass':'Amber Window Glass','lamp':'Warm Lantern','crystal':'Cyan Relay Crystal',
       'crystaledge':'Crystal Bright Facets','leaf':'Moss Green Leaves','leaflight':'Fresh Green Leaves',
       'paving':'Town Paving','pavinglight':'Town Pavinglight','grass':'Town Grass',
       'water':'Royal Deep Blue Water','foundation':'Town Foundation','bed':'Royal Deep Blue Moat Bed'}
mats={key:bpy.data.materials[name] for key,name in names.items()}
old=bpy.data.collections.get('Royal Castle of Neris')
if old:
    for obj in list(old.objects):bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(old)
castle=create_castle(mats)
castle.root.location=(-37,184,.152);castle.root.scale=(2,2,2)
castle.root['Reference Use']='Four images and Tripo silhouette studied; all local meshes constructed by castle_architecture/facades'
castle.root['Scale Comparison']='Twice original castle scale; comparable overall width and height to 170x Tripo'
# The new bridge deck meets the shared paving, rather than doubling its step height.
for obj in castle.collection.objects:
    if obj.name.startswith('Royal Bridge Deck'):obj.location.z=-.10
    elif obj.name.startswith('Bridge Bronze Inlay'):obj.location.z=.036
    elif obj.name.startswith('Bridge Diamond Emblem'):
        for spline in obj.data.splines:
            for point in spline.points:point.co.z=.045
military=bpy.data.objects['Neris Military Headquarters'];military.location=(115,172,.13)
tree_sites=iter([(69,96),(69,128),(68,220),(84,240),(124,240),(161,132),(161,177),(161,220),
                 (-104,275),(-68,275),(-12,275),(28,275),(161,96),(69,65)])
for obj in sorted(bpy.data.objects,key=lambda o:o.name):
    if obj.name.startswith('Garden Tree ') and -136<obj.location.x<63 and 92<obj.location.y<273:
        x,y=next(tree_sites);obj.location.x=x;obj.location.y=y

# Rebuild the terrain union once, with true openings for both moats. This also
# fills the former moat position and avoids coplanar overlapping extensions.
prefixes=('Town Garden Ground','Town Bedrock Plinth','Comparison Terrace','Castle Moat',
          'Moat Outer Bank','Moat Bank','Outer Town Boundary','Comparison Boundary','Royal Site ')
for obj in list(bpy.data.objects):
    if not obj.library and obj.name.startswith(prefixes):bpy.data.objects.remove(obj,do_unlink=True)
site=Architecture('Royal Site Terrain',mats)
for a,b,c,d in subtract([(-135,-120,178,283),(-327,52,-135,283)],ROYAL_MOAT+COMPARISON_MOAT):
    site.box('Royal Site Foundation',((a+c)/2,(b+d)/2,-1.1),(c-a,d-b,2),'foundation',0)
    site.box('Royal Site Lawn',((a+c)/2,(b+d)/2,-.16),(c-a,d-b,.30),'grass',0)
for a,b,c,d in ROYAL_MOAT:
    site.box('Castle Moat Lining',((a+c)/2,(b+d)/2,-.65),(c-a,d-b,.20),'bed',0)
    site.box('Castle Moat Water',((a+c)/2,(b+d)/2,.04),(c-a,d-b,.05),'water',0)
for x in [-132.3,58.3,-113.7,39.7]:
    site.box('Royal Site Moat Bank',(x,183,.23),(.5,170,.45),'trim',.01)
for y in [97.7,114.3,251.7,268.3]:
    spans=[(-132,-46),(-28,58)] if y<120 else [(-132,58)]
    for a,c in spans:site.box('Royal Site Moat Bank',((a+c)/2,y,.23),(c-a,.5,.45),'trim',.01)
for a,b,c,d in [(-327,52,-327,283),(-327,283,178,283),(178,-120,178,283),
                (-135,-120,178,-120),(-135,-120,-135,52),(-327,52,-135,52)]:
    site.box('Royal Site Boundary',((a+c)/2,(b+d)/2,.45),(max(.6,c-a),max(.6,d-b),.9),'stone',0)

layout=json.loads((ROOT/'expansion-layout.json').read_text(encoding='utf-8'))
layout['bounds']=[-327,-120,178,283]
layout['castle']=[-37,184];layout['military']=[115,172]
layout['royalRebuild']={'castle':[-37,184,.152],'scale':2,'military':[115,172,.13],
                        'construction':'New modeled geometry; no Tripo topology or textures reused'}
layout['water']=[[(a+c)/2,(b+d)/2,c-a,d-b] for a,b,c,d in ROYAL_MOAT]
(ROOT/'expansion-layout.json').write_text(json.dumps(layout,indent=2)+'\n',encoding='utf-8')
pave(layout);grass()
for name,pos,aim,energy in [('Royal Castle Spotlight',(-77,106,180),(-37,184,50),250000),
                           ('Military Headquarters Spotlight',(115,135,100),(115,172,15),100000)]:
    light=bpy.data.objects[name];light.location=pos
    light.rotation_euler=(Vector(aim)-light.location).to_track_quat('-Z','Y').to_euler()
    light.data.energy=energy
scene=bpy.context.scene
for name,pos,look,lens in [('Royal Rebuild Front',(-37,-100,115),(-37,183,49),46),
                          ('Royal Rebuild Detail',(-120,29,72),(-37,182,48),49),
                          ('Royal Rebuild Rear',(60,355,145),(-37,185,45),49),
                          ('Royal Rebuild Comparison',(-132,-210,188),(-132,179,45),34)]:
    camera=bpy.data.objects.get(name)
    if camera is None:
        camera=bpy.data.objects.new(name,bpy.data.cameras.new(name));scene.collection.objects.link(camera)
    camera.location=pos;camera.rotation_euler=(Vector(look)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.lens=lens;camera.data.clip_end=3000
scene.camera=bpy.data.objects['Royal Rebuild Comparison']
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
for screen in bpy.data.screens:
    for area in screen.areas:
        for space in area.spaces:
            if space.type=='VIEW_3D':space.clip_start=.5;space.clip_end=3000
bpy.data.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Expanded.blend'),compress=True)
for name in ['Royal Rebuild Front','Royal Rebuild Detail','Royal Rebuild Rear','Royal Rebuild Comparison']:
    scene.camera=bpy.data.objects[name];scene.render.filepath=str(ROOT/'Previews'/(name.replace(' ','-')+'.png'))
    bpy.ops.render.render(write_still=True)
