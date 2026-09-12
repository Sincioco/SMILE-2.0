"""Project Hunyuan's own six PBR views onto its mesh and bake portable UV maps."""
import bpy, json
from pathlib import Path
from mathutils import Vector, Matrix
root=Path(r'D:\AI\Mira3D\Mira3')
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-low-setup.blend'))
scene=bpy.context.scene;high=bpy.data.objects['Mira3.High'];low=bpy.data.objects['Mira3.Body']
views=json.loads((root/'paint-views.json').read_text(encoding='utf-8'))
mat=bpy.data.materials.new('Mira3.Hunyuan.Multiview.PBR');mat.use_nodes=True
high.data.materials.clear();high.data.materials.append(mat)
nodes=mat.node_tree.nodes;nodes.clear();links=mat.node_tree.links
def vector(op,a=None,b=None):
    n=nodes.new('ShaderNodeVectorMath');n.operation=op
    if a is not None:
        if hasattr(a,'is_output'):links.new(a,n.inputs[0])
        else:n.inputs[0].default_value=a
    if b is not None:
        slot=3 if op=='SCALE' else 1
        if hasattr(b,'is_output'):links.new(b,n.inputs[slot])
        else:n.inputs[slot].default_value=b
    return n.outputs['Value' if op in ('DOT_PRODUCT','DISTANCE','LENGTH') else 'Vector']
def math(op,a,b):
    n=nodes.new('ShaderNodeMath');n.operation=op
    for i,value in enumerate((a,b)):
        if hasattr(value,'is_output'):links.new(value,n.inputs[i])
        else:n.inputs[i].default_value=value
    return n.outputs[0]
def texture(path,uv,noncolor=False,closest=False):
    n=nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(path),check_existing=True)
    if noncolor:n.image.colorspace_settings.name='Non-Color'
    n.extension='EXTEND';n.interpolation='Closest' if closest else 'Linear';links.new(uv,n.inputs['Vector'])
    return n.outputs['Color']
geom=nodes.new('ShaderNodeNewGeometry')
def projection(folder,coord,is_head=False):
    expected=vector('ADD',vector('SCALE',coord,-1/1.15),(.5,.5,.5))
    weights=[];albedos=[];mrs=[]
    for view in views['views']:
        index=view['index'];matrix=Matrix(view['camera_matrix']);axisx=matrix.to_3x3().col[0];axisy=matrix.to_3x3().col[1];direction=matrix.to_3x3().col[2]
        uvnode=nodes.new('ShaderNodeCombineXYZ')
        links.new(math('ADD',vector('DOT_PRODUCT',coord,axisx/1.2),.5),uvnode.inputs['X'])
        links.new(math('ADD',vector('DOT_PRODUCT',coord,axisy/1.2),.5),uvnode.inputs['Y'])
        uv=uvnode.outputs[0]
        observed=texture(folder/f'position-{index}.png',uv,True,True)
        visible=math('LESS_THAN',vector('DISTANCE',observed,expected),.022 if is_head else .013)
        facing=math('POWER',math('MAXIMUM',vector('DOT_PRODUCT',geom.outputs['Normal'],direction),0),12 if is_head else 6)
        weight=math('MULTIPLY',facing,math('ADD',visible,.002))
        if is_head and index==0:weight=math('MULTIPLY',weight,2.5)
        weights.append(weight)
        albedos.append(vector('SCALE',texture(folder/f'albedo-{index}.png',uv),weight))
        mrs.append(vector('SCALE',texture(folder/f'mr-{index}.png',uv,True),weight))
    total=weights[0];base=albedos[0];mr=mrs[0]
    for i in range(1,6):
        total=math('ADD',total,weights[i]);base=vector('ADD',base,albedos[i]);mr=vector('ADD',mr,mrs[i])
    inverse=math('DIVIDE',1,math('MAXIMUM',total,.000001))
    return vector('SCALE',base,inverse),vector('SCALE',mr,inverse)
