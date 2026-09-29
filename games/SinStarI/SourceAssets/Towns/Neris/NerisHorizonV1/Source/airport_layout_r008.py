"""Two connected rear airstrips and a closer control tower; no rear glass halls."""
import bpy
import math
from mathutils import Vector
import forms as F


def apply(scene):
    for obj in list(scene.objects):
        assembly = obj.get('assembly', '')
        if obj.name in ('Horizon Name Panel','HORIZON','SPACEPORT','NERIS') or obj.name.startswith('Quay Coping'):
            bpy.data.objects.remove(obj, do_unlink=True)
        elif assembly in ('Transport Glass Halls', 'Royal Glass Hall', 'Rear Hall Gardens', 'Rear Runway And Taxiway'):
            bpy.data.objects.remove(obj, do_unlink=True)
        elif assembly in ('Centered Rear Tower', 'Tower Neris Relief'):
            obj.location.y -= 40
        elif assembly == 'Transport Hangar':
            obj.location.y -= 18
        elif assembly == 'Royal Hangar':
            obj.location.y -= 13
        elif obj.get('horizon_internal_light') and 'Hall' in obj.name:
            bpy.data.objects.remove(obj, do_unlink=True)
        elif obj.name.startswith(('Wave Roof Gold Standing Seam', 'Transport Hangar Roof Gold Standing Seam',
                                  'Royal Hangar Roof Gold Standing Seam')):
            obj.scale.x *= 3
            obj.location.z += .25

    # Keep all gold-ornament bays opaque, and retain the original roof skylights.
    tint = F.material('Terminal Tint', (.04,.23,.30), .1,.23,.025)
    tint.name = 'GW Terminal Tint'
    tint.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value = .43
    clear_count = 0
    for obj in scene.objects:
        if not obj.name.startswith(('Terminal Glazing', 'Terminal End Glazing')):
            continue
        points = [obj.matrix_world @ Vector(p) for p in obj.bound_box]
        x = sum(p.x for p in points) / 8
        ornament = abs(x) < 39 or any(abs(abs(x)-center) < 13.1 for center in (65,117,169,221))
        if not ornament:
            obj.data.materials.clear()
            obj.data.materials.append(tint)
            clear_count += 1

    import roof_sign_r008
    roof_sign_r008.apply(scene)
    for x,z in ((-17,256.15),(17,265.15)):
        housing = F.cone('Antenna Beacon Housing',(x,194,z),.8,.3,'Metal',top=.8,sides=12)
        housing['horizon_asset'] = True
        housing['assembly'] = 'Centered Rear Tower'

    before = set(scene.objects)
    for runway, cy in enumerate((355,270)):
        F.box('Runway '+str(runway+1)+' Asphalt', (0,cy,.06),(900,50,.12),'Runway Asphalt')
        for y in (cy-24,cy+24):
            F.box('Runway White Edge',(0,y,.145),(896,.9,.035),'Ivory')
        for x in range(-300,301,45):
            F.box('Runway Center Dash',(x,cy,.155),(24,1.0,.035),'Ivory')
        for end in (-1,1):
            F.box('Runway Threshold',(end*423,cy,.155),(1.8,46,.035),'Ivory')
            for y in (-19,-14,-9,-4,4,9,14,19):
                F.box('Threshold Piano Key',(end*408,cy+y,.155),(23,2,.035),'Ivory')
            for y in (-14,14):
                F.box('Runway Aiming Point',(end*260,cy+y,.155),(30,4,.035),'Ivory')
            data=bpy.data.curves.new('Runway Number','FONT')
            data.body=('36' if end<0 else '18')+('L' if (runway==0)==(end<0) else 'R')
            data.size=12
            data.align_x='CENTER'
            data.align_y='CENTER'
            data.extrude=.012
            obj=bpy.data.objects.new(data.name,data)
            scene.collection.objects.link(obj)
            obj.location=(end*365,cy,.18)
            obj.rotation_euler.z=-end*math.pi/2
            data.materials.append(F.MATERIALS['Ivory'])
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            bpy.context.view_layer.objects.active=obj
            bpy.ops.object.convert(target='MESH')
            obj.select_set(False)
        for x in range(-420,421,60):
            for y in (cy-26,cy+26):
                F.box('Flush Runway Edge Light',(x,y,.17),(.7,.7,.08),'Warm Light')
        for x in range(-330,331,30):
            F.box('Flush Runway Center Light',(x,cy,.20),(.5,.5,.06),'Warm Light')
        for x in (-426,426):
            for y in range(-20,21,5):
                F.box('Flush Threshold Light',(x,cy+y,.20),(.7,.7,.06),'Light')
    for end in (-1,1):
        for i in range(48):
            a,b=math.pi*i/48,math.pi*(i+1)/48
            def point(t,r,z=.12):
                return (end*(360+r*math.sin(t)),312.5+r*math.cos(t),z)
            polygon=[point(a,33.5),point(a,51.5),point(b,51.5),point(b,33.5)]
            # Clip to the gap so no near-coplanar asphalt overlaps either strip.
            for boundary,keep_above in ((295,True),(330,False)):
                clipped=[]
                for p,q in zip(polygon,polygon[1:]+polygon[:1]):
                    inside_p=p[1]>=boundary if keep_above else p[1]<=boundary
                    inside_q=q[1]>=boundary if keep_above else q[1]<=boundary
                    if inside_p:
                        clipped.append(p)
                    if inside_p != inside_q:
                        t=(boundary-p[1])/(q[1]-p[1])
                        clipped.append((p[0]+t*(q[0]-p[0]),boundary,.12))
                polygon=clipped
            if len(polygon)>=3:
                if end<0:
                    polygon.reverse()
                F.mesh('Runway U Turn Road',polygon,[tuple(range(len(polygon)))],'Runway Asphalt')
            F.beam('U Turn Gold Centerline',point(a,42.5,.25),point(b,42.5,.25),.40,'Gold',.025)
    for obj in set(scene.objects)-before:
        obj['horizon_asset']=True
        obj['assembly']='Twin Rear Runways'
        if obj.name.startswith(('Runway White','Runway Center','Runway Threshold','Threshold Piano',
                                'Runway Aiming','Runway Number')):
            obj.location.z += .10
    return clear_count
