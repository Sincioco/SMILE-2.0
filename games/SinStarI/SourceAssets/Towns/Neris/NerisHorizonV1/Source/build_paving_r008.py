"""Spaceport 01 paving and Neris tower relief on the retained r007 airport."""
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Revisions/r008'
PREVIEWS = ROOT / 'Previews/r008'
(OUT / 'Native').mkdir(parents=True, exist_ok=True)
PREVIEWS.mkdir(parents=True, exist_ok=True)
sys.path.insert(0, str(Path(__file__).parent))
import forms as F

bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'Source/Neris-Horizon-Gentle-Wave-r007.blend'))
scene = bpy.context.scene
scene.name = 'Horizon Gentle Wave r008'
for mat in bpy.data.materials:
    if mat.name.startswith('GW '):
        F.MATERIALS[mat.name[3:]] = mat

# Copy the actual reference deck, not the screenshot's lighting or a new palette.
reference = ROOT.parent / 'NerisSpaceport01V1/Source/NSP01-final-r08.blend'
with bpy.data.libraries.load(str(reference), link=False) as (_, loaded):
    loaded.materials = ['NSP01.MAT.Deck', 'NSP01.MAT.Dark']
for target, source in zip(('Paving', 'Joint'), loaded.materials):
    a = F.MATERIALS[target].node_tree.nodes['Principled BSDF']
    b = source.node_tree.nodes['Principled BSDF']
    for key in ('Base Color', 'Metallic', 'Roughness'):
        a.inputs[key].default_value = b.inputs[key].default_value
for obj in list(scene.objects):
    if obj.name.startswith(('Paving Expansion Joint', 'Side Apron Boundary',
                            'Tower Vertical Glazing', 'Tower Gold Mullion')):
        bpy.data.objects.remove(obj, do_unlink=True)
    elif 'Hangar Floor' in obj.name or obj.name == 'Terminal Ground Floor':
        obj.data.materials.clear()
        obj.data.materials.append(F.MATERIALS['Paving'])


def tagged(obj, assembly):
    obj['horizon_asset'] = True
    obj['assembly'] = assembly
    return obj


def flat(label, points, material, assembly='Neris Paving'):
    return tagged(F.mesh(label, points, [tuple(range(len(points)))], material), assembly)


def strip(label, a, b, width, z, material='Gold'):
    dx, dy = b[0] - a[0], b[1] - a[1]
    length = math.hypot(dx, dy)
    nx, ny = -dy * width / (2 * length), dx * width / (2 * length)
    return flat(label, [(a[0]-nx, a[1]-ny, z), (b[0]-nx, b[1]-ny, z),
                        (b[0]+nx, b[1]+ny, z), (a[0]+nx, a[1]+ny, z)], material)


def ring(label, x, y, radius, width, z):
    verts, faces = [], []
    for i in range(48):
        a = math.tau * i / 48
        for r in (radius - width / 2, radius + width / 2):
            verts.append((x + r * math.cos(a), y + r * math.sin(a), z))
        j = i * 2
        faces.append((j, j + 1, (j + 3) % 96, (j + 2) % 96))
    return tagged(F.mesh(label, verts, faces, 'Gold'), 'Neris Paving')


# Flat seams use the reference's 12 m tiles and avoid runway pavement height.
# Crossing directions differ in height; gold and lights sit above both.
for x in range(-468, 469, 12):
    strip('Paving Tile Joint X', (x, -198), (x, 398), .10, .045, 'Joint')
for y in range(-192, 397, 12):
    strip('Paving Tile Joint Y', (-478, y), (478, y), .10, .06, 'Joint')
for x in (-20, -12, 12, 20):
    strip('Arrival Gold Inlay', (x, -198), (x, -28), .25, .084)
for x, y, radius in ((0, -108, 9), (-365, -90, 13), (365, -90, 13)):
    ring('Apron Compass Ring', x, y, radius, .28, .085)
    tagged(F.star('Apron Neris Compass', x, y, .087, radius * .86), 'Neris Paving')
for cx in (-365, 365):
    points = [(cx-84,-174),(cx+84,-174),(cx+84,34),(cx-84,34)]
    for a, b in zip(points, points[1:] + points[:1]):
        strip('Apron Gold Border', a, b, .30, .086)
for x in (-474, 474):
    strip('Quayside Gold Border', (x, -192), (x, 290), .30, .086)
strip('Front Promenade Gold Border', (-474, -192), (474, -192), .30, .086)

