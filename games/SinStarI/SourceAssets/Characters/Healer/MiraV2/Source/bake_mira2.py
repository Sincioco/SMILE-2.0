import bpy,json,traceback
from pathlib import Path
root=Path(r'D:\AI\Mira3D\Mira2')
try:
 scene=bpy.context.scene;low=bpy.data.objects['Mira2.Body.Final'];high=bpy.data.objects['Mira2.High.Source']
 prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
 for d in prefs.devices: d.use=d.type=='OPTIX'
 scene.cycles.device='GPU';scene.cycles.samples=8
 mat=low.data.materials[0];nodes=mat.node_tree.nodes;target=nodes['BakeTarget'];shader=nodes.get('Principled BSDF')
 bpy.ops.object.select_all(action='DESELECT');high.hide_set(False);high.hide_render=False;high.select_set(True);low.select_set(True);bpy.context.view_layer.objects.active=low
 for channel,bake_type in [('BaseColor','EMIT'),('Normal','NORMAL'),('Roughness','ROUGHNESS')]:
  print('BAKING '+channel,flush=True)
  image=target.image if channel=='BaseColor' else bpy.data.images.new('Mira2.'+channel,width=4096,height=4096,alpha=False)
  if channel!='BaseColor': image.colorspace_settings.name='Non-Color'
  target.image=image;nodes.active=target
  if channel=='BaseColor':
   hm=high.data.materials[0];hn=hm.node_tree.nodes;hs=hn.get('Principled BSDF');ho=next(n for n in hn if n.type=='OUTPUT_MATERIAL');em=hn.new('ShaderNodeEmission')
   if hs.inputs['Base Color'].is_linked: hm.node_tree.links.new(hs.inputs['Base Color'].links[0].from_socket,em.inputs['Color'])
   else: em.inputs['Color'].default_value=hs.inputs['Base Color'].default_value
   hm.node_tree.links.new(em.outputs[0],ho.inputs['Surface'])
  bpy.ops.object.bake(type=bake_type)
  if channel=='BaseColor':
   hm.node_tree.links.new(hs.outputs[0],ho.inputs['Surface']);hn.remove(em)
  image.filepath_raw=str(root/('mira2-'+channel.lower()+'.png'));image.file_format='PNG';image.save();image.pack()
  image_node=nodes.new('ShaderNodeTexImage');image_node.image=image
  if channel=='BaseColor': mat.node_tree.links.new(image_node.outputs['Color'],shader.inputs['Base Color'])
  elif channel=='Normal':
   normal=nodes.new('ShaderNodeNormalMap');mat.node_tree.links.new(image_node.outputs['Color'],normal.inputs['Color']);mat.node_tree.links.new(normal.outputs['Normal'],shader.inputs['Normal'])
  elif channel=='Roughness': mat.node_tree.links.new(image_node.outputs['Color'],shader.inputs['Roughness'])
 shader.inputs['Metallic'].default_value=.1
 nodes.remove(target)
 high.hide_set(True);high.hide_render=True;bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
 bpy.ops.export_scene.gltf(filepath=str(root/'mira2-unrigged-body.glb'),export_format='GLB',use_selection=True,export_animations=False)
 bpy.ops.export_scene.fbx(filepath=str(root/'Mira2-TRELLIS2.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')
 bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira2-baked-body.blend'))
 (root/'bake-status.json').write_text(json.dumps({'status':'complete','bodyTriangles':sum(len(p.vertices)-2 for p in low.data.polygons)}))
except Exception:
 (root/'bake-status.json').write_text(json.dumps({'status':'failed','traceback':traceback.format_exc()}));raise
