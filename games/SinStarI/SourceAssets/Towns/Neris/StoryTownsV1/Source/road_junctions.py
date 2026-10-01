"""Author tangent road corners and trim caps that protrude beyond ring roads."""
import math
from itertools import combinations
from town_access import surface, outline
from town_document_codec import curve_contains, curve_bounds, raster_curves


def dot(a, b):
    return a[0]*b[0]+a[1]*b[1]


def add(a, b, scale=1):
    return a[0]+b[0]*scale, a[1]+b[1]*scale


def sub(a, b):
    return a[0]-b[0], a[1]-b[1]


def unit(a):
    length = math.hypot(*a)
    return a[0]/length, a[1]/length


def crossing(n, d, m, e):
    det = n[0]*m[1]-n[1]*m[0]
    if abs(det) < .00001:
        return None
    return (d*m[1]-n[1]*e)/det, (n[0]*e-d*m[0])/det


def trim_ring_caps(doc):
    """Shorten only the road's buried end, leaving the other endpoint in place."""
    rings = [b for b in doc['curves'] if b[:2] == [3, 3]]
    changed = 0
    for line in doc['curves']:
        if line[:2] != [4, 3]:
            continue
        for index, other in ((2, 4), (4, 2)):
            end, start = tuple(line[index:index+2]), tuple(line[other:other+2])
            for _, _, x, z, radius, _, width in rings:
                distance = math.dist(end, (x,z))
                if abs(distance-radius) > width/2 or line[6] <= width:
                    continue
                inside = math.dist(start, (x,z)) < radius-width
                outside = math.dist(start, (x,z)) > radius+width
                if not inside and not outside:
                    continue
                target = radius + (width-line[6])/2 - .1 if inside else radius + (line[6]-width)/2 + .1
                direction = unit(sub(start, end))
                low, high = 0.0, min(math.dist(start,end)*.25, line[6])
                if (inside and math.dist(add(end,direction,high),(x,z)) > target or
                        outside and math.dist(add(end,direction,high),(x,z)) < target):
                    continue
                for _ in range(32):
                    mid = (low+high)/2
                    test = math.dist(add(end,direction,mid),(x,z))
                    if (inside and test > target) or (outside and test < target):
                        low = mid
                    else:
                        high = mid
                end = add(end,direction,high)
                line[index:index+2] = end
                changed += 1
    return changed


def boundaries(doc):
    result = []
    for index, brush in enumerate(doc['curves']):
        form, kind, x, z, u, v, width = brush[:7]
        if kind != 3 or form not in (3,4):
            continue
        if form == 4:
            a, b = (x,z), (u,v)
            if math.dist(a,b) < .001:
                continue
            direction = unit(sub(b,a))
            for side in (-1,1):
                normal = -direction[1]*side, direction[0]*side
                result.append(dict(kind='line', index=index, a=a, b=b, normal=normal,
                                   distance=dot(normal,a)+width/2, width=width))
        else:
            for side in (-1,1):
                result.append(dict(kind='circle', index=index, center=(x,z),
                                   radius=u+side*width/2, side=side, width=width))
    return result


def centers(a, b, radius):
    if a['kind'] == b['kind'] == 'line':
        point = crossing(a['normal'],a['distance']+radius,b['normal'],b['distance']+radius)
        return [] if point is None else [point]
    if a['kind'] == b['kind']:
        return []  # Current authored rings do not intersect one another.
    if a['kind'] == 'circle':
        a,b = b,a
    offset = a['distance']+radius-dot(a['normal'],b['center'])
    adjusted = b['radius']+b['side']*radius
    if adjusted <= 0 or abs(offset) >= adjusted:
        return []
    base = add(b['center'],a['normal'],offset)
    delta = math.sqrt(adjusted*adjusted-offset*offset)
    tangent = -a['normal'][1],a['normal'][0]
    return [add(base,tangent,delta),add(base,tangent,-delta)]


def tangent(boundary, center, radius):
    if boundary['kind'] == 'line':
        point = add(center,boundary['normal'],-radius)
        line = sub(boundary['b'],boundary['a'])
        amount = dot(sub(point,boundary['a']),line)/dot(line,line)
        return point if 0 <= amount <= 1 else None
    return add(boundary['center'],unit(sub(center,boundary['center'])),boundary['radius'])


def clear_footprints(doc, brush, footprint):
    low,high,top,bottom = curve_bounds(brush)
    return not any(low <= x <= high and top <= z <= bottom and curve_contains(brush,x,z)
                   and surface(doc,x,z) not in (3,4) for x,z in footprint)


def crossings(doc, footprint):
    """A complete orthogonal crossing stores its four corners in one brush."""
    lines = [(i,b) for i,b in enumerate(doc['curves']) if b[:2] == [4,3]]
    added, pairs = [],set()
    for (i,a),(j,b) in combinations(lines,2):
        if abs(a[2]-a[4]) > .001:
            a,b = b,a
        if abs(a[2]-a[4]) > .001 or abs(b[3]-b[5]) > .001:
            continue
        x,z = a[2],b[3]
        half_x,half_z,radius = a[6]/2,b[6]/2,min(60.0,min(a[6],b[6])*.45)
        if (min(a[3],a[5]) > z-half_z-radius or max(a[3],a[5]) < z+half_z+radius or
                min(b[2],b[4]) > x-half_x-radius or max(b[2],b[4]) < x+half_x+radius):
            continue
        brush = [6,3,x,z,half_x,half_z,radius]
        if clear_footprints(doc,brush,footprint):
            added.append(brush)
            pairs.add((i,j))
    return added,pairs


def round_junctions(doc):
    """Add only exposed concave corners, preserving scenery and connected roads."""
    assert not any(b[0] in (5,6) for b in doc['curves']), 'Junctions are already prepared'
    trimmed = trim_ring_caps(doc)
    edges = boundaries(doc)
    footprint = [(x*10,z*10) for item in doc['items']
                 if item['template'] not in (7,13,27,35,36,37)
                 for x,z in outline(item)]
    added,pairs = crossings(doc,footprint)
    crossing_count = len(added)
    for a,b in combinations(edges,2):
        if a['index'] == b['index'] or (a['index'],b['index']) in pairs:
            continue
        radius = min(60.0, min(a['width'],b['width'])*.45)
        for center in centers(a,b,radius):
            if surface(doc,*center) in (3,4):
                continue
            p, q = tangent(a,center,radius),tangent(b,center,radius)
            if p is None or q is None:
                continue
            n,m = sub(p,center),sub(q,center)
            corner = crossing(n,dot(n,p),m,dot(m,q))
            if corner is None or math.dist(corner,center) > radius*3:
                continue
            probe = add(corner,unit(sub(corner,center)),1.0)
            if surface(doc,*probe) not in (3,4):
                continue
            # Both contacts must be visible road boundaries, not hidden under another road.
            if any(surface(doc,*add(p,unit(sub(center,p)),1.0)) in (3,4) for p in (p,q)):
                continue
            brush = [5,3,*center,*corner,radius]
            if not clear_footprints(doc,brush,footprint):
                continue
            if any(math.dist(center,(old[2],old[3])) < .01 for old in added):
                continue
            added.append(brush)
    assert len(doc['curves'])+len(added) <= 256, (doc['name'],'brush budget',len(added))
    doc['curves'].extend(added)
    doc['cells'] = raster_curves(doc)
    return dict(trimmed_caps=trimmed, rounded_corners=len(added)+crossing_count*3,
                junction_brushes=len(added),total_brushes=len(doc['curves']))
