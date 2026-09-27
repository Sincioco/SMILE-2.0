// Gatehouse/bridge owners only. Preserve palace fingerprint.
export const build = String.raw`
assert not bpy.data.objects.get('NC.Gatehouse.Arch.Vault'), 'Use a fresh preceding checkpoint; do not duplicate completed detail.'
remove('NC.Gatehouse.Arch.Massing')
a=archpath(12,15,11)[1:-1]
extrude_xz('NC.Gatehouse.Arch.Vault','Gatehouse',a+[(6,20),(-6,20)],-63.6,-50)
for face,y0,y1 in [('Front',-63.59,-63.30),('Rear',-50.3,-49.65)]:
    bevel(archband('NC.Gatehouse.Arch.'+face+'.Stone','Gatehouse',12,15,11,0,.65,y0,y1),.045)
    archband('NC.Gatehouse.Arch.'+face+'.Gold','Gatehouse',13.06,15.53,11,0,.12,y0-.015,y0+.02,'RoyalGold')
for side,x in [('West',-19),('East',19)]:
    for suffix,r0,r1,z,mat in [('Base',5.4,5.4,(0,1),'IvoryStone'),('Plinth',5.4,5,(1,1.5),'IvoryStone'),('Cornice',5,5.5,(19.2,19.65),'IvoryStone'),('Eaves',5.5,5.5,(19.65,20),'RoyalGold')]:
        cone('NC.Gatehouse.Tower.'+side+'.'+suffix,'Gatehouse',(x,-58),r0,r1,z,mat,48,'tower moulding')
    for z in [5,14]:
        torus_ring('NC.Gatehouse.Tower.'+side+'.String.'+str(z),'Gatehouse',(x,-58),5.06,z,.08)
for x in [-13.8,13.8]:
    bevel(box('NC.Gatehouse.Pier.'+str(x),'Gatehouse',(x-.45,x+.45),(-64.28,-63.8),(1.4,18.8)),.08)
box('NC.Gatehouse.Cornice.Stone','Gatehouse',(-15.4,15.4),(-63.55,-49.6),(19.1,19.65))
box('NC.Gatehouse.Cornice.Gold','Gatehouse',(-15.45,15.45),(-63.55,-49.55),(19.7,19.9),'RoyalGold')
pivot=bpy.data.objects['NC.Bridge.Pivot']
leaf=bpy.data.objects['NC.Bridge.Leaf.Main']
for v in leaf.data.vertices:
    if v.co.z>0:v.co.z=.18
# Real deck grooves within original top/thickness envelope.
for i in range(32):
    o=box('NC.Bridge.Plank.%02d'%i,'Bridge',(-4.98,4.98),(-20+i*.625+.025,-20+(i+1)*.625-.025),(.18,.25),'BridgeTimber','deck plank')
    o.parent=pivot
for side,x in [('West',-4.65),('East',4.65)]:
    o=box('NC.Bridge.Band.'+side,'Bridge',(x-.13,x+.13),(-19.8,-.3),(.251,.275),'DarkIron','attached reinforcement')
    o.parent=pivot
    for i,y in enumerate([-19,-15,-11,-7,-3]):
        o=cone('NC.Bridge.Rivet.'+side+str(i),'Bridge',(x,y),.095,.095,(.275,.31),'RoyalGold',8,'deck rivet');o.parent=pivot
# Hinges outside walkable deck, sharing the actual pivot.
for side,x in [('West',-5.1),('East',5.1)]:
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.18,depth=.2,location=(x,0,0),rotation=(0,math.pi/2,0))
    o=own(bpy.context.object,'NC.Bridge.Hinge.'+side,'Bridge','attached hinge','DarkIron');o.parent=pivot
bpy.context.view_layer.update()
assert fingerprint('Palace')==s['palace_m02_before']
s['bridge_detail']='32 real deck planks, two iron straps, ten fasteners, two pivot hinges; no pose-specific chains'
pivot.rotation_euler.x=0
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.region_3d.view_camera_zoom=15
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'objects':len(s.objects),'palace_unchanged':True,'bridge_children':len(pivot.children),'clear_deck_width':9.04}
`;
