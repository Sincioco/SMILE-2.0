"""Reduce the preserved Tripo staff for Mira's complete 20K triangle budget."""
import bpy
import bmesh
import json
from pathlib import Path

STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-staff-hd-original.blend'))
scene = bpy.context.scene
source = bpy.data.objects['Mira.Staff.HD.Source']
staff = source.copy()
staff.data = source.data.copy()
scene.collection.objects.link(staff)
staff.name = 'Mira.Staff.Reduced'
source.hide_render = True
source.hide_set(True)
bpy.ops.object.select_all(action='DESELECT')
staff.select_set(True)
bpy.context.view_layer.objects.active = staff
bm = bmesh.new()
bm.from_mesh(staff.data)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-7)
bm.to_mesh(staff.data)
bm.free()
# The HD staff contains folded connections that prevent a clean collapse mesh.
# A sub-millimeter closed authoring surface removes those defects before baking.
modifier = staff.modifiers.new('Closed staff authoring surface', 'REMESH')
modifier.mode = 'VOXEL'
modifier.voxel_size = .0008
modifier.use_smooth_shade = True
bpy.ops.object.modifier_apply(modifier=modifier.name)
modifier = staff.modifiers.new('Mira staff 1450 triangle reduction', 'DECIMATE')
modifier.decimate_type = 'COLLAPSE'
modifier.ratio = 1450 / sum(len(p.vertices) - 2 for p in staff.data.polygons)
modifier.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=modifier.name)
bm = bmesh.new()
bm.from_mesh(staff.data)
bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=list(bm.edges))
for edge in list(bm.edges):
    if edge.is_valid and len(edge.link_faces) > 2:
        assert edge.calc_length() < .0001
        bmesh.ops.pointmerge(bm, verts=list(edge.verts),
                            merge_co=(edge.verts[0].co + edge.verts[1].co) / 2)
# Separate independent face fans at a pinched point without moving the surface.
coordinates, corners = [], {}
for vertex in bm.verts:
    remaining = set(vertex.link_faces)
    while remaining:
        first = remaining.pop()
        pending, fan = [first], {first}
        while pending:
            face = pending.pop()
            for edge in face.edges:
                if vertex in edge.verts:
                    for adjacent in edge.link_faces:
                        if adjacent in remaining:
                            remaining.remove(adjacent)
                            pending.append(adjacent)
                            fan.add(adjacent)
        index = len(coordinates)
        coordinates.append(tuple(vertex.co))
        for face in fan:
            corners[(vertex, face)] = index
faces = [[corners[(v,f)] for v in f.verts] for f in bm.faces]
mesh = bpy.data.meshes.new('Mira.Staff.ClosedSurface')
mesh.from_pydata(coordinates, [], faces)
bm.free()
staff.data = mesh
bm = bmesh.new()
bm.from_mesh(mesh)
bmesh.ops.triangulate(bm, faces=list(bm.faces))
# Collapse can leave disconnected zero-volume two-sided triangles. Edge-manifold
# checks alone accept those shells, but Blender's exporter rejects duplicate faces.
face_groups = {}
for face in bm.faces:
    face_groups.setdefault(frozenset(face.verts),[]).append(face)
flat_shells = [group for group in face_groups.values() if len(group) > 1]
for group in flat_shells:
    assert len(group) == 2
    assert all(set(edge.link_faces) == set(group) for edge in group[0].edges)
bmesh.ops.delete(bm, geom=[face for group in flat_shells for face in group], context='FACES')
bmesh.ops.delete(bm, geom=[vertex for vertex in bm.verts if not vertex.link_faces], context='VERTS')
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
report = {'triangles': len(bm.faces), 'vertices': len(bm.verts),
          'removed_flat_duplicate_shells': len(flat_shells),
          'boundary_edges': sum(e.is_boundary for e in bm.edges),
          'nonmanifold_edges': sum(not e.is_manifold for e in bm.edges),
          'nonmanifold_vertices': sum(not v.is_manifold for v in bm.verts),
          'degenerate_faces': sum(f.calc_area() < 1e-12 for f in bm.faces),
          'defects': [{'length':e.calc_length(),'center':list((e.verts[0].co+e.verts[1].co)/2)}
                      for e in bm.edges if not e.is_manifold]}
bm.to_mesh(staff.data)
bm.free()
assert not staff.data.validate(verbose=True, clean_customdata=False)
(STAGE / 'staff-reduction.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-staff-reduced-work.blend'), compress=True)
print('STAFF_REDUCED '+json.dumps(report),flush=True)
