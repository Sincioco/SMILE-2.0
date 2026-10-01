"""Add reciprocal atlas destinations on existing roads; never repaint user terrain."""
import argparse
import json
import math
from pathlib import Path
from town_design import CATALOG, atomic_write, decode, encode, unwrap
from road_end_markers import relocate

FOLDER = Path(__file__).resolve().parent.parent


def positions(doc):
    return [((doc['xs'][t['x']]+doc['xs'][t['x']+1])/2,
             (doc['zs'][t['z']]+doc['zs'][t['z']+1])/2) for t in doc['map_tiles']]


def blocked_bounds(doc):
    result = []
    for item in doc['items']:
        shape = CATALOG['templates'][item['template']]
        low, high = shape['bounds']
        sx, _, sz = item['scale']
        # Conservative whole-assembly boxes also avoid decorations and bridge rails.
        a = math.radians(item['yaw'])
        c, s = math.cos(a), math.sin(a)
        points = [(item['position'][0]+x*10*sx/1000*c+z*10*sz/1000*s,
                   item['position'][2]-x*10*sx/1000*s+z*10*sz/1000*c)
                  for x in (low[0], high[0]) for z in (low[1], high[1])]
        result.append((min(p[0] for p in points)-25, min(p[1] for p in points)-25,
                       max(p[0] for p in points)+25, max(p[1] for p in points)+25))
    return result


def connect(doc, destinations, origin, nodes):
    """Preserve destinations and put every trigger on its final road cell."""
    missing = sorted(set(destinations)-{t['destination'] for t in doc['map_tiles']})
    if not missing:
        relocate(doc)
        return []
    before = {k: v for k, v in doc.items() if k != 'map_tiles'}
    occupied = {(t['x'], t['z']) for t in doc['map_tiles']}
    bounds = blocked_bounds(doc)
    columns, rows = doc['columns'], doc['rows']
    candidates = []
    for z in range(1, rows-2):
        for x in range(1, columns-2):
            if not all(doc['cells'][b*columns+a] in (3,4)
                       for a,b in ((x,z),(x+1,z),(x,z+1),(x+1,z+1))):
                continue
            px = (doc['xs'][x]+doc['xs'][x+2])/2
            pz = (doc['zs'][z]+doc['zs'][z+2])/2
            if any(x0 <= px <= x1 and z0 <= pz <= z1 for x0,z0,x1,z1 in bounds):
                continue
            if doc['xs'][x+2]-doc['xs'][x] > 220 or doc['zs'][z+2]-doc['zs'][z] > 220:
                continue
            candidates.append((x,z,px,pz))
    added = []
    cx = (doc['xs'][0]+doc['xs'][-1])/2
    cz = (doc['zs'][0]+doc['zs'][-1])/2
    extent = max(doc['xs'][-1]-doc['xs'][0], doc['zs'][-1]-doc['zs'][0])*.42
    for destination in missing:
        dx,dz = nodes[destination][0]-origin[0],nodes[destination][1]-origin[1]
        length = math.hypot(dx,dz)
        tx,tz = cx+dx/length*extent,cz+dz/length*extent
        pads = positions(doc)
        ordered = sorted(candidates, key=lambda p:(p[2]-tx)**2+(p[3]-tz)**2)
        chosen = next((p for p in ordered
            if all(math.hypot(p[2]-a,p[3]-b) >= 150 for a,b in pads)
            and all((a,b) not in occupied for a in range(p[0]-1,p[0]+3)
                    for b in range(p[1]-1,p[1]+3))), None)
        if chosen is None:
            raise ValueError(f'No clear road pad in {doc["name"]} for {destination}')
        x,z,px,pz = chosen
        for a in (x,x+1):
            for b in (z,z+1):
                doc['map_tiles'].append(dict(x=a,z=b,destination=destination))
                occupied.add((a,b))
        added.append(dict(destination=destination, center=[px,pz]))
    relocate(doc)
    assert before == {k:v for k,v in doc.items() if k != 'map_tiles'}
    assert len(doc['map_tiles']) <= 4096
    return added


def build(original, output):
    graph = json.loads((FOLDER/'world-layout.json').read_text(encoding='utf-8'))
    nodes = {name:(x,y) for name,x,y in graph['nodes']}
    names = list(nodes)
    reports = []
    writes = []
    output.mkdir(parents=True, exist_ok=True)
    for index,name in enumerate(names):
        source = original if name == 'Neris Town' else FOLDER/'Towns'/f'{name}.town'
        doc = decode(unwrap(source.read_bytes()), CATALOG)
        targets = [names[b if a == index else a] for a,b in graph['links'] if index in (a,b)]
        added = connect(doc, targets, nodes[name], nodes)
        payload = encode(doc, CATALOG)
        reopened = decode(payload, CATALOG)
        assert all(reopened[k] == v for k,v in doc.items() if k not in ('payload','dirty'))
        path = output/f'{name}.town' if name == 'Neris Town' else source
        writes.append((path,payload))
        reports.append(dict(name=name,destinations=sorted({t['destination'] for t in doc['map_tiles']})))
        print(name, 'added', len(added), 'destinations', len(targets), flush=True)
    for path,payload in writes:
        atomic_write(path,payload)
    (FOLDER/'travel-connections.json').write_text(json.dumps(reports,indent=2)+'\n',encoding='utf-8')


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--original',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    build(args.original,args.output)
