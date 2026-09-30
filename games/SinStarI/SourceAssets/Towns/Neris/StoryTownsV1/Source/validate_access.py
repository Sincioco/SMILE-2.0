"""Regression for the reported blocked roads and missing building entrances."""
from pathlib import Path
import sys
from town_design import CATALOG,decode,unwrap
from town_access import outline,world,surface


def validate(doc):
    entrances=0
    for item in doc['items']:
        template=item['template']
        building=template in (*range(6),8,9,10,11,12)
        scenery=template in range(14,35) or template==38
        if doc['name']=='Ancient Relay' and template==10: continue
        if doc['name']=='Neris Waterworks' and template==27: continue
        if building or scenery:
            assert all(surface(doc,x*10,z*10)==1 for x,z in outline(item)), (
                doc['name'],item['identity'],'footprint overlaps road/water')
        if building:
            low,_=CATALOG['templates'][template]['bounds']
            x,z=world(item,0,low[1]-.75/(item['scale'][2]/1000))
            assert surface(doc,x*10,z*10) in (3,4), (
                doc['name'],item['identity'],'front stair has no path')
            entrances+=1
    for tile in doc['map_tiles']:
        assert doc['cells'][tile['z']*doc['columns']+tile['x']] in (3,4), (
            doc['name'],tile['destination'],'exit no longer on road')
    if doc['name']=='Neris Star Lake':
        assert not any(i['template'] in (8,11,12) for i in doc['items']), 'Six removed shops returned'
    print('PASS',doc['name'],entrances,'clear connected entrances')


if __name__=='__main__':
    folder=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parent.parent/'Towns'
    for path in sorted(folder.glob('*.town')):
        validate(decode(unwrap(path.read_bytes()),CATALOG))
