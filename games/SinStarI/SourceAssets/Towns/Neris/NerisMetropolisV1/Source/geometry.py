"""Small material-batched architectural meshes shared by Blender and Studio exports.

Coordinates are metres, Z up. Each design is a reusable mesh, never one object
per window. Facade detail survives the native flat-PBR publication path.
"""
import math
from pathlib import Path
import bpy
from mathutils import Vector
from facades import STYLES


PALETTE = {
    'Glass': ((.075, .27, .49, 1), .62, .17, 0),
    'Silver': ((.52, .59, .62, 1), .8, .3, 0),
    'Stone': ((.54, .49, .38, 1), .1, .65, 0),
    'Dark': ((.022, .037, .049, 1), .5, .38, 0),
    'Gold': ((.94, .62, .22, 1), .5, .3, 2),
    'White': ((.72, .9, 1, 1), .2, .3, 1.7),
    'Magenta': ((.58, .05, .4, 1), .3, .3, 2),
    'Green': ((.08, .23, .07, 1), 0, .9, 0),
    'Leaf': ((.17, .34, .09, 1), 0, .9, 0),
    'Bark': ((.17, .075, .028, 1), 0, .9, 0),
    'Road': ((.09, .12, .14, 1), .15, .8, 0),
    'Paving': ((.41, .43, .4, 1), 0, .8, 0),
    'Water': ((.025, .16, .22, 1), .65, .14, 0),
    'Red': ((.52, .09, .035, 1), .6, .4, 0),
    'Ivory': ((.76,.73,.63,1), .05,.55,0),
    'Pale Stone': ((.62,.64,.60,1), .05,.6,0),
    'Teal Roof': ((.025,.23,.28,1), .68,.25,0),
    'Brass': ((.62,.40,.10,1), .75,.28,0),
    'Window Blue': ((.055,.23,.37,1), .5,.19,0),
    'Beacon': ((1,.008,.002,1), .1,.3,6),
    'Crystal Ice': ((.48,.80,.94,.36), .12,.065,.06),
    'Crystal Blue': ((.24,.59,.82,.48), .18,.085,.04),
    'Crystal Clear': ((.83,.95,1,.24), .08,.045,.04),
    'Crystal Frame': ((.62,.78,.86,1), .78,.17,.01),
    'Brick': ((.38,.14,.075,1), .02,.82,0),
    'Terracotta': ((.63,.29,.16,1), .03,.70,0),
    'Concrete': ((.46,.49,.48,1), .05,.72,0),
    'Graphite': ((.13,.17,.19,1), .12,.51,0),
    'Sandstone': ((.68,.56,.38,1), .03,.74,0),
    'Copper': ((.36,.24,.13,1), .65,.37,0),
    'Sage': ((.31,.40,.32,1), .12,.58,0),
    'WTC Glass': ((.52,.75,.94,1), .30,.08,.015),
}
for style,(rgb,cols,rows,metal,rough) in STYLES.items():
    PALETTE['Glass '+style]=((*rgb,1),metal,rough,0)


def material(key):
    name = 'Metropolis ' + key
    mat = bpy.data.materials.get(name)
    color, metal, rough, glow = PALETTE[key]
    if not mat:
        mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    node = mat.node_tree.nodes.get('Principled BSDF')
    node.inputs['Base Color'].default_value = (*color[:3],1)
    node.inputs['Alpha'].default_value = color[3]
    node.inputs['Metallic'].default_value = metal
    node.inputs['Roughness'].default_value = rough
    if key.startswith('Crystal '):
        node.inputs['Coat Weight'].default_value = .8
        node.inputs['Coat Roughness'].default_value = .035
    if key.startswith('Glass'):
        node.inputs['Coat Weight'].default_value = .45
        node.inputs['Coat Roughness'].default_value = .12
        filename = key.removeprefix('Glass ') + '-Glass.png' if key != 'Glass' else 'Azure-Glass.png'
        texture_path = Path(__file__).resolve().parents[1] / 'Textures' / filename
        if texture_path.exists():
            texture = mat.node_tree.nodes.get('Facade Windows') or mat.node_tree.nodes.new('ShaderNodeTexImage')
            texture.name = 'Facade Windows'
            texture.image = bpy.data.images.load(str(texture_path), check_existing=True)
            mat.node_tree.links.new(texture.outputs['Color'],node.inputs['Base Color'])
            mat['neris_facade_color_texture'] = str(texture_path)
    # Daytime glass remains simple blue glazing. Night presentation is separate.
    lights=mat.node_tree.nodes.get('Occupied Windows')
    if lights:
        mat.node_tree.nodes.remove(lights)
    if 'neris_facade_emission_texture' in mat:
        del mat['neris_facade_emission_texture']
    node.inputs['Emission Color'].default_value = color
    node.inputs['Emission Strength'].default_value = glow
    return mat


