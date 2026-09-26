"""Blender background entry point. Rebuild an explicit document from immutable templates."""
import hashlib
import json
import math
import re
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from town_document_codec import decode, unwrap, respond, key_path, atomic_write

ROOT = Path(__file__).resolve().parents[2]
TOWN = ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1'


def item_matrix(item):
    x, y, z = item['position']
    sx, sy, sz = item['scale']
    return (Matrix.Translation(Vector((x/10, z/10, (y-21)/10))) @
            Matrix.Rotation(math.radians(-item['yaw']), 4, 'Z') @
            Matrix.Diagonal((sx/1000, sz/1000, sy/1000, 1)))


def source_matrix(source):
    from mathutils import Euler
    return (Matrix.Translation(Vector(source['position'])) @
            Euler(source['rotation']).to_matrix().to_4x4() @
            Matrix.Diagonal((*source['scale'], 1)))


def populate_items(document, catalog):
    originals = {name: bpy.data.objects[name] for source in catalog['instances']
                 for name in source['members']}
    samples = {}
    for source in catalog['instances']:
        samples.setdefault(source['template'], source)
    output = bpy.data.collections.new('Town Editor Instances')
    bpy.context.scene.collection.children.link(output)
    for item in document['items']:
        source = samples[item['template']]
        delta = item_matrix(item) @ source_matrix(source).inverted()
        copies = {}
        for name in source['members']:
            original = originals[name]
            clone = original.copy()
            clone.name = 'Town %d - %s' % (item['identity'], original.name)
            output.objects.link(clone)
            copies[original] = clone
        for original, clone in copies.items():
            clone.parent = copies.get(original.parent)
            clone.matrix_world = delta @ original.matrix_world
            clone['town_identity'] = item['identity']
    bpy.data.batch_remove(ids=list(originals.values()))


