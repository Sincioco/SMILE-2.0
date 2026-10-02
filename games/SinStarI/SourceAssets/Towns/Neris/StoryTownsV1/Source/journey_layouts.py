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


def smooth(value):
    value = max(0, min(1, value))
    return value*value*(3-2*value)


def forest_height(x, z):
    height = sum(peak*smooth(1-math.hypot((x-cx)/rx, (z-cz)/rz))
                 for cx, cz, rx, rz, peak in
                 [(-225, 170, 150, 135, 32), (145, 190, 170, 155, 38),
                  (-25, -25, 185, 145, 19), (260, -245, 135, 110, 24)])
    # Both lake basins meet level water; the forest slopes roll up from their shores.
    for cx, cz, radius in [(-110, -155, 61), (185, -175, 48)]:
        height *= smooth((math.hypot(x-cx, z-cz)-radius)/55)
    # Keep the existing hut's footprint level, with a gentle surrounding transition.
    height *= smooth((math.hypot(x+105, z-121)-18)/35)
    return height


MOUNTAIN_PASS = [(-388,0),(-300,0),(-240,-100),(-140,-100),(-75,55),
                 (30,55),(115,-90),(225,-90),(300,0),(388,0)]
MOUNTAIN_RAMPS = [([(0, 55), (0, 140), (-90, 170), (-185, 220)], 116),
                  ([(180, -90), (140, -165), (150, -255), (205, -265)], 98)]


def mountain_height(x, z, valley, ramps):
    # Overlapping asymmetric massifs form connected ridges and saddles. Fluted
    # slopes use continuous, deterministic functions, not repeated mountain props.
    hills = [(-310,235,185,175,132,.4),(-185,220,205,180,116,1.2),
             (-5,285,205,195,166,2.1),(210,260,220,200,148,.8),
             (340,170,175,185,112,1.7),(-285,-285,225,215,142,2.8),
             (-70,-300,215,220,174,1.5),(205,-265,220,210,98,.2),
             (340,-270,180,205,128,2.4)]
    height = 0
    for cx,cz,rx,rz,peak,phase in hills:
        dx,dz=(x-cx)/rx,(z-cz)/rz
        radius=math.hypot(dx,dz)
        angle=math.atan2(dz,dx)
        ribs=1+.12*math.sin(angle*7+phase)+.055*math.sin(angle*13-phase)
        radius*=1+(ribs-1)*smooth(radius*4)
        # A quadratic crown has zero slope at the summit and joins the flank smoothly.
        if radius < .24:
            radius = .12 + radius*radius/.48
        flank=max(0,1-radius)**1.18
        height=max(height,peak*flank)
    # Broad green lower slopes surround a navigable winding valley floor.
    height *= smooth((distance(x,z,valley)-28)/65)
    # Retain two flat accessible lookouts and wide, steady-grade approaches.
    for points, peak in ramps:
        summit=math.dist((x,z),points[-1])
        height+=(peak-height)*(1-smooth((summit-18)/35))
        lengths = [math.dist(a,b) for a,b in zip(points, points[1:])]
        total, travelled = sum(lengths), 0
        best = (1e9, 0)
        for ((ax,az),(bx,bz)), length in zip(zip(points, points[1:]), lengths):
            t = max(0, min(1, ((x-ax)*(bx-ax)+(z-az)*(bz-az))/length**2))
            d = math.hypot(x-ax-t*(bx-ax), z-az-t*(bz-az))
            if d < best[0]:
                best = (d, peak*(travelled+t*length)/total)
            travelled += length
        blend = 1-smooth((best[0]-18)/40)
        height += (best[1]-height)*blend
    # A level foundation and broad shoulder keep the hillside cottage grounded.
    plot = 1-smooth((math.hypot(x+240,z+150)-12)/28)
    return height+(16.7445-height)*plot


def forest():
    t=base('Verdant Reach')
    t.wilderness=True
    path=[(-348,0),(-260,0),(-175,75),(-80,75),(0,-30),(100,-30),(190,80),(270,0),(348,0)]
    path=t.path(path,16)
    t.lake((-110,-155),[(-170,-166),(-141,-191),(-116,-179),(-92,-201),
        (-70,-179),(-61,-148),(-90,-143),(-106,-111),(-122,-139),(-158,-129)])
    t.lake((185,-175),[(148,-191),(169,-211),(184,-193),(211,-204),
        (225,-172),(202,-150),(188,-165),(168,-136),(160,-165)])
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
    t.height=forest_height
    t.acceptance_paths=[path]
    t.notes=['A wooded journey from the waterworks toward the eastern settlements.',
             'Rolling hills and two smooth, irregular lake basins; the winding trail remains walkable.']
    return t


