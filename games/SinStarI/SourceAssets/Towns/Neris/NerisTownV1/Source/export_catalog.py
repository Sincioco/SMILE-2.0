"""Export saved Blender assemblies as shared local-space authoring templates.

This is a build-time asset operation. It never saves or changes the source scene.
The existing Old Castle partitions preserve their supplied UV/PBR atlas.
"""
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import struct
import sys

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Source'))
from catalog_scene import assemblies, instance_owner
from catalog_features import collect, finish
from static_glb import write, material_json
import waterfront_plan

FLOAT3 = struct.Struct('<3f')


def material_key(material):
    value = material_json(material)
    value.pop('name', None)
    value['stone'] = bool(material.get('neris_stone_texture'))
    value['grass'] = bool(material.get('neris_grass_texture'))
    return json.dumps(value, sort_keys=True)


def split_parts(groups):
    result = []
    for material, triangles in groups.values():
        shared, current = set(), []
        for triangle in triangles:
            extra = set(triangle) - shared
            if len(shared) + len(extra) > 60000 or len(current) >= 65000:
                result.append((material.name, material, current, len(shared)))
                shared, current = set(), []
            shared.update(triangle)
            current.append(triangle)
        if current:
            result.append((material.name, material, current, len(shared)))
    return result


def export():
    items = assemblies()
    by_source = {a.source: a for a in items}
    by_member = {n: a for a in items if not a.collection for n in a.members}
    samples = {}
    for item in items:
        samples.setdefault(item.template, item)
    groups = {key: {} for key in samples}
    bounds = defaultdict(list)
    doors = defaultdict(list)
    features = {key: {'steps': [], 'solids': [], 'water': [], 'camera': []} for key in samples}
    source_materials = {}
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for instance in depsgraph.object_instances:
        owner = instance_owner(instance, by_source, by_member)
        if not owner or samples[owner.template].source != owner.source:
            continue
        obj = instance.object
        matrix = owner.matrix.inverted() @ instance.matrix_world
        collect(instance, owner, features[owner.template])
        if obj.get('neris_entrance'):
            entry = {
                'matrix': [list(row) for row in matrix],
                'width': obj['width'], 'height': obj['height'], 'label': obj['label']}
            if entry not in doors[owner.template]:
                doors[owner.template].append(entry)
        if owner.label == 'Old Castle' and obj.type == 'MESH':
            bounds[owner.template].extend(tuple(matrix @ Vector(p)) for p in obj.bound_box)
        if (owner.label == 'Old Castle' or obj.get('neris_door_leaf') or
                obj.get('neris_native_water') or
                obj.name.startswith(('Fountain Water', 'Royal Fountain Water')) or
                obj.type not in {'MESH', 'CURVE', 'FONT', 'SURFACE'}):
            continue
        mesh = obj.to_mesh()
        if not mesh:
            continue
        mesh.calc_loop_triangles()
        normals_matrix = matrix.to_3x3().inverted().transposed()
        positions = [FLOAT3.unpack(FLOAT3.pack(*(matrix @ v.co))) for v in mesh.vertices]
        normals = [FLOAT3.unpack(FLOAT3.pack(*(normals_matrix @ n.vector).normalized()))
                   for n in mesh.corner_normals]
        bounds[owner.template].extend(positions)
        for triangle in mesh.loop_triangles:
            material = mesh.materials[triangle.material_index]
            if material.name not in source_materials:
                source_materials[material.name] = material_key(material)
            key = source_materials[material.name]
            group = groups[owner.template].setdefault(key, [material, []])
            corners = tuple((positions[mesh.loops[i].vertex_index], normals[i])
                            for i in triangle.loops)
            a, b, c = [Vector(v[0]) for v in corners]
            if (b-a).cross(c-a).length_squared > 1e-12:
                group[1].append(corners)
        obj.to_mesh_clear()

    output = ROOT / 'Authoring'
    output.mkdir(exist_ok=True)
    (output / 'Templates').mkdir(exist_ok=True)
    templates, parts, aliases, signatures = [], [], {}, {}
    duplicate_triangles = 0
    for key, sample in samples.items():
        digest = hashlib.sha256()
        for material, triangles in groups[key].values():
            # Evaluated curves may expose the same faces through two depsgraph
            # entries. Keep one identically shaded, identically wound triangle.
            unique = {}
            for triangle in triangles:
                canonical = min(triangle, triangle[1:] + triangle[:1], triangle[2:] + triangle[:2])
                unique.setdefault(canonical, triangle)
            duplicate_triangles += len(triangles) - len(unique)
            triangles[:] = unique.values()
            digest.update(material_key(material).encode('utf-8'))
            for triangle in triangles:
                for position, normal in triangle:
                    digest.update(FLOAT3.pack(*position))
                    digest.update(FLOAT3.pack(*normal))
        signature = digest.hexdigest()
        # Door metadata/labels are part of a template's identity, too.
        signature += json.dumps(doors[key], sort_keys=True)
        if signature in signatures and sample.label != 'Old Castle':
            aliases[key] = signatures[signature]
            continue
        index = len(templates)
        signatures[signature] = index
        aliases[key] = index
        points = bounds[key]
        box = [[min(p[i] for p in points) for i in range(3)],
               [max(p[i] for p in points) for i in range(3)]] if points else None
        record = {'id': index, 'source': key, 'label': sample.label,
                  'category': sample.category, 'bounds': box, 'parts': [],
                  'doors': doors[key], 'sample': sample.source, **finish(features[key])}
        if sample.label == 'Old Castle':
            record['existingCastle'] = True
        for part in split_parts(groups[key]):
            parts.append((index, part))
        templates.append(record)

    batches, counts = [], []
    for template_id, part in sorted(parts, key=lambda p: p[1][3], reverse=True):
        destination = next((i for i, count in enumerate(counts)
                            if count + part[3] <= 125000 and len(batches[i]) < 16
                            and sum(len(p[1][2]) for p in batches[i]) + len(part[2]) <= 130000), None)
        if destination is None:
            destination = len(batches)
            batches.append([])
            counts.append(0)
        templates[template_id]['parts'].append([destination, len(batches[destination])])
        batches[destination].append((template_id, part))
        counts[destination] += part[3]

    catalog = {'schema': 1, 'source': 'Blend/' + Path(bpy.data.filepath).name,
               'source_sha256': hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
               'templates': templates, 'instances': [], 'chunks': [],
               'terrain': waterfront_plan.data(), 'removed_duplicate_triangles': duplicate_triangles}
    for item in items:
        position, rotation, scale = item.matrix.decompose()
        catalog['instances'].append({
            'id': item.identity, 'source': item.source, 'template': aliases[item.template],
            'position': list(position), 'rotation': list(rotation.to_euler()),
            'scale': list(scale), 'members': item.members, 'collection': item.collection})
    for index, batch in enumerate(batches):
        filename = f'Templates/Catalog-{index:02d}.glb'
        write(output / filename, [part for _, part in batch])
        catalog['chunks'].append({'file': filename, 'parts': len(batch),
            'triangles': sum(len(p[2]) for _, p in batch),
            'sha256': hashlib.sha256((output / filename).read_bytes()).hexdigest()})
    (output / 'catalog.json').write_text(json.dumps(catalog, indent=2) + '\n', encoding='utf-8')
    (output / 'Catalog.sm3d.json').write_text('{"version": 1}\n', encoding='utf-8')
    # Only generated chunks in the verified template directory may be retired.
    template_root = (output / 'Templates').resolve()
    retained = {(output / entry['file']).resolve() for entry in catalog['chunks']}
    for path in template_root.glob('Catalog-*.glb'):
        resolved = path.resolve()
        if resolved.parent == template_root and resolved not in retained:
            path.unlink()
    draws = sum(28 if templates[i['template']].get('existingCastle') else
                len(templates[i['template']]['parts']) for i in catalog['instances'])
    print(f'CATALOG EXPORT {len(items)} instances, {len(templates)} templates, '
          f'{len(parts)} parts, {len(batches)} models, {draws} full-scene submissions', flush=True)


if __name__ == '__main__':
    export()
