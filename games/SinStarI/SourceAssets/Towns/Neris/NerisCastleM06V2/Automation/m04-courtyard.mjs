// Courtyard and curtain wall detail; other owners fingerprinted.
export const build = String.raw`
assert not bpy.data.objects.get('NC.Courtyard.Paving.Tiles'), 'Use a fresh preceding checkpoint; do not duplicate completed detail.'
s['milestone']='M04';s['m04_independent_fingerprints']=json.dumps({owner:fingerprint(owner) for owner in ['Site','Gatehouse','Bridge','Palace']})
bpy.ops.wm.save_as_mainfile(filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M04-r001.blend')
# Layered curtain-wall plinths and cornices. Keep the original three-metre wall body.
walls=[('West',(-52.22,-48.78),(-64,64)),('East',(48.78,52.22),(-64,64)),('North',(-49,49),(60.78,64.22)),('SouthWest',(-49,-15),(-64.22,-60.78)),('SouthEast',(15,49),(-64.22,-60.78))]
for id,x,y in walls:
    for suffix,z in [('Base',(.08,.65)),('Plinth',(1.8,2.15)),('Cornice',(11.2,11.7))]:
        bevel(box('NC.Fortifications.Wall.'+id+'.'+suffix,'Fortifications',x,y,z),.045)
    box('NC.Fortifications.Wall.'+id+'.Gold','Fortifications',x,y,(11.72,11.82),'RoyalGold')
for o in bpy.data.collections['NC.Fortifications'].objects:
    if '.Merlon.' in o.name:bevel(o,.07)
# Actual open pointed arcades along the edges of the court.
for side,x in [('West',-36.5),('East',36.5)]:
    angle=-math.pi/2 if side=='West' else math.pi/2
    for i in range(6):
        yy=-39.5+(i+.5)*47/6
        pts=archpath(7.03,7.55,5.4)[1:-1]
        arch=extrude_xz('NC.Courtyard.Arcade.'+side+'.'+str(i),'Courtyard',pts+[(3.515,8),(-3.515,8)],-.28,.28)
        place(arch,x,yy,angle)
        trim=archband('NC.Courtyard.ArcadeTrim.'+side+'.'+str(i),'Courtyard',7.03,7.55,5.4,0,.13,-.30,-.27,'RoyalGold')
        place(trim,x,yy,angle)
    for i in range(7):
        yy=-39.5+i*47/6
        box('NC.Courtyard.ColumnBase.'+side+'.'+str(i),'Courtyard',(x-.57,x+.57),(yy-.57,yy+.57),(.03,.5))
        box('NC.Courtyard.ColumnCapital.'+side+'.'+str(i),'Courtyard',(x-.52,x+.52),(yy-.52,yy+.52),(5.35,5.65))
# Low terrace balustrades flank the broad staircase and turn along both sides.
def railing(name,a,b,z):
    length=math.dist(a,b);count=max(1,int(length/1.4))
    for i in range(count+1):
        t=i/count;x=a[0]+(b[0]-a[0])*t;y=a[1]+(b[1]-a[1])*t
        lathe(name+'.Baluster.'+str(i),'Courtyard',(x,y),[(.16,z),(.16,z+.2),(.10,z+.3),(.17,z+.68),(.10,z+.95),(.16,z+1.05)],segments=8)
    pipe(name+'.Rail','Courtyard',[(a[0],a[1],z+1.16),(b[0],b[1],z+1.16)],.13,'IvoryStone')
    pipe(name+'.Gold','Courtyard',[(a[0],a[1],z+1.29),(b[0],b[1],z+1.29)],.035,'RoyalGold')
for side,sign in [('West',-1),('East',1)]:
    railing('NC.Courtyard.TerraceRail.'+side,(sign*8,14.1),(sign*41,14.1),3)
# One paving mesh with actual gaps; its supporting base is below it.
base=bpy.data.objects['NC.Courtyard.Paving']
for v in base.data.vertices:
    if v.co.z>0:v.co.z-=.07
verts=[];faces=[]
for ix in range(10):
    for iy in range(9):
        x=-40+ix*8;y=-46+iy*6;n=len(verts)
        verts.extend([(x+.035,y+.035,.02),(x+7.965,y+.035,.02),(x+7.965,y+5.965,.02),(x+.035,y+5.965,.02)])
        faces.append((n,n+1,n+2,n+3))
mesh('NC.Courtyard.Paving.Tiles','Courtyard',verts,faces,'IvoryStone','separated paving surfaces')
# Courtyard edge inlay is outside the four-metre route.
for sign in [-1,1]:
    box('NC.Courtyard.Inlay.'+str(sign),'Courtyard',(sign*32-.04,sign*32+.04),(-45,7),(.03,.04),'RoyalGold')
remove('NC.Courtyard.Fountain.Crystal')
lathe('NC.Courtyard.Fountain.Pedestal','Courtyard',(0,-18),[(2.3,1.03),(2.3,1.25),(1.7,1.4),(1.4,2.1)],segments=32)
lathe('NC.Courtyard.Fountain.Crystal','Courtyard',(0,-18),[(.4,2.1),(1.35,4),(1.1,6.2),(0,10)],'CrystalBlue',6)
torus_ring('NC.Courtyard.Fountain.Rim','Courtyard',(0,-18),5.78,.9,.10,'IvoryStone')
torus_ring('NC.Courtyard.Fountain.RimGold','Courtyard',(0,-18),5.68,.97,.035)
for i in range(8):
    a=i*math.tau/8
    cone('NC.Courtyard.Fountain.Spout.'+str(i),'Courtyard',(2.5*math.cos(a),-18+2.5*math.sin(a)),.15,.15,(1.02,1.2),'RoyalGold',12)
for side,x in [('West',-26),('East',26)]:
    for i,y in enumerate([-32,-12,6]):
        name='NC.Courtyard.Planter.'+side+'.'+str(i)
        lathe(name,'Courtyard',(x,y),[(1.6,.02),(1.8,.25),(1.6,1.0),(1.85,1.15)],segments=24)
        cone(name+'.Soil','Courtyard',(x,y),1.6,1.6,(1.05,1.11),'DarkIron',24)
        lathe(name+'.Cypress','Courtyard',(x,y),[(.2,1.1),(.7,1.5),(1,2.8),(.9,4),(.5,5.5),(0,7)],'Foliage',12)
for o in bpy.data.collections['NC.Courtyard'].objects:
    if o.type=='MESH':normals(o)
# Real route dependency: set door fittings inside the existing recess.
for suffix in ['Mullion','Transom']:
    o=bpy.data.objects['NC.Palace.Portal.Main.'+suffix]
    o.location.y=14.53
    o.dimensions.y=.07
for o in s.objects:
    if o.name.startswith('NC.Palace.Portal.Main.Tracery'):o.location.y=14.5
s.camera=review_camera('NC.CAM.CourtyardRaised',(25,-55,32),(0,-2,7),30)
s.render.resolution_x=1600;s.render.resolution_y=1100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M04-r001/courtyard-raised.png'
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.Courtyard'];s.render.resolution_x=2048;s.render.resolution_y=1152
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M04-r001/courtyard-eye.png'
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'courtyard_objects':len(bpy.data.collections['NC.Courtyard'].objects),'independent_owners_unchanged':{owner:fingerprint(owner)==value for owner,value in json.loads(s['m04_independent_fingerprints']).items()}}
`;