def mountains():
    t=base('Greyglass Pass',800,2)
    t.wilderness=True
    path=MOUNTAIN_PASS
    path=t.path(path,18)
    ramps=[t.path(points,12) for points,_ in MOUNTAIN_RAMPS]
    for x,z in ((-290,45),(-120,-155),(30,110),(240,-145),(320,45)):
        t.grove(x,z,2,2,11,2.5)
    t.place(5,-240,-150,1.25,180)
    t.lamps([(-305,22),(-190,-74),(2,85),(221,-58),(320,25)],1.5)
    t.gates(destinations=('Verdant Reach','East Valley'))
    t.height=lambda x,z: mountain_height(x,z,path,list(zip(ramps,[p for _,p in MOUNTAIN_RAMPS])))
    t.acceptance_paths=[path]+ramps+[[(0,0),(0,55)]]
    t.notes=['Rocky switchback route linking the forest to East Valley.',
             'Connected fluted ridgelines enclose a winding valley; broad ramps reach 116 m and 98 m lookouts.']
    return t


def desert():
    t=base('Sunglass Expanse',720,3)
    t.wilderness=True
    path=[(-348,0),(-260,0),(-175,-95),(-40,-95),(80,-90),(190,50),(270,0),(348,0)]
    path=t.path(path,17)
    t.acceptance_paths=[path]
    t.disk(0,30,40,WATER)
    t.ring(0,30,65,15)
    t.path([(-40,-95),(-40,-30)],15)
    for x,z in ((-260,210),(-80,265),(170,260),(275,195),(-250,-245),(5,-260),(250,-220)):
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
    t=Town('Neris Waterworks',560,4,True)
    t.symmetric=False
    def point(radius,angle):
        a=math.radians(angle)
        return radius*math.sin(a),radius*math.cos(a)
    # Five full circular reservoirs alternate with smaller round gardens.
    for angle in range(0,360,72):
        x,z=point(190,angle)
        t.disk(x,z,48)
        t.disk(x,z,36.2,WATER)
        t.disk(x,z,11)
        gx,gz=point(175,angle+36)
        t.disk(gx,gz,23)
    # Two interlaced five-point stars carry service paths; land is authored first.
    for index in range(10):
        a,b=point(128,index*36),point(128,(index+4)*36)
        t.curve([a,((a[0]+b[0])/2,(a[1]+b[1])/2),b],14)
        a,b=point(128,index*36),point(175 if index%2 else 155,index*36)
        t.curve([a,((a[0]+b[0])/2,(a[1]+b[1])/2),b],16)
    t.disk(0,0,32)
    for index in range(10):
        t.path([point(128,index*36),point(128,(index+4)*36)],6)
        t.path([point(128,index*36),point(158 if index%2 else 152,index*36)],7)
    t.ring(0,0,25,7)
    t.path([(0,25),(0,128)],7)
    for angle in range(0,360,72):
        x,z=point(190,angle)
        t.ring(x,z,39,6)
        t.path([(x,z),point(152,angle)],6)
        t.place(27,x,z,1.05)
        # Trees stay on the outer bank, leaving the reservoir interior filled.
        for offset in (-20,20):
            dx,dz=point(45,angle+offset)
            t.place(19,x+dx,z+dz,.75)
        for turn in (-90,90):
            dx,dz=point(43,angle+turn)
            t.place(15,x+dx,z+dz,1.5)
        gx,gz=point(175,angle+36)
        t.ring(gx,gz,16,6)
        t.place(20,gx,gz,1.0)
        for turn in (-60,60):
            dx,dz=point(20,angle+36+turn)
            t.place(15,gx+dx,gz+dz,1.4)
    t.place(9,0,0,1.0)
    # Short west/east links reach the star without slicing through its center.
    for side in (-1,1):
        t.path([(side*67,0),(side*248,0)],9)
    t.gates(destinations=('Neris Town','Verdant Reach'))
    t.notes=['Five circular reservoirs filled to their promenades and five round gardens around a star-web of service paths.',
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
    from relief_landscape import build
    return build()


BUILDERS=(forest,mountains,desert,waterworks,relay,relief)
