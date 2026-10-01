"""Install prepared towns after Studio closes, preserving edits and every replaced key."""
import argparse
import hashlib
import json
from pathlib import Path
from town_design import CATALOG, atomic_write, decode, encode, unwrap
from town_document_codec import key_path, prepared_records


def same_layout(first, second):
    ignored = {'payload', 'dirty', 'sun', 'presets', 'map_tiles', 'terrain_style'}
    return {k: v for k, v in first.items() if k not in ignored} == {
        k: v for k, v in second.items() if k not in ignored}


def destinations(doc):
    return {tile['destination'] for tile in doc.get('map_tiles', [])}


def plan_town(data, path, baseline, changes, replace_night=False):
    raw = path.read_bytes()
    payload = unwrap(raw)
    incoming = decode(payload, CATALOG)
    name = incoming['name']
    records = prepared_records(raw)
    if records.get('.PreparedVersion') not in (bytes([0, 0, 0, 3]), bytes([0, 0, 0, 4]), bytes([0, 0, 0, 5])):
        raise ValueError('Map must be prepared before installation: ' + name)
    original = name == 'Neris Town'
    old = incoming if original else decode(unwrap((baseline / path.name).read_bytes()), CATALOG)
    keys = [('TownEditor.PermanentNeris' if original else 'TownEditor.Permanent.' + name),
            'TownEditor.Town.' + name, 'TownEditor.Recovery.' + name]
    # Original Neris uses the permanent key. Do not invent extra saved revisions.
    if original:
        keys = [keys[0]]
    reference = 'TownPrepared.Installed.' + hashlib.sha256(raw).hexdigest()
    for suffix, part in records.items():
        changes[reference + suffix] = part
    changes[reference + '.Terrain.Input'] = payload
    changes[reference + '.Roads.Input'] = payload
    for key in keys:
        target = key_path(data, key)
        doc = dict(incoming)
        if target.exists():
            current = decode(unwrap(target.read_bytes()), CATALOG)
            if not (same_layout(current, old) or same_layout(current, incoming)):
                raise ValueError('Retaining newer user layout; merge required: ' + key)
            current_links, incoming_links = destinations(current), destinations(incoming)
            added_original_link = False
            if original and current_links <= incoming_links:
                graph = json.loads((Path(__file__).resolve().parent.parent / 'world-layout.json').read_text(encoding='utf-8'))
                names = [node[0] for node in graph['nodes']]
                index = names.index(name)
                atlas_links = {names[b if a == index else a] for a,b in graph['links'] if index in (a,b)}
                added_original_link = incoming_links - current_links <= atlas_links
            if current_links != incoming_links and not added_original_link:
                raise ValueError('Retaining changed travel destinations; merge required: ' + key)
            doc['sun'] = current['sun']
            if current.get('terrain_style', 0) != old.get('terrain_style', 0):
                doc['terrain_style'] = current['terrain_style']
            if 'presets' in current:
                doc['presets'] = current['presets']
            if replace_night:
                doc['presets']['night'] = incoming['presets']['night']
                if doc['presets']['night_active']:
                    doc['sun'] = doc['presets']['night'].copy()
        changes[key] = encode(doc, CATALOG)
    # Pointer is installed after its immutable companions and the map revisions.
    changes['TownPrepared.Bundle.' + name] = reference.encode('ascii')
    return name


def install(data, backup, original=None, source=None, baseline=None, dry_run=False, maps=None,
            night_presets=()):
    folder = Path(__file__).resolve().parent.parent
    source = source or folder / 'Towns'
    baseline = baseline or source
    manifest = json.loads((folder / 'manifest.json').read_text(encoding='utf-8'))
    if maps:
        unknown = set(maps) - {record['name'] for record in manifest}
        if unknown:
            raise ValueError('Unknown maps: ' + ', '.join(sorted(unknown)))
        manifest = [record for record in manifest if record['name'] in maps]
    changes, names = {}, []
    for record in manifest:
        path = source / record['file']
        name = plan_town(data, path, baseline, changes, record['name'] in night_presets)
        if name != record['name']:
            raise ValueError('Map name differs from manifest: ' + str(path))
        names.append(name)
    if original is not None:
        name = plan_town(data, original, baseline, changes)
        if name != 'Neris Town':
            raise ValueError('Original map must be Neris Town')
        names.append(name)
    atlas_key = 'TownEditor.World.Luma - Story Atlas'
    if not key_path(data, atlas_key).exists():
        changes[atlas_key] = unwrap((folder / 'Luma - Story Atlas.world').read_bytes())
    previous = {key: key_path(data, key).read_bytes() if key_path(data, key).exists() else None
                for key in changes}
    if dry_run:
        print('Preflight passed:', len(names), 'towns;', len(changes), 'records. No saves changed.')
        return changes
    if backup.exists() and any(backup.iterdir()):
        raise ValueError('Choose a fresh backup directory; existing backup retained.')
    backup.mkdir(parents=True, exist_ok=True)
    for key, raw in previous.items():
        if raw is not None:
            (backup / key_path(data, key).name).write_bytes(raw)
    # Check every input again before the first mutation; Studio must be closed.
    for key, raw in previous.items():
        target = key_path(data, key)
        if (target.read_bytes() if target.exists() else None) != raw:
            raise ValueError('Save changed during preflight; close Studio and retry: ' + key)
    for key, payload in changes.items():
        atomic_write(key_path(data, key), payload)
    (backup / 'installed-keys.json').write_text(json.dumps(list(changes), indent=2) + '\n')
    for name in names:
        print('Installed prepared', name)
    return changes


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', type=Path, required=True)
    parser.add_argument('--backup', type=Path, required=True)
    parser.add_argument('--source', type=Path, help='Prepared generated map folder')
    parser.add_argument('--baseline', type=Path, help='Previous authored maps for detecting user layout edits')
    parser.add_argument('--original', type=Path, help='Prepared original Neris with relocated travel markers only')
    parser.add_argument('--dry-run', action='store_true')
    parser.add_argument('--map', action='append', dest='maps', help='Install only this generated map; may repeat')
    parser.add_argument('--night-preset', action='append', default=[],
                        help='Explicitly replace only this map night preset; keep its day settings')
    args = parser.parse_args()
    install(args.data, args.backup, args.original, args.source, args.baseline, args.dry_run,
            args.maps, args.night_preset)
