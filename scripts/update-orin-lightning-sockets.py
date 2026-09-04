"""Derive Orin's rigid equipment VFX sockets without modifying his GLB.

Run with the existing Python/NumPy asset environment after a checkpoint rebuild.
The outline is a simplified convex perimeter in the shield's principal plane.
"""
import json
import struct
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / 'games/SinStarI/SourceAssets/Characters/Tank/OrinV13'
raw = (PACKAGE / 'orin-v1.3-animation-checkpoint.glb').read_bytes()
length = struct.unpack_from('<I', raw, 12)[0]
doc = json.loads(raw[20:20 + length])
binary = raw[28 + length:]


def accessor(index):
    a = doc['accessors'][index]
    view = doc['bufferViews'][a['bufferView']]
    dtype = {5126: '<f4', 5123: '<u2', 5121: 'u1', 5125: '<u4'}[a['componentType']]
    size = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[a['type']]
    offset = view.get('byteOffset', 0) + a.get('byteOffset', 0)
    return np.ndarray((a['count'], size), dtype=dtype, buffer=binary, offset=offset,
                      strides=(view.get('byteStride', size * np.dtype(dtype).itemsize), np.dtype(dtype).itemsize)).copy()


def hand_vertices(node_name, hand_name):
    node = next(n for n in doc['nodes'] if n.get('name') == node_name)
    skin = doc['skins'][node['skin']]
    joint = next(i for i, n in enumerate(skin['joints']) if doc['nodes'][n]['name'] == hand_name)
    prim = doc['meshes'][node['mesh']]['primitives'][0]
    joints = accessor(prim['attributes']['JOINTS_0'])
    weights = accessor(prim['attributes']['WEIGHTS_0'])
    assert np.all(np.sum(np.where(joints == joint, weights, 0), axis=1) > .999), 'Equipment must be rigid to its hand.'
    points = accessor(prim['attributes']['POSITION'])
    inverse_bind = accessor(skin['inverseBindMatrices'])[joint].reshape(4, 4).T
    return (inverse_bind @ np.column_stack((points, np.ones(len(points)))).T).T[:, :3]


shield = hand_vertices('00_Shield', 'L_Hand')
center = shield.mean(axis=0)
_, _, axes = np.linalg.svd(shield - center, full_matrices=False)
projected = (shield - center) @ axes[:2].T
order = sorted(range(len(projected)), key=lambda i: tuple(projected[i]))


def cross(a, b, c):
    u, v = projected[b] - projected[a], projected[c] - projected[a]
    return u[0] * v[1] - u[1] * v[0]


halves = []
for sequence in (order, order[::-1]):
    half = []
    for i in sequence:
        while len(half) >= 2 and cross(half[-2], half[-1], i) <= 0:
            half.pop()
        half.append(i)
    halves.append(half[:-1])
hull = halves[0] + halves[1]
while len(hull) > 8:
    remove = min(range(len(hull)), key=lambda i: abs(cross(hull[i - 1], hull[i], hull[(i + 1) % len(hull)])))
    hull.pop(remove)

descriptor_path = PACKAGE / 'OrinV13.sm3d.json'
descriptor = json.loads(descriptor_path.read_text())
for i, vertex in enumerate(hull):
    descriptor['sockets'][f'ShieldRim{i}'] = {'node': 'L_Hand', 'translation': np.round(shield[vertex], 8).tolist()}

hammer = hand_vertices('01_Weapon', 'R_Hand')
tip = np.array(descriptor['sockets']['SwordTip']['translation'])
grip = np.array(descriptor['sockets']['SwordBase']['translation'])
shaft = (tip - grip) / np.linalg.norm(tip - grip)
head = hammer[((hammer - grip) @ shaft) > np.linalg.norm(tip - grip) * .72]
head_center = head.mean(axis=0)
_, _, head_axes = np.linalg.svd(head - head_center, full_matrices=False)
head_axis = head_axes[0]
head_projection = (head - head_center) @ head_axis
for name, point in [('HammerHead', head_center), ('HammerLeft', head_center + head_axis * head_projection.min()),
                    ('HammerRight', head_center + head_axis * head_projection.max())]:
    descriptor['sockets'][name] = {'node': 'R_Hand', 'translation': np.round(point, 8).tolist()}
descriptor_path.write_text(json.dumps(descriptor, indent=2) + '\n')
print(json.dumps({'socketCount': len(descriptor['sockets']), 'rim': [shield[i].tolist() for i in hull],
                  'head': head_center.tolist()}, indent=2))
