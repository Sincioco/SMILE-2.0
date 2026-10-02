"""Editable wilderness maps; no existing town or live save is overwritten.

Coordinates are metres. Heights are authored corner samples, not another runtime
sampler. Every prop is on a grid corner; native acceptance checks its final height.
Encounter clearings reserve space for later quests/combat, without adding enemies.
"""
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import random
from town_design import Town, CATALOG, GROUND, WATER, ROAD, atomic_write, encode, decode, unwrap, terrain_offset
from town_access import outline, surface, prepare
from journey_layouts import distance, forest, mountains, desert


def smooth(value):
    value = max(0, min(1, value))
    return value * value * (3 - 2 * value)


def highlands_height(x, z):
    main = 28 * smooth((120 - z) / 270)
    # Rounded flanking hills leave the switchback and stream corridor gentle.
    ridge = 18 * smooth(1 - abs(abs(x) - 132) / 45) * smooth(1 - abs(z + 45) / 120)
    return main + ridge


def basin_height(x, z):
    # Two steep falls descend 42 m with a broad crossing shelf between them. Away from
    # the river, wide shoulders blend into a gradual side-trail climb.
    falls = 26*smooth((x+20)/14) + 16*smooth((x-55)/14)
    trail = 42*smooth((x+150)/300)
    river = 1-smooth((abs(z-24)-20)/28)
    height = trail + (falls-trail)*river
    return height*smooth((math.hypot(x+128,z-30)-60)/25)


def start(name, style):
    town = Town(name, 384, 3, True)
    town.symmetric = False
    town.night = False
    town.terrain_style = style
    town.wilderness = True
    town.rect(-192, -192, 192, 192)
    return town


def stream(town, first, last, width=7):
    town.brush(4, WATER, *first, *last, width)
    return len(town.curves) - 1


def decorate(town, height, paths, clearings, seed, wooded=True):
    rng = random.Random(seed)
    document = town.document()
    for z in range(-171, 172, 18):
        for x in range(-171, 172, 18):
            if rng.random() > (.66 if wooded else .25):
                continue
            if min(distance(x, z, path) for path in paths) < 13:
                continue
            if any(math.hypot(x-cx, z-cz) < 22 for cx, cz in clearings):
                continue
            template = 18 + rng.randrange(2)
            town.place(template, x, z, rng.uniform(2.2, 3.4), rng.randrange(360))
            item = town.items[-1]
            if any(surface(document, px*10, pz*10) != GROUND for px, pz in outline(item)):
                town.items.pop()
    # Mark the clear routes without covering the playable clearings.
    for path in paths:
        previous = path[0]
        for x, z in path[1:-1]:
            if math.dist(previous, (x,z)) < 45:
                continue
            previous = (x,z)
            for dx in (-9, 9):
                town.place(15, x+dx, z+12, 1.6)
                if any(surface(document, px*10, pz*10) != GROUND
                       for px, pz in outline(town.items[-1])):
                    town.items.pop()
    for identity, item in enumerate(town.items, 1):
        x, _, z = item['position']
        x, z = round(x/30)*30, round(z/30)*30
        item['identity'] = identity
        item['position'] = [x, 23 + round(height(x/10, z/10)*1000)/100, z]


def highlands():
    town = start('Willowstep Highlands', 1)
    route = [(-174, 144), (-54, 144), (-54, 84), (-78, 24),
             (-54, -36), (-78, -90), (-54, -144), (-18, -144)]
    exit_route = [(-54, 144), (174, 144)]
    summit_route = [(-78, -90), (-132, -144), (-132, -45)]
    spawn_route = [(0, 0), (-69, 0)]
    clearings = [(-54, 84), (-54, -36), (-54, -144), (-132, -45)]
    flow = stream(town, (36, -150), (36, 138))
    town.disk(36, 150, 24, WATER)
    route = town.path(route, 7)
    exit_route = town.path(exit_route, 8)
    summit_route = town.path(summit_route, 7)
    spawn_route = town.path(spawn_route, 7)
    town.gates(144, ('Neris Relief Quarter', 'Silverfall Basin'), join_x=156, width=8)
    paths = [route, exit_route, summit_route, spawn_route]
    decorate(town, highlands_height, paths, clearings + [(0, 0)], 5127)
    town.notes = ['A wooded switchback rises 28 m to three open encounter clearings.',
                  'Flanking hills rise above the trail; a north-to-south stream drains into a low pool.',
                  'Working geographic label; quests and monster spawning are separate gameplay.']
    return town, highlands_height, flow, paths


