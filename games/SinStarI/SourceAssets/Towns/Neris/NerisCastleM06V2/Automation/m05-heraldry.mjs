// One flat Neris emblem master, reused by vertical banners and horizontal flag.
export const build = String.raw`
assert not bpy.data.meshes.get('NC.Artwork.Emblem.RoyalGold'), 'Use a fresh preceding checkpoint; do not duplicate completed heraldry.'
def emblem_master():
    # Reconstructed flat compass-star/crystal artwork; no labels or motto from the reference board.
    groups={'RoyalGold':([],[]),'IvoryStone':([],[]),'CrystalBlue':([],[])}
    def poly(mat,points):
        vs,fs=groups[mat];n=len(vs);vs.extend([(x,y,z) for x,y,z in points]);fs.append(tuple(range(n,n+len(points))))
    star=[(0,1),(.13,.23),(.8,0),(.13,-.2),(0,-1),(-.13,-.2),(-.8,0),(-.13,.23)]
    poly('RoyalGold',[(x,0,z) for x,z in star])
    inner=[(x*.77,z*.82) for x,z in star]
    poly('IvoryStone',[(x,-.006,z) for x,z in inner])
    for i in range(64):
        a=i*math.tau/64;b=(i+1)*math.tau/64
        poly('RoyalGold',[(r*math.cos(t),.005,r*math.sin(t)) for r,t in [(.51,a),(.54,a),(.54,b),(.51,b)]])
    for i in range(8):
        a=math.pi/8+i*math.tau/8
        poly('RoyalGold',[(r*math.cos(t),.008,r*math.sin(t)) for r,t in [(.52,a-.03),(.72,a),(.52,a+.03)]])
    poly('RoyalGold',[(0,-.012,.47),(.2,-.012,0),(0,-.012,-.48),(-.2,-.012,0)])
    poly('CrystalBlue',[(0,-.02,.39),(.145,-.02,0),(0,-.02,-.4),(-.145,-.02,0)])
    for mat,(vs,fs) in groups.items():
        d=bpy.data.meshes.new('NC.Artwork.Emblem.'+mat);d.from_pydata(vs,[],fs);d.update()
        d.materials.append(bpy.data.materials['NC.MAT.'+mat])
        d['artwork_source']='original-neris-flag-design.png; flat four-point compass, ring, eight short rays, central blue crystal; no text'
def emblem(name,owner,x,y,z,scale,angle=0):
    for mat in ['RoyalGold','IvoryStone','CrystalBlue']:
        d=bpy.data.meshes['NC.Artwork.Emblem.'+mat]
        o=bpy.data.objects.new(name+'.'+mat,d);bpy.data.collections['NC.'+owner].objects.link(o)
        o['owner']=owner.lower();o['role']='shared Neris insignia';o['stage']='M05';o.location=(x,y,z);o.scale=(scale,scale,scale);o.rotation_euler.z=angle
def banner(name,owner,x,y,z,w,h,angle=0,horizontal=False):
    points=[(-w/2,z+h),(w/2,z+h),(w/2,z+(0 if horizontal else .65)),(0,z),(-w/2,z+(0 if horizontal else .65))]
    cloth=extrude_xz(name+'.Cloth',owner,points,-.035,.035,'BannerTeal');place(cloth,x,y,angle)
    uv=cloth.data.uv_layers.new(name='BannerUV')
    for loop in cloth.data.loops:
        co=cloth.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=((co.x+w/2)/w,(co.z-z)/h)
    # Inset ivory and gold perimeter stitches are actual raised trim, shared palette.
    for mat,inset,r in [('IvoryStone',.18,.065),('RoyalGold',.08,.038)]:
        line=[(-w/2+inset,-.065,z+h-.12),(w/2-inset,-.065,z+h-.12),(w/2-inset,-.065,z+(inset if horizontal else .75)),(0,-.065,z+inset),(-w/2+inset,-.065,z+(inset if horizontal else .75)),(-w/2+inset,-.065,z+h-.12)]
        o=pipe(name+'.Border.'+mat,owner,line,r,mat);place(o,x,y,angle)
    o=pipe(name+'.Crossbar',owner,[(-w/2-.3,.03,z+h+.13),(w/2+.3,.03,z+h+.13)],.085,'RoyalGold');place(o,x,y,angle)
    loc=Matrix.Translation(Vector((x,y,0)))@Matrix.Rotation(angle,4,'Z')@Vector((0,-.10,z+h*.51))
    emblem(name+'.Emblem',owner,*loc,min(w*.5,h*.38),angle)
    for sx in [-w/2,w/2]:
        o=pipe(name+'.Bracket.'+str(sx),owner,[(sx,.0,z+h),(sx,.5,z+h)],.06,'DarkIron');place(o,x,y,angle)
emblem_master()
# Major entry banners and rhythmic outer-wall placement.
for side,x in [('West',-19),('East',19)]:
    banner('NC.Gatehouse.Banner.'+side,'Gatehouse',x,-63.2,2.5,3.5,9.5)
for side,x in [('West',-32),('East',32)]:
    banner('NC.Fortifications.Banner.Front.'+side,'Fortifications',x,-64.28,2.7,3.6,7.7)
for side,sign in [('West',-1),('East',1)]:
    for yy in [-32,0,32]:
        banner('NC.Fortifications.Banner.Side.'+side+'.'+str(yy),'Fortifications',sign*52.28,yy,3,3.2,7.4,sign*math.pi/2)
    banner('NC.Fortifications.Banner.CornerSouth.'+side,'Fortifications',sign*48,-66.18,3,3.4,10.5)
    banner('NC.Fortifications.Banner.CornerNorth.'+side,'Fortifications',sign*48,66.18,3,3.4,10.5,math.pi)
for x in [-27,27]:
    banner('NC.Fortifications.Banner.Rear.'+str(x),'Fortifications',x,64.28,2.8,3.4,7.6,math.pi)
for x in [-10,10]:
    banner('NC.Palace.Banner.Front.'+str(x),'Palace',x,13.52,17,2.6,10.5)
# Same flat artwork on a horizontal standard, with an independently modeled pole.
banner('NC.Fortifications.Flag.Horizontal','Fortifications',35,62,16.3,6,3.5,math.pi,True)
cone('NC.Fortifications.Flag.Pole','Fortifications',(38.4,62),.1,.1,(12,21.3),'RoyalGold',12)
cone('NC.Fortifications.Flag.Finial','Fortifications',(38.4,62),.28,0,(21.3,22.6),'CrystalBlue',6)
# The ceremonial main window carries the same insignia source.
emblem('NC.Palace.Window.Insignia','Palace',0,13.74,23,2.25)
# Crown jewel above the actual gate: clear of the entire bridge sweep.
emblem('NC.Gatehouse.Arch.Insignia','Gatehouse',0,-63.67,17.1,.68)
s['heraldry_master']='NC.Artwork.Emblem.*; one flat reconstruction of supplied flag-board compass/star + blue crystal; no motto'
s['flag_backface']='Double-sided static cloth; reverse shows the same symmetric appliqué artwork. No cloth or wind animation claimed.'
result={'banners':len([o for o in s.objects if o.name.endswith('.Cloth')]),'emblem_instances':len([o for o in s.objects if o.get('role')=='shared Neris insignia'])//3}
`;