def terrain(document):
    for obj in list(bpy.context.scene.objects):
        root = obj
        while root.parent:
            root = root.parent
        if root.name in ('Waterfront Terrain and Bridges', 'Waterfront Grout',
                         'Waterfront Slate Tiles', 'Neris Lawn Blade Batch'):
            # Resolve the full deletion list before unlinking parents.
            obj['town_remove_terrain'] = True
    bpy.data.batch_remove(ids=[obj for obj in bpy.context.scene.objects if obj.get('town_remove_terrain')])
    vertices, faces, materials = [], [], []
    cols, rows, cells = document['columns'], document['rows'], document['cells']
    xs, zs = document['xs'], document['zs']

    def cell(x, z):
        return cells[z*cols+x] if 0 <= x < cols and 0 <= z < rows else 0

    def quad(a, b, c, d, height, material):
        base = len(vertices)
        vertices.extend(((a/10, b/10, height), (c/10, b/10, height),
                         (c/10, d/10, height), (a/10, d/10, height)))
        faces.append((base, base+1, base+2, base+3))
        materials.append(material)

    def rail(a, b, c, d):
        # Closed boxes match native bridge-side walls and leave approaches open.
        base = len(vertices)
        vertices.extend((x/10, y/10, h) for h in (.22, 1.23)
                        for x, y in ((a, b), (c, b), (c, d), (a, d)))
        for indices in ((0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
            faces.append(tuple(base+i for i in indices))
            materials.append(3)
        faces.append((base+4, base+5, base+6, base+7))
        materials.append(4)

    for z in range(rows):
        x = 0
        while x < cols:
            start, kind = x, cell(x, z)
            x += 1
            while x < cols and cell(x, z) == kind:
                x += 1
            if kind:
                material = 0 if kind == 1 else 1 if kind == 2 else 2
                # Keep the authored lawn below the castle/HQ floors, not coplanar.
                height = -.01 if kind == 1 else .085 if kind == 2 else .212
                quad(xs[start], zs[z], xs[x], zs[z+1], height, material)
        for x in range(cols):
            if cell(x, z) == 4:
                if cell(x-1, z) == 2:
                    rail(xs[x]-2.25, zs[z], xs[x]+2.25, zs[z+1])
                if cell(x+1, z) == 2:
                    rail(xs[x+1]-2.25, zs[z], xs[x+1]+2.25, zs[z+1])
                if cell(x, z-1) == 2:
                    rail(xs[x], zs[z]-2.25, xs[x+1], zs[z]+2.25)
                if cell(x, z+1) == 2:
                    rail(xs[x], zs[z+1]-2.25, xs[x+1], zs[z+1]+2.25)
    mesh = bpy.data.meshes.new('Town Editable Surface')
    mesh.from_pydata(vertices, [], faces)
    for name in ('Town Grass', 'Royal Deep Blue Water', 'Town Paving', 'Pale Carved Stone', 'Aged Gold'):
        mesh.materials.append(bpy.data.materials[name])
    for poly, material in zip(mesh.polygons, materials):
        poly.material_index = material
    # Preserve the same two-metre grid as the native surface without per-tile meshes.
    uv = mesh.uv_layers.new(name='Town Grid')
    for loop in mesh.loops:
        point = mesh.vertices[loop.vertex_index].co
        uv.data[loop.index].uv = (point.x / 2, point.y / 2)
    paving_path = ROOT / 'tools/Character3DViewer/Assets/Neris/Neris-Paving.png'
    if not paving_path.is_file():
        raise ValueError('Paving texture missing; build the Viewer before saving')
    paving = bpy.data.materials['Town Paving']
    image = bpy.data.images.load(str(paving_path), check_existing=True)
    image.pack()
    texture = paving.node_tree.nodes.new('ShaderNodeTexImage')
    texture.image = image
    texture.extension = 'REPEAT'
    shader = next(n for n in paving.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    paving.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
    obj = bpy.data.objects.new('Town Editable Surface', mesh)
    bpy.context.scene.collection.objects.link(obj)


def lighting(document):
    for obj in list(bpy.context.scene.objects):
        if obj.type == 'LIGHT':
            bpy.data.objects.remove(obj, do_unlink=True)
    red, green, blue, intensity, ambient, azimuth, elevation, shadows = document['sun']
    sun = bpy.data.lights.new('Town Editor Sun', 'SUN')
    sun.color = (red/255, green/255, blue/255)
    sun.energy = intensity / 100
    sun.use_shadow = bool(shadows)
    obj = bpy.data.objects.new('Town Editor Sun', sun)
    bpy.context.scene.collection.objects.link(obj)
    direction = Vector((math.sin(math.radians(azimuth))*math.cos(math.radians(elevation)),
                        math.cos(math.radians(azimuth))*math.cos(math.radians(elevation)),
                        math.sin(math.radians(elevation))))
    obj.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    world = bpy.context.scene.world
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = ambient / 100


def destination(document, output_root):
    if document['mode'] == 1 and document['name'] == 'Neris Town':
        return output_root / 'Blend/Neris-Town-Waterfront.blend'
    safe = re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', document['name']).strip(' .')[:65] or 'Town'
    suffix = hashlib.sha256(document['name'].encode('utf-8')).hexdigest()[:12]
    result = output_root / 'Versions' / (safe + '-' + suffix + '.blend')
    if not result.resolve().is_relative_to(output_root.resolve()):
        raise ValueError('Invalid output path')
    return result


def main():
    args = sys.argv[sys.argv.index('--')+1:]
    request, data_folder = map(Path, args[:2])
    output_root = Path(args[2]) if len(args) > 2 else TOWN
    catalog = json.loads((TOWN / 'Authoring/catalog.json').read_text(encoding='utf-8'))
    document = decode(unwrap(request.read_bytes()), catalog, request=True)
    rid = document['request_id']
    try:
        respond(data_folder, rid, 0, 5, 'Opening saved template scene...')
        source = TOWN / 'Authoring/Catalog.blend'
        if hashlib.sha256(source.read_bytes()).hexdigest() != catalog['source_sha256']:
            raise ValueError('Immutable template scene checksum differs')
        target = destination(document, output_root)
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists() and document['mode'] == 2:
            raise ValueError('That Blender version already exists')
        bpy.ops.wm.open_mainfile(filepath=str(source))
        populate_items(document, catalog)
        respond(data_folder, rid, 0, 35, 'Rebuilding surfaces and water banks...')
        terrain(document)
        lighting(document)
        scene = bpy.context.scene
        scene['town_editor_name'] = document['name']
        scene['town_editor_request'] = rid
        scene['town_editor_items'] = len(document['items'])
        scene['town_editor_document_sha256'] = hashlib.sha256(document['payload']).hexdigest()
        snapshot = bpy.data.texts.new('Town Editor Document.json')
        snapshot.write(json.dumps({k: v for k, v in document.items() if k != 'payload'}, ensure_ascii=False))
        bpy.context.preferences.filepaths.save_version = 0
        staged = target.with_name(target.stem + '.pending.blend')
        respond(data_folder, rid, 0, 70, 'Writing and verifying Blender file...')
        bpy.ops.wm.save_as_mainfile(filepath=str(staged), compress=True)
        bpy.ops.wm.open_mainfile(filepath=str(staged))
        assert bpy.context.scene['town_editor_request'] == rid
        assert len({o['town_identity'] for o in bpy.context.scene.objects if 'town_identity' in o}) == len(document['items'])
        if target.exists() and document['mode'] == 2:
            raise ValueError('That Blender version already exists')
        staged.replace(target)
        # Preserve the matching Viewer snapshot even if the user closed the Viewer.
        atomic_write(key_path(data_folder, 'TownEditor.Town.' + document['name']), document['payload'])
        respond(data_folder, rid, 1, 100, 'Saved Blender and Viewer: ' + document['name'])
        print('PASS Blender save:', target, flush=True)
    except Exception as error:
        respond(data_folder, rid, 2, 0, str(error))
        raise


if __name__ == '__main__':
    main()
