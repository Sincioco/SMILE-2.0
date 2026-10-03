"""Authoring-time road clearance and front-door walks for generated Luma maps.

Only draft catalog scenery is adjusted. Native terminals, castles, military
campuses and the approved Ancient Relay centerpiece retain their authored layout.
The checks use continuous brush geometry rather than just an object's cell.
"""
import math
import bisect
from town_design import CATALOG, GROUND, ROAD, BRIDGE, world, prop_outline as outline
from town_document_codec import curve_contains, bezier_segments


def surface(doc,x,z):
    col=bisect.bisect_right(doc['xs'],x)-1
    row=bisect.bisect_right(doc['zs'],z)-1
    if not(0<=col<doc['columns'] and 0<=row<doc['rows']): return 0
    kind=doc.get('base_cells',doc['cells'])[row*doc['columns']+col]
    for brush in doc.get('curves',[]):
        if curve_contains(brush,x,z):
            k=brush[1]
            kind=4 if k==3 and kind in(2,4) else k
    return kind

def contains(item, x, z, margin=0):
    a=math.radians(item['yaw']); c,s=math.cos(a),math.sin(a)
    dx,dz=x-item['position'][0]/10,z-item['position'][2]/10
    px,pz=dx*c-dz*s,dx*s+dz*c
    low,high=CATALOG['templates'][item['template']]['bounds']
    return (low[0]*item['scale'][0]/1000-margin <= px <= high[0]*item['scale'][0]/1000+margin and
            low[1]*item['scale'][2]/1000-margin <= pz <= high[1]*item['scale'][2]/1000+margin)


def street_points(doc,x,z):
    candidates=[]
    for brush in doc['curves']:
        form,kind,x0,z0,x1,z1,width = brush[:7]
        if kind != ROAD: continue
        x0,z0,x1,z1,width=[v/10 for v in (x0,z0,x1,z1,width)]
        if form in (4,8):
            segments = [(x0,z0,x1,z1)] if form==4 else [
                tuple(v/10 for v in segment) for segment in bezier_segments(tuple(brush))]
            projected=[]
            for ax,az,bx,bz in segments:
                dx,dz=bx-ax,bz-az
                u=max(0,min(1,((x-ax)*dx+(z-az)*dz)/max(.001,dx*dx+dz*dz)))
                px,pz=ax+dx*u,az+dz*u
                projected.append((math.hypot(px-x,pz-z),px,pz))
            _,px,pz=min(projected)
        elif form==3:
            length=math.hypot(x-x0,z-z0)
            if length<.001:
                px,pz=x0,z0-x1
            else:
                px,pz=x0+(x-x0)*x1/length,z0+(z-z0)*x1/length
        else: continue
        if surface(doc,px*10,pz*10) in (ROAD,BRIDGE):
            candidates.append((math.hypot(px-x,pz-z),px,pz))
    # Equal-distance streets must make mirrored choices on opposite banks.
    # Raw world X as the tuple tie-breaker sends one house down a different road.
    return sorted(candidates,key=lambda p:(round(p[0],4),round(abs(p[1]-x),4),
                                           round(p[2],4),round(p[1]*(-1 if x<0 else 1),4)))


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
    # Centered civic buildings can sit in a narrow strip between two streets.
    # Try precise axial adjustments before an angular search shifts them sideways.
    attempts=[(step*.25,angle) for step in range(257) for angle in (0,180)] if orient and abs(ox)<.001 else []
    attempts += [(radius,angle) for radius in range(0,65,2)
                 for angle in ((0,) if radius==0 else range(0,360,15))]
    for radius,angle in attempts:
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


def roadside_access(doc,item):
    """Only paving meeting the actual front step counts as a finished entrance."""
    x0,z0,x1,_,_=front_step(item)
    return all(surface(doc,x*10,z*10) in (ROAD,BRIDGE)
               for x,z in (world(item,u,z0-.02) for u in (x0+.1,(x0+x1)/2,x1-.1)))


def front_step(item):
    steps=CATALOG['templates'][item['template']]['steps']
    return min((s for s in steps if s[0]<=0<=s[2]),key=lambda s:s[1])


