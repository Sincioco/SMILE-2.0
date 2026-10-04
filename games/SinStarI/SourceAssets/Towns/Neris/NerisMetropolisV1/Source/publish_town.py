"""Publish only the new authored Metropolis document; never alter existing towns."""
import json
import math
from pathlib import Path
import sys
from city_layout import EXTENT, LAND_RADIUS, LAKES, RINGS, placements, lake, HEROES

ROOT=Path(__file__).resolve().parents[1]
REPO=ROOT.parents[5]
sys.path.insert(0,str(REPO/'tools/Character3DViewer'))
from town_catalog import load_catalog
from town_document_codec import encode, decode, atomic_write


def publish(target):
    catalog=load_catalog()
    templates={t['label']:t['id'] for t in catalog['templates']}
    count=256
    edges=[(-EXTENT+i*EXTENT*2/count)*10 for i in range(count+1)]
    curves=[[2,1,0,0,LAND_RADIUS*10,0,0]]
    for x,y,rx,ry in LAKES:
        for i in range(48):
            a,b=i*math.tau/48,(i+1)*math.tau/48
            curves.append([7,2,x*10,y*10,(x+rx*math.cos(a))*10,
                (y+ry*math.sin(a))*10,0,(x+rx*math.cos(b))*10,(y+ry*math.sin(b))*10])
    for r in RINGS:
        curves.append([3,3,0,0,r*10,0,720 if r==RINGS[-1] else 480])
    for i in range(12):
        a=i*math.pi/6
        # Modeled bridge decks own the outer surface, without a coplanar road.
        end=2550 if i%3==0 else RINGS[-1]
        curves.append([4,3,2700*math.cos(a),2700*math.sin(a),
            end*10*math.cos(a),end*10*math.sin(a),480])
    day=[255,237,214,260,38,207,54,1,65]
    night=[140,174,255,112,14,207,25,1,45]
    layout=placements()
    items=[dict(identity=i+1,template=templates[v['name']],source=-1,
        position=[v['x']*10,21,v['y']*10],scale=[v['scale']*1000]*3,
        yaw=-math.degrees(v['yaw'])) for i,v in enumerate(layout)]
    # Authored Studio placements use the existing reusable Crystal Lamp catalog
    # item; they survive normal .town saves and .blend exports like any other item.
    for ring in RINGS:
        radius=ring+(46 if ring==RINGS[-1] else 34)
        for i in range(round(math.tau*radius/180)):
            a=(i+.5)*math.tau/round(math.tau*radius/180)
            x,y=radius*math.cos(a),radius*math.sin(a)
            if lake(x,y) or any(math.hypot(x-hx,y-hy)<100*s for _,hx,hy,s,_ in HEROES):
                continue
            items.append(dict(identity=len(items)+1,template=15,source=-1,
                position=[x*10,21,y*10],scale=[1800]*3,yaw=0))
    exits=[]
    for x,z,name in [(count-1,count//2,'Neris Town'),(0,count//2,'Neris Spaceport'),
                     (count//2,0,'Horizon Airport'),(count//2,count-1,'Neris Canals')]:
        exits.append(dict(x=x,z=z,destination=name))
    town=dict(name='Neris Metropolis',dirty=0,columns=count,rows=count,cell_size=edges[1]-edges[0],
        xs=edges,zs=edges,cells=[2]*(count*count),base_cells=[2]*(count*count),curves=curves,
        items=items,sun=day,map_tiles=exits,presets=dict(night_active=False,day=day,night=night),
        court_offset=[0,0],court_placed=False,terrain_style=0,landmark=0,
        teleport_spawn=[0,-3500],initial_camera=[52000,54000,-63000,0,1300,0,0,1,0,45,1,250000],
        npc_spawns=[None]*9)
    for i,item in enumerate(items):
        item['identity']=i+1
    assert len(items)<=1024
    payload=encode(town,catalog)
    decoded=decode(payload,catalog)
    assert len(decoded['items'])==len(items) and decoded['initial_camera']==town['initial_camera']
    target=Path(target)
    atomic_write(target,payload)
    print(json.dumps(dict(path=str(target),items=len(items),brushes=len(curves),bytes=len(payload))))


if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser(description='Generate a new initial city; never overwrite a live authored town.')
    parser.add_argument('--output',type=Path,required=True)
    publish(parser.parse_args().output)
