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


HIGHLAND_RIVER_BENDS = [((-65,-192),(55,-155),(-15,-75)),
                       ((-15,-75),(-85,-10),(-35,35)),
                       ((-35,35),(45,65),(45,135))]
HIGHLAND_RIVER = [tuple((1-t)**2*a+2*t*(1-t)*b+t*t*c for a,b,c in zip(*bend))
                  for index,bend in enumerate(HIGHLAND_RIVER_BENDS)
                  for t in (step/24 for step in range(25) if index == 0 or step > 0)]
HIGHLAND_BUILDINGS = [(2,-135,-117,1.7,180), (5,142,-30,1.4,270), (2,-130,90,1.8,90)]
HIGHLAND_ROUTE = [(-192,144),(-140,144),(-104,108),(-84,46),(-56,-24),
                 (-12,-72),(66,-66),(112,-8),(102,62),(144,144),(192,144)]


def highlands_height(x, z, lookout=()):
    bed = 28*smooth((144-z)/300)
    hills = sum(peak*smooth(1-math.hypot((x-cx)/rx, (z-cz)/rz))
                for cx,cz,rx,rz,peak in [(-153,-132,125,135,112), (137,-135,125,118,126),
                                       (-152,58,95,85,76), (150,66,98,110,92)])
    hills *= smooth((distance(x,z,HIGHLAND_RIVER)-18)/100)
    hills *= smooth((distance(x,z,HIGHLAND_ROUTE)-14)/46)
    height = (bed+hills)*smooth((math.hypot((x-28)/105,(z-174)/85)-1)/.45)
    if lookout:
        lengths = [math.dist(a,b) for a,b in zip(lookout,lookout[1:])]
        total, travelled, best = sum(lengths), 0, (1e9,0)
        for ((ax,az),(bx,bz)), length in zip(zip(lookout,lookout[1:]),lengths):
            u=max(0,min(1,((x-ax)*(bx-ax)+(z-az)*(bz-az))/length**2))
            offset=math.hypot(x-ax-u*(bx-ax),z-az-u*(bz-az))
            if offset<best[0]:best=(offset,bed+38*smooth((travelled+u*length)/total))
            travelled+=length
        height+=(best[1]-height)*(1-smooth((best[0]-12)/35))*smooth((distance(x,z,HIGHLAND_ROUTE)-7)/30)
    for _,cx,cz,_,_ in HIGHLAND_BUILDINGS:
        level = 64 if cx<0 and cz<0 else 28*smooth((144-cz)/300)
        blend = 1-smooth((math.hypot(x-cx,z-cz)-14)/25)
        height += (level-height)*blend
    return height


def basin_height(x, z):
    # Two steep falls descend 42 m with a broad crossing shelf between them. Away from
    # the river, wide shoulders blend into a gradual side-trail climb.
    falls = 26*smooth((x+20)/14) + 16*smooth((x-55)/14)
    trail = 42*smooth((x+150)/300)
    river = 1-smooth((abs(z-24)-20)/28)
    height = trail + (falls-trail)*river
    return height*smooth((math.hypot(x+166,z-32)-105)/22)


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
    town = start('Willowstep Highlands', 4)
    # An open snow-fed valley between uneven alpine ridges and climbable shoulders.
    route = [(-192,144),(-140,144),(-104,108),(-84,46),(-56,-24),
             (-12,-72),(66,-66),(112,-8),(102,62),(144,144),(192,144)]
    lookout = [(-56,-24),(-110,-20),(-146,-64),(-118,-108)]
    flows = []
    for bend, width in zip(HIGHLAND_RIVER_BENDS, (11,13,16)):
        town.curve(bend,width,WATER)
        flows.append(len(town.curves)-1)
    town.lake((28,172),[(-25,125),(10,110),(30,119),(60,105),(83,124),
        (101,140),(107,175),(86,212),(40,220),(-12,214),(-50,189),(-35,166),(-58,151)])
    main = town.path(route,8)
    start_join = min(main,key=lambda p:math.dist(p,(-84,46)))
    end_join = min(main,key=lambda p:math.dist(p,(103,62)))
    riverside = [start_join,(-36,90),(40,110),end_join]
    paths = [main,town.path(lookout,7),town.path(riverside,7)]
    height = lambda x,z: highlands_height(x,z,paths[1])
    town.gates(144, ('Neris Relief Quarter', 'Silverfall Basin'), join_x=156, width=8)
    paths[0][0]=(-188,144)
    paths[0][-1]=(188,144)
    decorate(town,height,paths,[(-118,-108),(0,0),(112,-8)],9137)
    town.items = [item for item in town.items if item['template'] not in (18,19)]
    for template,x,z,scale,yaw in HIGHLAND_BUILDINGS:
        town.place(template,x,z,scale,yaw)
    for x,z,scale,yaw in [(-165,-150,.35,25),(-146,-153,.25,90),(-162,-130,.22,160),
                           (161,-94,.4,70),(140,-117,.25,210),(171,-64,.2,10),
                           (-174,38,.35,110),(-160,50,.22,250),(132,164,.26,150)]:
        town.place(35,x,z,scale,yaw)
        town.items[-1]['scale'][1] *= .45
    town.notes = ['Rebuilt as a snowy mountain pass with broad alpine ridges and exposed rock on steep slopes.',
                  'Packed snow trails climb the west lookout and cross the meltwater to a second alpine slope.',
                  'Three broad meltwater bends feed a much larger irregular lake open across the south map edge.',
                  'The central dead-end road spur is removed; broad clearings support future encounters.']
    return town, height, flows, paths


