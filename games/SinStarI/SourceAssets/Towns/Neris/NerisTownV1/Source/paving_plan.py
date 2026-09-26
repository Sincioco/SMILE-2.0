"""Connected street footprints in Blender metres, independent of mesh objects."""
import math

CANALS = [(x-1.5, -38.5, x+1.5, 14.5) for x in (-12.5, 12.5)]
BRIDGES = [(x-2.1, y-2.4, x+2.1, y+2.4)
           for x in (-12.5, 12.5) for y in (-32, -10, 14)]
COMPARISON_MOAT = [(-359,65,-157,89),(-359,263,-157,287),
                   (-359,89,-335,263),(-181,89,-157,263)]
COMPARISON_BRIDGE = (-266,60,-250,98)
ROYAL_MOAT = [(-150,82,76,114),(-150,252,76,284),
              (-150,114,-114,252),(40,114,76,252)]
ROYAL_BRIDGE = (-46,76,-28,130)
DISTRICTS = {
    'Estates': ([-128, -96, -56], [-46, 0, 46]),
    'Homes': ([56, 94, 128], [-46, -14, 16, 46]),
    'Workers': ([-80, -54, -30, 0, 30, 54, 80], [-62, -90, -114]),
}


def rect(x, y, width, depth):
    return (x-width/2, y-depth/2, x+width/2, y+depth/2)


def plan(layout):
    roads = [rect(0, y, 260, 8) for y in (-60, 46, 60)]
    roads += [rect(0, -74, 8, 80), rect(0, -3, 18, 80),
              rect(0, 7, 34, 20), rect(0, 26, 28, 30),
              rect(-48, 74, 10, 28), rect(66, 76, 12, 32),
              rect(15, 75, 22, 20), rect(15, 64, 8, 14)]
    # A civic ring connects the royal district around City Hall to its front plaza.
    roads += [rect(x, 22, 6, 84) for x in (-21, 21)]
    roads += [rect(0, y, 116, 6) for y in (-32, -10, 14, 40)]
    for xs, ys in DISTRICTS.values():
        low, high = min(ys), max(ys)
        roads += [rect(x, (low+high)/2, 4, high-low+4) for x in xs]
        roads += [rect((min(xs)+max(xs))/2, y, max(xs)-min(xs)+4, 4) for y in ys]
    roads += [rect(x, 7, 4, 110) for x in (-128, -96, -56, 56, 94, 128)]
    roads += [rect(x, -54, 4, 16) for x in (-128, -96, -56, 56, 94, 128)]
    roads += [rect(x, -86, 4, 56) for x in (-80, 80)]
    # Temporary comparison castle remains beside the accepted royal castle.
    if 'comparisonCastle' in layout:
        roads += [rect(-245,60,244,8),rect(-258,79,12,38),
                  rect(-365,176,4,234),rect(-154,176,4,234),
                  rect(-260,293,214,4)]
    if 'royalRebuild' in layout:
        roads += [rect(-37,77,14,38),rect(-37,105,17,40),
                  rect(115,60,118,8),rect(115,102,12,84),
                  rect(171,177,4,228),rect(9,293,328,4)]
    for x, ys in [(32, [-22, 0, 22]), (-34, [-33, -14, 6, 25])]:
        roads += [rect(x, y, 20, 18) for y in ys]
    roads += [rect(x, 36, 14, 12) for x in (-36, 32)]
    roads += [rect(31, -36, 14, 12), rect(-34, -3.5, 20, 5), rect(-24, -59, 14, 6)]
    for home in layout['homes']:
        x, y = home['x'], home['y']
        ys = DISTRICTS[{'Large':'Estates','Medium':'Homes','Small':'Workers'}[home['style']]][1]
        door = y-home['depth']/2-.5
        street = max(v for v in ys if v < door)
        roads.append(rect(x, (door+street)/2, 4, door-street+2))
    # Normal street edges share the same two-metre grid. Bridge edges follow banks.
    return [(math.floor(a/2)*2, math.floor(b/2)*2,
             math.ceil(c/2)*2, math.ceil(d/2)*2) for a,b,c,d in roads]


def subtract(rectangles, holes):
    """Subtract openings without reconstructing their bounding boxes."""
    result = list(rectangles)
    for hx0, hy0, hx1, hy1 in holes:
        remaining = []
        for x0,y0,x1,y1 in result:
            a,b,c,d = max(x0,hx0), max(y0,hy0), min(x1,hx1), min(y1,hy1)
            if a >= c or b >= d:
                remaining.append((x0,y0,x1,y1))
                continue
            remaining += [(x0,y0,a,y1),(c,y0,x1,y1),(a,y0,c,b),(a,d,c,y1)]
        result = [r for r in remaining if r[0]<r[2] and r[1]<r[3]]
    return result
