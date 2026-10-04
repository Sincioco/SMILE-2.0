"""Garden, bridge and neighborhood template geometry for Neris Metropolis."""
import math
import random
from geometry import Mesh
from landmarks import sphere


def tree(m,x,y,size=1):
    m.cylinder(x,y,0,1.2*size,9*size,'Bark',top=.7*size,segments=6)
    m.cylinder(x,y,5*size,5*size,9*size,'Green',top=1*size,segments=8)
    m.cylinder(x+size,y,9*size,4*size,5*size,'Leaf',top=.2*size,segments=8)


def grove(seed,count=22):
    from studio_trees import append

    rng=random.Random(seed)
    m=Mesh('Park Grove '+str(seed))
    count=7 if seed==4 else 2
    for i in range(count):
        a=i*math.tau/count+rng.uniform(-.15,.15)
        r=42 if count==7 else 17
        x,y=r*math.cos(a),r*math.sin(a)
        s=rng.uniform(3.5,4.3)
        append(m,x,y,0,s,i%2,a)
        m.solids.append([[x-s*.4,y-s*.4,0],[x+s*.4,y+s*.4,5.6*s]])
    return m


def conservatory(name, length, width, height):
    m=Mesh(name)
    # Curved glass vaults with individually modeled arch ribs and longitudinal seams.
    for i in range(20):
        y0=-length/2+i*length/20; y1=y0+length/20
        def arch(y,j):
            a=j*math.pi/24
            taper=.6+.4*math.cos(y/length*math.pi)
            return (width/2*math.cos(a)*taper,y,2+height*math.sin(a)*taper)
        for j in range(24):
            m.polygon([arch(y0,j),arch(y1,j),arch(y1,j+1),arch(y0,j+1)],'Glass')
            if i%2==0:
                m.beam(arch(y0,j),arch(y0,j+1),.65,'White',6)
        for j in (4,8,12,16,20):
            m.beam(arch(y0,j),arch(y1,j),.3,'Silver',4)
    m.box((0,0,1),(width,length,2),'Stone')
    return m


def supertrees():
    m=Mesh('Supertree Grove')
    for i in range(9):
        a=i*math.tau/8
        x,y=(0,0) if i==8 else (65*math.cos(a),65*math.sin(a))
        h=46 if i==8 else 30+(i%3)*5
        m.cylinder(x,y,0,4,h,'Green',top=2,segments=12)
        m.solids.append([[x-4,y-4,0],[x+4,y+4,h]])
        m.ring(18,1.5,h,'Magenta',(x,y),48)
        m.ring(11,1,h-7,'Gold',(x,y),48)
        for j in range(12):
            t=j*math.tau/12
            m.beam((x+2*math.cos(t),y+2*math.sin(t),h-24),
                   (x+18*math.cos(t),y+18*math.sin(t),h),.45,'Silver',4)
    m.road([(-65,0,19),(-46,46,19),(0,65,19),(46,46,19),(65,0,19)],2.5,'Gold')
    return m


def bridge(name, style):
    m=Mesh(name)
    # A twenty-centimetre deck offset separates the modeled deck from avenue
    # paint at the ring crossing without changing the bridge's placement.
    span=650; deck=.412
    m.box((0,0,deck-1),(span,32,2),'Road')
    m.floors.append([[-span/2,-16,deck],[span/2,16,deck]])
    for side in (-1,1):
        m.box((0,side*17,deck),(span,2,3),'Silver')
        m.box((0,side*19,deck-1),(span,3,2),'Paving')
    if style=='suspension':
        for x in (-195,195):
            for y in (-21,21):
                m.box((x,y,55),(8,8,110),'Red')
                m.solids.append([[x-4,y-4,0],[x+4,y+4,110]])
            for z in (42,84,107):
                m.box((x,0,z),(7,48,6),'Red')
        for y in (-21,21):
            points=[]
            for x in range(-325,326,13):
                z=28+80*(x/195)**2 if abs(x)<=195 else 108-(abs(x)-195)*.65
                points.append((x,y,z))
                m.beam((x,y,deck),(x,y,z),.35,'Silver',4)
            for a,b in zip(points,points[1:]):
                m.beam(a,b,1.1,'Red')
    else:
        towers=[-230,-115,0,115,230] if style=='viaduct' else ([0] if style=='ada' else [-200,200])
        for x in towers:
            height=140 if style=='ada' else 94
            if style=='ada':
                m.cylinder(x,0,deck,7,height,'Silver',top=.8,segments=12)
            else:
                for y in (-20,20):
                    m.beam((x,y,0),(x,0,height),3,'Stone',6)
            m.solids.append([[x-4,-4,0],[x+4,4,height]])
            reach=290 if style=='ada' else 96
            for side in (-1,1):
                for step in range(1,9):
                    dx=reach*step/8
                    for y in (-16,16):
                        m.beam((x,0,height-step*2),(x+side*dx,y,deck),.32,'White',4)
    return m



GARDENS=[('Cloud Forest',lambda: conservatory('Cloud Forest',150,85,58)),
         ('Flower Dome',lambda: conservatory('Flower Dome',180,110,38)),
         ('Supertree Grove',supertrees)]
BRIDGES=[('Golden Gate Bridge',lambda: bridge('Golden Gate Bridge','suspension')),
         ('Millau Viaduct',lambda: bridge('Millau Viaduct','viaduct')),
         ('Beipanjiang Bridge',lambda: bridge('Beipanjiang Bridge','cable')),
         ('Ada Bridge',lambda: bridge('Ada Bridge','ada'))]
