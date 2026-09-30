"""Two modest, castle-free story locations with a City Hall and clear public paths."""
from town_design import Town, WATER, ROAD


def east_valley():
    t=Town('East Valley',420,smooth=True)
    t.rect(-174,-174,174,174)
    # A cross of waterways and mirrored orchard terraces surround the civic island.
    t.rect(-25,-174,25,174,WATER)
    t.rect(-174,63,174,87,WATER)
    t.rect(-47,75,47,174)
    t.rect(-45,-174,45,50)
    for x in (-140,-70,70,140):
        t.path([(x,-160),(x,158)],10)
    for z in (-145,-75,0,125):
        t.path([(-190,z),(190,z)],12)
    t.path([(0,-174),(0,92)],14)
    t.ring(0,-65,22,8)
    t.place(9,0,122,1.35)
    t.place(20,0,-65,1.25)
    for side in (-1,1):
        for z in (-120,-40,32,102,151):
            t.place(0,side*102,z,1.25,-side*90)
        for z in (-120,-40,34):
            t.grove(side*158,z,2,3,8,1.55)
            t.grove(side*48,z,1,3,9,1.65)
        t.grove(side*70,151,3,2,9,1.6)
        t.place(8,side*29,-122,1.05,side*90)
        t.place(17,side*29,-25,1.5,side*90)
        # Paired quiet memorial gardens, leaving space for the story's relief work.
        t.place(14,side*33,34,1.4,side*90)
        t.place(16,side*32,20,1.6)
        t.place(16,side*32,48,1.6)
        for z in (-150,-95,-35,30,103,155):
            t.place(15,side*61,z,1.6)
            t.place(15,side*149,z,1.7)
        t.place(15,side*16,91,1.8)
    t.gates()
    t.notes=['Castle-free relief village with a public City Hall',
        'Modest homes, paired orchards and practical market shelters',
        'Quiet gardens for the East Valley memorial and resident-ledger scenes']
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
