"""District placement, ground surfaces, connected streets, canals and landscape."""
import hashlib
import math

import bpy

from geometry import Geometry
from props import instance, templates, fountain, gate
from market import dress_town


def import_building(package, name, loc, angle):
    path = package.parents[3] / 'Assets' / 'Towns' / 'Neris' / 'ModelsV1' / (name + '.glb')
    # SourceAssets/Towns/Neris/TownV1 -> SinStarI via parents[3].
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    objects = set(bpy.data.objects) - before
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    root = bpy.data.objects.new(name + ' Placement', None)
    collection.objects.link(root)
    for obj in objects:
        for owner in list(obj.users_collection):
            owner.objects.unlink(obj)
        collection.objects.link(obj)
        if not obj.parent:
            obj.parent = root
    root.location = loc
    root.rotation_euler.z = angle
    root['Source Model'] = str(path.relative_to(package.parents[3])).replace('\\', '/')
    root['Source SHA256'] = hashlib.sha256(path.read_bytes()).hexdigest()
    return {'name': name, 'location': loc, 'angle': angle, 'objects': len(objects),
            'source': root['Source Model'], 'sha256': root['Source SHA256']}


def paving(g, name, x, y, width, depth, z=.08, tiled=True):
    g.box(name, (x, y, z - .14), (width, depth, .28), 'paving', .08)
    if not tiled:
        return
    # One mesh per paved area, with mortar gaps and deterministic tone variation.
    verts, faces = [], []
    nx, ny = math.ceil(width / 1.8), math.ceil(depth / 1.8)
    for i in range(nx):
        for j in range(ny):
            u, v = x - width / 2 + i * width / nx, y - depth / 2 + j * depth / ny
            n = len(verts)
            verts.extend([(u + .025, v + .025, z + .012),
                          (u + width / nx - .025, v + .025, z + .012),
                          (u + width / nx - .025, v + depth / ny - .025, z + .012),
                          (u + .025, v + depth / ny - .025, z + .012)])
            faces.append((n, n + 1, n + 2, n + 3))
    obj = g.mesh(name + ' Paving Stones', verts, faces, 'pavinglight')
    obj.data.materials.append(g.materials['paving'])
    for face in obj.data.polygons:
        face.material_index = int(g.rng.random() < .15)


def streets(g):
    g.box('Town Bedrock Plinth', (0, 0, -1.1), (102, 90, 2), 'foundation', .8)
    g.box('Town Garden Ground', (0, 0, -.16), (100, 88, .3), 'grass', .25)
    for x in [-49.5, 49.5]:
        g.box('Low Boundary Wall', (x, 0, .6), (.7, 88, 1.2), 'stone')
        g.box('Boundary Coping', (x, 0, 1.24), (1, 88, .18), 'trim')
    for x, width in [(-29, 41), (29, 41)]:
        g.box('South Boundary Wall', (x, -43.5, .6), (width, .7, 1.2), 'stone')
    g.box('North Boundary Wall', (0, 43.5, .6), (99, .7, 1.2), 'stone')
    paving(g, 'Central Promenade', 0, -3, 17, 79)
    paving(g, 'Civic Forecourt', 0, 25, 27, 29, z=.10)
    for x in [-21, 21]:
        paving(g, 'District Walk', x, 0, 5, 81)
    for y in [-32, -10, 14, 39]:
        paving(g, 'Cross Street', 0, y, 90, 4.8, z=.13)
    for x, ys in [(32, [-22, 0, 22]), (-34, [-33, -14, 6, 25])]:
        for y in ys:
            paving(g, 'Building Courtyard', x, y, 18, 16, z=.15)
    for x in [-36, -20, 32]:
        paving(g, 'North Home Court', x, 36, 12, 12, z=.16)
    paving(g, 'South Home Court', 31, -36, 13, 11, z=.16)
    paving(g, 'Neighborhood Market Court', -34, -3.5, 20, 5, z=.16)
    # Two shallow garden canals, with level stone bridges at the street crossings.
    for x in [-12.5, 12.5]:
        g.box('Canal Lining', (x, -12, .02), (3.4, 53, .12), 'dark')
        g.box('Canal Water', (x, -12, .10), (2.8, 53, .045), 'water', 0)
        for dx in [-1.6, 1.6]:
            g.box('Canal Stone Bank', (x + dx, -12, .22), (.28, 53, .44), 'trim')
        for y in [-32, -10, 14]:
            paving(g, 'Canal Footbridge', x, y, 4.2, 4.8, z=.30)
            for dy in [-2.25, 2.25]:
                g.beam('Bridge Handrail', (x - 2.15, y + dy, 1.2),
                       (x + 2.15, y + dy, 1.2), .07, 'gold')
                for dx in [-2, -1, 0, 1, 2]:
                    g.beam('Bridge Baluster', (x + dx, y + dy, .3),
                           (x + dx, y + dy, 1.2), .035, 'iron')


