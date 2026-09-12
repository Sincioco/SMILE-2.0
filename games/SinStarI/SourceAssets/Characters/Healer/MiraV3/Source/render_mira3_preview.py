import bpy
from pathlib import Path
from mathutils import Vector
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-unrigged-baked.blend'))
scene=bpy.context.scene;body=bpy.data.objects['Mira3.Body']
for obj in scene.objects:obj.hide_render=obj!=body
camera=scene.camera;camera.hide_render=False;camera.data.type='PERSP';camera.data.lens=60
camera.location=(2,-4,1.7);camera.rotation_euler=(Vector((0,0,.88))-camera.location).to_track_quat('-Z','Y').to_euler()
for name,position,power,size in [('Key',(-3,-4,5),600,4),('Fill',(3,-2,2),350,3),('Rim',(0,3,4),800,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=position;obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.11,.14,.20,1)
scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.4
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.render.filepath=str(root/'mira3-textured-preview.png');bpy.ops.render.render(write_still=True)
camera.location=(0,-1.4,1.53);camera.data.lens=90;camera.rotation_euler=(Vector((0,0,1.50))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(root/'mira3-face-preview.png');bpy.ops.render.render(write_still=True)
print('PREVIEWS_READY',flush=True)
