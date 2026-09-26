"""Focused real Blender new-version/overwrite protection/save-back acceptance."""
import hashlib
import json
import os
import subprocess
import sys
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIEWER = ROOT / 'tools/Character3DViewer'
sys.path.insert(0, str(VIEWER))
from town_document_codec import key_path, unwrap, decode, envelope, integer

town = ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1'
catalog = json.loads((town / 'Authoring/catalog.json').read_text(encoding='utf-8'))
appid = hashlib.sha256(b'smile.tests.town-editor').hexdigest()
data = Path(os.environ['LOCALAPPDATA']) / 'SMILE 2.0/Games' / appid / 'Data'
source = unwrap(key_path(data, 'TownEditor.Blender.Request').read_bytes())
document = decode(source, catalog, request=True)
test_root = ROOT / 'artifacts/tests' / ('town-blender-' + uuid.uuid4().hex[:10])
test_root.mkdir(parents=True)
request = test_root / 'request.bin'
blender = sorted(Path('C:/Program Files/Blender Foundation').glob('*/blender.exe'))[-1]
canonical = town / 'Blend/Neris-Town-Waterfront.blend'
before = hashlib.sha256(canonical.read_bytes()).digest()


def run(mode, request_id, expected):
    request.write_bytes(envelope(document['payload'] + integer(mode) + integer(request_id)))
    result = subprocess.run([str(blender), '--background', '--python-exit-code', '1',
                             '--python', str(VIEWER / 'town_blender_save.py'), '--',
                             str(request), str(test_root / 'Data'), str(test_root)],
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=120)
    (test_root / ('run-%d.log' % request_id)).write_bytes(result.stdout)
    assert (result.returncode == 0) == expected, result.stdout.decode('utf-8', errors='replace')[-3000:]


run(2, 1, True)
saved = list((test_root / 'Versions').glob('*.blend'))
assert len(saved) == 1
first = hashlib.sha256(saved[0].read_bytes()).digest()
run(2, 2, False)
assert hashlib.sha256(saved[0].read_bytes()).digest() == first, 'Duplicate version overwrote the original'
run(1, 3, True)
assert hashlib.sha256(saved[0].read_bytes()).digest() != first, 'Save-back did not replace the verified snapshot'
assert unwrap(key_path(test_root / 'Data', 'TownEditor.Town.' + document['name']).read_bytes()) == document['payload']
assert hashlib.sha256(canonical.read_bytes()).digest() == before, 'Acceptance modified the canonical scene'
print('PASS Blender new version, duplicate protection, save-back and matching Viewer snapshot')
print(test_root)
