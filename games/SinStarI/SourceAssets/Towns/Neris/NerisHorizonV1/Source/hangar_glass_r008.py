"""Terminal-style vertical glass walls at 80 percent opacity; hangar fronts stay open."""
import bpy
import math
import forms as F


def apply(scene):
    glass = F.material('Hangar Glass', (.04,.23,.30), .1,.28)
    glass.name = 'GW Hangar Glass'
    glass.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value = .8
    glass.surface_render_method = 'DITHERED'
    for name,cx,half,front,back,wall in [('Transport Hangar',-365,77,33,147,46),
                                       ('Royal Hangar',365,48,43,147,32)]:
        for obj in list(scene.objects):
            if obj.get('assembly') == name and any(obj.name.startswith(name + suffix)
                    for suffix in (' Wall',' Back',' Side Roof Infill',' Rear Wave Infill')):
                bpy.data.objects.remove(obj, do_unlink=True)
        before = set(scene.objects)
        def height(x):
            u = abs((x-cx)/half)
            return wall + 1.6 + 11*math.exp(-(u/.34)**2) + 4.5*math.exp(-((u-.86)/.2)**2) - 1.2
        sides = [((cx-half+4,front),(cx-half+4,back)),
                 ((cx+half-4,front),(cx+half-4,back)),
                 ((cx-half+4,back),(cx+half-4,back))]
        for a,b in sides:
            length = math.dist(a,b)
            count = math.ceil(length/13)
            for i in range(count):
                x,y = (a[j]+(b[j]-a[j])*i/count for j in (0,1))
                nx,ny = (a[j]+(b[j]-a[j])*(i+1)/count for j in (0,1))
                F.mesh(name+' Vertical Glass',[(x,y,.8),(nx,ny,.8),
                       (nx,ny,height(nx)),(x,y,height(x))],[(0,1,2,3)],'Hangar Glass')
                F.beam(name+' Glass Mullion',(x,y,.5),(x,y,height(x)),.65,'Ivory')
                for z in (12.5,25.7,39):
                    if z < min(height(x),height(nx))-1:
                        F.beam(name+' Glass Transom',(x,y,z),(nx,ny,z),.28,'Gold')
            F.beam(name+' Glass Mullion',(b[0],b[1],.5),(b[0],b[1],height(b[0])),.65,'Ivory')
        for obj in set(scene.objects)-before:
            obj['horizon_asset'] = True
            obj['assembly'] = name
