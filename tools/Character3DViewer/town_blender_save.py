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
from town_document_codec import decode, unwrap, respond, key_path, atomic_write, require_blender_support
from town_surface_layers import elevation
from town_surface_decks import footprints, rail_parts

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
    if catalog.get('royal_detail_revision'):
        sys.path.insert(0, str(TOWN / 'Source'))
        from royal_castle_detail import apply
        apply(catalog)
    originals = {name: bpy.data.objects[name] for source in catalog['instances']
                 for name in source['members']}
    samples = {}
    for source in catalog['instances']:
        samples.setdefault(source['template'], source)
    output = bpy.data.collections.new('Town Editor Instances')
    bpy.context.scene.collection.children.link(output)
    for item in document['items']:
        if 35 <= item['template'] <= 38:
            from town_blender_landforms import populate
            populate(item, item_matrix(item), output)
            continue
        source = samples[item['template']]
        delta = item_matrix(item) @ source_matrix(source).inverted()
        anchor = bpy.data.objects.new('Town Assembly %d' % item['identity'], None)
        output.objects.link(anchor)
        anchor.matrix_world = item_matrix(item)
        anchor['town_assembly'] = item['identity']
        anchor['town_template'] = item['template']
        copies = {}
        for name in source['members']:
            original = originals[name]
            clone = original.copy()
            clone.name = 'Town %d - %s' % (item['identity'], original.name)
            output.objects.link(clone)
            copies[original] = clone
        for original, clone in copies.items():
            clone.parent = copies.get(original.parent, anchor)
            clone.matrix_world = delta @ original.matrix_world
            clone['town_identity'] = item['identity']
            clone['town_member'] = original.name
            local = anchor.matrix_world.inverted() @ clone.matrix_world
            clone['town_local_matrix'] = [n for row in local for n in row]
    bpy.data.batch_remove(ids=list(originals.values()))


def terrain(document, catalog=None, patches=None):
    decks = footprints(document, catalog) if catalog else []
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

    def rail_box(a, b, c, d):
        # Closed boxes match native bridge-side walls and leave approaches open.
        base = len(vertices)
        vertices.extend((x/10, y/10, h) for h in (elevation('RAIL_BASE_Y'), elevation('RAIL_TOP_Y'))
                        for x, y in ((a, b), (c, b), (c, d), (a, d)))
        for indices in ((0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
            faces.append(tuple(base+i for i in indices))
            materials.append(3)
        faces.append((base+4, base+5, base+6, base+7))
        materials.append(4)

    def rail(a, b, c, d):
        vertical = abs(c-a) < abs(d-b)
        start = ((a+c)/2, b) if vertical else (a, (b+d)/2)
        finish = ((a+c)/2, d) if vertical else (c, (b+d)/2)
        for first, last in rail_parts(start, finish, decks):
            if vertical:
                rail_box(a, b+(d-b)*first, c, b+(d-b)*last)
            else:
                rail_box(a+(c-a)*first, b, a+(c-a)*last, d)

    def skirt(kind, neighbor, a, b, c, d):
        kind, neighbor = min(kind, 3), min(neighbor, 3)
        if kind == 0 or kind <= neighbor:
            return
        height = elevation('GROUND_Y' if kind == 1 else 'WATER_Y' if kind == 2 else 'ROAD_Y')
        base = len(vertices)
        vertices.extend(((a/10, b/10, elevation('BED_Y')), (a/10, b/10, height),
                         (c/10, d/10, height), (c/10, d/10, elevation('BED_Y'))))
        faces.append((base, base+1, base+2, base+3))
        materials.append(0 if kind == 1 else 5 if kind == 2 else 2)

    if patches is not None:
        from town_blender_terrain import mesh_data
        vertices, faces, materials = mesh_data(patches)
    elif document.get('curves'):
        raise ValueError('Curved terrain needs its prepared Studio geometry; export canceled.')
    else:
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
                    height = elevation('GROUND_Y' if kind == 1 else 'WATER_Y' if kind == 2 else 'ROAD_Y')
                    quad(xs[start], zs[z], xs[x], zs[z+1], height, material)
                    if kind == 2:
                        quad(xs[start]-1, zs[z]-1, xs[x]+1, zs[z+1]+1, elevation('BED_Y'), 5)
            for x in range(cols):
                kind = cell(x, z)
                skirt(kind, cell(x-1, z), xs[x], zs[z], xs[x], zs[z+1])
                skirt(kind, cell(x+1, z), xs[x+1], zs[z+1], xs[x+1], zs[z])
                skirt(kind, cell(x, z-1), xs[x+1], zs[z], xs[x], zs[z])
                skirt(kind, cell(x, z+1), xs[x], zs[z+1], xs[x+1], zs[z+1])
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
    bed = bpy.data.materials.new('Town Submerged Water Bed')
    bed.use_nodes = True
    shader = next(n for n in bed.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    shader.inputs['Base Color'].default_value = (.017, .076, .258, 1)
    shader.inputs['Roughness'].default_value = 1
    # Reload current lawn textures without changing the immutable assembly template.
    for image in list(bpy.data.images):
        if image.name.startswith(('Neris-Grass-Color', 'Neris-Grass-Normal')):
            stem = 'Neris-Grass-Normal' if 'Normal' in image.name else 'Neris-Grass-Color'
            replacement = bpy.data.images.load(str(TOWN / 'Textures' / (stem + '.png')), check_existing=False)
            if 'Normal' in stem:
                replacement.colorspace_settings.name = 'Non-Color'
            replacement.pack()
            for mat in bpy.data.materials:
                if mat.node_tree:
                    for node in mat.node_tree.nodes:
                        if node.type == 'TEX_IMAGE' and node.image == image:
                            node.image = replacement
    for name in ('Town Grass', 'Royal Deep Blue Water', 'Town Paving', 'Pale Carved Stone', 'Aged Gold', 'Town Submerged Water Bed'):
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
    image = bpy.data.images.load(str(paving_path), check_existing=False)
    image.pack()
    texture = paving.node_tree.nodes.new('ShaderNodeTexImage')
    texture.image = image
    texture.extension = 'REPEAT'
    shader = next(n for n in paving.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    paving.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
    shader.inputs['Roughness'].default_value = .38
    shader.inputs['Metallic'].default_value = 0
    for link in list(shader.inputs['Normal'].links):
        paving.node_tree.links.remove(link)
    obj = bpy.data.objects.new('Town Editable Surface', mesh)
    from town_blender_landforms import terrain_style
    terrain_style(document, mesh)
    bpy.context.scene.collection.objects.link(obj)


def lighting(document):
    for obj in list(bpy.context.scene.objects):
        if obj.type == 'LIGHT':
            bpy.data.objects.remove(obj, do_unlink=True)
    red, green, blue, intensity, ambient, azimuth, elevation, shadows = document['sun'][:8]
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
        require_blender_support(document)
        respond(data_folder, rid, 0, 5, 'Opening saved template scene...')
        source = TOWN / catalog.get('blend_source', 'Authoring/Catalog.blend')
        if hashlib.sha256(source.read_bytes()).hexdigest() != catalog['source_sha256']:
            raise ValueError('Immutable template scene checksum differs')
        target = destination(document, output_root)
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists() and document['mode'] == 2:
            raise ValueError('That Blender version already exists')
        bpy.ops.wm.open_mainfile(filepath=str(source))
        populate_items(document, catalog)
        respond(data_folder, rid, 0, 35, 'Rebuilding surfaces and water banks...')
        terrain(document, catalog)
        lighting(document)
        from town_blender_landmarks import populate
        populate(document)
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
