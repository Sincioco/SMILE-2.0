"""Retarget Zara's unarmed Walk/Run into private town variants of three heroes.

Run in background Blender. Licensed source motion and derived GLBs stay local in
each canonical character package; accepted combat models and saved poses are untouched.
"""
from pathlib import Path
import hashlib
import importlib.util
import json
import math
import sys
import numpy as np
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[1]
PACKAGES = ROOT/'games/SinStarI/SourceAssets/Characters'
spec = importlib.util.spec_from_file_location('motion_data', ROOT/'scripts/character-motion-data.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
MotionData = module.MotionData
SOURCE = PACKAGES/'Warrior/ZaraV1/Private/Zara-v1-animation-set.glb'
CONFIG = {
    'Zara': ('Warrior/ZaraV1', 'Private/Zara-v1-animation-set.glb',
             lambda name: name.startswith('Zara_Part') and name != 'Zara_Part_03'),
    'Arin': ('Paladin/ArinV57', 'arin-v5.7-idle-equipment-checkpoint.glb',
             lambda name: name.startswith('tripo_part')),
    'Orin': ('Tank/OrinV13', 'orin-v1.3-animation-checkpoint.glb', lambda name: name == '02_Body'),
    'Mira': ('Healer/MiraTripoV1', 'mira-animation-checkpoint.glb', lambda name: name == 'Mira.Body'),
}


def mapping(character):
    if character == 'Orin':
        result = {'Hip': 'pelvis', 'Waist': 'spine_01', 'Spine01': 'spine_02',
                  'Spine02': 'spine_03', 'NeckTwist01': 'neck_01', 'Head': 'head'}
        limbs = {'Clavicle': 'clavicle', 'Upperarm': 'upperarm', 'Forearm': 'lowerarm',
                 'Hand': 'hand', 'Thigh': 'thigh', 'Calf': 'calf', 'Foot': 'foot', 'ToeBase': 'ball'}
        for side in ['L', 'R']:
            result.update({side+'_'+target: source+'_'+side.lower() for target, source in limbs.items()})
    else:
        result = {'mixamorig:'+a: b for a, b in [('Hips', 'pelvis'), ('Spine', 'spine_01'),
            ('Spine1', 'spine_02'), ('Spine2', 'spine_03'), ('Neck', 'neck_01'), ('Head', 'head')]}
        limbs = {'Shoulder': 'clavicle', 'Arm': 'upperarm', 'ForeArm': 'lowerarm',
                 'Hand': 'hand', 'UpLeg': 'thigh', 'Leg': 'calf', 'Foot': 'foot', 'ToeBase': 'ball'}
        for side, suffix in [('Left', 'l'), ('Right', 'r')]:
            result.update({'mixamorig:'+side+a: b+'_'+suffix for a, b in limbs.items()})
            result.update({'mixamorig:'+side+'HandIndex'+str(i): 'index_0'+str(i)+'_'+suffix
                           for i in range(1, 4)})
    return result


def run(character):
    folder, filename, body = CONFIG[character]
    package = PACKAGES/folder
    accepted = package/filename
    output = package/'Private/TownLocomotion'
    output.mkdir(parents=True, exist_ok=True)
    target = MotionData(accepted, body)
    source = MotionData(SOURCE, lambda name: name.startswith('Zara_Part') and name != 'Zara_Part_03')
    names = {name:name for name in target.names if name in source.names} if character=='Zara' else mapping(character)
    pairs = {target.names[t]: source.names[s] for t, s in names.items()}
    inverse_pairs = {s: t for t, s in pairs.items()}
    hip = target.names['pelvis' if character=='Zara' else 'Hip' if character == 'Orin' else 'mixamorig:Hips']
    source_hip = source.names['pelvis']
    joints = set(n for skin in target.doc['skins'] for n in skin['joints'])
    # Orient the target's rest frame to Zara using anatomical shoulder/up axes.
    def body_frame(data, left, right):
        across = data.bind[data.names[left]].translation-data.bind[data.names[right]].translation
        across.y = 0; across.normalize()
        up = Vector((0, 1, 0)); forward = across.cross(up).normalized()
        return Matrix((across, up, forward)).transposed().to_quaternion()
    left = 'clavicle_l' if character=='Zara' else 'L_Clavicle' if character == 'Orin' else 'mixamorig:LeftShoulder'
    right = 'clavicle_r' if character=='Zara' else 'R_Clavicle' if character == 'Orin' else 'mixamorig:RightShoulder'
    alignment = body_frame(source, 'clavicle_l', 'clavicle_r') @ body_frame(target, left, right).inverted()
    corrections = {}
    # Align anatomical bone directions as well as rest axes: A-pose and T-pose
    # armatures cannot be retargeted by blindly copying local rotations.
    for t, s in pairs.items():
        descendants = [(other_t, other_s) for other_t, other_s in pairs.items()
                       if target.parents.get(other_t) == t and source.parents.get(other_s) == s]
        correction = alignment.copy()
        if descendants:
            child_t, child_s = descendants[0]
            td = target.bind[child_t].translation-target.bind[t].translation
            sd = source.bind[child_s].translation-source.bind[s].translation
            if min(td.length, sd.length) > .0001:
                correction = (alignment @ td).rotation_difference(sd) @ alignment
        corrections[t] = source.bind[s].to_quaternion().inverted() @ correction @ target.bind[t].to_quaternion()
    scale = target.bind[hip].translation.y/source.bind[source_hip].translation.y
    idle = target.sample('Idle', 0)
    idle_min = target.minimum(target.world(idle))
    motions, reports = {}, {}
    for name in ['Walk', 'Run']:
        source_name = name+'Unarmed'
        duration = source.duration(source_name)
        times = np.linspace(0, duration, round(duration*30)+1).tolist()
        source_world = [source.world(source.sample(source_name, time)) for time in times]
        source_minima = [source.minimum(world) for world in source_world]
        floor = min(source_minima)
        frames, minima, headings = [], [], []
        for index, world in enumerate(source_world):
            local = [m.copy() for m in target.rest]
            cache = {}
            def solve(t):
                if t in cache:
                    return cache[t]
                parent = solve(target.parents[t]) if t in target.parents else Matrix.Identity(4)
                translation, rotation, scaling = target.rest[t].decompose()
                if t in pairs:
                    desired = world[pairs[t]].to_quaternion() @ corrections[t]
                    rotation = parent.to_quaternion().inverted() @ desired
                local[t] = Matrix.LocRotScale(translation, rotation, scaling)
                cache[t] = parent @ local[t]
                return cache[t]
            for t in range(len(local)):
                solve(t)
            # Keep the accepted in-place origin. Copy Zara's vertical airtime,
            # measuring the actual skinned body, rather than flattening a run jump.
            # Town sprint keeps the stance contacts while reducing airborne bounce.
            airtime_scale = .35 if name=='Run' else 1.0
            desired_min = idle_min+max(0, source_minima[index]-floor)*scale*airtime_scale
            correction = desired_min-target.minimum([cache[i] for i in range(len(local))])
            parent = cache[target.parents[hip]] if hip in target.parents else Matrix.Identity(4)
            local[hip].translation += parent.inverted().to_3x3() @ Vector((0, correction, 0))
            final_world = target.world(local)
            minima.append(target.minimum(final_world))
            frames.append(local)
            across = final_world[target.names[left]].translation-final_world[target.names[right]].translation
            headings.append(math.degrees(math.atan2(across.z, across.x)))
        # Enforce a clean loop, including the tiny float differences at source endpoints.
        frames[-1] = [m.copy() for m in frames[0]]
        motions[name] = (times, frames, sorted(joints))
        reports[name] = {'sourceClip': source_name, 'frames': len(frames), 'durationSeconds': duration,
            'airtimeScale': airtime_scale,
            'bodyMinimumY': min(minima), 'bodyMaximumMinimumY': max(minima),
            'sourceGroundMinimumY': floor, 'shoulderHeadingRangeDegrees': [min(headings), max(headings)]}
    model = output/(character+'-Town.glb')
    target.write_town_variant(model, motions)
    reloaded = MotionData(model, body)
    for name, (times, frames, _) in motions.items():
        for index in [0, len(times)//2, len(times)-1]:
            measured = reloaded.minimum(reloaded.world(reloaded.sample(name, times[index])))
            expected = target.minimum(target.world(frames[index]))
            assert abs(measured-expected) < .00002, (character, name, measured, expected)
    report = {'character': character, 'source': 'ZaraV1 RunUnarmed / WalkUnarmed',
        'sourceSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        'acceptedModelSha256': hashlib.sha256(accepted.read_bytes()).hexdigest(),
        'townModelSha256': hashlib.sha256(model.read_bytes()).hexdigest(),
        'bindBodyMinimumY': target.minimum(target.bind), 'idleBodyMinimumY': idle_min,
        'originalModelSkinMaterialAndNonLocomotionClipsPreserved': True,
        'privateTownVariantOnly': True, 'clips': reports}
    (output/'retarget-report.json').write_text(json.dumps(report, indent=2)+'\n')
    print('TOWN RETARGET', json.dumps(report), flush=True)


if __name__ == '__main__':
    for character in sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else CONFIG:
        run(character)
