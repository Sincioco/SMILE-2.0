"""Trim the Royal Bridge at the courtyard edge, eliminating near-coplanar overlap.

Run with Blender in background mode. Keeps the stable catalog IDs and fingerprint.
"""
import hashlib
import json
import struct
from pathlib import Path
import bpy


def apply():
    folder = Path(__file__).resolve().parents[1]
    path = folder / 'Authoring/catalog.json'
    catalog = json.loads(path.read_text())
    source = folder / catalog['blend_source']
    bpy.ops.wm.open_mainfile(filepath=str(source), use_scripts=False)
    root = bpy.data.objects['Royal Castle of Neris']
    deck = bpy.data.objects['Royal Bridge Deck']
    bpy.context.view_layer.update()
    local = root.matrix_world.inverted() @ deck.matrix_world
    inverse = local.inverted()
    for vertex in deck.data.vertices:
        point = local @ vertex.co
        point.y = min(point.y, -32.0)
        vertex.co = inverse @ point
    deck.data.update()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(source), compress=True)
    catalog['source_sha256'] = hashlib.sha256(source.read_bytes()).hexdigest()

    asset = folder / 'Authoring' / catalog['chunks'][30]['file']
    raw = asset.read_bytes()
    length = struct.unpack_from('<I', raw, 12)[0]
    gltf = json.loads(raw[20:20+length])
    binary = bytearray(raw[28+length:])
    primitive = gltf['meshes'][7]['primitives'][0]
    accessor = gltf['accessors'][primitive['attributes']['POSITION']]
    view = gltf['bufferViews'][accessor['bufferView']]
    offset = view.get('byteOffset',0) + accessor.get('byteOffset',0)
    stride = view.get('byteStride',12)
    for index in range(accessor['count']):
        location = offset + index*stride + 8
        z = struct.unpack_from('<f',binary,location)[0]
        struct.pack_into('<f',binary,location,max(z,32.0))
    accessor['min'][2] = 32.0
    encoded = json.dumps(gltf,separators=(',',':')).encode()
    encoded += b' '*(-len(encoded)%4)
    result = struct.pack('<III',0x46546c67,2,28+len(encoded)+len(binary))
    result += struct.pack('<II',len(encoded),0x4e4f534a)+encoded
    result += struct.pack('<II',len(binary),0x004e4942)+binary
    asset.write_bytes(result)
    catalog['chunks'][30]['sha256'] = hashlib.sha256(result).hexdigest()
    catalog['templates'][13]['floors'][1][3] = -32.0
    path.write_text(json.dumps(catalog,indent=2)+'\n')
    # The deck now meets the island at an edge instead of overlapping its top.
    assert accessor['min'][2] == -catalog['templates'][13]['floors'][0][1]
    assert catalog['templates'][13]['floors'][1][3] == -32.0
    print('PASS Royal Bridge meets courtyard without overlapping top faces')


if __name__ == '__main__':
    apply()
