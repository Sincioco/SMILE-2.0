"""Sin's September 26 district diagram, in metres. North is positive Y.

One data owner drives authored ground, water, bridges, collision and the map.
The Tripo comparison is a separate western addition to the requested diagram.
"""
from paving_plan import rect, subtract

BOUNDS = (-490, -355, 275, 355)
MAIN = (-240, -340, 260, 352)
TRIPO_LAND = (-484, 90, -238, 352)
TRIPO_CONNECTION = (-270, 90, -230, 118)
CASTLE = (-116, 235)
TRIPO = (-366, 226)
MILITARY = (145, 242)
HALL = (0, -75)
TOWER = (0, 60)
TOWER_SCALE = 4
HALL_SCALE = 2
# Stair foot is Y=-93.48; the retained fountain at -112 bisects this forecourt.
HALL_PLAZA = (-50, -130.52, 50, -44)
ARRIVAL_SIGN_Y = -265
LEGACY_HOMES = [(-214,-163,90),(-214,-247,90),(-214,-289,90),
                (-122,-163,90),(-122,-247,90),(-122,-289,90),
                (232,29,-90),(232,-73,-90)]


def ring(outer, inner):
    a,b,c,d = outer
    e,f,g,h = inner
    return [(a,b,c,f),(a,h,c,d),(a,f,e,h),(g,f,c,h)]


ROYAL_WATER = ring((-229,133,-3,335),(-193,165,-39,303))
TRIPO_WATER = ring((-467,115,-265,337),(-443,139,-289,313))
HQ_WATER = ring((46,151,244,333),(68,173,222,311))
CIVIC_WATER = [(-66,-340,-50,-44),(50,-340,66,-44),(-66,-44,66,-28)]
OUTER_WATER = subtract([BOUNDS], [MAIN, TRIPO_LAND, TRIPO_CONNECTION])
WATER = ROYAL_WATER + TRIPO_WATER + HQ_WATER + CIVIC_WATER + OUTER_WATER
BRIDGES = [(-125,127,-107,181),(-374,106,-358,148),(131,140,159,194),
           (-73,-117,-43,-107),(43,-117,73,-107),
           (-73,-176,-43,-164),(43,-176,73,-164),(-73,-286,-43,-274),
           (43,-286,73,-274),(-7,-52,7,-20)]


def homes():
    result = []
    for style,xs,ys,w,d,yaw in [
        ('Large',[-208,-126],[-38,4,46,88],17,15,90),
        ('Medium',[-210,-118],[-142,-184,-226,-268,-310],11.2,12,90),
        ('Small',[119,213],[80,46,12,-22,-56,-90],9.3,9.5,-90)]:
        for column,x in enumerate(xs):
            for y in ys:
                result.append(dict(style=style,x=x,y=y,width=w,depth=d,yaw=yaw))
    return result


def roads():
    result = [rect(-110.5,110,733,14)]
    # A continuous land-backed circuit surrounds both side-by-side castles,
    # with a shared north/south street between their separate moats.
    result += [rect(-112,344,740,12),rect(-477,224,10,240)]
    result += [rect(x,225,14,242) for x in (-250,12,251)]
    result += [rect(10,-112,496,10)]
    result += [rect(x,-108,8,432) for x in (-86,86)]
    # Sin's yellow route: extend the main avenues along the west/east waterfront
    # and return at the southern corners. These are the town's outer routes.
    result += [rect(x,-110,14,454) for x in (-231,251)]
    result += [rect(-160,-331,156,14),rect(170,-331,176,14)]
    result += [rect(0,30,14,160),
               rect(-116,145,18,68),rect(145,151,20,90)]
    # Interior branches serve short front paths; no enclosing parallel boundary
    # roads. The space between a branch and the main avenue contains real houses.
    result += [rect(x,-110,8,454) for x in (-172,174)]
    result += [rect(-162,y,152,8) for y in (25,-205)]
    result += [rect(172,y,172,8) for y in (-5,-234)]
    # Shops and broad civic forecourt remain distinct districts.
    result += [HALL_PLAZA,rect(0,-229,14,222)]
    result += [rect(0,y,176,12) for y in (-170,-280)]
    result += [rect(*TOWER,72,70)]
    for home in homes():
        x,y = home['x'],home['y']
        dx = home['depth']/2
        if x<0:
            street = -172 if x<-190 else -86
        else:
            street = 174 if x>190 else 86
        door = x+dx if x<0 else x-dx
        result.append(rect((street+door)/2,y,abs(street-door)+3,4))
    for y in (-156,-208,-260):
        result.append(rect(107,y,44,18))
    for x,y,yaw in LEGACY_HOMES[:6]:
        street=-172 if x<-190 else -86
        result.append(rect((x+5+street)/2,y,street-x-5,4))
    for y in (29,-73):result.append(rect(200.5,y,53,4))
    return result


def data():
    return dict(revision='Waterfront districts — Sin diagram 2026-09-26',
        bounds=BOUNDS,land=[MAIN,TRIPO_LAND,TRIPO_CONNECTION],
        arrivalSign=[0,ARRIVAL_SIGN_Y],
        homes=homes(),legacyHomes=LEGACY_HOMES,castle=CASTLE,military=MILITARY,cityHall=HALL,tower=TOWER,
        comparisonCastle={'position':TRIPO},royalRebuild={'scale':2},towerScale=TOWER_SCALE,cityHallScale=HALL_SCALE,
        waterRectangles=WATER,bridges=BRIDGES,roadRectangles=roads(),
        labels=[('Old Castle',*TRIPO),('Royal Castle',*CASTLE),('Military HQ',*MILITARY),
                ('Comm Tower',*TOWER),('City Hall',*HALL),('Estates',-171,45),
                ('Homes',-175,-210),('Workers',166,0),('Weapon',125,-156),
                ('Item',125,-208),('Armor',125,-260)])
