"""Install generated native maps after Studio closes; back up each replaced key."""
import argparse
import json
from pathlib import Path
from town_design import CATALOG, atomic_write, decode, unwrap
from town_document_codec import key_path


def install(data, backup, original=None):
    folder = Path(__file__).resolve().parent.parent
    backup.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((folder / 'manifest.json').read_text(encoding='utf-8'))
    for record in manifest:
        payload = unwrap((folder / 'Towns' / record['file']).read_bytes())
        doc = decode(payload, CATALOG)
        assert doc['name'] == record['name']
        keys = ['TownEditor.Town.' + doc['name'], 'TownEditor.Recovery.' + doc['name']]
        if doc['name'] in ('Neris Spaceport', 'Horizon Airport'):
            keys.append('TownEditor.Permanent.' + doc['name'])
        for key in keys:
            target = key_path(data, key)
            if target.exists():
                saved = backup / target.name
                if saved.exists():
                    raise ValueError('Choose a fresh backup directory; existing backup retained.')
                saved.write_bytes(target.read_bytes())
            atomic_write(target, payload)
        print('Installed', doc['name'])
    payload = unwrap((folder / 'Luma - Story Atlas.world').read_bytes())
    target = key_path(data, 'TownEditor.World.Luma - Story Atlas')
    if target.exists():
        (backup / target.name).write_bytes(target.read_bytes())
    atomic_write(target, payload)
    if original is not None:
        payload = unwrap(original.read_bytes())
        incoming = decode(payload, CATALOG)
        target = key_path(data, 'TownEditor.PermanentNeris')
        current = decode(unwrap(target.read_bytes()), CATALOG)
        assert incoming['name'] == current['name'] == 'Neris Town'
        # A stale prepared copy must never replace newer user map edits.
        assert all(incoming[k] == v for k,v in current.items()
                   if k not in ('payload','dirty','map_tiles'))
        assert all(tile in incoming['map_tiles'] for tile in current['map_tiles'])
        (backup / target.name).write_bytes(target.read_bytes())
        atomic_write(target, payload)
        print('Added original Neris destinations; existing terrain, items and markers retained.')
    print('Installed separate Luma - Story Atlas; original Luma world retained.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', type=Path, required=True)
    parser.add_argument('--backup', type=Path, required=True)
    parser.add_argument('--original', type=Path, help='Prepared original Neris with additive travel markers only')
    args = parser.parse_args()
    install(args.data, args.backup, args.original)
