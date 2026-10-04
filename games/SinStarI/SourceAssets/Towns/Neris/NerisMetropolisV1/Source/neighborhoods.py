"""Distinct masonry/metal mid-rises within the existing city lots.

Template identities and footprints stay fixed so authored placements survive.
Individual windows are dark panes; landmark curtain walls remain exclusive to
landmarks. Studio owns night lighting; none is baked into these Blender assets.
"""
import math
from geometry import Mesh


def block(m,x,y,w,d,h,wall,base=0,rhythm=6,bands=False):
    m.box((x,y,base+h/2),(w,d,h),wall)
    m.solids.append([[x-w/2,y-d/2,0],[x+w/2,y+d/2,base+h]])
    for z in range(4,int(h)-2,5):
        for side in (-1,1):
            for offset in range(-int(w/2)+3,int(w/2)-2,rhythm):
                m.box((x+offset,y+side*(d/2+.09),base+z),(rhythm*.58,.18,2.9),'Window Blue')
            for offset in range(-int(d/2)+3,int(d/2)-2,rhythm):
                m.box((x+side*(w/2+.09),y+offset,base+z),(.18,rhythm*.58,2.9),'Window Blue')
        if bands and z%10==4:
            m.box((x,y,base+z+2),(w+.8,d+.8,.6),'Pale Stone')
    m.box((x,y,base+h+.5),(w+1,d+1,1),'Pale Stone')
    m.box((x,y,base+h+1),(max(1,w-3),max(1,d-3),.4),'Graphite')


def garden(m,x,y,z,w,d):
    m.box((x,y,z+.8),(w,d,1.4),'Green')
    for side in (-1,1):
        m.box((x+side*w/2,y,z+1.4),(.5,d,2),'Pale Stone')


def fins(m,x,y,w,d,h,material='Copper'):
    for offset in range(-int(w/2),int(w/2)+1,7):
        for side in (-1,1):
            m.box((x+offset,y+side*(d/2+.5),h/2),(.75,1.5,h),material)


def round_block(m,x,y,r,h,wall):
    m.cylinder(x,y,0,r,h,wall,segments=32)
    m.solids.append([[x-r,y-r,0],[x+r,y+r,h]])
    for z in range(4,int(h)-2,5):
        for i in range(24):
            a=i*math.tau/24
            m.box((x+(r+.08)*math.cos(a),y+(r+.08)*math.sin(a),z),
                  (.16,r*.16,2.7),'Window Blue',yaw=a)
        m.ring(r+.3,.65,z+2,'Pale Stone',center=(x,y),segments=64)
    m.cylinder(x,y,h,r+1,1,'Pale Stone',segments=32)
    garden(m,x,y,h+1,r*1.25,r*1.25)


def neighborhood(kind):
    m=Mesh('Neighborhood '+str(kind))
    style=kind%8
    alternate=kind>=8
    if style==0:
        # Brick warehouse conversions with pilasters and sawtooth roofs.
        wall='Terracotta' if alternate else 'Brick'
        for y,h in ((-26,35),(26,50 if alternate else 40)):
            block(m,0,y,94,40,h,wall,rhythm=8,bands=True)
            for x in range(-42,43,14):
                for side in (-1,1):
                    m.box((x,y+side*20.6,h/2),(1.1,1.5,h),'Sandstone')
            for x in (-30,0,30):
                m.loft([(x,y,h+1,18,27,math.pi/4),
                        (x,y,h+9,.4,27,math.pi/4)],'Graphite',4)
    elif style==1:
        # Unequal terraced apartment bars with planted setbacks.
        wall='Sage' if alternate else 'Ivory'
        for x,levels in ((-26,4),(26,3)):
            for level in range(levels):
                d=88-level*16; y=level*5-4
                block(m,x,y,41-level*3,d,18,wall,base=level*18,rhythm=7)
                garden(m,x,y-d/2+4,(level+1)*18,34-level*3,5)
    elif style==2:
        # Art Deco podium, narrow stepped towers and vertical stone ribs.
        wall='Sandstone' if alternate else 'Pale Stone'
        block(m,0,0,94,88,15,wall,rhythm=8)
        for x,h in ((-28,58),(0,88 if alternate else 78),(28,66)):
            block(m,x,4,23,54,h,wall,base=15,rhythm=5)
            fins(m,x,4,23,54,h+15,'Ivory')
            block(m,x,4,15,34,8,wall,base=h+15,rhythm=5)
            m.box((x,4,h+25),(8,22,4),'Copper')
    elif style==3:
        # Rounded hotels beside a lower commercial street wall.
        wall='Concrete' if alternate else 'Ivory'
        round_block(m,-24,10,23,76 if alternate else 61,wall)
        round_block(m,25,13,21,53,wall)
        block(m,0,-32,92,23,25,'Terracotta' if alternate else 'Graphite',rhythm=8)
    elif style==4:
        # An open courtyard, unequal wing heights and planted roofs.
        wall='Brick' if alternate else 'Sandstone'
        for x,y,w,d,h in ((-36,0,24,92,51),(36,0,24,92,36),
                          (0,35,47,22,43),(0,-35,47,22,21)):
            block(m,x,y,w,d,h,wall,rhythm=6,bands=True)
            garden(m,x,y,h+1,w-5,d-5)
        m.box((0,0,.35),(42,43,.7),'Paving')
    elif style==5:
        # Staggered offices with bronze fins and angled metal crowns.
        wall='Graphite' if alternate else 'Concrete'
        for x,y,w,d,h in ((-26,-15,40,61,68),(24,19,44,54,88 if alternate else 55)):
            block(m,x,y,w,d,h,wall,rhythm=7)
            fins(m,x,y,w,d,h)
            m.loft([(x,y,h+1,w*.7,d*.7,math.pi/4),
                    (x+9,y,h+12,w*.46,d*.7,math.pi/4)],'Copper',4)
        block(m,0,-37,26,18,16,'Sandstone',rhythm=6)
    elif style==6:
        # Apartment slabs with deep balconies and roof pergolas.
        wall='Terracotta' if alternate else 'Sage'
        for y,h in ((-26,56),(26,76 if alternate else 66)):
            block(m,0,y,92,35,h,'Ivory',rhythm=7)
            for side in (-1,1):
                m.box((side*45,y,h/2),(3,36,h+2),wall)
            for z in range(10,h,10):
                for side in (-1,1):
                    m.box((0,y+side*19,z),(86,5,.75),'Pale Stone')
                    m.box((0,y+side*21,z+1.7),(86,.35,1.5),'Graphite')
            for x in range(-35,36,14):
                m.box((x,y,h+5),(.8,29,.8),'Copper')
                for side in (-1,1):
                    m.box((x,y+side*14,h+3),(.7,.7,5),'Copper')
    else:
        # Civic bars around a small plaza, with a stepped corner pavilion.
        wall='Concrete' if alternate else 'Pale Stone'
        block(m,-28,0,37,90,54,wall,rhythm=9,bands=True)
        block(m,18,27,54,36,33,'Brick' if alternate else 'Sandstone',rhythm=8)
        block(m,18,-25,54,41,21,wall,rhythm=8)
        block(m,-28,7,26,61,20,wall,base=54,rhythm=6)
        m.box((12,0,.3),(53,15,.6),'Paving')
        garden(m,18,27,34,46,28)
    return m
