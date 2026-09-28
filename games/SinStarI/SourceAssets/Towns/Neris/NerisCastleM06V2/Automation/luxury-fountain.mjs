// Royal tiered fountain within the original courtyard footprint.
export const build = String.raw`
assert not bpy.data.objects.get('NC.Courtyard.Fountain.Royal.UpperBowl')
remove('NC.Courtyard.Fountain.Crystal')
# A 24-sided, multi-ring cut crystal replaces the original six-sided spike.
lathe('NC.Courtyard.Fountain.Crystal','Courtyard',(0,-18),[(.22,3.0),(.50,3.4),(.98,4.25),(1.12,5.4),(1.02,6.45),(.69,7.4),(.33,8.8),(0,10)],'CrystalBlue',24)
lathe('NC.Courtyard.Fountain.Royal.UpperBowl','Courtyard',(0,-18),[(1.15,1.65),(1.12,2),(1.8,2.25),(2.05,2.65),(2.05,2.85),(1.85,2.85),(1.65,2.65),(.9,2.55)],'IvoryStone',64)
torus_ring('NC.Courtyard.Fountain.Royal.BowlLip','Courtyard',(0,-18),2.02,2.86,.075,'PolishedBrass')
torus_ring('NC.Courtyard.Fountain.Royal.BowlWaist','Courtyard',(0,-18),1.33,2.13,.06,'PolishedBrass')
cone('NC.Courtyard.Fountain.Royal.UpperWater','Courtyard',(0,-18),1.81,1.81,(2.71,2.74),'Moat',64)
for i in range(8):
    a=i*math.tau/8
    # Sculpted flower pedestals and smaller crown crystals ring the central bowl.
    x=2.55*math.cos(a);y=-18+2.55*math.sin(a)
    lathe('NC.Courtyard.Fountain.Royal.SatelliteBase.'+str(i),'Courtyard',(x,y),[(.38,1),(.40,1.2),(.25,1.55),(.37,1.7)],'IvoryStone',20)
    lathe('NC.Courtyard.Fountain.Royal.SatelliteCrystal.'+str(i),'Courtyard',(x,y),[(.08,1.70),(.24,2.0),(.20,2.5),(0,3.3)],'CrystalBlue',12)
    torus_ring('NC.Courtyard.Fountain.Royal.Socket.'+str(i),'Courtyard',(x,y),.27,1.78,.045,'PolishedBrass')
    # Parabolic water streams land in the basin, inside the retained fountain bounds.
    points=[]
    for j in range(33):
        t=j/32;r=2.1+(4.65-2.1)*t;zz=2.75+3.7*t*(1-t)-1.75*t
        points.append((r*math.cos(a),-18+r*math.sin(a),zz))
    pipe('NC.Courtyard.Fountain.Royal.Jet.'+str(i),'Courtyard',points,.055,'CrystalBlue')
    for k in range(3):
        torus_ring('NC.Courtyard.Fountain.Royal.Splash.'+str(i)+'.'+str(k),'Courtyard',(4.65*math.cos(a),-18+4.65*math.sin(a)),.15+k*.15,1.02+k*.008,.018,'IvoryStone')
    # Royal rim medallions and gold bands remain outside the walkable ring.
    for rr,zz in [(5.80,.75),(5.85,.30)]:
        if i==0:torus_ring('NC.Courtyard.Fountain.Royal.RimGold.'+str(rr),'Courtyard',(0,-18),rr,zz,.048,'PolishedBrass')
for o in bpy.data.collections['NC.Courtyard'].objects:
    if o.type=='MESH' and ('.Fountain.Royal.' in o.name or o.name=='NC.Courtyard.Fountain.Crystal'):normals(o)
s.camera=review_camera('NC.CAM.FountainLuxury',(12,-35,13),(0,-18,4.4),48)
s.render.resolution_x=1400;s.render.resolution_y=1200;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Star-I-Assets/unused.png' if False else 'D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/fountain-detail.png'
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'image':s.render.filepath,'fountain_peak':10,'central_crystal_sides':24,'jets':8,'site_unchanged':fingerprint('Site')==json.loads(s['luxury_baselines'])['Site']}
`;
