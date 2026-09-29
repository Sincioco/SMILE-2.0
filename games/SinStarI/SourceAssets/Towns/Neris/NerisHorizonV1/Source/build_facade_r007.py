"""Castle-inspired finishes on the retained Gentle Wave airport, with real skylights."""
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Revisions/r007'
PREVIEWS = ROOT / 'Previews/r007'
(OUT / 'Native').mkdir(parents=True, exist_ok=True)
PREVIEWS.mkdir(parents=True, exist_ok=True)
sys.path.insert(0, str(Path(__file__).parent))
import forms as F
from forms import box, beam, panel, mesh

bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'Source/Neris-Horizon-Gentle-Wave-r006.blend'))
scene = bpy.context.scene
scene.name = 'Horizon Gentle Wave r007'
for mat in bpy.data.materials:
    if mat.name.startswith('GW '):
        F.MATERIALS[mat.name[3:]] = mat

# Read the actual accepted castle palette, not colors sampled from a lit screenshot.
castle = {}
for path in (ROOT.parent / 'NerisCastleM06V2/Native').glob('*.glb'):
    data = path.read_bytes()
    document = json.loads(data[20:20 + int.from_bytes(data[12:16], 'little')])
    for mat in document.get('materials', []):
        castle[mat['name']] = mat['pbrMetallicRoughness']
for name, source in [('Teal', 'TealRoof'), ('Gold', 'RoyalGold'), ('Glass Dark', 'RoyalGlass')]:
    values = castle['NC.MAT.' + source]
    node = F.MATERIALS[name].node_tree.nodes['Principled BSDF']
    node.inputs['Base Color'].default_value = values['baseColorFactor']
    node.inputs['Metallic'].default_value = values['metallicFactor']
    node.inputs['Roughness'].default_value = values['roughnessFactor']
    node.inputs['Alpha'].default_value = 1

# The earlier halls used opaque blue glazing; r005's shared alpha change erased it.
F.material('Hall Glass', (.035, .23, .31), .53, .19, .08).name = 'GW Hall Glass'
F.material('Garden', (.035, .115, .042), rough=.83).name = 'GW Garden'


def bounds(obj):
    points = [obj.matrix_world @ Vector(p) for p in obj.bound_box]
    return [min(p[a] for p in points) for a in range(3)], [max(p[a] for p in points) for a in range(3)]


def tag_new(before, assembly):
    for obj in set(scene.objects) - before:
        obj['horizon_asset'] = True
        obj['assembly'] = assembly


def finish(obj, name):
    obj.data.materials.clear()
    obj.data.materials.append(F.MATERIALS[name])
    for poly in obj.data.polygons:
        poly.material_index = 0


def roof_z(x, y=0):
    return (53 + 24 * math.exp(-(x / 90) ** 2)
            + 10 * math.exp(-((abs(x) - 228) / 53) ** 2)
            + 2 * math.sin(math.pi * max(0, min(150, y)) / 150))


def roof_shell(label, cells, thickness):
    vertices, faces, edges = [], [], {}
    for top in cells:
        start = len(vertices)
        vertices.extend(top + [(x, y, z - thickness) for x, y, z in top])
        faces.extend([(start, start + 1, start + 2, start + 3),
                      (start + 7, start + 6, start + 5, start + 4)])
        for i in range(4):
            a, b = top[i], top[(i + 1) % 4]
            key = tuple(sorted((tuple(a), tuple(b))))
            if key in edges:
                edges[key] = None
            else:
                edges[key] = (start + i, start + (i + 1) % 4)
    for pair in edges.values():
        if pair is not None:
            a, b = pair
            faces.append((a, a + 4, b + 4, b))
    return mesh(label, vertices, faces, 'Teal')


def arch(label, cx, y, bottom, top, half, width, material):
    spring = bottom + (top - bottom) * .60
    for sign in (-1, 1):
        beam(label, (cx + sign * half, y, bottom), (cx + sign * half, y, spring), width, material)
        def point(t):
            return (cx + sign * half * (1 - t) * (1 + .42 * t), y,
                    spring + (top - spring) * (2 * t - t * t))
        for i in range(8):
            beam(label, point(i / 8), point((i + 1) / 8), width, material)


