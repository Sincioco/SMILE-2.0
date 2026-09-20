"""Append one Mixamo Victory clip without rewriting accepted GLB model/clip data.

Run with the installed Blender: --background --python this_file -- Arin|Mira|Zara.
The original authoring checkpoint remains intact; a Victory checkpoint and audit
are saved in the character package. Existing animation indices remain unchanged.
"""
import copy
import hashlib
import json
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parent.parent
CHARACTERS = ROOT / 'games/SinStarI/SourceAssets/Characters'
CONFIG = {
    'Arin': ('Paladin/ArinV57', 'Blender/arin-v5.7-nine-clips.blend',
             'arin-v5.7-mixamo-victory-without-skin.fbx',
             'arin-v5.7-idle-equipment-checkpoint.glb', 'ArinV57.sm3d.json',
             'Blender/arin-v5.7-victory.blend', 'Calibration/victory-import.json'),
    'Mira': ('Healer/MiraTripoV1', 'Blender/mira-rigged-animation-checkpoint.blend',
             'Source/Mixamo/Victory.fbx', 'mira-animation-checkpoint.glb',
             'Mira.sm3d.json', 'Blender/mira-victory.blend', 'Source/victory-import.json'),
    'Zara': ('Warrior/ZaraV1', 'Private/Zara-v1-animation-source.blend',
             'Private/Mixamo/Victory.fbx', 'Private/Zara-v1-animation-set.glb',
             'ZaraV1.sm3d.json', 'Private/Zara-v1-victory.blend', 'victory-import.json'),
}


def read_glb(path):
    raw = path.read_bytes()
    assert struct.unpack_from('<4sI', raw) == (b'glTF', 2)
    size = struct.unpack_from('<I', raw, 12)[0]
    document = json.loads(raw[20:20 + size])
    binary_size = struct.unpack_from('<I', raw, 20 + size)[0]
    return document, raw[28 + size:28 + size + binary_size]


def write_glb(path, document, binary):
    encoded = json.dumps(document, separators=(',', ':')).encode()
    encoded += b' ' * (-len(encoded) % 4)
    output = bytearray(binary)
    output.extend(b'\0' * (-len(output) % 4))
    path.write_bytes(struct.pack('<4sII', b'glTF', 2, 28 + len(encoded) + len(output)) +
                     struct.pack('<I4s', len(encoded), b'JSON') + encoded +
                     struct.pack('<I4s', len(output), b'BIN\0') + output)


def fold_zara_armature(path):
    """Match the accepted GLB's removed object parent, including animated root TRS.

    Blender's armature-removal export does not scale/rotate root translations for
    this centimeter rig. Export the full hierarchy and compose that static parent
    explicitly; never change accepted skin, mesh or inverse-bind data.
    """
    document, data = read_glb(path)
    binary = bytearray(data)
    nodes = document['nodes']
    parent = next(i for i, n in enumerate(nodes) if n.get('name') == 'Armature')
    root = next(i for i, n in enumerate(nodes) if n.get('name') == 'root')
    assert root in nodes[parent]['children']
    p = nodes[parent]
    q = p.get('rotation', [0, 0, 0, 1])
    rotation = Quaternion((q[3], *q[:3]))
    scale = Vector(p.get('scale', [1, 1, 1]))
    assert max(scale) - min(scale) < .000001
    transform = Matrix.LocRotScale(Vector(p.get('translation', [0, 0, 0])), rotation, scale)
    for animation in document['animations']:
        for channel in animation['channels']:
            assert channel['target']['node'] != parent
            if channel['target']['node'] != root:
                continue
            a = document['accessors'][animation['samplers'][channel['sampler']]['output']]
            v = document['bufferViews'][a['bufferView']]
            kind = channel['target']['path']
            size = 4 if kind == 'rotation' else 3
            assert a['componentType'] == 5126
            values = []
            for index in range(a['count']):
                offset = v.get('byteOffset', 0) + a.get('byteOffset', 0) + index * v.get('byteStride', size * 4)
                value = struct.unpack_from('<' + 'f' * size, binary, offset)
                if kind == 'translation':
                    value = transform @ Vector(value)
                elif kind == 'rotation':
                    q = rotation @ Quaternion((value[3], *value[:3]))
                    value = (q.x, q.y, q.z, q.w)
                else:
                    assert kind == 'scale'
                    value = tuple(value[i] * scale[i] for i in range(3))
                struct.pack_into('<' + 'f' * size, binary, offset, *value)
                values.append(value)
            for field, operation in (('min', min), ('max', max)):
                if field in a:
                    a[field] = [operation(value[i] for value in values) for i in range(size)]
    n = nodes[root]
    q = n.get('rotation', [0, 0, 0, 1])
    local = Matrix.LocRotScale(Vector(n.get('translation', [0, 0, 0])),
                              Quaternion((q[3], *q[:3])), Vector(n.get('scale', [1, 1, 1])))
    translation, q, scale = (transform @ local).decompose()
    n.update(translation=list(translation), rotation=[q.x, q.y, q.z, q.w], scale=list(scale))
    nodes[parent]['children'].remove(root)
    document['scenes'][document.get('scene', 0)]['nodes'].append(root)
    write_glb(path, document, binary)


