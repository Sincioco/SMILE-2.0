"""Three contrasting, bilateral Neris capital candidates using existing assets."""
import math
from town_design import Town, GROUND, WATER, ROAD


def canals():
    t = Town('Neris Canals', smooth=True)
    # Four long points repeat the Neris emblem. Separate triangular quays leave
    # open canals between the central star and the residential districts.
    t.rect(-62,-62,62,62)
    for side in (-1,1):
        t.triangle([(-62,side*62),(0,side*336),(62,side*62)])
        t.triangle([(side*62,-62),(side*336,0),(side*62,62)])
        for bank in (-1,1):
            t.triangle([(side*91,bank*91),(side*291,bank*91),(side*91,bank*291)])
    t.path([(-345,0),(345,0)],16)
    t.path([(0,-322),(0,322)],14)
    for side in (-1,1):
        for bank in (-1,1):
            # One bridge reaches each quay. Its compact street triangle is well
            # inside the shoreline, leaving clear plots beside the pavement.
            t.path([(side*120,0),(side*120,bank*220),(side*220,bank*120),
                    (side*120,bank*120)],10)
            t.place(9 if bank==1 else 3,side*153,bank*153,1.35,0 if bank==1 else 180)
            for x,z in ((151,106),(196,106),(106,174),(106,226)):
                t.place(4,side*x,bank*z,1.05)
            for x,z in ((170,192),(212,147),(96,258)):
                t.place(19,side*x,bank*z,1.65)
            t.place(20,side*101,bank*101,1.4)
            t.lamps([(side*132,bank*110),(side*110,bank*190),
                     (side*188,bank*142)],1.8)
        for z in (-220,-150,-78,78,150,220):
            t.place(19,side*20,z,1.8)
        for z in (-260,-180,-100,100,180,260):
            t.place(15,side*12,z,1.8)
        t.place(10,side*40,35,1.4,0)
        t.place(8,side*40,-35,1.3,180)
        for x in (85,170,250):
            t.lamps([(side*x,-13),(side*x,13)],1.8)
    t.gates()
    t.notes=['Four-point Neris star with long ceremonial avenues',
        'Triangular residential quays separated by open canals',
        'Four short bridge approaches and clear waterfront streets']
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
        t.path([polar(150,a),polar(316,a)],14)
    # The cross-island street passes in front of the drawbridge, not under its rails.
    t.path([(-348,0),(-149,0),(-112,-42),(112,-42),(149,0),(348,0)],16)
    t.path([(0,-320),(0,-94)],16)
    t.path([(0,-36),(0,-28)],16)
    t.ring(0,-65,29,10)
    t.place(13,0,51,1.5)
    t.place(9,0,-65,1.4)
    for side in (-1,1):
        t.place(10,side*83,-65,1.15,side*90)
        for z in (-70,-30,10,50):
            t.place(19,side*108,z,1.55)
        for a in (45,135):
            x,z=polar(201,a)
            x*=side
            t.disk(x,z,34)
            t.path([polar(165,a*side),polar(238,a*side)],14)
            angle=math.radians(a*side)
            for offset in (-20,20):
                t.grove(x+offset*math.cos(angle),z-offset*math.sin(angle),2,2,8,1.8)
    # Homes face the orbital promenade; the inward ring stays mostly open water.
    for a in range(0,360,30):
        for offset in (-9,9):
            angle=a+offset
            x,z=polar(302,angle)
            t.place(3 if a%60==0 else 4,x,z,1.35,angle)
    for a in range(0,360,10):
        angle=a+5
        if angle % 45 == 0:
            angle += 5
        x,z=polar(319,angle)
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
    # Seven tangent arcs form an almost circular breakwater with a southern
    # opening. The independent palm trunk enters through that open water.
    arcs=[]
    for index in range(7):
        angle=-157.5+index*45
        arcs.append([polar(340,angle),polar(340/math.cos(math.pi/8),angle+22.5),
                     polar(340,angle+45)])
    for points in arcs:
        t.curve(points,46)
    t.curve([(0,-365),(0,-40),(0,252)],56)
    fronds=[]
    for z,extent,end_z in ((-224,208,-163),(-162,255,-95),(-100,284,-16),
                           (-38,283,67),(24,250,145),(86,198,211),(148,128,260)):
        for side in (-1,1):
            points=[(0,z),(side*extent*.76,z),(side*extent,end_z)]
            fronds.append(points)
            t.curve(points,37)
    # Roads follow the same smooth center curves. Two short curved bridges at
    # the trunk base connect the perimeter without cutting through any frond.
    for points in arcs:
        t.curve(points,10,ROAD)
    t.path([(0,-370),(0,224)],12)
    for side in (-1,1):
        t.curve([polar(340,side*157.5),(side*75,-356),(0,-335)],10,ROAD)
    for points in fronds:
        t.curve(points,8,ROAD)
        (ax,az),(bx,bz),(cx,cz)=points
        for index,amount in enumerate((.38,.60,.81)):
            u=1-amount
            x,z=u*u*ax+2*u*amount*bx+amount*amount*cx,u*u*az+2*u*amount*bz+amount*amount*cz
            dx,dz=2*(u*(bx-ax)+amount*(cx-bx)),2*(u*(bz-az)+amount*(cz-bz))
            length=math.hypot(dx,dz)
            # Mirror the homes on the upper bank, safely between road and shore.
            side=1 if cx>0 else -1
            nx,nz=-dz/length*side,dx/length*side
            t.place((5,0,4)[index],x+nx*11,z+nz*11,.9,
                    math.degrees(math.atan2(nx,nz))%360)
            t.place(19,x-nx*11,z-nz*11,1.3)
        for amount in (.52,.90):
            u=1-amount
            x,z=u*u*ax+2*u*amount*bx+amount*amount*cx,u*u*az+2*u*amount*bz+amount*amount*cz
            dx,dz=2*(u*(bx-ax)+amount*(cx-bx)),2*(u*(bz-az)+amount*(cz-bz))
            length=math.hypot(dx,dz)
            side=1 if cx>0 else -1
            t.place(15,x+dz/length*side*8,z-dx/length*side*8,1.8)
    t.place(9,0,246,1.35)
    for z in (-290,-240,-175,-110,20,90,165):
        t.lamps([(-10,z),(10,z)],1.8)
    for z in (-310,-265,-200,-140,-75,-10,55,120,195):
        for side in (-1,1):
            t.place(18,side*21,z,1.5)
    for angle in range(-140,141,20):
        x,z=polar(353,angle)
        t.place(19,x,z,1.7)
        x,z=polar(331,angle)
        t.place(15,x,z,1.8)
    for angle in range(-150,151,15):
        x,z=polar(326,angle)
        t.place(18,x,z,1.6)
    # End the outward links inside the perimeter stroke, without an inward cap.
    t.gates(z=-38,join_x=343)
    t.notes=['Smooth palm fronds inside an open circular breakwater',
        'Water channels between seven paired residential branches',
        'A central civic approach and linked outer promenade']
    return t
