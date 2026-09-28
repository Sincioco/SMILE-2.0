// Split the final GLB without a geometry round-trip: preserve exact vertices,
// normals and UVs. Retain only the site's top floor; omit buried faces and moat.
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { externalizeTexture } from './m06-native.mjs';

export function packNative(source, destination) {
  if (fs.existsSync(destination)) throw Error('Use a fresh native export folder.');
  const bytes = fs.readFileSync(source);
  const jsonLength = bytes.readUInt32LE(12);
  const original = JSON.parse(bytes.toString('utf8', 20, 20 + jsonLength));
  let binary = bytes.subarray(28 + jsonLength);
  const entries = [];
  const excluded = [];
  for (const node of original.nodes) {
    if (node.mesh === undefined) continue;
    if (node.children?.length || node.skin !== undefined) throw Error('Expected flat static geometry.');
    if (node.name === 'NC.Export.Site.IvoryStone') {
      // The foundation top closes the interior perimeter strips. Its deep sides
      // and underside are authoring geometry and must never appear below town.
      for (const primitive of original.meshes[node.mesh].primitives) {
        const positions = original.accessors[primitive.attributes.POSITION];
        const pv = original.bufferViews[positions.bufferView];
        const indices = original.accessors[primitive.indices];
        const iv = original.bufferViews[indices.bufferView];
        const size = indices.componentType === 5123 ? 2 : 4;
        const start = (iv.byteOffset || 0) + (indices.byteOffset || 0);
        const positionStart = (pv.byteOffset || 0) + (positions.byteOffset || 0);
        const read = offset => size === 2 ? binary.readUInt16LE(offset) : binary.readUInt32LE(offset);
        const kept = [];
        for (let i = 0; i < indices.count; i += 3) {
          const triangle = [0, 1, 2].map(j => read(start + (i + j) * size));
          if (triangle.every(v => Math.abs(binary.readFloatLE(positionStart + v * (pv.byteStride || 12) + 4)) < 0.0001))
            kept.push(...triangle);
        }
        if (!kept.length) throw Error('Foundation has no usable top floor.');
        // Unreferenced foundation vertices must also go: the native tangent
        // builder correctly rejects vertices with no incident triangle.
        const used = [...new Set(kept)], remap = new Map(used.map((v, i) => [v, i]));
        function appendView(data) {
          const offset = Math.ceil(binary.length / 4) * 4;
          binary = Buffer.concat([binary, Buffer.alloc(offset - binary.length), data]);
          original.bufferViews.push({ buffer: 0, byteOffset: offset, byteLength: data.length });
          return original.bufferViews.length - 1;
        }
        for (const index of Object.values(primitive.attributes)) {
          const a = original.accessors[index], v = original.bufferViews[a.bufferView];
          const components = { SCALAR: 1, VEC2: 2, VEC3: 3, VEC4: 4 }[a.type];
          const componentBytes = { 5120: 1, 5121: 1, 5122: 2, 5123: 2, 5125: 4, 5126: 4 }[a.componentType];
          const width = components * componentBytes;
          if (!width) throw Error('Unsupported foundation attribute.');
          const compact = Buffer.alloc(used.length * width);
          used.forEach((old, i) => binary.copy(compact, i * width,
            (v.byteOffset || 0) + (a.byteOffset || 0) + old * (v.byteStride || width),
            (v.byteOffset || 0) + (a.byteOffset || 0) + old * (v.byteStride || width) + width));
          a.bufferView = appendView(compact); a.byteOffset = 0; a.count = used.length;
          delete a.min; delete a.max;
          if (index === primitive.attributes.POSITION) {
            a.min = [0, 1, 2].map(c => Math.min(...used.map((_, i) => compact.readFloatLE(i * width + c * 4))));
            a.max = [0, 1, 2].map(c => Math.max(...used.map((_, i) => compact.readFloatLE(i * width + c * 4))));
          }
        }
        const compactIndices = Buffer.alloc(kept.length * 2);
        kept.forEach((old, i) => compactIndices.writeUInt16LE(remap.get(old), i * 2));
        indices.bufferView = appendView(compactIndices); indices.byteOffset = 0;
        indices.componentType = 5123; indices.count = kept.length;
        indices.min = [0]; indices.max = [used.length - 1];
      }
      node.name = 'NC.Export.Site.DeckFloor';
      original.meshes[node.mesh].name = node.name;
    }
    if (node.name.startsWith('NC.Export.Site.') && node.name !== 'NC.Export.Site.DeckFloor') {
      excluded.push(node.name);
      continue;
    }
    let vertices = 0, indices = 0;
    for (const primitive of original.meshes[node.mesh].primitives) {
      const count = original.accessors[primitive.attributes.POSITION].count;
      const indexCount = original.accessors[primitive.indices].count;
      if (count > 65535 || indexCount > 196608) throw Error('Primitive exceeds native budget.');
      vertices += count;
      indices += indexCount;
    }
    entries.push({ node, vertices, indices });
  }
  // Largest-first placement avoids wasting model slots in the complete town.
  // Counts come from final GLB accessors, so the exact native ceilings are safe.
  const bins = [];
  for (const entry of entries.sort((a, b) => b.vertices - a.vertices)) {
    let bin = bins.find(b => b.vertices + entry.vertices <= 131072 &&
      b.indices + entry.indices <= 393216 && b.entries.length < 16);
    if (!bin) {
      bin = { entries: [], vertices: 0, indices: 0 };
      bins.push(bin);
    }
    bin.entries.push(entry); bin.vertices += entry.vertices; bin.indices += entry.indices;
  }
  const batches = bins.map(b => b.entries);
  fs.mkdirSync(destination, { recursive: true });
  const manifest = { native_chunks: [], total_static_parts: entries.length,
    excluded, exclusion_reason: 'Town supplies water. Foundation sides/underside omitted; top floor retained.' };
  for (const [index, entries] of batches.entries()) {
    const json = { asset: original.asset, scene: 0, scenes: [{ nodes: [] }],
      nodes: [], meshes: [], accessors: [], bufferViews: [], buffers: [], materials: [] };
    for (const key of ['extensionsUsed', 'extensionsRequired', 'textures', 'samplers', 'images'])
      if (original[key]) json[key] = structuredClone(original[key]);
    const views = new Map(), accessors = new Map(), materials = new Map();
    const slices = [];
    let offset = 0;
    function copyView(index) {
      if (!views.has(index)) {
        const view = original.bufferViews[index];
        if ((view.buffer || 0) !== 0) throw Error('Expected one embedded buffer.');
        const aligned = Math.ceil(offset / 4) * 4;
        if (aligned > offset) slices.push(Buffer.alloc(aligned - offset));
        offset = aligned;
        const start = view.byteOffset || 0;
        const copy = { ...view, buffer: 0, byteOffset: offset };
        views.set(index, json.bufferViews.length);
        json.bufferViews.push(copy);
        slices.push(binary.subarray(start, start + view.byteLength));
        offset += view.byteLength;
      }
      return views.get(index);
    }
    function copyAccessor(index) {
      if (!accessors.has(index)) {
        const accessor = structuredClone(original.accessors[index]);
        if (accessor.sparse) throw Error('Unexpected sparse geometry.');
        accessor.bufferView = copyView(accessor.bufferView);
        accessors.set(index, json.accessors.length);
        json.accessors.push(accessor);
      }
      return accessors.get(index);
    }
    for (const { node } of entries) {
      const mesh = structuredClone(original.meshes[node.mesh]);
      for (const primitive of mesh.primitives) {
        for (const key of Object.keys(primitive.attributes))
          primitive.attributes[key] = copyAccessor(primitive.attributes[key]);
        primitive.indices = copyAccessor(primitive.indices);
        if (!materials.has(primitive.material)) {
          materials.set(primitive.material, json.materials.length);
          json.materials.push(structuredClone(original.materials[primitive.material]));
        }
        primitive.material = materials.get(primitive.material);
      }
      json.scenes[0].nodes.push(json.nodes.length);
      json.nodes.push({ ...node, mesh: json.meshes.length });
      json.meshes.push(mesh);
    }
    for (const image of json.images || [])
      if (image.bufferView !== undefined) image.bufferView = copyView(image.bufferView);
    const aligned = Math.ceil(offset / 4) * 4;
    if (aligned > offset) slices.push(Buffer.alloc(aligned - offset));
    const bin = Buffer.concat(slices);
    json.buffers.push({ byteLength: bin.length });
    const encoded = Buffer.from(JSON.stringify(json));
    const padded = Buffer.alloc(Math.ceil(encoded.length / 4) * 4, 32);
    encoded.copy(padded);
    const header = Buffer.alloc(20);
    header.write('glTF'); header.writeUInt32LE(2, 4);
    header.writeUInt32LE(28 + padded.length + bin.length, 8);
    header.writeUInt32LE(padded.length, 12); header.write('JSON', 16);
    const binHeader = Buffer.alloc(8);
    binHeader.writeUInt32LE(bin.length); binHeader.write('BIN\0', 4);
    const output = Buffer.concat([header, padded, binHeader, bin]);
    const file = `castle-${String(index).padStart(2, '0')}.glb`;
    fs.writeFileSync(path.join(destination, file), output);
    manifest.native_chunks.push({ file, parts: json.meshes.map(m => m.name),
      vertices: entries.reduce((sum, e) => sum + e.vertices, 0),
      triangles: entries.reduce((sum, e) => sum + e.indices / 3, 0),
      sha256: createHash('sha256').update(output).digest('hex') });
  }
  externalizeTexture(destination, manifest);
  return manifest;
}
