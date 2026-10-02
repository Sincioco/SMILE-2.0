"""Put travel triggers on the final road cells, side by side when an exit is shared."""
import bisect
import math
from town_access import surface, contains
from town_document_codec import curve_contains, raster_curves


def boundary_exits(doc):
    """Square boundary approaches; shared exits branch into separate labelled lanes."""
    xs,zs=doc['xs'],doc['zs']
    groups={}
    for tile in doc['map_tiles']:
        groups.setdefault(tile['destination'],[]).append(tile)
    exits=[]
    for name,tiles in groups.items():
        x=sum((xs[t['x']]+xs[t['x']+1])/2 for t in tiles)/len(tiles)
        z=sum((zs[t['z']]+zs[t['z']+1])/2 for t in tiles)/len(tiles)
        side=min(range(4),key=lambda s:(x-xs[0],xs[-1]-x,z-zs[0],zs[-1]-z)[s])
        roads=[b for b in doc.get('curves',[]) if b[1]==3 and b[0] in (4,8) and curve_contains(b,x,z)]
        width=max((b[6] for b in roads),default=120)
        if not doc.get('curves'):
            # Measure the actual legacy grid causeway, including its nonuniform cells.
            axis=(0,1) if side<2 else (1,0)
            lo=hi=0
            while lo<600 and surface(doc,x-axis[0]*(lo+5),z-axis[1]*(lo+5)) in (3,4):lo+=5
            while hi<600 and surface(doc,x+axis[0]*(hi+5),z+axis[1]*(hi+5)) in (3,4):hi+=5
            width=max(60,lo+hi)
            if side<2:z+=(hi-lo)/2
            else:x+=(hi-lo)/2
        exits.append(dict(name=name,x=x,z=z,side=side,width=width,center=z if side<2 else x))
    clusters=[]
    for e in sorted(exits,key=lambda e:(e['side'],e['center'])):
        if clusters and clusters[-1][-1]['side']==e['side'] and abs(clusters[-1][-1]['center']-e['center'])<max(e['width'],clusters[-1][-1]['width']):
            clusters[-1].append(e)
        else:clusters.append([e])
    doc.setdefault('curves',[])
    doc.setdefault('base_cells',doc['cells'].copy())
    result=[]
    for cluster in clusters:
        mean=sum(e['center'] for e in cluster)/len(cluster)
        lane=max(60,min(120,max(e['width'] for e in cluster)/len(cluster)))
        for ordinal,e in enumerate(cluster):
            side=e['side']; edges=zs if side<2 else xs
            center=e['center'] if len(cluster)==1 else mean+(ordinal-(len(cluster)-1)/2)*(lane+40)
            width=e['width'] if len(cluster)==1 else lane
            indices=[i for i in range(len(edges)-1) if edges[i+1]>center-width/2 and edges[i]<center+width/2]
            # Align paving and trigger widths so neither leaves a walkable bypass.
            low,high=edges[indices[0]],edges[indices[-1]+1]
            center=(low+high)/2; width=high-low
            boundary=(xs if side<2 else zs)[0 if side%2==0 else -1]
            inward=1 if side%2==0 else -1
            axis=(xs if side<2 else zs)
            approach=max(120,min(240,abs((e['x'] if side<2 else e['z'])-boundary)))
            start=[e['x'],e['z']]
            start[side//2]=boundary+inward*approach
            bend=[boundary+inward*80,center] if side<2 else [center,boundary+inward*80]
            # Join an existing lane before the fan-out, keeping the branch navigable.
            original=[e['x'],e['z']]
            if math.dist(original,start)>1:doc['curves'].append([4,3,*original,*start,width])
            if math.dist(start,bend)>1:doc['curves'].append([4,3,*start,*bend,width])
            if side<2: rectangle=[min(boundary,bend[0]),low,max(boundary,bend[0]),high]
            else: rectangle=[low,min(boundary,bend[1]),high,max(boundary,bend[1])]
            doc['curves'].append([1,3,*rectangle,0])
            border_indices=[i for i in range(len(axis)-1) if
                (axis[i]<boundary+60 if inward==1 else axis[i+1]>boundary-60)]
            for j in border_indices:
                for i in indices:
                    col,row=(j,i) if side<2 else (i,j)
                    assert not any(t['x']==col and t['z']==row for t in result),(doc['name'],'overlapping exit lanes')
                    result.append(dict(x=col,z=row,destination=e['name']))
    # Cover alternate roads reaching the same map edge too. Sampling the last
    # two metres catches rounded legacy ends; the final rectangle squares them.
    assigned={(t['x'],t['z']):t for t in result}
    for side in range(4):
        choices=[e for e in exits if e['side']==side]
        if not choices:continue
        edges=zs if side<2 else xs
        axis=xs if side<2 else zs
        boundary=axis[0 if side%2==0 else -1]
        inward=1 if side%2==0 else -1
        covered=[]
        for i,(low,high) in enumerate(zip(edges,edges[1:])):
            hit=any(surface(doc,*( (boundary+inward*d,low+(high-low)*u) if side<2 else
                                   (low+(high-low)*u,boundary+inward*d))) in (3,4)
                    for d in (1,20) for u in (.1,.5,.9))
            if hit:covered.append(i)
        runs=[]
        for i in covered:
            if runs and runs[-1][-1]+1==i:runs[-1].append(i)
            else:runs.append([i])
        for run in runs:
            low,high=edges[run[0]],edges[run[-1]+1]
            box=[min(boundary,boundary+inward*80),low,max(boundary,boundary+inward*80),high]
            if side>=2:box=[box[1],box[0],box[3],box[2]]
            doc['curves'].append([1,3,*box,0])
            for i in run:
                center=(edges[i]+edges[i+1])/2
                destination=min(choices,key=lambda e:abs(center-e['center']))['name']
                for j in range(len(axis)-1):
                    if (axis[j]<boundary+60 if inward==1 else axis[j+1]>boundary-60):
                        key=(j,i) if side<2 else (i,j)
                        assigned.setdefault(key,dict(x=key[0],z=key[1],destination=destination))
    result=list(assigned.values())
    doc['map_tiles']=result
    doc['cells']=raster_curves(doc)
    if 'flows' in doc:doc['flows'] += [[0,0,0] for _ in range(len(doc['curves'])-len(doc['flows']))]
    assert len(doc['curves'])<=256 and len(result)<=4096,(doc['name'],len(doc['curves']),len(result))
    return doc


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
