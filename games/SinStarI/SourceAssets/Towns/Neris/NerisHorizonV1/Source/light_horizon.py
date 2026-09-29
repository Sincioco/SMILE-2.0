"""Real Blender day/night lighting and fixed rear-terminal review images."""
import bpy, math
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Source/Neris-Horizon-Presentation-r004.blend'))
scene = bpy.context.scene
scene.frame_set(1500)
sun = next(o for o in scene.objects if o.type == 'LIGHT' and o.data.type == 'SUN')

def area(name, point, target, power, size, color):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.shape, data.size, data.color = power, 'DISK', size, color
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = point
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    obj['horizon_night_light'] = True

for x in (-195,-65,65,195):
    area('Terminal Interior Wash', (x,-300,48), (x,-300,0), 150000, 55, (1,.79,.49))
    area('Terminal Rear Facade', (x,-400,35), (x,-370,24), 75000, 30, (1,.86,.67))
for x in (-175,-82,50,148):
    area('Apron Floodlight', (x,193,33), (x,115,0), 150000, 9, (1,.9,.75))
for x,y in ((-230,0),(235,80)):
    area('Retained Glass Hall Wash', (x,y,25), (x,y,0), 65000, 20, (.6,.85,1))
for x,y in ((-82,15),(148,25)):
    area('Hangar Work Lights', (x,y,36), (x,y,0), 110000, 22, (1,.92,.8))
area('Tower Architectural Wash', (210,-170,45), (235,-194,115), 130000, 20, (.5,.78,1))

camera = scene.camera
camera.location = (580,-900,440)
aim = Vector((0,-110,65))
camera.rotation_euler = (aim-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale = 850
scene.render.engine = 'CYCLES'
scene.cycles.samples = 20
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 1280,720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type == 'VIEW_3D':
            a.spaces.active.region_3d.view_perspective = 'CAMERA'
            a.spaces.active.region_3d.view_camera_zoom = 0
            a.spaces.active.clip_end = 10000
            a.spaces.active.overlay.show_overlays = False
            a.spaces.active.shading.type = 'MATERIAL'
scene['night_light_count'] = sum(bool(o.get('horizon_night_light')) for o in scene.objects)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Source/Neris-Horizon-Day-r004.blend'), compress=True)
scene.render.filepath = str(root/'Previews/Horizon-r004-Rear-Day.png')
bpy.ops.render.render(write_still=True)
sun.data.energy = .12
background = scene.world.node_tree.nodes['Background']
background.inputs[0].default_value = (.12,.2,.4,1)
background.inputs[1].default_value = .09
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Source/Neris-Horizon-Night-r004.blend'), compress=True)
scene.render.filepath = str(root/'Previews/Horizon-r004-Rear-Night.png')
bpy.ops.render.render(write_still=True)
print('PASS HORIZON DAY/NIGHT:', scene['night_light_count'], 'authored lights', flush=True)
