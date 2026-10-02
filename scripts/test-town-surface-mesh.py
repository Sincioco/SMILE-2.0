"""Regression audit for visible material teeth and open raised road/water seams.

Reads Studio's actual portable mesh recipes, including cached geometry. Authored
curves remain the authority. The tolerance is one native unit (10 cm), larger
than the contour refinement's 0.25-unit chord tolerance.
"""
import argparse
from collections import defaultdict
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1'
sys.path.insert(0, str(ROOT / 'tools/Character3DViewer'))
sys.path.insert(0, str(PACKAGE / 'Source'))
from town_document_codec import decode, unwrap, prepared_records
from town_blender_terrain import read_prepared
from town_design import CATALOG
from town_access import surface
from town_surface_layers import LAYERS


def edge_key(a, b):
    return tuple(sorted((tuple(round(v, 5) for v in a), tuple(round(v, 5) for v in b))))


def audit(path):
    raw = path.read_bytes()
    document = decode(unwrap(raw), CATALOG)
    patches = read_prepared(prepared_records(raw).__getitem__)
    edges, walls = defaultdict(list), set()
    bad = []
    checked = 0
    for part in patches:
        x0, z0, x1, z1, y0, y1, x2, z2, kind, plane = part[:10]
        if kind in (1, 3) and plane in (1, 2, 4):
            a, b = (x0, z0), (x1, z1)
            if plane == 1:
                b = (x1, z0)
            elif plane == 2:
                b = (x0, z1)
            walls.add(edge_key(a, b))
            continue
        if kind not in (1, 2, 3) or plane not in (3, 5):
            continue
        points = [(x0, z0), (x1, z1), (x2, z2)]
        ys = [y0, y1, part[11]] if plane == 5 else [
            LAYERS['ROAD_Y' if kind == 3 else 'GROUND_Y']] * 3
        area = abs((x1-x0)*(z2-z0)-(x2-x0)*(z1-z0)) / 2
        if area < .000001:
            for a, b in zip(points, points[1:] + points[:1]):
                if a != b:
                    walls.add(edge_key(a, b))
            continue
        checked += 1
        for i in range(3):
            j = (i+1) % 3
            edges[edge_key(points[i], points[j])].append((kind, min(ys[i], ys[j])))
        if area < .1:
            continue
        x, z = (x0+x1+x2)/3, (z0+z1+z2)/3
        actual = min(3, surface(document, x, z))
        if actual == kind:
            continue
        # Accept polygonal chord error at a real boundary; reject isolated teeth.
        if all(min(3, surface(document, x+dx, z+dz)) != kind
               for dx, dz in ((1, 0), (-1, 0), (0, 1), (0, -1),
                              (1, 1), (-1, 1), (1, -1), (-1, -1))):
            bad.append({'kind': kind, 'actual': actual, 'points': points, 'area': area})
    open_edges = []
    wet_edges = 0
    for edge, faces in edges.items():
        water = [height for kind, height in faces if kind == 2]
        land = [height for kind, height in faces if kind in (1, 3)]
        if water and land and max(land) > min(water) + .01:
            wet_edges += 1
            if edge not in walls:
                open_edges.append(edge)
    return {'map': document['name'], 'triangles': checked, 'wet_edges': wet_edges,
            'wrong_surface_triangles': len(bad), 'open_wet_edges': len(open_edges),
            'examples': bad[:8], 'seam_examples': open_edges[:8]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--maps', type=Path, default=PACKAGE / 'Towns')
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    names = ['Neris Town'] + [r['name'] for r in json.loads(
        (PACKAGE / 'manifest.json').read_text(encoding='utf-8'))]
    results = []
    for name in dict.fromkeys(names):
        path = args.maps / (name + '.town')
        if not path.exists():
            raise FileNotFoundError(path)
        result = audit(path)
        results.append(result)
        print(name, 'wrong surface:', result['wrong_surface_triangles'],
              'open wet edges:', result['open_wet_edges'], flush=True)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
    failed = any(r['wrong_surface_triangles'] or r['open_wet_edges'] for r in results)
    print(('FAIL' if failed else 'PASS'), 'Town Surface Mesh:', len(results), 'maps')
    return int(failed)


if __name__ == '__main__':
    raise SystemExit(main())
