using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;

return AssetTool.Run(args);

internal static class AssetTool
{
    private const int MaximumParts = 16;
    private const int MaximumVerticesPerPart = 65535;
    private const int MaximumIndicesPerPart = 196608;
    private const int MaximumOutputBytes = 16 * 1024 * 1024;

    private sealed record Part(float[] Vertices, uint[] Indices, uint Material);
    private sealed record View(byte[] Buffer, int Offset, int Length, int Stride);

    public static int Run(string[] arguments)
    {
        try
        {
            if (arguments.Length == 2 && arguments[0].Equals("inspect", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write(Sm3dV2.Inspect(Path.GetFullPath(arguments[1])));
                return 0;
            }

            if (arguments.Length is not (4 or 6) ||
                !arguments[0].Equals("model", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "usage: smileasset model <input.gltf|input.glb> [--format-version 2] -o <output.sm3d>\n" +
                    "       smileasset inspect <input.sm3d>");

            var outputOption = Array.IndexOf(arguments, "-o");
            if (outputOption < 2 || outputOption != arguments.Length - 2)
                throw new InvalidDataException("SMA1001: model conversion requires '-o <output.sm3d>'.");

            var input = Path.GetFullPath(arguments[1]);
            var output = Path.GetFullPath(arguments[outputOption + 1]);
            var extension = Path.GetExtension(input);
            var requestedV2 = extension.Equals(".glb", StringComparison.OrdinalIgnoreCase);

            if (arguments.Length == 6)
            {
                if (!arguments[2].Equals("--format-version", StringComparison.OrdinalIgnoreCase) ||
                    arguments[3] != "2")
                    throw new InvalidDataException("SMA1001: the only supported format option is '--format-version 2'.");
                requestedV2 = true;
            }

            if (!extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".glb", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SMA1001: model input must be glTF 2.0 .gltf or .glb.");
            if (extension.Equals(".glb", StringComparison.OrdinalIgnoreCase) && !requestedV2)
                throw new InvalidDataException("SMA1001: GLB input requires SM3D version 2 output.");

            var bytes = requestedV2 ? Sm3dV2.Convert(input) : ConvertModel(input);
            var parent = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            var temporary = output + ".tmp-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, output, true);
            Console.WriteLine($"Converted {input}");
            Console.WriteLine($"Output: {output}");
            Console.WriteLine($"Version: {(requestedV2 ? 2 : 1)}");
            Console.WriteLine($"Bytes: {bytes.Length}");
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or
            InvalidDataException or FormatException or OverflowException)
        {
            Console.Error.WriteLine("error " + error.Message);
            return 2;
        }
    }

    private static byte[] ConvertModel(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 64
        });
        var root = document.RootElement;
        Require(root.ValueKind == JsonValueKind.Object, "SMA1002: glTF root must be an object.");
        var asset = Required(root, "asset");
        Require(Required(asset, "version").GetString() == "2.0", "SMA1003: only glTF 2.0 is supported.");
        var scenes = Required(root, "scenes");
        Require(scenes.ValueKind == JsonValueKind.Array && scenes.GetArrayLength() == 1,
            "SMA1004: the v1 model subset requires exactly one scene.");
        var baseDirectory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();
        var buffers = ReadBuffers(root, baseDirectory);
        var views = ReadViews(root, buffers);
        var accessors = Required(root, "accessors");
        Require(accessors.ValueKind == JsonValueKind.Array, "SMA1005: accessors must be an array.");
        var materialCount = 1;
        if (root.TryGetProperty("materials", out var materials))
        {
            Require(materials.ValueKind == JsonValueKind.Array, "SMA1006: materials must be an array.");
            materialCount = materials.GetArrayLength();
        }
        Require(materialCount is >= 1 and <= 64, "SMA1006: models require 1 to 64 material slots.");
        var meshes = Required(root, "meshes");
        Require(meshes.ValueKind == JsonValueKind.Array, "SMA1007: meshes must be an array.");
        var parts = new List<Part>();
        foreach (var mesh in meshes.EnumerateArray())
        {
            var primitives = Required(mesh, "primitives");
            Require(primitives.ValueKind == JsonValueKind.Array, "SMA1008: mesh primitives must be an array.");
            foreach (var primitive in primitives.EnumerateArray())
            {
                Require(!primitive.TryGetProperty("mode", out var mode) || mode.GetInt32() == 4,
                    "SMA1009: only triangle primitives are supported.");
                var attributes = Required(primitive, "attributes");
                var positions = ReadFloatAccessor(accessors, views, RequiredIndex(attributes, "POSITION"), 3, "POSITION");
                var normals = ReadFloatAccessor(accessors, views, RequiredIndex(attributes, "NORMAL"), 3, "NORMAL");
                var uvs = ReadFloatAccessor(accessors, views, RequiredIndex(attributes, "TEXCOORD_0"), 2, "TEXCOORD_0");
                Require(positions.Length / 3 == normals.Length / 3 && positions.Length / 3 == uvs.Length / 2,
                    "SMA1010: vertex attribute counts must match.");
                var vertexCount = positions.Length / 3;
                Require(vertexCount is >= 1 and <= MaximumVerticesPerPart,
                    $"SMA1011: each primitive supports 1 to {MaximumVerticesPerPart} vertices.");
                Require(primitive.TryGetProperty("indices", out var indexValue),
                    "SMA1012: indexed triangle primitives are required.");
                var indices = ReadIndexAccessor(accessors, views, indexValue.GetInt32());
                Require(indices.Length is >= 3 and <= MaximumIndicesPerPart && indices.Length % 3 == 0,
                    $"SMA1013: index counts must be triangular and at most {MaximumIndicesPerPart}.");
                Require(indices.All(index => index < vertexCount), "SMA1014: an index is outside its primitive vertex range.");
                for (var index = 0; index < indices.Length; index += 3)
                    (indices[index + 1], indices[index + 2]) = (indices[index + 2], indices[index + 1]);
                var vertices = new float[vertexCount * 8];
                for (var index = 0; index < vertexCount; index++)
                {
                    vertices[index * 8] = positions[index * 3];
                    vertices[index * 8 + 1] = positions[index * 3 + 1];
                    vertices[index * 8 + 2] = -positions[index * 3 + 2];
                    vertices[index * 8 + 3] = normals[index * 3];
                    vertices[index * 8 + 4] = normals[index * 3 + 1];
                    vertices[index * 8 + 5] = -normals[index * 3 + 2];
                    vertices[index * 8 + 6] = uvs[index * 2];
                    vertices[index * 8 + 7] = uvs[index * 2 + 1];
                }
                var material = primitive.TryGetProperty("material", out var materialValue)
                    ? materialValue.GetUInt32() : 0;
                Require(material < materialCount, "SMA1015: material reference is outside the material table.");
                parts.Add(new Part(vertices, indices, material));
            }
        }
        Require(parts.Count is >= 1 and <= MaximumParts,
            $"SMA1016: models require 1 to {MaximumParts} triangle primitives.");
        return WriteModel(parts, materialCount);
    }

    private static List<byte[]> ReadBuffers(JsonElement root, string baseDirectory)
    {
        var values = Required(root, "buffers");
        Require(values.ValueKind == JsonValueKind.Array, "SMA1017: buffers must be an array.");
        var result = new List<byte[]>();
        foreach (var value in values.EnumerateArray())
        {
            var uri = Required(value, "uri").GetString() ?? "";
            byte[] bytes;
            const string prefix = "data:application/octet-stream;base64,";
            if (uri.StartsWith(prefix, StringComparison.Ordinal))
                bytes = Convert.FromBase64String(uri[prefix.Length..]);
            else
            {
                Require(!Uri.TryCreate(uri, UriKind.Absolute, out _), "SMA1018: external buffer URI must be relative.");
                var candidate = Path.GetFullPath(Path.Combine(baseDirectory, uri.Replace('/', Path.DirectorySeparatorChar)));
                var rootPath = Path.GetFullPath(baseDirectory) + Path.DirectorySeparatorChar;
                Require(candidate.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase),
                    "SMA1019: external buffer escapes the model directory.");
                bytes = File.ReadAllBytes(candidate);
            }
            var declared = Required(value, "byteLength").GetInt32();
            Require(declared >= 0 && bytes.Length >= declared, "SMA1020: buffer is shorter than its declared size.");
            result.Add(bytes);
        }
        return result;
    }

