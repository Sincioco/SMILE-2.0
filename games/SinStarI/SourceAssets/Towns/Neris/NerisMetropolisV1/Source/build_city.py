"""Reproducible asset construction; never reads or writes existing live towns."""
import json
import math
from pathlib import Path
import bpy
from geometry import Mesh
from landmarks import DESIGNS
from luma_landmarks import ORIGINALS
from landscape import GARDENS,BRIDGES,grove
from neighborhoods import neighborhood
from sphere_venue import sphere
import facades
from city_layout import EXTENT,LAND_RADIUS,LAKES,placements,roads
import presentation

ROOT=Path(__file__).resolve().parents[1]


def templates(collection):
    result={}
    factories=[(n,f) for n,_,f in DESIGNS+ORIGINALS]+GARDENS+BRIDGES
    factories += [('Neighborhood '+str(i),lambda i=i: neighborhood(i)) for i in range(16)]
    factories += [('Park Grove 4',lambda: grove(4,22)),('Park Grove 7',lambda: grove(7,9))]
    factories += [('Neris Sphere',sphere)]
    for name,factory in factories:
        mesh=facades.skin(factory())
        # Aviation beacons are exported geometry as well as Blender-visible lights.
        spires={'Burj Khalifa':[(0,0,828)],'Jeddah Tower':[(0,0,1000)],
            'Petronas Twin Towers':[(-44,0,452),(44,0,452)],'Merdeka 118':[(17,1,679)],
            'Makkah Royal Clock Tower':[(0,0,601)],'One World Trade Center':[(0,0,541)],
            'Taipei 101':[(0,0,508)],'Jin Mao Tower':[(0,0,421)],
            'Oriental Pearl Tower':[(0,0,468)],'Luma Crown Spire':[(0,0,1250)]}
        for x,y,z in spires.get(name,[]):
            mesh.cylinder(x,y,z,2.2,3.2,'Beacon',segments=12)
        obj=mesh.object(collection)
        obj['metropolis_template']=name
        obj['metropolis_solids']=json.dumps(mesh.solids)
        obj['metropolis_floors']=json.dumps(mesh.floors)
        result[name]=obj
    return result


def build():
    facades.generate()
    value=presentation.scene('Neris Metropolis')
    previous=bpy.data.collections.get('Metropolis Reusable Templates')
    if previous:
        for obj in list(previous.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(previous)
    library=bpy.data.collections.new('Metropolis Reusable Templates')
    library.use_fake_user=True
    models=templates(library)
    items=placements()
    for i,item in enumerate(items):
        obj=models[item['name']].copy()
        value.collection.objects.link(obj)
        obj.name=f"{item['name']} / {i:04}"
        obj.location=(item['x'],item['y'],2.1)
        obj.rotation_euler.z=item['yaw']
        obj.scale=(item['scale'],)*3
    surface=Mesh('Circular Landscape and Water')
    surface.box((0,0,1.085),(EXTENT*2,EXTENT*2,2),'Water')
    surface.cylinder(0,0,0,LAND_RADIUS,2.09,'Green',segments=384)
    for x,y,rx,ry in LAKES:
        surface.loft([(x,y,2.10,rx,ry,0),(x,y,2.185,rx,ry,0)],'Water',96)
    surface.object(value.collection)
    routes=Mesh('Concentric Avenues and Park Paths')
    for i,path in enumerate(roads()):
        points=[(x,y,2.30) for x,y in path]
        routes.road(points,72 if i==4 else 48,'Paving')
        routes.road([(x,y,z+.02) for x,y,z in points],60 if i==4 else 36,'Road')
    routes.object(value.collection)
    presentation.camera(value,(7300,-9500,9300),(0,0,200),8300)
    (ROOT/'Blender').mkdir(exist_ok=True)
    (ROOT/'Previews').mkdir(exist_ok=True)
    (ROOT/'Authoring').mkdir(exist_ok=True)
    value.render.filepath=str(ROOT/'Previews/city-layout-01.png')
    (ROOT/'Authoring/layout.json').write_text(json.dumps({'name':'Neris Metropolis',
        'extent_metres':EXTENT,'land_radius_metres':LAND_RADIUS,'items':items},indent=2))
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blender/Neris-Metropolis.blend'),compress=True)
    return {'scene':value.name,'placed_templates':len(items),'unique_templates':len(models),
            'width_metres':2*EXTENT,'preview':value.render.filepath}
