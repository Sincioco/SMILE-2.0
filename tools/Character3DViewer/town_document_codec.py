"""Bounded TWN1/Save Data codec shared by the Blender worker and round-trip checks."""
import hashlib
import json
import struct
from pathlib import Path


def key_path(folder, key):
    return Path(folder) / (hashlib.sha256(key.encode('utf-8')).hexdigest() + '.bin')


def envelope(payload):
    return b'SMD4' + struct.pack('<II', 1, len(payload)) + hashlib.sha256(payload).digest() + payload


def unwrap(raw):
    if len(raw) < 44 or len(raw) > 524332 or raw[:4] != b'SMD4':
        raise ValueError('Invalid document envelope')
    version, size = struct.unpack_from('<II', raw, 4)
    payload = raw[44:]
    if version != 1 or size != len(payload) or hashlib.sha256(payload).digest() != raw[12:44]:
        raise ValueError('Document checksum mismatch')
    return payload


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


def decode(payload, catalog, request=False):
    r = Reader(payload)
    if bytes(r.byte() for _ in range(4)) != b'TWN\x01':
        raise ValueError('Unsupported town format')
    fingerprint = hashlib.sha256(json.dumps(catalog, sort_keys=True).encode()).hexdigest()
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
    result['sun'] = [r.integer() for _ in range(5)] + [r.precise(), r.precise(), r.byte()]
    result['payload'] = payload[:r.offset]
    if request:
        result['mode'], result['request_id'] = r.integer(), r.integer()
        if result['mode'] not in (1, 2) or result['request_id'] < 1:
            raise ValueError('Invalid save request')
    if r.offset != len(payload):
        raise ValueError('Trailing document data')
    return result


def respond(folder, request_id, status, progress, message):
    payload = b'TWR\x01' + integer(request_id) + integer(status) + integer(progress) + name(message[:80])
    atomic_write(key_path(folder, 'TownEditor.Blender.Response'), payload)
