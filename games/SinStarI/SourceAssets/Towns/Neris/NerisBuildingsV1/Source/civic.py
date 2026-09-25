"""Communication relay and larger civic hall reconstructed from the supplied sheets."""
import math

from mathutils import Vector

from geometry import face_point, on_face
from details import (opening,column,dome,crystal,banner,signboard,canopy,lantern,
                     star,planter,crate,stairs,crystal_fixture)
from shops import cornice,pipe


def ring(g,name,loc,radius,tube=.06,mat='gold'):
    x,y,z=loc
    return g.path(name,[(x+radius*math.cos(i*math.tau/80),
                        y+radius*math.sin(i*math.tau/80),z) for i in range(80)],tube,mat,True)


def buttress(g,side,plane,x,base,height,reach,width=.8):
    # Profile extruded across local x with its top sloping down toward the outer foot.
    profile=[(0,0),(-reach,0),(-reach,.55),(-.50,height*.60),(0,height)]
    verts=[(xx,y,z+base) for xx in [x-width/2,x+width/2] for y,z in profile]
    n=len(profile)
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    on_face(g.mesh('Sloping Stone Buttress',verts,faces,'trim'),side,plane)
    g.beam('Buttress Gold Spine',face_point(side,plane,x,base+.57,reach),
           face_point(side,plane,x,base+height*.60,.50),.045,'gold')


def relay_dish(g,loc,sign):
    rings,steps=12,48
    verts=[]
    for i in range(rings+1):
        r=max(.006,1.55*i/rings)
        verts += [(r*math.cos(j*math.tau/steps),r*math.sin(j*math.tau/steps),.43*r*r)
                  for j in range(steps)]
    faces=[(i*steps+j,i*steps+(j+1)%steps,(i+1)*steps+(j+1)%steps,(i+1)*steps+j)
           for i in range(rings) for j in range(steps)]
    obj=g.mesh('Parabolic Relay Dish',verts,faces,'trim',True)
    obj.location=loc
    rotation=Vector((sign*.68,0,.73)).to_track_quat('Z','Y')
    obj.rotation_mode='QUATERNION';obj.rotation_quaternion=rotation
    mod=obj.modifiers.new('Reflector Thickness','SOLIDIFY');mod.thickness=.06
    transform=lambda p:Vector(loc)+rotation@Vector(p)
    g.path('Dish Gold Rim',[transform((1.55*math.cos(j*math.tau/64),
                                    1.55*math.sin(j*math.tau/64),1.033)) for j in range(64)],
           .065,'gold',True)
    feed=transform((0,0,1.45))
    for a in [0,math.tau/3,math.tau*2/3]:
        g.beam('Dish Feed Support',transform((1.5*math.cos(a),1.5*math.sin(a),.99)),feed,.045,'gold')
    g.beam('Dish Central Receiver',transform((0,0,.15)),feed,.075,'iron')