def medallion(cx, y, z, size):
    for i in range(32):
        a, b = i * math.tau / 32, (i + 1) * math.tau / 32
        beam('Neris Compass Ring', (cx + size * math.cos(a), y, z + size * math.sin(a)),
             (cx + size * math.cos(b), y, z + size * math.sin(b)), .24, 'Gold')
    F.star('Neris Compass Inlay', cx, y - .3, z, size * .84, vertical=True)


# Keep all three original terminal skylight ribbons and all real openings unchanged.
skylight_count = sum(o.name.startswith('Roof Skylight') for o in scene.objects)
opaque = clear = 0
for obj in scene.objects:
    if not obj.get('horizon_asset') or obj.type != 'MESH':
        continue
    if 'Gentle Wave Roof' in obj.name or obj.name.startswith(('Control Room Roof', 'Control Roof Gold Band')):
        finish(obj, 'Teal')
    if obj.get('assembly') in ('Transport Glass Halls', 'Royal Glass Hall'):
        if any(n in obj.name for n in (' Glass', 'Side Wedge', 'Clerestory')):
            finish(obj, 'Hall Glass')
    if obj.name.startswith(('Terminal Glazing', 'Terminal End Glazing')):
        lo, hi = bounds(obj)
        cx = (lo[0] + hi[0]) / 2
        # Clear lower passenger windows; opaque blue upper spandrels and ceremonial bays.
        if lo[2] > 12 or (abs(cx) > 45 and int((cx + 260) / 26) % 3 != 1):
            finish(obj, 'Glass Dark')
            opaque += 1
        else:
            clear += 1

before = set(scene.objects)
# Keep the identical visible roof skin, omitting buried walls between adjacent cells.
for obj in list(scene.objects):
    if obj.get('assembly') == 'Gentle Wave Terminal' and obj.name.startswith('Gentle Wave Roof'):
        bpy.data.objects.remove(obj, do_unlink=True)
cells = []
roof_rows = [-5, 5, 25, 45, 65, 85, 105, 125, 145, 155]
for x in range(-265, 265, 10):
    for y, end in zip(roof_rows, roof_rows[1:]):
        if (abs(x + 5) < 15 or 205 < abs(x + 5) < 225) and 5 <= y < 145:
            continue
        cells.append([(x, y, roof_z(x, y)), (x + 10, y, roof_z(x + 10, y)),
                      (x + 10, end, roof_z(x + 10, end)), (x, end, roof_z(x, end))])
roof_shell('Gentle Wave Roof Royal Teal', cells, 1.3)
for x in range(-260, 261, 26):
    if x == 0:
        continue
    h = roof_z(x)
    for y in (-2.6, 152.6):
        for z, width, height in ((2, 4.2, 3), (12.5, 4, .8), (39, 4, .8), (h - 3, 5, 1.6)):
            box('Facade Carved Pier Collar', (x, y, z), (width, 1.9, height), 'Ivory')
            box('Facade Pier Gold Fillet', (x, y - .98 if y < 0 else y + .98, z + height / 2),
                (width, .22, .19), 'Gold')
        beam('Facade Fluted Pier Gold', (x, y - 1.0 if y < 0 else y + 1.0, 4),
             (x, y - 1.0 if y < 0 else y + 1.0, h - 4), .20, 'Gold')

for y in (-3.0, 153.0):
    for cx in (-221, -169, -117, -65, 65, 117, 169, 221):
        top = roof_z(cx) - 5
        arch('Ivory Lancet Reveal', cx, y, 13.3, top, 10.4, 1.1, 'Ivory')
        arch('Gold Lancet Tracery', cx, y - .68 if y < 0 else y + .68, 14, top - 1.5, 9.15, .30, 'Gold')
        beam('Lancet Center Mullion', (cx, y, 14), (cx, y, top - 8), .24, 'Gold')
        for side in (-1, 1):
            beam('Lancet Branch', (cx, y, top - 11), (cx + side * 6.2, y, top - 17), .25, 'Gold')
    for z in (12.4, 25.6):
        beam('Facade Horizontal Cornice', (-258, y, z), (258, y, z), .7, 'Ivory', 1.1)
        beam('Facade Horizontal Gold Inlay', (-258, y - .65 if y < 0 else y + .65, z + .15),
             (258, y - .65 if y < 0 else y + .65, z + .15), .20, 'Gold')

