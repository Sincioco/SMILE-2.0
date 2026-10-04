"""Publish append-only Studio templates without rebuilding the original Neris catalog."""
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import struct
import sys
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT.parent/'NerisTownV1'
sys.path.insert(0,str(BASE/'Source'))
from static_glb import write
from export_catalog import split_parts


def publish():
    base=json.loads((BASE/'Authoring/catalog.json').read_text())
    output=ROOT/'Authoring'
    (output/'Templates').mkdir(exist_ok=True)
    templates,parts=[],[]
    collection=bpy.data.collections['Metropolis Reusable Templates']
    for obj in collection.objects:
        if 'metropolis_template' not in obj:
            continue
        mesh=obj.data
        mesh.calc_loop_triangles()
        groups={}
        for triangle in mesh.loop_triangles:
            material=mesh.materials[triangle.material_index]
            group=groups.setdefault(material.name,[material,[]])
            corners=tuple((tuple(mesh.vertices[mesh.loops[i].vertex_index].co),
                           tuple(mesh.corner_normals[i].vector)) for i in triangle.loops)
            a,b,c=[Vector(v[0]) for v in corners]
            if (b-a).cross(c-a).length_squared>1e-12:
                group[1].append(corners)
        low=[min(v.co[i] for v in mesh.vertices) for i in range(3)]
        high=[max(v.co[i] for v in mesh.vertices) for i in range(3)]
        boxes=json.loads(obj['metropolis_solids'])
        label=obj['metropolis_template']
        # Entire open complexes must not become solid square blocks. Explicit
        # ground-contact boxes preserve the spaces between towers and bridge decks.
        if not boxes and label=='Marina Bay Sands':
            boxes=[[[x-24,-35,0],[x+24,19,190]] for x in (-82,0,82)]
        elif not boxes and label=='Petronas Twin Towers':
            boxes=[[[x-26,-26,0],[x+26,26,400]] for x in (-44,44)]
        elif not boxes and label=='Neris Tide Arcology':
            boxes=[[[x-30,-27,0],[x+30,27,430]] for x in (-43,43)]
        elif not boxes:
            boxes=[[low,high]]
        tid=len(base['templates'])+len(templates)
        record=dict(id=tid,source='metropolis:'+label,label=label,
            category='decoration' if label.startswith('Park Grove') else 'building',
            bounds=[low,high],parts=[],doors=[],steps=[],water=[],
            solids=[[lo[0],lo[1],hi[0],hi[1],hi[2]] for lo,hi in boxes],
            camera=[lo+hi for lo,hi in boxes],
            floors=[[lo[0],lo[1],hi[0],hi[1],hi[2]]
                    for lo,hi in json.loads(obj['metropolis_floors'])],
            sample=label)
        templates.append(record)
        for part in split_parts(groups):
            parts.append((tid,part))
    batches,counts=[],[]
    for tid,part in sorted(parts,key=lambda p:p[1][3],reverse=True):
        destination=next((i for i,count in enumerate(counts)
            if count+part[3]<=125000 and len(batches[i])<16 and
            sum(len(p[1][2]) for p in batches[i])+len(part[2])<=130000),None)
        if destination is None:
            destination=len(batches)
            batches.append([])
            counts.append(0)
        templates[tid-len(base['templates'])]['parts'].append(
            [len(base['chunks'])+destination,len(batches[destination])])
        batches[destination].append((tid,part))
        counts[destination]+=part[3]
    chunks=[]
    for index,batch in enumerate(batches,len(base['chunks'])):
        filename=f'Templates/Catalog-{index:02d}.glb'
        write(output/filename,[part for _,part in batch])
        chunks.append(dict(file=filename,parts=len(batch),
            triangles=sum(len(part[2]) for _,part in batch),
            sha256=hashlib.sha256((output/filename).read_bytes()).hexdigest()))
    catalog=dict(schema=1,base_templates=len(base['templates']),base_chunks=len(base['chunks']),
        base_sha256=hashlib.sha256((BASE/'Authoring/catalog.json').read_bytes()).hexdigest(),
        blend_source='Blender/Neris-Metropolis.blend',templates=templates,chunks=chunks)
    (output/'catalog-extension.json').write_text(json.dumps(catalog,indent=2)+'\n')
    (output/'Catalog.sm3d.json').write_text('{"version":1}\n')
    # Data-only slot classification lets Studio own day/night and beacon timing.
    styles=['Glacier','Azure','Sapphire','Cerulean','SilverBlue','Midnight']
    groups={i:[] for i in list(range(1,10))+list(range(20,32))}
    for index,batch in enumerate(batches,len(base['chunks'])):
        for part,(_,data) in enumerate(batch):
            material=data[1].name.removeprefix('Metropolis ')
            kind=(styles.index(material[6:])+1 if material.startswith('Glass ') else
                  7 if material=='Window Blue' else 9 if material=='Beacon' else
                  20+int(material.rsplit(' ',1)[1]) if material.startswith('Sphere Screen ') else 0)
            if kind: groups[kind].append(index*16+part)
    lines=["''' Generated catalog part classifications; Studio owns effects and timing.",
        'Module Smile.Tools.TownCityData','','Option Explicit','',
        'Public Function Kind(Slot As Number) As Number','','    Select Case Slot']
    for kind,slots in groups.items():
        for slot in slots:
            lines.extend([f'        Case {slot}',f'            Return {kind}'])
    lines+=['    End Select','','    Return 0','','End Function','','End Module','']
    (ROOT.parents[5]/'tools/Character3DViewer/TownCityData.smile').write_text('\n'.join(lines))
    layout=json.loads((output/'layout.json').read_text())
    by_name={t['label']:t for t in templates}
    draws=sum(len(by_name[item['name']]['parts']) for item in layout['items'])
    return dict(templates=len(templates),chunks=len(chunks),total_catalog_chunks=len(base['chunks'])+len(chunks),
                vertices=sum(counts),triangles=sum(c['triangles'] for c in chunks),city_draws=draws)


if __name__=='__main__':
    print(json.dumps(publish()),flush=True)