    private static List<View> ReadViews(JsonElement root, IReadOnlyList<byte[]> buffers)
    {
        var values = Required(root, "bufferViews");
        var result = new List<View>();
        foreach (var value in values.EnumerateArray())
        {
            var bufferIndex = Required(value, "buffer").GetInt32();
            Require(bufferIndex >= 0 && bufferIndex < buffers.Count, "SMA1021: bufferView references an invalid buffer.");
            var offset = value.TryGetProperty("byteOffset", out var offsetValue) ? offsetValue.GetInt32() : 0;
            var length = Required(value, "byteLength").GetInt32();
            var stride = value.TryGetProperty("byteStride", out var strideValue) ? strideValue.GetInt32() : 0;
            Require(offset >= 0 && length >= 0 && offset <= buffers[bufferIndex].Length &&
                length <= buffers[bufferIndex].Length - offset, "SMA1022: bufferView range is invalid.");
            result.Add(new View(buffers[bufferIndex], offset, length, stride));
        }
        return result;
    }

    private static float[] ReadFloatAccessor(JsonElement accessors, IReadOnlyList<View> views,
        int accessorIndex, int components, string semantic)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        var isNormalized = accessor.TryGetProperty("normalized", out var normalized) && normalized.ValueKind == JsonValueKind.True;
        Require(Required(accessor, "componentType").GetInt32() == 5126 &&
            Required(accessor, "type").GetString() == (components == 3 ? "VEC3" : "VEC2") && !isNormalized,
            $"SMA1023: {semantic} must use non-normalized float vectors.");
        var stride = view.Stride == 0 ? components * 4 : view.Stride;
        Require(stride >= components * 4 && offset + (long)Math.Max(0, count - 1) * stride + components * 4 <= view.Length,
            $"SMA1024: {semantic} accessor range is invalid.");
        var result = new float[count * components];
        for (var index = 0; index < count; index++)
            for (var component = 0; component < components; component++)
            {
                var bits = BinaryPrimitives.ReadInt32LittleEndian(view.Buffer.AsSpan(
                    view.Offset + offset + index * stride + component * 4, 4));
                result[index * components + component] = BitConverter.Int32BitsToSingle(bits);
                Require(float.IsFinite(result[index * components + component]),
                    $"SMA1025: {semantic} contains a non-finite value.");
            }
        return result;
    }

    private static uint[] ReadIndexAccessor(JsonElement accessors, IReadOnlyList<View> views, int accessorIndex)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        var componentType = Required(accessor, "componentType").GetInt32();
        Require(Required(accessor, "type").GetString() == "SCALAR" && componentType is 5121 or 5123 or 5125,
            "SMA1026: indices must be unsigned byte, unsigned short, or unsigned int scalars.");
        var size = componentType == 5121 ? 1 : componentType == 5123 ? 2 : 4;
        var stride = view.Stride == 0 ? size : view.Stride;
        Require(stride >= size && offset + (long)Math.Max(0, count - 1) * stride + size <= view.Length,
            "SMA1027: index accessor range is invalid.");
        var result = new uint[count];
        for (var index = 0; index < count; index++)
        {
            var source = view.Buffer.AsSpan(view.Offset + offset + index * stride, size);
            result[index] = componentType == 5121 ? source[0] : componentType == 5123
                ? BinaryPrimitives.ReadUInt16LittleEndian(source) : BinaryPrimitives.ReadUInt32LittleEndian(source);
        }
        return result;
    }

    private static JsonElement Accessor(JsonElement accessors, int index, IReadOnlyList<View> views,
        out View view, out int offset, out int count)
    {
        Require(index >= 0 && index < accessors.GetArrayLength(), "SMA1028: accessor index is invalid.");
        var accessor = accessors[index];
        Require(!accessor.TryGetProperty("sparse", out _), "SMA1029: sparse accessors are not supported.");
        var viewIndex = Required(accessor, "bufferView").GetInt32();
        Require(viewIndex >= 0 && viewIndex < views.Count, "SMA1030: accessor bufferView is invalid.");
        view = views[viewIndex];
        offset = accessor.TryGetProperty("byteOffset", out var offsetValue) ? offsetValue.GetInt32() : 0;
        count = Required(accessor, "count").GetInt32();
        Require(offset >= 0 && count > 0, "SMA1031: accessor offset/count is invalid.");
        return accessor;
    }

    private static byte[] WriteModel(IReadOnlyList<Part> parts, int materialCount)
    {
        var vertexCount = parts.Sum(part => part.Vertices.Length / 8);
        var indexCount = parts.Sum(part => part.Indices.Length);
        var size = checked(32 + parts.Count * 24 + vertexCount * 32 + indexCount * 4);
        Require(size <= MaximumOutputBytes, "SMA1032: converted model exceeds the 16 MiB runtime limit.");
        var output = new byte[size];
        "SM3D"u8.CopyTo(output);
        Write16(output, 4, 1); Write16(output, 6, 32);
        Write32(output, 8, (uint)parts.Count); Write32(output, 12, (uint)vertexCount);
        Write32(output, 16, (uint)indexCount); Write32(output, 20, (uint)materialCount);
        Write32(output, 24, (uint)size);
        var vertexStart = 32 + parts.Count * 24;
        var indexStart = vertexStart + vertexCount * 32;
        var firstVertex = 0;
        var firstIndex = 0;
        for (var partIndex = 0; partIndex < parts.Count; partIndex++)
        {
            var part = parts[partIndex];
            var table = 32 + partIndex * 24;
            Write32(output, table, (uint)firstVertex);
            Write32(output, table + 4, (uint)(part.Vertices.Length / 8));
            Write32(output, table + 8, (uint)firstIndex);
            Write32(output, table + 12, (uint)part.Indices.Length);
            Write32(output, table + 16, part.Material);
            for (var index = 0; index < part.Vertices.Length; index++)
                Write32(output, vertexStart + (firstVertex * 8 + index) * 4,
                    (uint)BitConverter.SingleToInt32Bits(part.Vertices[index]));
            for (var index = 0; index < part.Indices.Length; index++)
                Write32(output, indexStart + (firstIndex + index) * 4, part.Indices[index]);
            firstVertex += part.Vertices.Length / 8;
            firstIndex += part.Indices.Length;
        }
        Write32(output, 28, Checksum(output.AsSpan(32)));
        return output;
    }

    private static uint Checksum(ReadOnlySpan<byte> bytes)
    {
        var result = 2166136261U;
        foreach (var value in bytes) result = unchecked((result ^ value) * 16777619U);
        return result;
    }

    private static int RequiredIndex(JsonElement value, string name) => Required(value, name).GetInt32();
    private static JsonElement Required(JsonElement value, string name) => value.TryGetProperty(name, out var result)
        ? result : throw new InvalidDataException($"SMA1033: required glTF property '{name}' is missing.");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
    private static void Write16(Span<byte> output, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(output[offset..], value);
    private static void Write32(Span<byte> output, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(output[offset..], value);
}
