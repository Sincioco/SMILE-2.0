// Native payload derivation uses the existing Blender MCP, then plain JavaScript I/O.
// Pass portable_candidate and native_output_directory before dispatching partition.
export const partition = String.raw`
import bpy, os
source_scene=bpy.context.window.scene
assert not os.path.exists(native_output_directory)
os.makedirs(native_output_directory)
candidate=bpy.data.scenes.new('NC.NativeExport.Check')
bpy.context.window.scene=candidate
try:
    bpy.ops.import_scene.gltf(filepath=portable_candidate)
    objects=sorted([o for o in candidate.objects if o.type=='MESH'
        and o.name.startswith('NC.Export.') and not o.name.startswith('NC.Export.Bridge.')
        and o.name!='NC.Export.Site.Moat'],key=lambda o:o.name)
    batches=[];batch=[];vertices=0
    for o in objects:
        count=len(o.data.vertices)
        assert count<=65535
        if batch and (vertices+count>120000 or len(batch)>=12):
            batches.append(batch);batch=[];vertices=0
        batch.append(o);vertices+=count
    if batch:batches.append(batch)
    manifest=[]
    for i,batch in enumerate(batches):
        bpy.ops.object.select_all(action='DESELECT')
        for o in batch:o.select_set(True)
        bpy.context.view_layer.objects.active=batch[0]
        name='castle-%02d.glb'%i
        bpy.ops.export_scene.gltf(filepath=native_output_directory+'/'+name,
            export_format='GLB',use_selection=True,export_yup=True,export_apply=False,
            export_materials='EXPORT',export_texcoords=True,export_normals=True,
            export_animations=False,export_extras=False,export_cameras=False,
            export_lights=False,export_image_format='AUTO')
        manifest.append({'file':name,'parts':[o.name for o in batch],
            'vertices':sum(len(o.data.vertices) for o in batch),
            'triangles':sum(len(o.data.polygons) for o in batch)})
    result={'native_chunks':manifest,'total_static_parts':len(objects),
        'excluded':'NC.Export.Site.Moat: existing native terrain owns this surface'}
finally:
    bpy.context.window.scene=source_scene
    for o in list(candidate.objects):
        data=o.data
        bpy.data.objects.remove(o,do_unlink=True)
        if data and data.users==0:bpy.data.meshes.remove(data)
    bpy.data.scenes.remove(candidate)
`;

import fs from 'node:fs';
import path from 'node:path';
import {createHash} from 'node:crypto';
const hash=bytes=>createHash('sha256').update(bytes).digest('hex');

// Export image bytes unchanged. The project cooker owns runtime texture publication.
export function externalizeTexture(directory, manifest) {
  const textureFile='neris-ivory-courses-1024.png';
  for (const entry of manifest.native_chunks) {
    const file=path.join(directory,entry.file);
    let bytes=fs.readFileSync(file);
    const jsonLength=bytes.readUInt32LE(12);
    const json=JSON.parse(bytes.toString('utf8',20,20+jsonLength));
    for (const image of json.images||[]) {
      const view=json.bufferViews[image.bufferView];
      if (!view) throw Error('Expected newly exported embedded image.');
      const start=28+jsonLength+(view.byteOffset||0);
      fs.writeFileSync(path.join(directory,textureFile),bytes.subarray(start,start+view.byteLength));
      delete image.bufferView;
      image.uri=textureFile;
    }
    const encoded=Buffer.from(JSON.stringify(json));
    const padded=Buffer.alloc(Math.ceil(encoded.length/4)*4,32);encoded.copy(padded);
    const chunk=Buffer.alloc(8);chunk.writeUInt32LE(padded.length);chunk.write('JSON',4);
    bytes=Buffer.concat([bytes.subarray(0,12),chunk,padded,bytes.subarray(20+jsonLength)]);
    bytes.writeUInt32LE(bytes.length,8);
    fs.writeFileSync(file,bytes);entry.sha256=hash(bytes);
  }
  manifest.texture={file:textureFile,uri:textureFile,
    sha256:hash(fs.readFileSync(path.join(directory,textureFile)))};
  fs.writeFileSync(path.join(directory,'native-layout.json'),JSON.stringify(manifest,null,2)+'\n');
}
