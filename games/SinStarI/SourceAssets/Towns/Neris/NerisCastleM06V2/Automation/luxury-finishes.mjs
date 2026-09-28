// User-requested portal, lamps, natural foliage and royal bridge finishes.
export const build = String.raw`
import random
assert not bpy.data.objects.get('NC.Palace.Luxury.Door.Leaf.-1'), 'Entry pass already applied'
# Materials are local to this revision; original exports remain preserved.
def finish_material(name,base,roughness,metallic=0,emission=None):
    m=bpy.data.materials.new('NC.MAT.'+name);m.use_nodes=True;m.diffuse_color=(*base,1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*base,1)
    p.inputs['Roughness'].default_value=roughness;p.inputs['Metallic'].default_value=metallic
    if emission:
        p.inputs['Emission Color'].default_value=(*emission[0],1);p.inputs['Emission Strength'].default_value=emission[1]
    return m
finish_material('RoyalDoor',(.025,.105,.10),.34,.22)
finish_material('PolishedBrass',(.72,.43,.12),.23,.86)
finish_material('LampGlow',(1,.52,.12),.25,0,((1,.48,.10),4.0))
for o in s.objects:
    if '.Lantern.' in o.name and o.name.endswith('.Glass'):
        o.data.materials.clear();o.data.materials.append(bpy.data.materials['NC.MAT.LampGlow'])
        p=bpy.data.lights.new(o.name+'.Light','POINT');p.energy=65;p.color=(1,.57,.20);p.shadow_soft_size=.45
        light=bpy.data.objects.new(o.name+'.Light',p);bpy.data.collections['NC.'+o['owner'].title()].objects.link(light)
        light.location=o.location;light['owner']=o['owner'];light['role']='local lantern illumination'
# Raised twin leaves sit within the original portal, never across the approach.
for sign in [-1,1]:
    x=sign*1.48;name='NC.Palace.Luxury.Door.'
    panel=extrude_xz(name+'Leaf.'+str(sign),'Palace',[(xx+x,zz) for xx,zz in archpath(2.70,6.55,4.9,3.25)],14.39,14.54,'RoyalDoor')
    bevel(panel,.035)
    archband(name+'Border.'+str(sign),'Palace',2.48,6.28,4.70,3.4,.09,14.29,14.40,'PolishedBrass',x)
    archband(name+'InnerBorder.'+str(sign),'Palace',2.12,5.88,4.42,3.56,.038,14.27,14.32,'PolishedBrass',x)
    for zz in [4.5,6.9]:
        flower(name+'Rose.'+str(sign)+'.'+str(zz),'Palace',x,14.26,zz,.40)
    for zz in [3.65,5.45,7.45]:
        box(name+'Hinge.'+str(sign)+'.'+str(zz),'Palace',(sign*2.67-.12,sign*2.67+.12),(14.22,14.4),(zz-.13,zz+.13),'PolishedBrass')
    hx=sign*.43
    pipe(name+'Handle.'+str(sign),'Palace',[(hx+.18*math.cos(i*math.tau/32),14.16,5.7+.24*math.sin(i*math.tau/32)) for i in range(33)],.047,'PolishedBrass')
    flower(name+'HandlePlate.'+str(sign),'Palace',hx,14.28,5.8,.29)
    lancet(name+'Fanlight.'+str(sign),'Palace',sign*1.1,14.30,9.55,1.75,2.15,1.1,.035)
flower('NC.Palace.Luxury.Door.Crown','Palace',0,14.27,11.75,.39)
# Replace only smooth cypress crowns. Pots, paving, routes and collision radii stay put.
foliage_colors=[(.035,.13,.045),(.062,.205,.060),(.115,.275,.075)]
for i,color in enumerate(foliage_colors):finish_material('Cypress'+str(i),color,.88)
for tree in [o for o in list(bpy.data.collections['NC.Courtyard'].objects) if o.name.endswith('.Cypress')]:
    # Existing lathed vertices are in world space.
    points=[tree.matrix_world@v.co for v in tree.data.vertices];x=sum(p.x for p in points)/len(points);y=sum(p.y for p in points)/len(points)
    name=tree.name;remove(name);rng=random.Random(name)
    cone(name+'.Trunk','Courtyard',(x,y),.14,.035,(1.1,6.7),'BridgeTimber',10)
    buffers=[([],[]) for _ in range(3)]
    for level in range(13):
        z=1.55+level*.39;t=level/12;radius=.95*(1-t)**.6+.06
        count=max(3,9-int(t*5))
        for branch in range(count):
            a=branch*math.tau/count+level*2.399+rng.uniform(-.2,.2);rr=radius*rng.uniform(.63,1.04)
            center=Vector((x+rr*math.cos(a),y+rr*math.sin(a),z+rng.uniform(-.13,.14)))
            end=Vector((x+rr*1.13*math.cos(a),y+rr*1.13*math.sin(a),z+.36))
            if level<9:
                pipe(name+'.Branch.'+str(level)+'.'+str(branch),'Courtyard',[(x,y,z-.17),tuple(center),tuple(end)],.025,'BridgeTimber')
            # Tapered scale-leaf sprays make a ragged, layered evergreen silhouette.
            for spray in range(7):
                angle=a+(spray-3)*.29
                c=center+Vector((rng.uniform(-.13,.13),rng.uniform(-.13,.13),rng.uniform(-.16,.2)))
                length=rng.uniform(.32,.58)*(1-.35*t);width=rng.uniform(.09,.16)
                direction=Vector((math.cos(angle)*.48,math.sin(angle)*.48,.88));side=Vector((-math.sin(angle),math.cos(angle),0))*width
                for leaflet in range(3):
                    base=c+direction*(leaflet*length*.22);tip=base+direction*length;mid=base+direction*length*.32
                    verts,faces=buffers[(branch+spray+leaflet)%3];n=len(verts)
                    verts.extend([tuple(base),tuple(mid+side),tuple(tip),tuple(mid-side),tuple(mid+Vector((0,0,.075)))])
                    faces.extend([(n,n+1,n+4),(n+1,n+2,n+4),(n+2,n+3,n+4),(n+3,n,n+4),(n,n+3,n+2,n+1)])
    for i,(verts,faces) in enumerate(buffers):mesh(name+'.LeafSprays.'+str(i),'Courtyard',verts,faces,'Cypress'+str(i),'layered evergreen foliage')
# Bridge finishes remain under its original hinge, in local leaf coordinates.
pivot=bpy.data.objects['NC.Bridge.Pivot'];pivot.rotation_euler.x=0
for o in bpy.data.collections['NC.Bridge'].objects:
    if any(t in o.name for t in ['.UnderBand.','.UnderBrace.','.Band.','.Rivet.']):
        o.data.materials.clear();o.data.materials.append(bpy.data.materials['NC.MAT.PolishedBrass'])
def leaf_box(name,x,y,z,material='PolishedBrass'):
    o=box('NC.Bridge.Royal.'+name,'Bridge',x,y,z,material);o.parent=pivot;bevel(o,.014);return o
def leaf_pipe(name,points,r=.035):
    o=pipe('NC.Bridge.Royal.'+name,'Bridge',points,r,'PolishedBrass');o.parent=pivot;return o
for side in [-1,1]:
    x=side*4.75
    leaf_box('EdgeTop.'+str(side),(x-.14,x+.14),(-19.85,-.15),(.252,.28))
    leaf_box('EdgeUnder.'+str(side),(x-.16,x+.16),(-19.85,-.15),(-.335,-.28))
for yy in [-19.7,-.3]:
    leaf_box('EndTop.'+str(yy),(-4.75,4.75),(yy-.12,yy+.12),(.252,.28))
    leaf_box('EndUnder.'+str(yy),(-4.75,4.75),(yy-.16,yy+.16),(-.335,-.28))
for panel,cy in enumerate([-5,-15]):
    # Framed inset, rosette and scrolls readable as royal doors when raised.
    leaf_box('Inset.'+str(panel),(-3.7,3.7),(cy-3.0,cy+3.0),(-.31,-.218),'RoyalDoor')
    pts=[(-3.55,cy-2.85,-.35),(3.55,cy-2.85,-.35),(3.55,cy+2.85,-.35),(-3.55,cy+2.85,-.35),(-3.55,cy-2.85,-.35)]
    leaf_pipe('PanelFrame.'+str(panel),pts,.07)
    leaf_pipe('PanelInset.'+str(panel),[(x*.94,cy+(y-cy)*.91,z-.004) for x,y,z in pts],.025)
    # Eight-point heraldic relief rather than a structural obstruction.
    star=[]
    for i in range(16):
        a=i*math.tau/16;rr=1.37 if i%2==0 else .43
        star.append((rr*math.sin(a),cy+rr*math.cos(a),-.37))
    star.append(star[0]);leaf_pipe('Star.'+str(panel),star,.065)
    for sign in [-1,1]:
        leaf_pipe('Scroll.'+str(panel)+'.'+str(sign),[(sign*(1.4+i*.022)*math.cos(i*.09),cy+sign*1.65+.62*math.sin(i*.09),-.36) for i in range(61)],.033)
for side in [-1,1]:
    for yy in range(-19,0,2):
        o=cone('NC.Bridge.Royal.Stud.'+str(side)+'.'+str(yy),'Bridge',(side*4.75,yy),.065,.035,(-.39,-.335),'PolishedBrass',8);o.parent=pivot
for owner in ['Palace','Courtyard','Bridge']:
    for o in bpy.data.collections['NC.'+owner].objects:
        if o.type=='MESH' and ('.Luxury.Door.' in o.name or '.Cypress.' in o.name or '.Royal.' in o.name):normals(o)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=220
s['luxury_authorized_owners']='Palace, Fortifications, Gatehouse, Courtyard foliage, Bridge finish; shared materials and Review'
s.camera=review_camera('NC.CAM.PortalLuxury',(11,-6,12),(0,14,8),40)
s.render.resolution_x=1400;s.render.resolution_y=1100;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/portal-lamps.png'
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.render.render(write_still=True)
result={'file':bpy.data.filepath,'objects':len(s.objects),'image':s.render.filepath,'site_unchanged':fingerprint('Site')==json.loads(s['luxury_baselines'])['Site'],'bridge_hinge':list(pivot.location)}
`;
