"""Blender-native preview, editable-file delivery, and material-merged GLB export."""
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

from geometry import material


def repair_normals(collection):
    for obj in collection.objects:
        if obj.type=='MESH':
            bm=bmesh.new()
            bm.from_mesh(obj.data)
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
            bm.to_mesh(obj.data)
            bm.free()


def lighting(scene, dimensions):
    h=dimensions['height']
    scene.render.engine='BLENDER_EEVEE'
    scene.eevee.taa_render_samples=48
    scene.render.resolution_x=1280
    scene.render.resolution_y=1280
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.world.color=(.35,.35,.35)
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.63,.71,.8,1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.3
    scene.view_settings.view_transform='AgX'
    scene.view_settings.look='AgX - Medium High Contrast'
    stage=bpy.data.collections.new('Preview Studio — Not Exported')
    scene.collection.children.link(stage)
    mesh=bpy.data.meshes.new('Ground')
    extent=h*8
    mesh.from_pydata([(-extent,-extent,-.025),(extent,-extent,-.025),
                     (extent,extent,-.025),(-extent,extent,-.025)],[],[(0,1,2,3)])
    ground=bpy.data.objects.new('Preview Ground',mesh)
    stage.objects.link(ground)
    mesh.materials.append(material('Warm Gray Studio Floor',(.63,.63,.60),roughness=.85))
    for name,pos,power,size,color in [
        ('Large Key',(-h*1.1,-h*1.5,h*2),h*h*36,h*.95,(1,.88,.70)),
        ('Cool Fill',(h*1.3,-h*.5,h*1.3),h*h*20,h*.8,(.64,.82,1)),
        ('Rear Rim',(h*.6,h,h*1.7),h*h*48,h*.7,(1,.94,.84))]:
        light=bpy.data.lights.new(name,'AREA')
        light.energy=power
        light.shape='DISK'
        light.size=size
        light.color=color
        obj=bpy.data.objects.new(name,light)
        stage.objects.link(obj)
        obj.location=pos
        obj.rotation_euler=(Vector((0,0,h*.42))-obj.location).to_track_quat('-Z','Y').to_euler()
    camera=bpy.data.cameras.new('Architectural Camera')
    camera.type='ORTHO'
    obj=bpy.data.objects.new('Architectural Camera',camera)
    stage.objects.link(obj)
    scene.camera=obj
    return stage


def aim_camera(scene,dimensions,view):
    h=dimensions['height']
    target=Vector((0,0,h*.48))
    vectors={'Beauty':(1.22,-1.8,.95),'Front':(0,-2.8,0),'Back':(0,2.8,0),
             'Left':(-2.8,0,0),'Right':(2.8,0,0)}
    vec=Vector(vectors[view])*h
    cam=scene.camera
    cam.location=target+vec
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    if view=='Beauty':
        cam.data.ortho_scale=max(h*1.4,dimensions['width']*1.45,dimensions['depth']*1.45)
    else:
        cam.data.ortho_scale=max(h*1.15,dimensions['width']*1.15,dimensions['depth']*1.15)
    cam.data.lens=50
    cam.data.clip_end=1000


def pack_reference(scene,path):
    image=bpy.data.images.load(str(path),check_existing=True)
    image.pack()
    obj=bpy.data.objects.new('Original Four-View Reference — Packed',None)
    collection=bpy.data.collections.new('Reference — Not Exported')
    scene.collection.children.link(collection)
    collection.objects.link(obj)
    obj.empty_display_type='IMAGE'
    obj.data=image
    obj.empty_display_size=12
    obj.location=(-20,0,6)
    obj.rotation_euler.x=math.pi/2
    obj.hide_render=True
    obj.hide_set(True)
    return collection


