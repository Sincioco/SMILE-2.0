"""Landscape the two working terminals without moving their native landing assemblies."""
from math import sin, cos, radians
from town_design import Town, GROUND, WATER, ROAD


def point(r, a):
    return r*sin(radians(a)), r*cos(radians(a))


def spaceport():
    t = Town('Neris Spaceport', 1200, 5, smooth=True)
    t.center = (-550, -670)
    # A luminous orbital garden surrounds the unchanged crystal terminal and pads.
    t.disk(0, 0, 565)
    t.disk(0, 0, 488, WATER)
    t.disk(0, -50, 410)
    t.disk(0, 300, 235)
    for side in (-1, 1):
        t.disk(side*255, 260, 150)
        t.disk(side*350, 140, 65)
    t.ring(0, 0, 532, 18)
    for a in range(0, 360, 45):
        t.path([point(390, a), point(558, a)], 20)
    t.path([(-585, 0), (-335, 0), (-335, 300), (0, 300), (335, 300), (335, 0), (585, 0)], 20)
    t.path([(0, 20), (0, 550)], 28)
    t.ring(0, 355, 65, 14)
    # Immigration, embassy pavilions and observatories stay outside the landing pads.
    for side in (-1, 1):
        t.place(3, side*120, 310, 3.2, side*90)
        t.place(9, side*255, 270, 1.7, side*90)
        t.place(10, side*350, 255, 1.4)
        t.place(20, side*65, 355, 2.7)
        t.grove(side*185, 335, 4, 3, 15, 2.5)
        t.grove(side*360, 140, 2, 4, 14, 2.6)
        for z in (245, 295, 350, 405, 460):
            t.place(15, side*22, z, 2.8)
        for z in (270, 335, 390):
            t.place(17, side*98, z, 2.0, side*90)
    for a in range(0, 360, 15):
        x, z = point(550, a+7.5)
        t.place(19, x, z, 3.0, a)
        x, z = point(517, a+7.5)
        t.place(15, x, z, 2.6)
    for a in (45, 135, 225, 315):
        x, z = point(452, a)
        t.disk(x, z, 32)
        t.place(2, x, z, 2.2, a)
        for da in (-10, 10):
            x, z = point(465, a+da)
            t.disk(x, z, 12)
            t.place(20, x, z, 2.0)
    for a in range(0, 360, 45):
        t.path([point(390, a), point(558, a)], 20)
    t.gates(destinations=('Neris Town', 'Horizon Airport'))
    t.notes = ['Orbital welcome gardens and eight illuminated approach bridges',
               'Embassy pavilions, arrival plaza and water observatories',
               'Original terminal, doors, pads and alien arrival schedules retained']
    return t


def airport():
    t = Town('Horizon Airport', 1400, 5, smooth=True)
    t.center = (840, 125)
    t.symmetric = False
    # Keep the complete operating field clear. Wrap its land in scalloped coastal gardens.
    t.rect(-300, -500, 300, 500)
    for z in (-410, -205, 0, 205, 410):
        for side in (-1, 1):
            t.disk(side*295, z, 142)
            t.disk(side*385, z, 62, WATER)
    t.rect(-180, -615, 180, 615)
    t.path([(-535, 0), (-330, 0), (-330, -550), (330, -550), (330, 550), (-330, 550), (-330, 0)], 22)
    t.path([(-688, 0), (-330, 0)], 22)
    t.path([(330, 0), (688, 0)], 22)
    for side in (-1, 1):
        # Forecourt hotels and civic services; nothing crosses the runway or taxiways.
        t.disk(side*475, 0, 112)
        t.ring(side*475, 0, 82, 16)
        t.path([(side*330, 0), (side*585, 0)], 22)
        t.place(9, side*475, 36, 1.8)
        t.place(20, side*475, -35, 2.2)
        for dz in (-55, 55):
            t.place(3, side*530, dz, 2.1, side*90)
            t.grove(side*418, dz, 2, 3, 11, 2.1)
        for z in (-510, -315, -105, 105, 315, 510):
            t.place(15, side*320, z, 2.3)
        for z in (-410, -205, 205, 410):
            t.grove(side*310, z, 2, 4, 12, 2.6)
            t.disk(side*430, z, 24)
            t.path([(side*330, z), (side*417, z)], 10)
            t.place(2, side*430, z, 1.6, side*90)
        for dx in (-65, 65):
            for dz in (-65, 65):
                t.place(15, side*475+dx, dz, 2.5)
    for z in (-595, 595):
        for x in (-220, -110, 0, 110, 220):
            t.disk(x, z, 25)
            t.grove(x, z, 2, 2, 10, 2.4)
            t.place(15, x, z-18, 2.3)
    t.gates(destinations=('Neris Town', 'Neris Spaceport'))
    t.notes = ['Scalloped coastline and repeated sheltered lagoons',
               'Twin circular arrival villages and tree-lined perimeter drive',
               'Operating terminal, runway, aircraft and navigation beacons retained']
    return t