def basin():
    town = start('Silverfall Basin', 1)
    route = [(-192,-54),(-140,-54),(-102,-96),(-43,-83),
             (0,-70),(56,-70),(113,-40),(150,-54),(192,-54)]
    side_route = [(-192,142),(-105,160),(-25,154),(28,118),
                  (68,99),(138,96),(192,120)]
    crossing = [(28,-70),(28,118)]
    clearings = [(-90, -84), (0, -70), (90, -84)]
    flow = stream(town, (192,24), (-102,24), 24)
    town.lake((-162,29),[(-216,-30),(-179,-29),(-169,-9),(-142,-20),(-118,-4),
        (-96,21),(-119,49),(-127,81),(-151,72),(-176,97),(-193,65),(-218,74)])
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
    if town.name == 'Neris Relief Quarter':
        from relief_landscape import style
        doc['appearance'] = [style((a+b)/20,(c+d)/20) for c,d in zip(doc['zs'],doc['zs'][1:]) for a,b in zip(doc['xs'],doc['xs'][1:])]
    doc['heights'] = [round(height(x/10, z/10)*1000)/100
                      for z in doc['zs'] for x in doc['xs']]
    from road_end_markers import boundary_exits
    boundary_exits(doc)
    # Keep natural mountain slopes in their continuous Highland palette. A hard
    # per-cell height/slope color threshold creates visible stair-step boundaries.
    if town.name in ('Silverfall Basin','Willowstep Highlands') and not any(b[0] in (5,6) for b in doc['curves']):
        from road_junctions import round_junctions
        round_junctions(doc)
    doc['flows'] = [[0, 0, 0] for _ in doc['curves']]
    for index in (flow if isinstance(flow,list) else ([] if flow is None else [flow])):
        doc['flows'][index] = [1,1,100]
    for item in doc['items']:
        item['position'][1] = 23 + terrain_offset(doc, item['position'][0], item['position'][2])
    if town.name != 'Neris Relief Quarter':
        for item in doc['items']:
            if item['template'] == 15:
                item['template'] = 39
                item['scale'] = [1000,1000,1000]
    doc['items'] = [item for item in doc['items'] if item['template'] not in (15, 18, 19, 39) or
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
    assert len(town.items) < 1024 and cost < 3500 and count[15] + count[39] + 4 <= 128
    record = dict(name=town.name, file=path.name, size_m=(doc['xs'][-1]-doc['xs'][0])/10, items=len(town.items),
        trees=count[18]+count[19], lamps=count[15], houses=sum(count[i] for i in range(6)), castles=count[6]+count[13], city_halls=count[9],
        draw_cost=cost, campfires=count[39], local_lights=count[15]+count[39]+4,
        water_percent=round(100*sum(c in (2, 4) for c in doc['cells'])/len(doc['cells']), 1),
        story_spaces=town.notes, terrain_height_m=max(doc['heights'])/10,
        acceptance_paths=paths, acceptance_goal=paths[0][-1] if paths else (188,0),
        acceptance_gates=[[(doc['xs'][t['x']]+doc['xs'][t['x']+1])/20,
                           (doc['zs'][t['z']]+doc['zs'][t['z']+1])/20]
                          for t in doc['map_tiles']],
        acceptance_spawn=((-40,-95) if town.name == 'Sunglass Expanse' else
                          (28,0) if town.name == 'Silverfall Basin' else
                          paths[0][len(paths[0])//2] if town.name == 'Willowstep Highlands' else
                          (0,18) if town.name == 'Neris Relief Quarter' else (0,0)))
    print(town.name, len(payload), 'authored bytes;', record['trees'], 'trees;',
          record['lamps'], 'lamps;', record['terrain_height_m'], 'm high', flush=True)
    return record


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    designs = [highlands(), basin()]
    prepare(designs[0][0])
    for factory in (forest, mountains, desert):
        town = factory()
        prepare(town)
        designs.append((town, town.height or (lambda x,z: 0), None, town.acceptance_paths))
    records = [save(design, args.output) for design in designs]
    (args.output/'terrain-manifest.json').write_text(json.dumps(records, indent=2)+'\n')