def build_tower(g):
    g.root['Building Type']='Communication Tower'
    g.box('Relay Foundation',(0,0,.18),(12.9,10.3,.36),'stone')
    g.lathe('Octagonal Relay Base',[(4.3,.36),(4.3,7),(3.1,10)],mat='stone',steps=8,smooth=False)
    g.lathe('Tapered Main Relay Shaft',[(3.2,6),(2.9,16.7),(3.1,17.0)],mat='trim',steps=12,smooth=False)
    for z,r in [(.65,4.36),(6.7,4.34),(9.4,3.35),(16.4,3.1),(17.0,3.35)]:
        g.lathe('Relay Shaft Cornice',[(r,z),(r,z+.24)],mat='trim',steps=24)
        ring(g,'Relay Gold Band',(0,0,z+.25),r,.065)
    for side in range(4):
        # Inset teal stripe over the tapered shaft, and long crystal conduit.
        on_face(g.box('Shaft Teal Panel',(0,0,0),(1.12,.13,8.9),'roof'),side,2.86,0,11.9,.12)
        if side!=2:
            crystal_fixture(g,side,2.98,0,10.1,6.0)
        else:
            banner(g,side,3.02,0,16.45,1.40,7.4,'NERIS|CONNECTED')
        for x in [-2.62,2.62]:
            if side in [0,2]:
                column(g,*face_point(side,3.0,x,0)[:2],10.8,.70,.35)
            buttress(g,side,3.4,x,.2,5.3,2.25,.9)
        if side in [1,3]:
            banner(g,side,4.27,0,9.7,1.6,5.8,'WORDS|TRAVEL|FURTHER|TOGETHER')
            for x in [-.72,.72]:
                g.beam('Side Banner Bracket',face_point(side,3.2,x,9.8),
                       face_point(side,4.62,x,9.8),.07,'gold')
            for x in [-1.15,1.15]:
                opening(g,side,3.65,x,1.1,.65,2.8)
        for x in [-4.6,4.6]:
            column(g,*face_point(side,3.1,x,0)[:2],2.55,.60,.15)
    opening(g,0,4.20,0,.85,2.7,4.8,True)
    stairs(g,3.55,-4.10,6,.31,.145)
    for x in [-1.8,1.8]:
        lantern(g,0,4.1,x,3.8,1.3)
    signboard(g,0,3.88,7.10,5.4,'COMMUNICATION TOWER')
    for x in [-4.65,4.65]:
        banner(g,0,3.63,x,7.8,1.1,3.3,'NERIS')
        g.beam('Banner Cantilever',(x,-3.98,7.91),(math.copysign(2.62,x),-3.0,7.91),.075,'gold')
        g.beam('Banner Cantilever Brace',(x,-3.98,7.91),(math.copysign(2.62,x),-3.0,6.8),.06,'iron')
    for sign in [-1,1]:
        x=sign*4.65
        pipe(g,x,1.65,17.0)
        crystal(g,(x,1.65,17.4),2.2,.15)
        g.beam('Dish Mount Arm',(sign*2.5,1.65,14.3),(x,1.65,15.2),.15,'iron')
        relay_dish(g,(x,1.65,15.25),sign)
        g.beam('Dish Lower Brace',(sign*2.7,1.65,12.8),(x,1.65,15.2),.11,'gold')
    for radius,z in [(3.10,20.25),(3.45,20.55),(3.45,21.18),(3.10,21.47)]:
        ring(g,'Antenna Orbital Ring',(0,0,z),radius,.09)
    for j in range(8):
        a=j*math.tau/8
        x,y=3.45*math.cos(a),3.45*math.sin(a)
        g.beam('Antenna Ring Pillar',(x,y,20.5),(x,y,21.2),.055,'gold')
        if j%2==0:
            g.beam('Ring Structural Brace',(2.35*math.cos(a),2.35*math.sin(a),17.2),
                   (x,y,20.5),.11,'iron')
            g.beam('Antenna Mast',(x,y,19.8),(x,y,23.4),.065,'gold')
            g.lathe('Antenna Finial',[(.12,0),(.13,.17),(0,.65)],(x,y,23.3),'gold',8,False)
    crystal(g,(0,0,16.9),8.1,.56)
    opening(g,2,4.20,0,.40,1.5,2.65,True)
    for x in [-1.7,1.7]:
        pipe(g,x,4.52,6.6)
        crate(g,(x,4.85,.2),.85)
    return {'width':14.9,'depth':12.3,'height':25.1}


