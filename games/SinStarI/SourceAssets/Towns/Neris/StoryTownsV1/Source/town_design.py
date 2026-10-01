"""Author editable Studio towns with the TWN9 codec and existing catalog assets.

Design coordinates are metres; the native document uses ten world units/metre.
No Blender, renderer extensions or new asset dependencies are required.
"""
import json
import bisect
import math
import sys
from collections import Counter, deque
from pathlib import Path

ROOT = Path(__file__).resolve().parents[7]
sys.path.insert(0, str(ROOT / 'tools/Character3DViewer'))
from town_document_codec import atomic_write, decode, encode, unwrap, raster_curves, curve_contains

CATALOG = json.loads((ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/catalog.json').read_text(encoding='utf-8'))
GROUND, WATER, ROAD, BRIDGE = 1, 2, 3, 4
DAY = [255, 237, 214, 260, 38, 207.0, 54.0, 1, 65]
NIGHT = [135, 170, 255, 18, 14, 215.0, 32.0, 0, 45]


class Town:
    def __init__(self, name, size=720, step=3, smooth=False):
        self.name, self.size, self.step = name, size, step
        self.n = round(size / step)
        self.cells = [WATER] * (self.n * self.n)
        self.items, self.tiles, self.notes = [], [], []
        self.smooth, self.curves = smooth, []
        self.center = (0, 0)
        self.terrain_style = 0
        self.night = True
        self.night_preset = NIGHT.copy()
        self.symmetric = True
        self.destinations = ('Neris Spaceport', 'Horizon Airport')

    def brush(self, form, kind, x0, z0, x1, z1, width=0):
        if self.smooth:
            self.curves.append([form, kind, x0*10, z0*10, x1*10, z1*10, width*10])

    def paint(self, inside, kind):
        half = self.size / 2
        for z in range(self.n):
            pz = -half + (z + .5) * self.step
            for x in range(self.n):
                px = -half + (x + .5) * self.step
                if inside(px, pz):
                    i = z * self.n + x
                    self.cells[i] = BRIDGE if kind == ROAD and self.cells[i] == WATER else kind

    def rect(self, x0, z0, x1, z1, kind=GROUND):
        self.brush(1, kind, x0, z0, x1, z1)
        self.paint(lambda x, z: x0 <= x <= x1 and z0 <= z <= z1, kind)

    def disk(self, x, z, radius, kind=GROUND):
        self.brush(2, kind, x, z, radius, 0)
        self.paint(lambda a, b: (a-x)**2 + (b-z)**2 <= radius**2, kind)

    def ring(self, x, z, radius, width, kind=ROAD):
        self.brush(3, kind, x, z, radius, 0, width)
        self.paint(lambda a, b: abs(math.hypot(a-x, b-z)-radius) <= width/2, kind)

    def shape(self, form, points, width, kind):
        (x0,z0),(x1,z1),(x2,z2) = points
        brush = [form,kind,x0*10,z0*10,x1*10,z1*10,width*10,x2*10,z2*10]
        assert self.smooth, 'Editable shapes require continuous surfaces'
        self.curves.append(brush)
        self.paint(lambda x,z: curve_contains(brush,x*10,z*10),kind)

    def triangle(self, points, kind=GROUND):
        self.shape(7,points,0,kind)

    def curve(self, points, width, kind=GROUND):
        self.shape(8,points,width,kind)

    def path(self, points, width=12):
        for (x0, z0), (x1, z1) in zip(points, points[1:]):
            self.brush(4, ROAD, x0, z0, x1, z1, width)
            dx, dz = x1-x0, z1-z0
            def hit(x, z):
                t = max(0, min(1, ((x-x0)*dx+(z-z0)*dz)/(dx*dx+dz*dz)))
                return (x-x0-t*dx)**2 + (z-z0-t*dz)**2 <= (width/2)**2
            self.paint(hit, ROAD)

    def place(self, template, x, z, scale=1, yaw=0):
        self.items.append(dict(identity=len(self.items)+1, template=template, source=-1,
            position=[round(x*10, 5), 23.0, round(z*10, 5)],
            scale=[scale*1000]*3, yaw=yaw))

    def grove(self, x, z, columns=3, rows=3, gap=10, scale=1.7):
        for row in range(rows):
            for col in range(columns):
                self.place(18+(row+col)%2, x+(col-(columns-1)/2)*gap,
                           z+(row-(rows-1)/2)*gap, scale*(1+.07*((row+col)%3)), (row*37+col*63)%360)

    def lamps(self, points, scale=1.5):
        for x, z in points:
            self.place(15, x, z, scale)

    def homes(self, x, zs, side=1, template=4):
        for i, z in enumerate(zs):
            self.place(template, x, z, 1.25, 90*side)

    def gates(self, z=0, destinations=('Neris Spaceport', 'Horizon Airport'),
              join_x=None, width=18):
        """West/east pedestrian causeways; markers stay clear of railings/props."""
        end = self.size/2-12
        self.destinations = destinations
        for side, destination in zip((-1, 1), destinations):
            x = side*(end-12)
            bank_start = end-45 if join_x is None else join_x
            road_start = end-60 if join_x is None else join_x
            self.rect(min(side*bank_start, side*(end+4)), z-width*4/3,
                      max(side*bank_start, side*(end+4)), z+width*4/3)
            self.path([(side*road_start, z), (side*end, z)], width)
            self.lamps([(x-12,z-14),(x-12,z+14),(x+12,z-14),(x+12,z+14)], 2.0)
            for row in range(self.n):
                pz = -self.size/2+(row+.5)*self.step
                for col in range(self.n):
                    px = -self.size/2+(col+.5)*self.step
                    if abs(px-x) <= 4.5 and abs(pz-z) <= min(8, width/2):
                        self.tiles.append(dict(x=col, z=row, destination=destination))

    def document(self):
        edges = [(-self.size/2+i*self.step)*10 for i in range(self.n+1)]
        doc = dict(name=self.name, columns=self.n, rows=self.n, cell_size=self.step*10,
            xs=edges, zs=edges, cells=self.cells, items=self.items, map_tiles=self.tiles,
            sun=(self.night_preset if self.night else DAY).copy(), terrain_style=self.terrain_style,
            presets=dict(night_active=self.night, day=DAY.copy(), night=self.night_preset.copy()),
            court_offset=[0,0], court_placed=False)
        if self.smooth:
            doc.update(curves=self.curves, base_cells=[WATER]*len(self.cells))
            doc['cells'] = raster_curves(doc)
            self.cells = doc['cells']
        ox, oz = self.center
        if ox or oz:
            doc['xs'] = [x + ox*10 for x in doc['xs']]
            doc['zs'] = [z + oz*10 for z in doc['zs']]
            doc['items'] = [dict(i, position=[i['position'][0]+ox*10, i['position'][1],
                                            i['position'][2]+oz*10]) for i in self.items]
            doc['curves'] = [b.copy() for b in self.curves]
            for b in doc['curves']:
                b[2] += ox*10
                b[3] += oz*10
                if b[0] in (1, 4, 5, 7, 8):
                    b[4] += ox*10
                    b[5] += oz*10
                if b[0] in (7, 8):
                    b[7] += ox*10
                    b[8] += oz*10
        return doc

    def save(self, folder):
        doc = self.document()
        # Every placed prop must have support; a grove crossing a canal is a
        # layout defect even when the broad road network remains connected.
        for item in doc['items']:
            x, _, z = item['position']
            col = bisect.bisect_right(doc['xs'], x)-1
            row = bisect.bisect_right(doc['zs'], z)-1
            assert doc['cells'][row*self.n+col] != WATER, (self.name, item['template'], x, z, 'unsupported prop')
        cost = sum(len(CATALOG['templates'][i['template']]['parts']) +
                   4*len(CATALOG['templates'][i['template']].get('doors', [])) +
                   len(CATALOG['templates'][i['template']].get('water', [])) for i in self.items)
        lights = 4 + sum(1 if i['template']==15 else 2 if i['template'] in (7,9,10,13) else 0 for i in self.items)
        assert len(self.items) <= 1024 and cost < 3500 and lights <= 128, (self.name, len(self.items), cost, lights)
        assert {t['destination'] for t in self.tiles} == set(self.destinations)
        assert all(self.cells[t['z']*self.n+t['x']] in (ROAD, BRIDGE) for t in self.tiles)
        # Geometric symmetry and road connectivity before the native collision check.
        assert not self.symmetric or all(self.cells[z*self.n+x] == self.cells[z*self.n+self.n-1-x]
                   for z in range(self.n) for x in range(self.n))
        roads = {i for i, c in enumerate(self.cells) if c in (ROAD, BRIDGE)}
        visited, pending = set(), deque([next(iter(roads))])
        while pending:
            i = pending.popleft()
            if i in visited:
                continue
            visited.add(i)
            x,z = i%self.n,i//self.n
            for a,b in ((x-1,z),(x+1,z),(x,z-1),(x,z+1)):
                j=b*self.n+a
                if 0<=a<self.n and 0<=b<self.n and j in roads and j not in visited:
                    pending.append(j)
        assert roads == visited, (self.name, len(roads-visited), 'disconnected paving')
        path = folder / (self.name+'.town')
        atomic_write(path, encode(doc, CATALOG))
        loaded=decode(unwrap(path.read_bytes()), CATALOG)
        assert len(loaded['items'])==len(doc['items']) and loaded['cells']==doc['cells']
        # TWN6 stores precise values at 1/1,000,000 unit resolution.
        for expected, actual in zip(doc['items'],loaded['items']):
            assert all(expected[k]==actual[k] for k in ('identity','template','source'))
            assert all(abs(a-b)<.0011 for a,b in zip(expected['position']+expected['scale']+[expected['yaw']],
                                                    actual['position']+actual['scale']+[actual['yaw']]))
        count=Counter(i['template'] for i in self.items)
        return dict(name=self.name, file=path.name, size_m=self.size, items=len(self.items),
            trees=count[18]+count[19], lamps=count[15], houses=sum(count[i] for i in range(6)),
            castles=count[13], city_halls=count[9], draw_cost=cost, local_lights=lights,
            water_percent=round(100*sum(c in (WATER,BRIDGE) for c in self.cells)/len(self.cells),1),
            story_spaces=self.notes)
