"""Read the native saved mesh recipe; do not approximate authored curves a second time."""
from town_document_codec import Reader
from town_surface_layers import LAYERS


def read_prepared(read):
    def number(suffix):
        data = read(suffix)
        if len(data) != 4:
            raise ValueError('Invalid prepared terrain metadata')
        return int.from_bytes(data, 'big')

    version = number('.PreparedVersion')
    if version not in (3, 4, 5, 6, 7, 8):
        raise ValueError('Unsupported terrain recipe; save the map in the current Studio first.')
    batches = number('.Terrain.Batches')
    if not 1 <= batches <= 256:
        raise ValueError('Invalid prepared terrain batch count')
    result = []
    for batch in range(batches):
        prefix = '.Terrain.Batch%d.0' % batch
        count = number(prefix + '.Count') - 1
        if not 0 <= count <= 65536:
            raise ValueError('Invalid prepared terrain patch count')
        for first in range(0, count, 4096):
            r = Reader(read(prefix + '.%d.0' % (first // 4096)))
            length = min(4096, count - first)
            if r.integer() != length:
                raise ValueError('Incomplete prepared terrain page')
            for _ in range(length):
                patch = [r.precise() for _ in range(8)] + [r.integer(), r.integer()]
                y2, flow = 0, 0
                if version >= 4:
                    if patch[9] == 5:
                        y2 = r.precise()
                    flow = r.integer()
                if (not 1 <= patch[8] <= 6 or not 0 <= patch[9] <= 5 or
                        not 0 <= flow <= 3 or (flow and (patch[8],patch[9]) != (2,5))):
                    raise ValueError('Unsupported terrain patch; export canceled without flattening.')
                style = r.integer() if version >= 6 else 0
                if not 0 <= style <= 5:
                    raise ValueError("Invalid terrain appearance")
                result.append(patch + [style, y2, flow])
            if r.offset != len(r.data):
                raise ValueError('Unexpected prepared terrain page data')
    return result


def mesh_data(patches):
    vertices, faces, materials = [], [], []
    for patch in patches:
        x0, z0, x1, z1, y0, y1, x2, z2, kind, plane = patch[:10]
        style = patch[10] if len(patch) > 10 else 0
        height = LAYERS['ROAD_Y' if kind == 3 else 'WATER_Y' if kind == 2 else 'GROUND_Y']
        if plane == 5:
            points = [(x0,y0,z0), (x1,y1,z1), (x2,patch[11],z2)]
        elif plane == 3 and kind != 2:
            points = [(x0, height, z0), (x1, height, z1), (x2, height, z2)]
        elif plane in (1, 2, 4) and kind != 2:
            end_x, end_z = (x1, z0) if plane == 1 else (x0, z1) if plane == 2 else (x1, z1)
            points = [(x0, y0, z0), (x0, y1, z0), (end_x, y1, end_z), (end_x, y0, end_z)]
        else:
            if y0 > 0:
                height = y0
            points = [(x0, height, z0), (x0, height, z1), (x1, height, z1), (x1, height, z0)]
        base = len(vertices)
        scale = LAYERS['UNITS_PER_METER']
        origin = LAYERS['BLENDER_ORIGIN_Y']
        vertices.extend((x / scale, z / scale, (y - origin) / scale) for x, y, z in points)
        # Native X/Y/Z becomes Blender X/Z/Y, reversing handedness.
        faces.append(tuple(reversed(range(base, len(vertices)))))
        materials.append(6+(style-1)*2+(1 if kind==3 else 0) if style and kind in (1,3) else kind-1)
    return vertices, faces, materials
