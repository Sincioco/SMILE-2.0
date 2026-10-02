"""Explicit .blend save/open jobs. Reuse Neris assembly/surface/light conversion."""
import copy
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix

sys.path.insert(0, str(Path(__file__).resolve().parent))
from town_blender_save import TOWN, populate_items, terrain, lighting
from town_document_codec import decode, encode, unwrap, respond, key_path, atomic_write, landmark, require_blender_support
from town_blender_terrain import read_prepared


def matrix_close(a, b):
    return max(abs(a[i][j] - b[i][j]) for i in range(4) for j in range(4)) < .001


def import_document(path, catalog):
    bpy.ops.wm.open_mainfile(filepath=str(path), use_scripts=False)
    snapshot = bpy.data.texts.get('Town Editor Document.json')
    if snapshot is None:
        raise ValueError('Choose a town saved by the Town Editor.')
    document = json.loads(snapshot.as_string())
    payload = encode(document, catalog)
    if hashlib.sha256(payload).hexdigest() != bpy.context.scene.get('town_editor_document_sha256'):
        raise ValueError('Blender town metadata differs; import canceled.')
    anchors = [o for o in bpy.context.scene.objects if 'town_assembly' in o]
    if anchors:
        previous = {i['identity']: i for i in document['items']}
        items, identities = [], set()
        next_id = max(previous, default=0) + 1
        samples = {}
        for source in catalog['instances']:
            samples.setdefault(source['template'], source)
        for anchor in anchors:
            original = previous.get(anchor['town_assembly'])
            if original is None or anchor.get('town_template') != original['template']:
                raise ValueError('Unknown Blender assembly; import canceled.')
            members = [o for o in anchor.children_recursive if 'town_member' in o]
            if not members:
                continue  # Deleting a complete assembly removes it from the town.
            if 35 <= original['template'] <= 39:
                from town_blender_landforms import member_names
                sample = {'members': member_names(original['template'])}
            else:
                sample = samples[original['template']]
            actual_members = sorted(o['town_member'] for o in members)
            expected_members = sorted(sample['members'])
            previous_members = sorted(sample['members'] + sample.get('compatible_retired_members', []))
            if actual_members not in (expected_members, previous_members):
                raise ValueError('Incomplete assembly; move/delete the whole Town Assembly.')
            for member in members:
                values = member['town_local_matrix']
                local = Matrix([values[i:i+4] for i in range(0, 16, 4)])
                if not matrix_close(anchor.matrix_world @ local, member.matrix_world):
                    raise ValueError('Move the Town Assembly, not its individual parts.')
            position, rotation, scale = anchor.matrix_world.decompose()
            angles = rotation.to_euler('XYZ')
            if abs(angles.x) > .0001 or abs(angles.y) > .0001 or min(scale) <= 0:
                raise ValueError('Town assemblies support positive scale and upright rotation.')
            item = copy.deepcopy(original)
            if item['identity'] in identities:
                item['identity'], next_id = next_id, next_id + 1
            identities.add(item['identity'])
            item['position'] = [position.x*10, position.z*10+21, position.y*10]
            item['scale'] = [scale.x*1000, scale.z*1000, scale.y*1000]
            item['yaw'] = -math.degrees(angles.z)
            items.append(item)
        document['items'] = items
    else:
        # Earlier editor exports contain the full saved document but no movable anchors.
        # Entire deleted assemblies are still recoverable without guessing part transforms.
        present = {o['town_identity'] for o in bpy.context.scene.objects if 'town_identity' in o}
        document['items'] = [i for i in document['items'] if i['identity'] in present]
    document['landmark'] = landmark(document)
    document['name'] = path.stem[:80]
    return encode(document, catalog)


def export_document(document, catalog, target, request_id, status, patches=None):
    require_blender_support(document, patches)
    source = TOWN / catalog.get('blend_source', 'Authoring/Catalog.blend')
    if hashlib.sha256(source.read_bytes()).hexdigest() != catalog['source_sha256']:
        raise ValueError('Immutable template scene checksum differs.')
    bpy.ops.wm.open_mainfile(filepath=str(source), use_scripts=False)
    populate_items(document, catalog)
    status(35, 'Rebuilding town surfaces and lighting...')
    terrain(document, catalog, patches)
    lighting(document)
    from town_blender_landmarks import populate
    populate(document)
    # Normalize saved status before recording the portable document checksum.
    payload = encode(document, catalog)
    document = decode(payload, catalog)
    scene = bpy.context.scene
    scene['town_editor_name'] = document['name']
    scene['town_editor_request'] = request_id
    scene['town_editor_items'] = len(document['items'])
    scene['town_editor_document_sha256'] = hashlib.sha256(payload).hexdigest()
    snapshot = bpy.data.texts.new('Town Editor Document.json')
    snapshot.write(json.dumps({k: v for k, v in document.items() if k != 'payload'}, ensure_ascii=False))
    bpy.context.preferences.filepaths.save_version = 0
    staged = target.with_name(target.stem + '.%d.pending.blend' % request_id)
    try:
        status(75, 'Writing and verifying selected Blender file...')
        bpy.ops.wm.save_as_mainfile(filepath=str(staged), compress=True)
        bpy.ops.wm.open_mainfile(filepath=str(staged), use_scripts=False)
        if bpy.context.scene['town_editor_request'] != request_id:
            raise ValueError('Saved Blender verification failed.')
        staged.replace(target)
    finally:
        staged.unlink(missing_ok=True)


def main():
    mode, request_id, chosen, folder = sys.argv[sys.argv.index('--')+1:]
    mode, request_id, path, folder = int(mode), int(request_id), Path(chosen), Path(folder)
    catalog = json.loads((TOWN / 'Authoring/catalog.json').read_text(encoding='utf-8'))
    def status(progress, message):
        respond(folder, request_id, 0, progress, message, 'TownEditor.File.Response')
    try:
        status(10, 'Opening Blender in background...')
        if mode == 3:
            payload = unwrap(key_path(folder, 'TownEditor.File.Snapshot.%d' % request_id).read_bytes())
            document = decode(payload, catalog)
            patches = None
            if (document.get('curves') or any(document.get('heights', [])) or
                    any(document.get('appearance', []))):
                patches = read_prepared(lambda suffix: unwrap(key_path(
                    folder, 'TownEditor.File.Snapshot.%d%s' % (request_id, suffix)).read_bytes()))
            export_document(document, catalog, path, request_id, status, patches)
        elif mode == 4:
            payload = import_document(path, catalog)
            atomic_write(key_path(folder, 'TownEditor.File.Opened.%d' % request_id), payload)
        else:
            raise ValueError('Unsupported file operation.')
        respond(folder, request_id, 1, 100, 'Blender file ready.', 'TownEditor.File.Response')
        print('PASS Blender file:', path, flush=True)
    except Exception as error:
        respond(folder, request_id, 2, 0, str(error), 'TownEditor.File.Response')
        raise


if __name__ == '__main__':
    main()
