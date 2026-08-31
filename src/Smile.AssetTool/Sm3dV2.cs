using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

internal static class Sm3dV2
{
    private const int HeaderSize = 64;
    private const int DirectoryEntrySize = 32;
    private const int MaximumFileBytes = 16 * 1024 * 1024;
    private const int MaximumGltfJsonBytes = 4 * 1024 * 1024;
    private const int MaximumGlbBytes = 64 * 1024 * 1024;
    private const int MaximumBufferBytes = 32 * 1024 * 1024;
    private const int MaximumAggregateBufferBytes = 64 * 1024 * 1024;
    private const int MaximumBuffers = 16;
    private const int MaximumBufferViews = 512;
    private const int MaximumAccessors = 512;
    private const int MaximumScenes = 16;
    private const int MaximumNodes = 4096;
    private const int MaximumMeshes = 256;
    private const int MaximumSourcePrimitives = 4096;
    private const int MaximumImages = 128;
    private const int MaximumNameBytes = 1024;
    private const int MaximumParts = 16;
    private const int MaximumVertices = 131072;
    private const int MaximumIndices = 393216;
    private const int MaximumVerticesPerPart = 65535;
    private const int MaximumIndicesPerPart = 196608;
    private const int MaximumMaterials = 64;
    private const int MaximumTextures = 128;
    private const int MaximumTexturePathBytes = 1024;
    private const int MaximumChunks = 32;
    private const uint NoReference = uint.MaxValue;
    private const uint GlbMagic = 0x46546C67;
    private const uint GlbJson = 0x4E4F534A;
    private const uint GlbBin = 0x004E4942;
    private const uint ChunkOptional = 1;
    private const float BasisTolerance = 0.0001f;

    private static readonly string[] RequiredChunkIds = ["STR0", "PART", "VERT", "INDX", "MATL", "TEXR", "BOND"];
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private sealed record SourceBuffer(byte[] Bytes, int Length);
    private sealed record BufferView(byte[] Buffer, int Offset, int Length, int Stride, int Target);
    private sealed record TextureReference(string Path, uint Semantic);

    private sealed class Material
    {
        public required string Name { get; init; }
        public int BaseColorTexture { get; init; } = -1;
        public int NormalTexture { get; init; } = -1;
        public int OrmTexture { get; init; } = -1;
        public int EmissiveTexture { get; init; } = -1;
        public float[] BaseColor { get; init; } = [1, 1, 1, 1];
        public float Metallic { get; init; } = 1;
        public float Roughness { get; init; } = 1;
        public float NormalStrength { get; init; } = 1;
        public float OcclusionStrength { get; init; } = 1;
        public float[] Emissive { get; init; } = [0, 0, 0];
        public uint AlphaMode { get; init; }
        public float AlphaCutoff { get; init; } = 0.5f;
        public bool DoubleSided { get; init; }
    }

    private sealed class Part
    {
        public required string Name { get; init; }
        public required float[] Vertices { get; init; }
        public required uint[] Indices { get; init; }
        public required uint Material { get; init; }
        public required Vector3 Minimum { get; init; }
        public required Vector3 Maximum { get; init; }
    }

    private sealed class Model
    {
        public required string Name { get; init; }
        public required List<Part> Parts { get; init; }
        public required List<Material> Materials { get; init; }
        public required List<TextureReference> Textures { get; init; }
        public required Vector3 Minimum { get; init; }
        public required Vector3 Maximum { get; init; }
    }

    private sealed class Input : IDisposable
    {
        public required JsonDocument Document { get; init; }
        public required string BaseDirectory { get; init; }
        public byte[]? BinaryChunk { get; init; }

        public void Dispose() => Document.Dispose();
    }

    private sealed record Chunk(string Id, uint Flags, int Offset, int Length, int Count, int Stride);

    private sealed class StringTable
    {
        private readonly Dictionary<string, uint> _offsets = new(StringComparer.Ordinal);
        private readonly MemoryStream _stream = new();

        public StringTable()
        {
            _stream.WriteByte(0);
            _offsets.Add(string.Empty, 0);
        }

        public int Count => _offsets.Count;

        public uint Add(string value)
        {
            if (_offsets.TryGetValue(value, out var existing)) return existing;
            Require(!value.Contains('\0'), "SMA1130: model names and paths may not contain NUL characters.");
            var bytes = StrictUtf8.GetBytes(value);
            var offset = checked((uint)_stream.Length);
            _stream.Write(bytes);
            _stream.WriteByte(0);
            _offsets.Add(value, offset);
            return offset;
        }

        public byte[] Finish()
        {
            return _stream.ToArray();
        }
    }

    public static byte[] Convert(string path)
    {
        try
        {
            using var input = ReadInput(path);
            var model = ReadModel(input);
            return Write(model);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or DecoderFallbackException or FormatException)
        {
            throw new InvalidDataException("SMA1241: glTF contains a malformed value.", error);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException("SMA1292: a glTF source file or external buffer could not be read.", error);
        }
    }

    public static string Inspect(string path)
    {
        var length = new FileInfo(path).Length;
        Require(length is >= 32 and <= MaximumFileBytes,
            "SMA1200: SM3D file size is outside the supported range.");
        var bytes = File.ReadAllBytes(path);
        Require(bytes.Length >= 32 && bytes.Length <= MaximumFileBytes,
            "SMA1200: SM3D file size is outside the supported range.");
        Require(bytes.AsSpan(0, 4).SequenceEqual("SM3D"u8), "SMA1201: SM3D magic is invalid.");
        return Read16(bytes, 4) switch
        {
            1 => InspectV1(bytes),
            2 => InspectV2(bytes),
            _ => throw new InvalidDataException("SMA1202: SM3D version is unsupported.")
        };
    }

    private static Input ReadInput(string path)
    {
        var extension = Path.GetExtension(path);
        if (extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase))
        {
            var length = new FileInfo(path).Length;
            Require(length is >= 1 and <= MaximumGltfJsonBytes,
                $"SMA1242: textual glTF JSON must use 1 to {MaximumGltfJsonBytes} bytes.");
            return new Input
            {
                Document = ParseJson(File.ReadAllBytes(path)),
                BaseDirectory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory()
            };
        }

        Require(extension.Equals(".glb", StringComparison.OrdinalIgnoreCase),
            "SMA1100: SM3D v2 input must be .gltf or .glb.");
        var fileLength = new FileInfo(path).Length;
        Require(fileLength is >= 20 and <= MaximumGlbBytes,
            $"SMA1243: GLB input must use 20 to {MaximumGlbBytes} bytes.");
        var bytes = File.ReadAllBytes(path);
        Require(bytes.Length >= 20, "SMA1101: GLB header is truncated.");
        Require(Read32(bytes, 0) == GlbMagic, "SMA1102: GLB magic is invalid.");
        Require(Read32(bytes, 4) == 2, "SMA1103: only GLB version 2 is supported.");
        Require(Read32(bytes, 8) == bytes.Length, "SMA1104: GLB declared length must equal the file length.");

        byte[]? json = null;
        byte[]? binary = null;
        var offset = 12;
        var chunkIndex = 0;
        while (offset < bytes.Length)
        {
            Require((offset & 3) == 0 && bytes.Length - offset >= 8, "SMA1105: GLB chunk header is invalid or unaligned.");
            var length = checked((int)Read32(bytes, offset));
            var type = Read32(bytes, offset + 4);
            offset += 8;
            Require((length & 3) == 0 && length >= 0 && length <= bytes.Length - offset,
                "SMA1106: GLB chunk length is invalid or unaligned.");
            var content = bytes.AsSpan(offset, length).ToArray();
            if (type == GlbJson)
            {
                Require(chunkIndex == 0 && json == null, "SMA1107: GLB requires exactly one first JSON chunk.");
                Require(length <= MaximumGltfJsonBytes,
                    $"SMA1242: textual glTF JSON must use at most {MaximumGltfJsonBytes} bytes.");
                json = content;
            }
            else if (type == GlbBin)
            {
                Require(json != null && binary == null, "SMA1108: GLB permits at most one BIN chunk after JSON.");
                Require(length <= MaximumBufferBytes + 3,
                    $"SMA1244: an individual source buffer must use at most {MaximumBufferBytes} bytes plus GLB padding.");
                binary = content;
            }
            else
            {
                throw new InvalidDataException("SMA1109: GLB contains an unsupported chunk type.");
            }
            offset += length;
            chunkIndex++;
        }

