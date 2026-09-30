"""Luma travel spaces and Chapter One locations, using editable native templates.

Journey names are working geographic labels, not additions to accepted story canon.
Clearings reserve space for future encounters; these files do not implement combat.
"""
import math
import random
from town_design import Town, GROUND, WATER, ROAD


def base(name, size=720, style=1):
    t=Town(name,size,4,True)
    t.symmetric=False
    t.terrain_style=style
    t.night=False
    t.rect(-size/2,-size/2,size/2,size/2)
    return t


def distance(x,z,points):
    best=1e9
    for (a,b),(c,d) in zip(points,points[1:]):
        u=max(0,min(1,((x-a)*(c-a)+(z-b)*(d-b))/((c-a)**2+(d-b)**2)))
        best=min(best,math.hypot(x-a-u*(c-a),z-b-u*(d-b)))
    return best


def forest():
    t=base('Verdant Reach')
    path=[(-348,0),(-260,0),(-175,75),(-80,75),(0,-30),(100,-30),(190,80),(270,0),(348,0)]
    t.path(path,16)
    # Three connected battle clearings have open sight lines; the route winds between them.
    for x,z in ((-175,75),(0,-30),(190,80)):
        t.disk(x,z,38,ROAD)
    t.disk(-110,-155,45,WATER)
    t.disk(185,-175,32,WATER)
    rng=random.Random(1811)
    for row in range(23):
        for col in range(23):
            x=-325+col*29+rng.uniform(-6,6)
            z=-325+row*29+rng.uniform(-6,6)
            if distance(x,z,path)<40 or math.hypot(x+110,z+155)<61 or math.hypot(x-185,z+175)<48:
                continue
            t.place(18+(row+col)%2,x,z,rng.uniform(2.7,4.8),rng.uniform(0,360))
    for x,z in ((-265,165),(-30,240),(265,-215)):
        t.place(35,x,z,1.1,35)
    t.place(5,-105,121,1.4,180)
    t.lamps([(-242,26),(-193,116),(-10,8),(212,115)],1.7)
    t.gates(destinations=('Neris Waterworks','Greyglass Pass'))
    t.notes=['A wooded journey from the waterworks toward the eastern settlements.',
             'Three clearings reserved for encounters; monster spawning is separate gameplay.']
    return t


def mountains():
    t=base('Greyglass Pass',800,2)
    path=[(-388,0),(-300,0),(-240,-100),(-140,-100),(-75,55),(30,55),(115,-90),(225,-90),(300,0),(388,0)]
    t.path(path,18)
    for x,z in ((-185,-100),(0,55),(220,-90)):
        t.disk(x,z,31,ROAD)
    rng=random.Random(2480)
    for x in range(-320,321,80):
        for z in (-285,-200,200,285):
            t.place(35,x+rng.uniform(-12,12),z,rng.uniform(1.8,2.7),rng.uniform(0,360))
    for x,z in ((-330,115),(-195,25),(-35,-105),(135,100),(305,-125)):
        t.place(35,x,z,1.15,60)
    for x,z in ((-290,45),(-120,-155),(30,110),(240,-145),(320,45)):
        t.grove(x,z,2,2,11,2.5)
    t.place(38,40,137,.7,40)
    t.place(5,-240,-150,1.25,180)
    t.lamps([(-305,22),(-190,-74),(2,85),(221,-58),(320,25)],1.5)
    t.gates(destinations=('Verdant Reach','East Valley'))
    t.notes=['Rocky switchback route linking the forest to East Valley.',
             'Mountains are solid scenery; walking paths remain level and collision tested.']
    return t


