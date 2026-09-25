"""GLB pose sampling and body measurements for the town locomotion authoring tool.

Runs with Blender's bundled mathutils/numpy; does not rewrite mesh or skin data.
"""
import bisect
import copy
import json
import struct
import numpy as np
from mathutils import Matrix, Quaternion, Vector


class MotionData:
    def __init__(self, path, body_names):
        raw = path.read_bytes()
        size = struct.unpack_from('<I', raw, 12)[0]
        self.doc = json.loads(raw[20:20+size])
        self.binary = raw[28+size:]
        self.names = {n['name']: i for i, n in enumerate(self.doc['nodes']) if 'name' in n}
        self.parents = {c: i for i, n in enumerate(self.doc['nodes']) for c in n.get('children', [])}
        self.rest = [self.node_matrix(n) for n in self.doc['nodes']]
        self.bind = self.world(self.rest)
        self.clips = {a['name']: a for a in self.doc['animations']}
        self.tracks = {}
        for name, animation in self.clips.items():
            self.tracks[name] = [(c['target']['node'], c['target']['path'],
                self.accessor(animation['samplers'][c['sampler']]['input'])[:, 0],
                self.accessor(animation['samplers'][c['sampler']]['output'])) for c in animation['channels']]
        self.meshes = []
        for node in self.doc['nodes']:
            if 'skin' not in node or not body_names(node.get('name', '')):
                continue
            skin = self.doc['skins'][node['skin']]
            inverse = self.accessor(skin['inverseBindMatrices']).reshape(-1, 4, 4).transpose(0, 2, 1)
            cape = next((i for i, n in enumerate(skin['joints'])
                         if self.doc['nodes'][n].get('name') == 'MiraCape'), None)
            for primitive in self.doc['meshes'][node['mesh']]['primitives']:
                attrs = primitive['attributes']
                positions = self.accessor(attrs['POSITION'])
                joints = self.accessor(attrs['JOINTS_0'])
                weights = self.accessor(attrs['WEIGHTS_0'])
                keep = np.ones(len(positions), dtype=bool)
                if cape is not None:
                    keep &= ~np.any((joints == cape) & (weights > .0001), axis=1)
                self.meshes.append((np.column_stack((positions[keep], np.ones(sum(keep)))),
                    joints[keep], weights[keep], skin['joints'], inverse))

    def accessor(self, index):
        a = self.doc['accessors'][index]
        v = self.doc['bufferViews'][a['bufferView']]
        size = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[a['type']]
        dt = np.dtype({5126: '<f4', 5123: '<u2', 5121: 'u1', 5125: '<u4'}[a['componentType']])
        return np.ndarray((a['count'], size), dtype=dt, buffer=self.binary,
            offset=v.get('byteOffset', 0)+a.get('byteOffset', 0),
            strides=(v.get('byteStride', size*dt.itemsize), dt.itemsize)).copy()

    @staticmethod
    def node_matrix(node):
        if 'matrix' in node:
            return Matrix(np.array(node['matrix']).reshape(4, 4).T.tolist())
        q = node.get('rotation', [0, 0, 0, 1])
        return Matrix.LocRotScale(Vector(node.get('translation', [0, 0, 0])),
            Quaternion((q[3], *q[:3])), Vector(node.get('scale', [1, 1, 1])))

    def world(self, local):
        cache = {}
        def visit(i):
            if i not in cache:
                cache[i] = visit(self.parents[i]) @ local[i] if i in self.parents else local[i].copy()
            return cache[i]
        return [visit(i) for i in range(len(local))]

    def duration(self, name):
        return max(float(t[-1]) for _, _, t, _ in self.tracks[name])

    def sample(self, name, time):
        values = [dict(zip(('translation', 'rotation', 'scale'), m.decompose())) for m in self.rest]
        for node, kind, times, rows in self.tracks[name]:
            hi = min(len(times)-1, max(1, bisect.bisect_left(times, time)))
            lo = max(0, hi-1)
            f = min(1, max(0, (time-times[lo])/max(1e-9, times[hi]-times[lo])))
            a, b = rows[lo], rows[hi]
            if kind == 'rotation':
                values[node][kind] = Quaternion((a[3], *a[:3])).slerp(Quaternion((b[3], *b[:3])), f)
            else:
                values[node][kind] = Vector(a).lerp(Vector(b), f)
        return [Matrix.LocRotScale(v['translation'], v['rotation'], v['scale']) for v in values]

    def minimum(self, world):
        lows = []
        for verts, joints, weights, nodes, inverse in self.meshes:
            matrices = np.array([list(map(list, world[n])) for n in nodes]) @ inverse
            y = np.sum(np.einsum('vkij,vj->vki', matrices[joints], verts)[:, :, 1]*weights, axis=1)
            lows.append(float(y.min()))
        return min(lows)

    def write_town_variant(self, output, motions):
        """Only private town Walk/Run tracks change; all original binary bytes remain."""
        doc = copy.deepcopy(self.doc)
        payload = bytearray(self.binary)
        def accessor(rows, kind):
            array = np.asarray(rows, dtype='<f4')
            payload.extend(b'\0'*(-len(payload) % 4))
            view = len(doc['bufferViews'])
            doc['bufferViews'].append({'buffer': 0, 'byteOffset': len(payload), 'byteLength': array.nbytes})
            payload.extend(array.tobytes())
            index = len(doc['accessors'])
            doc['accessors'].append({'bufferView': view, 'componentType': 5126, 'count': len(rows),
                'type': kind, 'min': array.min(axis=0).tolist(), 'max': array.max(axis=0).tolist()})
            return index
        for name, (times, frames, nodes) in motions.items():
            animation = {'name': name, 'channels': [], 'samplers': []}
            time_accessor = accessor([[t] for t in times], 'SCALAR')
            for node in nodes:
                decomposed = [frame[node].decompose() for frame in frames]
                rotations = []
                previous = None
                for _, q, _ in decomposed:
                    if previous is not None and q.dot(previous) < 0:
                        q.negate()
                    rotations.append([q.x, q.y, q.z, q.w]); previous = q
                for kind, form, rows in [('translation', 'VEC3', [list(v[0]) for v in decomposed]),
                        ('rotation', 'VEC4', rotations), ('scale', 'VEC3', [list(v[2]) for v in decomposed])]:
                    animation['channels'].append({'sampler': len(animation['samplers']),
                        'target': {'node': node, 'path': kind}})
                    animation['samplers'].append({'input': time_accessor, 'output': accessor(rows, form),
                        'interpolation': 'LINEAR'})
            index = next(i for i, a in enumerate(doc['animations']) if a['name'] == name)
            doc['animations'][index] = animation
        doc['buffers'][0]['byteLength'] = len(payload)
        encoded = json.dumps(doc, separators=(',', ':')).encode()
        encoded += b' '*(-len(encoded) % 4)
        payload.extend(b'\0'*(-len(payload) % 4))
        output.write_bytes(struct.pack('<4sII', b'glTF', 2, 28+len(encoded)+len(payload))+
            struct.pack('<I4s', len(encoded), b'JSON')+encoded+
            struct.pack('<I4s', len(payload), b'BIN\0')+payload)
        assert payload[:len(self.binary)] == self.binary
        assert all(doc[k] == self.doc[k] for k in ('nodes', 'meshes', 'skins', 'materials', 'textures', 'images'))
        assert all(a == b for a, b in zip(doc['animations'], self.doc['animations']) if a['name'] not in motions)
