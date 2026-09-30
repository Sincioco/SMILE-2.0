"""Subtract placed assembly floors from generated terrain railing segments."""
import math


def footprints(document, catalog):
    result = []
    for item in document.get('items', []):
        for floor in catalog['templates'][item['template']].get('floors', []):
            result.append((item, floor))
    return result


def rail_parts(start, finish, decks):
    covered = []
    for item, floor in decks:
        angle = math.radians(item['yaw'])
        cosine, sine = math.cos(angle), math.sin(angle)
        sx, _, sz = item['scale']

        def local(point):
            x, z = point[0]-item['position'][0], point[1]-item['position'][2]
            return ((x*cosine-z*sine)*1000/sx, (x*sine+z*cosine)*1000/sz)

        first, last = 0., 1.
        a, b = local(start), local(finish)
        margin = 2250/min(sx, sz)
        for axis in range(2):
            low, high = floor[axis]*10-margin, floor[axis+2]*10+margin
            delta = b[axis]-a[axis]
            if abs(delta) < .000001:
                if not low <= a[axis] <= high:
                    last = -1
                    break
            else:
                enter, leave = sorted(((low-a[axis])/delta, (high-a[axis])/delta))
                first, last = max(first, enter), min(last, leave)
        if last > first:
            covered.append((first, last))
    cursor = 0.
    for first, last in sorted(covered):
        if first > cursor:
            yield cursor, first
        cursor = max(cursor, last)
    if cursor < 1:
        yield cursor, 1.
