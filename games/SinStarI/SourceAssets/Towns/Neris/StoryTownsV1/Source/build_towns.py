"""Reproduce Luma's editable native towns without external dependencies."""
import json
from pathlib import Path
from capital_layouts import canals, star_lake, crown_isles
from village_layouts import east_valley, home_village
from airport_layouts import spaceport, airport
from town_access import prepare

folder=Path(__file__).resolve().parent.parent
results=json.loads((folder/'manifest.json').read_text(encoding='utf-8'))
for make in (canals,star_lake,crown_isles,east_valley,home_village,spaceport,airport):
    town=prepare(make())
    result=town.save(folder/'Towns')
    results=[r for r in results if r['name']!=result['name']]+[result]
    print(json.dumps(result),flush=True)
(folder/'manifest.json').write_text(json.dumps(results,indent=2)+'\n',encoding='utf-8')
