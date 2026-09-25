"""Clean the supplied Tripo castle without decimating its architecture or UVs.

The import contains a thin skirt below z=.085 and blue water at z<.155.
Keep the supplied GLB unchanged; produce an editable cleaned mesh and PBR export.
Run with background Blender. No changes are made to a live Blender session.
"""
from pathlib import Path
import hashlib
import json
import shutil
import bpy
import numpy as np
from mathutils import Vector
from mathutils.kdtree import KDTree

ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path('C:/Users/louie/Downloads/Neris Castle - 4K - 2M - PBR - No Light.glb')
for folder in ('Original','Blend','Runtime','Previews'):
    (ROOT/folder).mkdir(exist_ok=True)
original=ROOT/'Original/Neris-Castle-Tripo.glb'
if not original.exists(): shutil.copy2(SOURCE,original)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(original))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
obj.name='Neris Tripo Castle Cleaned'
mesh=obj.data
positions=np.empty(len(mesh.vertices)*3,dtype=np.float32)
mesh.vertices.foreach_get('co',positions);positions=positions.reshape(-1,3)
loops=np.empty(len(mesh.loops),dtype=np.int32)
mesh.loops.foreach_get('vertex_index',loops)
uv=np.empty(len(mesh.loops)*2,dtype=np.float32)
mesh.uv_layers.active.data.foreach_get('uv',uv);uv=uv.reshape(-1,2)
assert all(p.loop_total==3 for p in mesh.polygons)
faces=loops.reshape(-1,3)
corners=positions[faces]
material=mesh.materials[0]
shader=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
image=shader.inputs['Base Color'].links[0].from_node.image
pixels=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32)
image.pixels.foreach_get(pixels);pixels=pixels.reshape(image.size[1],image.size[0],4)
rgb=pixels[(uv[:,1]*image.size[1]).astype(int).clip(0,image.size[1]-1),
           (uv[:,0]*image.size[0]).astype(int).clip(0,image.size[0]-1),:3].reshape(-1,3,3).mean(axis=1)
underside=corners[:,:,2].min(axis=1)<.085
water=(corners[:,:,2].mean(axis=1)<.155)&(rgb[:,2]>rgb[:,0]*1.16)&(rgb[:,1]>rgb[:,0]*1.07)
centers=corners.mean(axis=1)
normals=np.cross(corners[:,1]-corners[:,0],corners[:,2]-corners[:,0])
normals/=np.maximum(np.linalg.norm(normals,axis=1,keepdims=True),1e-10)
bridge=(abs(centers[:,0])<.19)&(centers[:,1]<-.28)
flat=(centers[:,2]<.155)&(abs(normals[:,2])>.60)&~bridge
# Keep the stone caps around tower feet. They sit at water height but connect
# directly to architecture above it, unlike the detached grey underside sheets.
seed=positions[(positions[:,2]>.16)&(positions[:,2]<.25)]
tree=KDTree(len(seed))
for index,point in enumerate(seed):tree.insert(point,index)
tree.balance()
caps=np.zeros(len(faces),dtype=bool)
for index in np.flatnonzero(flat & (centers[:,2]>.13)):
    caps[index]=tree.find(centers[index])[2]<.025
water|=flat&~caps
keep=~(underside|water)
remaining=faces[keep]
attached=positions[:,2]>.16
for step in range(400):
    connected=attached[remaining].any(axis=1)
    next_attached=attached.copy()
    next_attached[remaining[connected].ravel()]=True
    if np.array_equal(next_attached,attached):break
    attached=next_attached
else: raise RuntimeError('Low-fragment connectivity did not settle')
detached=keep&~attached[faces].any(axis=1)
keep&=~detached
used, remap=np.unique(faces[keep].ravel(),return_inverse=True)
clean=bpy.data.meshes.new('Cleaned Castle Architecture')
clean.from_pydata(positions[used].tolist(),[],remap.reshape(-1,3).tolist())
clean.materials.append(material)
clean.polygons.foreach_set('use_smooth',np.ones(len(clean.polygons),dtype=bool))
clean.uv_layers.new(name='UVMap').data.foreach_set('uv',uv.reshape(-1,3,2)[keep].ravel())
clean.update();obj.data=clean
bpy.data.meshes.remove(mesh)
obj['Cleanup']='Removed low hanging skirt and blue imported water; retained architecture, original UVs and 4K PBR maps'
obj['Source SHA256']=hashlib.sha256(original.read_bytes()).hexdigest()
collection=bpy.data.collections.new('Neris Tripo Castle Architecture')
bpy.context.scene.collection.children.link(collection)
for owner in list(obj.users_collection):owner.objects.unlink(obj)
collection.objects.link(obj)
for image in bpy.data.images:
    if image.source=='FILE' and not image.packed_file:image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Castle-Cleaned.blend'),compress=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Neris-Castle-Cleaned.glb'),export_format='GLB',
    export_animations=False,export_cameras=False,export_lights=False,export_tangents=True)
report={'source_sha256':obj['Source SHA256'],'source_triangles':len(faces),
        'removed_underside':int(underside.sum()),'removed_water':int((water&~underside).sum()),
        'removed_detached_low_fragments':int(detached.sum()),
        'retained_triangles':int(keep.sum()),'retained_vertices':len(used),
        'minimum_height':float(positions[used,2].min()),'decimation':False}
(ROOT/'cleanup.json').write_text(json.dumps(report,indent=2)+'\n')
print('CASTLE CLEANUP',json.dumps(report),flush=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1280;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Castle Preview World');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.27,.33,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
bpy.ops.object.light_add(type='AREA',location=(2,-3,5));bpy.context.object.data.energy=350;bpy.context.object.data.size=4
bpy.ops.object.camera_add();scene.camera=bpy.context.object
scene.camera.data.type='ORTHO';scene.camera.data.ortho_scale=1.3
for label,pos in [('Cleaned-Front',(2,-3,1.5)),('Cleaned-Underneath',(1,-2,-1))]:
    scene.camera.location=pos
    scene.camera.rotation_euler=(Vector((0,0,.4))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(ROOT/'Previews'/(label+'.png'));bpy.ops.render.render(write_still=True)
