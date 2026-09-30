"""Authoring-time road clearance and front-door walks for generated Luma maps.

Only draft catalog scenery is adjusted. Native terminals, castles, military
campuses and the approved Ancient Relay centerpiece retain their authored layout.
The checks use continuous brush geometry rather than just an object's cell.
"""
import math
import bisect
from town_design import CATALOG, GROUND, ROAD, BRIDGE


def surface(doc,x,z):
    col=bisect.bisect_right(doc['xs'],x)-1
    row=bisect.bisect_right(doc['zs'],z)-1
    if not(0<=col<doc['columns'] and 0<=row<doc['rows']): return 0
    kind=doc.get('base_cells',doc['cells'])[row*doc['columns']+col]
    for form,k,x0,z0,x1,z1,w in doc.get('curves',[]):
        dx,dz=x-x0,z-z0
        if form==1: hit=x0<=x<=x1 and z0<=z<=z1
        elif form==2: hit=dx*dx+dz*dz<=x1*x1
        elif form==3: hit=max(0,x1-w/2)**2<=dx*dx+dz*dz<=(x1+w/2)**2
        else:
            u=max(0,min(1,(dx*(x1-x0)+dz*(z1-z0))/max(.000001,(x1-x0)**2+(z1-z0)**2)))
            hit=(dx-u*(x1-x0))**2+(dz-u*(z1-z0))**2<=w*w/4
        if hit: kind=4 if k==3 and kind in(2,4) else k
    return kind

def world(item, x, z):
    a=math.radians(item['yaw']); c,s=math.cos(a),math.sin(a)
    x*=item['scale'][0]/1000; z*=item['scale'][2]/1000
    return item['position'][0]/10+x*c+z*s, item['position'][2]/10-x*s+z*c


def outline(item, margin=0):
    low,high=CATALOG['templates'][item['template']]['bounds']
    sx,sz=item['scale'][0]/1000,item['scale'][2]/1000
    x0,x1=low[0]-margin/sx,high[0]+margin/sx
    z0,z1=low[1]-margin/sz,high[1]+margin/sz
    nx,nz=max(2,math.ceil((x1-x0)*sx/3)),max(2,math.ceil((z1-z0)*sz/3))
    return [world(item,x0+(x1-x0)*i/nx,z0+(z1-z0)*j/nz)
            for i in range(nx+1) for j in range(nz+1)]


def contains(item, x, z, margin=0):
    a=math.radians(item['yaw']); c,s=math.cos(a),math.sin(a)
    dx,dz=x-item['position'][0]/10,z-item['position'][2]/10
    px,pz=dx*c-dz*s,dx*s+dz*c
    low,high=CATALOG['templates'][item['template']]['bounds']
    return (low[0]*item['scale'][0]/1000-margin <= px <= high[0]*item['scale'][0]/1000+margin and
            low[1]*item['scale'][2]/1000-margin <= pz <= high[1]*item['scale'][2]/1000+margin)


def street_points(doc,x,z):
    candidates=[]
    for form,kind,x0,z0,x1,z1,width in doc['curves']:
        if kind != ROAD: continue
        x0,z0,x1,z1,width=[v/10 for v in (x0,z0,x1,z1,width)]
        if form==4:
            dx,dz=x1-x0,z1-z0
            u=max(0,min(1,((x-x0)*dx+(z-z0)*dz)/max(.001,dx*dx+dz*dz)))
            px,pz=x0+dx*u,z0+dz*u
        elif form==3:
            length=max(.001,math.hypot(x-x0,z-z0))
            px,pz=x0+(x-x0)*x1/length,z0+(z-z0)*x1/length
        else: continue
        if surface(doc,px*10,pz*10) in (ROAD,BRIDGE):
            candidates.append((math.hypot(px-x,pz-z),px,pz))
    return sorted(candidates)


def clear_plot(doc,item,others):
    points=outline(item,.4)
    if any(surface(doc,x*10,z*10)!=GROUND for x,z in points): return False
    return not any(contains(other,x,z,.5) for other in others for x,z in points)


