"""A compact relief town between mountain foothills, a lake and an arid quarter."""
import math
from town_design import Town, ROAD
from journey_layouts import smooth, distance


MAIN = [(-200,0),(-132,0),(-70,-22),(0,0),(70,-16),(128,0),(200,0)]


def height(x,z,items=(),paths=()):
    value = sum(peak*smooth(1-math.hypot((x-cx)/rx,(z-cz)/rz))
        for cx,cz,rx,rz,peak in [(-135,149,103,100,49),(-42,158,94,84,61),
                               (151,-141,85,90,21),(-155,-150,73,77,13)])
    value *= smooth((distance(x,z,MAIN)-16)/40)
    value *= smooth((math.hypot(x-109,z-96)-61)/33)
    for path in paths:
        value *= smooth((distance(x,z,path)-12)/40)
    for item in items:
        if item['template'] < 14:
            cx,cz=item['position'][0]/10,item['position'][2]/10
            value *= smooth((math.hypot(x-cx,z-cz)-15)/20)
    return value


def style(x,z):
    if x>65+12*math.sin(z/36) and z < -38+10*math.sin(x/29):
        return 4  # Explicit Desert; the whole-map Meadow remains elsewhere.
    if z>93 and x<42 and height(x,z)>14:
        return 3  # Exposed Highland rock on the mountains only.
    if z>42:
        return 2
    return 1


def build():
    town=Town('Neris Relief Quarter',400,4,True)
    town.symmetric=False
    town.night=False
    town.wilderness=True
    town.rect(-200,-200,200,200)
    town.lake((109,96),[(59,79),(69,45),(96,57),(120,43),(146,67),
        (158,94),(142,109),(145,141),(112,151),(92,128),(69,132)])
    main=town.path(MAIN,14)
    market=town.path([(-132,0),(-152,-64),(-113,-108),(-45,-117),
        (3,-78),(0,0)],10)
    gardens=town.path([(-70,-22),(-78,44),(-28,73),(22,65),(43,15),(70,-16)],9)
    desert=town.path([(70,-16),(78,-79),(113,-106),(171,-88)],8)
    town.rect(-27,-8,27,31,ROAD)
    town.place(9,0,48,1.4,180)
    for template,x,z,yaw in [(0,-173,-51,90),(1,-111,-139,0),(4,-57,-148,0),
        (5,-25,-95,270),(0,-113,-18,270),(3,-113,44,90),(2,44,88,180),
        (5,66,-108,90),(2,151,-83,270)]:
        town.place(template,x,z,1.4,yaw)
    for x,z in [(-108,-72),(-76,-79),(-49,-63)]:
        town.place(17,x,z,1.4,0)
    for x,z in [(-170,90),(-130,90),(-62,98),(30,131),(-153,-172),(-45,-172)]:
        town.grove(x,z,2,2,10,2.4)
    town.place(36,158,-158,.45,20)
    town.place(39,164,-102,1.2,0)
    town.place(39,41,109,1.1,0)
    town.lamps([(-132,17),(-141,-90),(-69,-100),(-7,-49),(-70,55),(18,28)],1.4)
    town.gates(destinations=('Neris Town','Neris Waterworks'),width=14)
    town.height=lambda x,z:height(x,z,town.items,[main,market,gardens,desert])
    main[0]=(-196,0)
    main[-1]=(196,0)
    town.acceptance_paths=[main,market,gardens,desert]
    town.notes=['Relief town with a clinic plaza, supply market and cottages on gentle terraces.',
        'Northern mountain foothills frame a woodland lake; southeastern paths enter sandy hills and an oasis pavilion.',
        'Distinct connected districts, level building plots and broad traversable approaches.']
    return town
