import bpy
bpy.ops.wm.open_mainfile(filepath=r'D:\AI\Mira3D\Mira3\mira3-equipped-poses.blend')
"""Ground Mira3 from her measured body, retaining Run's intentional airborne frames."""
import bpy,json
from mathutils import Vector
from pathlib import Path
rig=bpy.data.objects['Mira3.Rig'];body=bpy.data.objects['Mira3.SkinnedBody'];scene=bpy.context.scene
if rig.get('Mira3Grounded'): raise RuntimeError('Grounding already applied; use the ungrounded checkpoint to regenerate')
def minimum():
    bpy.context.view_layer.update()
    return min((body.matrix_world @ v.co).z for v in body.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices)
rig.data.pose_position='REST';bind_min=minimum();common=-bind_min;rig.location.z+=common;bpy.context.view_layer.update();rig.data.pose_position='POSE'
hips=rig.pose.bones['mixamorig:Hips'];local_up=hips.bone.matrix_local.to_3x3().inverted() @ rig.matrix_world.to_3x3().inverted() @ Vector((0,0,1));reports=[]
for action in bpy.data.actions:
    rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0];start,end=map(int,action.frame_range);samples=[]
    for frame in range(start,end+1):
        scene.frame_set(frame);y=minimum();correction=-y if action.name in ('Idle','Walk') else max(0,-y)
        samples.append((frame,hips.location.copy(),correction,y))
    for frame,location,correction,y in samples:
        scene.frame_set(frame);hips.location=location+local_up*correction;hips.keyframe_insert(data_path='location',frame=frame)
    corrected=[]
    for frame,location,correction,y in samples:
        scene.frame_set(frame);corrected.append(minimum())
    reports.append({'clip':action.name,'firstFrameBefore':samples[0][3],'firstFrameAfter':corrected[0],'settledLastFrameAfter':corrected[-1],'minimumAfter':min(corrected),'maximumCorrection':max(s[2] for s in samples),'minimumCorrection':min(s[2] for s in samples),'frames':len(samples),'groundingByFrame':[{'frame':f-start,'offsetY':d} for f,l,d,y in samples]})
rig['Mira3Grounded']=True;rig.animation_data.action=bpy.data.actions['Idle'];rig.animation_data.action_slot=bpy.data.actions['Idle'].slots[0];scene.frame_set(1)
result={'bindMinimumBefore':bind_min,'commonActorOffsetY':common,'clips':reports,'policy':'Body only, excluding staff. Idle/Walk retain floor contact; other clips lift only negative body minima, preserving positive jump height.'}
Path(r'D:\AI\Mira3D\Mira3\mira3-grounding-repair.json').write_text(json.dumps(result,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=r'D:\AI\Mira3D\Mira3\mira3-grounded-rigged.blend')
result={**result,'clips':[{k:v for k,v in r.items() if k!='groundingByFrame'} for r in reports]}