# Flush lights remain outside the clear doorway and taxi/runway lanes. The native
# halo owner uses these same simple rows; no pole or new light-resource pool.
lights = [(side*26, -186+24*i) for side in (-1, 1) for i in range(7)]
lights += [(cx+side*84, -162+32*i) for cx in (-365,365) for side in (-1,1) for i in range(7)]
lights += [(x, -192) for x in (-450,-390,-330,-270,-210,-150,-90,90,150,210,270,330,390,450)]
for i, (x, y) in enumerate(lights):
    flat('Flush Floor Light Gold Socket', [(x-1,y-1,.10),(x+1,y-1,.10),
                                          (x+1,y+1,.10),(x-1,y+1,.10)], 'Gold')
    obj = flat('Flush Floor Light Lens', [(x-.48,y-.48,.14),(x+.48,y-.48,.14),
                                         (x+.48,y+.48,.14),(x-.48,y+.48,.14)], 'Light')
    obj['floor_light_index'] = i
    obj['floor_light_anchor'] = (x, y, .19)

# Sculpted relief on all four faces, with repeated heraldic lancets and crystals.
# The control-room windows and working antenna keep their original silhouettes.
def face_point(u, z, depth, side):
    angle = side * math.pi / 2
    return (u * math.cos(angle) + depth * math.sin(angle),
            230 + u * math.sin(angle) - depth * math.cos(angle), z)


def arch_path(cx, bottom, top, half):
    spring = top - half * 1.65
    left = [(cx-half, bottom), (cx-half, spring)]
    for i in range(1, 13):
        t = i / 12
        left.append((cx-half+half*t*t, spring+(top-spring)*(1.35*t-.35*t*t)))
    return left + [(2*cx-x,z) for x,z in reversed(left[:-1])]


def ribbon(label, points, width, depth, side, material='Gold'):
    verts, faces = [], []
    for a, b in zip(points, points[1:]):
        dx, dz = b[0]-a[0], b[1]-a[1]
        length = math.hypot(dx, dz)
        nx, nz = -dz*width/(2*length), dx*width/(2*length)
        k = len(verts)
        verts += [face_point(a[0]-nx,a[1]-nz,depth,side),face_point(b[0]-nx,b[1]-nz,depth,side),
                  face_point(b[0]+nx,b[1]+nz,depth,side),face_point(a[0]+nx,a[1]+nz,depth,side)]
        faces.append((k,k+1,k+2,k+3))
    return tagged(F.mesh(label, verts, faces, material), 'Tower Neris Relief')


for side in range(4):
    path = arch_path(0, 22, 178, 15)
    flat('Tower Royal Blue Lancet', [face_point(u,z,22.3,side) for u,z in path],
         'Glass Dark', 'Tower Neris Relief')
    ribbon('Tower Ivory Lancet Moulding', arch_path(0,21,181,17), 1.3, 22.65, side, 'Ivory')
    ribbon('Tower Gold Outer Tracery', path, .48, 23.0, side)
    ribbon('Tower Gold Inner Tracery', arch_path(0,25,174,13.4), .23, 23.08, side)
    for sign in (-1,1):
        ribbon('Tower Twin Lancet', arch_path(sign*6.4,30,151,6), .22, 23.18, side)
        ribbon('Tower Fluted Corner Gold', [(sign*20.2,7),(sign*20.2,184)], .32, 22.18, side)
    circle = [(8.2*math.sin(math.tau*i/48),119+8.2*math.cos(math.tau*i/48)) for i in range(49)]
    ribbon('Tower Compass Halo', circle, .30, 23.3, side)
    star = [(0,119)]
    for i in range(8):
        a = i * math.pi / 4
        r = 10.6 if i % 2 == 0 else 2.1
        star.append((r*math.sin(a),119+r*math.cos(a)))
    tagged(F.mesh('Tower Neris Star', [face_point(u,z,23.4,side) for u,z in star],
                  [(0,i+1,(i+1)%8+1) for i in range(8)], 'Gold'), 'Tower Neris Relief')
    verts = [face_point(0,119,26.3,side),face_point(0,129,23.65,side),
             face_point(3.3,119,23.65,side),face_point(0,109,23.65,side),
             face_point(-3.3,119,23.65,side)]
    tagged(F.mesh('Tower Cyan Crystal', verts, [(0,1,2),(0,2,3),(0,3,4),(0,4,1)], 'Light'), 'Tower Neris Relief')
    for z in (49,78,160):
        points = [(0,z+2.5),(1,z+1),(2.5,z),(1,z-1),(0,z-2.5),(-1,z-1),(-2.5,z),(-1,z+1),(0,z+2.5)]
        ribbon('Tower Gold Quatrefoil', points, .22, 23.3, side)
    for z in (7,18,183):
        ribbon('Tower Ivory Collar', [(-22,z),(22,z)], 1.5, 22.5, side, 'Ivory')
        ribbon('Tower Gold Collar', [(-22,z+.9),(22,z+.9)], .30, 22.65, side)

