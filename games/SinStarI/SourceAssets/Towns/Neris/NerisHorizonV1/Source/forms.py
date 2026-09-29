"""Small reusable mesh forms for the authored Horizon architecture and display ships."""
import math
import bpy
from mathutils import Vector

MATERIALS = {}


def material(name, rgb, metal=0.0, rough=.5, emission=0.0):
    value = bpy.data.materials.new(name)
    value.use_nodes = True
    node = value.node_tree.nodes.get('Principled BSDF')
    node.inputs['Base Color'].default_value = (*rgb, 1)
    node.inputs['Metallic'].default_value = metal
    node.inputs['Roughness'].default_value = rough
    node.inputs['Emission Color'].default_value = (*rgb, 1)
    node.inputs['Emission Strength'].default_value = emission
    MATERIALS[name] = value
    return value


def mesh(name, vertices, faces, finish):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATERIALS[finish])
    return obj


def box(name, position, size, finish):
    x, y, z = position
    w, d, h = (v / 2 for v in size)
    v = [(x+a*w, y+b*d, z+c*h) for a,b,c in
         [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
          (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    return mesh(name, v, [(0,3,2,1),(4,5,6,7),(0,1,5,4),
                          (1,2,6,5),(2,3,7,6),(3,0,4,7)], finish)


def beam(name, a, b, width, finish, depth=None):
    a, b = Vector(a), Vector(b)
    obj = box(name, (0,0,0), (width, depth or width, (b-a).length), finish)
    obj.location = (a+b)/2
    obj.rotation_euler = (b-a).to_track_quat('Z','Y').to_euler()
    return obj


def cone(name, point, radius, height, finish, top=0, sides=12):
    bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=radius,
        radius2=top, depth=height, location=point)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(MATERIALS[finish])
    return obj


def star(name, x, y, z, radius, vertical=False):
    vertices = [(x,y,z)]
    for i in range(8):
        angle = i * math.pi/4
        r = radius if i % 2 == 0 else radius*.19
        a,b = math.sin(angle)*r, math.cos(angle)*r
        vertices.append((x+a,y,z+b) if vertical else (x+a,y+b,z))
    return mesh(name, vertices, [(0,i+1,(i+1)%8+1) for i in range(8)], 'Gold')


def panel(name, corners, finish):
    return mesh(name, corners, [(0,1,2,3)], finish)


def dish(name, origin, radius, facing):
    # A real concave paraboloid, with rim and a feed suspended in front.
    axis = Vector(facing).normalized()
    basis = axis.to_track_quat('Z','Y')
    origin = Vector(origin)
    vertices = [origin]
    rings, segments = 5, 24
    for ring in range(1,rings+1):
        r = radius*ring/rings
        for i in range(segments):
            a = i*2*math.pi/segments
            vertices.append(origin + basis @ Vector((r*math.cos(a),r*math.sin(a),.32*r*r/radius)))
    faces = [(0,1+i,1+(i+1)%segments) for i in range(segments)]
    for ring in range(rings-1):
        for i in range(segments):
            a,b = 1+ring*segments+i,1+ring*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    mesh(name, vertices, faces, 'Ivory')
    feed = origin + axis*radius*.85
    cone(name+' Feed', feed, .6, 1.2, 'Metal')
    for i in range(segments):
        a,b = vertices[-segments+i], vertices[-segments+(i+1)%segments]
        beam(name+' Rim',a,b,.3,'Gold')
    for i in range(3):
        beam(name+' Support',vertices[-segments+i*8],feed,.23,'Metal')
    beam(name+' Pedestal',origin-Vector((0,0,5)),origin,1.1,'Metal')


def ship(name, x, y, scale=1, royal=False):
    """Static original display craft; all coordinates include landing-gear clearance."""
    before = set(bpy.context.scene.objects)
    def p(a,b,c): return (x+a*scale,y+b*scale,c*scale)
    def b(label, loc, size, mat):
        return box(name+' '+label,p(*loc),tuple(s*scale for s in size),mat)
    # Nose points toward the sea (+Y). Elliptical cross-sections form a readable hull.
    stations = [(-48,8,6),(-39,13,10),(-12,14,11),(22,11,9),(40,6,5),(48,1,1)]
    verts = []
    for yy,w,h in stations:
        for i in range(12):
            a = i*math.pi/6
            verts.append(p(math.cos(a)*w, yy, 13+math.sin(a)*h))
    faces = [tuple(reversed(range(12)))]
    for row in range(len(stations)-1):
        for i in range(12):
            faces.append((row*12+i,row*12+(i+1)%12,(row+1)*12+(i+1)%12,(row+1)*12+i))
    faces.append(tuple(range(len(verts)-12,len(verts))))
    mesh(name+' Fuselage',verts,faces,'Ivory')
    # Tinted canopy is a raised faceted insert; no intersecting coplanar skin.
    mesh(name+' Cockpit',[p(-7,21,20.8),p(7,21,20.8),p(4,38,17.9),p(-4,38,17.9),
                         p(-5,17,24),p(5,17,24)],
         [(0,1,2,3),(0,4,5,1),(4,0,3),(1,5,2)],'Glass Dark')
    for side in (-1,1):
        mesh(name+' Wing',[p(side*9,6,10),p(side*41,-25,9),p(side*39,-34,9),p(side*10,-27,10),
                           p(side*9,6,11.5),p(side*41,-25,10.5),p(side*39,-34,10.5),p(side*10,-27,11.5)],
             [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'Ivory')
        b('Wing Stripe',(side*25,-25,11.1),(22,1.3,.22),'Gold' if royal else 'Teal')
        engine = cone(name+' Engine',p(side*27,-21,12),5.8*scale,29*scale,'Ivory',top=5*scale)
        engine.rotation_euler[0] = math.pi/2
        inlet = cone(name+' Engine Inlet',p(side*27,-5.9,12),4.6*scale,.8*scale,'Metal',top=4.6*scale)
        inlet.rotation_euler[0] = math.pi/2
        glow = cone(name+' Exhaust',p(side*27,-36,12),3.6*scale,.8*scale,'Light',top=3.6*scale)
        glow.rotation_euler[0] = math.pi/2
        b('Hull Stripe',(side*12.7,-16,16),(1,35,1.8),'Gold' if royal else 'Teal')
        beam(name+' Gear',p(side*13,-18,8),p(side*17,-18,1.8),1.3*scale,'Metal')
        for yy in (-20,-16):
            wheel=cone(name+' Main Wheel',p(side*17,yy,1.5),1.5*scale,2.2*scale,'Metal',top=1.5*scale,sides=16)
            wheel.rotation_euler[1]=math.pi/2
            hub=cone(name+' Wheel Hub',p(side*18.15,yy,1.5),.6*scale,.15*scale,'Gold',top=.6*scale,sides=12)
            hub.rotation_euler[1]=math.pi/2
        mesh(name+' Tail',[p(side*5,-43,20),p(side*7,-47,36),p(side*8,-31,29),p(side*6,-25,20)],
             [(0,1,2,3)],'Ivory')
        for yy in (-26,-17,-8):
            b('Windows',(side*13.65,yy,17),(0.4,4,2),'Glass Dark')
    beam(name+' Nose Gear',p(0,32,8),p(0,32,1.5),1.4*scale,'Metal')
    for side in (-1,1):
        wheel=cone(name+' Nose Wheel',p(side*1.2,32,1.4),1.4*scale,1.4*scale,'Metal',top=1.4*scale,sides=16)
        wheel.rotation_euler[1]=math.pi/2
    for obj in set(bpy.context.scene.objects)-before:
        obj['fleet'] = 'Royal' if royal else 'Transport'
        obj['fleet_origin'] = [x,y,0]
    return {'name':name,'center':[x,y], 'width':82*scale,'length':96*scale,'height':36*scale}
