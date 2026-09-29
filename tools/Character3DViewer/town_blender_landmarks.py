"""Append the same versioned landmark assemblies selected by the native town owner."""
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2] / 'games/SinStarI/SourceAssets/Towns/Neris'


def append_collection(source, name):
    with bpy.data.libraries.load(str(source), link=False) as (available, loaded):
        if name not in available.collections:
            raise ValueError('Missing authored landmark collection: ' + name)
        loaded.collections = [name]
    collection = loaded.collections[0]
    bpy.context.scene.collection.children.link(collection)
    collection.hide_viewport = False
    collection.hide_render = False
    return len(collection.all_objects)


def populate(document):
    original = ROOT / 'NerisSpaceport01V1/Town/Neris-Town-Spaceport-SW-r003.blend'
    counts = {}
    if document['name'] == 'Neris Spaceport':
        counts['Spaceport01'] = append_collection(original, 'Neris Spaceport 01 - Southwest')
    elif document['name'].startswith('Neris Town'):
        counts['RoyalCourt'] = append_collection(original, 'Royal Court — M06-r006')
        if (document['xs'][0] <= -11400 and document['xs'][-1] >= -5400
                and document['zs'][0] <= -3550 and document['zs'][-1] >= 6050):
            source = ROOT / 'NerisHorizonV1/Town/r008/Neris-Town-Horizon-r008.blend'
            counts['Horizon'] = append_collection(source, 'Horizon Airport - West')
    for name, count in counts.items():
        bpy.context.scene['town_landmark_' + name] = count
    return counts
