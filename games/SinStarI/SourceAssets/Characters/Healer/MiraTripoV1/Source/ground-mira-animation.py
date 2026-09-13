"""Ground Mira's own clips, keep locomotion in place, and fold the cape above the floor."""
import bpy
import json
import math
import numpy as np
from pathlib import Path
from mathutils import Matrix, Vector

STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-tripo-equipment-work.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['Mira.Rig']
body = bpy.data.objects['Mira.Body']
staff = bpy.data.objects['Mira.Staff']
hips = rig.pose.bones['mixamorig:Hips']
cape = rig.pose.bones['MiraCape']
cape_index = body.vertex_groups['MiraCape'].index
cape_vertices = [v for v in body.data.vertices if any(g.group==cape_index and g.weight>.0001 for g in v.groups)]
cape_ids = {v.index for v in cape_vertices}
body_ids = [v.index for v in body.data.vertices if v.index not in cape_ids]


def at(name, frame):
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    scene.frame_set(frame)
    bpy.context.view_layer.update()


def write(bone, matrix, frame):
    kwargs = {}
    if bone.parent:
        kwargs = {'parent_matrix':bone.parent.matrix,'parent_matrix_local':bone.parent.bone.matrix_local}
    bone.matrix_basis = bone.bone.convert_local_to_pose(matrix,bone.bone.matrix_local,invert=True,**kwargs)
    bone.rotation_mode = 'QUATERNION'
    for prop in ('location','rotation_quaternion','scale'):
        bone.keyframe_insert(data_path=prop,frame=frame)


def minimum(obj, indices=None):
    bpy.context.view_layer.update()
    vertices = obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices
    if indices is None:
        indices = range(len(vertices))
    return min((obj.matrix_world @ vertices[i].co).z for i in indices)


rig.data.pose_position = 'REST'
bind_minimum = minimum(body)
common = -bind_minimum
rig.location.z += common
rig.data.pose_position = 'POSE'
ground_report = []
for action in bpy.data.actions:
    name = action.name
    at(name,1)
    origin = hips.matrix.translation.copy()
    samples = []
    for frame in range(1,int(action.frame_range[1])+1):
        at(name,frame)
        matrix = hips.matrix.copy()
        if name in ('Walk','Run'):
            matrix.translation.x = origin.x
            matrix.translation.y = origin.y
        low = minimum(body,body_ids)
        lift = -low if name in ('Idle','Walk') else max(0,-low)
        matrix.translation.z += lift
        samples.append((frame,matrix,low,lift))
    for frame,matrix,low,lift in samples:
        at(name,frame)
        write(hips,matrix,frame)
    ground_report.append({'clip':name,'frame0Before':samples[0][2],
                         'minimumBefore':min(s[2] for s in samples),'maximumLift':max(s[3] for s in samples)})
print('BODY_FLOOR_CONTACT_READY',flush=True)

# Solve the cape's vertical clearance analytically for one bounded hinge angle.
# All other skin weights and the top seam remain attached to their original bones.
coords = np.array([list(v.co) for v in cape_vertices],dtype=np.float64)
points = np.column_stack((coords,np.ones(len(coords))))
weights = {}
for index,vertex in enumerate(cape_vertices):
    for group in vertex.groups:
        name = body.vertex_groups[group.group].name
        weights.setdefault(name,np.zeros(len(coords)))[index] = group.weight
