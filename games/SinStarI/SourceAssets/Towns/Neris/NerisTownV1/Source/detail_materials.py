"""Apply the accepted vivid garden palette and softly polished stone to the derived town.

Uses Blender's bundled numpy and image writer; no downloaded textures or dependencies.
"""
from pathlib import Path
import math
import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
assert bpy.app.background and Path(bpy.data.filepath).name == 'Neris-Town-Detailed.blend'
folder = ROOT / 'Textures'
folder.mkdir(exist_ok=True)
size = 256
y, x = np.mgrid[0:size, 0:size] * math.tau / size
rng = np.random.default_rng(95125)
grain = np.zeros_like(x)
for _ in range(35):
    u, v = rng.integers(1, 50, 2)
    grain += np.sin(u*x+v*y+rng.uniform(0, math.tau)) / 35
vein = np.sin(2*x+3*y+.42*np.sin(3*x-y))**8
tone = np.clip(.94+.14*grain-.035*vein, .83, 1)
height = .0025*grain + .001*vein
dx = (np.roll(height,-1,1)-np.roll(height,1,1))*size/2
dy = (np.roll(height,-1,0)-np.roll(height,1,0))*size/2
normal = np.stack((-dx,-dy,np.ones_like(x)),axis=2)
normal /= np.linalg.norm(normal,axis=2,keepdims=True)

def image(name, rgb, noncolor=False):
    value = bpy.data.images.get(name) or bpy.data.images.new(name, size, size, alpha=True)
    if noncolor: value.colorspace_settings.name='Non-Color'
    pixels = np.concatenate((rgb,np.ones((size,size,1))),axis=2).astype(np.float32)
    value.pixels.foreach_set(pixels.ravel())
    value.filepath_raw=str(folder/(name+'.png'))
    value.file_format='PNG'
    value.save()
    value.pack()
    return value

color = image('Neris-Stone-Grain', np.stack((tone,tone,tone),axis=2))
bump = image('Neris-Stone-Normal', normal*.5+.5, True)
palette = {
    'Neris Mature Jade Leaves': ((.035,.23,.045),.58),
    'Neris Young Olive Leaves': ((.16,.43,.035),.53),
    'Neris Sunlit Leaf Tips': ((.42,.67,.065),.55),
    'Town Grass': ((.07,.31,.045),.93),
    'Flower Jade Foliage': ((.035,.26,.06),.64),
}
for name,(rgb,rough) in palette.items():
    m=bpy.data.materials[name]
    m.diffuse_color=(*rgb,1)
    n=m.node_tree.nodes.get('Principled BSDF')
    n.inputs['Base Color'].default_value=(*rgb,1)
    n.inputs['Roughness'].default_value=rough

for name in ['Town Paving','Town Pavinglight']:
    m=bpy.data.materials[name]
    nodes,links=m.node_tree.nodes,m.node_tree.links
    for n in list(nodes):
        if n.name.startswith('Neris Texture'): nodes.remove(n)
    shader=nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value=.43
    shader.inputs['Metallic'].default_value=0
    coord=nodes.new('ShaderNodeTexCoord'); coord.name='Neris Texture Coordinates'
    mapping=nodes.new('ShaderNodeVectorMath'); mapping.operation='SCALE'; mapping.name='Neris Texture Scale'
    mapping.inputs[3].default_value=1/1.8
    links.new(coord.outputs['Position'] if 'Position' in coord.outputs else coord.outputs['Object'],mapping.inputs[0])
    tex=nodes.new('ShaderNodeTexImage'); tex.name='Neris Texture Stone'; tex.image=color
    links.new(mapping.outputs[0],tex.inputs['Vector'])
    mix=nodes.new('ShaderNodeMixRGB'); mix.name='Neris Texture Tint'; mix.blend_type='MULTIPLY'
    mix.inputs[0].default_value=1
    mix.inputs[1].default_value=shader.inputs['Base Color'].default_value
    links.new(tex.outputs['Color'],mix.inputs[2]); links.new(mix.outputs[0],shader.inputs['Base Color'])
    tex=nodes.new('ShaderNodeTexImage'); tex.name='Neris Texture Grain Normal'; tex.image=bump
    links.new(mapping.outputs[0],tex.inputs['Vector'])
    normal_node=nodes.new('ShaderNodeNormalMap'); normal_node.name='Neris Texture Normal'
    normal_node.inputs['Strength'].default_value=.25
    links.new(tex.outputs['Color'],normal_node.inputs['Color'])
    links.new(normal_node.outputs[0],shader.inputs['Normal'])
    # The native exporter embeds these same maps and world-planar UVs.
    m['neris_stone_texture']=True

bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Detailed.blend'),compress=True)
print('MATERIALS vivid foliage and subtle stone sheen/texture saved',flush=True)
