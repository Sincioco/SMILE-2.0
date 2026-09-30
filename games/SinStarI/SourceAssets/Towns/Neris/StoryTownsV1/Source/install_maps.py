"""Install generated native maps after Studio closes; back up each replaced key."""
import argparse
import json
from pathlib import Path
from town_design import CATALOG, atomic_write, decode, unwrap
from town_document_codec import key_path


def install(data, backup):
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
    print('Installed separate Luma - Story Atlas; original Neris and Luma retained.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', type=Path, required=True)
    parser.add_argument('--backup', type=Path, required=True)
    args = parser.parse_args()
    install(args.data, args.backup)