arch('Grand Arrival Ivory Portal', 0, -4, 13.3, 73.3, 37, 2, 'Ivory')
arch('Grand Arrival Gold Portal', 0, -5.1, 14.2, 70.6, 34.8, .65, 'Gold')
for cx in (-17, 17):
    arch('Arrival Twin Tracery', cx, -4.0, 14, 47, 14.5, .34, 'Gold')
medallion(0, -5.5, 55.5, 7.5)
for x in (-36, 36):
    box('Arrival Support Plinth', (x, -20, 1.2), (3.4, 3.4, 2.4), 'Ivory')
    box('Arrival Support Capital', (x, -20, 10.8), (3.6, 3.6, .7), 'Gold')
for obj in scene.objects:
    if obj.get('assembly') == 'Gentle Wave Terminal' and obj.name.startswith('Entrance Canopy.'):
        finish(obj, 'Teal')
for x in range(-247, 248, 26):
    if abs(x) < 17 or 202 < abs(x) < 228:
        continue
    for y in range(-4, 154, 26):
        end = min(y + 26, 154)
        beam('Wave Roof Gold Standing Seam', (x, y, roof_z(x, y) + .19),
             (x, end, roof_z(x, end) + .19), .24, 'Gold')
tag_new(before, 'Gentle Wave Terminal')

# Replace each hangar roof with a teal shell around a genuine open, glazed ribbon.
hangar_skylights = []
for name, cx, half, front, back, wall in [('Transport Hangar', -365, 77, 48, 168, 46),
                                       ('Royal Hangar', 365, 48, 53, 163, 32)]:
    for obj in list(scene.objects):
        if obj.get('assembly') == name and 'Gentle Wave Roof' in obj.name:
            bpy.data.objects.remove(obj, do_unlink=True)
    before = set(scene.objects)
    def height(x):
        u = abs((x - cx) / half)
        return wall + 1.6 + 11 * math.exp(-(u / .34) ** 2) + 4.5 * math.exp(-((u - .86) / .2) ** 2)
    ys = [front, front + 18, back - 18, back]
    cells = []
    for i in range(64):
        a, b = cx - half + 2 * half * i / 64, cx - half + 2 * half * (i + 1) / 64
        for j in range(3):
            top = [(a, ys[j], height(a)), (b, ys[j], height(b)),
                   (b, ys[j + 1], height(b)), (a, ys[j + 1], height(a))]
            if 24 <= i < 40 and j == 1:
                panel(name + ' Roof Skylight', top, 'Glass Highlight')
            else:
                cells.append(top)
        for y in (front + 18, back - 18):
            if 24 <= i < 40:
                beam(name + ' Skylight End Curb', (a, y, height(a) + .12), (b, y, height(b) + .12), .35, 'Gold')
    roof_shell(name + ' Gentle Wave Roof', cells, 1.1)
    for x in (cx - half / 4, cx + half / 4):
        beam(name + ' Skylight Side Curb', (x, front + 18, height(x) + .13),
             (x, back - 18, height(x) + .13), .40, 'Gold')
    for x in (cx - half * .72, cx + half * .72):
        beam(name + ' Roof Gold Standing Seam', (x, front, height(x) + .2), (x, back, height(x) + .2), .28, 'Gold')
    for x in (cx - half + 4, cx + half - 4):
        box(name + ' Portal Gold Edge', (x, front - .8, wall / 2), (.6, .5, wall), 'Gold')
    tag_new(before, name)
    hangar_skylights.append({'assembly': name, 'width_m': half / 2, 'length_m': back - front - 36})

before = set(scene.objects)
for i in range(8):
    a, b = i * math.tau / 8, (i + 1) * math.tau / 8
    beam('Control Crown Gold Rim', (36.15 * math.cos(a), 230 + 36.15 * math.sin(a), 222.8),
         (36.15 * math.cos(b), 230 + 36.15 * math.sin(b), 222.8), .55, 'Gold')
medallion(0, 207.4, 156, 7)
tag_new(before, 'Centered Rear Tower')

