"""Write static, flat-PBR GLB without Blender re-splitting accepted corner normals."""
from array import array
import json
import struct
import math
from pathlib import Path


def material_json(material):
    node = next(n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    color = list(node.inputs["Base Color"].default_value)
    color[3] *= node.inputs["Alpha"].default_value
    result = {"name": material.name, "doubleSided": not material.use_backface_culling,
              "pbrMetallicRoughness": {"baseColorFactor": color,
                  "metallicFactor": node.inputs["Metallic"].default_value,
                  "roughnessFactor": node.inputs["Roughness"].default_value}}
    emission = list(node.inputs["Emission Color"].default_value)[:3]
    strength = node.inputs["Emission Strength"].default_value
    if strength > 0 and max(emission) > 0:
        result["emissiveFactor"] = emission
        result["extensions"] = {"KHR_materials_emissive_strength": {"emissiveStrength": strength}}
    if color[3] < 1:
        result["alphaMode"] = "BLEND"
    return result


def write(path, batch):
    document = {"asset": {"version": "2.0", "generator": "Neris evaluated geometry export"},
                "scene": 0, "scenes": [{"nodes": []}], "nodes": [], "meshes": [],
                "accessors": [], "bufferViews": [], "materials": [], "buffers": []}
    binary = bytearray()

    def texture(filename, semantic):
        images = document.setdefault('images', [])
        for i, item in enumerate(images):
            if item['name'] == filename: return i
        blob = (Path(__file__).resolve().parents[1] / 'Textures' / filename).read_bytes()
        binary.extend(b'\0' * (-len(binary) % 4))
        view = len(document['bufferViews'])
        document['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':len(blob)})
        binary.extend(blob)
        binary.extend(b'\0' * (-len(binary) % 4))
        i = len(images)
        images.append({'name':filename,'mimeType':'image/png','bufferView':view})
        document.setdefault('textures', []).append({'source':i,'sampler':0})
        document['samplers']=[{'magFilter':9729,'minFilter':9987,'wrapS':10497,'wrapT':10497}]
        return i

    def accessor(values, dimensions, component=5126):
        blob = array("f" if component == 5126 else "I", values).tobytes()
        view = len(document["bufferViews"])
        document["bufferViews"].append({"buffer": 0, "byteOffset": len(binary), "byteLength": len(blob)})
        binary.extend(blob)
        index = len(document["accessors"])
        document["accessors"].append({"bufferView": view, "componentType": component,
            "count": len(values) // dimensions, "type": {1: "SCALAR", 2: "VEC2", 3: "VEC3", 4: "VEC4"}[dimensions]})
        return index

    total_vertices = 0
    for index, (name, material, triangles, _) in enumerate(batch):
        positions, normals, tangents, indices, uvs, shared = [], [], [], [], [], {}
        stone = bool(material.get('neris_stone_texture'))
        for triangle in triangles:
            for corner in triangle:
                if corner not in shared:
                    shared[corner] = len(shared)
                    p, n = corner
                    positions.extend((p[0], p[2], -p[1]))
                    uvs.extend((p[0]/1.8, p[1]/1.8) if stone else (0.0,0.0))
                    nx, ny, nz = n[0], n[2], -n[1]
                    normals.extend((nx, ny, nz))
                    tx, ty, tz = (nz, 0.0, -nx) if abs(ny) < .9 else (0.0, -nz, ny)
                    length = math.sqrt(tx * tx + ty * ty + tz * tz)
                    tangents.extend((tx / length, ty / length, tz / length, 1.0))
                indices.append(shared[corner])
        vertex_count = len(shared)
        total_vertices += vertex_count
        assert vertex_count <= 65535 and len(indices) <= 196608
        position = accessor(positions, 3)
        document["accessors"][position]["min"] = [min(positions[axis::3]) for axis in range(3)]
        document["accessors"][position]["max"] = [max(positions[axis::3]) for axis in range(3)]
        attributes = {"POSITION": position, "NORMAL": accessor(normals, 3), "TANGENT": accessor(tangents, 4),
                      "TEXCOORD_0": accessor(uvs, 2)}
        document["materials"].append(material_json(material))
        if stone:
            document['materials'][-1]['pbrMetallicRoughness']['baseColorTexture'] = {'index':texture('Neris-Stone-Grain.png','color')}
            document['materials'][-1]['normalTexture'] = {'index':texture('Neris-Stone-Normal.png','normal'),'scale':.25}
        document["meshes"].append({"name": name, "primitives": [{"attributes": attributes,
            "indices": accessor(indices, 1, 5125), "material": index, "mode": 4}]})
        document["nodes"].append({"mesh": index, "name": name})
        document["scenes"][0]["nodes"].append(index)
    assert total_vertices <= 131072
    document["buffers"] = [{"byteLength": len(binary)}]
    if any("extensions" in material for material in document["materials"]):
        document["extensionsUsed"] = ["KHR_materials_emissive_strength"]
    encoded = json.dumps(document, separators=(",", ":")).encode()
    encoded += b" " * (-len(encoded) % 4)
    header = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(encoded) + 8 + len(binary))
    temporary = path.with_suffix('.glb.tmp')
    temporary.write_bytes(header + struct.pack("<II", len(encoded), 0x4E4F534A) + encoded
                          + struct.pack("<II", len(binary), 0x004E4942) + binary)
    temporary.replace(path)
