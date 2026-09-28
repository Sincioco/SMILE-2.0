// Courtyard lanterns and a separate night review environment.
export const build=String.raw`
assert not s.get('luxury_courtyard_lanterns')
# Center every flat wall banner in its new arcade bay, ahead of the stone relief.
for o in [o for o in bpy.data.collections['NC.Fortifications'].objects if o.name.endswith('.Cloth') and '.Banner.' in o.name and '.Corner' not in o.name]:
    prefix=o.name[:-6];p=o.location.copy()
    if '.Front.' in prefix:
        a,b=(-44.2,-14.0) if p.x<0 else (14.0,44.2);n=7;t=p.x;normal=Vector((0,-1,0));axis=Vector((1,0,0))
    elif '.Rear.' in prefix:
        a,b,n=-44.2,44.2,21;t=-p.x;normal=Vector((0,1,0));axis=Vector((-1,0,0))
    else:
        a,b,n=-56.2,56.2,26
        if '.West.' in prefix:t=-p.y;normal=Vector((-1,0,0));axis=Vector((0,-1,0))
        else:t=p.y;normal=Vector((1,0,0));axis=Vector((0,1,0))
    pitch=(b-a)/n;target=a+(max(0,min(n-1,int((t-a)/pitch)))+.5)*pitch
    delta=axis*(target-t)+normal*.40
    for part in bpy.data.collections['NC.Fortifications'].objects:
        if part.name.startswith(prefix+'.'):part.location+=delta
# Ten lantern posts preserve the main bridge-to-palace axis and existing planted bays.
positions=[(sign*44,y) for sign in [-1,1] for y in [-42,-22,-2]]+[(sign*10,y) for sign in [-1,1] for y in [-49,0]]
for i,(x,y) in enumerate(positions):
    prefix='NC.Courtyard.Lantern.Royal.'+str(i)
    lathe(prefix+'.Pedestal','Courtyard',(x,y),[(.48,.08),(.48,.30),(.34,.52),(.22,.75),(.16,3.35),(.31,3.55)],'PolishedBrass',20)
    lantern(prefix,'Courtyard',x,y,3.55)
    remove(prefix+'.Bracket')
    glass=bpy.data.objects[prefix+'.Glass'];glass.data.materials.clear();glass.data.materials.append(bpy.data.materials['NC.MAT.LampGlow'])
    light_data=bpy.data.lights.new(prefix+'.Light','POINT');light_data.energy=240;light_data.color=(1,.63,.29);light_data.shadow_soft_size=.65
    light=bpy.data.objects.new(prefix+'.Light',light_data);bpy.data.collections['NC.Courtyard'].objects.link(light);light.location=(x,y,4.2);light['owner']='courtyard';light['role']='courtyard lantern illumination'
# Broad concealed facade washes make carved depth readable in the night review.
for i,(location,target,power,color,size) in enumerate([((-24,3,13),(-7,20,23),3800,(1,.76,.46),14),((24,3,13),(7,20,23),3800,(1,.76,.46),14),((0,-18,7),(0,-18,1),350,(.10,.66,1),8)]):
    d=bpy.data.lights.new('NC.Courtyard.NightWash.'+str(i),'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(d.name,d);bpy.data.collections['NC.Courtyard'].objects.link(o);o.location=location;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();o['owner']='courtyard';o['role']='night architectural illumination'
# Preserve hue and cut facets while native halos supply the soft glow around crystals.
p=bpy.data.materials['NC.MAT.CrystalBlue'].node_tree.nodes.get('Principled BSDF');p.inputs['Emission Strength'].default_value=.8
water=bpy.data.materials.get('NC.MAT.FountainStream')
if not water:
    water=bpy.data.materials['NC.MAT.CrystalBlue'].copy();water.name='NC.MAT.FountainStream'
    p=water.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.035,.34,.56,1);p.inputs['Emission Strength'].default_value=.13;p.inputs['Roughness'].default_value=.19
for o in bpy.data.collections['NC.Courtyard'].objects:
    if '.Fountain.Royal.Jet.' in o.name:o.data.materials.clear();o.data.materials.append(water)
    if o.name=='NC.Courtyard.Fountain.Surface' or '.Fountain.Royal.Jet.' in o.name or '.Fountain.Royal.Splash.' in o.name or o.name=='NC.Courtyard.Fountain.Royal.UpperWater':o['native_effect']='fountain_water'
s['luxury_courtyard_lanterns']=10
s.camera=review_camera('NC.CAM.CourtyardNight',(77,-113,76),(0,-3,20),48)
s.render.resolution_x=1600;s.render.resolution_y=1100;s.render.resolution_percentage=100
# Separate review scene owns the night environment; source daylight settings are untouched.
night=s.copy();night.name='NC.Review.Night.M06-r003';night.world=s.world.copy();night.world.name='NC.World.Night.M06-r003'
for node in night.world.node_tree.nodes:
    if node.type=='BACKGROUND':node.inputs['Color'].default_value=(.035,.065,.14,1);node.inputs['Strength'].default_value=.14
for o in s.objects:
    if o.type=='LIGHT' and o.get('owner')=='review':
        pass
# Copy global review lights into the new scene and dim them without touching source data.
for collection in list(night.collection.children):
    if collection.name=='NC.Review':
        night.collection.children.unlink(collection);local=collection.copy();local.name='NC.Review.NightLights.M06-r003';night.collection.children.link(local)
        for o in list(local.objects):
            if o.type=='LIGHT':
                local.objects.unlink(o);c=o.copy();c.data=o.data.copy();local.objects.link(c);c.data.energy*=.025
night.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/courtyard-night.png'
bpy.ops.render.render(write_still=True,scene=night.name)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.context.window.scene=s
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'image':night.render.filepath,'new_lanterns':10,'world':'separate night review scene','source':bpy.data.filepath}
`;
