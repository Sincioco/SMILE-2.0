"""Bake the accepted Blender town into bounded SM3D-ready static GLB chunks.

Run with Blender --background Blend/Neris-Town-V1.blend --python this_file.
All evaluated geometry and collection instances are retained. No decimation.
"""
from pathlib import Path
import hashlib
import json
import sys
import struct
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
float3 = struct.Struct('<3f')
for instance in depsgraph.object_instances:
    obj = instance.object
    if obj.name.startswith('Neris Detailed Tree '):
        continue  # Export reusable templates below, preserving each tree's root transform.
    if obj.name.startswith(('Castle Moat Water', 'Comparison Moat Water')):
        continue  # The native lit water owns this surface; a second shallow plane flickered.
    if obj.name.startswith('Neris Tripo Castle'):
        continue  # Separate lossless UV/PBR partitions preserve the supplied castle atlas.
    if obj.type not in {"MESH", "FONT", "CURVE", "SURFACE"}:
        continue
    mesh = obj.to_mesh()
    if mesh is None:
        continue
    mesh.calc_loop_triangles()
    matrix = instance.matrix_world.copy()
    normal_matrix = matrix.to_3x3().inverted().transposed()
    # GLB stores float32. Weld by those exact output values, rather than retaining
    # duplicate vertices which differ only in discarded double-precision bits.
    positions = [float3.unpack(float3.pack(*(matrix @ vertex.co))) for vertex in mesh.vertices]
    normals = [float3.unpack(float3.pack(*(normal_matrix @ normal.vector).normalized()))
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
                material_keys[name] = (tuple(values), bool(material.get('neris_stone_texture')),
                                       bool(material.get('neris_grass_texture')))
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
for material, triangles in groups.values():
    if len(triangles)>100000:print(f"NERIS material: {material.name}: {len(triangles)} triangles",flush=True)
# The former 105-part allowance reserved 23 slots in the old 128-mesh pool.
# The native renderer now has 256 slots; preserve that reserve AND account for
# all 28 imported-castle parts. This is a scene resource check, not an exclusion.
assert len(parts) + 28 + 23 <= 256, "Town, imported castle, party and arena exceed native mesh capacity."
assert len(batches) + 14 + 5 + 3 <= 64, "Reserve live models for both castles, actors, trees and leaf."
source = Path(bpy.data.filepath)
manifest = {"source": source.relative_to(ROOT).as_posix(), "source_sha256": hashlib.sha256(
    source.read_bytes()).hexdigest(),
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

trees = sorted([o for o in bpy.data.objects if o.name.startswith('Garden Tree ')],key=lambda o:o.name)
templates = sorted({o.instance_collection.name for o in trees})
for index, name in enumerate(templates):
    obj = next(o for o in bpy.data.collections[name].objects if o.type=='MESH')
    mesh = obj.data
    mesh.calc_loop_triangles()
    tree_parts=[]
    for material_index,mat in enumerate(mesh.materials):
        triangles=[tuple((tuple(mesh.vertices[mesh.loops[i].vertex_index].co),
                          tuple(mesh.corner_normals[i].vector)) for i in t.loops)
                   for t in mesh.loop_triangles if t.material_index==material_index]
        tree_parts.append((mat.name,mat,triangles,len({v for t in triangles for v in t})))
    filename=f'Tree-{index}.glb'
    write(OUTPUT/filename, tree_parts)
    manifest.setdefault('tree_templates',[]).append({'file':filename,'parts':len(tree_parts),
        'triangles':sum(len(p[2]) for p in tree_parts),
        'sha256':hashlib.sha256((OUTPUT/filename).read_bytes()).hexdigest()})
manifest['trees']=[{'variant':templates.index(o.instance_collection.name),
    'position':[round(o.location.x*10,5),round(o.location.z*10+21,5),round(o.location.y*10,5)],
    'scale':[round(v*1000,5) for v in (o.scale.x,o.scale.z,o.scale.y)],
    'yaw':round(-o.rotation_euler.z*180/3.141592653589793,5)} for o in trees]
lamps=[]
for inst in depsgraph.object_instances:
    obj=inst.object
    if obj.type=='MESH' and any(m and m.name.startswith('Warm Lantern') for m in obj.data.materials):
        points=[inst.matrix_world @ v.co for v in obj.data.vertices]
        center=sum(points,Vector())/len(points)
        lamps.append([round(center.x*10,4),round(center.z*10+21,4),round(center.y*10,4)])
manifest['lamps']=lamps
(OUTPUT / "Neris.sm3d.json").write_text('{"version": 1}\n', encoding="utf-8")
(OUTPUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
