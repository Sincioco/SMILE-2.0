"""Render the original cast using the installed Blender; no external assets."""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
names = ['Tessa','Maren','Ilan','Bram','Sera','Pip','Nia','Tobin','Mochi']
for i,name in enumerate(names):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(ROOT/'Models'/f'{name}.glb'))
    new = set(bpy.data.objects)-before
    x = (i-2)*1.25 if i<5 else (i-6.5)*1.30
    y = 1.1 if i<5 else -1.05
    for obj in new:
        if obj.parent not in new:
            obj.location += Vector((x,y,0))
    bpy.ops.object.text_add(location=(x,y-.28,-.003))
    label = bpy.context.object
    label.data.body = name
    label.data.align_x = 'CENTER'
    label.data.size = .18
    label.data.extrude = .001
    label.rotation_euler = (0,0,0)
bpy.ops.mesh.primitive_plane_add(size=200)
floor = bpy.context.object
floor.location.z = -.015
mat = bpy.data.materials.new('Navy display floor')
mat.diffuse_color = (.055,.08,.11,1)
mat.use_nodes = True
mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.055,.08,.11,1)
floor.data.materials.append(mat)
for location,power,size in [((1,-5,7),1500,7),((-5,2,5),1100,5),((4,4,6),1400,5)]:
    bpy.ops.object.light_add(type='AREA',location=location)
    light=bpy.context.object
    light.data.energy=power
    light.data.shape='DISK'
    light.data.size=size
    light.rotation_euler=(Vector((0,0,.8))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-10,6.8))
camera=bpy.context.object
camera.rotation_euler=(Vector((0,.1,.7))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'
camera.data.ortho_scale=8.0
scene=bpy.context.scene
scene.camera=camera
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.render.resolution_x=1440
scene.render.resolution_y=1000
scene.render.resolution_percentage=100
scene.world.color=(.25,.25,.25)
scene.view_settings.view_transform='AgX'
scene.render.filepath=str(ROOT/'cast-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Residents.blend'))
bpy.ops.file.make_paths_relative()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Residents.blend'))
bpy.ops.render.render(write_still=True)
