"""Preserve Tripo's HD surface and prepare a reduced authoring copy in Blender.

Run with Blender --background --python this-file. Does not touch the visible scene.
The original GLB and its imported .blend remain immutable.
"""
import bpy
import bmesh
import json
import time
from pathlib import Path

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
STAGE.mkdir(parents=True, exist_ok=True)
START = time.monotonic()


def progress(message):
    print(f'[{time.monotonic() - START:.1f}s] {message}', flush=True)


def topology(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    remaining = set(bm.verts)
    components = []
    while remaining:
        first = remaining.pop()
        pending, found = [first], [first]
        while pending:
            vertex = pending.pop()
            for edge in vertex.link_edges:
                other = edge.other_vert(vertex)
                if other in remaining:
                    remaining.remove(other)
                    pending.append(other)
                    found.append(other)
        components.append({'vertices': len(found),
                           'min': [min(v.co[i] for v in found) for i in range(3)],
                           'max': [max(v.co[i] for v in found) for i in range(3)]})
    info = {'vertices': len(bm.verts), 'triangles': len(bm.faces),
            'boundary_edges': sum(e.is_boundary for e in bm.edges),
            'non_manifold_edges': sum(not e.is_manifold for e in bm.edges),
            'degenerate_faces': sum(f.calc_area() < 1e-14 for f in bm.faces),
            'components': sorted(components, key=lambda c: -c['vertices'])}
    bm.free()
    return info


bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-tripo-hd-face-repaired.blend'))
scene = bpy.context.scene
source = next(o for o in scene.objects if o.type == 'MESH')
source.name = 'Mira.Tripo.HD.Original'
reduced = source.copy()
reduced.data = source.data.copy()
scene.collection.objects.link(reduced)
reduced.name = 'Mira.Tripo.Reduced'
source.hide_render = True
source.hide_set(True)
bpy.ops.object.select_all(action='DESELECT')
reduced.select_set(True)
bpy.context.view_layer.objects.active = reduced
progress('Original preserved; welding only coincident UV seam vertices on the copy')
bm = bmesh.new()
bm.from_mesh(reduced.data)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-7)
bm.to_mesh(reduced.data)
bm.free()
reduced.data.update()
report = {'source': 'HD face correction derived from Source/mira-tripo-hd-original.glb', 'weld_distance': 1e-7,
          'welded_source': topology(reduced.data)}
(STAGE / 'geometry-inspection.json').write_text(json.dumps(report, indent=2) + '\n')
progress('Welded topology inspected: ' + json.dumps({k: v for k, v in report['welded_source'].items() if k != 'components'}))
modifier = reduced.modifiers.new('18.5K body authoring reduction', 'DECIMATE')
modifier.decimate_type = 'COLLAPSE'
modifier.ratio = 18500 / len(reduced.data.polygons)
modifier.use_collapse_triangulate = True
progress('Reducing the body to approximately 18,500 triangles')
bpy.ops.object.modifier_apply(modifier=modifier.name)
for polygon in reduced.data.polygons:
    polygon.use_smooth = True
report['reduced'] = topology(reduced.data)
(STAGE / 'geometry-inspection.json').write_text(json.dumps(report, indent=2) + '\n')
progress('Reduced topology: ' + json.dumps({k: v for k, v in report['reduced'].items() if k != 'components'}))
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-tripo-reduction-work.blend'), compress=True)
progress('REDUCTION_READY')
