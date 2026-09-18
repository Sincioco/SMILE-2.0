"""Bake a restrained Mixamo Idle and three original, unarmed earth casts."""
import json
import math
import sys
from pathlib import Path
import bpy
from mathutils import Matrix, Vector

SOURCE = Path(__file__).resolve().parent
PACKAGE = SOURCE.parent
sys.path.insert(0, str(SOURCE))
from assemble_kael_animation import retarget_clip
from fit_and_ground_kael import action_at, minimum_z, write_pose, held_pose, floor_safe_hand


def cast_pose(name, t, idle_hand):
    # Reference: Jared Koh, Avatar Earthbending Animation (lZRYNRqdWD4).
    # Readable anticipation, raised knee/stomp, low rooted stance and torso-led strikes.
    # time, crouch, twist, lean, left-hand XYZ, stance width, step, knee lift, sword guard
    neutral=(0,0,0,0,*idle_hand,0,0,0,0)
    charge=[neutral,
        (.07,.04,.18,-.10,.26,.06,.48,.4,.3,.19,.5),
        (.14,.22,.28,.14,.29,-.16,.27,1,1,0,.8),
        (.31,.13,.24,-.05,.27,-.12,.88,1,1,0,1)]
    chamber=(.42,.17,.48,-.10,.29,.07,.65,1,1,0,1)
    if name=='EarthSlam':
        keys=charge+[(.39,.12,.30,-.13,.24,-.02,.98,1,1,0,1),
            (.47,.25,-.42,.36,.13,-.40,.25,1,1.2,0,.9),
            (.64,.24,-.35,.29,.17,-.36,.30,1,1.2,0,.8)]
    elif name=='EarthVolley':
        keys=charge+[(.39,*chamber[1:]),
            (.45,.21,-.44,.27,.10,-.59,.67,1,1.2,0,.7),
            (.50,.16,.37,-.05,.29,.04,.65,1,1,0,1),
            (.55,.22,-.46,.28,.14,-.60,.73,1,1.2,0,.7),
            (.60,.17,.38,-.06,.29,.04,.61,1,1,0,1),
            (.65,.24,-.52,.32,.09,-.60,.62,1,1.25,0,.7),
            (.73,.22,-.43,.24,.14,-.50,.60,1,1.2,0,.8)]
    else:
        keys=charge+[chamber,
            (.55,.23,-.52,.30,.11,-.61,.68,1,1.25,0,.7),
            (.68,.22,-.44,.25,.15,-.52,.63,1,1.2,0,.8)]
    keys += [(.86,.13,.08,.03,.26,-.13,.59,.7,.7,0,.8),(1,*neutral[1:])]
    for a,b in zip(keys,keys[1:]):
        if t<=b[0]:
            weight=max(0,min(1,(t-a[0])/(b[0]-a[0])))
            weight=weight*weight*(3-2*weight)
            return [x+(y-x)*weight for x,y in zip(a[1:],b[1:])]
    return list(neutral[1:])


