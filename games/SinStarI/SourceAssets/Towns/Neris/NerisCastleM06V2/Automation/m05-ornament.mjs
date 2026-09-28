// Final facade fittings checked against the user-specified 4K front reference.
export const build = String.raw`
assert not bpy.data.objects.get('NC.Palace.Portal.Main.OuterReveal'), 'Use a fresh preceding checkpoint; do not duplicate completed detail.'
for o in s.objects:
    if o.name.startswith('NC.Gatehouse.Arch.Insignia.'):o.location.y=-63.62
# Layered reveals and keystones bring the main pointed arch closer to the 4K concept.
bevel(archband('NC.Palace.Window.Ceremonial.OuterReveal','Palace',7.95,15.05,10,16,.3,13.43,13.9),.055)
archband('NC.Palace.Window.Ceremonial.OuterGold','Palace',8.55,15.35,10,16,.07,13.40,13.46,'RoyalGold')
bevel(archband('NC.Palace.Portal.Main.OuterReveal','Palace',7,10.6,7,3,.3,13.35,13.82),.055)
archband('NC.Palace.Portal.Main.OuterGold','Palace',7.6,10.9,7,3,.07,13.32,13.39,'RoyalGold')
lathe('NC.Palace.Window.Crest','Palace',(0,13.5),[(.12,31.7),(.35,32.3),(.22,33.1),(0,33.7)],'CrystalBlue',6)
lathe('NC.Palace.Portal.Crest','Palace',(0,13.4),[(.06,14),(.26,14.4),(0,15.35)],'CrystalBlue',6)
bevel(archband('NC.Gatehouse.Arch.Front.OuterReveal','Gatehouse',13.7,15.85,11,0,.36,-63.59,-63.32),.04)
for x in [-10.4,10.4]:
    target=bpy.data.objects['NC.Gatehouse.Jamb.'+('West' if x<0 else 'East')]
    window('NC.Gatehouse.Window.Front.'+str(x),'Gatehouse',target,x,-64,7.2,1.55,5,3.7,.25,.2)
def lantern(name,owner,x,y,z):
    cone(name+'.Glass',owner,(x,y),.27,.27,(z,z+1.2),'WarmWindow',6)
    cone(name+'.Base',owner,(x,y),.36,.29,(z-.14,z),'DarkIron',6)
    cone(name+'.Hood',owner,(x,y),.42,0,(z+1.2,z+1.65),'RoyalGold',6)
    for i in range(6):
        a=i*math.tau/6
        pipe(name+'.Bar.'+str(i),owner,[(x+.28*math.cos(a),y+.28*math.sin(a),z),(x+.28*math.cos(a),y+.28*math.sin(a),z+1.2)],.028,'DarkIron')
    pipe(name+'.Bracket',owner,[(x,y+.75,z+.3),(x,y,z+.3)],.08,'DarkIron')
for x in [-7.65,7.65]:lantern('NC.Gatehouse.Lantern.'+str(x),'Gatehouse',x,-64.65,6.3)
for x in [-5.2,5.2]:lantern('NC.Palace.Lantern.'+str(x),'Palace',x,13.15,6)
# Small corbels repeat under eaves rather than adding new tower masses.
for side,sign in [('West',-1),('East',1)]:
    for yy in range(-48,49,8):
        x=sign*52.23
        box('NC.Fortifications.Corbel.'+side+'.'+str(yy),'Fortifications',(x-.16,x+.16),(yy-.22,yy+.22),(10.55,11.2),'RoyalGold')
for side,yy in [('Front',-64.22),('Back',64.22)]:
    for x in [-40,-32,-24,24,32,40]:
        box('NC.Fortifications.Corbel.'+side+'.'+str(x),'Fortifications',(x-.22,x+.22),(yy-.16,yy+.16),(10.55,11.2),'RoyalGold')
for x in [-37,-33,-29,-25,-21,21,25,29,33,37]:
    box('NC.Palace.Wing.Corbel.'+str(x),'Palace',(x-.2,x+.2),(15.65,16),(17.15,17.6),'RoyalGold')
for i in range(16):
    a=i*math.tau/16;x=13.05*math.cos(a);y=34+13.05*math.sin(a)
    o=box('NC.Palace.Drum.Corbel.'+str(i),'Palace',(x-.15,x+.15),(y-.15,y+.15),(36.95,37.6),'RoyalGold')
# The original neutral review illumination is retained for honest comparison.
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
s.render.resolution_x=2560;s.render.resolution_y=1440
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M05-r001/hero-three-quarter.png'
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.EntranceDetail'];s.render.resolution_x=1600;s.render.resolution_y=1200
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M05-r001/entrance-detail.png'
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.GateDetail'];s.render.resolution_x=2048;s.render.resolution_y=1152
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M05-r001/gate-detail.png'
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'source':bpy.data.filepath,'objects':len(s.objects),'reference_reinspected':'Neris - Castle - Front_4K.png','emission':0}
`;