def set_position(item,x,z):
    item['position'][0]=round(x*10,5)
    item['position'][2]=round(z*10,5)


def nearest_plot(doc,item,others,orient=False):
    ox,oz=item['position'][0]/10,item['position'][2]/10
    old_yaw=item['yaw']
    # Local correction only, deterministic and bounded; no regeneration of town design.
    for radius in range(0,65,2):
        angles=(0,) if radius==0 else range(0,360,15)
        for angle in angles:
            a=math.radians(angle)
            x,z=ox+radius*math.sin(a)*(-1 if ox<0 else 1),oz+radius*math.cos(a)
            set_position(item,x,z)
            item['yaw']=old_yaw
            if clear_plot(doc,item,others): return
            if orient:
                streets=street_points(doc,x,z)
                if not streets: continue
                _,px,pz=streets[0]
                item['yaw']=round(math.degrees(math.atan2(x-px,z-pz))%360,5)
            else:
                item['yaw']=old_yaw
            if clear_plot(doc,item,others): return
    raise AssertionError((doc['name'],item['identity'],'no clear local plot'))


def add_walk(t,doc,item,buildings,turn=True):
    low,_=CATALOG['templates'][item['template']]['bounds']
    # The round end of the walk meets the front stair edge, outside the footprint.
    width=max(6,t.step*1.5)
    x,z=world(item,0,low[1]-(width/2+.5)/(item['scale'][2]/1000))
    a=math.radians(item['yaw'])
    front=(-math.sin(a),-math.cos(a))
    for _,px,pz in street_points(doc,x,z):
        dx,dz=px-x,pz-z
        length=math.hypot(dx,dz)
        if length>.1 and (dx*front[0]+dz*front[1])/length<.5: continue
        steps=max(1,math.ceil(length))
        # A short entry walk may only cross open lawn or join existing paving.
        valid=True
        for i in range(steps+1):
            cx,cz=x+dx*i/steps,z+dz*i/steps
            if surface(doc,cx*10,cz*10) not in (GROUND,ROAD,BRIDGE):
                valid=False; break
            if any(other is not item and contains(other,cx,cz,width/2+.5) for other in buildings):
                valid=False; break
        if valid:
            t.path([(x,z),(px,pz)],width)
            return True
    if turn:
        ox,oz=item['position'][0]/10,item['position'][2]/10
        others=[i for i in buildings if i is not item]
        for _,px,pz in street_points(doc,ox,oz):
            item['yaw']=round(math.degrees(math.atan2(ox-px,oz-pz))%360,5)
            if clear_plot(doc,item,others) and add_walk(t,doc,item,buildings,False): return True
        raise AssertionError((t.name,item['identity'],'front door has no clear street approach'))
    return False


def prepare(t):
    """Run after the layout, before serialization and reciprocal travel markers."""
    # Ground painted after a street cannot silently cut the street into fragments.
    # The layouts deliberately place all their water before roads.
    t.curves.sort(key=lambda b:b[1]==ROAD)
    doc=t.document()
    # Use local design coordinates (airport placement adds a world translation).
    ox,oz=t.center
    if ox or oz:
        saved=t.center; t.center=(0,0); doc=t.document(); t.center=saved
    buildings=[i for i in t.items if i['template'] in (*range(6),8,9,10,11,12)]
    if t.name=='Ancient Relay': buildings=[]  # User explicitly accepted the central tower.
    for item in buildings:
        others=[i for i in buildings if i is not item]
        nearest_plot(doc,item,others,orient=True)
    # Connect every primary entrance only after all building footprints are settled.
    for item in buildings:
        add_walk(t,doc,item,buildings)
    saved=t.center; t.center=(0,0); doc=t.document(); t.center=saved
    for item in t.items:
        if item['template'] in range(14,35) or item['template']==38:
            # Pump crystals are deliberate structures on service platforms.
            if t.name=='Neris Waterworks' and item['template']==27: continue
            nearest_plot(doc,item,buildings)
    assert len(t.curves)<=256, (t.name,'surface brush limit')
    return t
