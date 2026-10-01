"""Bounded TWN1–TWN11 codec shared by the existing Blender document worker."""
import math
from functools import lru_cache
import hashlib
import json
import struct
import re
from pathlib import Path


def key_path(folder, key):
    return Path(folder) / (hashlib.sha256(key.encode('utf-8')).hexdigest() + '.bin')


def envelope(payload):
    return b'SMD4' + struct.pack('<II', 1, len(payload)) + hashlib.sha256(payload).digest() + payload


def unwrap(raw):
    if len(raw) < 44 or len(raw) > 64 * 1024 * 1024 or raw[:4] != b'SMD4':
        raise ValueError('Invalid document envelope')
    version, size = struct.unpack_from('<II', raw, 4)
    payload = raw[44:44 + size]
    if version != 1 or size > 1048576 or size != len(payload) or hashlib.sha256(payload).digest() != raw[12:44]:
        raise ValueError('Document checksum mismatch')
    if len(raw) > 44 + size:
        validate_prepared_bundle(raw, 44 + size)
    return payload


def validate_prepared_bundle(raw, offset):
    """Keep Blender/authoring readers compatible with native prepared .town exports."""
    if len(raw) - offset < 40 or raw[offset:offset + 4] != b'SMB1':
        raise ValueError('Invalid prepared bundle')
    if hashlib.sha256(raw[:-32]).digest() != raw[-32:]:
        raise ValueError('Prepared bundle checksum mismatch')
    count, = struct.unpack_from('<I', raw, offset + 4)
    if count > 2048:
        raise ValueError('Too many prepared records')
    offset += 8
    records = {}
    for _ in range(count):
        if offset + 8 > len(raw) - 32:
            raise ValueError('Incomplete prepared record')
        name_size, data_size = struct.unpack_from('<II', raw, offset)
        offset += 8
        if not 1 <= name_size <= 128 or not 44 <= data_size <= 1048620 or offset + name_size + data_size > len(raw) - 32:
            raise ValueError('Invalid prepared record size')
        name = raw[offset:offset + name_size].decode('ascii')
        offset += name_size
        if name[0] != '.' or any(c not in '._-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz' for c in name) or name in records:
            raise ValueError('Invalid prepared record name')
        part = raw[offset:offset + data_size]
        if part[:4] != b'SMD4' or struct.unpack_from('<II', part, 4) != (1, data_size - 44) or hashlib.sha256(part[44:]).digest() != part[12:44]:
            raise ValueError('Invalid prepared record checksum')
        records[name] = part[44:]
        offset += data_size
    if offset != len(raw) - 32:
        raise ValueError('Unexpected prepared bundle data')
    return records


def prepared_records(raw):
    """Read verified companion payloads without discarding native save preparation."""
    payload = unwrap(raw)
    offset = 44 + len(payload)
    return validate_prepared_bundle(raw, offset) if len(raw) > offset else {}


def integer(value):
    value = value * 2 if value >= 0 else -value * 2 - 1
    out = bytearray()
    while value >= 128:
        out.append((value % 128) + 128)
        value //= 128
    out.append(value)
    return out


def name(value):
    return integer(len(value)) + b''.join(integer(ord(c)) for c in value)


def atomic_write(path, payload):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    pending = path.with_suffix('.pending')
    pending.write_bytes(envelope(payload))
    pending.replace(path)


class Reader:
    def __init__(self, data):
        self.data, self.offset = data, 0

    def byte(self):
        if self.offset >= len(self.data):
            raise ValueError('Incomplete document')
        value = self.data[self.offset]
        self.offset += 1
        return value

    def integer(self):
        value = 0
        for i in range(8):
            digit = self.byte()
            value += (digit & 127) << (i * 7)
            if digit < 128:
                return value // 2 if value % 2 == 0 else -(value // 2) - 1
        raise ValueError('Invalid integer')

    def precise(self):
        value = self.integer() / 1000000
        if abs(value) > 1000000000:
            raise ValueError('Coordinate out of range')
        return value

    def name(self):
        size = self.integer()
        if not 1 <= size <= 80:
            raise ValueError('Invalid name length')
        values = [self.integer() for _ in range(size)]
        if any(c < 32 or c > 0x10ffff or 0xd800 <= c <= 0xdfff for c in values):
            raise ValueError('Invalid name')
        return ''.join(map(chr, values))

    def sun(self, version):
        light = [self.integer() for _ in range(5)] + [self.precise(), self.precise(), self.byte()]
        if version >= 2:
            light.append(self.byte() - 1)
        if (any(not 0 <= value <= 255 for value in light[:3])
                or not 0 <= light[3] <= 1000 or not 0 <= light[4] <= 100
                or light[7] not in (0, 1) or (version >= 2 and not -1 <= light[8] <= 100)):
            raise ValueError('Invalid sun settings')
        return light

    def flag(self):
        flag = self.byte()
        if flag not in (0, 1):
            raise ValueError('Invalid preset flag')
        return bool(flag)