def entrance_ground(doc,item):
    """An entrance apron may meet/underlap the bottom step, never the building body."""
    x0,z0,x1,z1,_=front_step(item)
    for x,z in outline(item):
        kind=surface(doc,x*10,z*10)
        if kind==GROUND: continue
        a=math.radians(item['yaw']); c,s=math.cos(a),math.sin(a)
        dx,dz=x-item['position'][0]/10,z-item['position'][2]/10
        px=(dx*c-dz*s)/(item['scale'][0]/1000)
        pz=(dx*s+dz*c)/(item['scale'][2]/1000)
        if kind not in (ROAD,BRIDGE) or not(pz<=z0 or (x0-.2<=px<=x1+.2 and pz<=z1)): return False
    return True


def near_road(doc,item):
    _,z0,_,_,_=front_step(item)
    for gap in (.25,.5,.75,1.0,1.5,2.0,3.0,4.0,5.0):
        x,z=world(item,0,z0-gap/(item['scale'][2]/1000))
        kind=surface(doc,x*10,z*10)
        if kind in (ROAD,BRIDGE): return True
        if kind!=GROUND: return False
    return False


def face_roadside(doc,item,others):
    """Prefer the adjacent street over a long path running alongside it."""
    original=item['yaw']
    nearby=original
    x,z=item['position'][0]/10,item['position'][2]/10
    angles=[original]+[round(math.degrees(math.atan2(x-px,z-pz))%360,5)
                       for _,px,pz in street_points(doc,x,z)]
    for yaw in angles:
        item['yaw']=yaw
        if not near_road(doc,item): continue
        if clear_plot(doc,item,others): nearby=yaw
        a=math.radians(yaw)
        for step in range(41):
            gap=step*.05
            set_position(item,x-math.sin(a)*gap,z-math.cos(a)*gap)
            if (roadside_access(doc,item) and entrance_ground(doc,item) and
                not any(contains(other,px,pz,.5) for other in others for px,pz in outline(item))):
                return True
        set_position(item,x,z)
    item['yaw']=nearby
    return False


def add_walk(t,doc,item,buildings,turn=True):
    if turn:
        others=[i for i in buildings if i is not item]
        if face_roadside(doc,item,others):
            return True
        # Face the nearest reachable street before drawing a walk. Keeping an old
        # backwards-facing doorway can otherwise select a distant diagonal road.
        ox,oz=item['position'][0]/10,item['position'][2]/10
        for _,px,pz in street_points(doc,ox,oz):
            item['yaw']=round(math.degrees(math.atan2(ox-px,oz-pz))%360,5)
            if clear_plot(doc,item,others) and add_walk(t,doc,item,buildings,False):
                return True
        raise AssertionError((t.name,item['identity'],'front door has no clear street approach'))
    x0,z0,x1,_,_=front_step(item)
    stair_width=(x1-x0)*item['scale'][0]/1000
    width=max(6,t.step*1.5,stair_width+2)
    # Cover the full bottom-step edge; the rounded cap's tip sits under the step.
    setback=math.sqrt((width/2)**2-(stair_width/2)**2)-.08
    x,z=world(item,(x0+x1)/2,z0-setback/(item['scale'][2]/1000))
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
            probe=dict(doc,curves=doc['curves']+[[4,ROAD,x*10,z*10,px*10,pz*10,width*10]])
            if entrance_ground(probe,item):
                t.path([(x,z),(px,pz)],width)
                return True
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
    doc['curves']=list(doc['curves'])
    for item in buildings:
        add_walk(t,doc,item,buildings)
    saved=t.center; t.center=(0,0); doc=t.document(); t.center=saved
    for item in t.items:
        if item['template'] in range(14,35) or item['template']==38:
            # Pump crystals are deliberate structures on service platforms.
            if t.name=='Neris Waterworks' and item['template']==27: continue
            nearest_plot(doc,item,buildings)
    from road_junctions import round_junctions
    round_junctions(doc)
    t.curves=doc['curves']
    # Trigonometric rotations can leave nominal cardinal edges a few microns
    # apart. Normalize authoring coordinates before rasterizing boundary cells.
    for brush in t.curves:
        brush[2:]=[round(v,3) for v in brush[2:]]
    assert len(t.curves)<=256, (t.name,'surface brush limit')
    return t
