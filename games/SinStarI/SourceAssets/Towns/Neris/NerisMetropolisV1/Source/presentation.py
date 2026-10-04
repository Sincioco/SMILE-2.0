"""Blender views and deterministic, shareable stills for the city package."""
from pathlib import Path
import math
import bpy
from mathutils import Vector, Quaternion
from geometry import Mesh, material
from landmarks import DESIGNS

ROOT = Path(__file__).resolve().parents[1]


def scene(name):
    previous = bpy.data.scenes.get(name)
    if previous:
        # Only this generator's named scene is replaced on an iteration.
        for obj in list(previous.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.scenes.remove(previous)
    value = bpy.data.scenes.new(name)
    bpy.context.window.scene = value
    value.unit_settings.system = 'METRIC'
    value.render.engine = 'BLENDER_EEVEE'
    value.render.resolution_x = 1600
    value.render.resolution_y = 1000
    value.render.resolution_percentage = 100
    value.render.image_settings.file_format = 'PNG'
    value.world = bpy.data.worlds.new(name + ' Sky')
    value.world.use_nodes = True
    value.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.19,.27,.38,1)
    value.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .65
    value.view_settings.view_transform = 'AgX'
    value.render.film_transparent = False
    lamp = bpy.data.lights.new(name+' Sun','SUN')
    lamp.energy = 3
    lamp.angle = math.radians(12)
    sun = bpy.data.objects.new(lamp.name,lamp)
    value.collection.objects.link(sun)
    sun.rotation_euler = (.38,-.55,-.55)
    return value


def camera(value, location, target, span):
    data=bpy.data.cameras.new(value.name+' Camera')
    data.type='ORTHO'
    data.ortho_scale=span
    data.clip_end=100000
    obj=bpy.data.objects.new(data.name,data)
    value.collection.objects.link(obj)
    obj.location=location
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    value.camera=obj
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                space=area.spaces.active
                space.clip_end=100000
                space.shading.type='MATERIAL'
                space.shading.color_type='MATERIAL'
                space.overlay.show_overlays=False
                space.region_3d.view_distance=span
                space.region_3d.view_location=target
                space.region_3d.view_rotation=obj.rotation_euler.to_quaternion()
                space.region_3d.view_perspective='ORTHO'
    return obj


def label(value, text, position, size, color='Silver'):
    data=bpy.data.curves.new(text,'FONT')
    data.body=text
    data.size=size
    data.align_x='CENTER'
    data.materials.append(material(color))
    obj=bpy.data.objects.new(text,data)
    value.collection.objects.link(obj)
    obj.location=position
    obj.rotation_euler=(math.pi/2,0,0)


def landmark_study():
    value=scene('Neris Metropolis | Landmark Study')
    positions=[-880,-500,-180,240,640,960]
    for (name,height,factory),x in zip(DESIGNS,positions):
        obj=factory().object(value.collection)
        obj.location=(x,0,0)
        label(value,name.replace(' World Financial Center',' WFC'),(x,-140,-50),21)
        label(value,str(height)+' m',(x,-140,-82),16,'Gold')
    base=Mesh('Architectural Study Ground')
    base.box((0,0,-12),(2500,900,20),'Paving')
    base.object(value.collection)
    camera(value,(1450,-3700,1650),(0,0,420),2650)
    label(value,'NERIS METROPOLIS / FIRST LANDMARK STUDIES',(0,0,1110),42)
    label(value,'SILHOUETTE & SCALE STUDY  /  WORK IN PROGRESS',(0,0,1050),22)
    (ROOT/'Previews').mkdir(exist_ok=True)
    (ROOT/'Blender').mkdir(exist_ok=True)
    value.render.filepath=str(ROOT/'Previews/landmark-study-01.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blender/Neris-Metropolis.blend'),compress=True)
    return {'scene':value.name,'designs':len(DESIGNS),'preview':value.render.filepath}
