"""Regression: streets used different tile dimensions/origins at their crossings."""
from pathlib import Path
import sys
import unittest
import json
from collections import deque

sys.path.insert(0, str(Path(__file__).resolve().parents[1] /
    'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Source'))
from paving_grid import tiles
from paving_plan import plan, subtract, CANALS, BRIDGES, DISTRICTS, COMPARISON_MOAT, COMPARISON_BRIDGE, ROYAL_MOAT


class PavingTests(unittest.TestCase):
    def test_canals_remain_open_except_at_real_bridges(self):
        layout=json.loads((Path(__file__).resolve().parents[1] /
            'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/expansion-layout.json').read_text())
        streets=subtract(plan(layout),CANALS+BRIDGES+COMPARISON_MOAT+ROYAL_MOAT)+[COMPARISON_BRIDGE]
        for bounds,_ in tiles(streets):
            for hole in CANALS+subtract(COMPARISON_MOAT,[COMPARISON_BRIDGE])+ROYAL_MOAT:
                self.assertFalse(min(bounds[2],hole[2])>max(bounds[0],hole[0]) and
                    min(bounds[3],hole[3])>max(bounds[1],hole[1]))

    def test_residential_loops_and_every_door_reach_city_hall(self):
        layout=json.loads((Path(__file__).resolve().parents[1] /
            'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/expansion-layout.json').read_text())
        roads=subtract(plan(layout),CANALS+COMPARISON_MOAT+ROYAL_MOAT)+BRIDGES+[COMPARISON_BRIDGE,(-46,86,-28,130)]
        cells={(x,y) for a,b,c,d in roads for x in range(int(a),int(c)) for y in range(int(b),int(d))}
        # Actual homes and civic/commercial buildings cannot be traversed as shortcuts.
        blockers=[(h['x']-h['width']/2,h['y']-h['depth']/2,
                   h['x']+h['width']/2,h['y']+h['depth']/2) for h in layout['homes']]
        blockers += [(-11.2,16.7,11.2,34.2),(26.3,15.3,37.3,28.7),
                     (26.3,-6.7,37.3,6.7),(26.3,-28.8,37.3,-15.3)]
        cells={p for p in cells if not any(a<=p[0]<=c and b<=p[1]<=d for a,b,c,d in blockers)}
        pending=deque([(0,7)]); reachable={(0,7)}
        while pending:
            x,y=pending.popleft()
            for next_point in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
                if next_point in cells and next_point not in reachable:
                    reachable.add(next_point);pending.append(next_point)
        for name,(xs,ys) in DISTRICTS.items():
            for x in xs:
                for y in ys:self.assertIn((x,y),reachable,name+' road intersection')
        for h in layout['homes']:
            door=(int(h['x']),int(h['y']-h['depth']/2)-2)
            self.assertIn(door,reachable,str(h)+' door route')
        self.assertIn((-228,96),reachable,'New castle entrance reaches City Hall across moat')
        self.assertIn((-37,128),reachable,'Rebuilt royal castle entrance reaches City Hall')
        self.assertIn((115,138),reachable,'Moved military headquarters reaches City Hall')

    def test_crossing_and_bridge_share_seams_and_tones(self):
        street = list(tiles([(-45, -12.4, 45, -7.6)]))
        bridge = list(tiles([(-14.6, -12.4, -10.4, -7.6)]))
        for bounds, tone in bridge:
            x0, y0, x1, y1 = bounds
            matching = [(b, t) for b, t in street if b[0] <= x0 and b[2] >= x1 and b[1] == y0 and b[3] == y1]
            self.assertEqual(len(matching), 1)
            self.assertEqual(matching[0][1], tone)

    def test_union_has_no_overlapping_faces_or_extra_seams(self):
        shapes = [(-2, -4, 2, 4), (-4, -2, 4, 2)]
        faces = list(tiles(shapes))
        self.assertEqual(len(faces), 12)
        self.assertAlmostEqual(sum((r[2]-r[0])*(r[3]-r[1]) for r, _ in faces), 12*1.95**2)
        for i, (a, _) in enumerate(faces):
            for b, _ in faces[i+1:]:
                self.assertFalse(min(a[2],b[2]) > max(a[0],b[0]) and min(a[3],b[3]) > max(a[1],b[1]))


if __name__ == '__main__':
    unittest.main()