def build_city_hall(g):
    g.root['Building Type']='City Hall'
    w,d,h=17.4,12.2,11.9
    g.box('Civic Foundation',(0,0,.3),(w+5.0,d+4.1,.6),'stone')
    g.box('Grand Civic Hall',(0,0,6.3),(w-.12,d-.12,11.4),'stone')
    g.masonry(w,d,11.4,.6)
    for z in [.85,5.7,11.65]:
        g.box('Civic Stone Belt',(0,0,z),(w+.3,d+.3,.24),'trim')
    cornice(g,w,d,12.0)
    dome(g,(0,0,12.60),6.0,3.75)
    crystal(g,(0,0,16.49),2.45,.46)
    for a in range(8):
        angle=a*math.tau/8
        g.beam('Crown Prong',(1.05*math.cos(angle),1.05*math.sin(angle),15.95),
               (1.05*math.cos(angle),1.05*math.sin(angle),17.3),.05,'gold')
    ring(g,'Civic Crown',(0,0,16.15),1.13,.10)
    for x in [-6.15,6.15]:
        for y in [-6.07,6.07]:
            column(g,x,y,13.6 if y<0 else 12.6,1.12,.6)
        crystal_fixture(g,0,6.68,x,10.2,2.15)
    opening(g,0,6.1,0,9.15,4.3,5.00,glass='roof')
    crystal_fixture(g,0,6.50,0,9.45,4.1)
    opening(g,0,6.1,0,1.18,5.1,5.20,True)
    signboard(g,0,6.35,8.17,7.0,'CITY HALL')
    canopy(g,0,6.12,0,6.9,7.2,1.40)
    stairs(g,6.0,-6.28,8,.37,.15)
    for x in [-3.35,3.35]:
        column(g,x,-6.62,7.0,.46,.6)
        lantern(g,0,6.14,x,4.8,1.4)
        for i in range(6):
            y=-6.8-i*.40
            column(g,x,y,1.05,.23,1.17-i*.15,False)
        g.beam('Stair Balustrade',(x,-6.7,2.30),(x,-9.0,1.37),.10,'gold')
    for x in [-4.6,4.6]:
        opening(g,0,6.1,x,9.0,1.1,2.4)
    for x in [-7.9,7.9]:
        banner(g,0,6.22,x,11.7,1.55,6.5,'PEOPLE|CULTURE|HARMONY')
    for side,plane,length in [(1,8.7,12.2),(2,6.1,17.4),(3,8.7,12.2)]:
        positions=[-3.55,3.55] if side==2 else [-length*.33,length*.33]
        for x in positions:
            opening(g,side,plane,x,1.05 if side==2 else 3.2,1.32,3.7 if side==2 else 2.45)
            opening(g,side,plane,x,7.15,1.32,3.85)
            planter(g,face_point(side,plane,x,5.72,.35),1.0)
        banner(g,side,plane,0,12.05,2.0,7.1,'NERIS|TOGETHER|FOR A|BRIGHTER|TOMORROW')
        if side==2:
            opening(g,side,plane,0,.62,1.6,3.3,True)
        for x in [-length*.47,length*.47]:
            column(g,*face_point(side,plane,x,0)[:2],8.6,.75,.6)
    for sign in [-1,1]:
        # Terraced outer wings, cyan civic beacons, and planted stone terraces.
        x=sign*9.40
        g.box('Civic Terrace',(x,0,1.4),(2.3,10.8,2.8),'stone')
        g.box('Terrace Cap',(x,0,2.85),(2.5,11.0,.25),'trim')
        for y in [-4.9,0,4.9]:
            column(g,x,y,6.0,.7,.6)
            if y<0:
                crystal_fixture(g,0,abs(y)+.42,x,4.6,1.35)
        for y in [-3.0,2.5]:
            planter(g,(x,y,2.99),1.1,True)
        for xx,y in [(sign*9.1,-7.7),(sign*5.0,-8.0)]:
            planter(g,(xx,y,.6),1.18,True)
        crystal(g,(sign*7.2,-7.72,.8),2.8,.31)
        g.cylinder('Civic Crystal Pedestal',(sign*7.2,-7.72,.74),.66,.3,'gold',12)
    return {'width':24,'depth':20,'height':19.15}
