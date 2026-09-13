"""Author portable staff grip, back carry and cape motion on Mira's own rig."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Matrix, Vector

STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-tripo-animation-work.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['Mira.Rig']
body = bpy.data.objects['Mira.Body']


def action_at(name, frame=1):
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    return action


def write_pose(bone, matrix, frame, parent=None):
    kwargs = {}
    if bone.parent:
        kwargs = {'parent_matrix': parent if parent is not None else bone.parent.matrix,
                  'parent_matrix_local': bone.parent.bone.matrix_local}
    bone.matrix_basis = bone.bone.convert_local_to_pose(matrix, bone.bone.matrix_local,
                                                       invert=True, **kwargs)
    bone.rotation_mode = 'QUATERNION'
    for prop in ('location','rotation_quaternion','scale'):
        bone.keyframe_insert(data_path=prop,frame=frame)


# Leave the right hand available for the staff and cast with the left hand.
reflection = Matrix.Diagonal((-1,1,1,1))
for name in ('Attack','HealOne'):
    action = action_at(name)
    samples = []
    for frame in range(1,int(action.frame_range[1])+1):
        action_at(name,frame)
        desired = {}
        for bone in rig.pose.bones:
            other_name = bone.name.replace('Left','TEMP').replace('Right','Left').replace('TEMP','Right')
            other = rig.pose.bones[other_name]
            desired[bone.name] = reflection @ other.matrix @ other.bone.matrix_local.inverted() @ reflection @ bone.bone.matrix_local
        samples.append((frame,desired))
    for frame,desired in samples:
        action_at(name,frame)
        for bone in rig.pose.bones:
            write_pose(bone,desired[bone.name],frame,desired.get(bone.parent.name) if bone.parent else None)

def forearm_basis(direction):
    along = direction.normalized()
    thumb = Vector((0,0,1))
    thumb = (thumb - along * thumb.dot(along)).normalized()
    palm = thumb.cross(along).normalized()
    return Matrix((thumb,along,palm)).transposed().to_4x4()


forearm_rest = rig.data.bones['mixamorig:RightForeArm'].matrix_local
hand_rest = rig.data.bones['mixamorig:RightHand'].matrix_local
wrist_rest_rotation = (forearm_rest.inverted() @ hand_rest).to_quaternion().to_matrix().to_4x4()
target = bpy.data.objects.new('Temporary Mira Grip Target',None)
pole = bpy.data.objects.new('Temporary Mira Elbow Pole',None)
scene.collection.objects.link(target)
scene.collection.objects.link(pole)
arm_names = ('mixamorig:RightArm','mixamorig:RightForeArm','mixamorig:RightHand')
held_clips = ('Idle','Walk','Attack','Defend','HealOne','HealParty','Hit')
for name in held_clips:
    action = action_at(name)
    forearm = rig.pose.bones[arm_names[1]]
    constraint = forearm.constraints.new('IK')
    constraint.target = target
    constraint.pole_target = pole
    constraint.chain_count = 2
    constraint.use_stretch = False
    constraint.pole_angle = math.pi
    samples = []
    for frame in range(1,int(action.frame_range[1])+1):
        action_at(name,frame)
        hips = rig.pose.bones['mixamorig:Hips']
        body_frame = hips.matrix @ hips.bone.matrix_local.inverted()
        target.location = body_frame @ Vector((-.17,-.12,.69))
        target.location.z = max(.700, target.location.z)
        pole.location = body_frame @ Vector((-.31,.05,.68))
        bpy.context.view_layer.update()
        desired = {n:rig.pose.bones[n].matrix.copy() for n in arm_names}
        forearm_pose = forearm_basis(desired[arm_names[2]].translation - desired[arm_names[1]].translation)
        forearm_pose.translation = desired[arm_names[1]].translation
        desired[arm_names[1]] = forearm_pose
        wrist = forearm_pose @ wrist_rest_rotation
        wrist.translation = desired[arm_names[2]].translation
        desired[arm_names[2]] = wrist
        samples.append((frame,desired))
    forearm.constraints.remove(constraint)
    for frame,desired in samples:
        action_at(name,frame)
        for n in arm_names:
            bone = rig.pose.bones[n]
            write_pose(bone,desired[n],frame,desired.get(bone.parent.name))
        for index,angle in ((1,25),(2,65),(3,65)):
            bone = rig.pose.bones['mixamorig:RightHandIndex'+str(index)]
            bone.rotation_mode = 'QUATERNION'
            bone.rotation_quaternion = Matrix.Rotation(math.radians(angle),4,'X').to_quaternion()
            bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
    print('GRIP_FITTED '+name,flush=True)
for obj in (target,pole):
    bpy.data.objects.remove(obj,do_unlink=True)

bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
cape_bone = rig.data.edit_bones.new('MiraCape')
cape_bone.head = (0,.045,.565)
cape_bone.tail = (0,.10,.16)
cape_bone.parent = rig.data.edit_bones['mixamorig:Hips']
staff_bone = rig.data.edit_bones.new('MiraStaff')
staff_bind_offset = Vector((0,.13,.69))
staff_bone.head = staff_bind_offset
staff_bone.tail = staff_bind_offset + Vector((0,0,1))
staff_bone.parent = rig.data.edit_bones['mixamorig:Hips']
bpy.ops.object.mode_set(mode='OBJECT')
cape_group = body.vertex_groups.new(name='MiraCape')
affected = 0
for vertex in body.data.vertices:
    x,y,z = vertex.co
    amount = min(1,max(0,(y-.035)/.02)) * min(1,max(0,(.61-z)/.09)) * min(1,max(0,(z-.07)/.03))
    if amount <= 0:
        continue
    weights = [(g.group,g.weight*(1-amount)) for g in vertex.groups]
    for group,weight in weights:
        body.vertex_groups[group].remove([vertex.index])
        if weight > .001:
            body.vertex_groups[group].add([vertex.index],weight,'REPLACE')
    cape_group.add([vertex.index],amount,'REPLACE')
    affected += 1

with bpy.data.libraries.load(str(STAGE / 'mira-staff-baked-work.blend')) as (available,loaded):
    loaded.objects = ['Mira.Staff.Reduced']
staff = loaded.objects[0]
scene.collection.objects.link(staff)
staff.name = 'Mira.Staff'
staff.hide_set(False)
for vertex in staff.data.vertices:
    vertex.co *= 1.08
    vertex.co.z -= .635 * 1.08
    vertex.co += staff_bind_offset
group = staff.vertex_groups.new(name='MiraStaff')
group.add(list(range(len(staff.data.vertices))),1,'REPLACE')
modifier = staff.modifiers.new('Mira Staff Rig','ARMATURE')
modifier.object = rig
staff.parent = rig
for action in list(bpy.data.actions):
    name = action.name
    action_at(name)
    for frame in range(1,int(action.frame_range[1])+1):
        action_at(name,frame)
        phase = (frame-1)/max(1,int(action.frame_range[1])-1)
        cape = rig.pose.bones['MiraCape']
        cape.rotation_mode = 'XYZ'
        cape.rotation_euler = (math.radians(5+5*math.sin(phase*2*math.pi)),0,0)
        cape.keyframe_insert(data_path='rotation_euler',frame=frame)
        hips = rig.pose.bones['mixamorig:Hips']
        body_frame = hips.matrix @ hips.bone.matrix_local.inverted()
        staff_pose = Matrix.Rotation(body_frame.to_euler('XYZ').z,4,'Z') @ rig.data.bones['MiraStaff'].matrix_local
        if name in ('Run','Death'):
            spine = rig.pose.bones['mixamorig:Spine2']
            spine_frame = spine.matrix @ spine.bone.matrix_local.inverted()
            lean = Matrix.Rotation(math.radians(-13),4,'Y')
            staff_pose = spine_frame.to_quaternion().to_matrix().to_4x4() @ lean @ rig.data.bones['MiraStaff'].matrix_local
            staff_pose.translation = spine_frame @ Vector((-.045,.155,.75))
        else:
            hand = rig.pose.bones['mixamorig:RightHand'].matrix
            staff_pose = Matrix((hand.col[2].xyz,hand.col[0].xyz,hand.col[1].xyz)).transposed().to_4x4()
            staff_pose.translation = hand @ Vector((0,.031,.013))
        write_pose(rig.pose.bones['MiraStaff'],staff_pose,frame)
    print('STAFF_AND_CAPE_KEYED '+name,flush=True)
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.vertex_group_limit_total(limit=4)
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
action_at('Idle',1)
(STAGE / 'equipment-fit.json').write_text(json.dumps({'capeVertices':affected,'heldClips':held_clips,
    'backCarryClips':['Run','Death'],'rigBones':len(rig.data.bones),'staffTriangles':len(staff.data.polygons)},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-tripo-equipment-work.blend'),compress=True)
print('EQUIPMENT_FIT_READY',flush=True)
