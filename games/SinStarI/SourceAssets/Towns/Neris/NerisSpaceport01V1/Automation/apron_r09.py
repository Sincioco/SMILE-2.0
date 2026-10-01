"""Remove apron grids and bake gold approach paint into filtered pavement.

Run in a disposable Blender process with r08 loaded. The r08 source is preserved;
the last argument is a new staging directory for the portable export and evidence.
"""
import bpy,json,sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Automation'))
from portable_export import chunked_export,scene_measure


def bake_approach(scene,destination):
    deck=scene.objects['NSP01.Hull.Approach']
    paint=[o for o in scene.objects if o.name.startswith('NSP01.Approach.Inlay.') or
           o.name in ('NSP01.Approach.CompassRing','NSP01.Compass.Plaza')]
    assert len(paint)==6
    bake=bpy.data.scenes.new('Approach Paint Bake')
    bake.render.engine='CYCLES';bake.cycles.samples=8;bake.cycles.use_denoising=False
    bake.render.resolution_x=2048;bake.render.resolution_y=4096
    bake.render.resolution_percentage=100
    bake.render.pixel_aspect_x=152*4096/(248*2048);bake.render.pixel_aspect_y=1
    bake.render.image_settings.file_format='PNG';bake.render.image_settings.color_mode='RGB'
    bake.view_settings.view_transform='Standard';bake.view_settings.look='None'
    bake.render.filepath=str(destination)
    for original in [deck]+paint:
        obj=original.copy();obj.data=original.data.copy();obj.parent=None
        obj.matrix_world=original.matrix_world.copy();bake.collection.objects.link(obj)
        for index,material in enumerate(original.data.materials):
            replacement=bpy.data.materials.new('Bake '+material.name);replacement.use_nodes=True
            nodes=replacement.node_tree.nodes;nodes.clear()
            emission=nodes.new('ShaderNodeEmission')
            emission.inputs['Color'].default_value=material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value
            output=nodes.new('ShaderNodeOutputMaterial')
            replacement.node_tree.links.new(emission.outputs[0],output.inputs['Surface'])
            obj.data.materials[index]=replacement
    camera=bpy.data.objects.new('Approach Paint Camera',bpy.data.cameras.new('Approach Paint Camera'))
    bake.collection.objects.link(camera);camera.location=(0,-266,100)
    camera.data.type='ORTHO';camera.data.ortho_scale=152;bake.camera=camera
    bpy.ops.render.render(write_still=True,scene=bake.name)
    bpy.data.batch_remove(ids=tuple(bake.objects));bpy.data.scenes.remove(bake)
    image=bpy.data.images.load(str(destination),check_existing=False);image.pack()
    material=bpy.data.materials.new('NSP01.MAT.FilteredApproach');material.use_nodes=True
    shader=material.node_tree.nodes['Principled BSDF'];shader.inputs['Roughness'].default_value=.9
    texture=material.node_tree.nodes.new('ShaderNodeTexImage');texture.image=image
    texture.interpolation='Linear'
    material.node_tree.links.new(texture.outputs['Color'],shader.inputs['Base Color'])
    deck.data.materials.clear();deck.data.materials.append(material)
    uv=deck.data.uv_layers.active or deck.data.uv_layers.new(name='Approach Paint')
    for loop in deck.data.loops:
        p=deck.matrix_world@deck.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=((p.x+76)/152,(p.y+390)/248)
    names=[o.name for o in paint]
    bpy.data.batch_remove(ids=tuple(paint))
    return names


stage=Path(sys.argv[sys.argv.index('--')+1]).resolve()
stage.mkdir(parents=True,exist_ok=False)
source=ROOT/'Source/NSP01-final-r08.blend'
target=ROOT/'Source/NSP01-final-r09.blend'
assert Path(bpy.data.filepath).resolve()==source.resolve() and not target.exists()
scene=bpy.context.scene
grid=[o for o in scene.objects if o.name.startswith(('NSP01.Approach.Seam.',
    'NSP01.Approach.LongSeam.','NSP01.Apron.Seam.','NSP01.Apron.Long.'))]
assert len(grid)==60,len(grid)
removed=[o.name for o in grid]
bpy.data.batch_remove(ids=tuple(grid))
paint=bake_approach(scene,stage/'Approach-Paint-r09.png')
scene['revision']='r09 apron grids removed and gold approach paint filtered'
bpy.ops.wm.save_as_mainfile(filepath=str(target))
export,selection=chunked_export(scene)
bpy.context.window.scene=export;bpy.context.view_layer.update()
before=scene_measure(export)
assert before['dimensions']==[720.0,620.0,404.0],before
portable=stage/'NSP01-preview.glb'
bpy.ops.export_scene.gltf(filepath=str(portable),export_format='GLB',use_active_scene=True,
    export_yup=True,export_extras=True,export_normals=True,export_texcoords=True,
    export_cameras=False,export_lights=False,export_animations=False)
review=bpy.data.scenes.new('r09 Round Trip');bpy.context.window.scene=review
bpy.ops.import_scene.gltf(filepath=str(portable));bpy.context.view_layer.update()
after=scene_measure(review)
assert before['triangles']==after['triangles'] and max(abs(a-b) for a,b in zip(before['dimensions'],after['dimensions']))<.001
report=dict(before=before,after=after,removed_grid=removed,baked_paint=paint,selection=selection)
(stage/'apron-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('PASS APRON',json.dumps({k:v for k,v in report.items() if k!='selection'}),flush=True)
