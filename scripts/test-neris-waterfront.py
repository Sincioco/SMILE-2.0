"""Regression checks for the reported broken road and bridge connections."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
layout = json.loads((root/'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/expansion-layout.json').read_text())
roads = [(x-w/2,y-d/2,x+w/2,y+d/2) for x,y,w,d in layout['paving']]


def contains(r,x,y):
    return r[0]-.001<=x<=r[2]+.001 and r[1]-.001<=y<=r[3]+.001


def touch(a,b):
    return a[0]<=b[2]+.001 and b[0]<=a[2]+.001 and a[1]<=b[3]+.001 and b[1]<=a[3]+.001


seen = {i for i,r in enumerate(roads) if contains(r,0,-106)}
pending = list(seen)
while pending:
    current = roads[pending.pop()]
    for i,r in enumerate(roads):
        if i not in seen and touch(current,r):
            seen.add(i)
            pending.append(i)


def connected(x,y,label):
    assert any(contains(roads[i],x,y) for i in seen), f'Disconnected route: {label} at {x},{y}'


for x,y,label in [(-366,110,'Tripo bridge landing'),(-116,140,'Royal bridge'),
        (145,145,'HQ entrance road'),(251,-112,'Worker/shop junction'),
        (-58,-112,'West City Hall crossing'),(58,-112,'East City Hall crossing'),
        (-231,-331,'West/south corner'),(251,-331,'East/south corner'),
        (-477,220,'West castle circuit'),(-250,344,'North castle circuit'),
        (12,220,'East castle circuit'),(145,344,'North HQ road'),(251,242,'East HQ road'),
        (0,layout['tower'][1]-36,'Tower front approach'),(0,-339,'Southern arrival avenue'),
        (-172,25,'Estates crossroads'),(-172,-205,'Middle crossroads'),
        (174,-5,'Workers crossroads'),(174,-234,'Shops crossroads')]:
    connected(x,y,label)
for home in layout['homes']:
    direction=1 if home['x']<0 else -1
    assert home['yaw']==90*direction, 'Residential front must face inward'
    connected(home['x']+direction*(home['depth']/2+2),home['y'],home['style']+' front path')
for x,y,yaw in layout['legacyHomes']:
    connected(x+(5 if x<0 else -5),y,'Cottage front path')
for y in (-156,-208,-260):connected(116,y,'Shop front path')

water=layout['waterRectangles']
for i,a in enumerate(water):
    for b in water[i+1:]:
        overlap=min(a[2],b[2])-max(a[0],b[0]),min(a[3],b[3])-max(a[1],b[1])
        assert min(overlap)<=.001, 'Overlapping water rectangles cause double blending'
for x in range(-477,14):
    assert any(contains(r,x,110) for r in layout['land']), 'Castle main avenue needs land'
    assert not any(contains(r,x,110) for r in water), 'Castle avenue must not stop over water'
assert sum(h['style']=='Large' for h in layout['homes'])==8
assert sum(h['style']=='Medium' for h in layout['homes'])==10
assert layout['towerScale']==4
assert layout['cityHallScale']==2
# The reported half-width south joins must cover the entire connecting streets.
for x in (-89,-83,83,89):
    for y in (-324,-330,-337):connected(x,y,'Flush south junction')
for item in layout['decorations']:
    x,y,r=item['x'],item['y'],item['radius']
    assert not any(a-r-.59<x<c+r+.59 and b-r-.59<y<d+r+.59
                   for a,b,c,d in layout['roadRectangles']+water), item
print('PASS all managed garden footprints clear roads and water')
print('PASS connected waterfront roads, all front paths, castle circuit, water seams and current district counts')
