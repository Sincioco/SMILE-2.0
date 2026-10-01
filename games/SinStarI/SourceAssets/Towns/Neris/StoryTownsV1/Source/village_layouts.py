"""Two modest, castle-free story locations with a City Hall and clear public paths."""
import math
from town_design import Town, WATER, ROAD


def east_valley():
    t=Town('East Valley',420,smooth=True)
    def point(radius, angle):
        angle=math.radians(angle)
        return radius*math.sin(angle),radius*math.cos(angle)
    # Water reveals the six-point silhouette inside a continuous circular bank.
    t.disk(0,0,198)
    t.disk(0,0,171,WATER)
    t.disk(0,0,96)
    for angle in range(30,390,60):
        t.triangle([point(96,angle-30),point(170,angle),point(96,angle+30)])
        x,z=point(146,angle+30)
        t.disk(x,z,22)
    # Nested promenades and six short radial bridges keep the entire town linked.
    t.ring(0,0,184,8)
    t.ring(0,0,77,7)
    t.ring(0,0,39,6)
    for angle in range(30,390,60):
        t.path([point(39,angle),point(184,angle)],7)
        x,z=point(146,angle+30)
        t.ring(x,z,16,5)
        t.path([point(162,angle+30),point(184,angle+30)],6)
        t.place(20,x,z,1.0)
        # Homes sit beside the star avenues, facing their front steps toward them.
        for radius,offset in ((108,16),(133,10)):
            cx,cz=point(radius,angle)
            nx,nz=point(1,angle+90)
            for side in (-1,1):
                t.place(0 if radius==108 else 4,cx+side*offset*nx,
                        cz+side*offset*nz,.9,(angle+side*90)%360)
        for radius in (55,91,151):
            cx,cz=point(radius,angle)
            nx,nz=point(1,angle+90)
            for side in (-1,1):
                t.place(15,cx+side*6*nx,cz+side*6*nz,1.5)
        for turn in (-90,90,180):
            dx,dz=point(9,angle+30+turn)
            t.place(19,x+dx,z+dz,1.1)
        for turn in (-60,60):
            dx,dz=point(19,angle+30+turn)
            t.place(15,x+dx,z+dz,1.45)
    # Twelve small ponds repeat the reference's inner dotted circle.
    for angle in range(0,360,30):
        x,z=point(28,angle+15)
        t.disk(x,z,3.5,WATER)
    t.place(9,0,0,1.1)
    for angle in range(0,360,15):
        x,z=point(194,angle+7.5)
        t.place(19,x,z,.85)
        x,z=point(176,angle+7.5)
        t.place(15,x,z,1.6)
    for angle in range(0,360,30):
        for radius in (51,64):
            x,z=point(radius,angle+15)
            t.place(19,x,z,1.35)
    t.gates()
    t.notes=['Six-point star inside a circular waterfront promenade',
        'Six round memorial gardens and twelve small ponds around the civic center',
        'Road-facing homes along six clear avenues with connected ring paths']
    return t


def home_village():
    t=Town("Orin's Village",420,smooth=True)
    def turn(x,z,angle):
        a=math.radians(angle)
        return round(x*math.cos(a)+z*math.sin(a),6),round(z*math.cos(a)-x*math.sin(a),6)
    # A central civic island and circular bank repeat the reference's round hub.
    t.disk(0,0,84)
    t.disk(0,0,56,WATER)
    t.disk(0,0,42)
    for angle in range(0,360,90):
        x,z=turn(0,146,angle)
        t.disk(x,z,40)
        t.disk(x,z,5.5,WATER)
        t.curve([turn(0,72,angle),turn(0,105,angle),turn(0,120,angle)],18)
        for side in (-1,1):
            x,z=turn(side*61,130,angle)
            t.disk(x,z,19)
            t.curve([turn(0,84,angle),turn(side*43,87,angle),turn(side*61,130,angle)],15)
    t.ring(0,0,71,8)
    t.ring(0,0,34,7)
    for angle in range(0,360,90):
        t.path([turn(0,34,angle),turn(0,119,angle)],8)
        x,z=turn(0,146,angle)
        t.ring(x,z,27,7)
        # Four houses form each small neighborhood, with doors toward its loop.
        for dx,dz in ((-11,-10),(11,-10),(-11,10),(11,10)):
            px,pz=turn(dx,146+dz,angle)
            t.place(0,px,pz,.85,(angle+(0 if dz<0 else 180))%360)
        for side in (-1,1):
            x,z=turn(side*61,130,angle)
            t.curve([turn(0,84,angle),turn(side*43,87,angle),turn(side*56,119,angle)],6,ROAD)
            t.ring(x,z,12,5)
            t.place(20,x,z,.85)
            for dx,dz in ((-9,10),(9,10)):
                px,pz=turn(side*61+dx,130+dz,angle)
                t.place(19,px,pz,.75)
            px,pz=turn(side*8,104,angle)
            t.place(15,px,pz,1.5)
        for offset in (-16,16):
            px,pz=turn(offset,179,angle)
            t.place(19,px,pz,.9)
            px,pz=turn(offset,164,angle)
            t.place(15,px,pz,1.4)
    for angle in range(15,360,30):
        x,z=turn(0,81,angle)
        t.place(19,x,z,.85)
        x,z=turn(0,62,angle)
        t.place(15,x,z,1.45)
    t.place(9,0,0,1.05)
    # Outside-only exits preserve all four residential loops and their ponds.
    t.gates(join_x=177,width=8)
    t.notes=["Working label for Orin's unnamed home village; not a new canon proper name",
        'Central circular civic island and four round residential neighborhoods',
        'Eight satellite fountain gardens with smooth curved banks and lantern-lit paths']
    return t
