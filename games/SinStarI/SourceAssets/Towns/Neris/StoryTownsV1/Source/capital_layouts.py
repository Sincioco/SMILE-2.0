"""Three contrasting, bilateral Neris capital candidates using existing assets."""
import math
from town_design import Town, GROUND, WATER, ROAD


def canals():
    t = Town('Neris Canals', smooth=True)
    t.rect(-320,-330,320,330)
    for side in (-1,1):
        t.rect(side*80-12,-300,side*80+12,210,WATER)
    for z in (-180,-60,90,210):
        t.rect(-300,z-10,300,z+10,WATER)
    t.rect(-85,150,85,325)
    t.path([(0,-330),(0,178)],18)
    for x in (-305,-180,180,305):
        t.path([(x,-315),(x,315)],12)
    for z in (-315,-240,-120,0,150,315):
        t.path([(-345,z),(345,z)],12 if z else 20)
    # Detours around civic and relay landmarks keep the ceremonial avenue walkable.
    t.ring(0,60,36,12)
    t.ring(0,-120,28,12)
    t.place(13,0,255,1.7)
    t.place(9,0,60,1.6)
    t.place(10,0,-120,1.5)
    for side in (-1,1):
        t.homes(side*146,(-282,-207,-165,-87,-32,45,108,186),side)
        t.homes(side*218,(-282,-207,-165,-87,-32,45,108,186),-side,3)
        t.place(3,side*215,270,1.8,side*90)
        for z in (-280,-208,-85,65,180,280):
            t.grove(side*276,z,3,3,10,1.8)
        for z in range(-300,146,30):
            if z in (-180,-60,90):
                continue
            if abs(z+120)>22 and abs(z-60)>35 and abs(z)<18:
                continue
            t.place(19,side*46,z,2.0,side*25)
        t.grove(side*118,265,2,4,12,2.2)
        for template,z in ((8,-275),(11,-232),(12,-198)):
            t.place(template,side*40,z,1.3,side*90)
        for z in (-30,130):
            t.disk(side*42,z,13,ROAD)
            t.path([(0,z),(side*42,z)],8)
            t.place(20,side*42,z,1.5)
            t.place(14,side*59,z,1.4,-side*90)
        for z in (-300,-255,-195,-150,-90,-30,15,105,150):
            t.place(15,side*17,z,1.8)
        for z in (-300,-220,-150,-75,30,100,180,275):
            t.place(15,side*190,z,1.6)
        for z in (-290,-200,-100,45,170,290):
            t.place(15,side*315,z,1.7)
        for z in (-285,-165,-30,125,190,300):
            t.place(16,side*99,z,1.8)
    t.gates()
    t.notes=['Ceremonial castle approach and four cross-canals',
        'Public water gardens beside the relay tower',
        'Paired repair-market streets and quiet residential banks']
    return t


def polar(radius, angle):
    a=math.radians(angle)
    return round(radius*math.sin(a),5), round(radius*math.cos(a),5)


def star_lake():
    t=Town('Neris Star Lake', smooth=True)
    t.disk(0,0,325)
    t.disk(0,0,245,WATER)
    t.disk(0,0,164)
    t.ring(0,0,278,16)
    t.ring(0,0,149,12)
    for a in range(0,360,45):
        t.path([polar(149,a),polar(316,a)],14)
    t.path([(-348,0),(348,0)],16)
    t.path([(0,-320),(0,55)],16)
    t.ring(0,-65,29,10)
    t.place(13,0,51,1.5)
    t.place(9,0,-65,1.4)
    for side in (-1,1):
        t.place(10,side*83,-47,1.15)
        for z in (-70,-30,10,50):
            t.place(19,side*108,z,1.55)
        for template,x in ((8,25),(11,55),(12,83)):
            t.place(template,side*x,-91,1.0)
        for a in (45,135):
            x,z=polar(201,a)
            x*=side
            t.disk(x,z,34)
            t.path([polar(165,a*side),polar(238,a*side)],14)
            t.grove(x-side*15,z+15,2,2,8,1.8)
            t.grove(x+side*15,z-15,2,2,8,1.8)
    # Homes face the orbital promenade; the inward ring stays mostly open water.
    for a in range(0,360,30):
        for offset in (-9,9):
            angle=a+offset
            x,z=polar(302,angle)
            t.place(3 if a%60==0 else 4,x,z,1.35,angle)
    for a in range(0,360,10):
        x,z=polar(319,a+5)
        t.place(18 if a%20 else 19,x,z,1.8,180-a)
    for a in range(0,360,15):
        x,z=polar(258,a+7.5)
        t.place(18,x,z,1.7,a)
        x,z=polar(267,a+7.5)
        t.place(15,x,z,1.8)
    for a in range(0,360,45):
        for radius in (174,228):
            for offset in (-4,4):
                x,z=polar(radius,a+offset)
                t.disk(x,z,5)
                t.place(15,x,z,2.0)
    for a in range(0,360,30):
        x,z=polar(158,a+15)
        t.place(15,x,z,1.6)
    for a in (22.5,157.5,202.5,337.5):
        x,z=polar(290,a)
        t.place(20,x,z,1.45)
        x,z=polar(314,a)
        t.place(16,x,z,1.8)
    t.gates()
    t.notes=['Eight luminous bridge spokes across a vast reservoir',
        'An island palace and circular civic promenade',
        'Relay maintenance gardens and an outer ring of homes']
    return t


def crown_isles():
    t=Town('Neris Crown Isles',780,smooth=True)
    centers=[(x,z) for x in (-228,0,228) for z in (-228,0,228)]
    for x,z in centers:
        t.disk(x,z,100)
    t.rect(-72,140,72,310)
    for x,z in centers:
        t.ring(x,z,69,12)
    for x in (-228,0,228):
        t.path([(x,-330),(x,300)],16)
    for z in (-228,0,228):
        t.path([(-365,z),(365,z)],16)
    t.place(13,0,242,1.5)
    t.place(9,0,37,1.65)
    t.place(10,0,-38,1.55)
    for side in (-1,1):
        t.place(7,side*228,244,1.2,0)
        # Twin northern academies/garrisons frame the single palace.
        for z in (160,195,285):
            t.grove(side*288,z,2,2,10,2.2)
        for z in (175,295):
            t.grove(side*168,z,2,2,10,2.0)
        for template,z in ((8,-39),(11,0),(12,39)):
            t.place(template,side*263,z,1.15,side*90)
            t.place(17,side*201,z,1.6,-side*90)
        t.place(20,side*43,0,1.7)
        t.grove(side*44,-43,2,2,10,1.8)
        t.grove(side*44,43,2,2,10,1.8)
    for x in (-228,0,228):
        for side in (-1,1):
            for dz in (-39,0,39):
                t.place(3 if dz else 4,x+side*42,-228+dz,1.3,side*90)
            for dz in (-66,66):
                t.grove(x+side*37,-228+dz,3,1,10,1.9)
    for x,z in centers:
        for dx,dz in ((-82,-32),(82,-32),(-82,32),(82,32)):
            t.place(19,x+dx,z+dz,2.0)
        for dx,dz in ((-61,-61),(61,-61),(-61,61),(61,61)):
            t.place(15,x+dx,z+dz,2.2)
    for side in (-1,1):
        for x,z in ((114,0),(114,-228),(114,228),(228,-114),(228,114)):
            for off in (-12,12):
                t.disk(side*x,z+off,4)
                t.place(15,side*x,z+off,2.2)
    t.gates()
    t.notes=['Nine garden islands separated by deep, clear waterways',
        'Long illuminated processional bridges',
        'Royal island, market quays and southern residential villages']
    return t
