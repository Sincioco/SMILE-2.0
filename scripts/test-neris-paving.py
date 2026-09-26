"""Regression: streets used different tile dimensions/origins at their crossings."""
from pathlib import Path
import sys
import unittest
import json

sys.path.insert(0, str(Path(__file__).resolve().parents[1] /
    'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Source'))
from paving_grid import tiles
from paving_plan import subtract


class PavingTests(unittest.TestCase):
    def test_canals_remain_open_except_at_real_bridges(self):
        layout=json.loads((Path(__file__).resolve().parents[1] /
            'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/expansion-layout.json').read_text())
        paved=[(x-w/2,y-d/2,x+w/2,y+d/2) for x,y,w,d in layout['paving']]
        holes=subtract(layout['waterRectangles'],layout['bridges'])
        for bounds,_ in tiles(paved):
            for hole in holes:
                self.assertFalse(min(bounds[2],hole[2])>max(bounds[0],hole[0]) and
                    min(bounds[3],hole[3])>max(bounds[1],hole[1]))

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
