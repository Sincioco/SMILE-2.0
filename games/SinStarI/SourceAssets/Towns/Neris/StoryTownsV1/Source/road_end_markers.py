"""Put travel triggers on the final road cells, side by side when an exit is shared."""
import bisect
import math
from town_access import surface, contains


def relocate(doc):
    xs,zs=doc['xs'],doc['zs']
    cx,cz=(xs[0]+xs[-1])/2,(zs[0]+zs[-1])/2
    groups={}
    for t in doc['map_tiles']:
        groups.setdefault(t['destination'],[]).append(((xs[t['x']]+xs[t['x']+1])/2,(zs[t['z']]+zs[t['z']+1])/2))
    ends=[]
    for brush in doc.get('curves',[]):
        form,k,x,z,u,v,w = brush[:7]
        if form!=4 or k!=3 or w<100:continue
        length=math.hypot(x-u,z-v)
        if length<120:continue
        for ex,ez,ix,iz in ((x,z,u,v),(u,v,x,z)):
            dx,dz=(ex-ix)/length,(ez-iz)/length
            if (ex-cx)*dx+(ez-cz)*dz<200:continue
            if abs(ex-cx)<.55*(xs[-1]-cx) and abs(ez-cz)<.55*(zs[-1]-cz):continue
            if surface(doc,ex+dx*(w/2+5),ez+dz*(w/2+5)) in (3,4):continue
            if any(surface(doc,ex-dz*w*s,ez+dx*w*s) in (3,4) for s in (-1.1,1.1)):continue
            if not any(math.hypot(ex-e[0],ez-e[1])<10 for e in ends):ends.append((ex,ez,dx,dz,w))
    candidates=[]
    if ends:
        for ex,ez,dx,dz,w in ends:
            cells=[]
            for row in range(max(0,bisect.bisect_right(zs,ez-w)-1),min(doc['rows'],bisect.bisect_right(zs,ez+w))):
                for col in range(max(0,bisect.bisect_right(xs,ex-w)-1),min(doc['columns'],bisect.bisect_right(xs,ex+w))):
                    px,pz=(xs[col]+xs[col+1])/2,(zs[row]+zs[row+1])/2
                    if surface(doc,px,pz) not in (3,4):continue
                    ahead=max(xs[col+1]-xs[col],zs[row+1]-zs[row])*1.05
                    if surface(doc,px+dx*ahead,pz+dz*ahead) in (3,4):continue
                    if abs((px-ex)*(-dz)+(pz-ez)*dx)>w*.48:continue
                    if any(surface(doc,px-dx*d,pz-dz*d) not in (3,4) for d in range(0,241,10)):continue
                    if any(contains(i,px/10,pz/10,1.0) for i in doc['items']):continue
                    cells.append((col,row,px,pz,dx,dz))
            candidates.extend(cells)
    else:
        # Original Neris uses edited grid roads rather than analytic brushes.
        for row in range(doc['rows']):
            for col in range(doc['columns']):
                if doc['cells'][row*doc['columns']+col] not in (3,4):continue
                px,pz=(xs[col]+xs[col+1])/2,(zs[row]+zs[row+1])/2
                radial=math.hypot(px-cx,pz-cz)
                for dx,dz in ((1,0),(-1,0),(0,1),(0,-1)):
                    if ((px-cx)*dx+(pz-cz)*dz)/max(1,radial)<.6:continue
                    if abs(px-cx)<.55*(xs[-1]-cx) and abs(pz-cz)<.55*(zs[-1]-cz):continue
                    ex=(xs[col+1]+.01 if dx>0 else xs[col]-.01) if dx else px
                    ez=(zs[row+1]+.01 if dz>0 else zs[row]-.01) if dz else pz
                    if surface(doc,ex,ez) in (3,4):continue
                    if any(surface(doc,px-dx*d,pz-dz*d) not in (3,4) for d in range(0,241,10)):continue
                    if any(contains(i,px/10,pz/10,1.0) for i in doc['items']):continue
                    candidates.append((col,row,px,pz,dx,dz))
    occupied=set();result=[];report=[]
    # Keep existing destinations near their previous exit, but never in series on a road.
    for name,points in groups.items():
        ox=sum(p[0] for p in points)/len(points);oz=sum(p[1] for p in points)/len(points)
        choices=sorted(candidates,key=lambda c:(c[2]-ox)**2+(c[3]-oz)**2)
        chosen=next((c for c in choices if (c[0],c[1]) not in occupied),None)
        if chosen is None:raise AssertionError((doc['name'],name,'no free road-end cell',len(candidates)))
        col,row,px,pz,dx,dz=chosen
        occupied.add((col,row));result.append(dict(x=col,z=row,destination=name))
        report.append(dict(destination=name,position=[px,pz],outward=[dx,dz]))
    doc['map_tiles']=result
    return report
