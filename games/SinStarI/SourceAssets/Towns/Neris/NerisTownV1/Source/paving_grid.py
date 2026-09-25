"""World-aligned two-metre paving, clipped to the union of supplied footprints."""
import math

TILE_SIZE = 2.0
HALF_GAP = .025


def tiles(rectangles):
    """Yield non-overlapping (bounds, tone) faces with seams only on the world grid."""
    cells = {}
    for left, front, right, back in rectangles:
        for i in range(math.floor(left / TILE_SIZE), math.ceil(right / TILE_SIZE)):
            for j in range(math.floor(front / TILE_SIZE), math.ceil(back / TILE_SIZE)):
                bounds = (max(left, i*TILE_SIZE+HALF_GAP),
                          max(front, j*TILE_SIZE+HALF_GAP),
                          min(right, (i+1)*TILE_SIZE-HALF_GAP),
                          min(back, (j+1)*TILE_SIZE-HALF_GAP))
                if bounds[0] < bounds[2] and bounds[1] < bounds[3]:
                    cells.setdefault((i, j), []).append(bounds)
    for (i, j), regions in sorted(cells.items()):
        # Union within this tile. Merge touching strips without inventing grout
        # at path intersections or drawing overlapping coplanar surfaces.
        xs = sorted({x for r in regions for x in (r[0], r[2])})
        strips = []
        for x0, x1 in zip(xs, xs[1:]):
            spans = sorted((r[1], r[3]) for r in regions if r[0] <= x0 and r[2] >= x1)
            merged = []
            for y0, y1 in spans:
                if merged and y0 <= merged[-1][1]:
                    merged[-1] = (merged[-1][0], max(y1, merged[-1][1]))
                else:
                    merged.append((y0, y1))
            for y0, y1 in merged:
                previous = next((k for k, r in enumerate(strips)
                                 if r[2] == x0 and r[1] == y0 and r[3] == y1), None)
                if previous is None:
                    strips.append((x0, y0, x1, y1))
                else:
                    strips[previous] = (strips[previous][0], y0, x1, y1)
        # The same tile keeps its tone across separate promenade/bridge meshes.
        tone = int(((i*73856093) ^ (j*19349663)) % 100 < 8)
        for bounds in strips:
            yield bounds, tone


def geometry(rectangles, height):
    vertices, faces, tones = [], [], []
    for (left, front, right, back), tone in tiles(rectangles):
        n = len(vertices)
        vertices.extend([(left, front, height), (right, front, height),
                         (right, back, height), (left, back, height)])
        faces.append((n, n+1, n+2, n+3))
        tones.append(tone)
    return vertices, faces, tones
