"""Template-local walk surfaces, solids and water; no saved-scene mutations."""
from itertools import product
from mathutils import Vector
from collision_bounds import solid_bounds


def collect(instance, owner, result):
    obj = instance.object
    inverse = owner.matrix.inverted()
    matrix = inverse @ instance.matrix_world
    name = obj.name.lower()
    if obj.type != 'MESH':
        return
    if owner.label in ('Leafy Tree', 'Slender Tree'):
        # Joined foliage must not turn the whole canopy into a ground obstacle.
        result['solids'].append([-.8, -.8, .8, .8])
        return
    corners = [matrix @ Vector(p) for p in obj.bound_box]
    low = [min(p[i] for p in corners) for i in range(3)]
    high = [max(p[i] for p in corners) for i in range(3)]
    if obj.get('neris_native_water') or obj.name.startswith(('Fountain Water', 'Royal Fountain Water')):
        result['water'].append([(low[0]+high[0])/2, (low[1]+high[1])/2,
                                high[2], (high[0]-low[0])/2, (high[1]-low[1])/2])
        return
    if obj.name.startswith(('Entrance Step', 'Front Door Step', 'Royal Entrance Stair',
                            'Sweeping Royal Garden Stair', 'Entrance Threshold')):
        # These templates have horizontal quarter-turn stair runs. Retain local
        # rectangles so rotating a placed building rotates its stairs as well.
        result['steps'].append(low[:2] + high[:2] + [high[2]])
        return
    if obj.get('neris_door_leaf') or any(s in name for s in
            ('leaf', 'leaves', 'grass', 'flower', 'petal', 'water', 'step', 'stair', 'threshold')):
        return
    for world_low, world_high in solid_bounds(instance):
        points = [inverse @ Vector(p) for p in product(*zip(world_low, world_high))]
        if (world_high[2] >= 1 and min(world_high[i]-world_low[i] for i in range(3)) >= .18
                and not any(s in name for s in ('banner', 'flag'))):
            result['camera'].append([min(p[i] for p in points) for i in range(3)] +
                                    [max(p[i] for p in points) for i in range(3)])
        if world_low[2] > 2.4 or world_high[2] < .65:
            continue
        box = [min(p[i] for p in points) for i in (0, 1)] + [max(p[i] for p in points) for i in (0, 1)]
        if min(box[2]-box[0], box[3]-box[1]) >= .015:
            result['solids'].append(box)


def finish(value):
    for key in value:
        value[key] = sorted(set(tuple(round(v, 6) for v in row) for row in value[key]))
    kept = []
    for box in sorted(value['solids'], key=lambda b: -(b[2]-b[0])*(b[3]-b[1])):
        if not any(a <= box[0] and b <= box[1] and c >= box[2] and d >= box[3]
                   for a, b, c, d in kept):
            kept.append(box)
    value['solids'] = kept
    boxes = value['camera']
    volume = lambda v: (v[3]-v[0])*(v[4]-v[1])*(v[5]-v[2])
    for _ in range(3):
        merged = []
        for box in sorted(boxes, key=lambda b: -volume(b)):
            for i, previous in enumerate(merged):
                if any(previous[j] > box[j+3]+.4 or box[j] > previous[j+3]+.4 for j in range(3)):
                    continue
                union = tuple(min(previous[j], box[j]) for j in range(3)) + tuple(max(previous[j], box[j]) for j in range(3, 6))
                overlap = 1
                for j in range(3):
                    overlap *= max(0, min(previous[j+3], box[j+3])-max(previous[j], box[j]))
                if volume(union) <= (volume(previous)+volume(box)-overlap)*1.12:
                    merged[i] = union
                    break
            else:
                merged.append(box)
        boxes = merged
    value['camera'] = boxes
    return value
