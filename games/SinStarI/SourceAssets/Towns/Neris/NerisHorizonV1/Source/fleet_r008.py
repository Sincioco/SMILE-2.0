"""Smooth original Neris aircraft: passenger, royal and enlarged heavy cargo."""
import bpy
import bmesh
import math
from mathutils import Matrix
import forms as F


def hull(name, stations, cx=0, z=13):
    vertices=[]
    for y,w,h in stations:
        for i in range(32):
            a=i*math.tau/32
            vertices.append((cx+w*math.cos(a),y,z+h*math.sin(a)))
    faces=[tuple(reversed(range(32)))]
    for row in range(len(stations)-1):
        for i in range(32):
            faces.append((row*32+i,row*32+(i+1)%32,(row+1)*32+(i+1)%32,(row+1)*32+i))
    faces.append(tuple(range(len(vertices)-32,len(vertices))))
    obj=F.mesh(name,vertices,faces,'Ivory')
    bm=bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    for poly in obj.data.polygons:
        poly.use_smooth=len(poly.vertices)==4
    return obj


def build(family, scale, origin):
    before=set(bpy.context.scene.objects)
    # Dense longitudinal sections and smooth normals remove the old angular hull.
    stations=[]
    for i in range(33):
        y=-48+3*i
        if y < -30:
            factor=.38+.62*math.sin((y+48)/18*math.pi/2)
        elif y > 16:
            factor=max(.03,math.cos((y-16)/32*math.pi/2))
        else:
            factor=1
        stations.append((y,14*factor,11*factor))
    body=hull(family+' Smooth Fuselage',stations)
    body.data.materials.append(F.MATERIALS['Teal'])
    body.data.materials.append(F.MATERIALS['Gold'])
    for face in body.data.polygons:
        if len(face.vertices)!=4:
            continue
        band=min(face.vertices)%32
        if band in (1,2,13,14):
            face.material_index=1
        elif band in (0,15):
            face.material_index=2

    # Curved dark cockpit fitted just above the nose surface.
    verts=[]
    for y in (21,24,27,30,33,36,39):
        factor=math.cos((y-16)/32*math.pi/2)
        for i in range(9):
            a=math.pi*(.30+.4*i/8)
            verts.append((14*factor*math.cos(a)*1.016,y,13+11*factor*math.sin(a)*1.016))
    cockpit=F.mesh(family+' Curved Cockpit',verts,[(r*9+i,r*9+i+1,(r+1)*9+i+1,(r+1)*9+i) for r in range(6) for i in range(8)],'Glass Dark')
    for p in cockpit.data.polygons:
        p.use_smooth=True
    for side in (-1,1):
        # A thick wing with a rounded perimeter and teal inset livery.
        points=[(side*9,7,10),(side*43,-25,9),(side*39,-35,9),(side*9,-29,10)]
        wing=F.mesh(family+' Swept Wing',points+[(x,y,z+1.4) for x,y,z in points],
                    [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'Ivory')
        bevel=wing.modifiers.new('Soft Aircraft Edges','BEVEL')
        bevel.width=.35
        bevel.segments=3
        F.panel(family+' Teal Wing Inset',[(side*15,-4,11.6),(side*38,-25,10.6),(side*35,-29,10.6),(side*15,-18,11.6)],'Teal')
        F.beam(family+' Gold Leading Edge',(side*10,7,11.55),(side*42,-25,10.55),.26,'Gold')
        F.star(family+' Neris Wing Emblem',side*23,-17,11.55,3.1)
        engine=hull(family+' Smooth Engine',[(-37,3.8,3.8),(-34,5.1,5.1),(-29,5.8,5.8),(-13,5.8,5.8),(-7,5.2,5.2),(-5,4.5,4.5)],side*28,12)
        for yy,material,radius in ((-5.05,'Metal',4.1),(-37.1,'Light',3.5),(-7.1,'Gold',5.23)):
            obj=F.cone(family+' Engine Rim',(side*28,yy,12),radius,.3,material,top=radius,sides=32)
            obj.rotation_euler.x=math.pi/2
        tail=[(side*5,-43,20),(side*7,-47,36),(side*8,-31,29),(side*6,-25,20)]
        F.panel(family+' Teal Tail Fin',tail,'Teal')
        for a,b in zip(tail,tail[1:]+tail[:1]):
            F.beam(family+' Tail Gold Trim',a,b,.22,'Gold')
        # Raised flag emblem on the outward fin side.
        F.star(family+' Neris Flag',0,0,0,2.1)
        emblem=bpy.context.scene.objects.get(family+' Neris Flag')
        if emblem:
            emblem.data.transform(Matrix.Translation((side*7.5,-37,27)) @ Matrix.Rotation(math.pi/2,4,'Y'))
            emblem.name=family+' Neris Tail Flag '+str(side)
        for yy in (-24,-16,-8,0,8):
            F.box(family+' Passenger Window',(side*13.95,yy,14.4),(.18,3.8,1.6),'Glass Dark')
        F.beam(family+' Landing Gear',(side*13,-18,7),(side*17,-18,1.8),1.2,'Metal')
        for yy in (-20,-16):
            wheel=F.cone(family+' Main Wheel',(side*17,yy,1.5),1.5,2.2,'Metal',top=1.5,sides=24)
            wheel.rotation_euler.y=math.pi/2
    F.beam(family+' Nose Gear',(0,32,7),(0,32,1.5),1.2,'Metal')
    wheel=F.cone(family+' Nose Wheel',(0,32,1.4),1.4,2,'Metal',top=1.4,sides=24)
    wheel.rotation_euler.y=math.pi/2
    if family=='Cargo':
        # High-volume freighter: wide teal loading-door panels and dorsal gold ribs.
        for side in (-1,1):
            F.box('Cargo Loading Door',(side*14.15,-14,13),(.2,23,8),'Teal')
            for yy in (-26,-2):
                F.beam('Cargo Door Gold Frame',(side*14.3,yy,9),(side*14.3,yy,17),.28,'Gold')
        F.box('Cargo Dorsal Spine',(0,-10,24.2),(3,48,.4),'Teal')
    objects=list(set(bpy.context.scene.objects)-before)
    matrix=Matrix.Translation(origin) @ Matrix.Rotation(math.pi,4,'Z') @ Matrix.Scale(scale,4)
    for obj in objects:
        obj.matrix_world=matrix @ obj.matrix_world
        obj['horizon_asset']=True
        obj['fleet']=family
        obj['fleet_origin']=list(origin)
        obj['assembly']=family+' Aircraft'
    return objects


def apply(scene):
    for obj in list(scene.objects):
        if obj.get('fleet') or obj.get('runway_aircraft_preview') or obj.name.startswith('Runway Aircraft'):
            bpy.data.objects.remove(obj,do_unlink=True)
    transport=build('Transport',1,(-365,-90,0))
    build('Royal',.82,(365,108,0))
    cargo=build('Cargo',1.30,(0,0,0))
    # Keep the cargo's export geometry local; linked preview instances show its
    # actual runway orientation without rebaking the native model's nose axis.
    for obj in cargo:
        obj.hide_render=True
        obj.hide_viewport=True
    for source,origin,position,angle,scale in ((transport,(-365,-90,0),(-380,355,.14),math.pi/2,.55),
                                             (cargo,(0,0,0),(200,270,.14),-math.pi/2,1)):
        anchor=bpy.data.objects.new('Runway Aircraft Preview',None)
        scene.collection.objects.link(anchor)
        anchor.location=position
        anchor.rotation_euler.z=angle
        anchor.scale=(scale,)*3
        anchor['runway_aircraft_preview']=True
        for original in source:
            obj=original.copy()
            obj.data=original.data
            scene.collection.objects.link(obj)
            obj.parent=anchor
            obj.matrix_basis=Matrix.Translation(tuple(-v for v in origin)) @ original.matrix_world
            obj.hide_render=False
            obj.hide_viewport=False
            obj.name='Runway Aircraft '+original.name
            obj['horizon_asset']=False
            obj['runway_aircraft_preview']=True
    # Both hangar craft retain their -Y Blender / -Z native nose, pointing out
    # through the hangar openings toward the front apron. Building yaw is unchanged.
