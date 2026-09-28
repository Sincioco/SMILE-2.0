// Palace architecture owner; explicit geometry cuts and shared module vocabulary.
export const helpers = String.raw`
from mathutils import Matrix
def normals(o):
    import bmesh
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
def cut(target,cutter):
    normals(cutter);bpy.context.view_layer.objects.active=target
    m=target.modifiers.new('Owned architectural recess','BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cutter
    bpy.ops.object.modifier_apply(modifier=m.name)
    remove(cutter.name)
def place(o,x,y,angle):
    o.matrix_world=Matrix.Translation(Vector((x,y,0)))@Matrix.Rotation(angle,4,'Z')@o.matrix_world
def window(name,owner,target,x,y,z,w=3,h=6,spring=4.5,recess=.2,band=.25,angle=0,pane='WarmWindow'):
    for o in list(bpy.data.collections['NC.'+owner].objects):
        if o.name.startswith(name+'.'):remove(o.name)
    points=archpath(w,h,spring,z)
    cutter=extrude_xz(name+'.Cutter',owner,points,-.15,recess+.025)
    place(cutter,x,y,angle)
    if isinstance(target,list):
        for i,t in enumerate(target):
            c=cutter.copy();c.data=cutter.data.copy();bpy.data.collections['NC.'+owner].objects.link(c);c.name=name+'.Cutter.'+str(i)
            cut(t,c)
        remove(cutter.name)
    elif target:cut(target,cutter)
    else:remove(cutter.name)
    pieces=[]
    pieces.append(extrude_xz(name+'.Pane',owner,points,recess-.025,recess,pane))
    pieces.append(bevel(archband(name+'.Stone',owner,w,h,spring,z,band,-.25,.035),.035))
    pieces.append(archband(name+'.Gold',owner,w+.04,h+.02,spring,z,.06,-.29,-.24,'RoyalGold'))
    pieces.append(box(name+'.Sill',owner,(-w/2-band-.12,w/2+band+.12),(-.42,.1),(z-.18,z),'IvoryStone'))
    pieces.append(box(name+'.Mullion',owner,(-.07,.07),(-.13,recess-.03),(z,z+h-.12),'RoyalGold'))
    cross=z+spring*.63
    pieces.append(box(name+'.Transom',owner,(-w/2,w/2),(-.1,recess-.04),(cross-.05,cross+.05),'RoyalGold'))
    for side in [-1,1]:
        points=[(side*w*.42,-.09,z+spring*.55),(side*w*.3,-.09,z+spring),(0,-.09,z+h*.88)]
        pieces.append(pipe(name+'.Tracery.'+str(side),owner,points,.045,'RoyalGold'))
    for o in pieces:place(o,x,y,angle)
    return pieces
def review_camera(name,location,target,lens=48):
    o=bpy.data.objects.get(name)
    if not o:
        d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);bpy.data.collections['NC.Review'].objects.link(o)
        o['owner']='review';o['role']='additional fixed detail camera'
    o.location=location;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();o.data.lens=lens
    return o
`;
export const build = String.raw`
assert not bpy.data.objects.get('NC.Palace.Portal.Main.Pane'), 'Use a fresh preceding checkpoint; do not duplicate completed detail.'
for o in list(bpy.data.collections['NC.Palace'].objects):
    if o.name.startswith('NC.Palace.Prototype.'):remove(o.name)
remove('NC.Palace.Keep.EntranceLintel')
a=archpath(6,10,7,3)[1:-1]
lintel=extrude_xz('NC.Palace.Keep.EntranceVault','Palace',a+[(3,32),(-3,32)],14,17)
window('NC.Palace.Portal.Main','Palace',None,0,14,3,6,10,7,.6,.45,pane='DarkIron')
targets=[lintel,bpy.data.objects['NC.Palace.Keep.Front.West'],bpy.data.objects['NC.Palace.Keep.Front.East']]
window('NC.Palace.Window.Ceremonial','Palace',targets,0,14,16,7,14.5,10,.3,.35,pane='TealRoof')
for x in [-36,-29,-23,23,29,36]:
    target=bpy.data.objects['NC.Palace.Wing.'+('West' if x<0 else 'East')]
    window('NC.Palace.Window.WingFront.'+str(x),'Palace',target,x,16,6)
# Side elevations have two ordered bays of shallow pointed openings.
for side,x,angle in [('West',-40,-math.pi/2),('East',40,math.pi/2)]:
    target=bpy.data.objects['NC.Palace.Wing.'+side]
    for y in [23,32.5,42]:
        for z in [5.5,12.2]:
            window('NC.Palace.Window.WingSide.'+side+'.'+str(y)+'.'+str(z),'Palace',target,x,y,z,2.4,4.5,3.2,.2,.2,angle)
for x in [-32,-23,-12,12,23,32]:
    window('NC.Palace.Window.Gallery.'+str(x),'Palace',bpy.data.objects['NC.Palace.RearGallery'],x,57,6,3,6,4.5,.2,.25,math.pi)
window('NC.Palace.Portal.Rear','Palace',bpy.data.objects['NC.Palace.RearGallery'],0,57,3,4,7,5,.5,.35,math.pi,'DarkIron')
# Upper rear keep is visible above the lower gallery.
for x in [-10,0,10]:
    window('NC.Palace.Window.KeepRear.'+str(x),'Palace',bpy.data.objects['NC.Palace.Keep.Main'],x,52,22,3,6.8,4.8,.25,.25,math.pi,'TealRoof')
for side,x,angle in [('West',-16,-math.pi/2),('East',16,math.pi/2)]:
    for y in [26,40]:
        window('NC.Palace.Window.KeepSide.'+side+'.'+str(y),'Palace',bpy.data.objects['NC.Palace.Keep.Main'],x,y,23.5,3,6,4.3,.2,.25,angle,'TealRoof')
for x in [-12,-8,8,12]:
    bevel(box('NC.Palace.Pier.Front.'+str(x),'Palace',(x-.4,x+.4),(13.55,14.1),(3,31.5)),.08)
    for z in [3.6,14.2,31.6]:
        box('NC.Palace.Pier.Cap.'+str(x)+'.'+str(z),'Palace',(x-.6,x+.6),(13.3,14.15),(z-.25,z+.2))
        box('NC.Palace.Pier.Gold.'+str(x)+'.'+str(z),'Palace',(x-.62,x+.62),(13.28,14.16),(z+.2,z+.3),'RoyalGold')
for z in [3.6,14.2,31.6]:
    for side,xx in [('West',(-16.25,-4.2)),('East',(4.2,16.25))]:
        bevel(box('NC.Palace.Band.Front.'+side+str(z),'Palace',xx,(13.65,14.1),(z-.12,z+.13)),.05)
        box('NC.Palace.Band.Gold.'+side+str(z),'Palace',xx,(13.6,14.1),(z+.13,z+.2),'RoyalGold')
    for side,xx in [('West',(-16.3,-15.9)),('East',(15.9,16.3))]:
        box('NC.Palace.Band.Side.'+side+str(z),'Palace',xx,(17,52.2),(z-.12,z+.14))
    box('NC.Palace.Band.Rear.'+str(z),'Palace',(-16.3,16.3),(51.9,52.3),(z-.12,z+.14))
# Layered wing/rear roof eaves and base plinth.
for id,x,y,z in [('West',(-40.3,-16.7),(15.7,50.3),18),('East',(16.7,40.3),(15.7,50.3),18),('Gallery',(-40.3,40.3),(49.7,57.3),16)]:
    box('NC.Palace.Cornice.'+id,'Palace',x,y,(z-.45,z-.15))
    box('NC.Palace.Cornice.Gold.'+id,'Palace',(x[0]-.06,x[1]+.06),(y[0]-.06,y[1]+.06),(z-.15,z),'RoyalGold')
# Curved main dome rebuilt only in the palace owner.
remove('NC.Palace.Dome')
v=[];faces=[];rings=17;segments=64
for j in range(rings):
    phi=j*math.pi/(2*(rings-1));r=13*math.cos(phi);z=38+13*math.sin(phi)
    for i in range(segments):v.append((r*math.cos(i*math.tau/segments),34+r*math.sin(i*math.tau/segments),z))
for j in range(rings-1):
    for i in range(segments):a=j*segments+i;b=j*segments+(i+1)%segments;faces.append((a,b,b+segments,a+segments))
dome=mesh('NC.Palace.Dome','Palace',v,faces,'TealRoof','smooth hemispherical dome')
for p in dome.data.polygons:p.use_smooth=True
for i in range(16):
    a=i*math.tau/16
    pipe('NC.Palace.Dome.Rib.%02d'%i,'Palace',[(13.055*math.cos(j*math.pi/32)*math.cos(a),34+13.055*math.cos(j*math.pi/32)*math.sin(a),38+13.055*math.sin(j*math.pi/32)) for j in range(17)],.075)
for z,r,th in [(32.25,13.1,.12),(37.55,13.25,.15),(38,13.3,.13),(50.8,1.9,.14)]:
    torus_ring('NC.Palace.Drum.Cornice.'+str(z),'Palace',(0,34),r,z,th)
# Recessed, repeated drum apertures within the low drum.
for i in range(12):
    a=i*math.tau/12
    x=12.92*math.sin(a);y=34-12.92*math.cos(a)
    window('NC.Palace.Window.Drum.%02d'%i,'Palace',bpy.data.objects['NC.Palace.Drum'],x,y,33.2,1.5,3.2,2.2,.45,.16,a,'TealRoof')
for o in bpy.data.collections['NC.Palace'].objects:
    if o.type=='MESH':normals(o)
s.camera=review_camera('NC.CAM.EntranceDetail',(33,-40,29),(0,15,19),45)
s.render.resolution_x=1600;s.render.resolution_y=1200
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M03-r001/entrance-detail.png'
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'palace_objects':len(bpy.data.collections['NC.Palace'].objects),'window_panes':len([o for o in bpy.data.collections['NC.Palace'].objects if o.name.endswith('.Pane')]),'dome_ribs':16,'source':bpy.data.filepath}
`;
