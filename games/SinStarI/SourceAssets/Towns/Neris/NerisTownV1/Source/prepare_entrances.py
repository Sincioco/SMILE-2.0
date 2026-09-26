"""Make the modeled town entrances real recesses with reusable hinged leaves.

Run on the saved Expanded file in background Blender. The imported Tripo reference
remains untouched: its baked portcullis is not a separable authored door.
"""
from pathlib import Path
import math
import sys
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path[:0] = [str(ROOT / 'Source'), str(ROOT.parent / 'NerisBuildingsV1/Source')]
from geometry import Geometry
from details import arch_outline
from civic import build_city_hall, build_tower
from shops import build_shop


def bounds(obj, inverse):
    points = [inverse @ obj.matrix_world @ Vector(p) for p in obj.bound_box]
    return [min(p[i] for p in points) for i in range(3)], [max(p[i] for p in points) for i in range(3)]


def cut_recess(collection, cutter, inverse, width, height, depth, ignored=None):
    for obj in list(collection.objects):
        if obj.type not in {'MESH', 'CURVE', 'FONT'} or obj == ignored:
            continue
        lo, hi = bounds(obj, inverse)
        if (hi[0] < -width/2 or lo[0] > width/2 or hi[1] < -.2 or
                lo[1] > depth or hi[2] < 0 or lo[2] > height):
            continue
        # The exact arch-shaped boolean cuts the actual wall and its trim.
        if obj.type != 'MESH':
            mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(bpy.context.evaluated_depsgraph_get()))
            replacement = bpy.data.objects.new(obj.name + ' Cut', mesh)
            collection.objects.link(replacement)
            replacement.parent = obj.parent
            replacement.matrix_world = obj.matrix_world
            bpy.data.objects.remove(obj, do_unlink=True)
            obj = replacement
        modifier = obj.modifiers.new('Operable Entrance Recess', 'BOOLEAN')
        modifier.operation = 'DIFFERENCE'
        modifier.solver = 'EXACT'
        modifier.object = cutter
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)


