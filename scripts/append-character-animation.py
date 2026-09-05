"""Append one compatible GLB animation while retaining accepted mesh/texture bytes.

This is not retargeting: named node hierarchy and rest transforms must match.
Use an explicit review output first, then inspect deformation before promotion.
"""
import argparse
import copy
import hashlib
import json
import struct
from pathlib import Path


def read_glb(path):
    raw = path.read_bytes()
    assert struct.unpack_from('<II', raw) == (0x46546C67, 2), 'Expected GLB 2.0'
    length, kind = struct.unpack_from('<II', raw, 12)
    assert kind == 0x4E4F534A
    doc = json.loads(raw[20:20 + length])
    size, kind = struct.unpack_from('<II', raw, 20 + length)
    assert kind == 0x004E4942 and len(doc['buffers']) == 1
    return doc, raw[28 + length:28 + length + size]


def append_clip(base, source, clip_name):
    doc, binary = read_glb(base)
    donor, donor_binary = read_glb(source)
    source_clip = next(a for a in donor['animations'] if a['name'] == clip_name)
    assert all(a['name'] != clip_name for a in doc['animations']), 'Clip already exists'
    names = {n.get('name'): i for i, n in enumerate(doc['nodes'])}
    assert len(names) == len(doc['nodes']), 'Target node names must be unique'
    parents = {child: n.get('name') for n in doc['nodes'] for child in n.get('children', [])}
    donor_parents = {child: n.get('name') for n in donor['nodes'] for child in n.get('children', [])}
    mapping = {}
    for i, node in enumerate(donor['nodes']):
        assert node.get('name') in names, f"Missing node {node.get('name')}"
        target_index = names[node.get('name')]
        target = doc['nodes'][target_index]
        assert parents.get(target_index) == donor_parents.get(i), 'Different parent hierarchy'
        for key in ('matrix', 'translation', 'rotation', 'scale'):
            assert target.get(key) == node.get(key), f'Different rest {key}: {node.get("name")}'
        mapping[i] = target_index

    result = copy.deepcopy(doc)
    payload = bytearray(binary)
    views, accessors = {}, {}

    def copy_accessor(index):
        if index in accessors:
            return accessors[index]
        accessor = copy.deepcopy(donor['accessors'][index])
        assert 'sparse' not in accessor, 'Sparse animation data is unsupported'
        view_index = accessor['bufferView']
        if view_index not in views:
            view = copy.deepcopy(donor['bufferViews'][view_index])
            assert view.get('buffer', 0) == 0
            offset = view.get('byteOffset', 0)
            while len(payload) % 4:
                payload.append(0)
            view['byteOffset'] = len(payload)
            payload.extend(donor_binary[offset:offset + view['byteLength']])
            views[view_index] = len(result['bufferViews'])
            result['bufferViews'].append(view)
        accessor['bufferView'] = views[view_index]
        accessors[index] = len(result['accessors'])
        result['accessors'].append(accessor)
        return accessors[index]

    clip = copy.deepcopy(source_clip)
    for sampler in clip['samplers']:
        sampler['input'] = copy_accessor(sampler['input'])
        sampler['output'] = copy_accessor(sampler['output'])
    for channel in clip['channels']:
        channel['target']['node'] = mapping[channel['target']['node']]
    result['animations'].append(clip)
    result['buffers'][0]['byteLength'] = len(payload)
    while len(payload) % 4:
        payload.append(0)
    header = json.dumps(result, separators=(',', ':')).encode()
    header += b' ' * ((-len(header)) % 4)
    raw = (struct.pack('<III', 0x46546C67, 2, 28 + len(header) + len(payload)) +
           struct.pack('<II', len(header), 0x4E4F534A) + header +
           struct.pack('<II', len(payload), 0x004E4942) + payload)
    report = {
        'baseSha256': hashlib.sha256(base.read_bytes()).hexdigest(),
        'donorSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'outputSha256': hashlib.sha256(raw).hexdigest(),
        'clipAdded': clip_name,
        'originalClipsPreserved': [a['name'] for a in doc['animations']],
        'originalBinaryBytesPreserved': len(binary),
        'restTransformsAndHierarchyMatch': True,
        'meshSkinMaterialTextureMetadataUnchanged': all(
            result.get(k) == doc.get(k) for k in ('nodes', 'meshes', 'skins', 'materials', 'textures', 'images')),
    }
    assert payload[:len(binary)] == binary
    return raw, report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', type=Path, required=True)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--clip', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    assert args.output.resolve() not in (args.base.resolve(), args.source.resolve()), 'Use a new review output'
    raw, report = append_clip(args.base, args.source, args.clip)
    args.output.write_bytes(raw)
    args.report.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report, indent=2))
