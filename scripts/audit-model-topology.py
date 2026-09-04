"""Read-only GLB topology audit. Run with Blender --background --factory-startup.

Only the report and optional diagnostic render are written. Never saves a model.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Vector


def components(adjacency):
    remaining = set(adjacency)
    result = []
    while remaining:
        pending = [min(remaining)]
        found = set()
        while pending:
            current = pending.pop()
            if current in found:
                continue
            found.add(current)
            pending.extend(adjacency[current] - found)
        remaining -= found
        result.append(sorted(found))
    return result


def topology(mesh):
    mesh.verts.ensure_lookup_table()
    mesh.verts.index_update()
    mesh.edges.index_update()
    mesh.faces.index_update()
    boundary = [edge for edge in mesh.edges if edge.is_boundary]
    boundary_graph = defaultdict(set)
    graph = defaultdict(set)
    for vertex in mesh.verts:
        graph[vertex.index]
    for edge in mesh.edges:
        a, b = [vertex.index for vertex in edge.verts]
        graph[a].add(b)
        graph[b].add(a)
        if edge.is_boundary:
            boundary_graph[a].add(b)
            boundary_graph[b].add(a)
    regions = []
    for indices in components(boundary_graph):
        coords = [mesh.verts[index].co for index in indices]
        regions.append({
            "closedLoop": all(len(boundary_graph[index]) == 2 for index in indices),
            "vertexCount": len(indices),
            "representativeVertices": indices[:16],
            "minimumLocal": [round(min(v[axis] for v in coords), 7) for axis in range(3)],
            "maximumLocal": [round(max(v[axis] for v in coords), 7) for axis in range(3)],
        })
    return {
        "vertices": len(mesh.verts), "faces": len(mesh.faces),
        "boundaryEdges": len(boundary),
        "nonManifoldEdgesIncludingBoundary": sum(not e.is_manifold for e in mesh.edges),
        "overConnectedEdges": sum(len(e.link_faces) > 2 for e in mesh.edges),
        "zeroAreaFaces": sum(f.calc_area() <= 1e-12 for f in mesh.faces),
        "inconsistentWindingEdges": sum(e.is_manifold and not e.is_contiguous for e in mesh.edges),
        "disconnectedComponents": len(components(graph)),
        "closedBoundaryLoops": sum(region["closedLoop"] for region in regions),
        "boundaryRegions": regions,
        "representativeBoundaryEdges": [[v.index for v in e.verts] for e in boundary[:24]],
        "selfIntersections": None,
    }, boundary


def run():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--preview", type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    source_hash = hashlib.sha256(args.source.read_bytes()).hexdigest()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(args.source))
    objects = sorted((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda obj: obj.name)
    reports = []
    segments = []
    for obj in objects:
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        raw, boundary = topology(mesh)
        weights = Counter()
        for index in sorted({v.index for edge in boundary for v in edge.verts}):
            for group in obj.data.vertices[index].groups:
                if group.weight >= 0.2:
                    weights[obj.vertex_groups[group.group].name] += 1
        # Texture/normal splits are not necessarily holes. Audit both, without
        # modifying the imported mesh; welding only affects this temporary BMesh.
        bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=1e-6)
        welded, welded_boundary = topology(mesh)
        for edge in welded_boundary:
            segments.append([obj.matrix_world @ vertex.co for vertex in edge.verts])
        reports.append({"object": obj.name, "raw": raw, "coincidentWeld1eMinus6": welded,
                        "rawBoundarySkinGroups": dict(sorted(weights.items())),
                        "shoulderArmpitReviewRequired": any("arm" in name.lower() or "shoulder" in name.lower() for name in weights)})
        mesh.free()
    report = {
        "schemaVersion": 1, "source": args.source.name, "sourceSha256": source_hash,
        "tool": "scripts/audit-model-topology.py", "toolVersion": 1,
        "toolSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        "blenderVersion": bpy.app.version_string,
        "developmentUse": "allowed", "productionAssetApproval": "blocked", "releaseEnablement": "blocked",
        "reason": "User-confirmed visible hollow interior; source geometry repair requires separate approval.",
        "limitations": ["Boundary loops can be intentional openings or separate armor pieces; counts alone do not classify visible holes.",
                        "Temporary position welding separates UV/normal splits from remaining open geometry; it does not repair the source.",
                        "Local-space mesh counts, before skinning. Skin-group hints identify areas for shoulder/armpit review.",
                        "Consistent winding is measured across manifold edges; global outward normals are not provable for open surfaces.",
                        "Robust self-intersection classification is not implemented.",
                        "Preview shows unskinned source surfaces with boundary edges, without Viewer calibration or fire; it is not a native Viewer screenshot."],
        "meshes": reports,
    }
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    if args.preview:
        preview(objects, segments, args.preview)
    if hashlib.sha256(args.source.read_bytes()).hexdigest() != source_hash:
        raise RuntimeError("Source changed during read-only audit; discard this report.")
    print(json.dumps({"meshes": len(reports), "weldedBoundaryEdges": sum(r["coincidentWeld1eMinus6"]["boundaryEdges"] for r in reports),
                      "productionAssetApproval": "blocked", "sourceUnchanged": True}))


def preview(objects, segments, output):
    surface = bpy.data.materials.new("Diagnostic gray, preview only")
    surface.diffuse_color = (0.28, 0.35, 0.43, 1)
    for obj in objects:
        # The audit uses source vertices; show those same vertices, not a skinned
        # animation pose underneath unskinned boundary lines. This scene is disposable.
        for modifier in list(obj.modifiers):
            if modifier.type == "ARMATURE":
                obj.modifiers.remove(modifier)
        obj.data.materials.clear()
        obj.data.materials.append(surface)
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ obj.data.vertices[index].co for obj in objects
              for index in sorted({i for polygon in obj.data.polygons for i in polygon.vertices})]
    low = Vector([min(point[axis] for point in points) for axis in range(3)])
    high = Vector([max(point[axis] for point in points) for axis in range(3)])
    center = (low + high) / 2
    extent = max(high - low)
    edges = bpy.data.curves.new("Remaining open edges, temporary welded diagnostic", "CURVE")
    edges.dimensions = "3D"
    edges.bevel_depth = extent * 0.0006
    edges.bevel_resolution = 0
    for segment in segments:
        spline = edges.splines.new("POLY")
        spline.points.add(1)
        for vertex, point in zip(spline.points, segment):
            vertex.co = (*point, 1)
    red = bpy.data.materials.new("Open edge highlight")
    red.diffuse_color = (1, 0.04, 0.015, 1)
    edges.materials.append(red)
    obj = bpy.data.objects.new("Boundary diagnostic", edges)
    bpy.context.scene.collection.objects.link(obj)
    camera_data = bpy.data.cameras.new("Diagnostic camera")
    camera = bpy.data.objects.new("Diagnostic camera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = center + Vector((extent * 0.2, -extent * 2, extent * 0.13))
    camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = extent * 1.18
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.background_type = "WORLD"
    scene.world.color = (0.025, 0.03, 0.045)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 1280
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    output.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)


run()
