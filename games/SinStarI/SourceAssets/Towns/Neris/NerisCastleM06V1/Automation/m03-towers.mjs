// Palace and fortification tower geometry; existing category heights preserved.
export const helpers = String.raw`
def lathe(name,owner,xy,profile,material='IvoryStone',segments=48):
    vs=[];faces=[]
    for r,z in profile:
        vs.extend([(xy[0]+r*math.cos(i*math.tau/segments),xy[1]+r*math.sin(i*math.tau/segments),z) for i in range(segments)])
    for j in range(len(profile)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments;faces.append((a,b,b+segments,a+segments))
    faces.extend([tuple(reversed(range(segments))),tuple((len(profile)-1)*segments+i for i in range(segments))])
    o=mesh(name,owner,vs,faces,material,'profiled architectural form')
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    return o
def tower_detail(prefix,owner,xy,r,bottom,top,roofTop,crystalTop):
    shaft=bpy.data.objects[prefix+'.Shaft']
    for p in shaft.data.polygons:p.use_smooth=len(p.vertices)==4
    lathe(prefix+'.Base',owner,xy,[(r*1.05,bottom),(r*1.05,bottom+.55),(r,bottom+1.05)])
    lathe(prefix+'.Capital',owner,xy,[(r,top-1.6),(r*1.06,top-1.05),(r*1.10,top-.7),(r*1.10,top-.25)])
    torus_ring(prefix+'.Capital.Gold',owner,xy,r*1.10,top-.24,.10)
    for z in [bottom+1.1,top-8,top-1.65]:
        torus_ring(prefix+'.String.'+str(z),owner,xy,r+.03,z,.07)
    for i in range(4):
        a=i*math.pi/2;x=xy[0]+r*math.sin(a);y=xy[1]-r*math.cos(a)
        w=2.2 if r>4 else 1.8;h=5 if r>4 else 5.5
        window(prefix+'.Window.'+str(i),owner,shaft,x,y,top-h-2.1,w,h,h*.72,.42,.2,a)
    # A restrained bell profile gives a readable transition into the roof.
    remove(prefix+'.Roof')
    roof=[(r*1.12,top),(r*1.04,top+.5),(r*.87,top+1.2),(r*.66,top+(roofTop-top)*.36),(r*.4,top+(roofTop-top)*.62),(.27,roofTop-.5),(.08,roofTop)]
    lathe(prefix+'.Roof',owner,xy,roof,'TealRoof')
    torus_ring(prefix+'.Roof.Eave',owner,xy,r*1.12,top+.03,.075)
    for i in range(8):
        a=i*math.tau/8
        pipe(prefix+'.Roof.Rib.'+str(i),owner,[(xy[0]+(rr+.045)*math.cos(a),xy[1]+(rr+.045)*math.sin(a),zz+.045) for rr,zz in roof],.05)
    remove(prefix+'.Crystal')
    cr=.7 if r>4 else .55
    lathe(prefix+'.Crystal',owner,xy,[(cr*.25,roofTop),(cr,roofTop+(crystalTop-roofTop)*.3),(cr*.75,roofTop+(crystalTop-roofTop)*.60),(0,crystalTop)],'CrystalBlue',6)
    cone(prefix+'.Crystal.Socket',owner,xy,cr*.75,cr*.75,(roofTop,roofTop+.35),'RoyalGold',12,'crystal socket')
`;
export const build = String.raw`
assert not bpy.data.objects.get('NC.Palace.Dome.Rib.0'), 'Use a fresh preceding checkpoint; do not duplicate completed detail.'
for id,xy in [('SW',(-48,-60)),('SE',(48,-60)),('NW',(-48,60)),('NE',(48,60))]:
    tower_detail('NC.Fortifications.Corner.'+id,'Fortifications',xy,6,0,24,33,37)
for id,xy,top in [('Front.West',(-18,18),31),('Front.East',(18,18),31),('Rear.West',(-18,48),37),('Rear.East',(18,48),37)]:
    tower_detail('NC.Palace.Turret.'+id,'Palace',xy,3,3,top,top+9,top+12)
# Brass handles and segmented lower door panels distinguish the main portal from a window.
for x in [-1.4,1.4]:
    box('NC.Palace.Portal.Panel.'+str(x),'Palace',(x-1.15,x+1.15),(14.47,14.5),(3.35,7.15),'TealRoof')
    for xx in [x-.92,x+.92]:
        box('NC.Palace.Portal.PanelTrim.'+str(xx),'Palace',(xx-.035,xx+.035),(14.43,14.47),(3.55,6.95),'RoyalGold')
for x in [-.35,.35]:
    pipe('NC.Palace.Portal.Handle.'+str(x),'Palace',[(x+.14*math.cos(i*math.tau/16),14.35,5.5+.2*math.sin(i*math.tau/16)) for i in range(17)],.045,'RoyalGold')
s.camera=review_camera('NC.CAM.TowerDetail',(68,-86,28),(48,-60,22),58)
s.render.resolution_x=1408;s.render.resolution_y=1100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M03-r001/tower-detail.png'
bpy.ops.render.render(write_still=True)
s.camera=review_camera('NC.CAM.DomeDetail',(52,-22,69),(0,34,44),60)
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M03-r001/dome-detail.png'
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'objects':len(s.objects),'independent_owners_unchanged':{owner:fingerprint(owner)==value for owner,value in json.loads(s['m03_independent_fingerprints']).items()}}
`;
