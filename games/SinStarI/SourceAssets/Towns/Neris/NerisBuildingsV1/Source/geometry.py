"""Small mesh-building vocabulary for the Neris architectural source assets."""
import math
import random

import bpy
from mathutils import Vector


def material(name, color, metallic=0.0, roughness=0.5, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metallic
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Emission Color'].default_value = (*color, 1)
    shader.inputs['Emission Strength'].default_value = emission
    return mat


def palette():
    mats = {
        'stone': material('Ivory Limestone', (0.68, 0.625, 0.51), roughness=0.78),
        'trim': material('Pale Carved Stone', (0.83, 0.77, 0.65), roughness=0.62),
        'gold': material('Aged Gold', (0.55, 0.34, 0.12), 0.76, 0.29),
        'brightgold': material('Polished Gold Edge', (0.82, 0.60, 0.26), 0.7, 0.23),
        'roof': material('Teal Enamel Roof', (0.032, 0.19, 0.25), 0.4, 0.29),
        'roofalt': material('Teal Enamel Alternate', (0.045, 0.235, 0.29), 0.4, 0.31),
        'cloth': material('Neris Teal Banner', (0.025, 0.145, 0.185), roughness=0.87),
        'dark': material('Recess Shadow', (0.018, 0.027, 0.029), roughness=0.9),
        'iron': material('Patinated Iron', (0.095, 0.12, 0.125), 0.68, 0.38),
        'wood': material('Dark Walnut', (0.17, 0.083, 0.03), roughness=0.72),
        'woodalt': material('Walnut Light Grain', (0.245, 0.145, 0.068), roughness=0.78),
        'glass': material('Amber Window Glass', (0.13, 0.067, 0.015), 0.2, 0.22, 0.4),
        'lamp': material('Warm Lantern', (1.0, 0.58, 0.17), 0.05, 0.25, 2),
        'crystal': material('Cyan Relay Crystal', (0.01, 0.48, 0.8), 0.25, 0.18, 0.7),
        'crystaledge': material('Crystal Bright Facets', (0.15, 0.78, 1.0), 0.2, 0.15, 1.1),
        'leaf': material('Moss Green Leaves', (0.11, 0.20, 0.063), roughness=0.9),
        'leaflight': material('Fresh Green Leaves', (0.21, 0.31, 0.105), roughness=0.85),
        'bottle': material('Blue Potion Ceramic', (0.02, 0.25, 0.56), 0.3, 0.18),
    }
    for i, factor in enumerate([0.93, 0.97, 1.035, 1.065]):
        mats[f'stone{i}'] = material(f'Limestone Block Tone {i+1}',
                                    tuple(c * factor for c in (0.68, 0.625, 0.51)),
                                    roughness=0.78)
    return mats


class Geometry:
    """Owns one building collection, root transform, and material palette."""

    def __init__(self, name, materials):
        self.collection = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(self.collection)
        self.root = bpy.data.objects.new(name, None)
        self.collection.objects.link(self.root)
        self.materials = materials
        self.rng = random.Random(1709)

    def mesh(self, name, verts, faces, mat='stone', smooth=False):
        data = bpy.data.meshes.new(name)
        data.from_pydata(verts, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        self.collection.objects.link(obj)
        obj.parent = self.root
        data.materials.append(self.materials[mat])
        for poly in data.polygons:
            poly.use_smooth = smooth
        return obj

    def bevel(self, obj, amount=0.035, segments=2):
        mod = obj.modifiers.new('Soft Cut Edges', 'BEVEL')
        mod.width = amount
        mod.segments = segments
        mod = obj.modifiers.new('Weighted Corner Normals', 'WEIGHTED_NORMAL')
        mod.keep_sharp = True
        return obj

    def box(self, name, loc, size, mat='stone', bevel=0.03):
        x, y, z = (v / 2 for v in size)
        verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
                 (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
        faces = [(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
        obj = self.mesh(name, verts, faces, mat)
        obj.location = loc
        if bevel:
            self.bevel(obj, min(bevel, min(size) * 0.23))
        return obj

    def lathe(self, name, profile, loc=(0,0,0), mat='gold', steps=32, smooth=True):
        verts = [(r * math.cos(j*math.tau/steps), r * math.sin(j*math.tau/steps), z)
                 for r,z in profile for j in range(steps)]
        faces = [(i*steps+j, i*steps+(j+1)%steps, (i+1)*steps+(j+1)%steps,
                  (i+1)*steps+j) for i in range(len(profile)-1) for j in range(steps)]
        faces += [tuple(reversed(range(steps))),
                  tuple((len(profile)-1)*steps+j for j in range(steps))]
        obj = self.mesh(name, verts, faces, mat, smooth)
        obj.location = loc
        return obj

    def cylinder(self, name, loc, radius, depth, mat='gold', steps=20):
        return self.lathe(name, [(radius,-depth/2),(radius,depth/2)], loc, mat, steps)

    def beam(self, name, start, end, radius, mat='gold', steps=12):
        a, b = Vector(start), Vector(end)
        obj = self.cylinder(name, (a+b)/2, radius, (b-a).length, mat, steps)
        obj.rotation_euler = (b-a).to_track_quat('Z','Y').to_euler()
        return obj

    def path(self, name, points, radius=0.04, mat='gold', cyclic=False):
        data = bpy.data.curves.new(name, 'CURVE')
        data.dimensions = '3D'
        data.resolution_u = 1
        data.bevel_depth = radius
        data.bevel_resolution = 2
        spline = data.splines.new('POLY')
        spline.points.add(len(points)-1)
        for point, xyz in zip(spline.points, points):
            point.co = (*xyz, 1)
        spline.use_cyclic_u = cyclic
        obj = bpy.data.objects.new(name, data)
        self.collection.objects.link(obj)
        obj.parent = self.root
        data.materials.append(self.materials[mat])
        return obj

    def prism(self, name, outline, depth, mat='trim'):
        """Extrude an x/z outline along y; outline runs counterclockwise in x/z."""
        n = len(outline)
        verts = [(x,y,z) for y in (-depth/2,depth/2) for x,z in outline]
        faces = [tuple(range(n)),tuple(reversed(range(n,2*n)))]
        faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
        return self.mesh(name, verts, faces, mat)

    def text(self, name, text, loc, size, mat='brightgold', rotation=0):
        data = bpy.data.curves.new(name, 'FONT')
        data.body = text
        data.align_x = 'CENTER'
        data.align_y = 'CENTER'
        data.size = size
        data.extrude = 0.007
        data.bevel_depth = 0.001
        data.space_character = 1.15
        obj = bpy.data.objects.new(name, data)
        self.collection.objects.link(obj)
        obj.parent = self.root
        obj.location = loc
        obj.rotation_euler = (math.pi/2,0,rotation)
        data.materials.append(self.materials[mat])
        return obj

    def masonry(self, width, depth, height, bottom=0.35):
        """Staggered stone courses are actual geometry, retained in the GLB."""
        rows = math.ceil(height / 0.7)
        course = height / rows
        for face, length, distance in [(0,width,depth/2),(1,depth,width/2),
                                       (2,width,depth/2),(3,depth,width/2)]:
            angle = face * math.pi/2
            for row in range(rows):
                cuts = [-length/2]
                x = -length/2 + (0.72 if row%2 else 1.4)
                while x < length/2:
                    cuts.append(x)
                    x += 1.4
                cuts.append(length/2)
                for left,right in zip(cuts,cuts[1:]):
                    x = (left+right)/2
                    loc = (x*math.cos(angle)+distance*math.sin(angle),
                           x*math.sin(angle)-distance*math.cos(angle),
                           bottom+(row+0.5)*course)
                    obj = self.box(f'Masonry Side {face+1} Course {row+1}', loc,
                                   (right-left-0.012,0.2,course-0.012),
                                   f'stone{self.rng.randrange(4)}',0.012)
                    obj.rotation_euler.z = angle


def face_point(side, depth, x, z, outward=0):
    angle = side * math.pi/2
    return (x*math.cos(angle)+(depth+outward)*math.sin(angle),
            x*math.sin(angle)-(depth+outward)*math.cos(angle),z)


def on_face(obj, side, depth, x=0, z=0, outward=0):
    obj.location = face_point(side, depth, x, z, outward)
    obj.rotation_euler.z = side*math.pi/2
    return obj
