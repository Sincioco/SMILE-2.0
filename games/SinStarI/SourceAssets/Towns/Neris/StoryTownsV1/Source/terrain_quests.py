"""Two editable terrain journey maps; no existing town or live save is overwritten.

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
from town_design import Town, CATALOG, GROUND, WATER, ROAD, atomic_write, encode, decode, unwrap
from town_access import outline, surface
from journey_layouts import distance


def smooth(value):
    value = max(0, min(1, value))
    return value * value * (3 - 2 * value)


def highlands_height(x, z):
    main = 28 * smooth((120 - z) / 270)
    # Rounded flanking hills leave the switchback and stream corridor gentle.
    ridge = 18 * smooth(1 - abs(abs(x) - 132) / 45) * smooth(1 - abs(z + 45) / 120)
    return main + ridge


def basin_height(x, z):
    # Three broad 14 m terraces; each transition remains below 30 degrees.
    return sum(14 * smooth((x - start) / 54) for start in (-132, -42, 48))


def start(name, style):
    town = Town(name, 384, 3, True)
    town.symmetric = False
    town.night = False
    town.terrain_style = style
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
        for x, z in path[1:-1]:
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
    town.path(route, 7)
    town.path(exit_route, 8)
    town.path(summit_route, 7)
    town.path(spawn_route, 7)
    town.disk(0, 0, 12, ROAD)
    for x, z in clearings:
        town.disk(x, z, 15, ROAD)
    town.gates(144, ('Neris Relief Quarter', 'Silverfall Basin'), join_x=156, width=8)
    paths = [route, exit_route, summit_route, spawn_route]
    decorate(town, highlands_height, paths, clearings + [(0, 0)], 5127)
    town.notes = ['A wooded switchback rises 28 m to three open encounter clearings.',
                  'Flanking hills rise above the trail; a north-to-south stream drains into a low pool.',
                  'Working geographic label; quests and monster spawning are separate gameplay.']
    return town, highlands_height, flow, paths


def basin():
    town = start('Silverfall Basin', 2)
    route = [(-174, -54), (-126, -54), (-90, -84), (-36, -54),
             (0, -84), (54, -54), (90, -84), (144, -54), (174, -54)]
    side_route = [(-144, -54), (-144, 105), (0, 105), (144, 105)]
    spawn_route = [(0, 0), (0, -84)]
    clearings = [(-90, -84), (0, -84), (90, -84)]
    flow = stream(town, (150, 24), (-150, 24), 9)
    town.disk(-159, 24, 18, WATER)
    town.path(route, 8)
    town.path(side_route, 7)
    town.path(spawn_route, 7)
    town.disk(0, 0, 12, ROAD)
    for x, z in clearings:
        town.disk(x, z, 17, ROAD)
    town.gates(-54, ('Willowstep Highlands', 'Greyglass Pass'), join_x=156, width=8)
    paths = [route, side_route, spawn_route]
    decorate(town, basin_height, paths, clearings + [(0, 0)], 6103, False)
    town.notes = ['Three 14 m terraces form a 42 m climb, with open encounter shelves.',
                  'A broad east-to-west stream descends the terrace slopes into the lower basin.',
                  'Draped cascades, not vertical free-fall water simulation; no enemies are added.']
    return town, basin_height, flow, paths


def save(design, folder):
    town, height, flow, paths = design
    doc = town.document()
    doc['heights'] = [round(height(x/10, z/10)*1000)/100
                      for z in doc['zs'] for x in doc['xs']]
    doc['flows'] = [[0, 0, 0] for _ in doc['curves']]
    doc['flows'][flow] = [1, 1, 100]
    payload = encode(doc, CATALOG)
    path = folder / (town.name + '.town')
    if path.exists():
        raise ValueError('Output already exists; choose a new folder: ' + str(path))
    atomic_write(path, payload)
    restored = decode(unwrap(path.read_bytes()), CATALOG)
    assert restored['heights'] == doc['heights'] and restored['flows'] == doc['flows']
    for item in doc['items']:
        assert all(surface(doc, x*10, z*10) == GROUND for x, z in outline(item))
    count = Counter(item['template'] for item in doc['items'])
    cost = sum(len(CATALOG['templates'][item['template']]['parts']) for item in doc['items'])
    assert len(town.items) < 1024 and cost < 3500 and count[15] + 4 <= 128
    record = dict(name=town.name, file=path.name, size_m=384, items=len(town.items),
        trees=count[18]+count[19], lamps=count[15], houses=0, castles=0, city_halls=0,
        draw_cost=cost, local_lights=count[15]+4,
        water_percent=round(100*sum(c in (2, 4) for c in doc['cells'])/len(doc['cells']), 1),
        story_spaces=town.notes, terrain_height_m=max(doc['heights'])/10,
        acceptance_paths=paths)
    print(town.name, len(payload), 'authored bytes;', record['trees'], 'trees;',
          record['lamps'], 'lamps;', record['terrain_height_m'], 'm high', flush=True)
    return record


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    records = [save(factory(), args.output) for factory in (highlands, basin)]
    (args.output/'terrain-manifest.json').write_text(json.dumps(records, indent=2)+'\n')