def append_animation(base_path, clip_path):
    document, binary = read_glb(base_path)
    incoming, incoming_binary = read_glb(clip_path)
    original = copy.deepcopy(document)
    assert not any(a['name'] == 'Victory' for a in document['animations'])
    animation = copy.deepcopy(next(a for a in incoming['animations'] if a['name'] == 'Victory'))
    node_names = {n['name']: i for i, n in enumerate(document['nodes']) if 'name' in n}
    def parents(model):
        return {child: model['nodes'][index].get('name')
                for index, node in enumerate(model['nodes']) for child in node.get('children', [])}
    existing_parents, incoming_parents = parents(document), parents(incoming)
    accessor_map, view_map = {}, {}
    output = bytearray(binary)

    def accessor(index):
        if index in accessor_map:
            return accessor_map[index]
        item = copy.deepcopy(incoming['accessors'][index])
        view_index = item['bufferView']
        assert 'sparse' not in item
        if view_index not in view_map:
            view = copy.deepcopy(incoming['bufferViews'][view_index])
            start = view.get('byteOffset', 0)
            output.extend(b'\0' * (-len(output) % 4))
            view['byteOffset'] = len(output)
            view['buffer'] = 0
            output.extend(incoming_binary[start:start + view['byteLength']])
            view_map[view_index] = len(document['bufferViews'])
            document['bufferViews'].append(view)
        item['bufferView'] = view_map[view_index]
        accessor_map[index] = len(document['accessors'])
        document['accessors'].append(item)
        return accessor_map[index]

    for channel in animation['channels']:
        source_index = channel['target']['node']
        name = incoming['nodes'][source_index]['name']
        assert name in node_names, f'New animation node is absent from accepted model: {name}'
        assert incoming_parents.get(source_index) == existing_parents.get(node_names[name]), (
            f'Animation parent differs from accepted model: {name}')
        channel['target']['node'] = node_names[name]
    for sampler in animation['samplers']:
        sampler['input'] = accessor(sampler['input'])
        sampler['output'] = accessor(sampler['output'])
    document['animations'].append(animation)
    document['buffers'][0]['byteLength'] = len(output)
    for field in ('nodes', 'meshes', 'skins', 'materials', 'textures', 'images', 'scenes'):
        assert document.get(field) == original.get(field), field
    assert document['animations'][:-1] == original['animations']
    assert output[:len(binary)] == binary
    write_glb(base_path, document, output)
    return {'preservedModelAndOldAnimationBytes': True,
            'animationParentNamesMatch': True,
            'previousClips': [a['name'] for a in original['animations']],
            'newClipIndex': len(original['animations']), 'newAnimationChannels': len(animation['channels'])}


def activate(rig, action, frame):
    rig.animation_data_create()
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()


