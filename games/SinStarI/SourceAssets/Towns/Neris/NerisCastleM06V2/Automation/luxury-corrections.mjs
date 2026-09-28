// Owner-local architectural corrections and native effect ownership.
export const build = String.raw`
# Focused corrections: complete outer wall arcades, seated balcony piers and curved window tracery.
assert not s.get('luxury_architecture_corrected')
for o in list(bpy.data.collections['NC.Fortifications'].objects):
    if o.name.startswith(('NC.Fortifications.Luxury.BlindArch.','NC.Fortifications.Luxury.Pilaster.')):remove(o.name)
# Local X is the wall run and local -Y is the outer face. Adjacent trim overlaps at piers.
wall_runs=[('SouthWest',-44.2,-14.0,0,-64,0),('SouthEast',14.0,44.2,0,-64,0),
    ('West',-56.2,56.2,-52,0,-math.pi/2),('East',-56.2,56.2,52,0,math.pi/2),
    ('North',-44.2,44.2,0,64,math.pi)]
wall_checks=[]
for side,a,b,x,y,angle in wall_runs:
    prefix='NC.Fortifications.Luxury.FullArcade.'+side
    count=round((b-a)/4.3);pitch=(b-a)/count
    for i in range(count):
        center=a+(i+.5)*pitch
        o=bevel(archband(prefix+'.Arch.'+str(i),'Fortifications',pitch-.28,10.47,8.45,.65,.15,-.42,-.10,x=center),.018)
        place(o,x,y,angle)
    for i in range(count+1):
        xx=a+i*pitch
        for suffix,w,za,zb,depth in [('Pier',.17,.60,9.15,.46),('Foot',.27,.35,.78,.53),('Capital',.26,8.97,9.22,.53)]:
            o=bevel(box(prefix+'.'+suffix+'.'+str(i),'Fortifications',(xx-w,xx+w),(-depth,-.1),(za,zb)),.024)
            place(o,x,y,angle)
    # Full-length base and stringcourse close the formerly short wall-to-gate joints.
    for suffix,za,zb,depth in [('Base',.08,.66,.48),('Plinth',1.8,2.15,.30),('Cornice',11.2,11.7,.30)]:
        o=box(prefix+'.'+suffix,'Fortifications',(a-.27,b+.27),(-depth,-.12),(za,zb));place(o,x,y,angle)
    wall_checks.append({'side':side,'bays':count,'run':[a,b],'foot_z':.35,'base_top':.66})
# Replace all balcony piers as one local family, including their flutes and gold rings.
for side,sign in [('West',-1),('East',1)]:
    prefix='NC.Palace.Luxury.Balcony.'+side+'.Pier.'
    for o in list(bpy.data.collections['NC.Palace'].objects):
        if o.name.startswith(prefix):remove(o.name)
    for x in [sign*22.5,sign*26,sign*32.7,sign*39]:
        lux_column(prefix+str(x),'Palace',x,15.6,3.25,9.43,.21)
# Replace angular glazing chevrons with nested smooth lancets matching the palace portal.
window_count=0
for owner in ['Palace','Gatehouse','Fortifications']:
    panes=[o for o in bpy.data.collections['NC.'+owner].objects if o.name.endswith('.Pane') and '.Ceremonial.' not in o.name and '.Portal.' not in o.name]
    for pane in panes:
        prefix=pane.name[:-5]
        old=[o for o in bpy.data.collections['NC.'+owner].objects if o.name.startswith(prefix+'.Tracery.')]
        if not old:continue
        for o in old:remove(o.name)
        w=max(v.co.x for v in pane.data.vertices)-min(v.co.x for v in pane.data.vertices)
        bottom=min(v.co.z for v in pane.data.vertices);h=max(v.co.z for v in pane.data.vertices)-bottom
        for i,(ww,hh,rr) in enumerate([(w*.83,h*.92,.042),(w*.68,h*.84,.024)]):
            o=pipe(prefix+'.CurvedTracery.'+str(i),owner,[(xx,-.105-i*.018,zz) for xx,zz in archpath(ww,hh,hh*.68,bottom+.045,32)],rr)
            o.matrix_world=pane.matrix_world.copy()
        window_count+=1
# The native animated water owner replaces these static Blender preview surfaces only at export.
for o in bpy.data.collections['NC.Courtyard'].objects:
    if '.Fountain.Royal.Jet.' in o.name or '.Fountain.Royal.Splash.' in o.name or o.name in ['NC.Courtyard.Fountain.Water','NC.Courtyard.Fountain.Royal.UpperWater']:
        o['native_effect']='fountain_water'
s['luxury_architecture_corrected']=True
s['architecture_correction_checks']=json.dumps({'wall_arcades':wall_checks,'balcony_pier_top':12.68,'balcony_deck_bottom':12.65,'curved_windows':window_count})
s.camera=review_camera('NC.CAM.WallCorrection',(-68,-115,24),(-27,-63,9),60)
s.render.resolution_x=1600;s.render.resolution_y=1100;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/wall-correction.png'
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.render.render(write_still=True)
result={'checks':json.loads(s['architecture_correction_checks']),'image':s.render.filepath}
`;
