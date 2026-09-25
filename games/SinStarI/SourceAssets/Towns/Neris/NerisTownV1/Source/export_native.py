"""Bake the accepted Blender town into bounded SM3D-ready static GLB chunks.

Run with Blender --background Blend/Neris-Town-V1.blend --python this_file.
All evaluated geometry and collection instances are retained. No decimation.
"""
from pathlib import Path
import hashlib
import json
import sys
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from static_glb import write

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Runtime"
OUTPUT.mkdir(exist_ok=True)
depsgraph = bpy.context.evaluated_depsgraph_get()
groups = {}
material_keys = {}
instance_count = 0
degenerate_count = 0
for instance in depsgraph.object_instances:
    obj = instance.object
    if obj.type not in {"MESH", "FONT", "CURVE", "SURFACE"}:
        continue
    mesh = obj.to_mesh()
    if mesh is None:
        continue
    mesh.calc_loop_triangles()
    matrix = instance.matrix_world.copy()
    normal_matrix = matrix.to_3x3().inverted().transposed()
    positions = [tuple(matrix @ vertex.co) for vertex in mesh.vertices]
    normals = [tuple((normal_matrix @ normal.vector).normalized())
               for normal in mesh.corner_normals]
    materials = list(mesh.materials)
    for triangle in mesh.loop_triangles:
        a, b, c = [Vector(positions[index]) for index in triangle.vertices]
        if (b - a).cross(c - a).length_squared <= 1e-12:
            degenerate_count += 1
            continue
        material = materials[triangle.material_index] if materials else None
        name = material.name if material else "Unassigned"
        if name not in material_keys:
            node = next((n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None) if material and material.use_nodes else None
            if node:
                values = []
                for socket in ("Base Color", "Metallic", "Roughness", "Alpha", "Emission Color", "Emission Strength"):
                    value = node.inputs[socket].default_value
                    values.append(tuple(round(v, 6) for v in value) if hasattr(value, "__len__") else round(value, 6))
                material_keys[name] = tuple(values)
            else:
                material_keys[name] = name
        key = material_keys[name]
        if key not in groups:
            groups[key] = [material, []]
        groups[key][1].append(tuple(
            (positions[mesh.loops[index].vertex_index], normals[index])
            for index in triangle.loops))
    obj.to_mesh_clear()
    instance_count += 1

source_triangles = sum(len(value[1]) for value in groups.values())
print(f"NERIS: {instance_count} evaluated instances, {source_triangles} triangles", flush=True)
# Exact position/normal sharing preserves geometry and shading while saving slots.
parts = []
for material, triangles in groups.values():
    shared, part_triangles = set(), []
    for triangle in triangles:
        extra = set(triangle) - shared
        if len(shared) + len(extra) > 60000 or len(part_triangles) >= 65000:
            parts.append((material.name, material, part_triangles, len(shared)))
            shared, part_triangles = set(), []
        shared.update(triangle)
        part_triangles.append(triangle)
    if part_triangles:
        parts.append((material.name, material, part_triangles, len(shared)))
batches = []
counts = []
for part in sorted(parts, key=lambda value: value[3], reverse=True):
    destination = next((i for i, count in enumerate(counts)
                        if count + part[3] <= 125000 and len(batches[i]) < 16
                        and sum(len(p[2]) for p in batches[i]) + len(part[2]) <= 130000), None)
    if destination is None:
        batches.append([])
        counts.append(0)
        destination = len(batches) - 1
    batches[destination].append(part)
    counts[destination] += part[3]
print(f"NERIS: {len(parts)} parts, {len(groups)} unique materials, {len(batches)} models", flush=True)
assert len(parts) <= 105, "Reserve meshes for party and arena."
assert len(batches) <= 56, "Leave native model slots for five actors and backgrounds."
manifest = {"source": "Blend/Neris-Town-V1.blend", "source_sha256": hashlib.sha256(
    (ROOT / "Blend/Neris-Town-V1.blend").read_bytes()).hexdigest(),
    "evaluated_instances": instance_count, "triangles": source_triangles,
    "removed_degenerate_triangles": degenerate_count,
    "coordinate_mapping": "Blender XYZ -> SMILE XZY; runtime scale 10, height +21",
    "chunks": []}
for index, batch in enumerate(batches):
    filename = f"Neris-{index:02d}.glb"
    write(OUTPUT / filename, batch)
    manifest["chunks"].append({"file": filename, "parts": len(batch),
        "triangles": sum(len(part[2]) for part in batch),
        "sha256": hashlib.sha256((OUTPUT / filename).read_bytes()).hexdigest()})
    print(f"NERIS: exported {index + 1}/{len(batches)}", flush=True)
(OUTPUT / "Neris.sm3d.json").write_text('{"version": 1}\n', encoding="utf-8")
(OUTPUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
