"""Reference-led royal palace: gatehouse, stepped gardens, tracery and bridge."""
import math
from expansion_architecture import Architecture, arch, block, dome, hip_roof, cypress, planter
from details import crystal, star, banner, lantern
from paving_grid import geometry


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


def watchtower(g, x, y, height, radius):
    g.lathe('Royal Tower Masonry',[(radius+ .45,0),(radius+.45,1.8),(radius,2.2),
        (radius,height-2.4),(radius+.30,height-1.8),(radius+.45,height-1.2),(radius+.45,height)],
        (x,y,0),'stone',24)
    for z in [1.8,height-2.3,height-.2]:
        g.lathe('Tower Gold Cornice',[(radius+.48,0),(radius+.48,.14)],(x,y,z),'gold',24)
    for z in range(3, int(height-2), 2):
        g.lathe('Tower Stone Course',[(radius+.025,0),(radius+.025,.045)],(x,y,z),'trim',24)
    roof = [(radius+.8,0),(radius+.78,.25),(radius*.70,1.3),(radius*.18,4.4),(.05,4.8)]
    g.lathe('Royal Teal Turret',roof,(x,y,height),'roof',24)
    for i in range(8):
        a = i*math.tau/8
        g.path('Turret Gold Rib',[(x+r*math.cos(a),y+r*math.sin(a),height+z+.04) for r,z in roof],.045)
        g.box('Corbel',(x+(radius+.17)*math.cos(a),y+(radius+.17)*math.sin(a),height-1.3),(.32,.32,1.3),'trim',.02)
    crystal(g,(x,y,height+4.8),max(1.3,radius),max(.22,radius*.16))
    for side in range(4):
        a=side*math.pi/2
        u=x*math.cos(a)+y*math.sin(a)
        plane=radius+x*math.sin(a)-y*math.cos(a)
        arch(g,side,plane,u,height-5.0,max(.5,radius*.42),2.6,glass='dark')
        if radius>1.8:
            banner(g,side,plane,u,height-6.1,radius*1.05,min(8,height-8),'')


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
        for y in [-28,28]:watchtower(g,x,y,20,2.5)
    # Raised gate arch is open underneath; the lifted grille does not block the route.
    for x in [-8.5,8.5]:watchtower(g,x,-30,12.5,1.75)
    g.box('Gatehouse Crown',(0,-30,11.4),(12,2.8,2.8),'stone',.06)
    portal(g,-31.45,.2,10,10.1)
    for x in [-4,-3,-2,-1,0,1,2,3,4]:
        g.beam('Raised Portcullis',(x,-30.1,7.7),(x,-30.1,11.0),.085,'iron')
    for z in [8,9,10]:g.beam('Portcullis Crossbar',(-4,-30.1,z),(4,-30.1,z),.075,'iron')
    for x in [-5.9,5.9]:lantern(g,0,31.3,x,5.0,2.8)
    g.box('Gate Walkway',(0,-30,13),(12.8,3.1,.30),'trim',.04)
    for x in range(-6,7,2):g.box('Gate Battlement',(x,-31.3,13.6),(.9,.8,1),'trim',.02)
    # Three stepped palace masses leave terraces visible around the dominant keep.
    block(g,'Royal Lower Palace',0,14,58,25,14)
    block(g,'Royal Upper Terraces',0,15,42,23,25)
    block(g,'King Central Keep',0,15,23,24,43)
    dome(g,0,15,43.5,12.4,9.5)
    g.lathe('Royal Dome Lantern',[(1.35,0),(1.35,.4),(.9,.6),(.9,1.5),(1.2,1.7)],(0,15,53),'gold',16)
    crystal(g,(0,15,54.7),3.6,.55)
    for x in [-28,28]:
        for y in [2,25]:watchtower(g,x,y,21,1.2)
    for x in [-20,20]:
        for y in [4,25]:watchtower(g,x,y,31,1.3)
    for x in [-11.5,11.5]:
        for y in [3,27]:watchtower(g,x,y,44.5,.9)
    # The reference's tall blue lancet and star are the main front/back focal point.
    for side,plane in [(0,-2.9),(2,27.1)]:
        arch(g,side,plane,0,14.2,9.2,27.3,glass='roof')
        star(g,side,plane,0,28,3.0,.45)
        for x in [-8.7,8.7]:banner(g,side,plane,x,39,2.5,17,'')
    arch(g,0,-1.28,0,6.5,6.1,7.4,True)
    portal(g,1.20,6.4,7.5,9.3)
    for x in [-2.8,0,2.8]:
        g.beam('Great Window Tracery',(x,2.50,14.4),(x,2.50,36.5),.09,'gold')
        g.path('Lancet Tracery',[(x-1.3,2.50,35),(x-.85,2.50,37),
            (x,2.50,38.8),(x+.85,2.50,37),(x+1.3,2.50,35)],.09,'gold')
    for side,plane in [(1,11.6),(3,11.6)]:
        arch(g,side,plane,15 if side==1 else -15,13,8,28,glass='roof')
    for x in [-24,-17,17,24]:
        for z in [1.0,7.3]:arch(g,0,-1.4,x,z,2.4,5.3)
    for x in [-16,16]:
        for z in [15.2,20.1]:arch(g,0,-3.4,x,z,2.7,4.5)
    for side in [1,3]:
        for u in [5,13,21]:
            for z in [1,7.3]:arch(g,side,29.1,u if side==1 else -u,z,2.3,5.1)
    for x in [-24,24]:hip_roof(g,x,19,14.4,9,17,2.4)
    for x in [-16,16]:hip_roof(g,x,20,25.4,8,13,2.6)
    for z,half,y in [(14.5,28,1.4),(25.5,20,3.3)]:
        for sign in [-1,1]:
            balustrade(g,(sign*12,y,z),(sign*half,y,z))
            balustrade(g,(sign*half,y,z),(sign*half,27,z))
            for yy in [5,11,23]:cypress(g,sign*(half-1.4),yy,z,3.2)
    # Broad central steps and symmetrical diagonal garden stairs.
    for step in range(26):
        height=(step+1)*.25
        g.box('Royal Entrance Stair',(0,-8+step*.36,height/2),(7.5,.39,height),'trim',0)
    for sign in [-1,1]:
        for level,x,y,z,rise in [(0,18,-10,.1,7),(1,24,-1,7.1,7.3)]:
            for step in range(28):
                h=(step+1)*rise/28
                g.box('Terraced Garden Stair',(sign*x,y+step*.38,z+h/2),(4.2,.41,h),'trim',0)
            for edge in [-2.3,2.3]:
                balustrade(g,(sign*x+edge,y,z+.1),(sign*x+edge,y+10.4,z+rise+.1))
        for x,y in [(27,-18),(27,-8),(14,-6),(31,1)]:cypress(g,sign*x,y,.1,5.2)
        for x,y in [(16,-18),(9,-8)]:planter(g,sign*x,y,.1,1.7)
    g.cylinder('Royal Fountain Basin',(0,-16,.35),4.2,.7,'trim',48)
    g.cylinder('Royal Fountain Water',(0,-16,.72),3.6,.08,'water',48)
    crystal(g,(0,-16,.80),4.2,.8)
    # The bridge keeps its accepted footprint and traversal elevation.
    g.box('Royal Bridge Deck',(0,-38,.02),(9,22,.26),'pavinglight',.04)
    for x in [-4.6,4.6]:
        balustrade(g,(x,-49,.20),(x,-27,.20))
        for y in [-48,-37,-28]:
            g.box('Bridge Carved Pier',(x,y,-.30),(.8,1.3,1.7),'trim',.04)
        for y in [-44,-33]:
            arch_points=[(x,y+4.5*math.cos(a),-1.3+.9*math.sin(a)) for a in [i*math.pi/16 for i in range(17)]]
            g.path('Bridge Stone Arch',arch_points,.24,'trim')
    for x in [-5.3,5.3]:
        g.box('Bridge Crystal Pedestal',(x,-49,.9),(1.6,1.6,1.8),'stone',.04)
        crystal(g,(x,-49,1.9),2.2,.36)
    return g
