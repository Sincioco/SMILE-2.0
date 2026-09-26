"""Identify editable assemblies in the saved Neris scene without changing Blender.

Collection placements share templates. Authored landmarks retain their complete
hierarchies. Furniture originally authored under one broad parent is grouped by
its visible fountain, sign, wayfinding post or crystal-circle center.
"""
from collections import defaultdict
from dataclasses import dataclass
import hashlib

import bpy
from mathutils import Matrix, Vector


LANDMARKS = {
    'Royal Castle of Neris': 'Royal Castle',
    'Neris Military Headquarters': 'Military HQ',
    'Neris-City-Hall Editable': 'City Hall',
    'Neris-Communication-Tower Editable': 'Comm Tower',
    'Neris-Weapon-Store Editable': 'Weapon Shop',
    'Neris-Armor-Store Editable': 'Armor Shop',
    'Neris-Item-Store Editable': 'Item Shop',
}
HOMES = {'Family House', 'Courtyard Cottage', 'Garden Pavilion',
         'Neris New Large Home', 'Neris New Medium Home', 'Neris New Small Home'}
LABELS = {'Neris New Large Home': 'Estate House',
          'Neris New Medium Home': 'Middle Class House',
          'Neris New Small Home': 'Small House',
          'Detailed Neris Flower Planter': 'Flower Planter',
          'Neris Detailed Tree 1': 'Leafy Tree',
          'Neris Detailed Tree 2': 'Slender Tree',
          'Market Stall Template': 'Market Stall',
          'Neris Tripo Castle Architecture': 'Old Castle'}


@dataclass
class Assembly:
    source: str
    template: str
    label: str
    category: str
    matrix: Matrix
    members: list
    collection: str = ''

    @property
    def identity(self):
        return hashlib.sha256(self.source.encode('utf-8')).hexdigest()[:16]


def center(obj):
    points = [obj.matrix_world @ Vector(p) for p in obj.bound_box]
    return Vector(tuple((min(p[i] for p in points) + max(p[i] for p in points)) / 2
                        for i in range(3)))


def furniture_groups(parent_name):
    parent = bpy.data.objects[parent_name]
    children = list(parent.children_recursive)
    anchors = []
    for obj in children:
        if obj.name.startswith('Fountain Water'):
            point = center(obj)
            point.z = 0
            anchors.append((obj.name, 'Fountain', point))
        elif obj.name.startswith('Wayfarer Plaza'):
            point = center(obj)
            point.z = 0
            anchors.append((obj.name, 'Crystal Circle', point))
        elif obj.name.startswith('Wayfinding Post'):
            point = center(obj)
            point.z = 0
            anchors.append((obj.name, 'Wayfinding Sign', point))
    if parent_name == '03 Plaza and Street Furniture':
        anchors.append(('Kingdom of Neris Sign', 'Kingdom Sign', Vector((0, -265, 0))))
    groups = defaultdict(list)
    for obj in children:
        if obj.type not in {'MESH', 'CURVE', 'FONT', 'SURFACE'}:
            continue
        position = center(obj)
        anchor = min(anchors, key=lambda a: (a[2].xy - position.xy).length_squared)
        groups[anchor[0]].append(obj.name)
    for source, label, point in anchors:
        members = groups[source]
        if not members:
            continue
        # Group geometry can contain distinct lettering or authored fountain sizes.
        # Deduplication by evaluated geometry belongs to export, not name heuristics.
        yield Assembly(source, 'group:' + source, label, 'decoration',
                       Matrix.Translation(point), members)


def assemblies():
    result = []
    for obj in sorted(bpy.context.scene.objects, key=lambda o: o.name):
        if obj.parent:
            continue
        if obj.instance_collection:
            name = obj.instance_collection.name
            category = 'building' if name in HOMES or 'Castle' in name else 'decoration'
            result.append(Assembly(obj.name, 'collection:' + name,
                                   LABELS.get(name, name), category,
                                   obj.matrix_world.copy(), [obj.name], name))
        elif obj.name in LANDMARKS:
            result.append(Assembly(obj.name, 'root:' + obj.name, LANDMARKS[obj.name],
                                   'building', obj.matrix_world.copy(),
                                   [obj.name] + [c.name for c in obj.children_recursive]))
    result.extend(furniture_groups('03 Plaza and Street Furniture'))
    result.extend(furniture_groups('Waterfront Estate Garden Features'))
    return sorted(result, key=lambda a: (a.category, a.template, a.source))


def instance_owner(instance, by_source, by_member):
    if instance.is_instance and instance.parent:
        parent = instance.parent.original.name
        owner = by_source.get(parent) or by_member.get(parent)
        if owner:
            return owner
    return by_member.get(instance.object.original.name)


def inventory():
    items = assemblies()
    by_source = {a.source: a for a in items}
    by_member = {name: a for a in items if not a.collection for name in a.members}
    templates = {}
    samples = {}
    for item in items:
        if item.template not in samples:
            samples[item.template] = item.source
            templates[item.template] = {'label': item.label, 'category': item.category,
                                        'instances': 0, 'triangles': 0, 'materials': set(),
                                        'source': item.source}
        templates[item.template]['instances'] += 1
    unowned = defaultdict(int)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for instance in depsgraph.object_instances:
        obj = instance.object
        if obj.type not in {'MESH', 'CURVE', 'FONT', 'SURFACE'}:
            continue
        owner = instance_owner(instance, by_source, by_member)
        if not owner:
            original = obj.original
            while original.parent:
                original = original.parent
            unowned[original.name] += 1
            continue
        if owner.source != samples[owner.template]:
            continue
        mesh = obj.to_mesh()
        if mesh:
            mesh.calc_loop_triangles()
            record = templates[owner.template]
            record['triangles'] += len(mesh.loop_triangles)
            record['materials'].update(m.name for m in mesh.materials if m)
            obj.to_mesh_clear()
    for record in templates.values():
        record['materials'] = sorted(record['materials'])
    return items, templates, dict(unowned)


if __name__ == '__main__':
    import json
    from pathlib import Path

    items, templates, unowned = inventory()
    output = Path(__file__).resolve().parents[1] / 'Authoring'
    output.mkdir(exist_ok=True)
    report = {'schema': 1, 'source': bpy.data.filepath, 'instances': len(items),
              'templates': templates, 'unowned': unowned}
    (output / 'catalog-inventory.json').write_text(json.dumps(report, indent=2) + '\n',
                                                  encoding='utf-8')
    print('CATALOG', len(items), 'instances;', len(templates), 'templates;',
          sum(len(t['materials']) for t in templates.values()), 'material parts', flush=True)
    print('UNOWNED', json.dumps(unowned), flush=True)