def bake_casts(pose_sampler=cast_pose, casts=(('EarthHurl',97),('EarthVolley',121),('EarthSlam',91)),
               family='earth', input_checkpoint='kael-v1-grounded.blend', replace_idle=True,
               right_sampler=None):
    """Shared offline IK bake; every family supplies poses and owns a separate checkpoint."""
    bpy.ops.wm.open_mainfile(filepath=str(PACKAGE / 'Blender' / input_checkpoint))
    rig = bpy.data.objects['Kael.Rig']
    body = bpy.data.objects['Kael.Body']
    sword = bpy.data.objects['Kael.Sword']
    palm = Vector(json.loads((SOURCE/'grounding-report.json').read_text())['sword']['palmLocalMeters'])
    rest_inverse = rig.data.bones['KaelSword'].matrix_local.inverted()
    points = [rest_inverse @ v.co for v in sword.data.vertices]
    hips = rig.pose.bones['mixamorig:Hips']
    action_at(rig, 'Idle', 1)
    origin = hips.matrix.translation.copy()
    idle_report = None
    if replace_idle:
        bpy.data.actions.remove(bpy.data.actions['Idle'])
        idle_report = retarget_clip(rig, 'Idle', SOURCE/'Mixamo/BreathingIdle.fbx',
            'Breathing Idle: Sway 0, Breathing 25, Overdrive 50; downloaded for Kael')
        hips = rig.pose.bones['mixamorig:Hips']
        action_at(rig, 'Idle', 1)
        origin = hips.matrix.translation.copy()
        # The new relaxed hand is reoriented to keep the long blade above the floor.
        for f in range(1, int(bpy.data.actions['Idle'].frame_range[1])+1):
            action_at(rig, 'Idle', f)
            pose = hips.matrix.copy()
            pose.translation.x, pose.translation.y = origin.x, origin.y
            pose.translation.z += .001-minimum_z(body)
            write_pose(hips, pose, f)
            bpy.context.view_layer.update()
            hand = rig.pose.bones['mixamorig:RightHand']
            safe, _ = floor_safe_hand(rig, hand.matrix.copy(), palm, points)
            write_pose(hand, safe, f)
            bpy.context.view_layer.update()
            write_pose(rig.pose.bones['KaelSword'], held_pose(hand.matrix, palm), f)
    action_at(rig, 'Idle', 1)
    base = {b.name:b.matrix_basis.copy() for b in rig.pose.bones}
    feet = {side:rig.pose.bones['mixamorig:'+side+'Foot'].matrix.copy() for side in ('Left','Right')}
    hands = {side:rig.pose.bones['mixamorig:'+side+'Hand'].matrix.translation.copy() for side in ('Left','Right')}
    names = ['mixamorig:'+n for n in ('LeftArm','LeftForeArm','LeftHand','RightArm','RightForeArm','RightHand',
                                    'LeftUpLeg','LeftLeg','LeftFoot','RightUpLeg','RightLeg','RightFoot')]
    targets, constraints = {}, []
    for side in ('Left','Right'):
        for limb, end in (('ForeArm','Hand'),('Leg','Foot')):
            target = bpy.data.objects.new('EarthBake.'+side+end, None)
            bpy.context.scene.collection.objects.link(target)
            constraint = rig.pose.bones['mixamorig:'+side+limb].constraints.new('IK')
            constraint.target = target
            constraint.chain_count = 2
            constraint.use_stretch = False
            targets[side+end] = target
            constraints.append((rig.pose.bones['mixamorig:'+side+limb], constraint))
    report=[]
    for name, length in casts:
        old=bpy.data.actions.get(name)
        if old: bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name); action.use_fake_user=True
        rig.animation_data.action=action
        samples=[]
        for f in range(1,length+1):
            t=(f-1)/(length-1)
            crouch,twist,lean,lx,ly,lz,width,step,knee,guard=pose_sampler(name,t,hands['Left'])
            bpy.context.scene.frame_set(f)
            for b in rig.pose.bones: b.matrix_basis=base[b.name]
            hip=rig.pose.bones['mixamorig:Hips']
            bpy.context.view_layer.update()
            hip_pose=hip.matrix.copy()
            hip_position=hip_pose.translation.copy()
            hip_pose=Matrix.Rotation(twist*.3,4,'Z') @ hip_pose
            hip_pose.translation=hip_position
            hip_pose.translation.z-=crouch
            hip.matrix=hip_pose
            bpy.context.view_layer.update()
            spine=rig.pose.bones['mixamorig:Spine1']
            spine_pose=spine.matrix.copy()
            rotated=Matrix.Rotation(lean,4,'X') @ Matrix.Rotation(twist*.7,4,'Z') @ spine_pose
            rotated.translation=spine_pose.translation
            spine.matrix=rotated
            bpy.context.view_layer.update()
            for side, sign in (('Left',1),('Right',-1)):
                foot=feet[side].translation.copy()
                foot.x+=sign*.10*width
                foot.y+=(-.20 if side=='Left' else .14)*step
                if side=='Left': foot.z+=knee
                targets[side+'Foot'].location=rig.matrix_world @ foot
            left=Vector((lx,ly,lz))
            envelope=max(0,min(1,t/.07,(1-t)/.14))
            right=Vector((-lx,ly+.045,lz-.035))
            if name=='EarthVolley' and t>.36:
                exchange=math.exp(-((t-.55)/.028)**4)
                right=Vector((-.27,.06,.64)).lerp(right,exchange)
                left=left.lerp(Vector((.27,.06,.64)),exchange)
            if right_sampler:
                right=right_sampler(name,t,hands['Right'])
            else:
                right=hands['Right'].lerp(right,envelope)
            for side, p in (('Left',left),('Right',right)):
                targets[side+'Hand'].location=rig.matrix_world @ p
            bpy.context.view_layer.update()
            matrices={'mixamorig:Hips':hips.matrix.copy(),'mixamorig:Spine1':spine.matrix.copy()}
            matrices.update({n:rig.pose.bones[n].matrix.copy() for n in names})
            bases={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
            samples.append((f,bases,matrices))
        for bone,constraint in constraints: constraint.mute=True
        for f,bases,matrices in samples:
            bpy.context.scene.frame_set(f)
            for b in rig.pose.bones:
                b.matrix_basis=bases[b.name]
                for prop in ('location','rotation_quaternion','scale'): b.keyframe_insert(data_path=prop,frame=f)
            bpy.context.view_layer.update()
            for n,m in matrices.items():
                write_pose(rig.pose.bones[n],m,f)
                bpy.context.view_layer.update()
            pose=hips.matrix.copy(); pose.translation.x,pose.translation.y=origin.x,origin.y
            write_pose(hips,pose,f); bpy.context.view_layer.update()
            pose=hips.matrix.copy(); pose.translation.z+=.001-minimum_z(body)
            write_pose(hips,pose,f); bpy.context.view_layer.update()
            hand=rig.pose.bones['mixamorig:RightHand']
            safe,_=floor_safe_hand(rig,hand.matrix.copy(),palm,points)
            # The hidden sword stays floor-safe without constraining the casting hand.
            write_pose(rig.pose.bones['KaelSword'],held_pose(safe,palm),f)
        trajectories={bone:[] for bone in ('Head','HandLeft','HandRight','FootLeft')}
        for f in range(1,length+1):
            bpy.context.scene.frame_set(f); bpy.context.view_layer.update()
            for label,node in (('Head','Head'),('HandLeft','LeftHand'),('HandRight','RightHand'),('FootLeft','LeftFoot')):
                trajectories[label].append(list(rig.pose.bones['mixamorig:'+node].matrix.translation))
        spans={label:[max(p[i] for p in positions)-min(p[i] for p in positions)
                      for i in range(3)] for label,positions in trajectories.items()}
        if min(max(spans['HandLeft']),max(spans['HandRight']))<.35 or spans['Head'][2]<.12:
            raise RuntimeError((family+' cast body motion missing',name,spans))
        report.append({'clip':name,'frames':length,'sampleRate':30,'sword':'hidden during '+family+' casts by the caller',
                       'poseTravelMeters':spans})
        for bone,constraint in constraints: constraint.mute=False
        print(family.upper()+'_BAKED',name,flush=True)
    for bone,constraint in constraints: bone.constraints.remove(constraint)
    for target in targets.values(): bpy.data.objects.remove(target,do_unlink=True)
    action_at(rig,'Idle',1)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(PACKAGE/('Blender/kael-v1-'+family+'-grounded.blend')),compress=True)
    (SOURCE/(family+'-animation-report.json')).write_text(json.dumps({'idle':idle_report,'casts':report},indent=2),encoding='utf-8',newline='\n')

if __name__=='__main__': bake_casts()