def legacy_landmark(label):
    for kind, original in enumerate(('Neris Spaceport', 'Horizon Airport'), 1):
        if label == original or re.fullmatch(re.escape(original) + r' [0-9]+', label):
            return kind
    return 0


def landmark(document):
    return document.get('landmark', legacy_landmark(document['name']))


def require_blender_support(document):
    if any(document.get('heights', [])) or any(flow[0] for flow in document.get('flows', [])):
        raise ValueError('Blender elevations/directed flow unsupported. Use Save For Viewer; destination retained.')


def decode(payload, catalog, request=False):
    r = Reader(payload)
    if bytes(r.byte() for _ in range(3)) != b'TWN':
        raise ValueError('Unsupported town format')
    version = r.byte()
    if not 1 <= version <= 11:
        raise ValueError('Unsupported town format')
    fingerprint = catalog.get('document_fingerprint') or hashlib.sha256(json.dumps(catalog, sort_keys=True).encode()).hexdigest()
    if r.name() != fingerprint:
        raise ValueError('Town catalog differs from the saved document')
    result = {'name': r.name(), 'dirty': r.byte()}
    columns, rows, size = r.integer(), r.integer(), r.precise()
    if not (1 <= columns <= 512 and 1 <= rows <= 512 and size > 0):
        raise ValueError('Invalid surface dimensions')
    xs = [r.precise() for _ in range(columns + 1)]
    zs = [r.precise() for _ in range(rows + 1)]
    if any(b <= a for edges in (xs, zs) for a, b in zip(edges, edges[1:])):
        raise ValueError('Invalid surface edges')
    cells = []
    for _ in range((columns * rows + 15) // 16):
        value = r.integer()
        if not 0 <= value < 8**16:
            raise ValueError('Invalid cells')
        for _ in range(16):
            cells.append(value % 8)
            value //= 8
    if any(c > 4 for c in cells):
        raise ValueError('Invalid surface')
    result.update(columns=columns, rows=rows, cell_size=size, xs=xs, zs=zs,
                  cells=cells[:columns*rows])
    count = r.integer()
    if not 0 <= count <= 1024:
        raise ValueError('Too many items')
    items, ids = [], set()
    for _ in range(count):
        identity, template, source = r.integer(), r.integer(), r.integer()
        position = [r.precise() for _ in range(3)]
        scale = [r.precise() for _ in range(3)]
        yaw = r.precise()
        if (identity < 1 or identity in ids or not 0 <= template < len(catalog['templates'])
                or not -1 <= source < len(catalog['instances']) or min(scale) <= 0):
            raise ValueError('Invalid item')
        ids.add(identity)
        items.append(dict(identity=identity, template=template, source=source,
                          position=position, scale=scale, yaw=yaw))
    result['items'] = items
    result['sun'] = r.sun(version)
    if version >= 3:
        count = r.integer()
        if not 0 <= count <= 4096:
            raise ValueError('Too many map-load tiles')
        tiles, occupied = [], set()
        for _ in range(count):
            x, z, destination = r.integer(), r.integer(), r.name()
            if not (0 <= x < columns and 0 <= z < rows) or (x, z) in occupied:
                raise ValueError('Invalid or duplicate map-load tile')
            occupied.add((x, z))
            tiles.append(dict(x=x, z=z, destination=destination))
        result['map_tiles'] = tiles
    if version >= 4:
        result['presets'] = {'night_active': r.flag(),
                             'day': r.sun(4) if r.flag() else None,
                             'night': r.sun(4) if r.flag() else None}
    if version >= 5:
        result['court_offset'] = [r.precise(), r.precise()]
        result['court_placed'] = r.flag()
    if version >= 6:
        count = r.integer()
        if not 0 <= count <= 256:
            raise ValueError('Invalid curved surface count')
        if count:
            result['base_cells'] = result['cells'][:]
            brushes = []
            for _ in range(count):
                brush = [r.integer(), r.integer()] + [r.precise() for _ in range(5)]
                form, kind, x0, z0, x1, z1, width = brush
                if not 1 <= form <= (8 if version >= 9 else 6 if version >= 8 else 4):
                    raise ValueError('Invalid curved surface brush')
                if version >= 9 and form >= 7:
                    brush += [r.precise(), r.precise()]
                x2, z2 = brush[7:] if form >= 7 else (0, 0)
                if not (1 <= form <= (8 if version >= 9 else 6 if version >= 8 else 4) and 0 <= kind <= 4 and width >= 0
                        and (form not in (2, 3) or x1 > 0)
                        and (form != 5 or (width > 0 and (x1-x0)**2+(z1-z0)**2 > width**2))
                        and (form != 6 or (x1 > 0 and z1 > 0 and width > 0))
                        and (form != 7 or abs((x1-x0)*(z2-z0)-(z1-z0)*(x2-x0)) > .000001)
                        and (form != 8 or width > 0)):
                    raise ValueError('Invalid curved surface brush')
                brushes.append(brush)
            result['curves'] = brushes
            result['cells'] = raster_curves(result)
    if version >= 7:
        result['terrain_style'] = r.byte()
        if result['terrain_style'] > 3:
            raise ValueError('Invalid terrain style')
    if version >= 10:
        result['landmark'] = r.byte()
        if not 0 <= result['landmark'] <= 2:
            raise ValueError('Invalid landmark identity')
    if version >= 11:
        total = r.integer()
        if total != (columns + 1) * (rows + 1):
            raise ValueError('Invalid terrain corner count')
        heights = []
        while len(heights) < total:
            length, height = r.integer(), r.integer()
            if not 1 <= length <= total-len(heights) or abs(height) > 1000000:
                raise ValueError('Invalid terrain height run')
            heights.extend([height / 100] * length)
        result['heights'] = heights
        flows, groups = [], set()
        for brush in result.get('curves', []):
            mode, sign, speed = r.integer(), r.integer(), r.integer()
            if mode == 0:
                if sign != 0 or speed != 0:
                    raise ValueError('Flat water has directed-flow metadata')
            elif mode == 1:
                dx, dz = brush[4]-brush[2], brush[5]-brush[3]
                length = math.hypot(dx, dz)
                if (brush[0] != 4 or brush[1] != 2 or brush[6] <= 0 or
                        length <= .000001 or sign not in (-1, 1) or not 0 <= speed <= 400):
                    raise ValueError('Invalid following-water brush')
                groups.add((round(sign*dx/length, 6), round(sign*dz/length, 6), speed))
            else:
                raise ValueError('Unsupported water mode')
            flows.append([mode, sign, speed])
        if len(groups) > 3:
            raise ValueError('More than three distinct flow directions/speeds')
        result['flows'] = flows
    result['payload'] = payload[:r.offset]
    if request:
        result['mode'], result['request_id'] = r.integer(), r.integer()
        if result['mode'] not in (1, 2) or result['request_id'] < 1:
            raise ValueError('Invalid save request')
    if r.offset != len(payload):
        raise ValueError('Trailing document data')
    return result


@lru_cache(maxsize=512)
def bezier_segments(brush):
    x0,z0,x1,z1 = brush[2:6]
    x2,z2 = brush[7:]
    bend = math.hypot(x0-2*x1+x2,z0-2*z1+z2)
    count = max(1,min(256,int(math.sqrt(bend))+1))
    previous, result = (x0,z0), []
    for index in range(1,count+1):
        t = index/count
        u = 1-t
        current = (u*u*x0+2*u*t*x1+t*t*x2,u*u*z0+2*u*t*z1+t*t*z2)
        result.append((*previous,*current))
        previous = current
    return tuple(result)


def curve_contains(brush, x, z):
    """Same analytic predicates as SurfacePaint3D; also used by authoring checks."""
    form, _, x0, z0, x1, z1, width = brush[:7]
    dx, dz = x-x0, z-z0
    if form == 7:
        x2, z2 = brush[7:]
        signs = ((x1-x0)*(z-z0)-(z1-z0)*(x-x0),
                 (x2-x1)*(z-z1)-(z2-z1)*(x-x1),
                 (x0-x2)*(z-z2)-(z0-z2)*(x-x2))
        return min(signs) >= 0 or max(signs) <= 0
    if form == 8:
        low_x, high_x, low_z, high_z = curve_bounds(brush)
        if not (low_x <= x <= high_x and low_z <= z <= high_z):
            return False
        for ax, az, bx, bz in bezier_segments(tuple(brush)):
            if not (min(ax,bx)-width/2 <= x <= max(ax,bx)+width/2 and
                    min(az,bz)-width/2 <= z <= max(az,bz)+width/2):
                continue
            dx, dz = bx-ax, bz-az
            t = max(0,min(1,((x-ax)*dx+(z-az)*dz)/max(.000001,dx*dx+dz*dz)))
            if (x-ax-t*dx)**2+(z-az-t*dz)**2 <= width*width/4:
                return True
        return False
    if form == 1:
        return x0 <= x <= x1 and z0 <= z <= z1
    if form == 2:
        return dx*dx+dz*dz <= x1*x1
    if form == 3:
        return max(0,x1-width/2)**2 <= dx*dx+dz*dz <= (x1+width/2)**2
    if form == 5:
        ax, az = x1-x0, z1-z0
        distance, radius = ax*ax+az*az, width*width
        along, across = dx*ax+dz*az, dx*az-dz*ax
        return (radius <= along <= distance and dx*dx+dz*dz >= radius
                and across*across*(distance-radius) <= radius*(distance-along)**2)
    if form == 6:
        dx,dz = abs(dx),abs(dz)
        return (dx <= x1+width and dz <= z1+width and
                (dx <= x1 or dz <= z1 or (dx-x1-width)**2+(dz-z1-width)**2 >= width**2))
    t = max(0,min(1,(dx*(x1-x0)+dz*(z1-z0))/max(.000001,(x1-x0)**2+(z1-z0)**2)))
    return (dx-t*(x1-x0))**2+(dz-t*(z1-z0))**2 <= width*width/4


def curve_bounds(brush):
    form, _, x0, z0, x1, z1, width = brush[:7]
    if form in (7,8):
        x2, z2 = brush[7:]
        radius = width/2 if form == 8 else 0
        return min(x0,x1,x2)-radius, max(x0,x1,x2)+radius, min(z0,z1,z2)-radius, max(z0,z1,z2)+radius
    if form in (2, 3):
        extent = x1 + (width / 2 if form == 3 else 0)
        return x0-extent, x0+extent, z0-extent, z0+extent
    if form == 5:
        return min(x0-width,x1), max(x0+width,x1), min(z0-width,z1), max(z0+width,z1)
    if form == 6:
        return x0-x1-width, x0+x1+width, z0-z1-width, z0+z1+width
    extent = width / 2 if form == 4 else 0
    return min(x0,x1)-extent, max(x0,x1)+extent, min(z0,z1)-extent, max(z0,z1)+extent


def raster_curves(document):
    """Derived cells for legacy consumers; the exact brush stack stays authoritative."""
    import bisect
    cells = document['base_cells'][:]
    xs, zs, columns = document['xs'], document['zs'], document['columns']
    for brush in document['curves']:
        kind = brush[1]
        low_x, high_x, low_z, high_z = curve_bounds(brush)
        for row in range(max(0,bisect.bisect_right(zs,low_z)-1), min(len(zs)-1,bisect.bisect_right(zs,high_z))):
            z = (zs[row]+zs[row+1]) / 2
            for col in range(max(0,bisect.bisect_right(xs,low_x)-1), min(len(xs)-1,bisect.bisect_right(xs,high_x))):
                x = (xs[col]+xs[col+1]) / 2
                if curve_contains(brush, x, z):
                    index = row*columns+col
                    cells[index] = 4 if kind == 3 and cells[index] in (2,4) else kind
    return cells


def encode(document, catalog):
    fingerprint = catalog.get('document_fingerprint') or hashlib.sha256(json.dumps(catalog, sort_keys=True).encode()).hexdigest()
    # Eight-value legacy snapshots must retain their checksum when opened.
    version = (7 if 'terrain_style' in document else 6 if document.get('curves') else 5 if 'court_placed' in document else 4 if 'presets' in document else 3 if 'map_tiles' in document
               else 2 if len(document['sun']) == 9 else 1)
    if any(brush[0] >= 5 for brush in document.get('curves', [])):
        version = 8
    if any(brush[0] >= 7 for brush in document.get('curves', [])):
        version = 9
    if 'landmark' in document:
        if not 0 <= document['landmark'] <= 2:
            raise ValueError('Invalid landmark identity')
        if document['landmark'] != legacy_landmark(document['name']):
            version = 10
    heights = document.get('heights', [])
    flows = document.get('flows', [])
    if heights and len(heights) != (document['columns']+1)*(document['rows']+1):
        raise ValueError('Invalid terrain corner count')
    if any(not math.isfinite(h) or abs(h) > 10000 for h in heights):
        raise ValueError('Non-finite or out-of-range terrain height')
    if flows and len(flows) != len(document.get('curves', [])):
        raise ValueError('Invalid water metadata count')
    if any(heights) or any(any(flow) for flow in flows):
        version = 11
    result = bytearray(b'TWN') + bytes([version]) + name(fingerprint) + name(document['name']) + b'\0'
    precise = lambda value: integer(round(value * 1000000))
    result += integer(document['columns']) + integer(document['rows']) + precise(document['cell_size'])
    for edge in document['xs'] + document['zs']:
        result += precise(edge)
    cells = document['base_cells'] if document.get('curves') else document['cells']
    for start in range(0, len(cells), 16):
        result += integer(sum(cell * 8**i for i, cell in enumerate(cells[start:start+16])))
    result += integer(len(document['items']))
    for item in document['items']:
        result += integer(item['identity']) + integer(item['template']) + integer(item['source'])
        for value in item['position'] + item['scale'] + [item['yaw']]:
            result += precise(value)
    for value in document['sun'][:5]:
        result += integer(value)
    result += precise(document['sun'][5]) + precise(document['sun'][6]) + bytes([document['sun'][7]])
    if version >= 2:
        result += bytes([(document['sun'][8] if len(document['sun']) > 8 else -1) + 1])
    if version >= 3:
        result += integer(len(document.get('map_tiles', [])))
        for tile in document.get('map_tiles', []):
            result += integer(tile['x']) + integer(tile['z']) + name(tile['destination'])
    if version >= 4:
        presets = document.get('presets', {'night_active': False, 'day': None, 'night': None})
        result += bytes([int(presets['night_active'])])
        for key in ('day', 'night'):
            light = presets[key]
            result += bytes([int(light is not None)])
            if light is not None:
                for value in light[:5]:
                    result += integer(value)
                result += precise(light[5]) + precise(light[6]) + bytes([light[7], light[8] + 1])
    if version >= 5:
        result += b''.join(precise(value) for value in document.get('court_offset', [0, 0]))
        result += bytes([int(document.get('court_placed', False))])
    if version >= 6:
        result += integer(len(document.get('curves', [])))
        for brush in document.get('curves', []):
            result += integer(brush[0]) + integer(brush[1])
            result += b''.join(precise(value) for value in brush[2:])
    if version >= 7:
        result += bytes([document.get('terrain_style', 0)])
    if version >= 10:
        result += bytes([landmark(document)])
    if version >= 11:
        total = (document['columns']+1)*(document['rows']+1)
        quantized = [round(h*100) for h in heights] if heights else [0]*total
        result += integer(total)
        start = 0
        while start < total:
            end = start+1
            while end < total and quantized[end] == quantized[start]:
                end += 1
            result += integer(end-start) + integer(quantized[start])
            start = end
        for flow in flows or [[0, 0, 0]]*len(document.get('curves', [])):
            if len(flow) != 3 or any(not isinstance(value, int) for value in flow):
                raise ValueError('Invalid water metadata')
            result += b''.join(integer(value) for value in flow)
    if len(result) > 524288:
        raise ValueError('Town exceeds the native 512 KiB document limit')
    decode(result, catalog)  # Same format/range checks apply in both directions.
    return bytes(result)


def respond(folder, request_id, status, progress, message, key='TownEditor.Blender.Response'):
    payload = b'TWR\x01' + integer(request_id) + integer(status) + integer(progress) + name(message[:80])
    atomic_write(key_path(folder, key), payload)
