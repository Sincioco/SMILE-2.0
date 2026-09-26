"""Tiled lawn albedo/normal and a bounded batch of short grass tufts.

Uses Blender's bundled numpy. Scatter on grass with conservative object clearance, so
streets, water, roofs and raised courtyards remain clear. No runtime allocation.
"""
from pathlib import Path
import math
import random
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]


def textures():
    size = 1024
    rng = np.random.default_rng(92626)
    y, x = np.mgrid[0:size, 0:size] * math.tau / size
    tone = .85 + .08*np.sin(3*x+2*y) + .06*np.sin(5*y-x) + .04*np.sin(9*x+7*y)
    tone += rng.normal(0, .008, (size, size))
    rgb = tone[..., None] * np.array([.115, .29, .032])
    height = np.zeros((size, size))
    # Thousands of overlapping tapered strokes give the ground a dense cut-grass
    # weave. Wrapped pixels make both maps seamless at the two-meter repeat.
    for _ in range(15000):
        px, py = rng.uniform(0, size, 2)
        angle = rng.uniform(0, math.tau)
        length = rng.uniform(7, 29)
        width = rng.uniform(.7, 2.2)
        tint = np.array([.105, .275, .030]) * rng.uniform(.88, 1.12)
        for t in np.linspace(0, 1, int(length*1.6)):
            for across in [-1, 0, 1]:
                dx = across*width*(1-t)
                ix = int(px+math.cos(angle)*length*t-math.sin(angle)*dx) % size
                iy = int(py+math.sin(angle)*length*t+math.cos(angle)*dx) % size
                rgb[iy, ix] = tint*(.94+.10*t)
                height[iy, ix] = max(height[iy, ix], .0007*math.sin(math.pi*t))
    # Gentle smoothing keeps fine blade relief stable as the camera moves.
    for _ in range(4):
        height = (height*4 + np.roll(height,1,0) + np.roll(height,-1,0)
                  + np.roll(height,1,1) + np.roll(height,-1,1))/8
        rgb = (rgb*4 + np.roll(rgb,1,0) + np.roll(rgb,-1,0)
               + np.roll(rgb,1,1) + np.roll(rgb,-1,1))/8
    dx = (np.roll(height,-1,1)-np.roll(height,1,1))*size/4
    dy = (np.roll(height,-1,0)-np.roll(height,1,0))*size/4
    normal = np.stack((-dx,-dy,np.ones_like(height)), axis=2)
    normal /= np.linalg.norm(normal,axis=2,keepdims=True)
    result = []
    for name, data, noncolor in [('Neris-Grass-Color',rgb,False),
                                  ('Neris-Grass-Normal',normal*.5+.5,True)]:
        image = bpy.data.images.get(name) or bpy.data.images.new(name,size,size,alpha=True)
        if noncolor:
            image.colorspace_settings.name = 'Non-Color'
        image.pixels.foreach_set(np.concatenate((data,np.ones((size,size,1))),axis=2).astype(np.float32).ravel())
        image.filepath_raw = str(ROOT/'Textures'/(name+'.png'))
        image.file_format = 'PNG'
        image.save()
        image.pack()
        result.append(image)
    return result


