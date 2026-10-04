"""Append real mesh thumbnails to the untouched original Studio palette."""
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]


def render():
    templates=json.loads((ROOT/'Authoring/catalog-extension.json').read_text())['templates']
    original=bpy.data.images.load(str(ROOT.parent/'NerisTownV1/Authoring/Town-Palette.png'))
    width,old_height=original.size
    old_rows=old_height//96
    total_rows=math.ceil((old_rows*5+len(templates))/5)
    height=total_rows*96
    pixels=[0.0]*(width*height*4)
    old=list(original.pixels)
    pixels[(height-old_height)*width*4:]=old
    scene=bpy.data.scenes.new('Metropolis Catalog Photographs')
    bpy.context.window.scene=scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x=128; scene.render.resolution_y=96
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.film_transparent=True
    scene.display.shading.light='STUDIO'
    scene.display.shading.color_type='MATERIAL'
    scene.display.shading.show_shadows=True
    scene.display.shading.show_cavity=True
    camera=bpy.data.objects.new('Catalog Camera',bpy.data.cameras.new('Catalog Camera'))
    camera.data.type='ORTHO'; camera.data.clip_end=20000
    scene.collection.objects.link(camera); scene.camera=camera
    library=bpy.data.collections['Metropolis Reusable Templates']
    members={o['metropolis_template']:o for o in library.objects}
    folder=ROOT/'Previews/Catalog'
    folder.mkdir(exist_ok=True)
    for index,template in enumerate(templates):
        obj=members[template['label']].copy()
        scene.collection.objects.link(obj)
        low,high=map(Vector,template['bounds'])
        target=(low+high)*.5
        extent=max(high-low)
        camera.location=target+Vector((1.2,-1.6,1))*max(10,extent)*2
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.ortho_scale=max(20,extent*1.7)
        path=folder/(str(template['id'])+'.png')
        scene.render.filepath=str(path)
        bpy.ops.render.render(write_still=True)
        tile=bpy.data.images.load(str(path))
        data=list(tile.pixels)
        slot=old_rows*5+index
        left=(slot%5)*128; bottom=height-(slot//5+1)*96
        for y in range(96):
            offset=((bottom+y)*width+left)*4
            pixels[offset:offset+512]=data[y*512:(y+1)*512]
        bpy.data.images.remove(tile)
        bpy.data.objects.remove(obj,do_unlink=True)
    atlas=bpy.data.images.new('Combined Town Palette',width=width,height=height,alpha=True)
    atlas.pixels.foreach_set(pixels)
    atlas.filepath_raw=str(ROOT/'Authoring/Town-Palette.png')
    atlas.file_format='PNG'; atlas.save()
    print(json.dumps(dict(thumbnails=len(templates),width=width,height=height)))


if __name__=='__main__':
    render()
