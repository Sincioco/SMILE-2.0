"""Generate native catalog tables and an exact initial surface partition.

Run after export_catalog.py. Geometry remains in its shared GLB templates; these
tables contain only document data and template-to-model-part references.
"""
import json
import hashlib
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[5]
OUTPUT = REPO / 'tools/Character3DViewer'

# Curate the immutable catalog's repeated placements only in the palette. Keep
# every template ID/geometry/fingerprint for existing towns and Blender exports.
# These copies have the same design (sub-millimetre export rounding); the original
# fountain also has a historical water-roughness difference, not a separate prop.
PALETTE_COPIES = {f'group:{name}.{index:03}'
                  for name in ('Fountain Water', 'Wayfarer Plaza')
                  for index in range(1, 6)}
PALETTE_LABELS = {'group:Wayfinding Post': 'Civic Plaza Sign',
                  'group:Wayfinding Post.002': 'Market Walk Sign'}


def palette_slots(catalog):
    """Stable template IDs share compact preview slots for repeated placements."""
    slots, result = {}, []
    for template in catalog['templates']:
        source = template['source']
        if source in PALETTE_COPIES:
            source = source.rsplit('.', 1)[0]
        result.append(slots.setdefault(source, len(slots)))
    return result


def number(value):
    return f'{value:.6f}'


def vector(values):
    return 'P.Vector(' + ', '.join(number(v) for v in values) + ')'


def quoted(value):
    return '"' + value.replace('"', '""') + '"'


def begin(name, imports=()):
    return ["''' Generated from the saved Blender catalog. Data only; no input or renderer ownership.",
            'Module Smile.Tools.' + name, '', 'Option Explicit', '', *imports, '']


def item_fields(item, indent):
    x, y, z = item['position']
    sx, sy, sz = item['scale']
    return [indent + f'Result.Position = {vector((x*10, z*10+21, y*10))}',
            indent + f'Result.Scale = {vector((sx*1000, sz*1000, sy*1000))}',
            indent + f'Result.Yaw = {number(-math.degrees(item["rotation"][2]))}']


