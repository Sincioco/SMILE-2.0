"""Town landscape and street furniture; reusable collection instances keep editing fast."""
import math

import bpy

from geometry import Geometry
from details import crystal, column, star, banner


def instance(collection, target, name, location, angle=0, scale=1):
    obj = bpy.data.objects.new(name, None)
    obj.instance_type = 'COLLECTION'
    obj.instance_collection = collection
    target.objects.link(obj)
    obj.location = location
    obj.rotation_euler.z = angle
    obj.scale = (scale,) * 3
    return obj


def templates(mats):
    result = {}
    for name in ['Tree', 'Crystal Lamp', 'Bench', 'Flower Bed']:
        g = Geometry(name, mats)
        if name == 'Tree':
            g.lathe('Rooted Trunk', [(.5, 0), (.25, .8), (.17, 3.7)], mat='wood', steps=9)
            for i in range(7):
                a = i * 2.4
                x, y = math.cos(a) * 1.2, math.sin(a) * 1.2
                g.beam('Branch', (0, 0, 2), (x, y, 3.9 + i % 2 * .4), .11, 'wood')
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.65,
                    location=(x, y, 4.1 + i % 3 * .4))
                obj = bpy.context.object
                for col in list(obj.users_collection):
                    col.objects.unlink(obj)
                g.collection.objects.link(obj)
                obj.parent = g.root
                obj.name = 'Faceted Neris Canopy'
                obj.scale = (1, .88, .83)
                obj.data.materials.append(mats['treeleaf' if i % 2 else 'treebright'])
        elif name == 'Crystal Lamp':
            g.lathe('Lamp Pedestal', [(.36, 0), (.36, .2), (.18, .3), (.10, 3.3),
                                     (.30, 3.45)], mat='gold', steps=12)
            crystal(g, (0, 0, 3.48), .85, .2)
            for i in range(4):
                a = i * math.pi / 2
                g.beam('Crystal Cage', (.31 * math.cos(a), .31 * math.sin(a), 3.4),
                       (0, 0, 4.45), .025, 'gold')
        elif name == 'Bench':
            for x in [-.85, .85]:
                g.box('Bench Stone Leg', (x, 0, .31), (.24, .6, .62), 'trim')
            for y in [-.22, 0, .22]:
                g.box('Bench Seat Slat', (0, y, .64), (2.3, .18, .13), 'wood')
            for z in [.9, 1.14]:
                g.box('Bench Back Slat', (0, .34, z), (2.3, .1, .18), 'wood')
            for x in [-.9, .9]:
                g.beam('Bench Frame', (x, .33, .2), (x, .33, 1.25), .045, 'gold')
        else:
            g.cylinder('Garden Raised Rim', (0, 0, .18), 1.5, .36, 'trim', 32)
            g.cylinder('Garden Soil', (0, 0, .38), 1.36, .06, 'soil', 32)
            for i in range(23):
                a = i * 2.4
                r = 1.15 * math.sqrt((i + .5) / 23)
                x, y = math.cos(a) * r, math.sin(a) * r
                g.lathe('Flower Foliage', [(.16, 0), (.22, .18), (.03, .45)],
                        (x, y, .4), 'leaflight', 7, False)
                g.lathe('Flower Petals', [(.03, 0), (.13, .12), (.08, .17)],
                        (x, y, .79), 'flower', 7, False)
        bpy.context.scene.collection.children.unlink(g.collection)
        result[name] = g.collection
    return result


def fountain(g, x, y):
    loc = lambda z: (x, y, z)
    g.cylinder('Fountain Octagonal Steps', loc(.18), 5, .36, 'trim', 8)
    g.lathe('Fountain Basin Wall', [(4.3, .35), (4.3, .85), (4, 1.1),
                                  (3.65, 1.1), (3.65, .4)], loc(0), 'stone', 48)
    g.cylinder('Fountain Water', loc(.78), 3.68, .04, 'water', 48)
    g.lathe('Fountain Crystal Pedestal', [(1, .45), (.7, 1), (.48, 1.7),
                                        (1.1, 2), (.85, 2.16)], loc(0), 'trim', 16)
    crystal(g, loc(2.1), 2.2, .48)
    for i in range(8):
        a = i * math.pi / 4
        points = []
        for j in range(20):
            t = j / 19
            r = .5 + 2.5 * t
            points.append((x + r * math.cos(a), y + r * math.sin(a),
                           2.05 + 1.5 * t - 2.7 * t * t))
        g.path('Arc of Water', points, .04, 'water')


def gate(g):
    for x in [-6.6, 6.6]:
        column(g, x, -39, 6.7, 1.1)
        banner(g, 0, 39, x, 5.9, 1.7, 3.1, 'NERIS')
    g.box('Gate Crown', (0, -39, 6.95), (14.6, 1.15, .8), 'roof', .16)
    g.box('Gate Gold Line', (0, -39.61, 6.64), (14.5, .07, .09), 'gold')
    g.text('Neris Arrival Sign', 'N E R I S', (0, -39.61, 7), .52)
    for x in [-8, 8]:
        crystal(g, (x, -39, 0), 2.7, .42)
