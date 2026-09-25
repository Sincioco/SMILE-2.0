"""Reload and inspect the delivered scene and its building approaches."""
import hashlib
import json
import math
from collections import deque
from pathlib import Path

import bpy
from mathutils import Vector


PACKAGE = Path(__file__).resolve().parents[1]


def bounds(objects, transform=None):
    points = []
    for obj in objects:
        if obj.type not in {'MESH', 'CURVE', 'FONT'}:
            continue
        matrix = obj.matrix_world if transform is None else transform @ obj.matrix_world
        points.extend(matrix @ Vector(corner) for corner in obj.bound_box)
    return [[min(p[i] for p in points) for i in range(3)],
            [max(p[i] for p in points) for i in range(3)]]


def approaches(rectangles):
    """Coarse plan check only, not collision/navmesh acceptance."""
    def clear(x, y):
        if not (-48 <= x <= 48 and -42 <= y <= 42):
            return False
        if any(low[0] - .25 <= x <= high[0] + .25 and
               low[1] - .25 <= y <= high[1] + .25 for low, high in rectangles):
            return False
        if -38.5 <= y <= 14.5 and any(abs(x - c) <= 1.85 for c in [-12.5, 12.5]):
            if not any(abs(y - bridge) < 1.9 for bridge in [-32, -10, 14]):
                return False
        if x * x + (y + 20) ** 2 <= 5.3 ** 2:
            return False
        return True

    seen = {(0, -42)}
    queue = deque(seen)
    while queue:
        x, y = queue.popleft()
        for point in [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)]:
            if point not in seen and clear(*point):
                seen.add(point)
                queue.append(point)
    return seen


def main():
    bpy.ops.wm.open_mainfile(filepath=str(PACKAGE / 'Blend' / 'Neris-Town-V1.blend'))
    scene = bpy.context.scene
    bpy.context.view_layer.update()
    source_placements = json.loads((PACKAGE / 'placements.json').read_text(encoding='utf-8'))
    assert len(source_placements) == 13
    reports = []
    for placement in source_placements:
        if 'source' in placement:
            path = PACKAGE.parents[3] / placement['source']
            assert hashlib.sha256(path.read_bytes()).hexdigest() == placement['sha256']
            collection = bpy.data.collections[placement['name']]
            box = bounds(collection.objects)
        else:
            obj = bpy.data.objects[placement['name']]
            box = bounds(obj.instance_collection.objects, obj.matrix_world)
        assert all(math.isfinite(v) for p in box for v in p)
        reports.append({'name': placement['name'], 'bounds': box, 'angle': placement['angle']})
    overlaps = []
    for i, a in enumerate(reports):
        for b in reports[i + 1:]:
            amin, amax = a['bounds']; bmin, bmax = b['bounds']
            if all(min(amax[k], bmax[k]) - max(amin[k], bmin[k]) > .01 for k in [0, 1]):
                overlaps.append([a['name'], b['name']])
    assert not overlaps, overlaps
    reachable = approaches([r['bounds'] for r in reports])
    for report, placement in zip(reports, source_placements):
        low, high = report['bounds']
        x, y = placement['location'][:2]
        direction = round(placement['angle'] / (math.pi / 2))
        if direction == -1:
            target = (math.floor(low[0] - 1), round(y))
        elif direction == 1:
            target = (math.ceil(high[0] + 1), round(y))
        else:
            target = (round(x), math.floor(low[1] - 1))
        report['approach_point'] = target
        report['approach_reachable'] = target in reachable
        assert target in reachable, (report['name'], target)
    unique_triangles = 0
    for mesh in bpy.data.meshes:
        if mesh.users:
            assert all(math.isfinite(c) for vertex in mesh.vertices for c in vertex.co)
            assert mesh.materials, mesh.name
            mesh.calc_loop_triangles()
            unique_triangles += len(mesh.loop_triangles)
    assert len([o for o in scene.objects if o.type == 'CAMERA']) == 5
    assert not bpy.data.libraries, 'Scene must not require linked external libraries.'
    assert not [im.name for im in bpy.data.images if im.users and im.source == 'FILE' and not im.packed_file]
    previews = {}
    for label in ['overview', 'plan', 'arrival', 'market', 'homes']:
        path = PACKAGE / 'Previews' / f'Neris-Town-{label}.png'
        image = bpy.data.images.load(str(path))
        assert tuple(image.size) == ((1920, 1920) if label == 'plan' else (1920, 1080))
        previews[label] = {'size': list(image.size), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
        bpy.data.images.remove(image)
    # Regression: camera markers once reset every render to frame 1's overview.
    assert len({p['sha256'] for p in previews.values()}) == 5, 'Preview cameras repeated.'
    for frame, prefix in enumerate(['01', '02', '03', '04', '05'], 1):
        scene.frame_set(frame)
        assert scene.camera.name.startswith(prefix), scene.camera.name
    result = {'status': 'PASS', 'blender_version': bpy.app.version_string,
              'scene_objects': len(scene.objects), 'building_count': len(reports),
              'unique_mesh_triangles_before_modifiers_and_instancing': unique_triangles,
              'building_footprint_overlaps': overlaps, 'reachable_grid_cells': len(reachable),
              'route_check_scope': '1 m plan grid, building bounds and canal crossings only; not runtime navigation.',
              'buildings': reports, 'previews': previews,
              'blend_sha256': hashlib.sha256((PACKAGE / 'Blend' / 'Neris-Town-V1.blend').read_bytes()).hexdigest()}
    (PACKAGE / 'validation.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print('NERIS_TOWN_VALIDATION_PASS', result['scene_objects'], 'objects;', len(reports), 'building approaches')


if __name__ == '__main__':
    main()