def assemble_town(package, mats, homes):
    g = Geometry('01 Ground and Streets', mats)
    streets(g)
    placements = []
    for name, loc, angle in [
        ('Neris-City-Hall', (0, 26, .16), 0),
        ('Neris-Communication-Tower', (0, 2, .16), 0),
        ('Neris-Weapon-Store', (32, 22, .20), -math.pi / 2),
        ('Neris-Item-Store', (32, 0, .20), -math.pi / 2),
        ('Neris-Armor-Store', (32, -22, .20), -math.pi / 2),
    ]:
        placements.append(import_building(package, name, loc, angle))
    neighborhood = Geometry('02 Residential District', mats)
    housing = [('Family House', -34, 25, math.pi / 2),
               ('Courtyard Cottage', -34, 6, math.pi / 2),
               ('Garden Pavilion', -34, -14, math.pi / 2),
               ('Family House', -34, -33, math.pi / 2),
               ('Courtyard Cottage', -36, 39, 0),
               ('Garden Pavilion', -20, 35, 0),
               ('Family House', 32, 37, 0),
               ('Courtyard Cottage', 31, -36, math.pi / 2)]
    for i, (name, x, y, angle) in enumerate(housing, 1):
        obj = instance(homes[name], neighborhood.collection, f'Home {i:02} — {name}',
                       (x, y, .20), angle)
        obj['Purpose'] = 'Residential exterior concept; no interior.'
        placements.append({'name': obj.name, 'location': list(obj.location), 'angle': angle})
    props = Geometry('03 Plaza and Street Furniture', mats)
    fountain(props, 0, -20)
    gate(props)
    # Circular arrival/safe-point plaza off the main street, left of the fountain.
    props.cylinder('Wayfarer Plaza', (-22, -22, .22), 4.7, .3, 'trim', 48)
    props.cylinder('Wayfarer Teal Inlay', (-22, -22, .40), 3.9, .055, 'roof', 48)
    for i in range(8):
        a = i * math.pi / 4
        props.path('Arrival Gold Rays', [(-22 + math.cos(a) * r, -22 + math.sin(a) * r, .44)
                                       for r in [1, 3.6]], .055, 'gold')
    stock = templates(mats)
    landscape = Geometry('04 Trees and Garden Beds', mats)
    tree_positions = [(x, y) for x in [-46, 46] for y in [-37, -25, -12, 1, 14, 27, 39]]
    tree_positions += [(x, 41) for x in [-7, 7, 19]]
    tree_positions += [(-17, y) for y in [-25, -3, 21]] + [(17, y) for y in [-25, -3, 21]]
    tree_positions += [(-6, -35), (6, -35), (-7, -10), (7, -10)]
    for i, (x, y) in enumerate(tree_positions):
        instance(stock['Tree'], landscape.collection, f'Garden Tree {i + 1:02}',
                 (x, y, .03), i * 1.7, .85 + (i % 4) * .085)
    for x in [-7, 7]:
        for y in [-28, -13, 13]:
            instance(stock['Flower Bed'], landscape.collection, 'Round Flower Garden', (x, y, .13))
        for y in [-34, -25, -12, 11, 17]:
            instance(stock['Crystal Lamp'], props.collection, 'Promenade Crystal Lamp', (x, y, .18))
    for x in [-20, 20]:
        for y in [-35, -17, 3, 25, 37]:
            instance(stock['Crystal Lamp'], props.collection, 'Neighborhood Crystal Lamp', (x, y, .18))
    for x in [-6, 6]:
        for y in [-23, -17]:
            instance(stock['Bench'], props.collection, 'Fountain Seat', (x, y, .15),
                     math.pi / 2 if x < 0 else -math.pi / 2)
    for x in [-25, 25]:
        for y in [12, -10]:
            instance(stock['Bench'], props.collection, 'District Seat', (x, y, .2))
    dress_town(mats, stock, landscape, props)
    return placements