        Require(offset == bytes.Length && json != null, "SMA1110: GLB chunks do not exactly cover the file.");
        return new Input
        {
            Document = ParseJson(json!),
            BinaryChunk = binary,
            BaseDirectory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory()
        };
    }

    private static JsonDocument ParseJson(byte[] bytes)
    {
        try
        {
            return JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64
            });
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("SMA1111: glTF JSON is invalid.", error);
        }
    }

    private static Model ReadModel(Input input)
    {
        var root = input.Document.RootElement;
        Require(root.ValueKind == JsonValueKind.Object, "SMA1112: glTF root must be an object.");
        Require(Required(Required(root, "asset"), "version").GetString() == "2.0",
            "SMA1113: only glTF 2.0 is supported.");
        ValidateStaticProfile(root);
        var scenes = Required(root, "scenes");
        Require(scenes.ValueKind == JsonValueKind.Array && scenes.GetArrayLength() is >= 1 and <= MaximumScenes,
            $"SMA1114: the SM3D v2 static profile requires 1 to {MaximumScenes} scenes.");
        var activeScene = root.TryGetProperty("scene", out var sceneIndex) ? sceneIndex.GetInt32() : 0;
        Require(activeScene >= 0 && activeScene < scenes.GetArrayLength(), "SMA1115: the active scene index is invalid.");

        var nodes = Required(root, "nodes");
        Require(nodes.ValueKind == JsonValueKind.Array && nodes.GetArrayLength() is >= 1 and <= MaximumNodes,
            $"SMA1245: glTF requires 1 to {MaximumNodes} nodes.");
        var meshes = Required(root, "meshes");
        Require(meshes.ValueKind == JsonValueKind.Array && meshes.GetArrayLength() is >= 1 and <= MaximumMeshes,
            $"SMA1117: glTF requires 1 to {MaximumMeshes} meshes.");

        var buffers = ReadBuffers(root, input.BaseDirectory, input.BinaryChunk);
        var views = ReadViews(root, buffers);
        var accessors = Required(root, "accessors");
        Require(accessors.ValueKind == JsonValueKind.Array && accessors.GetArrayLength() is >= 1 and <= MaximumAccessors,
            $"SMA1116: glTF requires 1 to {MaximumAccessors} accessors.");
        var textures = new List<TextureReference>();
        var materials = ReadMaterials(root, textures);
        var implicitMaterial = -1;
        if (materials.Count == 0)
        {
            materials.Add(new Material { Name = "Default" });
            implicitMaterial = 0;
        }

        var parts = new List<Part>();
        var activeNodes = Required(scenes[activeScene], "nodes");
        Require(activeNodes.ValueKind == JsonValueKind.Array && activeNodes.GetArrayLength() <= MaximumNodes,
            "SMA1246: active scene nodes must be an array within the node limit.");
        var traversalPath = new bool[nodes.GetArrayLength()];
        var instanceOrdinal = 0;
        var sourcePrimitiveCount = 0;

        void TraverseNode(int nodeIndex, Matrix4x4 parentTransform)
        {
            Require(nodeIndex >= 0 && nodeIndex < nodes.GetArrayLength(), "SMA1247: scene or child node index is invalid.");
            Require(!traversalPath[nodeIndex], "SMA1248: node hierarchy contains a cycle.");
            var node = nodes[nodeIndex];
            Require(node.ValueKind == JsonValueKind.Object, "SMA1249: each node must be an object.");
            traversalPath[nodeIndex] = true;
            var worldTransform = ReadNodeTransform(node) * parentTransform;
            var determinant = worldTransform.GetDeterminant();
            var invertible = Matrix4x4.Invert(worldTransform, out var inverse);
            Require(float.IsFinite(determinant) && MathF.Abs(determinant) > 1e-8f && invertible,
                "SMA1250: reachable node transform is singular or non-finite.");
            var inverseTranspose = Matrix4x4.Transpose(inverse);

            if (node.TryGetProperty("mesh", out var meshValue))
            {
                var meshIndex = meshValue.GetInt32();
                Require(meshIndex >= 0 && meshIndex < meshes.GetArrayLength(), "SMA1251: node mesh index is invalid.");
                instanceOrdinal++;
                ReadMeshInstance(node, nodeIndex, meshes[meshIndex], meshIndex, instanceOrdinal, worldTransform,
                    inverseTranspose, determinant, accessors, views, materials, textures, ref implicitMaterial,
                    parts, ref sourcePrimitiveCount);
            }

            if (node.TryGetProperty("children", out var children))
            {
                Require(children.ValueKind == JsonValueKind.Array && children.GetArrayLength() <= MaximumNodes,
                    "SMA1252: node children must be an array within the node limit.");
                foreach (var child in children.EnumerateArray()) TraverseNode(child.GetInt32(), worldTransform);
            }
            traversalPath[nodeIndex] = false;
        }

        foreach (var nodeIndex in activeNodes.EnumerateArray()) TraverseNode(nodeIndex.GetInt32(), Matrix4x4.Identity);

        Require(parts.Count is >= 1 and <= MaximumParts, $"SMA1131: models require 1 to {MaximumParts} parts.");
        Require(parts.Sum(part => part.Vertices.Length / 12) <= MaximumVertices,
            $"SMA1132: models support at most {MaximumVertices} total vertices.");
        Require(parts.Sum(part => part.Indices.Length) <= MaximumIndices,
            $"SMA1133: models support at most {MaximumIndices} total indices.");
        var minimum = new Vector3(parts.Min(part => part.Minimum.X), parts.Min(part => part.Minimum.Y), parts.Min(part => part.Minimum.Z));
        var maximum = new Vector3(parts.Max(part => part.Maximum.X), parts.Max(part => part.Maximum.Y), parts.Max(part => part.Maximum.Z));
        var firstRootIndex = activeNodes.GetArrayLength() > 0 ? activeNodes[0].GetInt32() : -1;
        var fallbackName = firstRootIndex >= 0 && firstRootIndex < nodes.GetArrayLength()
            ? OptionalName(nodes[firstRootIndex], "Model") : "Model";
        var name = OptionalName(scenes[activeScene], fallbackName);
        return new Model
        {
            Name = name,
            Parts = parts,
            Materials = materials,
            Textures = textures,
            Minimum = minimum,
            Maximum = maximum
        };
    }

    private static void ReadMeshInstance(JsonElement node, int nodeIndex, JsonElement mesh, int meshIndex,
        int instanceOrdinal, Matrix4x4 worldTransform, Matrix4x4 inverseTranspose, float determinant,
        JsonElement accessors, IReadOnlyList<BufferView> views, List<Material> materials,
        List<TextureReference> textures, ref int implicitMaterial, List<Part> parts, ref int sourcePrimitiveCount)
    {
        Require(mesh.ValueKind == JsonValueKind.Object, "SMA1253: each mesh must be an object.");
        RejectProperty(mesh, "weights", "mesh weights");
        var meshName = OptionalName(mesh, $"Mesh {meshIndex + 1}");
        var nodeName = OptionalName(node, $"Node {nodeIndex + 1}");
        var primitives = Required(mesh, "primitives");
        Require(primitives.ValueKind == JsonValueKind.Array && primitives.GetArrayLength() > 0,
            "SMA1118: mesh primitives must be a nonempty array.");
        sourcePrimitiveCount = checked(sourcePrimitiveCount + primitives.GetArrayLength());
        Require(sourcePrimitiveCount <= MaximumSourcePrimitives,
            $"SMA1254: reachable scene supports at most {MaximumSourcePrimitives} source primitives.");

        var primitiveIndex = 0;
        foreach (var primitive in primitives.EnumerateArray())
        {
            Require(primitive.ValueKind == JsonValueKind.Object, "SMA1255: each primitive must be an object.");
            RejectProperty(primitive, "targets", "morph targets");
            RejectExtensions(primitive, "primitive");
            Require(!primitive.TryGetProperty("mode", out var mode) || mode.GetInt32() == 4,
                "SMA1119: only indexed triangle primitives are supported.");
            var attributes = Required(primitive, "attributes");
            Require(attributes.ValueKind == JsonValueKind.Object, "SMA1256: primitive attributes must be an object.");
            foreach (var attribute in attributes.EnumerateObject())
                Require(attribute.Name is "POSITION" or "NORMAL" or "TEXCOORD_0" or "TANGENT",
                    $"SMA1257: unsupported primitive attribute '{attribute.Name}' would be lost.");

            var positions = ReadFloatAccessor(accessors, views, RequiredIndex(attributes, "POSITION"), 3,
                "POSITION", MaximumVerticesPerPart);
            var normals = ReadFloatAccessor(accessors, views, RequiredIndex(attributes, "NORMAL"), 3,
                "NORMAL", MaximumVerticesPerPart);
            var uvs = ReadFloatAccessor(accessors, views, RequiredIndex(attributes, "TEXCOORD_0"), 2,
                "TEXCOORD_0", MaximumVerticesPerPart);
            var vertexCount = positions.Length / 3;
            Require(vertexCount == normals.Length / 3 && vertexCount == uvs.Length / 2,
                "SMA1120: vertex attribute counts must match.");
            Require(primitive.TryGetProperty("indices", out var indexValue),
                "SMA1122: indexed triangle primitives are required.");
            var indices = ReadIndexAccessor(accessors, views, indexValue.GetInt32(), MaximumIndicesPerPart);
            Require(indices.Length >= 3 && indices.Length % 3 == 0,
                $"SMA1123: index counts must be triangular and at most {MaximumIndicesPerPart}.");
            Require(indices.All(index => index < vertexCount), "SMA1124: an index is outside its primitive vertex range.");
            if (determinant >= 0)
                for (var index = 0; index < indices.Length; index += 3)
                    (indices[index + 1], indices[index + 2]) = (indices[index + 2], indices[index + 1]);

            var vertices = new float[checked(vertexCount * 12)];
            for (var index = 0; index < vertexCount; index++)
            {
                var sourcePosition = new Vector3(positions[index * 3], positions[index * 3 + 1], positions[index * 3 + 2]);
                var position = ToSmile(Vector3.Transform(sourcePosition, worldTransform));
                var sourceNormal = new Vector3(normals[index * 3], normals[index * 3 + 1], normals[index * 3 + 2]);
                var normal = Normalize(ToSmile(Vector3.TransformNormal(sourceNormal, inverseTranspose)),
                    "SMA1125: NORMAL contains a zero-length or unusable vector.");
                vertices[index * 12] = position.X;
                vertices[index * 12 + 1] = position.Y;
                vertices[index * 12 + 2] = position.Z;
                vertices[index * 12 + 3] = normal.X;
                vertices[index * 12 + 4] = normal.Y;
                vertices[index * 12 + 5] = normal.Z;
                vertices[index * 12 + 10] = uvs[index * 2];
                vertices[index * 12 + 11] = uvs[index * 2 + 1];
            }

            ValidateTriangles(vertices, indices);
            if (attributes.TryGetProperty("TANGENT", out var tangentAccessor))
            {
                var tangents = ReadFloatAccessor(accessors, views, tangentAccessor.GetInt32(), 4,
                    "TANGENT", MaximumVerticesPerPart);
                Require(tangents.Length / 4 == vertexCount, "SMA1126: TANGENT count must match POSITION.");
                for (var index = 0; index < vertexCount; index++)
                {
                    var sourceTangent = new Vector3(tangents[index * 4], tangents[index * 4 + 1], tangents[index * 4 + 2]);
                    var normal = new Vector3(vertices[index * 12 + 3], vertices[index * 12 + 4], vertices[index * 12 + 5]);
                    var tangent = ToSmile(Vector3.TransformNormal(sourceTangent, worldTransform));
                    tangent -= normal * Vector3.Dot(normal, tangent);
                    tangent = Normalize(tangent, "SMA1127: TANGENT is unusable after orthogonalization.");
                    var handedness = tangents[index * 4 + 3];
                    Require(MathF.Abs(MathF.Abs(handedness) - 1) <= BasisTolerance,
                        "SMA1128: TANGENT handedness must be -1 or 1.");
                    vertices[index * 12 + 6] = tangent.X;
                    vertices[index * 12 + 7] = tangent.Y;
                    vertices[index * 12 + 8] = tangent.Z;
                    vertices[index * 12 + 9] = (handedness < 0 ? -1f : 1f) * (determinant < 0 ? 1f : -1f);
                }
            }
            else
            {
                GenerateTangents(vertices, indices);
            }
            ValidateBasis(vertices, "SMA1258: emitted tangent basis is not canonical.");

            uint material;
            if (primitive.TryGetProperty("material", out var materialValue))
            {
                material = materialValue.GetUInt32();
            }
            else
            {
                if (implicitMaterial < 0)
                {
                    Require(materials.Count < MaximumMaterials,
                        "SMA1259: an unassigned primitive requires an implicit material beyond the 64-material limit.");
                    implicitMaterial = materials.Count;
                    materials.Add(new Material { Name = "Default" });
                }
                material = checked((uint)implicitMaterial);
            }
            Require(material < materials.Count, "SMA1129: material reference is outside the material table.");
            Require(parts.Count < MaximumParts, $"SMA1131: models require 1 to {MaximumParts} parts.");
            Require(parts.Sum(part => part.Vertices.Length / 12) <= MaximumVertices - vertexCount,
                $"SMA1132: models support at most {MaximumVertices} total vertices.");
            Require(parts.Sum(part => part.Indices.Length) <= MaximumIndices - indices.Length,
                $"SMA1133: models support at most {MaximumIndices} total indices.");
            var (partMinimum, partMaximum) = Bounds(vertices);
            parts.Add(new Part
            {
                Name = $"{nodeName}/{meshName} [Node {nodeIndex}, Mesh {meshIndex}, Primitive {primitiveIndex}, Instance {instanceOrdinal}]",
                Vertices = vertices,
                Indices = indices,
                Material = material,
                Minimum = partMinimum,
                Maximum = partMaximum
            });
            primitiveIndex++;
        }
    }

    private static void ValidateStaticProfile(JsonElement root)
    {
        RejectProperty(root, "skins", "skins");
        RejectProperty(root, "animations", "animations");
        if (root.TryGetProperty("extensionsRequired", out var required))
        {
            Require(required.ValueKind == JsonValueKind.Array, "SMA1260: extensionsRequired must be an array.");
            foreach (var extension in required.EnumerateArray())
                Require(extension.GetString() == "KHR_materials_emissive_strength",
                    $"SMA1261: required extension '{extension.GetString()}' is unsupported by the static profile.");
        }
        ValidateOptionalCollection(root, "materials", MaximumMaterials);
        ValidateOptionalCollection(root, "textures", MaximumTextures);
        ValidateOptionalCollection(root, "images", MaximumImages);
        ValidateOptionalCollection(root, "samplers", MaximumTextures);
        ValidateOptionalCollection(root, "extensionsUsed", 64);
    }

    private static void ValidateOptionalCollection(JsonElement root, string property, int maximum)
    {
        if (!root.TryGetProperty(property, out var values)) return;
        Require(values.ValueKind == JsonValueKind.Array && values.GetArrayLength() <= maximum,
            $"SMA1288: {property} must be an array of at most {maximum} entries.");
    }

    private static void RejectProperty(JsonElement owner, string property, string feature)
    {
        if (owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(property, out _))
            throw new InvalidDataException($"SMA1262: unsupported {feature} would be lost during static conversion.");
    }

    private static void RejectExtensions(JsonElement owner, string context, string? allowed = null)
    {
        if (!owner.TryGetProperty("extensions", out var extensions)) return;
        Require(extensions.ValueKind == JsonValueKind.Object, $"SMA1263: {context} extensions must be an object.");
        foreach (var extension in extensions.EnumerateObject())
            Require(extension.Name == allowed,
                $"SMA1264: unsupported {context} extension '{extension.Name}' would change static output.");
    }

    private static Matrix4x4 ReadNodeTransform(JsonElement node)
    {
        RejectProperty(node, "skin", "node skinning");
        RejectProperty(node, "weights", "node morph weights");
        RejectExtensions(node, "node");
        var hasMatrix = node.TryGetProperty("matrix", out var matrixValue);
        var hasTrs = node.TryGetProperty("translation", out _) || node.TryGetProperty("rotation", out _) ||
            node.TryGetProperty("scale", out _);
        Require(!hasMatrix || !hasTrs, "SMA1265: a node may not combine matrix and TRS transforms.");
        if (hasMatrix)
        {
            var values = ReadTransformValues(matrixValue, 16, "node matrix");
            var result = new Matrix4x4(
                values[0], values[1], values[2], values[3],
                values[4], values[5], values[6], values[7],
                values[8], values[9], values[10], values[11],
                values[12], values[13], values[14], values[15]);
            Require(MathF.Abs(result.M14) <= 1e-6f && MathF.Abs(result.M24) <= 1e-6f &&
                MathF.Abs(result.M34) <= 1e-6f && MathF.Abs(result.M44 - 1) <= 1e-6f,
                "SMA1266: node matrix must be a finite affine transform.");
            return result;
        }

        var translation = node.TryGetProperty("translation", out var translationValue)
            ? ReadTransformValues(translationValue, 3, "node translation") : [0f, 0f, 0f];
        var scale = node.TryGetProperty("scale", out var scaleValue)
            ? ReadTransformValues(scaleValue, 3, "node scale") : [1f, 1f, 1f];
        var rotation = node.TryGetProperty("rotation", out var rotationValue)
            ? ReadTransformValues(rotationValue, 4, "node rotation") : [0f, 0f, 0f, 1f];
        var quaternion = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        Require(quaternion.LengthSquared() > 1e-12f, "SMA1267: node rotation quaternion is zero or unusable.");
        quaternion = Quaternion.Normalize(quaternion);
        return Matrix4x4.CreateScale(scale[0], scale[1], scale[2]) *
            Matrix4x4.CreateFromQuaternion(quaternion) *
            Matrix4x4.CreateTranslation(translation[0], translation[1], translation[2]);
    }

    private static float[] ReadTransformValues(JsonElement value, int count, string name)
    {
        Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == count,
            $"SMA1268: {name} must contain exactly {count} numbers.");
        var result = new float[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = value[index].GetSingle();
            Require(float.IsFinite(result[index]), $"SMA1269: {name} contains a non-finite value.");
        }
        return result;
    }

    private static Vector3 ToSmile(Vector3 value)
    {
        Require(Finite(value), "SMA1270: transformed geometry contains a non-finite value.");
        return new Vector3(value.X, value.Y, -value.Z);
    }

    private static List<SourceBuffer> ReadBuffers(JsonElement root, string baseDirectory, byte[]? binaryChunk)
    {
        var values = Required(root, "buffers");
        Require(values.ValueKind == JsonValueKind.Array && values.GetArrayLength() is >= 1 and <= MaximumBuffers,
            $"SMA1134: glTF requires 1 to {MaximumBuffers} buffers.");
        var result = new List<SourceBuffer>();
        var usedBinary = false;
        var bufferIndex = 0;
        var aggregateBytes = 0;
        foreach (var value in values.EnumerateArray())
        {
            Require(value.ValueKind == JsonValueKind.Object, "SMA1271: each buffer must be an object.");
            var declared = Required(value, "byteLength").GetInt32();
            Require(declared >= 0 && declared <= MaximumBufferBytes,
                $"SMA1244: an individual source buffer must use at most {MaximumBufferBytes} bytes.");
            aggregateBytes = checked(aggregateBytes + declared);
            Require(aggregateBytes <= MaximumAggregateBufferBytes,
                $"SMA1272: aggregate declared source buffers exceed {MaximumAggregateBufferBytes} bytes.");
            byte[] bytes;
            var isBinary = false;
            if (!value.TryGetProperty("uri", out var uriValue))
            {
                Require(binaryChunk != null && !usedBinary && bufferIndex == 0,
                    "SMA1135: only GLB buffer zero may omit its URI.");
                bytes = binaryChunk!;
                usedBinary = true;
                isBinary = true;
            }
            else
            {
                var uri = uriValue.GetString() ?? string.Empty;
                const string prefix = "data:application/octet-stream;base64,";
                if (uri.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var encoded = uri[prefix.Length..];
                    Require(encoded.Length <= checked((MaximumBufferBytes + 2) / 3 * 4),
                        "SMA1273: Base64 source buffer exceeds the encoded-size limit.");
                    bytes = System.Convert.FromBase64String(encoded);
                }
                else
                {
                    Require(!Uri.TryCreate(uri, UriKind.Absolute, out _), "SMA1136: external buffer URI must be relative.");
                    var candidate = Path.GetFullPath(Path.Combine(baseDirectory, uri.Replace('/', Path.DirectorySeparatorChar)));
                    var rootPath = Path.GetFullPath(baseDirectory) + Path.DirectorySeparatorChar;
                    Require(candidate.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase),
                        "SMA1137: external buffer escapes the model directory.");
                    var externalLength = new FileInfo(candidate).Length;
                    Require(externalLength <= MaximumBufferBytes,
                        $"SMA1244: an individual source buffer must use at most {MaximumBufferBytes} bytes.");
                    bytes = File.ReadAllBytes(candidate);
                }
            }

            Require(declared >= 0 && bytes.Length >= declared && (!isBinary || bytes.Length - declared <= 3),
                "SMA1138: buffer length does not match its declared size.");
            result.Add(new SourceBuffer(bytes.AsSpan(0, declared).ToArray(), declared));
            bufferIndex++;
        }
        Require(binaryChunk == null || usedBinary, "SMA1139: GLB BIN chunk is not referenced by buffer zero.");
        return result;
    }

    private static List<BufferView> ReadViews(JsonElement root, IReadOnlyList<SourceBuffer> buffers)
    {
        var values = Required(root, "bufferViews");
        Require(values.ValueKind == JsonValueKind.Array && values.GetArrayLength() <= MaximumBufferViews,
            $"SMA1140: bufferViews must be an array of at most {MaximumBufferViews} entries.");
        var result = new List<BufferView>();
        foreach (var value in values.EnumerateArray())
        {
            var bufferIndex = Required(value, "buffer").GetInt32();
            Require(bufferIndex >= 0 && bufferIndex < buffers.Count, "SMA1141: bufferView references an invalid buffer.");
            var offset = value.TryGetProperty("byteOffset", out var offsetValue) ? offsetValue.GetInt32() : 0;
            var length = Required(value, "byteLength").GetInt32();
            var stride = value.TryGetProperty("byteStride", out var strideValue) ? strideValue.GetInt32() : 0;
            var target = value.TryGetProperty("target", out var targetValue) ? targetValue.GetInt32() : 0;
            Require(offset >= 0 && length >= 0 && offset <= buffers[bufferIndex].Length &&
                length <= buffers[bufferIndex].Length - offset, "SMA1142: bufferView range exceeds the declared buffer length.");
            Require(stride == 0 || (stride is >= 4 and <= 252 && stride % 4 == 0),
                "SMA1143: bufferView stride must be zero or an aligned value from 4 through 252.");
            Require(target is 0 or 34962 or 34963, "SMA1274: bufferView target is unsupported.");
            Require(stride == 0 || target != 34963, "SMA1275: index bufferViews may not declare byteStride.");
            result.Add(new BufferView(buffers[bufferIndex].Bytes, offset, length, stride, target));
        }
        return result;
    }

    private static float[] ReadFloatAccessor(JsonElement accessors, IReadOnlyList<BufferView> views,
        int accessorIndex, int components, string semantic, int maximumCount)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        Require(count <= maximumCount, $"SMA1276: {semantic} accessor count exceeds {maximumCount}.");
        var type = components switch { 2 => "VEC2", 3 => "VEC3", 4 => "VEC4", _ => string.Empty };
        var normalized = accessor.TryGetProperty("normalized", out var normalizedValue) && normalizedValue.ValueKind == JsonValueKind.True;
        Require(Required(accessor, "componentType").GetInt32() == 5126 &&
            Required(accessor, "type").GetString() == type && !normalized,
            $"SMA1144: {semantic} must use non-normalized float {type} data.");
        var stride = view.Stride == 0 ? components * 4 : view.Stride;
        Require((view.Offset + offset) % 4 == 0 && stride >= components * 4 &&
            view.Target is 0 or 34962 &&
            offset + (long)Math.Max(0, count - 1) * stride + components * 4 <= view.Length,
            $"SMA1145: {semantic} accessor range or stride is invalid.");
        var valueCount = checked(count * components);
        Require(checked((long)valueCount * sizeof(float)) <= MaximumFileBytes,
            $"SMA1277: {semantic} accessor allocation exceeds the model-memory limit.");
        var result = new float[valueCount];
        for (var index = 0; index < count; index++)
        {
            for (var component = 0; component < components; component++)
            {
                var bits = BinaryPrimitives.ReadInt32LittleEndian(view.Buffer.AsSpan(
                    view.Offset + offset + index * stride + component * 4, 4));
                var number = BitConverter.Int32BitsToSingle(bits);
                Require(float.IsFinite(number), $"SMA1146: {semantic} contains a non-finite value.");
                result[index * components + component] = number;
            }
        }
        return result;
    }

    private static uint[] ReadIndexAccessor(JsonElement accessors, IReadOnlyList<BufferView> views,
        int accessorIndex, int maximumCount)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        Require(count <= maximumCount, $"SMA1278: index accessor count exceeds {maximumCount}.");
        var componentType = Required(accessor, "componentType").GetInt32();
        var normalized = accessor.TryGetProperty("normalized", out var normalizedValue) && normalizedValue.ValueKind == JsonValueKind.True;
        Require(Required(accessor, "type").GetString() == "SCALAR" && componentType is 5121 or 5123 or 5125 && !normalized,
            "SMA1147: indices must be unsigned byte, unsigned short, or unsigned int scalars.");
        var size = componentType == 5121 ? 1 : componentType == 5123 ? 2 : 4;
        var stride = size;
        Require(view.Stride == 0 && view.Target is 0 or 34963 && (view.Offset + offset) % size == 0 &&
            offset + (long)Math.Max(0, count - 1) * stride + size <= view.Length,
            "SMA1148: index accessor range or stride is invalid.");
        var result = new uint[count];
        for (var index = 0; index < count; index++)
        {
            var source = view.Buffer.AsSpan(view.Offset + offset + index * stride, size);
            result[index] = componentType == 5121 ? source[0] : componentType == 5123
                ? BinaryPrimitives.ReadUInt16LittleEndian(source) : BinaryPrimitives.ReadUInt32LittleEndian(source);
        }
        return result;
    }

    private static JsonElement Accessor(JsonElement accessors, int index, IReadOnlyList<BufferView> views,
        out BufferView view, out int offset, out int count)
    {
        Require(index >= 0 && index < accessors.GetArrayLength(), "SMA1149: accessor index is invalid.");
        var accessor = accessors[index];
        Require(!accessor.TryGetProperty("sparse", out _), "SMA1150: sparse accessors are not supported.");
        var viewIndex = Required(accessor, "bufferView").GetInt32();
        Require(viewIndex >= 0 && viewIndex < views.Count, "SMA1151: accessor bufferView is invalid.");
        view = views[viewIndex];
        offset = accessor.TryGetProperty("byteOffset", out var offsetValue) ? offsetValue.GetInt32() : 0;
        count = Required(accessor, "count").GetInt32();
        Require(offset >= 0 && count > 0, "SMA1152: accessor offset/count is invalid.");
        return accessor;
    }

    private static List<Material> ReadMaterials(JsonElement root, List<TextureReference> textures)
    {
        if (!root.TryGetProperty("materials", out var values))
            return [];
        Require(values.ValueKind == JsonValueKind.Array && values.GetArrayLength() <= MaximumMaterials,
            $"SMA1153: models support at most {MaximumMaterials} declared materials.");
        var result = new List<Material>();
        var materialIndex = 0;
        foreach (var value in values.EnumerateArray())
        {
            Require(value.ValueKind == JsonValueKind.Object, "SMA1279: each material must be an object.");
            RejectExtensions(value, "material", "KHR_materials_emissive_strength");
            var pbr = value.TryGetProperty("pbrMetallicRoughness", out var pbrValue) ? pbrValue : default;
            if (pbr.ValueKind != JsonValueKind.Undefined)
                Require(pbr.ValueKind == JsonValueKind.Object, "SMA1280: pbrMetallicRoughness must be an object.");
            var baseColor = pbr.ValueKind == JsonValueKind.Object && pbr.TryGetProperty("baseColorFactor", out var baseValue)
                ? ReadFactor(baseValue, 4, 0, 1, "baseColorFactor") : new float[] { 1, 1, 1, 1 };
            var metallic = OptionalNumber(pbr, "metallicFactor", 1, 0, 1);
            var roughness = OptionalNumber(pbr, "roughnessFactor", 1, 0, 1);
            var baseTexture = ReadTexture(root, pbr, "baseColorTexture", 1, textures);
            var normalTexture = ReadTexture(root, value, "normalTexture", 2, textures);

            var metallicRoughnessPath = ReadTexturePath(root, pbr, "metallicRoughnessTexture");
            var occlusionPath = ReadTexturePath(root, value, "occlusionTexture");
            Require(metallicRoughnessPath == null || occlusionPath == null ||
                string.Equals(metallicRoughnessPath, occlusionPath, StringComparison.Ordinal),
                "SMA1154: metallic-roughness and occlusion must use one packed ORM texture path.");
            var ormPath = metallicRoughnessPath ?? occlusionPath;
            var ormTexture = ormPath == null ? -1 : AddTexture(textures, ormPath, 3);
            var emissiveTexture = ReadTexture(root, value, "emissiveTexture", 4, textures);
            var normalStrength = OptionalTextureNumber(value, "normalTexture", "scale", 1, 0, 8);
            var occlusionStrength = OptionalTextureNumber(value, "occlusionTexture", "strength", 1, 0, 1);
            var emissive = value.TryGetProperty("emissiveFactor", out var emissiveValue)
                ? ReadFactor(emissiveValue, 3, 0, 1, "emissiveFactor") : new float[] { 0, 0, 0 };
            var emissiveStrength = 1f;
            if (value.TryGetProperty("extensions", out var extensions) &&
                extensions.TryGetProperty("KHR_materials_emissive_strength", out var emissiveExtension))
            {
                Require(emissiveExtension.ValueKind == JsonValueKind.Object,
                    "SMA1281: KHR_materials_emissive_strength must be an object.");
                emissiveStrength = OptionalNumber(emissiveExtension, "emissiveStrength", 1, 0, 64);
            }
            for (var component = 0; component < emissive.Length; component++) emissive[component] *= emissiveStrength;

            var alphaText = value.TryGetProperty("alphaMode", out var alphaValue) ? alphaValue.GetString() : "OPAQUE";
            var alphaMode = alphaText switch
            {
                "OPAQUE" or null => 0U,
                "MASK" => 1U,
                "BLEND" => 2U,
                _ => throw new InvalidDataException("SMA1155: alphaMode must be OPAQUE, MASK, or BLEND.")
            };
            var alphaCutoff = OptionalNumber(value, "alphaCutoff", 0.5f, 0, 1);
            var doubleSided = value.TryGetProperty("doubleSided", out var sideValue) && sideValue.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new InvalidDataException("SMA1156: doubleSided must be Boolean.")
            };
            result.Add(new Material
            {
                Name = OptionalName(value, $"Material {materialIndex + 1}"),
                BaseColorTexture = baseTexture,
                NormalTexture = normalTexture,
                OrmTexture = ormTexture,
                EmissiveTexture = emissiveTexture,
                BaseColor = baseColor,
                Metallic = metallic,
                Roughness = roughness,
                NormalStrength = normalStrength,
                OcclusionStrength = occlusionStrength,
                Emissive = emissive,
                AlphaMode = alphaMode,
                AlphaCutoff = alphaCutoff,
                DoubleSided = doubleSided
            });
            materialIndex++;
        }
        return result;
    }

    private static int ReadTexture(JsonElement root, JsonElement owner, string property, uint semantic,
        List<TextureReference> textures)
    {
        var path = ReadTexturePath(root, owner, property);
        return path == null ? -1 : AddTexture(textures, path, semantic);
    }

    private static string? ReadTexturePath(JsonElement root, JsonElement owner, string property)
    {
        if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(property, out var info)) return null;
        Require(info.ValueKind == JsonValueKind.Object, $"SMA1157: {property} must be an object.");
        if (info.TryGetProperty("texCoord", out var texCoord))
            Require(texCoord.GetInt32() == 0, $"SMA1282: {property} requires unsupported texture coordinate set {texCoord.GetInt32()}.");
        RejectExtensions(info, property);
        var textureIndex = Required(info, "index").GetInt32();
        var textureValues = Required(root, "textures");
        Require(textureValues.ValueKind == JsonValueKind.Array && textureValues.GetArrayLength() <= MaximumTextures && textureIndex >= 0 &&
            textureIndex < textureValues.GetArrayLength(), $"SMA1158: {property} texture index is invalid.");
        var texture = textureValues[textureIndex];
        Require(texture.ValueKind == JsonValueKind.Object, "SMA1283: each texture must be an object.");
        RejectExtensions(texture, "texture");
        ValidateSampler(root, texture, property);
        var imageIndex = Required(texture, "source").GetInt32();
        var images = Required(root, "images");
        Require(images.ValueKind == JsonValueKind.Array && images.GetArrayLength() <= MaximumImages &&
            imageIndex >= 0 && imageIndex < images.GetArrayLength(),
            $"SMA1159: {property} image index is invalid.");
        Require(images[imageIndex].ValueKind == JsonValueKind.Object, "SMA1284: each image must be an object.");
        Require(images[imageIndex].TryGetProperty("uri", out var uriValue),
            "SMA1160: embedded image bytes are not supported in SM3D v2 M1.");
        return ValidateTexturePath(uriValue.GetString() ?? string.Empty);
    }

    private static void ValidateSampler(JsonElement root, JsonElement texture, string property)
    {
        if (!texture.TryGetProperty("sampler", out var samplerValue)) return;
        var samplerIndex = samplerValue.GetInt32();
        var samplers = Required(root, "samplers");
        Require(samplers.ValueKind == JsonValueKind.Array && samplers.GetArrayLength() <= MaximumTextures &&
            samplerIndex >= 0 && samplerIndex < samplers.GetArrayLength(),
            $"SMA1285: {property} sampler index is invalid.");
        var sampler = samplers[samplerIndex];
        Require(sampler.ValueKind == JsonValueKind.Object, "SMA1286: each sampler must be an object.");
        var magFilter = sampler.TryGetProperty("magFilter", out var magValue) ? magValue.GetInt32() : 9729;
        var minFilter = sampler.TryGetProperty("minFilter", out var minValue) ? minValue.GetInt32() : 9987;
        var wrapS = sampler.TryGetProperty("wrapS", out var wrapSValue) ? wrapSValue.GetInt32() : 10497;
        var wrapT = sampler.TryGetProperty("wrapT", out var wrapTValue) ? wrapTValue.GetInt32() : 10497;
        Require(magFilter == 9729 && minFilter == 9987 && wrapS == 10497 && wrapT == 10497,
            $"SMA1287: {property} sampler is not representable by the imported repeat/trilinear policy.");
        RejectExtensions(sampler, "sampler");
    }

    private static int AddTexture(List<TextureReference> textures, string path, uint semantic)
    {
        var existing = textures.FindIndex(value => value.Semantic == semantic &&
            string.Equals(value.Path, path, StringComparison.Ordinal));
        if (existing >= 0) return existing;
        Require(textures.Count < MaximumTextures, $"SMA1161: models support at most {MaximumTextures} texture references.");
        textures.Add(new TextureReference(path, semantic));
        return textures.Count - 1;
    }

    private static string ValidateTexturePath(string path)
    {
        Require(path.Length > 0 && StrictUtf8.GetByteCount(path) <= MaximumTexturePathBytes,
            $"SMA1162: texture paths must use 1 to {MaximumTexturePathBytes} UTF-8 bytes.");
        Require(!Path.IsPathRooted(path) && !path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal) &&
            !path.Contains('\\') && !path.Contains(':') && !Uri.TryCreate(path, UriKind.Absolute, out _),
            "SMA1163: texture paths must be forward-slash project-relative paths.");
        Require(path.IndexOfAny(['*', '?', '[', ']', '{', '}', '!', ';', '"', '<', '>', '|']) < 0,
            "SMA1164: texture path contains an unsupported character.");
        foreach (var character in path)
            Require(!char.IsControl(character), "SMA1165: texture paths may not contain control characters.");
        foreach (var segment in path.Split('/'))
            Require(segment.Length > 0 && segment != "." && segment != "..",
                "SMA1166: texture paths may not contain empty, current, or parent segments.");
        return path;
    }

    private static float OptionalTextureNumber(JsonElement owner, string textureProperty, string numberProperty,
        float defaultValue, float minimum, float maximum)
    {
        if (!owner.TryGetProperty(textureProperty, out var texture)) return defaultValue;
        return OptionalNumber(texture, numberProperty, defaultValue, minimum, maximum);
    }

    private static float OptionalNumber(JsonElement owner, string property, float defaultValue, float minimum, float maximum)
    {
        if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(property, out var value)) return defaultValue;
        var result = value.GetSingle();
        Require(float.IsFinite(result) && result >= minimum && result <= maximum,
            $"SMA1167: {property} must be finite and within {minimum.ToString(CultureInfo.InvariantCulture)} through {maximum.ToString(CultureInfo.InvariantCulture)}.");
        return result;
    }

    private static float[] ReadFactor(JsonElement value, int count, float minimum, float maximum, string name)
    {
        Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == count,
            $"SMA1168: {name} must contain exactly {count} numbers.");
        var result = new float[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = value[index].GetSingle();
            Require(float.IsFinite(result[index]) && result[index] >= minimum && result[index] <= maximum,
                $"SMA1169: {name} contains an invalid value.");
        }
        return result;
    }

    private static void ValidateTriangles(float[] vertices, uint[] indices)
    {
        for (var index = 0; index < indices.Length; index += 3)
        {
            var a = Position(vertices, indices[index]);
            var b = Position(vertices, indices[index + 1]);
            var c = Position(vertices, indices[index + 2]);
            var area = Vector3.Cross(b - a, c - a).LengthSquared();
            Require(float.IsFinite(area) && area > 1e-12f, "SMA1170: geometry contains a degenerate triangle.");
        }
    }

    private static void GenerateTangents(float[] vertices, uint[] indices)
    {
        var vertexCount = vertices.Length / 12;
        var tangent = new Vector3[vertexCount];
        var bitangent = new Vector3[vertexCount];
        for (var index = 0; index < indices.Length; index += 3)
        {
            var ia = checked((int)indices[index]);
            var ib = checked((int)indices[index + 1]);
            var ic = checked((int)indices[index + 2]);
            var a = Position(vertices, (uint)ia);
            var b = Position(vertices, (uint)ib);
            var c = Position(vertices, (uint)ic);
            var uva = Uv(vertices, ia);
            var uvb = Uv(vertices, ib);
            var uvc = Uv(vertices, ic);
            var edge1 = b - a;
            var edge2 = c - a;
            var duv1 = uvb - uva;
            var duv2 = uvc - uva;
            var determinant = duv1.X * duv2.Y - duv1.Y * duv2.X;
            Require(float.IsFinite(determinant) && MathF.Abs(determinant) > 1e-12f,
                "SMA1171: tangent generation requires nondegenerate UV derivatives.");
            var reciprocal = 1.0f / determinant;
            var direction = (edge1 * duv2.Y - edge2 * duv1.Y) * reciprocal;
            var other = (edge2 * duv1.X - edge1 * duv2.X) * reciprocal;
            Require(Finite(direction) && Finite(other), "SMA1172: tangent generation produced a non-finite value.");
            tangent[ia] += direction; tangent[ib] += direction; tangent[ic] += direction;
            bitangent[ia] += other; bitangent[ib] += other; bitangent[ic] += other;
        }

        for (var index = 0; index < vertexCount; index++)
        {
            var normal = new Vector3(vertices[index * 12 + 3], vertices[index * 12 + 4], vertices[index * 12 + 5]);
            var direction = tangent[index] - normal * Vector3.Dot(normal, tangent[index]);
            direction = Normalize(direction, "SMA1173: tangent generation produced a zero-length tangent.");
            var handedness = Vector3.Dot(Vector3.Cross(normal, direction), bitangent[index]) < 0 ? -1f : 1f;
            vertices[index * 12 + 6] = direction.X;
            vertices[index * 12 + 7] = direction.Y;
            vertices[index * 12 + 8] = direction.Z;
            vertices[index * 12 + 9] = handedness;
        }
    }

    private static void ValidateBasis(float[] vertices, string error)
    {
        for (var index = 0; index < vertices.Length / 12; index++)
        {
            var normal = new Vector3(vertices[index * 12 + 3], vertices[index * 12 + 4], vertices[index * 12 + 5]);
            var tangent = new Vector3(vertices[index * 12 + 6], vertices[index * 12 + 7], vertices[index * 12 + 8]);
            var handedness = vertices[index * 12 + 9];
            Require(Finite(normal) && Finite(tangent) &&
                MathF.Abs(normal.LengthSquared() - 1) <= BasisTolerance &&
                MathF.Abs(tangent.LengthSquared() - 1) <= BasisTolerance &&
                MathF.Abs(Vector3.Dot(normal, tangent)) <= BasisTolerance &&
                MathF.Abs(MathF.Abs(handedness) - 1) <= BasisTolerance, error);
        }
    }

    private static Vector3 Position(float[] vertices, uint index) => new(
        vertices[checked((int)index) * 12], vertices[checked((int)index) * 12 + 1], vertices[checked((int)index) * 12 + 2]);

    private static Vector2 Uv(float[] vertices, int index) => new(vertices[index * 12 + 10], vertices[index * 12 + 11]);

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static Vector3 Normalize(Vector3 value, string error)
    {
        Require(Finite(value) && value.LengthSquared() > 1e-12f, error);
        return Vector3.Normalize(value);
    }

    private static (Vector3 Minimum, Vector3 Maximum) Bounds(float[] vertices)
    {
        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        for (var index = 0; index < vertices.Length / 12; index++)
        {
            var value = Position(vertices, (uint)index);
            minimum = Vector3.Min(minimum, value);
            maximum = Vector3.Max(maximum, value);
        }
        return (minimum, maximum);
    }

    private static byte[] Write(Model model)
    {
        var strings = new StringTable();
        var modelName = strings.Add(model.Name);
        var partNames = model.Parts.Select(part => strings.Add(part.Name)).ToArray();
        var materialNames = model.Materials.Select(material => strings.Add(material.Name)).ToArray();
        var texturePaths = model.Textures.Select(texture => strings.Add(texture.Path)).ToArray();
        var stringBytes = strings.Finish();
        var partBytes = new byte[model.Parts.Count * 32];
        var vertexCount = model.Parts.Sum(part => part.Vertices.Length / 12);
        var indexCount = model.Parts.Sum(part => part.Indices.Length);
        var vertexBytes = new byte[vertexCount * 48];
        var indexBytes = new byte[indexCount * 4];
        var materialBytes = new byte[model.Materials.Count * 80];
        var textureBytes = new byte[model.Textures.Count * 16];
        var boundsBytes = new byte[(model.Parts.Count + 1) * 32];

        var firstVertex = 0;
        var firstIndex = 0;
        for (var partIndex = 0; partIndex < model.Parts.Count; partIndex++)
        {
            var part = model.Parts[partIndex];
            var partOffset = partIndex * 32;
            Write32(partBytes, partOffset, partNames[partIndex]);
            Write32(partBytes, partOffset + 4, (uint)firstVertex);
            Write32(partBytes, partOffset + 8, (uint)(part.Vertices.Length / 12));
            Write32(partBytes, partOffset + 12, (uint)firstIndex);
            Write32(partBytes, partOffset + 16, (uint)part.Indices.Length);
            Write32(partBytes, partOffset + 20, part.Material);
            Write32(partBytes, partOffset + 24, (uint)(partIndex + 1));
            for (var vertex = 0; vertex < part.Vertices.Length / 12; vertex++)
            {
                var target = (firstVertex + vertex) * 48;
                for (var field = 0; field < 12; field++) WriteFloat(vertexBytes, target + field * 4, part.Vertices[vertex * 12 + field]);
            }
            for (var index = 0; index < part.Indices.Length; index++)
                Write32(indexBytes, (firstIndex + index) * 4, part.Indices[index]);
            WriteBounds(boundsBytes, (partIndex + 1) * 32, part.Minimum, part.Maximum);
            firstVertex += part.Vertices.Length / 12;
            firstIndex += part.Indices.Length;
        }
        WriteBounds(boundsBytes, 0, model.Minimum, model.Maximum);

        for (var index = 0; index < model.Materials.Count; index++)
        {
            var material = model.Materials[index];
            var offset = index * 80;
            Write32(materialBytes, offset, materialNames[index]);
            Write32(materialBytes, offset + 4, Reference(material.BaseColorTexture));
            Write32(materialBytes, offset + 8, Reference(material.NormalTexture));
            Write32(materialBytes, offset + 12, Reference(material.OrmTexture));
            Write32(materialBytes, offset + 16, Reference(material.EmissiveTexture));
            Write32(materialBytes, offset + 20, material.AlphaMode);
            Write32(materialBytes, offset + 24, material.DoubleSided ? 1U : 0U);
            for (var component = 0; component < 4; component++) WriteFloat(materialBytes, offset + 32 + component * 4, material.BaseColor[component]);
            WriteFloat(materialBytes, offset + 48, material.Metallic);
            WriteFloat(materialBytes, offset + 52, material.Roughness);
            WriteFloat(materialBytes, offset + 56, material.NormalStrength);
            WriteFloat(materialBytes, offset + 60, material.OcclusionStrength);
            for (var component = 0; component < 3; component++) WriteFloat(materialBytes, offset + 64 + component * 4, material.Emissive[component]);
            WriteFloat(materialBytes, offset + 76, material.AlphaCutoff);
        }

        for (var index = 0; index < model.Textures.Count; index++)
        {
            var offset = index * 16;
            Write32(textureBytes, offset, texturePaths[index]);
            Write32(textureBytes, offset + 4, model.Textures[index].Semantic);
        }

        var chunkData = new[] { stringBytes, partBytes, vertexBytes, indexBytes, materialBytes, textureBytes, boundsBytes };
        var chunkCounts = new[] { strings.Count, model.Parts.Count, vertexCount, indexCount, model.Materials.Count, model.Textures.Count, model.Parts.Count + 1 };
        var chunkStrides = new[] { 0, 32, 48, 4, 80, 16, 32 };
        var directoryBytes = checked(RequiredChunkIds.Length * DirectoryEntrySize);
        var dataOffset = Align4(HeaderSize + directoryBytes);
        var fileSize = dataOffset + chunkData.Sum(value => Align4(value.Length));
        Require(fileSize <= MaximumFileBytes, "SMA1174: converted SM3D v2 model exceeds the 16 MiB limit.");
        var output = new byte[fileSize];
        "SM3D"u8.CopyTo(output);
        Write16(output, 4, 2);
        Write16(output, 6, HeaderSize);
        Write32(output, 12, (uint)fileSize);
        Write32(output, 20, (uint)RequiredChunkIds.Length);
        Write32(output, 24, HeaderSize);
        Write32(output, 28, DirectoryEntrySize);
        Write32(output, 32, modelName);
        Write32(output, 36, (uint)model.Parts.Count);
        Write32(output, 40, (uint)vertexCount);
        Write32(output, 44, (uint)indexCount);
        Write32(output, 48, (uint)model.Materials.Count);
        Write32(output, 52, (uint)model.Textures.Count);

        var current = dataOffset;
        for (var index = 0; index < RequiredChunkIds.Length; index++)
        {
            var directory = HeaderSize + index * DirectoryEntrySize;
            Encoding.ASCII.GetBytes(RequiredChunkIds[index]).CopyTo(output, directory);
            Write32(output, directory + 8, (uint)current);
            Write32(output, directory + 12, (uint)chunkData[index].Length);
            Write32(output, directory + 16, (uint)chunkCounts[index]);
            Write32(output, directory + 20, (uint)chunkStrides[index]);
            chunkData[index].CopyTo(output, current);
            current += Align4(chunkData[index].Length);
        }
        Write32(output, 16, Checksum(output.AsSpan(HeaderSize)));
        return output;
    }

    private static uint Reference(int value) => value < 0 ? NoReference : checked((uint)value);

    private static void WriteBounds(Span<byte> output, int offset, Vector3 minimum, Vector3 maximum)
    {
        WriteFloat(output, offset, minimum.X); WriteFloat(output, offset + 4, minimum.Y); WriteFloat(output, offset + 8, minimum.Z);
        WriteFloat(output, offset + 12, maximum.X); WriteFloat(output, offset + 16, maximum.Y); WriteFloat(output, offset + 20, maximum.Z);
    }

    private static string InspectV1(byte[] bytes)
    {
        Require(Read16(bytes, 6) == 32, "SMA1203: SM3D v1 header size is invalid.");
        var parts = checked((int)Read32(bytes, 8));
        var vertices = checked((int)Read32(bytes, 12));
        var indices = checked((int)Read32(bytes, 16));
        var materials = checked((int)Read32(bytes, 20));
        Require(parts is >= 1 and <= MaximumParts && vertices > 0 && indices > 0 && materials is >= 1 and <= MaximumMaterials,
            "SMA1204: SM3D v1 summary counts are invalid.");
        var expected = checked(32 + parts * 24 + vertices * 32 + indices * 4);
        Require(expected == bytes.Length && Read32(bytes, 24) == bytes.Length,
            "SMA1205: SM3D v1 exact size is invalid.");
        Require(Read32(bytes, 28) == Checksum(bytes.AsSpan(32)), "SMA1206: SM3D v1 checksum is invalid.");
        var partTableBytes = checked(parts * 24);
        var vertexBytes = checked(vertices * 32);
        for (var partIndex = 0; partIndex < parts; partIndex++)
        {
            var partOffset = 32 + partIndex * 24;
            var firstVertex = checked((int)Read32(bytes, partOffset));
            var partVertices = checked((int)Read32(bytes, partOffset + 4));
            var firstIndex = checked((int)Read32(bytes, partOffset + 8));
            var partIndices = checked((int)Read32(bytes, partOffset + 12));
            var material = Read32(bytes, partOffset + 16);
            Require(partVertices is >= 1 and <= MaximumVerticesPerPart &&
                partIndices is >= 3 and <= MaximumIndicesPerPart && partIndices % 3 == 0 &&
                firstVertex >= 0 && firstVertex <= vertices && partVertices <= vertices - firstVertex &&
                firstIndex >= 0 && firstIndex <= indices && partIndices <= indices - firstIndex &&
                material < materials && Read32(bytes, partOffset + 20) == 0,
                "SMA1289: SM3D v1 part record, range, material, or reserved field is invalid.");
            for (var value = 0; value < partVertices * 8; value++)
                Require(float.IsFinite(BitConverter.Int32BitsToSingle(unchecked((int)Read32(bytes,
                    32 + partTableBytes + (firstVertex * 8 + value) * 4)))),
                    "SMA1290: SM3D v1 vertex contains a non-finite value.");
            for (var index = 0; index < partIndices; index++)
                Require(Read32(bytes, 32 + partTableBytes + vertexBytes + (firstIndex + index) * 4) < partVertices,
                    "SMA1291: SM3D v1 index is outside its local vertex range.");
        }
        return string.Join('\n',
        [
            "SM3D",
            "Version: 1",
            $"Parts: {parts}",
            $"Vertices: {vertices}",
            $"Indices: {indices}",
            $"Triangles: {indices / 3}",
            $"Materials: {materials}",
            "TextureReferences: 0",
            string.Empty
        ]);
    }

    private static string InspectV2(byte[] bytes)
    {
        var model = ParseV2(bytes);
        var positive = 0;
        var negative = 0;
        foreach (var part in model.Parts)
        {
            for (var vertex = 0; vertex < part.Vertices.Length / 12; vertex++)
            {
                if (part.Vertices[vertex * 12 + 9] < 0) negative++;
                else positive++;
            }
        }

        var lines = new List<string>
        {
            "SM3D",
            "Version: 2",
            $"Name: {model.Name}",
            $"Parts: {model.Parts.Count}",
            $"Vertices: {model.Parts.Sum(part => part.Vertices.Length / 12)}",
            $"Indices: {model.Parts.Sum(part => part.Indices.Length)}",
            $"Triangles: {model.Parts.Sum(part => part.Indices.Length) / 3}",
            $"Materials: {model.Materials.Count}",
            $"TextureReferences: {model.Textures.Count}",
            $"Bounds: {Vector(model.Minimum)} | {Vector(model.Maximum)}",
            $"Tangents: +{positive} -{negative}"
        };
        for (var index = 0; index < model.Parts.Count; index++)
        {
            var part = model.Parts[index];
            lines.Add($"Part {index}: {part.Name} | Vertices {part.Vertices.Length / 12} | Indices {part.Indices.Length} | " +
                $"Material {part.Material} | Bounds {Vector(part.Minimum)} | {Vector(part.Maximum)}");
        }
        for (var index = 0; index < model.Materials.Count; index++)
        {
            var material = model.Materials[index];
            lines.Add($"Material {index}: {material.Name} | BaseColor {ReferenceName(model, material.BaseColorTexture)} | " +
                $"Normal {ReferenceName(model, material.NormalTexture)} | ORM {ReferenceName(model, material.OrmTexture)} | " +
                $"Emissive {ReferenceName(model, material.EmissiveTexture)} | BaseFactor {Values(material.BaseColor)} | " +
                $"Metallic {Number(material.Metallic)} | Roughness {Number(material.Roughness)} | " +
                $"NormalStrength {Number(material.NormalStrength)} | OcclusionStrength {Number(material.OcclusionStrength)} | " +
                $"EmissiveFactor {Values(material.Emissive)} | Alpha {AlphaName(material.AlphaMode)} | " +
                $"Cutoff {Number(material.AlphaCutoff)} | DoubleSided {material.DoubleSided}");
        }
        for (var index = 0; index < model.Textures.Count; index++)
            lines.Add($"Texture {index}: {TextureSemantic(model.Textures[index].Semantic)} | {model.Textures[index].Path}");
        lines.Add(string.Empty);
        return string.Join('\n', lines);
    }

    private static Model ParseV2(byte[] bytes)
    {
        Require(bytes.Length >= HeaderSize && bytes.Length <= MaximumFileBytes, "SMA1207: SM3D v2 file size is invalid.");
        Require(Read16(bytes, 6) == HeaderSize && Read32(bytes, 8) == 0 && Read32(bytes, 12) == bytes.Length &&
            Read32(bytes, 20) is >= 1 and <= MaximumChunks && Read32(bytes, 24) == HeaderSize &&
            Read32(bytes, 28) == DirectoryEntrySize && Read32(bytes, 56) == 0 && Read32(bytes, 60) == 0,
            "SMA1208: SM3D v2 header fields are invalid.");
        Require(Read32(bytes, 16) == Checksum(bytes.AsSpan(HeaderSize)), "SMA1209: SM3D v2 checksum is invalid.");
        var chunkCount = checked((int)Read32(bytes, 20));
        var directoryEnd = checked(HeaderSize + chunkCount * DirectoryEntrySize);
        Require(directoryEnd <= bytes.Length, "SMA1210: SM3D v2 chunk directory is truncated.");
        var chunks = new Dictionary<string, Chunk>(StringComparer.Ordinal);
        for (var index = 0; index < chunkCount; index++)
        {
            var offset = HeaderSize + index * DirectoryEntrySize;
            var idBytes = bytes.AsSpan(offset, 4);
            Require(idBytes.ToArray().All(value => value is >= 32 and <= 126), "SMA1211: chunk ID is invalid.");
            var id = Encoding.ASCII.GetString(idBytes);
            var flags = Read32(bytes, offset + 4);
            var chunkOffset = checked((int)Read32(bytes, offset + 8));
            var length = checked((int)Read32(bytes, offset + 12));
            var count = checked((int)Read32(bytes, offset + 16));
            var stride = checked((int)Read32(bytes, offset + 20));
            Require((flags & ~ChunkOptional) == 0 && Read32(bytes, offset + 24) == 0 && Read32(bytes, offset + 28) == 0,
                "SMA1212: chunk flags or reserved fields are invalid.");
            Require(chunkOffset >= Align4(directoryEnd) && (chunkOffset & 3) == 0 && length >= 0 &&
                chunkOffset <= bytes.Length && length <= bytes.Length - chunkOffset,
                "SMA1213: chunk range or alignment is invalid.");
            Require(chunks.TryAdd(id, new Chunk(id, flags, chunkOffset, length, count, stride)),
                "SMA1214: duplicate chunk IDs are invalid.");
        }

        var ranges = chunks.Values.Where(chunk => chunk.Length > 0).OrderBy(chunk => chunk.Offset).ToArray();
        for (var index = 1; index < ranges.Length; index++)
            Require(ranges[index - 1].Offset + ranges[index - 1].Length <= ranges[index].Offset,
                "SMA1215: chunk ranges overlap.");
        foreach (var chunk in chunks.Values)
            Require(RequiredChunkIds.Contains(chunk.Id, StringComparer.Ordinal) || (chunk.Flags & ChunkOptional) != 0,
                $"SMA1216: unknown required chunk '{chunk.Id}' is unsupported.");
        foreach (var id in RequiredChunkIds)
            Require(chunks.TryGetValue(id, out var chunk) && chunk.Flags == 0,
                $"SMA1217: required chunk '{id}' is missing or optional.");

        var strings = chunks["STR0"];
        var parts = chunks["PART"];
        var vertices = chunks["VERT"];
        var indices = chunks["INDX"];
        var materials = chunks["MATL"];
        var textures = chunks["TEXR"];
        var bounds = chunks["BOND"];
        var partCount = checked((int)Read32(bytes, 36));
        var vertexCount = checked((int)Read32(bytes, 40));
        var indexCount = checked((int)Read32(bytes, 44));
        var materialCount = checked((int)Read32(bytes, 48));
        var textureCount = checked((int)Read32(bytes, 52));
        Require(partCount is >= 1 and <= MaximumParts && vertexCount is >= 1 and <= MaximumVertices &&
            indexCount is >= 3 and <= MaximumIndices && indexCount % 3 == 0 &&
            materialCount is >= 1 and <= MaximumMaterials && textureCount is >= 0 and <= MaximumTextures,
            "SMA1218: SM3D v2 summary counts are invalid.");
        Require(parts.Count == partCount && parts.Stride == 32 && parts.Length == partCount * 32 &&
            vertices.Count == vertexCount && vertices.Stride == 48 && vertices.Length == vertexCount * 48 &&
            indices.Count == indexCount && indices.Stride == 4 && indices.Length == indexCount * 4 &&
            materials.Count == materialCount && materials.Stride == 80 && materials.Length == materialCount * 80 &&
            textures.Count == textureCount && textures.Stride == 16 && textures.Length == textureCount * 16 &&
            bounds.Count == partCount + 1 && bounds.Stride == 32 && bounds.Length == (partCount + 1) * 32 &&
            strings.Count >= 1 && strings.Stride == 0 && strings.Length >= 1,
            "SMA1219: SM3D v2 chunk count, length, or stride is invalid.");
        ValidateStringTable(bytes, strings);
        var modelName = ReadString(bytes, strings, Read32(bytes, 32));

        var textureValues = new List<TextureReference>();
        for (var index = 0; index < textureCount; index++)
        {
            var offset = textures.Offset + index * 16;
            var path = ValidateTexturePath(ReadString(bytes, strings, Read32(bytes, offset)));
            var semantic = Read32(bytes, offset + 4);
            Require(semantic is >= 1 and <= 4 && Read32(bytes, offset + 8) == 0 && Read32(bytes, offset + 12) == 0,
                "SMA1220: texture-reference record is invalid.");
            Require(!textureValues.Any(value => value.Semantic == semantic && string.Equals(value.Path, path, StringComparison.Ordinal)),
                "SMA1221: duplicate texture-reference records are invalid.");
            textureValues.Add(new TextureReference(path, semantic));
        }

        var materialValues = new List<Material>();
        for (var index = 0; index < materialCount; index++)
        {
            var offset = materials.Offset + index * 80;
            var baseReference = ReadReference(bytes, offset + 4, textureValues, 1);
            var normalReference = ReadReference(bytes, offset + 8, textureValues, 2);
            var ormReference = ReadReference(bytes, offset + 12, textureValues, 3);
            var emissiveReference = ReadReference(bytes, offset + 16, textureValues, 4);
            var alpha = Read32(bytes, offset + 20);
            var flags = Read32(bytes, offset + 24);
            Require(alpha <= 2 && flags <= 1 && Read32(bytes, offset + 28) == 0,
                "SMA1222: material alpha, flags, or reserved field is invalid.");
            var baseColor = Enumerable.Range(0, 4).Select(component => ReadFinite(bytes, offset + 32 + component * 4, 0, 1, "base-color factor")).ToArray();
            var metallic = ReadFinite(bytes, offset + 48, 0, 1, "metallic factor");
            var roughness = ReadFinite(bytes, offset + 52, 0, 1, "roughness factor");
            var normalStrength = ReadFinite(bytes, offset + 56, 0, 8, "normal strength");
            var occlusionStrength = ReadFinite(bytes, offset + 60, 0, 1, "occlusion strength");
            var emissive = Enumerable.Range(0, 3).Select(component => ReadFinite(bytes, offset + 64 + component * 4, 0, 64, "emissive factor")).ToArray();
            var alphaCutoff = ReadFinite(bytes, offset + 76, 0, 1, "alpha cutoff");
            materialValues.Add(new Material
            {
                Name = ReadString(bytes, strings, Read32(bytes, offset)),
                BaseColorTexture = baseReference,
                NormalTexture = normalReference,
                OrmTexture = ormReference,
                EmissiveTexture = emissiveReference,
                BaseColor = baseColor,
                Metallic = metallic,
                Roughness = roughness,
                NormalStrength = normalStrength,
                OcclusionStrength = occlusionStrength,
                Emissive = emissive,
                AlphaMode = alpha,
                AlphaCutoff = alphaCutoff,
                DoubleSided = flags != 0
            });
        }

        var modelBounds = ReadBounds(bytes, bounds.Offset);
        var partValues = new List<Part>();
        var expectedVertex = 0;
        var expectedIndex = 0;
        for (var partIndex = 0; partIndex < partCount; partIndex++)
        {
            var offset = parts.Offset + partIndex * 32;
            var firstVertex = checked((int)Read32(bytes, offset + 4));
            var partVertices = checked((int)Read32(bytes, offset + 8));
            var firstIndex = checked((int)Read32(bytes, offset + 12));
            var partIndices = checked((int)Read32(bytes, offset + 16));
            var material = Read32(bytes, offset + 20);
            Require(firstVertex == expectedVertex && firstIndex == expectedIndex &&
                partVertices is >= 1 and <= MaximumVerticesPerPart &&
                partIndices is >= 3 and <= MaximumIndicesPerPart && partIndices % 3 == 0 &&
                partVertices <= vertexCount - firstVertex && partIndices <= indexCount - firstIndex &&
                material < materialCount && Read32(bytes, offset + 24) == partIndex + 1 && Read32(bytes, offset + 28) == 0,
                "SMA1223: part record, range, material, or bounds index is invalid.");
            var partVertexValues = new float[partVertices * 12];
            for (var vertex = 0; vertex < partVertices; vertex++)
            {
                var source = vertices.Offset + (firstVertex + vertex) * 48;
                for (var field = 0; field < 12; field++)
                    partVertexValues[vertex * 12 + field] = ReadFinite(bytes, source + field * 4,
                        field == 9 ? -1 : float.NegativeInfinity, field == 9 ? 1 : float.PositiveInfinity, "vertex");
                var normal = new Vector3(partVertexValues[vertex * 12 + 3], partVertexValues[vertex * 12 + 4], partVertexValues[vertex * 12 + 5]);
                var tangent = new Vector3(partVertexValues[vertex * 12 + 6], partVertexValues[vertex * 12 + 7], partVertexValues[vertex * 12 + 8]);
                var handedness = partVertexValues[vertex * 12 + 9];
                Require(MathF.Abs(normal.LengthSquared() - 1) <= BasisTolerance &&
                    MathF.Abs(tangent.LengthSquared() - 1) <= BasisTolerance &&
                    MathF.Abs(Vector3.Dot(normal, tangent)) <= BasisTolerance &&
                    MathF.Abs(MathF.Abs(handedness) - 1) <= BasisTolerance,
                    "SMA1224: vertex normal, tangent, or handedness is invalid.");
            }
            var partIndexValues = new uint[partIndices];
            for (var index = 0; index < partIndices; index++)
            {
                var value = Read32(bytes, indices.Offset + (firstIndex + index) * 4);
                Require(value < partVertices, "SMA1225: part index is outside its local vertex range.");
                partIndexValues[index] = value;
            }
            ValidateTriangles(partVertexValues, partIndexValues);
            var declaredBounds = ReadBounds(bytes, bounds.Offset + (partIndex + 1) * 32);
            var computedBounds = Bounds(partVertexValues);
            Require(declaredBounds.Minimum == computedBounds.Minimum && declaredBounds.Maximum == computedBounds.Maximum,
                "SMA1226: part bounds do not match geometry.");
            partValues.Add(new Part
            {
                Name = ReadString(bytes, strings, Read32(bytes, offset)),
                Vertices = partVertexValues,
                Indices = partIndexValues,
                Material = material,
                Minimum = declaredBounds.Minimum,
                Maximum = declaredBounds.Maximum
            });
            expectedVertex += partVertices;
            expectedIndex += partIndices;
        }
        Require(expectedVertex == vertexCount && expectedIndex == indexCount,
            "SMA1227: part ranges do not cover the vertex and index chunks exactly.");
        var computedModelMinimum = new Vector3(partValues.Min(part => part.Minimum.X), partValues.Min(part => part.Minimum.Y), partValues.Min(part => part.Minimum.Z));
        var computedModelMaximum = new Vector3(partValues.Max(part => part.Maximum.X), partValues.Max(part => part.Maximum.Y), partValues.Max(part => part.Maximum.Z));
        Require(modelBounds.Minimum == computedModelMinimum && modelBounds.Maximum == computedModelMaximum,
            "SMA1228: model bounds do not match part bounds.");
        return new Model
        {
            Name = modelName,
            Parts = partValues,
            Materials = materialValues,
            Textures = textureValues,
            Minimum = modelBounds.Minimum,
            Maximum = modelBounds.Maximum
        };
    }

    private static void ValidateStringTable(byte[] bytes, Chunk strings)
    {
        Require(bytes[strings.Offset] == 0, "SMA1229: string table must begin with the empty string.");
        var count = 0;
        var offset = 0;
        while (offset < strings.Length)
        {
            var end = Array.IndexOf(bytes, (byte)0, strings.Offset + offset, strings.Length - offset);
            Require(end >= 0, "SMA1230: string table entry is not NUL terminated.");
            try
            {
                StrictUtf8.GetString(bytes, strings.Offset + offset, end - strings.Offset - offset);
            }
            catch (DecoderFallbackException error)
            {
                throw new InvalidDataException("SMA1231: string table contains invalid UTF-8.", error);
            }
            count++;
            offset = end - strings.Offset + 1;
        }
        Require(offset == strings.Length && count == strings.Count, "SMA1232: string table count or length is invalid.");
    }

    private static string ReadString(byte[] bytes, Chunk strings, uint value)
    {
        var offset = checked((int)value);
        Require(offset >= 0 && offset < strings.Length && (offset == 0 || bytes[strings.Offset + offset - 1] == 0),
            "SMA1233: string reference is not at an entry boundary.");
        var end = Array.IndexOf(bytes, (byte)0, strings.Offset + offset, strings.Length - offset);
        Require(end >= 0, "SMA1234: referenced string is not NUL terminated.");
        try
        {
            return StrictUtf8.GetString(bytes, strings.Offset + offset, end - strings.Offset - offset);
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidDataException("SMA1235: referenced string is invalid UTF-8.", error);
        }
    }

    private static int ReadReference(byte[] bytes, int offset, IReadOnlyList<TextureReference> textures, uint semantic)
    {
        var value = Read32(bytes, offset);
        if (value == NoReference) return -1;
        Require(value < textures.Count && textures[(int)value].Semantic == semantic,
            "SMA1236: material texture reference or semantic is invalid.");
        return checked((int)value);
    }

    private static (Vector3 Minimum, Vector3 Maximum) ReadBounds(byte[] bytes, int offset)
    {
        var minimum = new Vector3(ReadFinite(bytes, offset, float.NegativeInfinity, float.PositiveInfinity, "bounds"),
            ReadFinite(bytes, offset + 4, float.NegativeInfinity, float.PositiveInfinity, "bounds"),
            ReadFinite(bytes, offset + 8, float.NegativeInfinity, float.PositiveInfinity, "bounds"));
        var maximum = new Vector3(ReadFinite(bytes, offset + 12, float.NegativeInfinity, float.PositiveInfinity, "bounds"),
            ReadFinite(bytes, offset + 16, float.NegativeInfinity, float.PositiveInfinity, "bounds"),
            ReadFinite(bytes, offset + 20, float.NegativeInfinity, float.PositiveInfinity, "bounds"));
        Require(Read32(bytes, offset + 24) == 0 && Read32(bytes, offset + 28) == 0 &&
            minimum.X <= maximum.X && minimum.Y <= maximum.Y && minimum.Z <= maximum.Z,
            "SMA1237: bounds record is invalid.");
        return (minimum, maximum);
    }

    private static float ReadFinite(byte[] bytes, int offset, float minimum, float maximum, string name)
    {
        var value = BitConverter.Int32BitsToSingle(unchecked((int)Read32(bytes, offset)));
        Require(float.IsFinite(value) && value >= minimum && value <= maximum, $"SMA1238: {name} contains an invalid number.");
        return value;
    }

    private static string ReferenceName(Model model, int reference) => reference < 0 ? "-" : model.Textures[reference].Path;
    private static string AlphaName(uint value) => value switch { 0 => "OPAQUE", 1 => "MASK", 2 => "BLEND", _ => "INVALID" };
    private static string TextureSemantic(uint value) => value switch { 1 => "BaseColor", 2 => "Normal", 3 => "ORM", 4 => "Emissive", _ => "Invalid" };
    private static string Vector(Vector3 value) => $"{Number(value.X)},{Number(value.Y)},{Number(value.Z)}";
    private static string Values(IEnumerable<float> values) => string.Join(',', values.Select(Number));
    private static string Number(float value) =>
        (value == 0 ? 0 : value).ToString("0.######", CultureInfo.InvariantCulture);

    private static string OptionalName(JsonElement value, string fallback)
    {
        if (!value.TryGetProperty("name", out var name)) return fallback;
        var result = name.GetString() ?? string.Empty;
        Require(result.Length > 0 && !result.Contains('\0') && StrictUtf8.GetByteCount(result) <= MaximumNameBytes,
            $"SMA1239: names must be nonempty, contain no NUL, and use at most {MaximumNameBytes} UTF-8 bytes.");
        return result;
    }

    private static int RequiredIndex(JsonElement value, string name) => Required(value, name).GetInt32();

    private static JsonElement Required(JsonElement value, string name) => value.TryGetProperty(name, out var result)
        ? result : throw new InvalidDataException($"SMA1240: required glTF property '{name}' is missing.");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private static int Align4(int value) => checked((value + 3) & ~3);

    private static ushort Read16(ReadOnlySpan<byte> value, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(value[offset..]);
    private static uint Read32(ReadOnlySpan<byte> value, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(value[offset..]);
    private static void Write16(Span<byte> output, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(output[offset..], value);
    private static void Write32(Span<byte> output, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(output[offset..], value);
    private static void WriteFloat(Span<byte> output, int offset, float value) => Write32(output, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));

    private static uint Checksum(ReadOnlySpan<byte> bytes)
    {
        var result = 2166136261U;
        foreach (var value in bytes) result = unchecked((result ^ value) * 16777619U);
        return result;
    }
}
