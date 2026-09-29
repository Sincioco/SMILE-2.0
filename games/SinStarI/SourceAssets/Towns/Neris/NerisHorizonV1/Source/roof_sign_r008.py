"""A Neris teal-and-gold entrance sign following the terminal's wave roof."""
import bpy
import math
from mathutils import Matrix
import forms as F


def roof(x):
    return 53 + 24 * math.exp(-(x / 90) ** 2) + 10 * math.exp(-((abs(x) - 228) / 53) ** 2)


def apply(scene):
    before = set(scene.objects)
    # Both edges follow the actual roof curve, with a substantial gold frame.
    points = [(x, -3.9, roof(x) + 1.6) for x in range(-90, 91, 3)]
    points += [(x, -3.9, roof(x) + 25.6) for x in range(90, -91, -3)]
    F.mesh('Horizon Wave Roof Sign', points, [tuple(range(len(points)))], 'Teal')
    for offset in (1.6, 25.6):
        for x in range(-90, 90, 3):
            F.beam('Roof Sign Gold Wave Frame', (x,-4.3,roof(x)+offset),
                   (x+3,-4.3,roof(x+3)+offset), .75, 'Gold')
    for x in (-90,90):
        F.beam('Roof Sign Gold End', (x,-4.3,roof(x)+1.6),
               (x,-4.3,roof(x)+25.6), .9, 'Gold')
    for label, size, offset in [('NERIS',4.3,18.0),('HORIZON AIRPORT',8.5,7.5),
                                ('A BRIGHTER TOMORROW',2.5,3.0)]:
        data = bpy.data.curves.new(label, 'FONT')
        data.body = label
        data.size = size
        data.align_x = 'CENTER'
        data.extrude = .12
        data.resolution_u = 5
        obj = bpy.data.objects.new('Roof Sign ' + label, data)
        scene.collection.objects.link(obj)
        obj.location = (0,-4.9,0)
        obj.rotation_euler.x = math.pi/2
        data.materials.append(F.MATERIALS['Gold'])
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target='MESH')
        transform = obj.matrix_world.copy()
        for vertex in obj.data.vertices:
            p = transform @ vertex.co
            p.z += roof(p.x) + offset
            vertex.co = p
        obj.matrix_world = Matrix.Identity(4)
        obj.select_set(False)
    for x in (-109,109):
        top = roof(x) + 22
        outline = [(x-8,-4.1,top),(x+8,-4.1,top),(x+8,-4.1,top-24),
                   (x,-4.1,top-31),(x-8,-4.1,top-24)]
        F.mesh('Roof Sign Neris Banner', outline, [tuple(range(5))], 'Teal')
        for a,b in zip(outline, outline[1:]+outline[:1]):
            F.beam('Roof Sign Banner Gold Frame', (a[0],-4.6,a[2]),(b[0],-4.6,b[2]),.65,'Gold')
        F.beam('Roof Sign Banner Bar',(x-12,-4.5,top+1),(x+12,-4.5,top+1),.9,'Gold')
        F.beam('Roof Sign Mount',(x,3,roof(x)),(x,-3.8,top-1),1.0,'Gold')
        star = []
        for i in range(8):
            a = math.pi*i/4
            r = 5.5 if i%2 == 0 else 1.4
            star.append((x+math.sin(a)*r,-5.1,top-12+math.cos(a)*r))
        F.mesh('Roof Sign Neris Compass',star,[tuple(range(8))],'Gold')
    for obj in set(scene.objects)-before:
        obj['horizon_asset'] = True
        obj['assembly'] = 'Wave Roof Airport Sign'
