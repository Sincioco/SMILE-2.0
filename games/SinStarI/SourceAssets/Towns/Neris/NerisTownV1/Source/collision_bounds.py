"""Shared exported solid bounds; preserve the hollow royal portal opening."""
from mathutils import Vector


def solid_bounds(instance):
    obj = instance.object
    points = [instance.matrix_world @ Vector(p) for p in obj.bound_box]
    if obj.name.startswith('Royal Pointed Portal'):
        # These single meshes contain both jambs and the pointed crown. A whole
        # mesh AABB seals the gate. Bound each jamb below the arch springline,
        # and only the crown above it. Rotation stays in the instance transform.
        vertices = [v.co for v in obj.data.vertices]
        middle = (min(p.x for p in vertices) + max(p.x for p in vertices)) / 2
        bottom = min(p.z for p in vertices)
        spring = bottom + (max(p.z for p in vertices) - bottom) * .60
        groups = [[p for p in vertices if p.z <= spring and p.x < middle],
                  [p for p in vertices if p.z <= spring and p.x >= middle],
                  [p for p in vertices if p.z >= spring]]
        for group in groups:
            if not group:
                continue
            # Pad the top group down to the split, covering connecting faces.
            if group is groups[2]:
                group = group + [Vector((p.x, p.y, spring)) for p in group]
            yield bounds([instance.matrix_world @ p for p in group])
    else:
        yield bounds(points)


def bounds(points):
    return ([min(p[i] for p in points) for i in range(3)],
            [max(p[i] for p in points) for i in range(3)])