class Mesh:
    def __init__(self, name):
        self.name, self.vertices, self.faces, self.colors = name, [], [], []
        self.solids, self.floors = [], []

    def polygon(self, vertices, key):
        start = len(self.vertices)
        self.vertices.extend(vertices)
        self.faces.append(tuple(range(start, start + len(vertices))))
        self.colors.append(key)

    def box(self, center, size, key='Glass', yaw=0):
        x, y, z = center
        w, d, h = (v / 2 for v in size)
        ca, sa = math.cos(yaw), math.sin(yaw)
        p = [(x + a*ca-b*sa, y+a*sa+b*ca, z+c)
             for c in (-h, h) for a, b in ((-w,-d),(w,-d),(w,d),(-w,d))]
        for face in ((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)):
            self.polygon([p[i] for i in face], key)

    def loft(self, rings, key='Glass', segments=24):
        """Rings are (x, y, z, radius-x, radius-y, rotation)."""
        rows = []
        for x, y, z, rx, ry, angle in rings:
            ca, sa = math.cos(angle), math.sin(angle)
            rows.append([(x+rx*math.cos(i*math.tau/segments)*ca-ry*math.sin(i*math.tau/segments)*sa,
                          y+rx*math.cos(i*math.tau/segments)*sa+ry*math.sin(i*math.tau/segments)*ca,z)
                         for i in range(segments)])
        self.polygon(list(reversed(rows[0])), key)
        self.polygon(rows[-1], key)
        for lower, upper in zip(rows, rows[1:]):
            for i in range(segments):
                j = (i+1) % segments
                self.polygon([lower[i],lower[j],upper[j],upper[i]], key)

    def cylinder(self, x, y, z, radius, height, key='Silver', top=None, segments=24):
        top = radius if top is None else top
        self.loft([(x,y,z,radius,radius,0),(x,y,z+height,top,top,0)],key,segments)

    def beam(self, start, end, radius, key='Silver', segments=6):
        a, b = Vector(start), Vector(end)
        direction = (b-a).normalized()
        u = direction.cross(Vector((0,0,1)))
        if u.length < .01:
            u = direction.cross(Vector((0,1,0)))
        u.normalize()
        v = direction.cross(u)
        for i in range(segments):
            t, t2 = i*math.tau/segments, (i+1)*math.tau/segments
            p, q = radius*(u*math.cos(t)+v*math.sin(t)), radius*(u*math.cos(t2)+v*math.sin(t2))
            self.polygon([tuple(a+p),tuple(a+q),tuple(b+q),tuple(b+p)],key)

    def road(self, points, width, key='Road'):
        for a, b in zip(points, points[1:]):
            dx, dy = b[0]-a[0], b[1]-a[1]
            length = math.hypot(dx,dy)
            if length < .001:
                continue
            ox, oy = -dy/length*width/2, dx/length*width/2
            self.polygon([(a[0]-ox,a[1]-oy,a[2]),(b[0]-ox,b[1]-oy,b[2]),
                          (b[0]+ox,b[1]+oy,b[2]),(a[0]+ox,a[1]+oy,a[2])],key)

    def ring(self, radius, width, z, key='Road', center=(0,0), segments=192):
        points = [(center[0]+radius*math.cos(i*math.tau/segments),
                   center[1]+radius*math.sin(i*math.tau/segments),z) for i in range(segments+1)]
        self.road(points,width,key)

    def object(self, collection):
        data = bpy.data.meshes.new(self.name)
        data.from_pydata(self.vertices, [], self.faces)
        keys = list(dict.fromkeys(self.colors))
        for key in keys:
            data.materials.append(key if isinstance(key,bpy.types.Material) else material(key))
        for poly, key in zip(data.polygons,self.colors):
            poly.material_index = keys.index(key)
        data.update()
        uv = data.uv_layers.new(name='Facade')
        for poly in data.polygons:
            axis = 1 if abs(poly.normal.x) > abs(poly.normal.y) else 0
            for loop in poly.loop_indices:
                p = data.vertices[data.loops[loop].vertex_index].co
                uv.data[loop].uv = (p[axis]/16, p.z/16)
        obj = bpy.data.objects.new(self.name, data)
        collection.objects.link(obj)
        return obj


def square_tower(mesh, x, y, base, width, depth, height, key='Glass', floors=True):
    mesh.box((x,y,base+height/2),(width,depth,height),key)
    mesh.box((x,y,base+height),(width+1,depth+1,1.4),'Silver')
    if floors:
        for z in range(6,int(height),8):
            mesh.box((x,y,base+z),(width+.4,depth+.4,.55),'Silver')
        for fx in (-.35,0,.35):
            for side in (-1,1):
                mesh.box((x+fx*width,y+side*(depth/2+.25),base+height/2),(.55,.45,height),'White')
