"""Rebuild Mira1's original oval staff as closed primitives inside a loaded rig.

The source silhouette and dimensions come from the preserved authored staff.
No other Mira candidate's geometry, grip transform or skin is imported.
"""
import bpy
import bmesh
import math
from mathutils import Vector


def repair_staff(staff):
    vertices, faces, colors, smooth = [], [], [], []

    def add(points, polygons, color, shaded=False):
        offset = len(vertices)
        vertices.extend(points)
        for polygon in polygons:
            faces.append(tuple(offset + i for i in polygon))
            colors.append(color)
            smooth.append(shaded and len(polygon) == 4)

    def cone(z, length, r1, r2, color, count=8):
        points = [(radius * math.cos(2 * math.pi * i / count),
                   radius * math.sin(2 * math.pi * i / count), height)
                  for radius, height in [(r1, z - length / 2), (r2, z + length / 2)]
                  for i in range(count)]
        polygons = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
        polygons += [(i, (i + 1) % count, (i + 1) % count + count, i + count)
                     for i in range(count)]
        add(points, polygons, color, True)

    def strip(side, color, radial=0, y=0, width=.026, depth=.020, count=22):
        points, polygons = [], [(3, 2, 1, 0)]
        for i in range(count + 1):
            angle = .13 + (math.pi - .28) * i / count
            center = Vector((side * (.125 + radial) * math.sin(angle), y,
                             .708 + (.178 + radial) * math.cos(angle)))
            tangent = Vector((side * .125 * math.cos(angle), 0,
                              -.178 * math.sin(angle))).normalized()
            normal = Vector((-tangent.z, 0, tangent.x))
            taper = .38 + .62 * math.sin(math.pi * i / count) ** .4
            for a, b in [(-1, -1), (1, -1), (1, 1), (-1, 1)]:
                points.append(center + normal * (a * width * taper / 2)
                              + Vector((0, b * depth / 2, 0)))
        for i in range(count):
            for j in range(4):
                polygons.append((4 * i + j, 4 * i + (j + 1) % 4,
                                 4 * (i + 1) + (j + 1) % 4, 4 * (i + 1) + j))
        polygons.append(tuple(4 * count + j for j in range(4)))
        add(points, polygons, color, True)

    def gem(z, height, radius, count=6):
        points = [(0, 0, z - height / 2), (0, 0, z + height / 2)]
        for dz, r in [(-height * .08, radius), (height * .12, radius * .82)]:
            for i in range(count):
                angle = 2 * math.pi * i / count
                points.append((r * math.cos(angle), r * math.sin(angle), z + dz))
        polygons = []
        for i in range(count):
            j = (i + 1) % count
            polygons.extend([(0, 2 + j, 2 + i),
                             (2 + i, 2 + j, 2 + count + j, 2 + count + i),
                             (1, 2 + count + i, 2 + count + j)])
        add(points, polygons, 3)

    # Palette: ivory, navy grip, antique gold, water crystal.
    cone(-.295, 1.19, .011, .014, 0, 12)
    cone(.36, .16, .017, .017, 1, 12)
    for z in [-.89, -.72, -.68, .278, .291, .447, .459]:
        cone(z, .015, .018, .018, 2)
    cone(-.935, .07, .012, .019, 2)
    gem(-.99, .052, .013)
    cone(.50, .082, .020, .029, 2)
    cone(.496, .05, .021, .023, 0)
    cone(.545, .028, .022, .037, 2)
    for side in [-1, 1]:
        strip(side, 2)
        strip(side, 0, -.009, -.012, .007, .003, 18)
        strip(side, 3, .012, -.013, .004, .004, 16)
    gem(.725, .168, .029)
    gem(.909, .044, .010, 5)
    cone(.604, .086, .015, .004, 2)

    mesh = bpy.data.meshes.new('Mira1.ClosedOvalStaff')
    mesh.from_pydata(vertices, [], faces)
    for polygon, color, shade in zip(mesh.polygons, colors, smooth):
        polygon.material_index = color
        polygon.use_smooth = shade
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(edge.is_manifold for edge in bm.edges), 'Staff surface must be closed'
    bm.to_mesh(mesh)
    bm.free()
    uv = mesh.uv_layers.new(name='UVMap')
    for polygon in mesh.polygons:
        center = .125 + .25 * polygon.material_index
        for loop, offset in zip(polygon.loop_indices, [(-.003, -.003), (.003, -.003), (0, .003)]):
            uv.data[loop].uv = (center + offset[0], .5 + offset[1])
        polygon.material_index = 0
    mesh.materials.append(staff.data.materials[0])
    staff.data = mesh
    staff.vertex_groups.clear()
    staff.vertex_groups.new(name='mixamorig:RightHand').add(list(range(len(mesh.vertices))), 1, 'REPLACE')
    mesh.update()
    return {'part': staff.name, 'triangles': len(mesh.polygons), 'boundaryEdges': 0,
            'method': 'Original oval staff dimensions; closed primitive reconstruction before reduction'}
