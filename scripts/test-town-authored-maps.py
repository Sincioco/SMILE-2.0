"""Focused regression checks for the reported October 3 authored-map defects."""
from pathlib import Path
import math
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1/Source'))
from town_design import CATALOG, decode, unwrap
from town_document_codec import curve_contains

folder = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1/Towns'


def load(name):
    return decode(unwrap((folder / (name + '.town')).read_bytes()), CATALOG)


def surface(doc, x, z):
    for brush in reversed(doc['curves']):
        if curve_contains(brush, x, z):
            return brush[1]
    return 2


for name, half_width, samples in [
    ('East Valley', 35, (2000, 2050, 2090)),
    ('Neris Canals', 80, (3200, 3400, 3590)),
    ("Orin's Village", 40, (1380, 1500, 1850, 2010, 2090)),
]:
    doc = load(name)
    for side in (-1, 1):
        for x in samples:
            for z in (-half_width + 1, 0, half_width - 1):
                assert surface(doc, side*x, z) == 3, (name, x, z, 'road missing')
            for z in (-half_width - 2, half_width + 2):
                assert surface(doc, side*x, z) != 3, (name, x, z, 'road widened')
    print('PASS consistent mirrored road widths:', name)

doc = load('Neris Star Lake')
assert surface(doc, 3400, 2200) == 2
assert surface(doc, 0, -3260) == 2
print('PASS Star Lake circular shoreline without northeast spur')
doc = load('Ancient Relay')
assert surface(doc, 0, 1920) != 3 and surface(doc, 0, -1920) != 3
assert surface(doc, 1800, 0) == surface(doc, -1800, 0) == 3
print('PASS Ancient Relay ring without north/south stubs')
doc = load('Neris Spaceport')
cx, cz = (doc['xs'][0]+doc['xs'][-1])/2, (doc['zs'][0]+doc['zs'][-1])/2
for sx in (-1, 1):
    for sz in (-1, 1):
        assert surface(doc, cx+sx*5700, cz+sz*4150) == 2
print('PASS Spaceport four removed exterior spurs')
doc = load('Neris Waterworks')
for angle in range(0, 360, 72):
    a = math.radians(angle)
    x, z = 1900*math.sin(a), 1900*math.cos(a)
    # Both sides of the spoke, just inside each reservoir rim.
    for side in (-1, 1):
        px = x-300*math.sin(a)+side*70*math.cos(a)
        pz = z-300*math.cos(a)-side*70*math.sin(a)
        assert surface(doc, px, pz) == 2, (angle, side)
    assert surface(doc, x, z) != 2
print('PASS five Waterworks reservoirs retain water beside their spokes and central pads')
doc = load('Neris Relief Quarter')
hall = [item for item in doc['items'] if item['template'] == 9]
assert len(hall) == 1 and hall[0]['position'][::2] == [0, 0] and hall[0]['yaw'] == 0
assert surface(doc, 0, 650) == 1 and surface(doc, 0, 1220) == 3
print('PASS Relief City Hall centered, south-facing, without old north approach')
