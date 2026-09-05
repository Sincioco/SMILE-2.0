"""Preserve Orin's equipped source and export a body-only T-pose for Mixamo."""
from pathlib import Path
import json
import argparse
import bpy
from mathutils import Vector, Matrix, Quaternion

ROOT = Path(__file__).resolve().parents[1] / 'games/SinStarI/SourceAssets/Characters/Tank/OrinV10'
parser = argparse.ArgumentParser()
parser.add_argument('--package', help='Canonical output package directory.')
parser.add_argument('--version', default='v1.0')
parser.add_argument('--t-pose', help='A clean neutral GLB; omit only to reproduce the initial rig diagnostic.')
args = parser.parse_args(__import__('sys').argv[__import__('sys').argv.index('--') + 1:] if '--' in __import__('sys').argv else [])
if args.package:
    ROOT = Path(args.package).resolve()
PREFIX = 'orin-' + args.version
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(Path(args.t_pose) if args.t_pose else ROOT / (PREFIX + '.original.glb')))
rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
rig = rigs[0] if len(rigs) == 1 else None
body = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name not in ('Weapon', 'Shield', 'Icosphere')]
equipment = [bpy.data.objects[n] for n in ('Weapon', 'Shield') if n in bpy.data.objects]
report = {'bones': len(rig.data.bones) if rig else 0, 'bodyMeshes': len(body), 'equipment': [o.name for o in equipment],
          'sourceBones': {b.name: {'head':list(b.head_local), 'tail':list(b.tail_local)} for b in rig.data.bones} if rig else {}}

def point_bone(name, direction):
    bone = rig.pose.bones[name]
    current = (bone.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
    correction = current.rotation_difference(Vector(direction).normalized())
    head = bone.head.copy()
    bone.matrix = Matrix.Translation(head) @ correction.to_matrix().to_4x4() @ Matrix.Translation(-head) @ bone.matrix
    bpy.context.view_layer.update()

# Tripo assigned some hip cloth vertices to hands/arms. Remove those remote
# influences before lifting the arms, preserving and normalizing local weights.
if not args.t_pose:
    report['repairedClothWeights'] = {}
    for name in ('tripo_part_12', 'tripo_part_13', 'tripo_part_16', 'tripo_part_17', 'tripo_part_18'):
        obj = bpy.data.objects[name]
        changed = 0
        for vertex in obj.data.vertices:
            weights = [(obj.vertex_groups[g.group].name, g.weight) for g in vertex.groups if g.weight > 0]
            local = [(n, w) for n, w in weights if not any(s in n for s in ('Hand', 'arm', 'Clavicle', 'Head', 'Neck'))]
            if len(local) != len(weights):
                for group in obj.vertex_groups:
                    group.remove([vertex.index])
                total = sum(w for _, w in local)
                if total <= 0:
                    local, total = [('Waist', 1)], 1
                for n, w in local:
                    obj.vertex_groups[n].add([vertex.index], w / total, 'REPLACE')
                changed += 1
        report['repairedClothWeights'][name] = changed
    for side, sign in [('L', 1), ('R', -1)]:
        for segment in ('Upperarm', 'Forearm'):
            point_bone(f'{side}_{segment}', (sign, 0, 0))
for obj in equipment:
    obj.hide_set(True)
    obj.hide_render = True
if rig:
    rig.hide_set(True)
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.shading.type = 'MATERIAL'
        area.spaces.active.region_3d.view_distance = 2.2
        area.spaces.active.region_3d.view_location = Vector((0, 0, 0.5))
        area.spaces.active.region_3d.view_rotation = Quaternion((1, 0, 0), 1.57079632679)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Blender' / (PREFIX + '-t-pose.blend')))
# The editable Blend retains the hidden equipment and source rig. Bake only the
# disposable export scene so Mixamo receives the actual T-pose, without a rig.
for obj in body:
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        if mod.type == 'ARMATURE':
            bpy.ops.object.modifier_apply(modifier=mod.name)
    world = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_world = world
    obj.vertex_groups.clear()
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(ROOT / (PREFIX + '-no-equipment-t-pose.glb')), use_selection=True, export_animations=False)
# FBX embeds from real image files; packed glTF buffers alone are insufficient.
texture_dir = ROOT / 'Textures'
texture_dir.mkdir(exist_ok=True)
for image in bpy.data.images:
    if image.type == 'IMAGE' and image.size[0] > 0:
        image.filepath_raw = str(texture_dir / (Path(image.name).stem + '.png'))
        image.file_format = 'PNG'
        image.save()
bpy.ops.export_scene.fbx(filepath=str(ROOT / (PREFIX + '-mixamo-upload.fbx')), use_selection=True,
    object_types={'MESH'}, apply_unit_scale=True, bake_anim=False, path_mode='COPY', embed_textures=True)
(ROOT / (PREFIX + '-source-inspection.json')).write_text(json.dumps(report, indent=2) + '\n')
print('ORIN_TPOSE_READY', json.dumps({k:v for k,v in report.items() if k != 'sourceBones'}))
