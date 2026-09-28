import {runBlender} from './blender-background.mjs';
await runBlender('D:/Projects/Sin-Star-I-Assets/Neris-Castle/native-integration/Neris-Town-Royal-Castle-r003.blend',String.raw`
import sys,json,hashlib
from pathlib import Path
sys.path.insert(0,'D:/SMILE 2.0/tools/Character3DViewer')
from town_document_codec import decode,unwrap,encode
from town_blender_files import import_document
root=Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle')
catalog=json.loads(Path('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/catalog.json').read_text())
for revision in ('r002','r003'):
    folder=root/'native-integration'
    native=decode(unwrap((folder/('Neris-Town-Royal-Castle-'+revision+'.town')).read_bytes()),catalog)
    imported=decode(import_document(folder/('Neris-Town-Royal-Castle-'+revision+'.blend'),catalog),catalog)
    assert native['sun']==imported['sun'] and native['cells']==imported['cells']
    assert len(native['items'])==len(imported['items'])
    actual={i['identity']:i for i in imported['items']}
    for item in native['items']:
        other=actual[item['identity']]
        assert item['template']==other['template']
        for key in ('position','scale'):
            assert max(abs(a-b) for a,b in zip(item[key],other[key]))<.001
    print('PASS Blender/native round trip',revision,len(native['items']),'items; saved Sun, terrain and transforms preserved',flush=True)
before=decode(unwrap((root/'checkpoints/Town-r003/before-road.town').read_bytes()),catalog)
after=decode(unwrap((root/'native-integration/Neris-Town-Royal-Castle-r003.town').read_bytes()),catalog)
assert before['items']==after['items'] and before['sun']==after['sun']
changed=0
for row in range(after['rows']):
    z=(after['zs'][row]+after['zs'][row+1])*.5
    for col in range(after['columns']):
        x=(after['xs'][col]+after['xs'][col+1])*.5;index=row*after['columns']+col
        if -4960<=x<-2280 and 820<=z<1012:assert after['cells'][index]==3
        else:assert before['cells'][index]==after['cells'][index]
        changed+=before['cells'][index]!=after['cells'][index]
assert changed==470 and 1012 in after['zs']
print('PASS only 470 promenade cells changed; full-width road meets drawbridge at Z=1012')
`, 'D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/Town-r003/round-trip.log');
