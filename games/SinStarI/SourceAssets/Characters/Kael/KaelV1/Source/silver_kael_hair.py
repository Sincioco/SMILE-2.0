"""Assign a cool silver material to Kael's existing hair geometry, preserving texture detail.

The original packed textures stay byte-identical. A final skinned hair part preserves
body=0 and sword=1 for existing runtime attachments and visibility controls.
"""
import json
from pathlib import Path
import bmesh
import bpy
import numpy as np

COLOR = (.32, .31, .38, 1)
ROUGHNESS = .42


def finish_material(material):
    shader = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    shader.inputs['Metallic'].default_value = 0
    shader.inputs['Roughness'].default_value = ROUGHNESS
    for link in list(shader.inputs['Metallic'].links) + list(shader.inputs['Roughness'].links):
        material.node_tree.links.remove(link)
    for node in material.node_tree.nodes:
        if node.type == 'MIX_RGB' and node.blend_type == 'MULTIPLY':
            node.inputs[2].default_value = COLOR


def finish_gltf(gltf):
    material = next(m for m in gltf['materials'] if m.get('name') == 'Kael.SilverGrayHair')
    surface = material['pbrMetallicRoughness']
    surface.update(baseColorFactor=list(COLOR), metallicFactor=0, roughnessFactor=ROUGHNESS)
    surface.pop('metallicRoughnessTexture', None)


def apply(body):
    existing=bpy.data.objects.get('Kael.ZHair')
    if existing:
        finish_material(existing.data.materials[0])
        return existing
    material=body.data.materials[0]
    shader=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    image=shader.inputs['Base Color'].links[0].from_node.image
    pixels=np.empty(len(image.pixels),dtype=np.float32)
    image.pixels.foreach_get(pixels)
    pixels=pixels.reshape(image.size[1],image.size[0],4)
    uv=body.data.uv_layers.active
    chosen=[]
    for face in body.data.polygons:
        p=face.center
        # Crown, back lengths and the two forward locks; exclude face and central tabard.
        region=(p.z>.855 and abs(p.x)<.125) or (
            .72<p.z<.89 and .025<abs(p.x)<.105 and p.y<.015) or (
            p.z>.39 and abs(p.x)<.14 and p.y>.025) or (
            p.z>.79 and abs(p.x)<.105 and p.y>0)
        # The lower forward locks flank the white central breast cloth.
        if p.z < .82 and p.y < 0 and abs(p.x) < .04:
            region = False
        if not region:
            continue
        coords=[uv.data[i].uv for i in face.loop_indices]
        u=sum(v.x for v in coords)/len(coords)
        v=sum(v.y for v in coords)/len(coords)
        color=pixels[min(image.size[1]-1,int(v*image.size[1])),min(image.size[0]-1,int(u*image.size[0])),:3]
        if color.mean()>.32 and color.max()-color.min()<.10:
            chosen.append(face.index)
    if len(chosen)<300:
        raise RuntimeError(('Hair selection unexpectedly small',len(chosen)))
    # Regression witnesses on the canonical v1 grounded topology, inspected in
    # front/back/side views: tint the missed locks, never the breast cloth or face.
    coverage = {'backTip': 17459, 'frontLock': 15620,
                'shoulderRootUpper': 29348, 'shoulderRootLower': 29415}
    protected = {'breastCloth': 17963, 'face': 19849}
    selected=set(chosen)
    for label, index in coverage.items():
        if index not in selected:
            raise RuntimeError(('Hair coverage regression', label, index))
    for label, index in protected.items():
        if index in selected:
            raise RuntimeError(('Non-hair recolored', label, index))
    hair=body.copy(); hair.data=body.data.copy(); hair.name='Kael.ZHair'
    bpy.context.collection.objects.link(hair)
    for obj,keep in ((body,False),(hair,True)):
        mesh=bmesh.new(); mesh.from_mesh(obj.data); mesh.faces.ensure_lookup_table()
        remove=[f for f in mesh.faces if (f.index in selected)!=keep]
        bmesh.ops.delete(mesh,geom=remove,context='FACES')
        loose=[v for v in mesh.verts if not v.link_faces]
        bmesh.ops.delete(mesh,geom=loose,context='VERTS')
        mesh.to_mesh(obj.data); mesh.free(); obj.data.update()
    silver=material.copy(); silver.name='Kael.SilverGrayHair'
    silver_shader=next(n for n in silver.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    # glTF exports the texture plus this linear base-color factor, without repainting the atlas.
    source=silver_shader.inputs['Base Color'].links[0].from_socket
    tint=silver.node_tree.nodes.new('ShaderNodeMixRGB')
    tint.blend_type='MULTIPLY'; tint.inputs[0].default_value=1
    tint.inputs[2].default_value=COLOR
    silver.node_tree.links.new(source,tint.inputs[1])
    silver.node_tree.links.new(tint.outputs[0],silver_shader.inputs['Base Color'])
    finish_material(silver)
    hair.data.materials.clear(); hair.data.materials.append(silver)
    report={'hairTriangles':len(chosen),'bodyTriangles':len(body.data.polygons),
            'baseColorFactorLinear':list(COLOR), 'metallicFactor':0, 'roughnessFactor':ROUGHNESS,
            'partOrder':['Kael.Body','Kael.Sword','Kael.ZHair'],
            'coverageWitnesses':coverage, 'protectedWitnesses':protected,
            'originalPackedTextures':'unchanged', 'method':'material factor on selected existing skinned hair faces'}
    (Path(__file__).parent/'silver-hair-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
    return hair
