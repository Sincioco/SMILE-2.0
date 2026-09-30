"""Regression for garden disks cutting roads, obstructed plots and in-lane exits."""
import sys,math,bisect
from pathlib import Path
from town_design import CATALOG,decode,unwrap

from town_access import surface, outline


def validate(doc):
    for item in doc['items']:
        if item['template'] < 6:
            assert all(surface(doc,x*10,z*10)==1 for x,z in outline(item)), ('Building crosses road/water',item['identity'])
    for form,kind,x0,z0,x1,z1,width in doc['curves']:
        if form == 4 and kind == 3:
            steps=max(1,math.ceil(math.hypot(x1-x0,z1-z0)/10))
            for i in range(steps+1):
                assert surface(doc,x0+(x1-x0)*i/steps,z0+(z1-z0)*i/steps) in (3,4), 'Garden cuts road'
    targets=set()
    cx=(doc['xs'][0]+doc['xs'][-1])/2
    cz=(doc['zs'][0]+doc['zs'][-1])/2
    for tile in doc['map_tiles']:
        x=(doc['xs'][tile['x']]+doc['xs'][tile['x']+1])/2
        z=(doc['zs'][tile['z']]+doc['zs'][tile['z']+1])/2
        assert math.hypot(x-cx,z-cz)>5700, 'Travel trigger is on the circulating road'
        assert surface(doc,x,z) in (3,4), 'Exit is not on a road'
        targets.add(tile['destination'])
    assert len(targets)==7
    print('PASS Spaceport road continuity, building plots and seven outward exits')


if __name__ == '__main__':
    path=Path(__file__).resolve().parent.parent/'Towns/Neris Spaceport.town'
    validate(decode(unwrap(path.read_bytes()),CATALOG))
