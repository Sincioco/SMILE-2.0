// Palace balconies and owner-local cornice refinement.
export const build = String.raw`
assert not bpy.data.objects.get('NC.Palace.Luxury.Balcony.West.Deck')
def balcony_rail(name,owner,a,b,z):
    length=math.dist(a,b);count=max(2,int(length/.68))
    for i in range(count+1):
        t=i/count;x=a[0]+(b[0]-a[0])*t;y=a[1]+(b[1]-a[1])*t
        lathe(name+'.Baluster.'+str(i),owner,(x,y),[(.12,z),(.12,z+.13),(.065,z+.26),(.125,z+.65),(.07,z+.91),(.13,z+1.02)],segments=8)
    for dz,r,mat in [(0,.11,'IvoryStone'),(1.12,.16,'IvoryStone'),(1.27,.038,'RoyalGold')]:
        pipe(name+'.Rail.'+str(dz),owner,[(a[0],a[1],z+dz),(b[0],b[1],z+dz)],r,mat)

for side,sign in [('West',-1),('East',1)]:
    x0,x1=sorted([sign*22.5,sign*39])
    prefix='NC.Palace.Luxury.Balcony.'+side
    bevel(box(prefix+'.Deck','Palace',(x0-.2,x1+.2),(12.65,16.15),(12.65,13.13)),.065)
    box(prefix+'.GoldLip','Palace',(x0-.22,x1+.22),(12.62,16.15),(12.97,13.05),'RoyalGold')
    balcony_rail(prefix+'.Front','Palace',(x0,12.85),(x1,12.85),13.15)
    for x in [x0,x1]:balcony_rail(prefix+'.Side.'+str(x),'Palace',(x,12.85),(x,16.1),13.15)
    for i in range(6):
        x=x0+(x1-x0)*i/5
        # Deep profiled brackets below the projecting balcony.
        lathe(prefix+'.Bracket.'+str(i),'Palace',(x,15.3),[(.13,11.55),(.22,11.85),(.45,12.35),(.58,12.65)],segments=12)
    for i,x in enumerate([sign*25,sign*30.5,sign*36]):
        window(prefix+'.UpperWindow.'+str(i),'Palace',bpy.data.objects['NC.Palace.Wing.'+side],x,16,14.45,1.7,2.9,1.8,.25,.19,0,'RoyalGlass')
    # Fluted pilasters bridge the plain lower wall into the balcony supports.
    for x in [sign*21.8,sign*26,sign*32.7,sign*39.3]:
        lux_column(prefix+'.Pier.'+str(x),'Palace',x,15.6,3.25,9.05,.21)

# Repeated Gothic eave arches and pendant brackets on the palace drum.
for i in range(40):
    a=i*math.tau/40
    x=13.22*math.sin(a);y=34-13.22*math.cos(a)
    o=archband('NC.Palace.Luxury.DrumArcade.'+str(i),'Palace',1.42,.88,.33,36.68,.10,-.10,.15)
    place(o,x,y,a)
    for z,rr in [(36.64,13.28),(37.86,13.45)]:
        # Rings are made once; avoid coplanar duplicate geometry.
        if i==0:torus_ring('NC.Palace.Luxury.DrumRing.'+str(z),'Palace',(0,34),rr,z,.10,'IvoryStone')
    o=box('NC.Palace.Luxury.DrumBracket.'+str(i),'Palace',(-.11,.11),(-.32,.12),(36.43,36.83),'RoyalGold')
    place(o,x,y,a)

# Detailed tower collars replace the visual emptiness beneath broad teal roofs.
for owner in ['Palace','Gatehouse','Fortifications']:
    shafts=[o for o in bpy.data.collections['NC.'+owner].objects if o.name.endswith('.Shaft')]
    for shaft in shafts:
        bounds=[shaft.matrix_world@Vector(v) for v in shaft.bound_box]
        x=(min(v.x for v in bounds)+max(v.x for v in bounds))/2
        y=(min(v.y for v in bounds)+max(v.y for v in bounds))/2
        top=max(v.z for v in bounds);r=(max(v.x for v in bounds)-min(v.x for v in bounds))/2
        prefix=shaft.name[:-6]+'.Luxury'
        for i in range(16):
            a=i*math.tau/16;xx=x+(r+.1)*math.sin(a);yy=y-(r+.1)*math.cos(a)
            width=min(1.25,r*math.tau/16*.8)
            o=archband(prefix+'.CorniceArch.'+str(i),owner,width,.65,.22,top-1.3,.08,-.16,.16)
            place(o,xx,yy,a)
            o=box(prefix+'.GoldPendant.'+str(i),owner,(-.07,.07),(-.19,.12),(top-1.68,top-1.26),'RoyalGold')
            place(o,xx,yy,a)
        # Smooth tower-roof profiles, while crystals keep their crisp faceted geometry.
        roof=bpy.data.objects.get(shaft.name[:-6]+'.Roof')
        if roof:
            for p in roof.data.polygons:p.use_smooth=abs(p.normal.z)<.98

# Architectural blind arcades and paired gate columns articulate the broad entrance wall.
for sign in [-1,1]:
    for i,x in enumerate([sign*8.0,sign*8.7]):
        lux_column('NC.Gatehouse.Luxury.PortalColumn.'+str(sign)+'.'+str(i),'Gatehouse',x,-64.55,.8,10.2,.28)
    for x in [sign*27,sign*37,sign*43]:
        bevel(box('NC.Fortifications.Luxury.Pilaster.'+str(x),'Fortifications',(x-.34,x+.34),(-64.50,-64.10),(.75,11.25)),.035)
        for z in [1.4,9.9,11.05]:
            box('NC.Fortifications.Luxury.Pilaster.Cap.'+str(x)+'.'+str(z),'Fortifications',(x-.55,x+.55),(-64.65,-64.03),(z,z+.18))
    for x in [sign*25,sign*29,sign*35,sign*39,sign*43]:
        archband('NC.Fortifications.Luxury.BlindArch.'+str(x),'Fortifications',2.7,2.25,1.0,8.65,.12,-64.38,-64.15,x=x)
for i in range(13):
    x=-13.2+i*2.2
    archband('NC.Gatehouse.Luxury.CorniceArch.'+str(i),'Gatehouse',1.75,1.35,.45,18.25,.13,-64.37,-64.05,x=x)
    box('NC.Gatehouse.Luxury.Pendant.'+str(i),'Gatehouse',(x-.10,x+.10),(-64.41,-64.07),(17.98,18.40),'RoyalGold')
for i,(w,h,b,y) in enumerate([(14.55,16.28,.18,-63.92),(15.10,16.56,.16,-64.08)]):
    bevel(archband('NC.Gatehouse.Luxury.Arch.'+str(i),'Gatehouse',w,h,11,0,b,y,y+.18),.028)
    archband('NC.Gatehouse.Luxury.ArchGold.'+str(i),'Gatehouse',w+.05,h+.035,11,0,.035,y-.035,y,'RoyalGold')
# Facade, roof-ridge and cornice detail is owner-local; gate passage and bridge stay unchanged.
for owner in ['Palace','Gatehouse','Fortifications']:
    for o in bpy.data.collections['NC.'+owner].objects:
        if '.Luxury.' in o.name and o.type=='MESH':normals(o)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
s.render.resolution_x=1600;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/facade-progress.png'
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.render.render(write_still=True)
result={'image':s.render.filepath,'objects':len(s.objects),
 'unchanged_geometry':{owner:fingerprint(owner)==v for owner,v in json.loads(s['luxury_baselines']).items()}}
`;
