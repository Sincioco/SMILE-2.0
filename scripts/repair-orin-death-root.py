"""Blender: retain Mixamo armature-object fall motion in the accepted GLB's Root.

Only Death Root TRS channels change. Meshes, materials, skin, equipment and other
clips retain their original bytes. The first pose defines the existing facing.
"""
import argparse
import copy
import hashlib
import json
import struct
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector


def read_glb(path):
    raw = path.read_bytes()
    size = struct.unpack_from('<I', raw, 12)[0]
    doc = json.loads(raw[20:20 + size])
    return doc, bytearray(raw[28 + size:])


def values(doc, binary, index):
    a = doc['accessors'][index]
    v = doc['bufferViews'][a['bufferView']]
    width = {'SCALAR': 1, 'VEC3': 3, 'VEC4': 4}[a['type']]
    assert a['componentType'] == 5126
    offset = v.get('byteOffset', 0) + a.get('byteOffset', 0)
    stride = v.get('byteStride', width * 4)
    return [struct.unpack_from('<' + 'f' * width, binary, offset + i * stride) for i in range(a['count'])]


def matrix(node):
    if 'matrix' in node:
        return Matrix([node['matrix'][i:i + 4] for i in range(0, 16, 4)]).transposed()
    q = node.get('rotation', [0, 0, 0, 1])
    return Matrix.LocRotScale(Vector(node.get('translation', [0, 0, 0])),
        Quaternion((q[3], q[0], q[1], q[2])), Vector(node.get('scale', [1, 1, 1])))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--model', type=Path, required=True)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    assert args.output.resolve() != args.model.resolve(), 'Use a review output first'
    doc, binary = read_glb(args.model)
    before = copy.deepcopy(doc)
    # Tripo has a Root joint above Hip. Mixamo puts the whole-body transform there.
    root = next(i for i, n in enumerate(doc['nodes']) if n.get('name') == 'Root')
    clip = next(a for a in doc['animations'] if a['name'] == 'Death')
    root_pose = copy.deepcopy(doc['nodes'][root])
    times = None
    for channel in clip['channels']:
        if channel['target']['node'] != root:
            continue
        sampler = clip['samplers'][channel['sampler']]
        sample_times = [v[0] for v in values(doc, binary, sampler['input'])]
        if times is None or len(sample_times) > len(times):
            times = sample_times
        root_pose[channel['target']['path']] = values(doc, binary, sampler['output'])[0]
    assert times and len(times) > 1
    duration = max(max(v[0] for v in values(doc, binary, s['input'])) for s in clip['samplers'])
    times = [min(duration, frame / 30) for frame in range(round(duration * 30) + 1)]
    # A node may have a static armature parent, but no animated ancestor is allowed.
    parents = {child: i for i, n in enumerate(doc['nodes']) for child in n.get('children', [])}
    parent_world = Matrix.Identity(4)
    p = parents.get(root)
    while p is not None:
        assert not any(c['target']['node'] == p for c in clip['channels']), 'Animated parent needs baking too'
        parent_world = matrix(doc['nodes'][p]) @ parent_world
        p = parents.get(p)
    accepted_first = parent_world @ matrix(root_pose)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(args.source))
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    scene = bpy.context.scene
    scene.render.fps = 30
    end = rig.animation_data.action.frame_range[1]
    convert = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, -1, 0, 0), (0, 0, 0, 1)))
    scene.frame_set(1)
    bpy.context.view_layer.update()
    source_object_first = convert @ rig.matrix_world @ convert.inverted()
    # Bone axes are reoriented by FBX/glTF import. Align actual joint POSITIONS,
    # then transfer only the discarded object motion in that world coordinate frame.
    first_nodes = copy.deepcopy(doc['nodes'])
    for channel in clip['channels']:
        sampler = clip['samplers'][channel['sampler']]
        first_nodes[channel['target']['node']][channel['target']['path']] = values(doc, binary, sampler['output'])[0]
    def first_world(index):
        local = matrix(first_nodes[index])
        return first_world(parents[index]) @ local if index in parents else local
    names = ('Hip', 'Spine02', 'Head', 'L_Foot', 'R_Foot')
    source_points = np.array([tuple((convert @ rig.matrix_world @ rig.pose.bones[name].matrix).translation) for name in names])
    target_points = np.array([tuple(first_world(next(i for i, n in enumerate(doc['nodes']) if n.get('name') == name)).translation) for name in names])
    source_center, target_center = source_points.mean(0), target_points.mean(0)
    u, _, vt = np.linalg.svd((source_points - source_center).T @ (target_points - target_center))
    rotate = vt.T @ u.T
    assert np.linalg.det(rotate) > 0
    align = Matrix.Identity(4)
    for i in range(3):
        for j in range(3):
            align[i][j] = rotate[i, j]
    align.translation = Vector(target_center - rotate @ source_center)
    alignment_error = float(np.linalg.norm((source_points @ rotate.T + np.array(align.translation)) - target_points, axis=1).max())
    assert alignment_error < .005, alignment_error
    samples = {'translation': [], 'rotation': [], 'scale': []}
    previous = None
    for time in times:
        frame = min(end, 1 + time * 30)
        scene.frame_set(int(frame), subframe=frame - int(frame))
        source_object = convert @ rig.matrix_world @ convert.inverted()
        motion = align @ source_object @ source_object_first.inverted() @ align.inverted()
        position, rotation, scale = (parent_world.inverted() @ motion @ accepted_first).decompose()
        if previous and previous.dot(rotation) < 0:
            rotation.negate()
        previous = rotation.copy()
        samples['translation'].append(tuple(position))
        samples['rotation'].append((rotation.x, rotation.y, rotation.z, rotation.w))
        samples['scale'].append(tuple(scale))

    def accessor(rows, kind):
        while len(binary) % 4:
            binary.append(0)
        offset = len(binary)
        binary.extend(struct.pack('<' + 'f' * sum(len(r) for r in rows), *(x for r in rows for x in r)))
        view = len(doc['bufferViews'])
        doc['bufferViews'].append({'buffer': 0, 'byteOffset': offset, 'byteLength': len(binary) - offset})
        result = len(doc['accessors'])
        doc['accessors'].append({'bufferView': view, 'componentType': 5126, 'count': len(rows), 'type': kind})
        return result

    time_accessor = accessor([(t,) for t in times], 'SCALAR')
    doc['accessors'][time_accessor].update(min=[min(times)], max=[max(times)])
    clip['channels'] = [c for c in clip['channels'] if c['target']['node'] != root]
    for path, rows in samples.items():
        sampler = len(clip['samplers'])
        clip['samplers'].append({'input': time_accessor, 'output': accessor(rows, 'VEC4' if path == 'rotation' else 'VEC3'), 'interpolation': 'LINEAR'})
        clip['channels'].append({'sampler': sampler, 'target': {'node': root, 'path': path}})
    doc['buffers'][0]['byteLength'] = len(binary)
    for key in ('nodes', 'meshes', 'skins', 'materials', 'images', 'textures'):
        assert doc.get(key) == before.get(key), key
    assert [a for a in doc['animations'] if a['name'] != 'Death'] == [a for a in before['animations'] if a['name'] != 'Death']
    encoded = json.dumps(doc, separators=(',', ':')).encode()
    encoded += b' ' * (-len(encoded) % 4)
    binary += b'\0' * (-len(binary) % 4)
    output = struct.pack('<III', 0x46546C67, 2, 28 + len(encoded) + len(binary)) + struct.pack('<II', len(encoded), 0x4E4F534A) + encoded + struct.pack('<II', len(binary), 0x004E4942) + binary
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(output)
    report = {'source': args.source.name, 'sourceSha256': hashlib.sha256(args.source.read_bytes()).hexdigest(),
        'beforeSha256': hashlib.sha256(args.model.read_bytes()).hexdigest(), 'afterSha256': hashlib.sha256(output).hexdigest(),
        'policy': 'Bake armature object motion into Root, aligned to accepted Death frame zero; retain all other channels and assets.',
        'samples': len(times), 'duration': max(times), 'alignmentMaximumError': alignment_error,
        'firstRoot': samples['rotation'][0], 'lastRoot': samples['rotation'][-1]}
    args.output.with_suffix('.repair.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report))


main()
