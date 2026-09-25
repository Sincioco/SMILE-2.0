"""Align the accepted town's original streets, expansion and royal paving."""
from pathlib import Path
import json
import math
import sys
import bpy
from mathutils import Matrix

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Source'))
from paving_grid import geometry


def replace_surface(obj, rectangles, height):
    vertices, faces, tones = geometry(rectangles, height)
    mesh = bpy.data.meshes.new(obj.name+' Aligned')
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(bpy.data.materials['Town Pavinglight'])
    mesh.materials.append(bpy.data.materials['Town Paving'])
    for face, tone in zip(mesh.polygons, tones):
        face.material_index = tone
    mesh.update()
    old = obj.data
    obj.data = mesh
    obj.parent = None
    obj.matrix_world = Matrix.Identity(4)
    if old.users == 0:
        bpy.data.meshes.remove(old)


def apply(paved=None):
    bpy.context.view_layer.update()
    surfaces = [o for o in bpy.data.objects if o.type == 'MESH' and ' Paving Stones' in o.name]
    for obj in surfaces:
        points = [obj.matrix_world @ v.co for v in obj.data.vertices]
        # Recover the footprint from the old inset stones. Metadata makes reruns exact.
        bounds = obj.get('neris_paving_bounds') or [min(p.x for p in points)-.025,
            min(p.y for p in points)-.025, max(p.x for p in points)+.025, max(p.y for p in points)+.025]
        height = max(p.z for p in points)
        replace_surface(obj, [bounds], height)
        obj['neris_paving_bounds'] = list(bounds)
    if paved is None:
        paved = json.loads((ROOT/'expansion-layout.json').read_text())['paving']
    rectangles = [(math.floor(x-w/2), math.floor(y-d/2), math.ceil(x+w/2), math.ceil(y+d/2))
                  for x, y, w, d in paved]
    replace_surface(bpy.data.objects['Unified Expanded Slate Tiles'], rectangles, .212)
    # The royal platforms retain their thickness; add matching tiled tops.
    for name in ['Royal Bridge Deck', 'Military Precinct']:
        base = bpy.data.objects[name]
        points = [base.matrix_world @ v.co for v in base.data.vertices]
        bounds = [min(p.x for p in points), min(p.y for p in points),
                  max(p.x for p in points), max(p.y for p in points)]
        surface_name = name+' Aligned Tiles'
        obj = bpy.data.objects.get(surface_name)
        if obj is None:
            obj = bpy.data.objects.new(surface_name, bpy.data.meshes.new(surface_name))
            base.users_collection[0].objects.link(obj)
        replace_surface(obj, [bounds], max(p.z for p in points)+.012)
    bpy.context.scene['Neris Paving Grid'] = '2 metre world grid; shared origin and tone; clipped edges'
    bpy.context.view_layer.update()
    print('PAVING aligned', len(surfaces), 'original streets plus expansion and royal platforms', flush=True)


if __name__ == '__main__':
    assert bpy.app.background and Path(bpy.data.filepath).name == 'Neris-Town-Expanded.blend'
    apply()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Expanded.blend'), compress=True)
