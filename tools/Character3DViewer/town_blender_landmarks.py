"""Append the same versioned landmark assemblies selected by the native town owner."""
from pathlib import Path
import bpy
import math
from town_surface_layers import distance, elevation

ROOT = Path(__file__).resolve().parents[2] / 'games/SinStarI/SourceAssets/Towns/Neris'


def append_collection(source, name, offset=(0, 0)):
    with bpy.data.libraries.load(str(source), link=False) as (available, loaded):
        if name not in available.collections:
            raise ValueError('Missing authored landmark collection: ' + name)
        loaded.collections = [name]
    collection = loaded.collections[0]
    bpy.context.scene.collection.children.link(collection)
    collection.hide_viewport = False
    collection.hide_render = False
    from mathutils import Matrix, Vector
    displacement = Matrix.Translation(Vector((offset[0] / 10, offset[1] / 10, 0)))
    members = set(collection.all_objects)
    # Appended objects need evaluated world matrices before applying saved offsets.
    bpy.context.view_layer.update()
    for obj in members:
        if obj.parent not in members:
            obj.matrix_world = displacement @ obj.matrix_world
    return len(collection.all_objects)


def append_court(offset):
    source = ROOT / 'NerisSpaceport01V1/Town/Neris-Town-Spaceport-SW-r003.blend'
    name = 'Royal Court \u2014 M06-r006'
    count = append_collection(source, name, offset)
    members = set(bpy.data.collections[name].all_objects)
    for obj in members:
        if obj.parent not in members:
            obj.location.z += distance('STRUCTURE_CLEARANCE')
    return count


def append_airport():
    scene = bpy.context.scene
    source = ROOT / 'NerisHorizonV1/Source/Neris-Horizon-Gentle-Wave-r009.blend'
    with bpy.data.libraries.load(str(source), link=False) as (_, loaded):
        loaded.scenes = ['Horizon Gentle Wave r009']
    incoming = loaded.scenes[0]
    collection = bpy.data.collections.new('Horizon Airport - East')
    scene.collection.children.link(collection)
    anchor = bpy.data.objects.new('Horizon Full Size Placement', None)
    collection.objects.link(anchor)
    anchor.location = (740, 125, .212)
    anchor.rotation_euler.z = -math.pi / 2
    count = 0
    for obj in list(incoming.objects):
        if not (obj.get('horizon_asset') or obj.get('horizon_internal_light') or obj.get('runway_aircraft_preview')):
            continue
        collection.objects.link(obj)
        if obj.parent is None:
            obj.parent = anchor
        count += 1
    bpy.data.scenes.remove(incoming)
    bpy.context.window.scene = scene
    return count


def append_spaceport():
    """Append the current standalone source, preserving all authored hierarchies."""
    scene = bpy.context.scene
    source = ROOT / 'NerisSpaceport01V1/Source/NSP01-final-r09.blend'
    with bpy.data.libraries.load(str(source), link=False) as (_, loaded):
        loaded.scenes = ['NSP01.NerisSpaceportFinal']
    incoming = loaded.scenes[0]
    collection = bpy.data.collections.new('Neris Spaceport 01 - Southwest')
    scene.collection.children.link(collection)
    anchor = bpy.data.objects.new('Spaceport Full Size Placement', None)
    collection.objects.link(anchor)
    anchor.location = (-550, -760, elevation('ROAD_Y') + distance('STRUCTURE_CLEARANCE'))
    anchor.rotation_euler.z = math.pi
    members = {obj for obj in incoming.objects
               if obj.get('asset_id') == 'NSP01' and obj.get('export_eligible')}
    if not members:
        raise ValueError('Spaceport source contains no eligible asset objects.')
    for obj in list(members):
        parent = obj.parent
        while parent:
            members.add(parent)
            parent = parent.parent
    for obj in members:
        collection.objects.link(obj)
        if obj.parent is None:
            obj.parent = anchor
    bpy.data.scenes.remove(incoming)
    bpy.context.window.scene = scene
    return len(members)


def append_visitors():
    """Use the native pad anchors, with all visitors parked for Blender inspection."""
    from mathutils import Vector, Matrix
    source = ROOT / 'NerisSpaceport01V1/Fleet/Alien-Visitors-r001.blend'
    with bpy.data.libraries.load(str(source), link=False) as (_, loaded):
        loaded.objects = [name for name in _.objects]
    collection = bpy.data.collections.new('Alien Visitors - Four Landing Pads')
    bpy.context.scene.collection.children.link(collection)
    count = 0
    for obj in loaded.objects:
        if 'alien_ship' not in obj:
            bpy.data.objects.remove(obj)
            continue
        ship = int(obj['alien_ship'])
        display = Vector(((ship % 2) * 95 - 47.5, (ship // 2) * 100 - 50, 0))
        pad = Vector((-244 if ship < 2 else -856,
                      -804 if ship % 2 == 0 else -916,
                      67.312 if ship % 2 == 0 else 107.312))
        heading = math.pi / 2 if ship < 2 else -math.pi / 2
        placement = Matrix.Translation(pad) @ Matrix.Rotation(heading, 4, 'Z')
        obj.matrix_world = placement @ Matrix.Translation(-display) @ obj.matrix_basis.copy()
        collection.objects.link(obj)
        count += 1
    return count


def populate(document):
    original = ROOT / 'NerisSpaceport01V1/Town/Neris-Town-Spaceport-SW-r003.blend'
    counts = {}
    if document['name'] == 'Neris Spaceport':
        counts['Spaceport01'] = append_spaceport()
        counts['AlienVisitors'] = append_visitors()
    elif document['name'] == 'Horizon Airport':
        counts['Horizon'] = append_airport()
    if document.get('court_placed', document['name'].startswith('Neris Town')):
        counts['RoyalCourt'] = append_court(document.get('court_offset', (0, 0)))
    for name, count in counts.items():
        bpy.context.scene['town_landmark_' + name] = count
    return counts
