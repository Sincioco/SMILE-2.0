import bpy, json, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.render.fps=30
bpy.ops.import_scene.gltf(filepath=str(root/'Private/red-dragon-vrax-trial.glb'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.render.resolution_x=900;scene.render.resolution_y=680;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('TrialWorld');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.14,.18,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
bpy.ops.object.camera_add(location=(1.3,-2.65,1.05))
camera=bpy.context.object;camera.rotation_euler=(Vector((0,-.15,.37))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=43;scene.camera=camera
for pos,power,size in [((1,-1,2.5),170,3),((-2,-1,1),100,2),((0,2,2),220,2)]:
 bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(Vector((0,-.2,.3))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object;floor.location.z=-.005
material=bpy.data.materials.new('TrialFloor');material.diffuse_color=(.045,.05,.06,1);floor.data.materials.append(material)
report=[]
full_report=[]
for action in [a for a in bpy.data.actions if a.name.startswith('Vrax_')]:
 rig.animation_data_create();rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0]
 for track in rig.animation_data.nla_tracks:track.mute=True
 minima=[]
 for frame in range(1,int(action.frame_range[1])+1):
  scene.frame_set(frame);bpy.context.view_layer.update()
  ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();points=[ev.matrix_world@v.co for v in mesh.vertices]
  assert all(math.isfinite(c) for p in points for c in p),(action.name,frame,'nonfinite')
  minima.append(min(p.z for p in points));ev.to_mesh_clear()
 assert min(minima)>-.005,(action.name,min(minima))
 full_report.append({'clip':action.name,'sampledFrames':len(minima),'frameZeroBodyMinimumZ':minima[0],'minimumZ':min(minima)})
assert len(full_report)==10
for name in ['Vrax_Idle','Vrax_Attack','Vrax_Run']:
 action=next(a for a in bpy.data.actions if a.name==name or a.name.startswith(name+'_'))
 rig.animation_data_create();rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0]
 for track in rig.animation_data.nla_tracks:track.mute=True
 scene.frame_set(int(action.frame_range[1]*.45));bpy.context.view_layer.update()
 ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();points=[ev.matrix_world@v.co for v in mesh.vertices]
 report.append({'clip':name,'frame':scene.frame_current,'minZ':min(p.z for p in points),'bounds':[[min(p[i] for p in points),max(p[i] for p in points)]for i in range(3)]});ev.to_mesh_clear()
 scene.render.filepath=str(root/'Private'/('preview-'+name+'.png'));bpy.ops.render.render(write_still=True)
(root/'roundtrip-preview-validation.json').write_text(json.dumps({'floorTolerance':.005,'frameRate':30,'allClips':full_report,'previews':report},indent=2)+'\n')
print('ROUNDTRIP REVIEW COMPLETE')