import airport_layout_r008
import fleet_r008
import runway_surface_r008
import hangar_glass_r008
clear_panels = airport_layout_r008.apply(scene)
hangar_glass_r008.apply(scene)
runway_surface_r008.apply(scene, ROOT / 'Textures/r008/Runway-Paint.png')
fleet_r008.apply(scene)
bpy.context.view_layer.update()
scene['revision'] = 'r008'
scene['floor_light_count'] = len(lights)
scene['tower_ornament_faces'] = 4
asset = [o for o in scene.objects if o.type == 'MESH' and o.get('horizon_asset')]
sys.path.insert(0, str(ROOT.parent / 'NerisTownV1/Source'))
import static_glb
groups = {'Horizon': {}, 'Transport': {}, 'Royal': {}, 'Cargo': {}}
deps = bpy.context.evaluated_depsgraph_get()
for obj in asset:
    evaluated = obj.evaluated_get(deps)
    data = evaluated.to_mesh()
    data.calc_loop_triangles()
    transform = obj.matrix_world
    family = obj.get('fleet', 'Horizon')
    origin = Vector(obj.get('fleet_origin', (0, 0, 0)))
    normal = transform.to_3x3().inverted().transposed()
    for tri in data.loop_triangles:
        mat = data.materials[tri.material_index]
        bucket = groups[family].setdefault(mat.name, [mat, []])
        bucket[1].append(tuple((tuple(transform @ data.vertices[v].co - origin),
                                tuple((normal @ (data.corner_normals[loop].vector if obj.get('fleet') else tri.normal)).normalized()))
                               for v, loop in zip(tri.vertices, tri.loops)))
    evaluated.to_mesh_clear()
for family, mats in groups.items():
    count = sum(len({c for tri in value[1] for c in tri}) for value in mats.values())
    print('EXPORT BUDGET', family, count, flush=True)
    static_glb.write(OUT / f'Native/{family}.glb', [(n,m,t,None) for n,(m,t) in sorted(mats.items())])
report = json.loads((ROOT / 'Revisions/r007/asset-manifest.json').read_text())
report.update(revision='r008', design='Gentle Wave with Neris paving and tower relief',
              parts={k: len(v) for k,v in groups.items()},
              triangles={k: sum(len(v[1]) for v in mats.values()) for k,mats in groups.items()},
              paving_reference='NSP01.MAT.Deck', tile_spacing_m=12, floor_lights=len(lights),
              floor_light_anchors=lights, compass_medallions=3, tower_relief_faces=4,
              tower_m={'height':285.3,'center':[0,190]}, models=4, aircraft_instances=12,
              runway_centers=[355,270], runway_count=2, runway_cycle_seconds=150,
              runway_arrival_interval_seconds=15, cargo_interval_seconds=150,
              hangar_rear_y_m=150, translucent_terminal_panels=clear_panels,
              rear_hall_planters=0, restored_rear_halls='Removed by request',
              glass_hall_bounds_m={}, hangar_aircraft_heading='Noses outward toward front apron',
              runway_paint_texture='Textures/r008/Runway-Paint.png',
              runway_paint_filter='Anisotropic with mipmaps',
              airport_sign='Wave-shaped roof sign above main entrance',
              hangar_glass_opacity=.8, antenna_beacons=3, white_platform_border=False)
(OUT / 'asset-manifest.json').write_text(json.dumps(report, indent=2))
(OUT / 'checksums.json').write_text(json.dumps({f'Native/{f}.glb': hashlib.sha256((OUT / f'Native/{f}.glb').read_bytes()).hexdigest() for f in groups}, indent=2))
scene.camera = scene.objects['Review Hero']
scene.cycles.samples = 16
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Source/Neris-Horizon-Gentle-Wave-r008.blend'), compress=True)
print('PASS R008 EXPORT', flush=True)
for name in ('Hero', 'Front', 'Rear', 'Top'):
    scene.camera = scene.objects['Review ' + name]
    scene.render.filepath = str(PREVIEWS / (name + '.png'))
    bpy.ops.render.render(write_still=True)