# Planting uses the earlier planter/tree proportions and stays outside aircraft lanes.
before = set(scene.objects)
gardens = [(x, 201) for x in (-318, -290, -262, -234, -206, -178, -150)]
gardens += [(x, 211) for x in (184, 210, 236, 262, 288)]
gardens += [(-334, 221), (-334, 253), (301, 231), (301, 258)]
for x, y in gardens:
    box('Hall Ivory Planter', (x, y, .9), (6, 8, 1.8), 'Ivory')
    box('Hall Planter Soil', (x, y, 1.85), (5, 7, .14), 'Metal')
    F.cone('Hall Evergreen Lower', (x, y, 6), 2.4, 9, 'Garden', top=.6)
    F.cone('Hall Evergreen Upper', (x, y, 11), 1.55, 9, 'Garden')
tag_new(before, 'Rear Hall Gardens')
bpy.context.view_layer.update()
assert skylight_count == sum(o.name.startswith('Roof Skylight') for o in scene.objects)

# The export pipeline flips Blender Y into native Z twice: native aircraft nose is -Z.
# Blender's parked runway preview already points correctly toward increasing X.
scene['revision'] = 'r007'
scene['native_aircraft_nose'] = '-Z'
scene['hangar_skylights'] = 2
asset = [o for o in scene.objects if o.type == 'MESH' and o.get('horizon_asset')]
sys.path.insert(0, str(ROOT.parent / 'NerisTownV1/Source'))
import static_glb
groups = {'Horizon': {}, 'Transport': {}, 'Royal': {}}
deps = bpy.context.evaluated_depsgraph_get()
for obj in asset:
    evaluated = obj.evaluated_get(deps)
    data = evaluated.to_mesh()
    data.calc_loop_triangles()
    transform = obj.matrix_world
    normal = transform.to_3x3().inverted().transposed()
    family = obj.get('fleet', 'Horizon')
    origin = Vector(obj.get('fleet_origin', (0, 0, 0)))
    for tri in data.loop_triangles:
        mat = data.materials[tri.material_index]
        bucket = groups[family].setdefault(mat.name, [mat, []])
        bucket[1].append(tuple((tuple(transform @ data.vertices[v].co - origin),
                                tuple((normal @ tri.normal).normalized())) for v in tri.vertices))
    evaluated.to_mesh_clear()
for family, mats in groups.items():
    print('EXPORT BUDGET', family, sum(len({corner for tri in value[1] for corner in tri}) for value in mats.values()), flush=True)
    static_glb.write(OUT / f'Native/{family}.glb', [(name, mat, tris, None) for name, (mat, tris) in sorted(mats.items())])
report = json.loads((ROOT / 'Revisions/r006/asset-manifest.json').read_text())
report.update(revision='r007', design='Gentle Wave with Neris royal facade',
              parts={k: len(v) for k, v in groups.items()},
              triangles={k: sum(len(v[1]) for v in mats.values()) for k, mats in groups.items()},
              castle_palette=['NC.MAT.TealRoof', 'NC.MAT.RoyalGold', 'NC.MAT.RoyalGlass'],
              retained_terminal_skylight_panels=skylight_count, hangar_skylights=hangar_skylights,
              restored_rear_halls='Opaque blue glazing, ivory sawtooth roofs, gold frames and evergreen planters',
              rear_hall_planters=len(gardens),
              facade_glazing={'opaque_panels': opaque, 'clear_panels': clear},
              glass='Opaque royal-blue upper facade; clear passenger windows and retained skylights',
              native_aircraft_nose=[0, 0, -1])
(OUT / 'asset-manifest.json').write_text(json.dumps(report, indent=2))
(OUT / 'checksums.json').write_text(json.dumps({f'Native/{f}.glb': hashlib.sha256((OUT / f'Native/{f}.glb').read_bytes()).hexdigest() for f in groups}, indent=2))
scene.camera = scene.objects['Review Hero']
scene.cycles.samples = 16
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Source/Neris-Horizon-Gentle-Wave-r007.blend'), compress=True)
print('PASS R007 EXPORT ' + json.dumps(report), flush=True)
for name in ('Hero', 'Front', 'Rear', 'Top'):
    scene.camera = scene.objects['Review ' + name]
    scene.render.filepath = str(PREVIEWS / (name + '.png'))
    bpy.ops.render.render(write_still=True)