def write_pose(bone, matrix, frame, parent=None):
    kwargs = {}
    if bone.parent:
        kwargs = {'parent_matrix': parent if parent is not None else bone.parent.matrix,
                  'parent_matrix_local': bone.parent.bone.matrix_local}
    bone.matrix_basis = bone.bone.convert_local_to_pose(matrix, bone.bone.matrix_local,
                                                       invert=True, **kwargs)
    bone.rotation_mode = 'QUATERNION'
    for prop in ('location', 'rotation_quaternion', 'scale'):
        bone.keyframe_insert(data_path=prop, frame=frame)


def minimum(meshes, body_only=False):
    bpy.context.view_layer.update()
    graph = bpy.context.evaluated_depsgraph_get()
    values = []
    for obj in meshes:
        vertices = obj.evaluated_get(graph).data.vertices
        cape_group = obj.vertex_groups.get('MiraCape') if body_only else None
        for vertex in vertices:
            if cape_group and any(g.group == cape_group.index and g.weight > .0001
                                  for g in obj.data.vertices[vertex.index].groups):
                continue
            values.append((obj.matrix_world @ vertex.co).z)
    return min(values)


def main(character):
    relative, blend, fbx, model, descriptor, checkpoint, audit = CONFIG[character]
    package = CHARACTERS / relative
    model = package / model
    before_hash = hashlib.sha256(model.read_bytes()).hexdigest()
    assert not any(a['name'] == 'Victory' for a in read_glb(model)[0]['animations']), 'Already has Victory'
    bpy.ops.wm.open_mainfile(filepath=str(package / blend))
    scene = bpy.context.scene
    scene.render.fps = 30
    rig = next(o for o in scene.objects if o.type == 'ARMATURE')
    meshes = [o for o in scene.objects if o.type == 'MESH' and
              (o.parent == rig or any(m.type == 'ARMATURE' and m.object == rig for m in o.modifiers))]
    body = [o for o in meshes if (o.name.startswith('tripo_part') if character == 'Arin' else
            o.name == 'Mira.Body' if character == 'Mira' else o.name != 'Zara_Part_03')]
    idle = bpy.data.actions['Idle']
    activate(rig, idle, int(idle.frame_range[0]))
    idle_low = minimum(body, True)
    reference = {p.name: p.matrix_basis.copy() for p in rig.pose.bones}
    grip_bone = 'MiraStaff' if character == 'Mira' else 'Weapon' if character == 'Zara' else None
    hand_name = 'hand_r' if character == 'Zara' else 'mixamorig:RightHand'
    grip = (rig.pose.bones[hand_name].matrix.inverted() @ rig.pose.bones[grip_bone].matrix
            if grip_bone else None)
    rig.data.pose_position = 'REST'
    bind_low = minimum(body, True)
    rig.data.pose_position = 'POSE'
    before_objects = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(package / fbx))
    imported = set(bpy.data.objects) - before_objects
    source = next(o for o in imported if o.type == 'ARMATURE')
    source_action = source.animation_data.action
    first, last = map(round, source_action.frame_range)
    conversion = rig.matrix_world.inverted() @ source.matrix_world
    missing = set(p.name for p in rig.pose.bones) - set(p.name for p in source.pose.bones)
    assert missing <= {'MiraStaff', 'MiraCape'}, f'Skeleton mismatch: {sorted(missing)}'
    action = bpy.data.actions.new('Victory')
    action.use_fake_user = True
    activate(rig, action, first)
    samples = []
    for source_frame in range(first, last + 1):
        scene.frame_set(source_frame)
        bpy.context.view_layer.update()
        desired = {}
        for p in source.pose.bones:
            matrix = conversion @ p.matrix
            desired[p.name] = Matrix.LocRotScale(matrix.translation, matrix.to_quaternion(), Vector((1, 1, 1)))
        samples.append(desired)
    for frame, desired in enumerate(samples, 1):
        for p in rig.pose.bones:
            if p.name in desired:
                write_pose(p, desired[p.name], frame, desired.get(p.parent.name) if p.parent else None)
            else:
                p.matrix_basis = reference[p.name]
                for prop in ('location', 'rotation_quaternion', 'scale'):
                    p.keyframe_insert(data_path=prop, frame=frame)
        # Keep the accepted neutral wrists and closed equipment grip on this new clip.
        for p in rig.pose.bones:
            if character in ('Arin', 'Mira') and ('Hand' in p.name):
                if character == 'Arin' or 'RightHand' in p.name:
                    p.rotation_quaternion = reference[p.name].to_quaternion()
                    p.keyframe_insert(data_path='rotation_quaternion', frame=frame)
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.actions.remove(source_action)
    hips = rig.pose.bones['pelvis' if character == 'Zara' else 'mixamorig:Hips']
    # Correct new-clip floor penetration; preserve authored airtime above the floor.
    contact = []
    for frame in range(1, len(samples) + 1):
        activate(rig, action, frame)
        low = minimum(body, True)
        pose = hips.matrix.copy()
        pose.translation += rig.matrix_world.inverted().to_3x3() @ Vector((0.0, 0.0, max(0.0, idle_low - low)))
        write_pose(hips, pose, frame)
        bpy.context.view_layer.update()
        if grip_bone:
            write_pose(rig.pose.bones[grip_bone], rig.pose.bones[hand_name].matrix @ grip, frame)
        contact.append({'frame': frame - 1, 'bodyMinimumY': minimum(body, True),
                        'sceneMinimumY': minimum(meshes)})
    assert min(row['bodyMinimumY'] for row in contact) >= idle_low - .0001, (idle_low, min(row['bodyMinimumY'] for row in contact))
    activate(rig, action, 1)
    scene.frame_start, scene.frame_end = 1, len(samples)
    bpy.ops.wm.save_as_mainfile(filepath=str(package / checkpoint), compress=True)
    # Export only this action; use the same armature-root convention as the accepted asset.
    for other in list(bpy.data.actions):
        if other != action:
            bpy.data.actions.remove(other)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [rig, *meshes]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    temp = ROOT / f'artifacts/temp/{character.lower()}-victory-only.glb'
    bpy.ops.export_scene.gltf(filepath=str(temp), export_format='GLB', use_selection=True,
        export_animations=True, export_animation_mode='ACTIONS', export_merge_animation='ACTION',
        export_anim_single_armature=True, export_armature_object_remove=character == 'Arin',
        export_rest_position_armature=True, export_reset_pose_bones=False,
        export_optimize_animation_keep_anim_armature=False, export_skins=True)
    if character == 'Zara':
        fold_zara_armature(temp)
    preserved = append_animation(model, temp)
    description_path = package / descriptor
    description = json.loads(description_path.read_text(encoding='utf-8-sig'))
    description['clips']['Victory'] = {'loop': False}
    description_path.write_text(json.dumps(description, indent=2) + '\n')
    report = {'character': character, 'source': fbx, 'sourceSha256': hashlib.sha256((package / fbx).read_bytes()).hexdigest(),
              'checkpoint': checkpoint, 'checkpointSha256': hashlib.sha256((package / checkpoint).read_bytes()).hexdigest(),
              'previousModelSha256': before_hash, 'modelSha256': hashlib.sha256(model.read_bytes()).hexdigest(),
              'sampleRate': 30, 'frames': len(samples), 'bindBodyMinimumY': bind_low,
              'idleFrameZeroBodyMinimumY': idle_low, 'victoryFrameZero': contact[0],
              'minimumBodyY': min(r['bodyMinimumY'] for r in contact),
              'minimumSceneY': min(r['sceneMinimumY'] for r in contact),
              'samples': [contact[0], contact[len(contact)//2], contact[-1]], **preserved}
    (package / audit).write_text(json.dumps(report, indent=2) + '\n')
    print('VICTORY_APPENDED', json.dumps(report), flush=True)


if __name__ == '__main__':
    main(sys.argv[sys.argv.index('--') + 1])
