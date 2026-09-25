"""The three Neris shops, sharing the architectural language of Sin's references."""
import math

from geometry import face_point, on_face
from details import (opening, column, dome, crystal, banner, signboard, canopy,
                     lantern, star, shield, sword, planter, crate, barrel, bottle, stairs,
                     crystal_fixture)


def cornice(g,w,d,z):
    for dz,extra,h,mat in [(0,.30,.24,'trim'),(.22,.42,.13,'gold'),
                            (.35,.53,.20,'trim'),(.57,.30,.20,'roof')]:
        if mat=='roof':
            for side,plane,length in [(0,d/2,w),(1,w/2,d),(2,d/2,w),(3,w/2,d)]:
                on_face(g.box('Teal Cornice Inlay',(0,0,0),(length+.3,.13,h),mat),
                        side,plane,0,z+dz,.12)
        else:
            g.box('Continuous Roof Cornice',(0,0,z+dz),(w+extra,d+extra,h),mat)


def pipe(g,x,y,height):
    g.cylinder('Copper Workshop Pipe',(x,y,height/2+.3),.16,height,'iron')
    for h in [.5,1.4,3.0,4.6,6.2,height+.2]:
        if h<height+.3:
            g.cylinder('Pipe Collar',(x,y,h),.205,.13,'gold')
    g.lathe('Vent Cowl',[(.18,0),(.27,.08),(.27,.18),(.17,.22)],(x,y,height+.3),'iron')


def shelf(g,side,plane,x,bottom,width=2.1):
    for dz in [0,.65,1.3,1.95]:
        on_face(g.box('Apothecary Shelf',(0,0,0),(width,.55,.11),'woodalt'),
                side,plane,x,bottom+dz,.57)
    for dx in [-width/2,width/2]:
        on_face(g.box('Shelf Upright',(0,0,0),(.1,.55,2.1),'iron'),
                side,plane,x+dx,bottom+.95,.57)
    for row in range(3):
        for i in range(5):
            bottle(g,face_point(side,plane,x+(i-2)*width*.17,bottom+row*.65+.07,.59),blue=(i+row)%2==0)


def weapon_rack(g,side,plane,x,bottom):
    for dx in [-.72,.72]:
        on_face(g.box('Weapon Rack Post',(0,0,0),(.13,.38,2.15),'woodalt'),
                side,plane,x+dx,bottom+1.07,.72)
    for z in [.12,1.35]:
        on_face(g.box('Weapon Rack Crossbar',(0,0,0),(1.65,.36,.13),'iron'),
                side,plane,x,bottom+z,.72)
    for i in range(4):
        sword(g,side,plane+.42,x+(i-1.5)*.36,bottom+.55,1.55+(i%2)*.23)


def armor_display(g,side,plane,x,bottom):
    p=lambda dx,z,out=.95:face_point(side,plane,x+dx,bottom+z,out)
    g.cylinder('Armor Display Plinth',p(0,.08),.48,.16,'iron')
    g.beam('Armor Stand',p(0,.15),p(0,1.75),.10,'wood')
    chest=[(-.4,.4),(-.35,-.25),(0,-.38),(.35,-.25),(.4,.4),(0,.5)]
    on_face(g.prism('Breastplate Display',chest,.3,'iron'),side,plane,x,bottom+1.5,.95)
    for dx in [-.44,.44]:
        g.lathe('Display Pauldron',[(.26,0),(.28,.14),(.12,.30)],p(dx,1.74),'trim',12)
    g.lathe('Display Helm',[(.20,0),(.24,.25),(.12,.46),(0,.54)],p(0,2.02),'trim',12)
    shield(g,side,plane+.7,x,bottom+.87,.78)


def potion_emblem(g,plane,z):
    # Gold flask silhouette stands proud of the round teal medallion.
    disk=g.cylinder('Potion Emblem Medallion',(0,-plane-.36,z),.96,.15,'roof',48)
    disk.rotation_euler.x=math.pi/2
    circle=[(.98*math.sin(i*math.tau/64),-plane-.48,z+.98*math.cos(i*math.tau/64)) for i in range(64)]
    g.path('Medallion Gold Rim',circle,.055,'gold',True)
    flask=[(-.22,.6),(.22,.6),(.22,.48),(.12,.48),(.12,.20),(.42,-.06),
           (.45,-.33),(.29,-.52),(-.29,-.52),(-.45,-.33),(-.42,-.06),(-.12,.20),(-.12,.48),(-.22,.48)]
    g.path('Raised Flask Symbol',[(x,-plane-.50,z+h) for x,h in flask],.035,'brightgold',True)


