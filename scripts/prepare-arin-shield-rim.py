"""Derive eight bind-local shield perimeter sockets; preserve the old flame anchors."""
import argparse
import json
import struct
from pathlib import Path
import numpy as np

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--model', type=Path, required=True)
parser.add_argument('--descriptor', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
raw = args.model.read_bytes()
length = struct.unpack_from('<I', raw, 12)[0]
doc = json.loads(raw[20:20 + length])
binary = raw[28 + length:]


def accessor(index):
    a = doc['accessors'][index]
    v = doc['bufferViews'][a['bufferView']]
    dtype = np.dtype({5126: '<f4', 5123: '<u2', 5121: 'u1', 5125: '<u4'}[a['componentType']])
    size = {'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[a['type']]
    return np.ndarray((a['count'], size), dtype=dtype, buffer=binary,
        offset=v.get('byteOffset', 0) + a.get('byteOffset', 0),
        strides=(v.get('byteStride', size*dtype.itemsize), dtype.itemsize)).copy()


node = next(n for n in doc['nodes'] if n.get('name') == 'ArinShield')
skin = doc['skins'][node['skin']]
joint = next(i for i, n in enumerate(skin['joints']) if doc['nodes'][n]['name'] == 'mixamorig:LeftHand')
primitive = doc['meshes'][node['mesh']]['primitives'][0]
a = primitive['attributes']
joints, weights = accessor(a['JOINTS_0']), accessor(a['WEIGHTS_0'])
assert np.all(np.sum(np.where(joints == joint, weights, 0), axis=1) > .999)
points = accessor(a['POSITION'])
inverse = accessor(skin['inverseBindMatrices'])[joint].reshape(4, 4).T
points = (inverse @ np.column_stack((points, np.ones(len(points)))).T).T[:, :3]
center = points.mean(axis=0)
_, _, axes = np.linalg.svd(points-center, full_matrices=False)
projected = (points-center) @ axes[:2].T
order = sorted(range(len(points)), key=lambda i: tuple(projected[i]))


def cross(a, b, c):
    u, v = projected[b]-projected[a], projected[c]-projected[a]
    return u[0]*v[1]-u[1]*v[0]


halves = []
for sequence in (order, order[::-1]):
    half = []
    for i in sequence:
        while len(half) >= 2 and cross(half[-2], half[-1], i) <= 0:
            half.pop()
        half.append(i)
    halves.append(half[:-1])
hull = halves[0]+halves[1]
while len(hull) > 8:
    i = min(range(len(hull)), key=lambda i: abs(cross(hull[i-1], hull[i], hull[(i+1) % len(hull)])))
    hull.pop(i)
assert len(hull) == 8
descriptor = json.loads(args.descriptor.read_text())
for i, vertex in enumerate(hull):
    descriptor['sockets'][f'ShieldRim{i}'] = {
        'node': 'mixamorig:LeftHand', 'translation': np.round(points[vertex], 8).tolist()}
descriptor['clips']['Death'] = {'loop': False}
args.output.write_text(json.dumps(descriptor, indent=2)+'\n')
print(json.dumps({'rim': [points[i].tolist() for i in hull], 'socketCount': len(descriptor['sockets'])}))
