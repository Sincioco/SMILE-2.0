"""Reference-led royal palace: gatehouse, stepped gardens, tracery and bridge."""
import math
from expansion_architecture import Architecture, arch, block, dome, hip_roof, cypress, planter
from details import crystal, star, banner, lantern
from paving_grid import geometry
from castle_facades import framed_window, pilaster, arcaded_cornice, crown_lantern
from castle_facades import great_facade, gate_spandrels, stair_sweep, bridge_inlay


def balustrade(g, start, end, height=1.1):
    x0, y0, z0 = start; x1, y1, z1 = end
    distance = math.hypot(x1-x0, y1-y0)
    count = max(2, math.ceil(distance/.8))
    for i in range(count+1):
        t = i/count; x = x0+(x1-x0)*t; y = y0+(y1-y0)*t; z = z0+(z1-z0)*t
        g.lathe('Carved Ivory Baluster',[(.14,0),(.14,.12),(.075,.28),(.13,.65),(.10,height-.12),(.14,height)],
                (x,y,z),'trim',6)
    for dz, radius, mat in [(0,.12,'trim'),(height,.15,'trim'),(height+.12,.035,'gold')]:
        g.beam('Terrace Handrail',(x0,y0,z0+dz),(x1,y1,z1+dz),radius,mat)


def watchtower(g, x, y, height, radius, open_crown=False):
    g.lathe('Royal Tower Masonry',[(radius+ .45,0),(radius+.45,1.8),(radius,2.2),
        (radius,height-2.4),(radius+.30,height-1.8),(radius+.45,height-1.2),(radius+.45,height)],
        (x,y,0),'stone',24)
    for z in [1.8,height-2.3,height-.2]:
        g.lathe('Tower Gold Cornice',[(radius+.48,0),(radius+.48,.14)],(x,y,z),'gold',24)
    for z in range(3, int(height-2), 2):
        g.lathe('Tower Stone Course',[(radius+.025,0),(radius+.025,.045)],(x,y,z),'trim',24)
    arcaded_cornice(g,x,y,height-.8,radius,12)
    roof = [(radius+.8,0),(radius+.78,.25),(radius*.70,1.3),(radius*.18,4.4),(.05,4.8)]
    if open_crown:
        crown_lantern(g,x,y,height+.3,radius+.20,1.8)
        roof=[]
    if roof:g.lathe('Royal Teal Turret',roof,(x,y,height),'roof',24)
    for i in range(8):
        a = i*math.tau/8
        if roof:g.path('Turret Gold Rib',[(x+r*math.cos(a),y+r*math.sin(a),height+z+.04) for r,z in roof],.045)
    if roof:crystal(g,(x,y,height+4.8),max(1.3,radius),max(.22,radius*.16))
    for side in range(4):
        a=side*math.pi/2
        u=x*math.cos(a)+y*math.sin(a)
        plane=radius+x*math.sin(a)-y*math.cos(a)
        framed_window(g,side,plane,u,height-5.4,max(.7,radius*.50),3.2,glass='dark')
        if radius>1.8:
            banner(g,side,plane,u,height-6.1,radius*1.12,min(9,height-8),'')


def portal(g, y, bottom, width, height, depth=.30):
    shoulder=height*.64
    outline=[(-width/2,0),(-width/2,shoulder)]
    outline += [(-width/2+width/2*(i/12)**2,shoulder+(height-shoulder)*i/12) for i in range(1,13)]
    outline += [(-x,z) for x,z in reversed(outline[:-1])]
    for offset,radius,mat in [(0,.40,'trim'),(-.35,.11,'gold'),(-.65,.19,'trim')]:
        g.path('Royal Pointed Portal',[(x*(1+offset/width),y-depth+offset*.35,bottom+z) for x,z in outline],radius,mat)