def build_shop(g,kind):
    w,d,h=10.2,8.0,8.0
    g.root['Reference Interpretation']='Detailed exterior concept model; closed facade openings'
    g.root['Building Type']=kind
    g.box('Foundation',(0,0,.20),(w+1.7,d+1.3,.4),'stone')
    g.box('Structural Core',(0,0,4.25),(w-.10,d-.10,7.7),'stone',.03)
    g.masonry(w,d,7.7,.4)
    for z in [.55,4.1,7.75]:
        g.box('Horizontal Stone Course',(0,0,z),(w+.24,d+.24,.16),'trim')
    cornice(g,w,d,8.05)
    dome(g,(0,0,8.62),4.05,2.35)
    crystal(g,(0,0,11.08),1.15,.23)
    for x in [-4.52,4.52]:
        for y in [-3.98,3.98]:
            column(g,x,y,9.6 if y<0 else 9.0,.88,.38)
        crystal_fixture(g,0,4.42,x,7.25,1.55)
    for side,plane,length in [(0,4.12,10.2),(1,5.22,8),(2,4.12,10.2),(3,5.22,8)]:
        for i in range(9):
            x=(i-4)*length/10
            on_face(g.box('Cornice Dentil',(0,0,0),(.14,.22,.30),'gold'),
                    side,plane,x,7.95,.16)
        for x in [-length*.35,length*.35]:
            crystal_fixture(g,side,plane,x,3.75,.65)
            planter(g,face_point(side,plane,x,4.50,.40),.64)
    # The monumental front opening sits behind the sign and shop porch.
    opening(g,0,4.0,0,5.9,3.0,4.20,glass='roof')
    if kind=='Weapon Store':
        sword(g,0,4.55,0,7.02,4.20,cyan=True)
    elif kind=='Armor Store':
        shield(g,0,4.58,0,8.05,1.52)
    else:
        potion_emblem(g,4.43,8.08)
        crystal(g,(0,-4.25,9.9),1.35,.22)
    for x in [-2.65,2.65]:
        opening(g,0,4.0,x,6.40,.92,1.78)
    opening(g,0,4.0,0,.74,3.8,3.95,True)
    stairs(g,4.4,-4.08,5,.31,.15)
    for x in [-2.28,2.28]:
        column(g,x,-4.53,4.52,.26,.5,False)
        lantern(g,0,4.05,x,2.77,1.25)
    canopy(g,0,4.10,0,4.76,5.5,1.15)
    signboard(g,0,4.12,5.71,5.65,kind.upper())
    for x in [-5.78,5.78]:
        banner(g,0,4.02,x,7.75,1.10,3.3,'NERIS')
        g.beam('Side Sign Bracket',(x,-4.02,7.84),(x*.83,-4.02,7.84),.055,'gold')
    # The rear has a distinct service entry; side work areas follow the drawings.
    opening(g,2,4.0,2.8,.43,1.1,2.3,True)
    for side,plane in [(1,5.1),(2,4.0),(3,5.1)]:
        for x in [-2.2,2.2]:
            opening(g,side,plane,x,5.85,.87,1.85)
        banner(g,side,plane,0,8.16,1.55,4.5,
               'PEOPLE|BUILD|BRIGHTER|WORLDS')
        if side!=2:
            canopy(g,side,plane,0,3.60,3.8,1.2)
            opening(g,side,plane,0,.4,1.05,2.3,True)
            lantern(g,side,plane,-1.25,2.58)
    for x in [-5.10,5.10]:
        planter(g,(x,-4.52,0),1.12)
        planter(g,(x,3.55,0),1.10)
    pipe(g,-3.3,4.37,7.8)
    pipe(g,-2.68,4.38,3.0)
    for p in [(3.2,4.6,0),(2.95,4.5,.8),(-3.4,-4.7,0)]:
        crate(g,p,.72)
    barrel(g,(-4.5,4.72,0),1.05)
    barrel(g,(5.7,1.75,0),.95)
    if kind=='Weapon Store':
        weapon_rack(g,0,4.25,3.4,0)
        weapon_rack(g,1,5.4,0,0)
        weapon_rack(g,2,4.3,-2.0,0)
        shield(g,0,4.35,-3.3,1.0,.72)
    elif kind=='Armor Store':
        for x in [-3.15,3.15]:
            armor_display(g,0,4.05,x,0)
        armor_display(g,1,5.30,0,0)
        weapon_rack(g,2,4.35,-1.8,0)
    else:
        for side,plane,x in [(0,4.55,-3.45),(1,5.3,0),(3,5.3,0),(2,4.2,-2.1)]:
            shelf(g,side,plane,x,.13,1.75)
        g.box('Potion Sales Counter',(0,-5.0,1.10),(2.55,.7,.15),'woodalt')
        for x in [-1.1,1.1]:
            g.box('Counter Leg',(x,-5.0,.60),(.13,.55,1.05),'wood')
        for i in range(7):
            bottle(g,((i-3)*.31,-5.0,1.2),blue=i%2==0)
    return {'width':13.6,'depth':11.8,'height':12.3}
