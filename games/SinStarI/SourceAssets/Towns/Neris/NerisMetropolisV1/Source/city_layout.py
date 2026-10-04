"""One authored metre-space layout for the Blender city and native town document."""
import math
import random

EXTENT=3200
LAND_RADIUS=2800
RINGS=(270,820,1430,2050,2680)
ROAD_WIDTH=32
LAKES=[(-1560,120,470,630),(2020,-1470,380,640)]

# These are artistic city scales. They are deliberately independent of actual
# architectural heights, per Sin's direction. Source silhouettes retain metres.
HEROES=[
    ('Luma Crown Spire',0,0,1,0),
    ('Burj Khalifa',300,1110,1,0),
    ('Shanghai Tower',1270,420,1,0),
    ('Petronas Twin Towers',-670,1630,1.4,0),
    ('Marina Bay Sands',1470,-1780,1.85,4.71238898038469),
    ('Jeddah Tower',250,-2340,1,0),
    ('Shanghai World Financial Center',1620,850,1,0),
    ('Merdeka 118',-480,520,1,0),
    ('Makkah Royal Clock Tower',-510,-480,1,0),
    ('Ping An Finance Center',-2340,430,1,0),
    ('Lotte World Tower',2200,450,1,0),
    ('One World Trade Center',-1720,1710,1,0),
    ('Guangzhou CTF Finance Centre',-1200,1910,1,0),
    ('Tianjin CTF Finance Centre',-2160,-1010,1,0),
    ('CITIC Tower',-1300,-2030,1,0),
    ('Taipei 101',1000,-840,1,0),
    ('Jin Mao Tower',1240,900,1,0),
    ('Oriental Pearl Tower',1750,170,1.15,0),
    ('Neris Tide Arcology',2170,-750,1.3,0),
    ('Starweave Civic Hall',-240,-1040,1.65,0),
    ('Aether Gate Observatory',-2050,920,1,0),
    ('Cloud Forest',770,-1700,1.7,0.5),
    ('Flower Dome',1070,-2150,1.7,0.5235987755982988),
    ('Supertree Grove',410,-1690,1.5,0),
    ('Golden Gate Bridge',2875,0,1,0),
    ('Millau Viaduct',0,2875,1,1.5707963267948966),
    ('Beipanjiang Bridge',-2875,0,1,0),
    ('Ada Bridge',0,-2875,1,1.5707963267948966),
    ('Neris Sphere',630,-1675,1,0),
]


def lake(x,y):
    return any(((x-a)/rx)**2+((y-b)/ry)**2<1 for a,b,rx,ry in LAKES)


def road_distance(x,y):
    r=math.hypot(x,y)
    closest=min(abs(r-ring) for ring in RINGS)
    if r>260:
        angle=math.atan2(y,x)
        closest=min(closest,r*abs(math.sin(angle-round(angle/(math.pi/6))*math.pi/6)))
    return closest


def placements():
    # Keep the removed Jin Mao landmark's lot reserved; do not move the other
    # authored blocks when removing the foreground Taipei-like tower.
    items=[dict(name=n,x=x,y=y,scale=s,yaw=a) for n,x,y,s,a in HEROES if n!='Jin Mao Tower']
    rng=random.Random(41026)
    # Frontage blocks follow each avenue's tangent, with a sidewalk setback.
    # Four nearby buildings share each block; landmarks retain their open plazas.
    for ring in RINGS[1:]:
        for side in (-1,1):
            if ring == RINGS[-1] and side == 1:
                continue
            radius=ring+side*(98 if ring == RINGS[-1] else 90)
            count=round(math.tau*radius/128)
            for i in range(count):
                angle=(i+.5)*math.tau/count
                x,y=radius*math.cos(angle),radius*math.sin(angle)
                yaw=angle+math.pi/2
                ca,sa=math.cos(yaw),math.sin(yaw)
                corners=[(x+dx*ca-dy*sa,y+dx*sa+dy*ca)
                         for dx in (-54,0,54) for dy in (-52,0,52)]
                if any(lake(px,py) or math.hypot(px,py)>LAND_RADIUS-8 or
                       road_distance(px,py)<34 for px,py in corners):
                    continue
                if x<-850 and -1350<y<1500:
                    continue
                if any(math.hypot(x-a,y-b)<(320 if n=='Marina Bay Sands' else 255)*s
                       for n,a,b,s,_ in HEROES):
                    continue
                kind=rng.randrange(0,8) if y>0 or (x>0 and y>-1200) else rng.randrange(8,14)
                if y<-1500 and ring==RINGS[-1] and rng.random()<.18:
                    kind=rng.randrange(14,16)
                if any(math.hypot(x-a,y-b)<440*s for _,a,b,s,_ in HEROES):
                    kind=rng.randrange(8,14)
                items.append(dict(name='Neighborhood '+str(kind),x=round(x,3),y=round(y,3),
                                  scale=1,yaw=yaw))
    # Groves batch individual tree meshes without a single enormous park collider.
    for y in range(-1260,1471,180):
        for x in range(-2450,-900,180):
            if math.hypot(x,y)>2600 or road_distance(x,y)<110:
                continue
            if any(lake(x+dx,y+dy) for dx in (-90,0,90) for dy in (-90,0,90)):
                continue
            if any(math.hypot(x-a,y-b)<150*s for _,a,b,s,_ in HEROES):
                continue
            items.append(dict(name='Park Grove 4',x=x,y=y,scale=1,yaw=rng.random()*math.tau))
    for i in range(72):
        a=(i+.5)*math.tau/72
        x,y=2750*math.cos(a),2750*math.sin(a)
        if min(abs(x),abs(y))<65:
            continue
        items.append(dict(name='Park Grove 7',x=x,y=y,scale=.75,yaw=a))
    assert len(items)<=1024
    return items


def roads():
    paths=[]
    for radius in RINGS:
        paths.append([(radius*math.cos(i*math.tau/256),radius*math.sin(i*math.tau/256))
                      for i in range(257)])
    for i in range(12):
        a=i*math.pi/6
        end=2550 if i%3==0 else 2680
        paths.append([(270*math.cos(a),270*math.sin(a)),(end*math.cos(a),end*math.sin(a))])
    return paths
