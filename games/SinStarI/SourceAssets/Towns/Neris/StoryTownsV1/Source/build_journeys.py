"""Build only the six journey/story maps and update their manifest entries."""
import json
from pathlib import Path
from journey_layouts import BUILDERS
from town_access import prepare
folder=Path(__file__).resolve().parent.parent
records=json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
for make in BUILDERS:
    town=prepare(make())
    record=town.save(folder/'Towns')
    records=[r for r in records if r['name']!=record['name']]+[record]
    print(json.dumps(record),flush=True)
(folder/'manifest.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