def export_glb(g,path):
    """Bake evaluated transforms and merge by material; keep source parts editable."""
    bpy.context.view_layer.update()
    depsgraph=bpy.context.evaluated_depsgraph_get()
    groups={}
    source_triangles=0
    for obj in g.collection.objects:
        if obj.type not in {'MESH','CURVE','FONT'}:
            continue
        evaluated=obj.evaluated_get(depsgraph)
        mesh=evaluated.to_mesh()
        matrix=obj.matrix_world
        for face in mesh.polygons:
            mat=mesh.materials[face.material_index]
            group=groups.setdefault(mat.name,{'mat':mat,'verts':[],'faces':[],'smooth':[]})
            offset=len(group['verts'])
            group['verts'].extend([tuple(matrix@mesh.vertices[v].co) for v in face.vertices])
            group['faces'].append(tuple(range(offset,offset+len(face.vertices))))
            group['smooth'].append(face.use_smooth)
            source_triangles+=len(face.vertices)-2
        evaluated.to_mesh_clear()
    export_collection=bpy.data.collections.new('Export Temporary')
    bpy.context.scene.collection.children.link(export_collection)
    bounds=[]
    export_triangles=0
    for name,data in groups.items():
        mesh=bpy.data.meshes.new(name+' Export Mesh')
        mesh.from_pydata(data['verts'],[],data['faces'])
        mesh.materials.append(data['mat'])
        for poly,smooth in zip(mesh.polygons,data['smooth']):
            poly.use_smooth=smooth
        # Weld shared vertices within each material to keep files compact.
        bm=bmesh.new(); bm.from_mesh(mesh)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
        collapsed=[face for face in bm.faces if face.calc_area()<1e-10]
        if collapsed:
            bmesh.ops.delete(bm,geom=collapsed,context='FACES_ONLY')
        bmesh.ops.triangulate(bm,faces=list(bm.faces))
        export_triangles+=len(bm.faces)
        bm.to_mesh(mesh); bm.free()
        obj=bpy.data.objects.new(name,mesh)
        export_collection.objects.link(obj)
        bounds.extend(tuple(v.co) for v in mesh.vertices)
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',
        collection=export_collection.name,export_apply=True,export_animations=False,
        export_cameras=False,export_lights=False,export_extras=True)
    stats={'source_objects':len(g.collection.objects),'triangles':export_triangles,
           'source_triangles_before_cleanup':source_triangles,
           'export_material_groups':len(groups),
           'bounds_min':[min(v[i] for v in bounds) for i in range(3)],
           'bounds_max':[max(v[i] for v in bounds) for i in range(3)]}
    for obj in list(export_collection.objects):
        mesh=obj.data
        bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.meshes.remove(mesh)
    bpy.data.collections.remove(export_collection)
    return stats


def deliver(g,dimensions,slug,reference,package,exports,views):
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'
    scene.unit_settings.scale_length=1
    scene['Created By']='Louiery R. Sincioco (Sin)'
    scene['Front Direction']='-Y; Z up. GLB uses standard Y up.'
    scene['Model Status']='Exterior concept model; not integrated into the game runtime.'
    repair_normals(g.collection)
    stats=export_glb(g,exports/(slug+'.glb'))
    lighting(scene,dimensions)
    pack_reference(scene,reference)
    aim_camera(scene,dimensions,'Beauty')
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.shading.type='MATERIAL'
            space.region_3d.view_distance=dimensions['height']*1.65
            space.region_3d.view_location=(0,0,dimensions['height']*.46)
            space.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
    bpy.ops.wm.save_as_mainfile(filepath=str(package/'Blend'/(slug+'.blend')),compress=True)
    for view in views:
        print('NERIS_RENDER',slug,view,flush=True)
        aim_camera(scene,dimensions,view)
        scene.render.filepath=str(package/'Previews'/(slug+'-'+view.lower()+'.png'))
        bpy.ops.render.render(write_still=True)
    stats.update({'name':slug,'dimensions_design':dimensions,'reference':reference.name})
    (package/'Previews'/(slug+'-validation.json')).write_text(json.dumps(stats,indent=2)+'\n')
    print('NERIS_COMPLETE',json.dumps(stats),flush=True)