base,mr=projection(root,geom.outputs['Position'])
headviews=json.loads((root/'HeadPaint'/'paint-views.json').read_text(encoding='utf-8'))
headcoord=vector('SCALE',vector('SUBTRACT',geom.outputs['Position'],headviews['center']),headviews['scale'])
headbase,headmr=projection(root/'HeadPaint',headcoord,True)
splitpos=nodes.new('ShaderNodeSeparateXYZ');links.new(geom.outputs['Position'],splitpos.inputs[0])
blend=math('MINIMUM',math('MAXIMUM',math('DIVIDE',math('SUBTRACT',splitpos.outputs['Z'],.391),.024),0),1)
base=vector('ADD',vector('SCALE',base,math('SUBTRACT',1,blend)),vector('SCALE',headbase,blend))
mr=vector('ADD',vector('SCALE',mr,math('SUBTRACT',1,blend)),vector('SCALE',headmr,blend))
# Hunyuan stores metalness in R; glTF stores it in B. Skin/hair stay nonmetallic.
splitmr=nodes.new('ShaderNodeSeparateColor');links.new(mr,splitmr.inputs[0])
packedmr=nodes.new('ShaderNodeCombineColor');packedmr.inputs['Red'].default_value=1
links.new(math('MAXIMUM',splitmr.outputs['Green'],math('MULTIPLY',blend,.45)),packedmr.inputs['Green'])
links.new(math('MULTIPLY',splitmr.outputs['Red'],math('SUBTRACT',1,blend)),packedmr.inputs['Blue'])
mr=packedmr.outputs[0]
emit=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial');links.new(emit.outputs[0],output.inputs[0])
targetmat=bpy.data.materials.new('Mira3.Hunyuan.Baked');targetmat.use_nodes=True
low.data.materials.clear();low.data.materials.append(targetmat)
targetnodes=targetmat.node_tree.nodes;targetlinks=targetmat.node_tree.links;shader=targetnodes.get('Principled BSDF')
target=targetnodes.new('ShaderNodeTexImage');target.name='BakeTarget';targetnodes.active=target
direct_target=nodes.new('ShaderNodeTexImage');direct_target.name='DirectBakeTarget'
scene.render.engine='CYCLES';scene.cycles.samples=4
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for device in prefs.devices:device.use=device.type=='OPTIX'
scene.cycles.device='GPU'
scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.008;scene.render.bake.max_ray_distance=.024;scene.render.bake.margin=12
bpy.ops.object.select_all(action='DESELECT');high.hide_set(False);high.select_set(True);bpy.context.view_layer.objects.active=high
modifier=high.modifiers.new('Clean Normal Bake Surface','REMESH');modifier.mode='VOXEL';modifier.voxel_size=.0014;modifier.use_smooth_shade=True
bpy.ops.object.modifier_apply(modifier=modifier.name)
for poly in high.data.polygons:poly.use_smooth=True
high.select_set(False);high.hide_set(True);high.hide_render=True;low.hide_set(False);low.select_set(True);bpy.context.view_layer.objects.active=low
for kind,socket,bake in [('basecolor',base,'EMIT'),('metallic-roughness',mr,'EMIT'),('normal',None,'NORMAL')]:
    print('BAKING',kind,flush=True)
    image=bpy.data.images.new('Mira3.'+kind,width=4096,height=4096,alpha=False)
    if kind!='basecolor':image.colorspace_settings.name='Non-Color'
    target.image=image;targetnodes.active=target
    if socket:links.new(socket,emit.inputs['Color'])
    if bake=='EMIT':
        scene.render.bake.use_selected_to_active=False;low.data.materials[0]=mat;direct_target.image=image;nodes.active=direct_target
    else:
        scene.render.bake.use_selected_to_active=True;low.data.materials[0]=targetmat
        high.hide_set(False);high.hide_render=False;high.select_set(True)
    bpy.ops.object.bake(type=bake)
    image.filepath_raw=str(root/f'mira3-{kind}.png');image.file_format='PNG';image.save();image.pack()
    node=targetnodes.new('ShaderNodeTexImage');node.image=image
    if kind=='basecolor':targetlinks.new(node.outputs['Color'],shader.inputs['Base Color'])
    elif kind=='metallic-roughness':
        split=targetnodes.new('ShaderNodeSeparateColor');targetlinks.new(node.outputs['Color'],split.inputs[0])
        targetlinks.new(split.outputs['Green'],shader.inputs['Roughness']);targetlinks.new(split.outputs['Blue'],shader.inputs['Metallic'])
    else:
        normal=targetnodes.new('ShaderNodeNormalMap');targetlinks.new(node.outputs['Color'],normal.inputs['Color']);targetlinks.new(normal.outputs['Normal'],shader.inputs['Normal'])
targetnodes.remove(target)
nodes.remove(direct_target);low.data.materials[0]=targetmat
high.hide_set(True);high.hide_render=True;bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
# A self-contained textured source retains the paint coordinate system for provenance.
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-baked-source.blend'))
# Export the character in Blender's -Y forward convention, 1.72 m tall and grounded.
height=max(v.co.z for v in low.data.vertices)-min(v.co.z for v in low.data.vertices);factor=1.72/height
minimum=min(v.co.z for v in low.data.vertices)
for v in low.data.vertices:v.co=Vector((-v.co.x*factor,-v.co.y*factor,(v.co.z-minimum)*factor))
low.data.update()
bpy.ops.export_scene.gltf(filepath=str(root/'mira3-unrigged-body.glb'),export_format='GLB',use_selection=True,export_animations=False,export_tangents=True)
bpy.ops.wm.obj_export(filepath=str(root/'Mira3-Mixamo.obj'),export_selected_objects=True,export_materials=False,forward_axis='NEGATIVE_Z',up_axis='Y')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'mira3-unrigged-baked.blend'))
print('MIRA3_BAKED_READY',flush=True)