def prepare():
    assert bpy.app.background
    names = {'stone': 'Ivory Limestone', 'trim': 'Pale Carved Stone', 'gold': 'Aged Gold',
             'brightgold': 'Polished Gold Edge', 'roof': 'Teal Enamel Roof',
             'roofalt': 'Teal Enamel Alternate', 'cloth': 'Neris Teal Banner',
             'dark': 'Recess Shadow', 'iron': 'Patinated Iron', 'wood': 'Dark Walnut',
             'woodalt': 'Walnut Light Grain', 'glass': 'Amber Window Glass',
             'lamp': 'Warm Lantern', 'crystal': 'Cyan Relay Crystal',
             'crystaledge': 'Crystal Bright Facets', 'leaf': 'Moss Green Leaves',
             'leaflight': 'Fresh Green Leaves', 'bottle': 'Blue Potion Ceramic'}
    names.update({f'stone{i}': f'Limestone Block Tone {i+1}' for i in range(4)})
    mats = {key: bpy.data.materials[name] for key, name in names.items()}
    # Restore editable source geometry in place of the five flattened GLB imports.
    for label in ['City Hall', 'Communication Tower', 'Weapon Store', 'Armor Store', 'Item Store']:
        name = 'Neris-' + label.replace(' ', '-')
        old = bpy.data.objects.get(name + ' Placement')
        if not old:
            continue
        transform = old.matrix_world.copy()
        owners = list(old.users_collection)
        for collection in owners:
            for obj in list(collection.all_objects):
                bpy.data.objects.remove(obj, do_unlink=True)
            bpy.data.collections.remove(collection)
        g = Geometry(name + ' Editable', mats)
        if label == 'City Hall': build_city_hall(g)
        elif label == 'Communication Tower': build_tower(g)
        else: build_shop(g, label)
        g.root.matrix_world = transform
        g.root['neris_building'] = label
    bpy.context.view_layer.update()
    # Existing template instances retain their placement, scale and material palette.
    doors = [o for o in bpy.data.objects if o.type == 'MESH' and
             o.name.startswith(('Closed Door', 'Pointed Portal')) and not o.get('neris_hinged')]
    castle = bpy.data.objects['Royal Castle of Neris']
    if not castle.get('neris_entrance_prepared'):
        g = Geometry('Royal Entry Construction', mats)
        proxy = g.prism('Palace Entrance Proxy', arch_outline(6.1, 7.4), .08, 'iron')
        proxy.parent = castle
        proxy.location = (0, 1.08, 6.5)
        doors.append(proxy)
        castle['neris_entrance_prepared'] = True
    for number, door in enumerate(doors):
        collection = castle.users_collection[0] if door.name == 'Palace Entrance Proxy' else door.users_collection[0]
        linked = collection.name in bpy.context.scene.collection.children
        if not linked: bpy.context.scene.collection.children.link(collection)
        bpy.context.view_layer.update()
        points = [v.co for v in door.data.vertices]
        x0, x1 = min(v.x for v in points), max(v.x for v in points)
        z0, z1 = min(v.z for v in points), max(v.z for v in points)
        width, height = x1-x0, z1-z0
        frame = door.matrix_world @ Matrix.Translation(((x0+x1)/2, 0, z0))
        inverse = frame.inverted()
        depth = max(.9, width*.65)
        helper = Geometry('Entrance Work', mats)
        cutter = helper.prism('Entrance Cutter', arch_outline(width+.025, height+.02), depth+.25, 'dark')
        cutter.parent = None
        cutter.matrix_world = frame @ Matrix.Translation((0, depth/2-.075, -.01))
        bpy.context.view_layer.update()
        cut_recess(collection, cutter, inverse, width, height, depth, door)
        marker = bpy.data.objects.new('Neris Entrance', None)
        collection.objects.link(marker)
        marker.parent = door.parent
        marker.matrix_world = frame
        marker['neris_entrance'] = True
        marker['width'] = width
        marker['height'] = height
        marker['label'] = door.parent.get('neris_building', door.parent.name) if door.parent else 'Palace'
        # A shallow unlit-looking vestibule hides the solid exterior-model interior.
        back = helper.prism('Entrance Interior Shadow', arch_outline(width, height), .035, 'dark')
        back.parent = None
        back.matrix_world = frame @ Matrix.Translation((0, depth-.08, 0))
        floor = helper.box('Entrance Threshold', (0, 0, 0), (width, depth+.1, .08), 'trim', 0)
        floor.parent = None
        floor.matrix_world = frame @ Matrix.Translation((0, depth/2+.05, -.045))
        for obj in [back, floor]:
            helper.collection.objects.unlink(obj)
            collection.objects.link(obj)
            world = obj.matrix_world.copy()
            obj.parent = marker
            obj.matrix_world = world
        # The native runtime instances this same normalized two-leaf model.
        for side in [-1, 1]:
            outline = [(0, 0), (side*.5, 0), (side*.5, .64)]
            outline += [(side*(.5-.5*(i/16)**2), .64+.36*i/16) for i in range(1,17)]
            leaf = helper.prism('Neris Door Leaf', outline, .025, 'iron')
            leaf.parent = marker
            leaf.matrix_basis = Matrix.Diagonal((width, .7, height, 1))
            leaf['neris_door_leaf'] = True
            leaf['neris_leaf_side'] = side
            helper.collection.objects.unlink(leaf)
            collection.objects.link(leaf)
            edge = helper.path('Neris Door Leaf Border', [(x, -.022, z) for x,z in outline], .007, 'gold', True)
            edge.parent = marker
            edge.matrix_basis = Matrix.Diagonal((width, .7, height, 1))
            edge['neris_door_leaf'] = True
            edge['neris_leaf_side'] = side
            helper.collection.objects.unlink(edge)
            collection.objects.link(edge)
        bpy.data.objects.remove(door, do_unlink=True)
        for obj in list(helper.collection.objects): bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(helper.collection)
        if not linked: bpy.context.scene.collection.children.unlink(collection)
        print('ENTRANCE', number+1, marker['label'], round(width, 2), flush=True)
    bpy.context.view_layer.update()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Blend/Neris-Town-Expanded.blend'), compress=True)


def repair_palace():
    """Cut the palace wall for existing prepared sources, using the actual owner."""
    castle = bpy.data.objects['Royal Castle of Neris']
    if castle.get('neris_palace_wall_open'): return
    marker = next(o for o in castle.children if o.get('neris_entrance'))
    frame = marker.matrix_world.copy()
    width, height = marker['width'], marker['height']
    depth = max(.9, width*.65)
    helper = Geometry('Palace Recess Work', {'dark': bpy.data.materials['Recess Shadow']})
    cutter = helper.prism('Palace Cutter', arch_outline(width+.025,height+.02),depth+.25,'dark')
    cutter.parent = None
    cutter.matrix_world = frame @ Matrix.Translation((0,depth/2-.075,-.01))
    bpy.context.view_layer.update()
    cut_recess(castle.users_collection[0], cutter, frame.inverted(), width, height, depth)
    for obj in list(helper.collection.objects): bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(helper.collection)
    castle['neris_palace_wall_open'] = True
    print('PALACE actual exterior wall recess cut',flush=True)


if __name__ == '__main__':
    prepare()
