"""The King's moated castle and adjacent military parade headquarters."""
import math
from expansion_architecture import Architecture, arch, block, dome, hip_roof, balcony
from expansion_architecture import cypress, planter, buttress, flag
from details import crystal, banner, star


def tower(g,x,y,h,r=2.1):
    g.cylinder('Fortress Tower',(x,y,h/2),r,h,'stone',16)
    for z in [.35,h*.40,h-.4]:
        g.cylinder('Tower Stone Collar',(x,y,z),r+.20,.32,'trim',16)
    g.cylinder('Tower Gold Cornice',(x,y,h),r+.38,.14,'gold',16)
    g.lathe('Teal Turret Roof',[(r+.45,0),(r*.3,4.8),(0,5.3)],(x,y,h+.1),'roof',16)
    for i in range(8):
        a=i*math.tau/8
        g.beam('Turret Gold Rib',(x+(r+.46)*math.cos(a),y+(r+.46)*math.sin(a),h+.15),
               (x,y,h+5.45),.055)
    crystal(g,(x,y,h+5.4),1.9,.28)
    for side in range(4):
        # Each banner is on the corresponding tangent to the cylindrical wall.
        xx=x*math.cos(side*math.pi/2)+y*math.sin(side*math.pi/2)
        plane=r+x*math.sin(side*math.pi/2)-y*math.cos(side*math.pi/2)
        banner(g,side,plane,xx,h-2,1.8,5.4,'')


def curtain(g,w,d,h,gate=9):
    # Front opening remains physically open so the bridge/parade approach is walkable.
    for x in [-(w+gate)/4,(w+gate)/4]:
        g.box('Front Curtain Wall',(x,-d/2,h/2),((w-gate)/2,1.1,h),'stone',.04)
    for x in [-w/2,w/2]:
        g.box('Side Curtain Wall',(x,0,h/2),(1.1,d,h),'stone',.04)
    g.box('Rear Curtain Wall',(0,d/2,h/2),(w,1.1,h),'stone',.04)
    for side,length,plane in [(0,w,d/2),(1,d,w/2),(2,w,d/2),(3,d,w/2)]:
        for i in range(math.ceil(length/2.5)):
            u=-length/2+(i+.5)*length/math.ceil(length/2.5)
            if side==0 and abs(u)<gate/2+.5: continue
            a=side*math.pi/2
            x=u*math.cos(a)+plane*math.sin(a); y=u*math.sin(a)-plane*math.cos(a)
            g.box('Crenellation',(x,y,h+.35),(1.0,1.0,.8),'trim',.02)
        for u in [-length*.25,length*.25]:
            banner(g,side,plane,u,h-.5,2.0,min(6,h-1),'')
    for x in [-gate/2-.7,gate/2+.7]:
        buttress(g,x,-d/2,h+1.4)


