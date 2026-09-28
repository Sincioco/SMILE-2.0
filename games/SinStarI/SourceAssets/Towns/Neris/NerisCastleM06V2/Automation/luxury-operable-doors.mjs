// Hinged palace entry and reviewed final botanical/bridge cleanup.
export const build = String.raw`
assert not bpy.data.objects.get('NC.Palace.Door.Pivot.West')
# Clean ornamental bridge panels; authentic shared Neris artwork faces outward when raised.
for o in list(bpy.data.collections['NC.Bridge'].objects):
    if '.UnderBrace.' in o.name or '.Royal.Star.' in o.name:remove(o.name)
for panel,cy in enumerate([-5,-15]):
    for mat in ['RoyalGold','IvoryStone','CrystalBlue']:
        d=bpy.data.meshes['NC.Artwork.Emblem.'+mat]
        o=bpy.data.objects.new('NC.Bridge.Royal.Emblem.'+str(panel)+'.'+mat,d)
        bpy.data.collections['NC.Bridge'].objects.link(o);o['owner']='bridge';o['role']='shared Neris insignia';o['stage']='M06-r003'
        o.parent=bpy.data.objects['NC.Bridge.Pivot'];o.location=(0,cy,-.405);o.rotation_euler.x=math.pi/2;o.scale=(1.7,1.7,1.7)
# Fill the open center of each tree with a finer offset layer of the same botanical sprays.
for o in [v for v in list(bpy.data.collections['NC.Courtyard'].objects) if '.Cypress.LeafSprays.' in v.name]:
    verts=[v.co.copy() for v in o.data.vertices];faces=[tuple(p.vertices) for p in o.data.polygons]
    tree=o.name.split('.LeafSprays.')[0];trunk=bpy.data.objects[tree+'.Trunk'];center=trunk.location
    n=len(verts);angle=1.1;cx=center.x;cy=center.y
    extra=[]
    for v in verts:
        xx=(v.x-cx)*.60;yy=(v.y-cy)*.60
        extra.append((cx+xx*math.cos(angle)-yy*math.sin(angle),cy+xx*math.sin(angle)+yy*math.cos(angle),1.35+(v.z-1.35)*.985))
    d=bpy.data.meshes.new(o.data.name+'.Dense');d.from_pydata([tuple(v) for v in verts]+extra,[],faces+[tuple(i+n for i in f) for f in faces]);d.update()
    for m in o.data.materials:d.materials.append(m)
    old=o.data;o.data=d
    if old.users==0:bpy.data.meshes.remove(old)
# Remove the fixed lower door plane and divide the actual doorway into two leaves.
remove('NC.Palace.Portal.Main.Pane');remove('NC.Palace.Portal.Main.Transom');remove('NC.Palace.Portal.Main.Mullion')
for o in list(bpy.data.collections['NC.Palace'].objects):
    if o.name.startswith('NC.Palace.Portal.Main.Tracery'):remove(o.name)
upper=[(-3,9.82)]+archpath(6,10,7,3)[1:-1]+[(3,9.82)]
extrude_xz('NC.Palace.Portal.Main.FixedFanlight','Palace',upper,14.575,14.6,'RoyalDoor')
box('NC.Palace.Portal.Main.Transom','Palace',(-3,3),(14.32,14.56),(9.75,9.84),'PolishedBrass')
for side,sign in [('West',-1),('East',1)]:
    pivot=bpy.data.objects.new('NC.Palace.Door.Pivot.'+side,None);bpy.data.collections['NC.Palace'].objects.link(pivot)
    pivot.location=(sign*3,14.55,3);pivot['owner']='palace';pivot['role']='palace door hinge';pivot['motion_owner']='Door'+side
    panel=box('NC.Palace.Door.Backing.'+side,'Palace',(-3,-.01) if sign<0 else (.01,3),(14.405,14.60),(3.03,9.78),'RoyalDoor')
    chosen=[panel]
    for o in list(bpy.data.collections['NC.Palace'].objects):
        n=o.name
        if n.startswith('NC.Palace.Luxury.Door.') and not any(v in n for v in ['Fanlight','Crown']):
            if ('.'+str(sign)) in n:chosen.append(o)
    # The sign is an explicit token, never a substring of another leaf's name.
    for o in chosen:
        old=o.matrix_world.copy();o.parent=pivot;o.matrix_world=old;o['motion_owner']='Door'+side
# Clear a bounded entrance recess in the existing keep. Its outside mass stays fixed.
cutter=box('NC.Palace.Door.CorridorCut','Palace',(-3,3),(16.8,24),(3,10))
cut(bpy.data.objects['NC.Palace.Keep.Main'],cutter)
box('NC.Palace.Door.VestibuleFloor','Palace',(-3,3),(14.5,24),(2.95,3),'IvoryStone')
box('NC.Palace.Door.VestibuleBack','Palace',(-3,3),(23.95,24),(3,10),'DarkIron')
for o in bpy.data.collections['NC.Palace'].objects:
    if o.type=='MESH' and ('.Door.' in o.name or 'FixedFanlight' in o.name):normals(o)
bpy.context.view_layer.update()
# Source open-pose evidence; restore closed authoring pose before export.
for side,angle in [('West',95),('East',-95)]:bpy.data.objects['NC.Palace.Door.Pivot.'+side].rotation_euler.z=math.radians(angle)
s.camera=bpy.data.objects['NC.CAM.PortalLuxury'];s.render.resolution_x=1400;s.render.resolution_y=1100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/portal-open.png'
bpy.ops.render.render(write_still=True)
for side in ['West','East']:bpy.data.objects['NC.Palace.Door.Pivot.'+side].rotation_euler.z=0
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'doors':{side:len([o for o in s.objects if o.get('motion_owner')=='Door'+side]) for side in ['West','East']},'image':s.render.filepath}
`;
