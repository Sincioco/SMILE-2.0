"""Export hinged leaves and exact stair/threshold footprints from the saved town."""
from pathlib import Path
import json
import math
import sys
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[5]
sys.path.insert(0, str(ROOT / 'Source'))
from static_glb import write


def export():
    entries, surfaces, seen = [], [], set()
    for instance in bpy.context.evaluated_depsgraph_get().object_instances:
        obj = instance.object
        matrix = instance.matrix_world
        if obj.get('neris_entrance'):
            pos = matrix.translation
            key = tuple(round(v, 4) for v in pos)
            if key in seen: continue
            seen.add(key)
            right, inward, up = [matrix.to_3x3().col[i] for i in range(3)]
            entries.append(dict(label=obj['label'], position=[pos.x*10, pos.z*10+21, pos.y*10],
                width=obj['width']*right.length*10, height=obj['height']*up.length*10,
                right=[right.normalized().x, right.normalized().y]))
        if obj.type != 'MESH' or not obj.name.startswith(('Entrance Step', 'Front Door Step',
                'Royal Entrance Stair', 'Sweeping Royal Garden Stair', 'Entrance Threshold')):
            continue
        points = [Vector(p) for p in obj.bound_box]
        low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
        high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
        center = matrix @ Vector(((low.x+high.x)/2, (low.y+high.y)/2, high.z))
        right = matrix.to_3x3().col[0]
        forward = matrix.to_3x3().col[1]
        surfaces.append([center.x*10, center.y*10, center.z*10+21,
            right.normalized().x, right.normalized().y,
            (high.x-low.x)*right.length*5, (high.y-low.y)*forward.length*5])
    entries.sort(key=lambda d: (d['label'], d['position']))
    surfaces = sorted(set(tuple(round(v, 5) for v in s) for s in surfaces))
    assert entries and len(entries) <= 80
    # One normalized left and right leaf, including their bronze edging.
    first = next(o for o in bpy.data.objects if o.get('neris_entrance'))
    parts = []
    for side in [-1, 1]:
        for obj in sorted(first.children, key=lambda o: o.name):
            if not obj.get('neris_door_leaf') or obj['neris_leaf_side'] != side: continue
            evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
            mesh = evaluated.to_mesh()
            mesh.calc_loop_triangles()
            triangles = [tuple((tuple(mesh.vertices[mesh.loops[i].vertex_index].co),
                                tuple(mesh.corner_normals[i].vector)) for i in t.loops)
                         for t in mesh.loop_triangles]
            material = mesh.materials[0]
            parts.append((f'Leaf {side} {material.name}', material, triangles, len(mesh.vertices)))
            evaluated.to_mesh_clear()
    assert len(parts) == 4
    write(ROOT / 'Runtime/Door-Leaves.glb', parts)
    data = {'doors': entries, 'surfaces': surfaces, 'source': 'Blend/'+Path(bpy.data.filepath).name,
            'excluded': 'Tripo reference portcullis remains baked into its exterior mesh.'}
    (ROOT / 'Runtime/entrances.json').write_text(json.dumps(data, indent=2)+'\n')
    f = lambda value: f'{value:.5f}'
    lines = ["''' Generated doorway and stair footprints from Blender; no mutable scene state.",
        'Module Smile.Tools.NerisTownEntrancesData', '', 'Option Explicit', '',
        'Import Smile.Simple3D.Precision3D As P', '',
        f'Public Const DOOR_COUNT = {len(entries)}', f'Public Const SURFACE_COUNT = {len(surfaces)}', '',
        'Public Type Door', '    Position As P.Vector3', '    Width As Double', '    Height As Double',
        '    RightX As Double', '    RightZ As Double', '    Label As Text', 'End Type', '',
        'Public Type Surface', '    X As Double', '    Z As Double', '    Height As Double',
        '    RightX As Double', '    RightZ As Double', '    HalfWidth As Double',
        '    HalfDepth As Double', 'End Type', '',
        'Public Function DoorAt(Index As Number) As Door', '', '    Dim Result As Door', '',
        '    Select Case Index']
    for i, entry in enumerate(entries):
        lines += [f'        Case {i}', f"            Result.Position = P.Vector({', '.join(map(f,entry['position']))})",
            f"            Result.Width = {f(entry['width'])}", f"            Result.Height = {f(entry['height'])}",
            f"            Result.RightX = {f(entry['right'][0])}", f"            Result.RightZ = {f(entry['right'][1])}",
            f"            Result.Label = \"{entry['label']}\"", '']
    lines += ['    End Select', '', '    Return Result', '', 'End Function', '',
        'Public Function SurfaceAt(Index As Number) As Surface', '', '    Dim Result As Surface', '', '    Select Case Index']
    for i, surface in enumerate(surfaces):
        lines += [f'        Case {i}'] + [f'            Result.{name} = {f(value)}'
            for name, value in zip(['X','Z','Height','RightX','RightZ','HalfWidth','HalfDepth'], surface)] + ['']
    lines += ['    End Select', '', '    Return Result', '', 'End Function', '', 'End Module', '']
    (REPO / 'tools/Character3DViewer/NerisTownEntrancesData.smile').write_text('\n'.join(lines), encoding='utf-8')
    print('ENTRANCE EXPORT', len(entries), 'doors,', len(surfaces), 'walkable surfaces', flush=True)


if __name__ == '__main__':
    export()
