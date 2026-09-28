// Palace-only royal window and portal refinement through existing Blender MCP.
export const build = String.raw`
# Palace facade refinement. Retain apertures and primary masses; add carved depth.
assert not bpy.data.objects.get('NC.Palace.Luxury.Main.Jamb.West.0'), 'Revision already applied'
def lux_column(name,owner,x,y,z,height,r):
    lathe(name,owner,(x,y),[(r*1.45,z),(r*1.45,z+.18),(r*1.1,z+.32),(r,z+.50),
        (r,z+height-.6),(r*1.15,z+height-.4),(r*1.5,z+height-.25),(r*1.5,z+height)],segments=20)
    for zz in [z+.34,z+height-.42]:
        torus_ring(name+'.Gold.'+str(zz),owner,(x,y),r*1.19,zz,.045)
    for k in range(5):
        a=math.pi+k*math.pi/4
        pipe(name+'.Flute.'+str(k),owner,[(x+r*1.005*math.cos(a),y+r*1.005*math.sin(a),z+.7),
            (x+r*1.005*math.cos(a),y+r*1.005*math.sin(a),z+height-.72)],.023,'IvoryStone')

def flower(name,owner,x,y,z,r):
    # A quatrefoil outline in the facade plane.
    points=[]
    for j in range(97):
        a=j*math.tau/96
        rr=r*(.78+.22*math.cos(4*a))
        points.append((x+rr*math.cos(a),y,z+rr*math.sin(a)))
    pipe(name,owner,points,.042,'RoyalGold')

def lancet(name,owner,x,y,z,w,h,spring,th=.06):
    points=archpath(w,h,spring,z,24)
    return pipe(name,owner,[(x+xx,y,zz) for xx,zz in points],th,'RoyalGold')

# Replace sparse stock divisions only; existing authentic heraldry remains in place.
for o in list(bpy.data.collections['NC.Palace'].objects):
    if o.name.startswith('NC.Palace.Window.Ceremonial.') and any(v in o.name for v in ['.Mullion','.Transom','.Tracery']):
        remove(o.name)
for i,(w,h,b,y) in enumerate([(7.25,14.65,.16,13.50),(8.65,15.45,.22,13.10),(9.35,15.82,.20,12.85)]):
    bevel(archband('NC.Palace.Luxury.Main.Arch.'+str(i),'Palace',w,h,10,16,b,y,y+.28),.025)
    archband('NC.Palace.Luxury.Main.ArchGold.'+str(i),'Palace',w+.08,h+.05,10,16,.047,y-.035,y,'RoyalGold')
for side,sign in [('West',-1),('East',1)]:
    for i,x in enumerate([4.45,5.10]):
        lux_column('NC.Palace.Luxury.Main.Jamb.'+side+'.'+str(i),'Palace',sign*x,12.95-i*.10,15.75,10.4,.23)
    # The pointed glazing subdivides into soaring nested lancets and cusps.
    lancet('NC.Palace.Luxury.Main.Tracery.'+side,'Palace',sign*1.72,13.90,16.25,2.95,11.85,8.15,.065)
    lancet('NC.Palace.Luxury.Main.InnerTracery.'+side,'Palace',sign*1.72,13.87,16.3,2.52,11.40,8.00,.033)
    for x in [sign*.72,sign*2.65]:
        pipe('NC.Palace.Luxury.Main.Mullion.'+str(x),'Palace',[(x,13.92,16.3),(x,13.92,24.25)],.043)
    flower('NC.Palace.Luxury.Main.Quatrefoil.'+side,'Palace',sign*1.72,13.78,26.40,.73)
    for zz in [18.5,20]:
        flower('NC.Palace.Luxury.Main.LowerCusp.'+side+'.'+str(zz),'Palace',sign*1.72,13.80,zz,.32)
lancet('NC.Palace.Luxury.Main.CrownTracery','Palace',0,13.86,25.7,2.7,3.9,1.8,.055)
flower('NC.Palace.Luxury.Main.CrownRose','Palace',0,13.77,28.1,.48)
# Enlarge the source-derived central insignia together, preserving its proportions.
for o in bpy.data.collections['NC.Palace'].objects:
    if o.name.startswith('NC.Palace.Window.Insignia.'):
        o.scale*=1.22
# Layered gabled hood and paired columns make the entrance read as a ceremonial portal.
for i in range(3):
    w=7.9+i*.44;h=11.1+i*.23;y=13.03-i*.16
    bevel(archband('NC.Palace.Luxury.Portal.Hood.'+str(i),'Palace',w,h,7,3,.17,y,y+.24),.024)
    archband('NC.Palace.Luxury.Portal.HoodGold.'+str(i),'Palace',w+.06,h+.06,7,3,.045,y-.025,y,'RoyalGold')
for sign in [-1,1]:
    for i in range(2):
        lux_column('NC.Palace.Luxury.Portal.Column.'+str(sign)+'.'+str(i),'Palace',
            sign*(4.35+i*.55),12.90-i*.08,3.15,6.85,.21)
# Keep the walkable portal and staircase unchanged.
for o in bpy.data.collections['NC.Palace'].objects:
    if o.name.startswith('NC.Palace.Luxury.') and o.type=='MESH':normals(o)
s.camera=bpy.data.objects['NC.CAM.EntranceDetail']
s.render.resolution_x=1400;s.render.resolution_y=1100;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/main-window-progress.png'
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.render.render(write_still=True)
result={'source':bpy.data.filepath,'palace_objects':len(bpy.data.collections['NC.Palace'].objects),
 'image':s.render.filepath,'unchanged_geometry':{owner:fingerprint(owner)==v for owner,v in json.loads(s['luxury_baselines']).items()}}
`;