def create(mats, curtain):
    g=Architecture('Royal Castle of Neris',mats)
    g.root.location=(-48,127,.13)
    g.root['Design Reference']='Neris - Castle.png; four-view royal palace sheet'
    g.root['Construction']='Independently modeled architectural geometry; Tripo mesh studied only as a reference'
    g.box('Castle Island',(0,0,-.25),(77,64,.5),'trim',.12)
    curtain(g,74,60,10,10)
    # Wall bases/cornices and shallow joints add readable masonry at street height.
    for y in [-30,30]:
        spans=[(-22.0,30.0),(22.0,30.0)] if y<0 else [(0,74)]
        for x,width in spans:
            for z,h in [(.65,1.3),(8.9,.35),(9.65,.28)]:
                g.box('Fortress Carved Course',(x,y,z),(width,1.45,h),'trim',.025)
            for row in range(2,10):
                g.box('Fortress Mortar Line',(x,y,row),(width,1.16,.035),'trim',0)
                for k in range(math.ceil(width/3)):
                    xx=x-width/2+1.5+k*3+(row%2)*1.5
                    if xx<x+width/2:
                        g.box('Fortress Masonry Joint',(xx,y,row+.5),(.025,1.17,1),'trim',0)
    for x in [-37,37]:
        for z in [.8,8.9,9.65]:g.box('Side Fortress Course',(x,0,z),(1.5,60,.28),'trim',.02)
        for row in range(2,10):g.box('Side Mortar Course',(x,0,row),(1.17,60,.04),'trim',0)
    for x in [-35,35]:
        for y in [-28,28]:watchtower(g,x,y,21.5,2.8)
    # Raised gate arch is open underneath; the lifted grille does not block the route.
    for x in [-8.5,8.5]:watchtower(g,x,-30,14.5,2.3)
    g.box('Gatehouse Crown',(0,-30,11.4),(12,2.8,2.8),'stone',.06)
    portal(g,-31.45,.2,10,10.1)
    gate_spandrels(g)
    for x in [-4,-3,-2,-1,0,1,2,3,4]:
        g.beam('Raised Portcullis',(x,-30.1,7.7),(x,-30.1,11.0),.085,'iron')
    for z in [8,9,10]:g.beam('Portcullis Crossbar',(-4,-30.1,z),(4,-30.1,z),.075,'iron')
    for x in [-5.9,5.9]:lantern(g,0,31.3,x,5.0,2.8)
    g.box('Gate Walkway',(0,-30,13),(12.8,3.1,.30),'trim',.04)
    for x in range(-6,7,2):g.box('Gate Battlement',(x,-31.3,13.6),(.9,.8,1),'trim',.02)
    # Three stepped palace masses leave terraces visible around the dominant keep.
    block(g,'Royal Lower Palace',0,14,58,25,14)
    block(g,'Royal Upper Terraces',0,15,42,23,25)
    # The central drum and projecting lancet now read as a cathedral-like palace,
    # not a featureless rectangular tower with a blue strip attached.
    g.lathe('King Central Rotunda',[(12.4,0),(12.4,14.1),(11.95,14.4),(11.95,42.9)],
            (0,15,0),'stone',40)
    for z in [14.4,25.2,42.4]:arcaded_cornice(g,0,15,z,11.95,24)
    block(g,'Central Lancet Front',0,3.6,13,1.7,28.5,14.1)
    block(g,'Central Lancet Rear',0,26.4,13,1.7,28.5,14.1)
    dome(g,0,15,43.5,12.4,9.5)
    crown_lantern(g,0,15,53.1,1.35,2)
    for x in [-28,28]:
        for y in [2,25]:watchtower(g,x,y,21,1.55)
    for x in [-20,20]:
        for y in [4,25]:watchtower(g,x,y,32.4,1.7)
    for x in [-11.5,11.5]:
        for y in [3,27]:watchtower(g,x,y,44.5,1.3,True)
    # The reference's tall blue lancet and star are the main front/back focal point.
    for side,plane in [(0,-2.7),(2,27.3)]:great_facade(g,side,plane)
    framed_window(g,0,-1.28,0,6.5,6.1,7.4,glass='glass')
    portal(g,1.20,6.4,7.5,9.3)
    for side,plane in [(1,11.6),(3,11.6)]:
        framed_window(g,side,plane,15 if side==1 else -15,14.5,7.4,26)
    for x in [-24,-17,17,24]:
        for z in [1.0,7.3]:framed_window(g,0,-1.4,x,z,2.8,5.3,glass='glass')
    for x in [-16,16]:
        framed_window(g,0,-3.4,x,15.0,3.2,9.0)
    for side in [1,3]:
        for u in [5,13,21]:
            for z in [1,7.3]:framed_window(g,side,29.1,u if side==1 else -u,z,2.3,5.1,glass='glass')
        for u in [6,14,22]:framed_window(g,side,21.1,u if side==1 else -u,15.2,2.8,8.8)
    for x in [-24,-17,17,24]:
        for z in [1,7.3]:framed_window(g,2,26.6,x,z,2.4,5.1,glass='glass')
    for side,plane in [(0,-1.30),(2,26.65)]:
        for x in [-28,-21,-13,13,21,28]:pilaster(g,side,plane,x,.4,13.4,.75)
    # Rounded garden bays and layered parapets soften the stepped palace masses.
    for sign in [-1,1]:
        for x,y,z,r in [(24,3,14.15,4.4),(16,5,25.15,3.6)]:
            g.cylinder('Rounded Terrace Bay',(sign*x,y,z/2),r,z,'stone',24)
            arcaded_cornice(g,sign*x,y,z-.7,r,14)
            framed_window(g,0,r-y,sign*x,z-10.8,2.8,8.6)
            for i in range(9):
                a=math.pi+i*math.pi/8
                xx=sign*x+(r+.22)*math.cos(a);yy=y+(r+.22)*math.sin(a)
                g.lathe('Terrace Bay Baluster',[(.14,0),(.08,.25),(.14,.65),(.10,1.05)],(xx,yy,z+.20),'trim',6)
            g.path('Curved Terrace Crown',[(sign*x+(r+.24)*math.cos(math.pi+i*math.pi/24),
                y+(r+.24)*math.sin(math.pi+i*math.pi/24),z+1.27) for i in range(25)],.14,'trim')
            for yy in [y-1,y+1]:planter(g,sign*x,yy,z+.25,1.0)
    for x in [-24,24]:hip_roof(g,x,19,14.4,9,17,2.4)
    for x in [-16,16]:hip_roof(g,x,20,25.4,8,13,2.6)
    for z,half,y in [(14.5,28,1.4),(25.5,20,3.3)]:
        for sign in [-1,1]:
            balustrade(g,(sign*12,y,z),(sign*half,y,z))
            balustrade(g,(sign*half,y,z),(sign*half,27,z))
            for yy in [5,11,23]:cypress(g,sign*(half-1.4),yy,z,3.2)
    # Broad central steps and continuous sweeping side approaches.
    for step in range(26):
        height=(step+1)*.25
        g.box('Royal Entrance Stair',(0,-8+step*.36,height/2),(7.5,.39,height),'trim',0)
    for sign in [-1,1]:
        stair_sweep(g,sign,balustrade)
        for x,y in [(27,-18),(27,-8),(14,-6),(31,1)]:cypress(g,sign*x,y,.1,5.2)
        for x,y in [(16,-18),(9,-8)]:planter(g,sign*x,y,.1,1.7)
    g.cylinder('Royal Fountain Basin',(0,-16,.35),4.2,.7,'trim',48)
    g.cylinder('Royal Fountain Water',(0,-16,.72),3.6,.08,'water',48)
    crystal(g,(0,-16,.80),4.2,.8)
    # Extend the crossing over the wider moat; paving owns its walking surface.
    g.box('Royal Bridge Deck',(0,-40.5,.02),(9,27,.26),'pavinglight',.04)
    bridge_inlay(g)
    for x in [-4.6,4.6]:
        balustrade(g,(x,-54,.20),(x,-27,.20))
        for y in [-53,-44.5,-36,-28]:
            g.box('Bridge Carved Pier',(x,y,-.30),(.8,1.3,1.7),'trim',.04)
        for y in [-49,-40.5,-32]:
            arch_points=[(x,y+4.5*math.cos(a),-1.3+.9*math.sin(a)) for a in [i*math.pi/16 for i in range(17)]]
            g.path('Bridge Stone Arch',arch_points,.24,'trim')
    for x in [-5.3,5.3]:
        g.box('Bridge Crystal Pedestal',(x,-54,.9),(1.6,1.6,1.8),'stone',.04)
        crystal(g,(x,-54,1.9),2.2,.36)
    # Formal terrace gardens echo the reference without aging or dirt textures.
    for sign in [-1,1]:
        for x,y,z,h in [(30,-16,.1,6.8),(30,-6,.1,7.5),(8,-3,.1,8),
                        (26,9,14.7,5.5),(25,20,14.7,6),(17,12,25.7,5.1),
                        (17,22,25.7,4.4)]:
            g.cylinder('Royal Cypress Planter',(sign*x,y,z+.25),1.05,.5,'trim',20)
            g.cylinder('Royal Cypress Soil',(sign*x,y,z+.51),.84,.04,'dark',20)
            g.lathe('Royal Cypress Foliage',[(.10,0),(.95,h*.18),(.82,h*.45),
                (.52,h*.76),(.03,h)],(sign*x,y,z+.54),'leaf',16)
        for y in [-22,-12]:
            g.box('Royal Garden Flower Bed',(sign*24,y,.45),(3.8,2.2,.7),'trim',.06)
            for dx in [-1.2,0,1.2]:planter(g,sign*24+dx,y,.8,.72)
    return g
