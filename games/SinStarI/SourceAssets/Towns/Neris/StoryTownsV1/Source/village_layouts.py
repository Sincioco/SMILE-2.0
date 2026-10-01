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
    t.disk(0,0,177)
    # Quiet ponds occupy gardens; the village streets and home plots stay on land.
    t.rect(-149,-149,149,149)
    for side in (-1,1):
        for x,z,r in ((87,55,26),(99,67,20),(74,45,17)):
            t.disk(side*x,z,r,WATER)
    t.path([(-132,-132),(132,-132),(132,132),(-132,132),(-132,-132)],12)
    t.path([(-190,0),(190,0)],14)
    t.path([(0,-177),(0,95)],14)
    for side in (-1,1):
        for x,z,yaw in ((52,-108,0),(92,-108,0),(110,-66,-side*90),
                        (110,-30,-side*90),(72,108,180)):
            t.place(1 if x==52 else 0,side*x,z,1.25,yaw)
        t.place(8,side*37,-56,1.1,side*90)
        t.place(17,side*29,-18,1.5,side*90)
        t.grove(side*48,51,2,3,8,1.8)
        t.grove(side*30,36,2,4,9,1.9)
        t.grove(side*79,-57,2,3,9,1.65)
        t.grove(side*19,-90,1,4,9,1.75)
        for x,z in ((17,-165),(17,-100),(17,-35),(17,70),(81,-144),
                    (145,-45),(145,15),(145,57),(122,112),(64,144),(157,-17)):
            t.place(15,side*x,z,1.65)
        t.place(20,side*47,91,1.2)
        t.place(14,side*43,78,1.4)
        t.place(16,side*87,121,1.5)
    t.place(9,0,119,1.25)
    t.gates()
    t.notes=["Working label for Orin's unnamed home village; not a new canon proper name",
        'Castle-free City Hall, family homes and armor-repair stalls',
        'Sheltered garden ponds, clear village streets and lantern-lit home entrances']
    return t