def apply():
    color, normal = textures()
    grass = bpy.data.materials['Town Grass']
    grass.diffuse_color = (.10,.27,.03,1)
    nodes, links = grass.node_tree.nodes, grass.node_tree.links
    nodes.clear()
    shader = nodes.new('ShaderNodeBsdfPrincipled')
    shader.inputs['Base Color'].default_value = (1,1,1,1)
    shader.inputs['Roughness'].default_value = .92
    output = nodes.new('ShaderNodeOutputMaterial')
    links.new(shader.outputs['BSDF'],output.inputs['Surface'])
    position = nodes.new('ShaderNodeNewGeometry')
    scale = nodes.new('ShaderNodeVectorMath'); scale.operation = 'SCALE'
    scale.inputs[3].default_value = .5
    links.new(position.outputs['Position'],scale.inputs[0])
    albedo = nodes.new('ShaderNodeTexImage'); albedo.image = color
    relief = nodes.new('ShaderNodeTexImage'); relief.image = normal
    for tex in [albedo,relief]:
        links.new(scale.outputs[0],tex.inputs['Vector'])
    links.new(albedo.outputs['Color'],shader.inputs['Base Color'])
    bump = nodes.new('ShaderNodeNormalMap'); bump.inputs['Strength'].default_value = .18
    links.new(relief.outputs['Color'],bump.inputs['Color'])
    links.new(bump.outputs[0],shader.inputs['Normal'])
    grass['neris_grass_texture'] = True
    grass['neris_grass_repeat_meters'] = 2.0

    old = bpy.data.objects.get('Neris Lawn Blade Batch')
    if old:
        bpy.data.objects.remove(old,do_unlink=True)
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    # Rasterize evaluated object bounds once. This avoids thousands of expensive
    # scene ray casts across the town's 23,000 collection instances. Conservative
    # clearance also leaves short grass out from under building eaves and shrubs.
    cell = .50
    import json
    bounds = json.loads((ROOT/'expansion-layout.json').read_text())['bounds']
    west, south, east, north = bounds
    shape = (math.ceil((north-south)/cell), math.ceil((east-west)/cell))
    ground = np.full(shape, -100.0)
    blockers = []
    for inst in depsgraph.object_instances:
        item = inst.object
        if item.type not in {'MESH','CURVE','FONT','SURFACE'}:
            continue
        points = [inst.matrix_world @ Vector(corner) for corner in item.bound_box]
        lo = [min(p[a] for p in points) for a in range(3)]
        hi = [max(p[a] for p in points) for a in range(3)]
        slots = list(item.data.materials)
        lawn = slots and all(m and m.name == grass.name for m in slots)
        padding = 0 if lawn else .18
        x0 = max(0,math.floor((lo[0]-padding-west)/cell))
        x1 = min(shape[1],math.ceil((hi[0]+padding-west)/cell))
        y0 = max(0,math.floor((lo[1]-padding-south)/cell))
        y1 = min(shape[0],math.ceil((hi[1]+padding-south)/cell))
        if x0>=x1 or y0>=y1:
            continue
        if lawn:
            ground[y0:y1,x0:x1] = np.maximum(ground[y0:y1,x0:x1],hi[2])
        elif item.get('neris_paving_surface') or item.name.startswith(('Unified Expanded Road Mortar','Unified Expanded Slate Tiles')):
            # These meshes combine disconnected road tiles. Their whole-object
            # box includes the lawns between streets; mask each actual tile.
            for face in item.data.polygons:
                corners = [inst.matrix_world @ item.data.vertices[v].co for v in face.vertices]
                fx0 = max(0,math.floor((min(p.x for p in corners)-.18-west)/cell))
                fx1 = min(shape[1],math.ceil((max(p.x for p in corners)+.18-west)/cell))
                fy0 = max(0,math.floor((min(p.y for p in corners)-.18-south)/cell))
                fy1 = min(shape[0],math.ceil((max(p.y for p in corners)+.18-south)/cell))
                blockers.append((fy0,fy1,fx0,fx1,hi[2]))
        else:
            blockers.append((y0,y1,x0,x1,hi[2]))
    free = ground > -1
    for y0,y1,x0,x1,z in blockers:
        free[y0:y1,x0:x1] &= z < ground[y0:y1,x0:x1]+.02
    # Keep the outer boundary clipped and leave a little extra clearance on roads.
    free[[0,-1],:] = False; free[:,[0,-1]] = False
    cells = np.argwhere(free)
    print('GRASS placement mask:',len(cells),'open cells',flush=True)
    assert len(cells) >= 13000, 'Grass placement must retain the open town lawns.'
    rng = random.Random(92626)
    vertices, faces, colors = [], [], []
    placements = []
    for j,i in rng.sample(list(map(tuple,cells)),min(13000,len(cells))):
        x,y = west+(i+.5)*cell,south+(j+.5)*cell
        point = Vector((x,y,float(ground[j,i])))
        placements.append([round(x,3),round(y,3),round(point.z,3)])
        for _ in range(3):
            a = rng.uniform(0,math.tau)
            base = point + Vector((rng.uniform(-.10,.10),rng.uniform(-.10,.10),.006))
            h,w = rng.uniform(.10,.18),rng.uniform(.045,.07)
            side = Vector((math.cos(a),math.sin(a),0))
            bend = side*rng.uniform(-.055,.055)
            i = len(vertices)
            vertices.extend([tuple(base-side*w/2),tuple(base+side*w/2),
                             tuple(base+bend+Vector((0,0,h)))])
            faces.append((i,i+1,i+2))
            colors.append(rng.randrange(2))
    mesh = bpy.data.meshes.new('Neris Short Lawn Blades')
    mesh.from_pydata(vertices,[],faces); mesh.update()
    for name,rgb in [('Neris Grass Blades Jade',(.09,.24,.027)),
                     ('Neris Grass Blades Sunlit',(.115,.295,.035))]:
        material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        material.use_nodes = True; material.use_backface_culling = False
        node = material.node_tree.nodes.get('Principled BSDF')
        node.inputs['Base Color'].default_value = (*rgb,1)
        node.inputs['Roughness'].default_value = .90
        material.diffuse_color = (*rgb,1)
        mesh.materials.append(material)
    for face,index in zip(mesh.polygons,colors):
        face.material_index = index
    obj = bpy.data.objects.new('Neris Lawn Blade Batch',mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj['tuft_count'] = len(placements)
    obj['blades_per_tuft'] = 3
    obj['scatter_seed'] = 92626
    bpy.context.scene['Neris Grass'] = f'{len(placements)} fixed short tufts, two-meter albedo/normal repeat'
    print('GRASS',len(placements),'tufts',len(faces),'blades',flush=True)


if __name__ == '__main__':
    assert bpy.app.background and Path(bpy.data.filepath).name == 'Neris-Town-Expanded.blend'
    apply()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Expanded.blend'),compress=True)
