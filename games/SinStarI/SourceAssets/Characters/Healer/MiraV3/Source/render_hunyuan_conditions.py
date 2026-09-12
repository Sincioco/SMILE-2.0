import bpy, math, json
from pathlib import Path
from mathutils import Vector
root=Path(r"D:/AI/Mira3D/Mira3")
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
bpy.ops.import_scene.gltf(filepath=r"D:/AI/Mira3D/ComfyUI/output/Mira3/mira3-hunyuan21-shape_00001_.glb")
body=next(o for o in scene.objects if o.type=="MESH")
body.name="Mira3.HunyuanShape"
# Comfy's GLB is already Z-up in Blender. Rotate around Z to face paint camera +Y.
body.rotation_mode="XYZ"
body.rotation_euler.rotate_axis("Z",math.pi)
bpy.context.view_layer.objects.active=body
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
coords=[v.co.copy() for v in body.data.vertices]
center=Vector([(min(v[i] for v in coords)+max(v[i] for v in coords))/2 for i in range(3)])
diameter=max((v-center).length for v in coords)*2
factor=1.15/diameter
for v in body.data.vertices:v.co=(v.co-center)*factor
for poly in body.data.polygons:poly.use_smooth=False
material=bpy.data.materials.new("Hunyuan Geometry Conditions");material.use_nodes=True
body.data.materials.clear();body.data.materials.append(material)
nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
geom=nodes.new("ShaderNodeNewGeometry")
scale=nodes.new("ShaderNodeVectorMath");scale.operation="SCALE"
add=nodes.new("ShaderNodeVectorMath");add.operation="ADD";add.inputs[1].default_value=(.5,.5,.5)
emit=nodes.new("ShaderNodeEmission");out=nodes.new("ShaderNodeOutputMaterial")
links.new(scale.outputs[0],add.inputs[0]);links.new(add.outputs[0],emit.inputs[0]);links.new(emit.outputs[0],out.inputs[0])
camera=bpy.data.objects.new("Paint Camera",bpy.data.cameras.new("Paint Camera"));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type="ORTHO";camera.data.ortho_scale=1.2
scene.render.engine="CYCLES";scene.cycles.samples=1;scene.cycles.use_denoising=False
scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG";scene.render.image_settings.color_mode="RGB";scene.render.image_settings.color_depth="8"
scene.view_settings.view_transform="Raw";scene.view_settings.look="None";scene.view_settings.exposure=0;scene.view_settings.gamma=1
world=bpy.data.worlds.new("White");scene.world=world;world.use_nodes=True;world.node_tree.nodes.get("Background").inputs[0].default_value=(1,1,1,1)
views=[(0,0),(0,90),(0,180),(0,270),(60,0),(-60,180)]
records=[]
for index,(elev,azim) in enumerate(views):
 e=math.radians(-elev);a=math.radians(azim+90)
 camera.location=(1.45*math.cos(e)*math.cos(a),1.45*math.cos(e)*math.sin(a),1.45*math.sin(e))
 camera.rotation_euler=(-camera.location).to_track_quat("-Z","Y").to_euler()
 for kind,socket,mult in [("normal","Normal",.5),("position","Position",-1/1.15)]:
  links.new(geom.outputs[socket],scale.inputs[0]);scale.inputs[3].default_value=mult
  scene.render.filepath=str(root/f"{kind}-{index}.png")
  bpy.ops.render.render(write_still=True)
 records.append({"index":index,"elev":elev,"azim":azim,"camera_matrix":[list(r) for r in camera.matrix_world]})
(root/"paint-views.json").write_text(json.dumps({"views":records,"rotation_z":180,"center":list(center),"scale":factor,"ortho_scale":1.2},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/"mira3-paint-conditions.blend"))
print("HUNYUAN_CONDITIONS_READY",flush=True)
