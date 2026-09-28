// Courtyard wall lanterns and isolated night review.
export const build=String.raw`def lantern(name,owner,x,y,z):
    cone(name+'.Glass',owner,(x,y),.27,.27,(z,z+1.2),'LampGlow',6)
    cone(name+'.Base',owner,(x,y),.36,.29,(z-.14,z),'DarkIron',6)
    cone(name+'.Hood',owner,(x,y),.42,0,(z+1.2,z+1.65),'RoyalGold',6)
    for i in range(6):
        a=i*math.tau/6
        pipe(name+'.Bar.'+str(i),owner,[(x+.28*math.cos(a),y+.28*math.sin(a),z),(x+.28*math.cos(a),y+.28*math.sin(a),z+1.2)],.028,'DarkIron')
    pipe(name+'.Bracket',owner,[(x,y+.75,z+.3),(x,y,z+.3)],.08,'DarkIron')

# User correction: wall-mounted courtyard lanterns, no freestanding posts.
for o in list(bpy.data.collections['NC.Courtyard'].objects):
    if o.name.startswith('NC.Courtyard.Lantern.Royal.'):remove(o.name)
positions=[(sign*48.3,y,5.5,-sign*math.pi/2) for sign in [-1,1] for y in [-42,-22,-2]]+[(x,-60.3,5.5,math.pi)for x in [-30,30]]
for i,(x,y,z,angle) in enumerate(positions):
    prefix='NC.Courtyard.Lantern.Wall.'+str(i)
    lantern(prefix,'Courtyard',0,0,z)
    plate=bevel(box(prefix+'.WallPlate','Courtyard',(-.28,.28),(.66,.83),(z+.02,z+.75),'PolishedBrass'),.07)
    for o in list(bpy.data.collections['NC.Courtyard'].objects):
        if o.name.startswith(prefix+'.'):place(o,x,y,angle)
    d=bpy.data.lights.new(prefix+'.Light','POINT');d.energy=300;d.color=(1,.63,.29);d.shadow_soft_size=.7
    o=bpy.data.objects.new(d.name,d);bpy.data.collections['NC.Courtyard'].objects.link(o);o.location=(x,y,z+.6);o['owner']='courtyard';o['role']='courtyard wall lantern illumination'
s['luxury_courtyard_lanterns']=8
# An independent night review uses the actual source geometry and its own compositor.
n=bpy.data.scenes.get('NC.Review.Night.M06-r003')
if n:bpy.data.scenes.remove(n)
n=s.copy();n.name='NC.Review.Night.M06-r003';n.use_fake_user=True;n.world=s.world.copy();n.world.name='NC.World.Night.M06-r003'
for node in n.world.node_tree.nodes:
    if node.type=='BACKGROUND':node.inputs['Color'].default_value=(.035,.065,.14,1);node.inputs['Strength'].default_value=.14
for c in list(n.collection.children):
    if c.name=='NC.Review':
        n.collection.children.unlink(c);local=c.copy();local.name='NC.Review.NightLights.M06-r003';n.collection.children.link(local)
        for light in list(local.objects):
            if light.type=='LIGHT':
                local.objects.unlink(light);copy=light.copy();copy.data=light.data.copy();local.objects.link(copy);copy.data.energy*=.025
n.compositing_node_group=n.compositing_node_group.copy();n.compositing_node_group.name='NC.Review.NightGlow'
for node in n.compositing_node_group.nodes:
    if node.type=='R_LAYERS':node.scene=n
n.camera=bpy.data.objects['NC.CAM.CourtyardNight'];n.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/courtyard-night.png'
bpy.ops.render.render(write_still=True,scene=n.name)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter'];bpy.context.window.scene=s
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'image':n.render.filepath,'wall_lanterns':8,'freestanding_lanterns':0}
`;
