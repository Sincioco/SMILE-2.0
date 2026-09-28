// Flush courtyard paving route and concealed perimeter illumination.
export const build=String.raw`
assert not bpy.data.objects.get('NC.Courtyard.ProcessionalPath')
mat=bpy.data.materials.new('NC.MAT.PathStone');mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.36,.405,.38,1);p.inputs['Roughness'].default_value=.76
# A solid paving insert is cut flush into the existing paving, not raised above it.
path=box('NC.Courtyard.ProcessionalPath','Courtyard',(-4,4),(-64,0),(-.12,.02),'PathStone')
ring=cone('NC.Courtyard.PathOuterCut','Courtyard',(0,-18),9.6,9.6,(-.12,.02),'PathStone',128)
bpy.context.view_layer.objects.active=path;m=path.modifiers.new('Path circle union','BOOLEAN');m.operation='UNION';m.solver='EXACT';m.object=ring;bpy.ops.object.modifier_apply(modifier=m.name);remove(ring.name)
hole=cone('NC.Courtyard.PathInnerCut','Courtyard',(0,-18),6.6,6.6,(-1,1),'PathStone',128);cut(path,hole)
# Give existing individual paving quads thickness before subtracting the flush insert.
tiles=bpy.data.objects['NC.Courtyard.Paving.Tiles'];vs=[];fs=[]
for poly in tiles.data.polygons:
    quad=[tiles.matrix_world@tiles.data.vertices[i].co for i in poly.vertices];n=len(vs)
    vs.extend([tuple(v) for v in quad]+[(v.x,v.y,-.12)for v in quad]);fs.extend([(n,n+1,n+2,n+3),(n+7,n+6,n+5,n+4)])
    for i in range(4):fs.append((n+i,n+4+i,n+4+(i+1)%4,n+(i+1)%4))
remove(tiles.name);tiles=mesh('NC.Courtyard.Paving.Tiles','Courtyard',vs,fs)
cutter=path.copy();cutter.data=path.data.copy();bpy.data.collections['NC.Courtyard'].objects.link(cutter);cutter.name='NC.Courtyard.PathPavingCut';cutter['owner']='courtyard'
for v in cutter.data.vertices:v.co.z+=.2 if v.co.z>0 else -.2
cut(tiles,cutter)
# Thin flush brass borders are recessed into the path sides, clear of the fountain.
for sign in [-1,1]:
    for ya,yb in [(-64,-18-math.sqrt(9.6**2-4**2)),(-18+math.sqrt(9.6**2-4**2),0)]:
        box('NC.Courtyard.PathBorder.'+str(sign)+'.'+str(ya),'Courtyard',(sign*4-.04,sign*4+.04),(ya,yb),(-.02,.023),'PolishedBrass')
for radius in [6.60,9.60]:
    # Outer circle omits the two route intersections; the inner ring remains continuous.
    pts=[];part=0
    for i in range(257):
        a=i*math.tau/256;x=radius*math.cos(a);y=-18+radius*math.sin(a)
        allowed=radius<7 or abs(x)>=4
        if allowed:pts.append((x,y,.02))
        elif len(pts)>1:
            pipe('NC.Courtyard.PathCircle.'+str(radius)+'.'+str(part),'Courtyard',pts,.032,'PolishedBrass');pts=[];part+=1
        else:pts=[]
    if len(pts)>1:pipe('NC.Courtyard.PathCircle.'+str(radius)+'.'+str(part),'Courtyard',pts,.032,'PolishedBrass')
# Complete perimeter and tower uplighting, without adding visible lamp posts.
for i,(location,target,size,power) in enumerate([((-34,-73,3),(-30,-64,12),24,4800),((34,-73,3),(30,-64,12),24,4800),((-62,-30,3),(-52,-28,14),32,6200),((-62,30,3),(-52,28,14),32,6200),((62,-30,3),(52,-28,14),32,6200),((62,30,3),(52,28,14),32,6200),((-27,74,3),(-24,64,14),28,5200),((27,74,3),(24,64,14),28,5200),((-49,34,8),(-17,33,30),20,5200),((49,34,8),(17,33,30),20,5200),((0,68,15),(0,44,34),22,6200)]):
    d=bpy.data.lights.new('NC.Courtyard.PerimeterWash.'+str(i),'AREA');d.energy=power;d.color=(1,.73,.40);d.shape='DISK';d.size=size
    o=bpy.data.objects.new(d.name,d);bpy.data.collections['NC.Courtyard'].objects.link(o);o.location=location;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();o['owner']='courtyard';o['role']='concealed night architectural wash'
n=bpy.data.scenes.get('NC.Review.Night.M06-r003')
if n:
    for o in n.objects:
        if o.type=='LIGHT' and o.get('owner')=='review':o.data.energy*=4
s.camera=bpy.data.objects['NC.CAM.Top'];s.render.resolution_x=1600;s.render.resolution_y=1200;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/courtyard-path.png'
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
result={'image':s.render.filepath,'path_top':.02,'paving_top':.02,'path_width':8,'fountain_clearance':6.6-5.85,'perimeter_washes':11}
`;