def basin():
    town = start('Silverfall Basin', 0)
    route = [(-192, -54), (-144, -54), (-90, -84), (-36, -70),
             (36, -70), (90, -84), (144, -54), (192, -54)]
    side_route = [(-192, 105), (-126, 120), (-60, 102), (-30, 105),
                  (30, 105), (90, 123), (144, 105), (192, 105)]
    crossing = [(0, -70), (0, 105)]
    clearings = [(-90, -84), (0, -70), (90, -84)]
    flow = stream(town, (150, 24), (-102, 24), 24)
    town.curve([(-141, 4), (-139, 64), (-106, 30)], 48, WATER)
    town.gates(-54, ('Willowstep Highlands', 'Greyglass Pass'), join_x=156, width=8)
    route = town.path(route, 8)
    side_route = town.path(side_route, 7)
    crossing = town.path(crossing, 7)
    paths = [route, side_route, crossing]
    # The paving reaches the boundary; acceptance keeps the party's footprint inside it.
    for path in paths[:2]:
        path[0] = (-188, path[0][1])
        path[-1] = (188, path[-1][1])
    decorate(town, basin_height, paths, clearings + [(0, 0)], 6103, True)
    town.notes = ['Two steep cascades descend 42 m, separated by a broad river-crossing shelf.',
                  'Winding side trails reach both map edges with rounded junctions and no enclosing loop.',
                  'A 24 m stream feeds a broad irregular lake, surrounded by grassy woodland.',
                  'Draped cascades, not vertical free-fall water simulation; no enemies are added.']
    return town, basin_height, flow, paths


def save(design, folder):
    town, height, flow, paths = design
    doc = town.document()
    existing = Path(__file__).resolve().parent.parent / 'Towns' / (town.name + '.town')
    if existing.exists():
        previous = decode(unwrap(existing.read_bytes()), CATALOG)
        assert previous['xs'] == doc['xs'] and previous['zs'] == doc['zs']
        # The atlas owns destinations; a landscape refresh must retain its links.
        doc['map_tiles'] = previous['map_tiles']
    doc['heights'] = [round(height(x/10, z/10)*1000)/100
                      for z in doc['zs'] for x in doc['xs']]
    # Keep natural mountain slopes in their continuous Highland palette. A hard
    # per-cell height/slope color threshold creates visible stair-step boundaries.
    if town.name == 'Silverfall Basin':
        from road_junctions import round_junctions
        round_junctions(doc)
    doc['flows'] = [[0, 0, 0] for _ in doc['curves']]
    if flow is not None:
        doc['flows'][flow] = [1, 1, 100]
    for item in doc['items']:
        item['position'][1] = 23 + terrain_offset(doc, item['position'][0], item['position'][2])
    doc['items'] = [item for item in doc['items'] if item['template'] not in (18, 19) or
                    all(surface(doc, x*10, z*10) == GROUND for x, z in outline(item))]
    town.items = doc['items']
    payload = encode(doc, CATALOG)
    path = folder / (town.name + '.town')
    if path.exists():
        raise ValueError('Output already exists; choose a new folder: ' + str(path))
    atomic_write(path, payload)
    restored = decode(unwrap(path.read_bytes()), CATALOG)
    assert restored.get('heights', [0]*len(doc['heights'])) == doc['heights']
    assert restored.get('flows', [[0,0,0] for _ in doc['curves']]) == doc['flows']
    for item in doc['items']:
        assert all(surface(doc, x*10, z*10) not in (0, WATER) for x, z in outline(item)), (town.name, item['template'], item['position'])
    count = Counter(item['template'] for item in doc['items'])
    cost = sum(len(CATALOG['templates'][item['template']]['parts']) for item in doc['items'])
    assert len(town.items) < 1024 and cost < 3500 and count[15] + 4 <= 128
    record = dict(name=town.name, file=path.name, size_m=(doc['xs'][-1]-doc['xs'][0])/10, items=len(town.items),
        trees=count[18]+count[19], lamps=count[15], houses=sum(count[i] for i in range(6)), castles=0, city_halls=0,
        draw_cost=cost, local_lights=count[15]+4,
        water_percent=round(100*sum(c in (2, 4) for c in doc['cells'])/len(doc['cells']), 1),
        story_spaces=town.notes, terrain_height_m=max(doc['heights'])/10,
        acceptance_paths=paths, acceptance_goal=paths[0][-1],
        acceptance_gates=[[(doc['xs'][t['x']]+doc['xs'][t['x']+1])/20,
                           (doc['zs'][t['z']]+doc['zs'][t['z']+1])/20]
                          for t in doc['map_tiles']],
        acceptance_spawn=(-40,-95) if town.name == 'Sunglass Expanse' else (0,0))
    print(town.name, len(payload), 'authored bytes;', record['trees'], 'trees;',
          record['lamps'], 'lamps;', record['terrain_height_m'], 'm high', flush=True)
    return record


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    designs = [highlands(), basin()]
    for factory in (forest, mountains, desert):
        town = factory()
        prepare(town)
        designs.append((town, town.height or (lambda x,z: 0), None, town.acceptance_paths))
    records = [save(design, args.output) for design in designs]
    (args.output/'terrain-manifest.json').write_text(json.dumps(records, indent=2)+'\n')
