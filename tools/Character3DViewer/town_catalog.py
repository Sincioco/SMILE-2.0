"""Append published city templates while retaining every original town identity."""
import copy
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]/'games/SinStarI/SourceAssets/Towns/Neris'
BASE=ROOT/'NerisTownV1'
EXTENSION=ROOT/'NerisMetropolisV1'


def load_catalog():
    path=BASE/'Authoring/catalog.json'
    raw=path.read_bytes()
    result=json.loads(raw)
    extension=json.loads((EXTENSION/'Authoring/catalog-extension.json').read_text())
    if (hashlib.sha256(raw).hexdigest()!=extension['base_sha256'] or
        len(result['templates'])!=extension['base_templates'] or
        len(result['chunks'])!=extension['base_chunks']):
        raise ValueError('Metropolis extension must be republished against the current Neris catalog.')
    # Existing documents still name the original, immutable catalog family.
    result['document_fingerprint']=result.get('document_fingerprint') or hashlib.sha256(
        json.dumps(result,sort_keys=True).encode()).hexdigest()
    result['base_template_count']=len(result['templates'])
    result['base_chunk_count']=len(result['chunks'])
    result['templates']+=copy.deepcopy(extension['templates'])
    for chunk in extension['chunks']:
        entry=copy.deepcopy(chunk)
        entry['file']='../../NerisMetropolisV1/Authoring/'+entry['file']
        result['chunks'].append(entry)
    return result


def append_blender_templates(catalog):
    """Load library meshes only into an explicitly requested Blender export."""
    import bpy
    path=EXTENSION/'Blender/Neris-Metropolis.blend'
    with bpy.data.libraries.load(str(path),link=False) as (source,target):
        target.collections=['Metropolis Reusable Templates']
    collection=target.collections[0]
    samples=[]
    by_name={o['metropolis_template']:o for o in collection.objects if 'metropolis_template' in o}
    for template in catalog['templates'][catalog['base_template_count']:]:
        obj=by_name[template['label']]
        samples.append(dict(id=len(catalog['instances'])+len(samples),source=template['source'],
            template=template['id'],position=[0,0,0],rotation=[0,0,0],scale=[1,1,1],members=[obj.name]))
    catalog['instances']+=samples