w = weights['MiraCape']
pivot = cape.bone.head_local.copy()
relative = coords - np.array(pivot)
angles = np.arange(-175,176,dtype=np.float64)
radians = np.radians(angles)
cosines,sines = np.cos(radians),np.sin(radians)
cape_report = []
seam_clearance = []
for action in bpy.data.actions:
    name = action.name
    feasible = []
    preferences = []
    for frame in range(1,int(action.frame_range[1])+1):
        at(name,frame)
        parent = rig.matrix_world @ hips.matrix @ hips.bone.matrix_local.inverted()
        fixed = np.zeros(len(coords))
        for bone_name,bone_weights in weights.items():
            if bone_name == 'MiraCape':
                continue
            bone = rig.pose.bones[bone_name]
            transform = rig.matrix_world @ bone.matrix @ bone.bone.matrix_local.inverted()
            fixed += (points @ np.array(transform)[2]) * bone_weights
        row = np.array(parent)[2]
        a = w * (row[1]*relative[:,1] + row[2]*relative[:,2])
        b = w * (-row[1]*relative[:,2] + row[2]*relative[:,1])
        c = w * (row[0]*relative[:,0] + (parent @ pivot).z) + fixed
        minima = (cosines[:,None]*a + sines[:,None]*b + c).min(axis=1)
        if minima.max() < .001:
            lift = .001 - float(minima.max())
            assert lift < .006, (name,frame,lift)
            matrix = hips.matrix.copy()
            matrix.translation.z += lift
            write(hips,matrix,frame)
            bpy.context.view_layer.update()
            minima += lift
            seam_clearance.append({'clip':name,'frame':frame,'lift':lift})
        preferred = 5 + 5*math.sin((frame-1)/max(1,int(action.frame_range[1])-1)*2*math.pi)
        feasible.append(minima >= .000999)
        preferences.append(preferred)
    # Solve the entire clip so the hem anticipates floor contact instead of
    # snapping between disconnected valid angles at the moment of impact.
    delta = angles[:,None] - angles[None,:]
    transition = 2 * delta**2
    transition[np.abs(delta) > 5] = np.inf
    scores = (angles-preferences[0])**2 * .01
    scores[~feasible[0]] = np.inf
    history = []
    for frame in range(1,len(feasible)):
        candidates = scores[:,None] + transition
        previous = np.argmin(candidates,axis=0)
        scores = candidates[previous,np.arange(len(angles))] + .01*(angles-preferences[frame])**2
        scores[~feasible[frame]] = np.inf
        assert np.isfinite(scores).any(), (name,frame+1,'No smooth cape path')
        history.append(previous)
    index = int(np.argmin(scores))
    chosen = [float(angles[index])]
    for previous in reversed(history):
        index = int(previous[index])
        chosen.append(float(angles[index]))
    chosen.reverse()
    for frame,degrees in enumerate(chosen,1):
        at(name,frame)
        rotation = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(degrees),4,'X') @ Matrix.Translation(-pivot)
        desired = hips.matrix @ hips.bone.matrix_local.inverted() @ rotation @ cape.bone.matrix_local
        write(cape,desired,frame)
    cape_report.append({'clip':name,'minDegrees':min(chosen),'maxDegrees':max(chosen),
                        'maxFrameStep':max([abs(a-b) for a,b in zip(chosen,chosen[1:])] or [0])})
    print('CAPE_GROUNDED '+json.dumps(cape_report[-1]),flush=True)

staff_report = []
# The staff slips from its back mount during Death and settles beside her.
# A floor correction on a still-upright back attachment would visibly float it.
at('Death',10)
staff_bone = rig.pose.bones['MiraStaff']
release = staff_bone.matrix.copy()
settled = Matrix.Rotation(math.radians(90),4,'X') @ staff_bone.bone.matrix_local
settled.translation = Vector((.37,.45,.09))
write(staff_bone,settled,110)
settled.translation.z += .003 - minimum(staff)
for frame in range(10,111):
    at('Death',frame)
    t = min(1,max(0,(frame-10)/45))
    smooth = t*t*(3-2*t)
    rotation = release.to_quaternion().slerp(settled.to_quaternion(),smooth)
    position = release.translation.lerp(settled.translation,smooth)
    position.z += .12*math.sin(math.pi*t)
    write(staff_bone,Matrix.LocRotScale(position,rotation,Vector((1,1,1))),frame)
for action in bpy.data.actions:
    name = action.name
    maximum_lift = 0
    for frame in range(1,int(action.frame_range[1])+1):
        at(name,frame)
        bone = rig.pose.bones['MiraStaff']
        matrix = bone.matrix.copy()
        low = minimum(staff)
        lift = max(0,.002-low)
        if lift:
            matrix.translation.z += lift
            write(bone,matrix,frame)
        maximum_lift = max(maximum_lift,lift)
    staff_report.append({'clip':name,'maximumLift':maximum_lift})
at('Idle',1)
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-tripo-grounded-work.blend'),compress=True)
(STAGE / 'grounding-repair.json').write_text(json.dumps({'bindBodyMinimumBefore':bind_minimum,
    'commonOffset':common,'body':ground_report,'cape':cape_report,'seamClearance':seam_clearance,'staff':staff_report,
    'policy':'Body contact excludes cape/equipment; preserve Run airtime; cape hinge clears floor; Walk and Run are in place.'},indent=2))
print('MIRA_GROUNDING_READY',flush=True)