def generate(catalog, include_initial_surface=True):
    fingerprint = catalog.get('document_fingerprint') or hashlib.sha256(json.dumps(catalog, sort_keys=True).encode('utf-8')).hexdigest()
    castle = json.loads((ROOT.parent / 'TripoCastleV1/Runtime/manifest.json').read_text(encoding='utf-8'))
    assert len(castle['chunks']) == 14 and all(p['parts'] == 2 for p in castle['chunks'])
    lines = begin('TownCatalogData', [
        'Import Smile.Simple3D.Precision3D As P',
        'Import Smile.Tools.TownDocument As Document'])
    count = len(catalog['chunks'])
    lines += [f'Public Const FINGERPRINT = "{fingerprint}"',
              f'Public Const CHUNK_COUNT = {count}',
              f'Public Const BASE_CHUNK_COUNT = {catalog.get("base_chunk_count",count)}',
              f'Public Const TEMPLATE_COUNT = {len(catalog["templates"])}',
              f'Public Const INITIAL_ITEMS = {len(catalog["instances"])}', '',
              'Public Type Template', '    Label As Text', '    Category As Number',
              '    PaletteVisible As Boolean', '    PreviewIndex As Number',
              '    Low As P.Vector3', '    High As P.Vector3',
              '    PartCount As Number', '    Parts[32] As Number',
              '    SolidCount As Number', '    CameraCount As Number', '    StepCount As Number',
              '    FloorCount As Number', '    DoorCount As Number', '    WaterCount As Number', 'End Type', '',
              'Public Function TemplateAt(Index As Number) As Template', '',
              '    Dim Result As Template', '', '    Select Case Index']
    previews = palette_slots(catalog)
    for template in catalog['templates']:
        parts = [m*16+p for m, p in template['parts']]
        if template.get('existingCastle'):
            # The current lossless castle publication contains two parts per model.
            parts = [(count+m)*16+p for m in range(14) for p in range(2)]
        assert len(parts) <= 32
        low, high = template['bounds']
        category = 1 if template['category'] == 'building' else 3
        lines += [f'        Case {template["id"]}',
                  f'            Result.Label = {quoted(PALETTE_LABELS.get(template["source"], template["label"]))}',
                  f'            Result.Category = {category}',
                  f'            Result.PaletteVisible = {template["source"] not in PALETTE_COPIES}',
                  f'            Result.PreviewIndex = {previews[template["id"]]}',
                  f'            Result.Low = {vector((low[0]*10,low[2]*10,low[1]*10))}',
                  f'            Result.High = {vector((high[0]*10,high[2]*10,high[1]*10))}',
                  f'            Result.PartCount = {len(parts)}',
                  f'            Result.SolidCount = {len(template["solids"])}',
                  f'            Result.CameraCount = {len(template["camera"])}',
                  f'            Result.StepCount = {len(template["steps"])}',
                  f'            Result.FloorCount = {len(template.get("floors", []))}',
                  f'            Result.DoorCount = {len(template["doors"])}',
                  f'            Result.WaterCount = {len(template["water"])}']
        lines.extend(f'            Result.Parts[{i}] = {p}' for i, p in enumerate(parts))
        lines.append('')
    lines += ['    End Select', '', '    Return Result', '', 'End Function', '',
              'Public Function InitialItem(Index As Number) As Document.Item', '',
              '    Dim Result As Document.Item', '', '    Select Case Index']
    for index, item in enumerate(catalog['instances']):
        template = catalog['templates'][item['template']]
        lines += [f'        Case {index}', f'            Result.Identity = {index+1}',
                  f'            Result.SourceIndex = {index}',
                  f'            Result.Template = {item["template"]}',
                  f'            Result.Category = {1 if template["category"] == "building" else 3}',
                  '            Result.Active = True', *item_fields(item, '            '), '']
    lines += ['    End Select', '', '    Return Result', '', 'End Function', '',
        "''' Palette defaults are independent of whether a template occurs in the original town.",
        'Public Function PlacementSample(TemplateIndex As Number) As Document.Item', '',
        '    Dim Result As Document.Item', '    Dim Shape As Template', '    Dim Index As Number', '',
        '    For Index = 0 To INITIAL_ITEMS - 1', '',
        '        Result = InitialItem(Index)', '',
        '        If Result.Template = TemplateIndex Then', '            Return Result',
        '        End If', '', '    End For', '',
        '    Shape = TemplateAt(TemplateIndex)',
        '    Result.Template = TemplateIndex', '    Result.Category = Shape.Category',
        '    Result.SourceIndex = -1', '    Result.Identity = 0',
        '    Result.Active = Shape.PartCount > 0',
        '    Result.Position = P.Vector(0.0, 21.0, 0.0)',
        '    Result.Scale = P.Vector(1000.0, 1000.0, 1000.0)', '    Result.Yaw = 0.0', '',
        '    Return Result', '', 'End Function', '', 'End Module', '']
    lawns = []
    for model, chunk in enumerate(catalog['chunks']):
        raw = (ROOT / 'Authoring' / chunk['file']).read_bytes()
        gltf = json.loads(raw[20:20+struct.unpack_from('<I', raw, 12)[0]])
        for part, mesh in enumerate(gltf['meshes']):
            mat = gltf['materials'][mesh['primitives'][0]['material']]
            if mat['name'] == 'Town Grass':
                lawns.append(model * 16 + part)
    lines = lines[:-2] + ['Public Function IsLawnPart(Slot As Number) As Boolean', '',
        '    Dim Result As Boolean', '',
        '    Result = ' + ' Or '.join('Slot = ' + str(n) for n in lawns), '',
        '    Return Result', '', 'End Function', '', 'End Module', '']
    (OUTPUT / 'TownCatalogData.smile').write_text('\n'.join(lines), encoding='utf-8')

    if not include_initial_surface:
        return

    terrain = catalog['terrain']
    west, south, east, north = terrain['bounds']
    xs = {west, east}
    zs = {south, north}
    xs.update(range(math.ceil(west), math.ceil(east), 2))
    zs.update(range(math.ceil(south), math.ceil(north), 2))
    for rectangle in terrain['land'] + terrain['waterRectangles'] + terrain['bridges'] + terrain['roadRectangles']:
        xs.update(v for v in (rectangle[0], rectangle[2]) if west <= v <= east)
        zs.update(v for v in (rectangle[1], rectangle[3]) if south <= v <= north)
    xs, zs = sorted(xs), sorted(zs)
    assert len(xs) <= 513 and len(zs) <= 513
    inside = lambda x, z, rectangles: any(a <= x <= c and b <= z <= d for a, b, c, d in rectangles)
    cells = []
    for a, b in zip(zs, zs[1:]):
        for c, d in zip(xs, xs[1:]):
            x, z = (c+d)*.5, (a+b)*.5
            water = not inside(x, z, terrain['land']) or inside(x, z, terrain['waterRectangles'])
            bridge = inside(x, z, terrain['bridges'])
            road = inside(x, z, terrain['roadRectangles']) or bridge
            cells.append(4 if water and bridge else 2 if water else 3 if road else 1)
    packed = [sum(v*8**j for j, v in enumerate(cells[i:i+16])) for i in range(0, len(cells), 16)]
    lines = begin('TownInitialSurface', ['Import Smile.Simple3D.SurfaceGrid3D As Grid',
                                       'Import Smile.Simple3D.TerrainHeights3D As Heights'])
    lines += ['Public Sub Populate(ByRef Value As Grid.State)', '', '    Dim Index As Number', '',
              f'    Value.Columns = {len(xs)-1}', f'    Value.Rows = {len(zs)-1}',
              f'    Value.OriginX = {number(west*10)}', f'    Value.OriginZ = {number(south*10)}',
              '    Value.CellSize = 20.0', '    Value.Revision = Value.Revision + 1', '',
              '    Call Heights.Reset(Value.Heights, (Value.Columns + 1) * (Value.Rows + 1))', '',
              '    Value.Curves.BrushCount = 0', '']
    for name, values in [('XEdges', xs), ('ZEdges', zs)]:
        lines.extend(f'    Value.{name}[{i}] = {number(v*10)}' for i, v in enumerate(values))
        lines.append('')
    # Runs keep large uninterrupted lawn/water areas compact without changing any cell.
    index = 0
    while index < len(packed):
        end = index + 1
        while end < len(packed) and packed[end] == packed[index]:
            end += 1
        if end - index > 2:
            lines += [f'    For Index = {index} To {end-1}',
                      f'        Value.Cells[Index] = {packed[index]}', '    End For', '']
        else:
            lines.extend(f'    Value.Cells[{i}] = {packed[i]}' for i in range(index, end))
        index = end
    lines += ['', 'End Sub', '', 'End Module', '']
    (OUTPUT / 'TownInitialSurface.smile').write_text('\n'.join(lines), encoding='utf-8')
    print(f'NATIVE CATALOG {len(catalog["instances"])} items; exact surface {len(xs)-1} x {len(zs)-1}')


if __name__ == '__main__':
    generate(json.loads((ROOT / 'Authoring/catalog.json').read_text(encoding='utf-8')))