def desert():
    t=base('Sunglass Expanse',720,3)
    path=[(-348,0),(-260,0),(-175,-95),(-40,-95),(80,-90),(190,50),(270,0),(348,0)]
    t.path(path,17)
    t.disk(0,30,40,WATER)
    t.ring(0,30,65,15)
    t.path([(-40,-95),(-40,-30)],15)
    for x,z in ((-260,210),(-80,265),(170,260),(275,195),(-250,-245),(5,-265),(250,-220)):
        t.place(37,x,z,1.4,(x+z)%360)
    for x,z in ((-305,130),(155,-200),(300,120),(-85,-185)):
        t.place(36,x,z,1.1,(x-z)%360)
    for angle in (0,45,90,135,180):
        a=math.radians(angle)
        t.place(19,91*math.cos(a),30+91*math.sin(a),3.2,angle)
    t.place(2,-91,30,1.6,90)
    t.place(17,-84,2,1.4,90)
    t.place(38,140,110,.95,35)
    t.lamps([(-87,55),(-97,6),(90,50),(125,-95)],1.7)
    t.gates(destinations=('Horizon Airport','Ancient Relay'))
    t.notes=['An exposed desert route with a sheltered oasis and ruined waymarks.',
             'Working geography for Luma; space reserved for travel encounters.']
    return t


def waterworks():
    t=base('Neris Waterworks',560,0)
    for z in (-160,0,160):
        for x in (-140,140):
            t.disk(x,z,57,WATER)
            t.ring(x,z,68,10)
    t.path([(-268,0),(268,0)],20)
    for x in (-220,0,220):
        t.path([(x,-240),(x,240)],16)
    for z in (-160,160):
        t.path([(-220,z),(220,z)],16)
    t.place(10,0,-215,1.3,180)
    t.place(9,0,215,1.2,0)
    for x in (-140,140):
        for z in (-160,0,160):
            # Pump pavilion on a solid central platform.
            t.disk(x,z,18)
            t.path([(x,z),(x+68,z)],8)
            t.place(27,x,z,1.6,0)
    for x in (-240,240):
        t.grove(x,90,2,3,10,2)
        t.grove(x,-90,2,3,10,2)
    t.lamps([(x,z) for x in (-28,28) for z in (-170,-80,80,170)],1.7)
    t.gates(destinations=('Neris Town','Verdant Reach'))
    t.notes=['Chapter One waterworks: paired settling basins, pump platforms and service lanes.',
             'Industrial machinery and scripted disaster sequence remain separate story work.']
    return t


def relay():
    t=base('Ancient Relay',560,3)
    t.ring(0,0,90,16)
    t.ring(0,0,180,12)
    t.path([(-268,0),(268,0)],18)
    t.path([(0,-200),(0,200)],18)
    t.place(10,0,0,2.2,180)
    for angle in (25,65,145,205,245,325):
        a=math.radians(angle)
        t.place(38,130*math.cos(a),130*math.sin(a),1.0,angle)
        t.place(27,65*math.cos(a),65*math.sin(a),1.5,angle)
    for x,z in ((-200,-190),(200,-190),(-200,190),(200,190)):
        t.place(36,x,z,1.4,45)
    t.lamps([(115,0),(-115,0),(0,115),(0,-115)],2)
    t.gates(destinations=('Sunglass Expanse','Neris Waterworks'))
    t.notes=['Chapter One ancient relay exterior; radial maintenance paths and weathered spires.',
             'Not the separate Chapter Five off-world observatory.']
    return t


def relief():
    t=base('Neris Relief Quarter',400,0)
    for z in (-130,0,130):
        t.path([(-188,z),(188,z)],16)
    for x in (-110,0,110):
        t.path([(x,-150),(x,150)],14)
    t.place(9,0,75,1.5,180)
    t.place(3,-62,78,1.8,180)
    t.place(2,62,78,1.8,180)
    for x in (-155,155):
        t.homes(x,(-95,-40,55,108),side=1 if x<0 else -1,template=5)
    for x in (-57,57):
        for z in (-85,-35):
            t.place(17,x,z,1.5,0)
    t.disk(60,-180,12,WATER)
    t.grove(-57,-178,5,1,12,2.2)
    t.grove(57,170,5,1,12,2.2)
    t.lamps([(x,z) for x in (-25,25) for z in (-110,-50,35,120)],1.5)
    t.gates(destinations=('Neris Town','Neris Waterworks'))
    t.notes=['Chapter One clinic and relief staging: medical hall, supply stalls and modest homes.',
             'Architecture is draft staging; does not replace accepted character or story canon.']
    return t


BUILDERS=(forest,mountains,desert,waterworks,relay,relief)