def create_castle(mats):
    g=Architecture('Royal Castle of Neris',mats)
    g.root.location=(-48,127,.13)
    g.root['Design Reference']='Neris - Castle.png'
    g.box('Castle Island',(0,0,-.25),(77,64,.5),'trim',.15)
    curtain(g,74,60,10,10)
    for x in [-35,35]:
        for y in [-28,28]: tower(g,x,y,20,2.5)
    # Tiered palace: wide lower wings, elevated central keep, soaring lantern dome.
    block(g,'Lower Palace Wings',0,13,58,21,16)
    block(g,'Upper Palace Terraces',0,14,42,18,26)
    block(g,'King’s Central Keep',0,13,24,20,43)
    dome(g,0,13,43.5,12.4,9.5)
    for x in [-20,20]:
        for y in [6,21]: tower(g,x,y,31,1.8)
    for x in [-12,12]:
        for y in [3,23]: tower(g,x,y,45,1.45)
    arch(g,0,-2.4,0,.25,6.2,9,True)
    arch(g,0,-2.85,0,12,8.2,27,glass='roof')
    star(g,0,-2.8,0,26,2.9,.42)
    for side,plane,u in [(1,12,13),(2,23,0),(3,12,-13)]:
        arch(g,side,plane,u,15,7,24,glass='roof')
    for x in [-25,25]:
        hip_roof(g,x,13,16.3,9,22,3.2)
    for x in [-16.5,16.5]:
        hip_roof(g,x,15,26.3,9,18,3.0)
    for x in [-25,-17,17,25]:
        for z in [2,9]: arch(g,0,-2.5,x,z,2.4,5.0)
    for x in [-16,16]:
        arch(g,0,-5,x,18,2.8,6)
    for side in [1,3]:
        for u in [4,13,22]:
            for z in [2,9]: arch(g,side,29,u if side==1 else -u,z,2.3,5)
    for x in [-9,9]: banner(g,0,-2.8,x,39,2.5,16,'')
    for x in [-27,27]:
        for y in [-17,-7,1]: cypress(g,x,y,.1,5)
    for x in [-15,15]:
        for y in [-18,-7]: planter(g,x,y,.1,1.7)
    g.cylinder('Royal Fountain Basin',(0,-16,.35),4.2,.7,'trim',40)
    g.cylinder('Royal Fountain Water',(0,-16,.72),3.6,.08,'water',40)
    crystal(g,(0,-16,.80),4.2,.8)
    for x in [-19,19]:
        balcony(g,x,2.5,16.3,16,1.8)
    # Level three-span stone bridge over the moat, raised banks and gold rails.
    g.box('Royal Bridge Deck',(0,-38,.02),(9,22,.26),'pavinglight',.04)
    for x in [-4.6,4.6]:
        g.beam('Bridge Gold Handrail',(x,-49,1.25),(x,-27,1.25),.085)
        for y in [-48,-44,-40,-36,-32,-28]:
            g.beam('Bridge Baluster',(x,y,.16),(x,y,1.25),.045,'iron')
    for y in [-44,-37,-30]:
        g.box('Bridge Pier',(0,y,-.55),(8.8,.7,1.1),'stone',.03)
    return g


def create_headquarters(mats):
    g=Architecture('Neris Military Headquarters',mats)
    g.root.location=(66,125,.13)
    g.root['Design Reference']='Neris - Military HQ.png'
    g.box('Military Precinct',(0,0,-.10),(78,70,.20),'pavinglight',.1)
    curtain(g,74,66,6.8,11)
    for x in [-35,35]:
        for y in [-31,31]: tower(g,x,y,16,2.2)
    block(g,'Command Hall',0,21,27,17,22)
    dome(g,0,21,22.4,10.3,6)
    arch(g,0,-12.5,0,.3,4.6,7.2,True)
    arch(g,0,-12.65,0,9,4.8,10,glass='roof')
    for x in [-10,10]: banner(g,0,-12.7,x,20,2.1,9,'')
    for x in [-25,25]:
        block(g,'Officers Barracks',x,18,19,20,11)
        hip_roof(g,x,18,11.3,20,21,3)
        for dx in [-6,-2,2,6]:
            for z in [1.2,6.3]: arch(g,0,-8,x+dx,z,1.45,3.4)
    g.box('Parade Ground Lawn',(0,-7,.065),(50,33,.13),'grass',0)
    for x in [-25.4,25.4]:
        g.box('Parade Lawn Border',(x,-7,.15),(.28,33.7,.20),'trim',.02)
    for y in [-23.6,9.6]:
        g.box('Parade Lawn Border',(0,y,.15),(51.1,.28,.20),'trim',.02)
    g.cylinder('Flag Saluting Dais',(0,-6,.3),2.1,.6,'trim',24)
    flag(g,0,-6,.60,24,6)
    for x in [-29,29]:
        for y in [-22,-12,-2,7]: cypress(g,x,y,.1,3.5)
    for x in [-8,8]: planter(g,x,-30,.1,1.5)
    return g
