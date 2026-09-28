// Ephemeral bpy snippets dispatched through the existing native Blender MCP.
export const primitives = String.raw`import bpy, json, math
from mathutils import Vector
s=bpy.context.scene
assert s.get('run_owner')=='neris-castle-2026-09-28-M01-r001'
spec=json.loads(s['layout_json'])
def own(o, name, owner, role, material='IvoryStone'):
    o.name=name
    o['owner']=owner.lower()
    o['role']=role
    o['stage']=s['milestone']
    c=bpy.data.collections['NC.'+owner]
    for previous in list(o.users_collection): previous.objects.unlink(o)
    c.objects.link(o)
    if o.type=='MESH':
        o.data.name=name+'.Mesh'
        o.data.materials.clear()
        o.data.materials.append(bpy.data.materials['NC.MAT.'+material])
    return o
def reset(owner):
    c=bpy.data.collections['NC.'+owner]
    for o in list(c.objects):
        assert o.get('owner')==owner.lower()
        data=o.data
        bpy.data.objects.remove(o,do_unlink=True)
        if isinstance(data,bpy.types.Mesh) and data.users==0: bpy.data.meshes.remove(data)
def box(name,owner,x,y,z,material='IvoryStone',role='massing'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=((x[0]+x[1])/2,(y[0]+y[1])/2,(z[0]+z[1])/2))
    o=bpy.context.object
    o.dimensions=(x[1]-x[0],y[1]-y[0],z[1]-z[0])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return own(o,name,owner,role,material)
def cone(name,owner,xy,r1,r2,z,material='TealRoof',vertices=32,role='massing'):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=z[1]-z[0],location=(xy[0],xy[1],(z[0]+z[1])/2))
    return own(bpy.context.object,name,owner,role,material)
def mesh(name,owner,verts,faces,material='IvoryStone',role='massing'):
    data=bpy.data.meshes.new(name+'.Mesh')
    data.from_pydata(verts,[],faces);data.update()
    o=bpy.data.objects.new(name,data);bpy.data.collections['NC.'+owner].objects.link(o)
    return own(o,name,owner,role,material)
def hip(name,owner,x,y,z,material='TealRoof'):
    dx=(x[1]-x[0]);dy=(y[1]-y[0]);inset=min(dx,dy)*0.36
    v=[(x[0],y[0],z[0]),(x[1],y[0],z[0]),(x[1],y[1],z[0]),(x[0],y[1],z[0]),(x[0]+inset,y[0]+inset,z[1]),(x[1]-inset,y[0]+inset,z[1]),(x[1]-inset,y[1]-inset,z[1]),(x[0]+inset,y[1]-inset,z[1])]
    return mesh(name,owner,v,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],material)

def remove(name):
    o=bpy.data.objects.get(name)
    if o:
        assert o.get('owner') in ['gatehouse','bridge','palace','fortifications','courtyard']
        d=o.data;bpy.data.objects.remove(o,do_unlink=True)
        if isinstance(d,bpy.types.Mesh) and d.users==0:bpy.data.meshes.remove(d)
def bevel(o,width=.08):
    m=o.modifiers.new('Stone edge bevel','BEVEL');m.width=width;m.segments=2
    m.limit_method='ANGLE'
    return o
def archpath(w,h,spring,bottom=0,steps=16):
    # Clockwise boundary: left foot, curved crown, right foot.
    half=w/2;left=[(-half,bottom),(-half,bottom+spring)]
    for i in range(1,steps+1):
        t=i/steps
        left.append((-half*(1-t*t),bottom+spring*(1-t)**2+2*(spring+(h-spring)*.70)*(1-t)*t+h*t*t))
    return left+ [(-x,z) for x,z in reversed(left[:-1])]
def extrude_xz(name,owner,points,y0,y1,material='IvoryStone'):
    n=len(points);v=[(x,y,z) for y in (y0,y1) for x,z in points]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,owner,v,f,material,'architectural detail')
def archband(name,owner,w,h,spring,bottom,band,y0,y1,material='IvoryStone',x=0):
    a=archpath(w,h,spring,bottom);b=archpath(w+2*band,h+band,spring,bottom)
    n=len(a);v=[(xx+x,y,zz) for y in (y0,y1) for line in (a,b) for xx,zz in line]
    f=[]
    for i in range(n-1):
        for j,k in [(0,n),(2*n,3*n),(0,2*n),(n,3*n)]:
            f.append((j+i,j+i+1,k+i+1,k+i))
    f.extend([(0,n,3*n,2*n),(n-1,2*n-1,4*n-1,3*n-1)])
    return mesh(name,owner,v,f,material,'pointed arch surround')
def pipe(name,owner,points,radius,material='RoyalGold'):
    c=bpy.data.curves.new(name+'.Curve','CURVE');c.dimensions='3D'
    c.bevel_depth=radius;c.bevel_resolution=1;c.resolution_u=1
    p=c.splines.new('POLY');p.points.add(len(points)-1)
    for v,co in zip(p.points,points):v.co=(*co,1)
    o=bpy.data.objects.new(name,c);bpy.data.collections['NC.'+owner].objects.link(o)
    own(o,name,owner,'raised trim',material);c.materials.append(bpy.data.materials['NC.MAT.'+material])
    return o
def torus_ring(name,owner,xy,r,z,thickness=.1,material='RoyalGold'):
    return pipe(name,owner,[(xy[0]+r*math.cos(i*math.tau/64),xy[1]+r*math.sin(i*math.tau/64),z) for i in range(65)],thickness,material)
def fingerprint(owner):
    import hashlib
    values=[]
    for o in sorted(bpy.data.collections['NC.'+owner].objects,key=lambda v:v.name):
        values.append((o.name,list(o.matrix_world),[(tuple(v.co)) for v in o.data.vertices] if o.type=='MESH' else []))
    return hashlib.sha256(repr(values).encode()).hexdigest()
`;
