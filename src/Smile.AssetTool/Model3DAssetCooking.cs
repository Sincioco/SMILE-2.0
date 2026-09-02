using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

#pragma warning disable CA1416 // SMILE's build-time image cooker is intentionally Windows-native.

public enum Model3DAssetCookStatus
{
    Cooked,
    CacheHit,
    CacheRecovered
}

public sealed class Model3DAssetCookRequest
{
    public required string ProjectDirectory { get; init; }
    public required string CacheRoot { get; init; }
    public required string SourcePath { get; init; }
    public required string LogicalPath { get; init; }
    public required string TextureOutputDirectory { get; init; }
    public required string Profile { get; init; }
    public string? DescriptorPath { get; init; }
    public string? Identity { get; init; }
    public int? SampleRate { get; init; }
    public string ProductionState { get; init; } = "Prototype";
}

public sealed class Model3DAssetCookOutput
{
    public required string LogicalPath { get; init; }
    public required string FullPath { get; init; }
    public required string Sha256 { get; init; }
    public required long Length { get; init; }
    public bool IsTexture { get; init; }
    public string? Semantic { get; init; }
    public string? SourceMimeType { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool SourceWasLossy { get; init; }
}

public sealed class Model3DAssetCookResult
{
    public required string CacheKey { get; init; }
    public required string CacheDirectory { get; init; }
    public required Model3DAssetCookStatus Status { get; init; }
    public required IReadOnlyList<Model3DAssetCookOutput> Outputs { get; init; }
    public required string Inspection { get; init; }
}

public static class Model3DAssetCooker
{
    public const string ConverterVersion = "smile-model3d-cooker-m7c-a-v3";
    private const int MaximumSourceBytes = 64 * 1024 * 1024;
    private const int MaximumJsonBytes = 4 * 1024 * 1024;
    private const int MaximumImageBytes = 32 * 1024 * 1024;
    private const int MaximumImageDimension = 4096;
    private const long MaximumImagePixels = 16L * 1024 * 1024;
    private const long MaximumDecodedTextureBytes = 256L * 1024 * 1024;

    public static Model3DAssetCookResult Cook(Model3DAssetCookRequest request)
    {
        ValidateRequest(request);
        using var prepared = PrepareSource(request);
        var key = BuildKey(request, prepared);
        var cacheRoot = Path.GetFullPath(request.CacheRoot);
        Directory.CreateDirectory(cacheRoot);
        var cacheDirectory = Path.Combine(cacheRoot, key);
        using var mutex = new Mutex(false, "Global\\SMILE_Model3D_" + key);
        var ownsMutex = false;
        try
        {
            ownsMutex = mutex.WaitOne(TimeSpan.FromMinutes(2));
        }
        catch (AbandonedMutexException)
        {
            ownsMutex = true;
        }
        if (!ownsMutex)
            throw new IOException("SMA1400: timed out waiting for the Model3DAsset cache entry.");
        try
        {
            if (TryReadCache(cacheDirectory, key, out var cached))
                return Result(cacheDirectory, key, Model3DAssetCookStatus.CacheHit, cached!);

            var recovered = Directory.Exists(cacheDirectory);
            var temporary = Path.Combine(cacheRoot, ".tmp-" + key + "-" + Guid.NewGuid().ToString("N"));
            var displaced = Path.Combine(cacheRoot, ".old-" + key + "-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(temporary);
                var manifest = BuildEntry(request, prepared, temporary, key);
                WriteManifest(Path.Combine(temporary, "cook-manifest.json"), manifest);
                if (Directory.Exists(cacheDirectory))
                    Directory.Move(cacheDirectory, displaced);
                Directory.Move(temporary, cacheDirectory);
                if (Directory.Exists(displaced))
                    Directory.Delete(displaced, recursive: true);
                if (!TryReadCache(cacheDirectory, key, out cached))
                    throw new IOException("SMA1401: freshly written Model3DAsset cache entry failed validation.");
                return Result(cacheDirectory, key, recovered ? Model3DAssetCookStatus.CacheRecovered :
                    Model3DAssetCookStatus.Cooked, cached!);
            }
            catch
            {
                if (!Directory.Exists(cacheDirectory) && Directory.Exists(displaced))
                    Directory.Move(displaced, cacheDirectory);
                throw;
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
                if (Directory.Exists(displaced)) Directory.Delete(displaced, recursive: true);
            }
        }
        finally
        {
            if (ownsMutex) mutex.ReleaseMutex();
        }
    }

    private static Model3DAssetCookResult Result(string cacheDirectory, string key,
        Model3DAssetCookStatus status, CookManifest manifest) => new()
    {
        CacheDirectory = cacheDirectory,
        CacheKey = key,
        Status = status,
        Inspection = manifest.Inspection,
        Outputs = manifest.Outputs.Select(output => new Model3DAssetCookOutput
        {
            LogicalPath = output.LogicalPath,
            FullPath = Path.Combine(cacheDirectory, output.RelativePath.Replace('/', Path.DirectorySeparatorChar)),
            Sha256 = output.Sha256,
            Length = output.Length,
            IsTexture = output.IsTexture,
            Semantic = output.Semantic,
            SourceMimeType = output.SourceMimeType,
            Width = output.Width,
            Height = output.Height,
            SourceWasLossy = output.SourceWasLossy
        }).ToArray()
    };

    private static void ValidateRequest(Model3DAssetCookRequest request)
    {
        var project = Path.GetFullPath(request.ProjectDirectory);
        var source = Path.GetFullPath(request.SourcePath);
        if (!File.Exists(source) || !IsContained(project, source))
            throw new InvalidDataException("SMA1402: Model3DAsset source must be an existing file inside its project.");
        if (new FileInfo(source).Length is <= 0 or > MaximumSourceBytes)
            throw new InvalidDataException($"SMA1403: Model3DAsset source must be 1 through {MaximumSourceBytes} bytes.");
        if (!request.LogicalPath.EndsWith(".sm3d", StringComparison.OrdinalIgnoreCase) ||
            request.LogicalPath.Contains("..", StringComparison.Ordinal))
            throw new InvalidDataException("SMA1404: Model3DAsset LogicalPath must be a confined .sm3d path.");
        if (request.DescriptorPath != null &&
            (!File.Exists(request.DescriptorPath) || !IsContained(project, request.DescriptorPath)))
            throw new InvalidDataException("SMA1405: Model3DAsset descriptor must be an existing file inside its project.");
    }

    private static PreparedSource PrepareSource(Model3DAssetCookRequest request)
    {
        var sourceBytes = File.ReadAllBytes(request.SourcePath);
        byte[] jsonBytes;
        byte[]? glbBinary = null;
        if (Path.GetExtension(request.SourcePath).Equals(".glb", StringComparison.OrdinalIgnoreCase))
            (jsonBytes, glbBinary) = ReadGlb(sourceBytes);
        else
            jsonBytes = sourceBytes;
        if (jsonBytes.Length > MaximumJsonBytes)
            throw new InvalidDataException($"SMA1406: glTF JSON exceeds {MaximumJsonBytes} bytes.");

        var root = JsonNode.Parse(jsonBytes, new JsonNodeOptions { PropertyNameCaseInsensitive = false },
            new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 100 }) as JsonObject ??
            throw new InvalidDataException("SMA1407: glTF root must be an object.");
        var dependencies = new List<DependencyBytes>();
        var buffers = LoadBuffers(root, request, glbBinary, dependencies);
        var imageUses = CollectImageUses(root);
        var textures = PrepareTextures(root, request, buffers, imageUses, dependencies);
        return new PreparedSource(root, buffers, textures, dependencies, sourceBytes);
    }

    private static (byte[] Json, byte[]? Binary) ReadGlb(byte[] bytes)
    {
        if (bytes.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != 0x46546c67 ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) != 2 ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)) != bytes.Length)
            throw new InvalidDataException("SMA1408: GLB 2.0 header is invalid.");
        byte[]? json = null;
        byte[]? binary = null;
        for (var offset = 12; offset < bytes.Length;)
        {
            if (offset > bytes.Length - 8) throw new InvalidDataException("SMA1409: GLB chunk header is truncated.");
            var length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset)));
            var type = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4));
            offset += 8;
            if (length < 0 || offset > bytes.Length - length)
                throw new InvalidDataException("SMA1410: GLB chunk range is invalid.");
            var chunk = bytes.AsSpan(offset, length).ToArray();
            if (type == 0x4e4f534a)
                json = json == null ? chunk.AsSpan().TrimEnd((byte)0, (byte)' ').ToArray() :
                    throw new InvalidDataException("SMA1411: GLB contains duplicate JSON chunks.");
            else if (type == 0x004e4942)
                binary = binary == null ? chunk :
                    throw new InvalidDataException("SMA1412: GLB contains duplicate BIN chunks.");
            offset += length;
        }
        return (json ?? throw new InvalidDataException("SMA1413: GLB JSON chunk is missing."), binary);
    }

    private static List<byte[]> LoadBuffers(JsonObject root, Model3DAssetCookRequest request, byte[]? glbBinary,
        List<DependencyBytes> dependencies)
    {
        var array = root["buffers"] as JsonArray ?? throw new InvalidDataException("SMA1414: glTF buffers are missing.");
        var result = new List<byte[]>();
        for (var index = 0; index < array.Count; index++)
        {
            var buffer = array[index] as JsonObject ?? throw new InvalidDataException("SMA1415: glTF buffer must be an object.");
            byte[] bytes;
            var uri = buffer["uri"]?.GetValue<string>();
            if (uri == null)
            {
                if (index != 0 || glbBinary == null)
                    throw new InvalidDataException("SMA1416: a URI-less glTF buffer requires the GLB BIN chunk.");
                bytes = glbBinary;
            }
            else
            {
                bytes = ReadUriBytes(uri, request.SourcePath, "buffer", MaximumSourceBytes, out var dependency);
                if (dependency != null) dependencies.Add(dependency);
            }
            var declared = buffer["byteLength"]?.GetValue<int>() ?? -1;
            if (declared < 0 || bytes.Length < declared)
                throw new InvalidDataException("SMA1417: glTF buffer is shorter than its declared length.");
            result.Add(bytes);
            buffer["uri"] = "buffer-" + index + ".bin";
        }
        return result;
    }

    private static Dictionary<int, ImageUse> CollectImageUses(JsonObject root)
    {
        var result = new Dictionary<int, ImageUse>();
        var materials = root["materials"] as JsonArray;
        var textures = root["textures"] as JsonArray;
        if (materials == null || textures == null) return result;
        for (var materialIndex = 0; materialIndex < materials.Count; materialIndex++)
        {
            if (materials[materialIndex] is not JsonObject material) continue;
            var pbr = material["pbrMetallicRoughness"] as JsonObject;
            Register(pbr?["baseColorTexture"], "base-color", materialIndex);
            Register(material["normalTexture"], "normal", materialIndex);
            Register(material["emissiveTexture"], "emissive", materialIndex);

            var metallicRoughness = TextureSource(pbr?["metallicRoughnessTexture"]);
            var occlusion = TextureSource(material["occlusionTexture"]);
            if (metallicRoughness != null)
            {
                var use = RegisterSource(metallicRoughness.Value.ImageIndex, "orm", materialIndex,
                    metallicRoughness.Value.TextureIndex);
                if (occlusion != null)
                {
                    use.Semantics.Add("occlusion");
                    if (occlusion.Value.ImageIndex != metallicRoughness.Value.ImageIndex)
                        use.OcclusionImageIndex = occlusion.Value.ImageIndex;
                    ((JsonObject)textures[occlusion.Value.TextureIndex]!)["source"] =
                        metallicRoughness.Value.ImageIndex;
                }
            }
            else if (occlusion != null)
            {
                var use = RegisterSource(occlusion.Value.ImageIndex, "orm", materialIndex,
                    occlusion.Value.TextureIndex);
                use.OcclusionOnly = true;
            }
        }
        return result;

        void Register(JsonNode? infoNode, string semantic, int materialIndex)
        {
            if (infoNode is not JsonObject info || info["index"] == null) return;
            var textureIndex = info["index"]!.GetValue<int>();
            if (textureIndex < 0 || textureIndex >= textures.Count || textures[textureIndex] is not JsonObject texture ||
                texture["source"] == null)
                throw new InvalidDataException("SMA1418: material texture reference is outside the texture table.");
            var imageIndex = texture["source"]!.GetValue<int>();
            RegisterSource(imageIndex, semantic, materialIndex, textureIndex);
        }

        (int TextureIndex, int ImageIndex)? TextureSource(JsonNode? infoNode)
        {
            if (infoNode is not JsonObject info || info["index"] == null) return null;
            var textureIndex = info["index"]!.GetValue<int>();
            if (textureIndex < 0 || textureIndex >= textures.Count ||
                textures[textureIndex] is not JsonObject texture || texture["source"] == null)
                throw new InvalidDataException("SMA1418: material texture reference is outside the texture table.");
            return (textureIndex, texture["source"]!.GetValue<int>());
        }

        ImageUse RegisterSource(int imageIndex, string semantic, int materialIndex, int textureIndex)
        {
            if (!result.TryGetValue(imageIndex, out var use))
            {
                use = new ImageUse(imageIndex, materialIndex);
                result.Add(imageIndex, use);
            }
            use.Semantics.Add(semantic);
            use.TextureIndexes.Add(textureIndex);
            return use;
        }
    }

    private static List<PreparedTexture> PrepareTextures(JsonObject root, Model3DAssetCookRequest request,
        IReadOnlyList<byte[]> buffers, Dictionary<int, ImageUse> uses, List<DependencyBytes> dependencies)
    {
        var images = root["images"] as JsonArray;
        if (uses.Count == 0) return new List<PreparedTexture>();
        if (images == null) throw new InvalidDataException("SMA1419: textured materials require an images table.");
        var decodedTotal = 0L;
        var result = new List<PreparedTexture>();
        foreach (var pair in uses.OrderBy(pair => pair.Key))
        {
            if (pair.Key < 0 || pair.Key >= images.Count || images[pair.Key] is not JsonObject image)
                throw new InvalidDataException("SMA1420: texture source is outside the images table.");
            var use = pair.Value;
            var effective = use.Semantics.Where(value => value != "occlusion").Distinct(StringComparer.Ordinal).ToArray();
            if (effective.Length == 0) effective = new[] { "occlusion" };
            if (effective.Length != 1)
                throw new InvalidDataException("SMA1421: one source image cannot serve incompatible texture semantics.");
            var semantic = effective[0];
            var bytes = ReadImageBytes(image, root, buffers, request, dependencies, out var mime);
            var source = DecodeImage(bytes, ref decodedTotal);
            if (semantic == "orm")
            {
                if (use.OcclusionOnly)
                    ApplyOrmChannels(source, occlusion: source, neutralOcclusion: false,
                        neutralMetallicRoughness: true);
                else if (use.OcclusionImageIndex != null)
                {
                    if (use.OcclusionImageIndex < 0 || use.OcclusionImageIndex >= images.Count ||
                        images[use.OcclusionImageIndex.Value] is not JsonObject occlusionImage)
                        throw new InvalidDataException("SMA1437: occlusion image is outside the images table.");
                    var occlusionBytes = ReadImageBytes(occlusionImage, root, buffers, request, dependencies,
                        out _);
                    using var occlusion = DecodeImage(occlusionBytes, ref decodedTotal);
                    if (source.Width != occlusion.Width || source.Height != occlusion.Height)
                        throw new InvalidDataException(
                            "SMA1438: separate occlusion and metallic-roughness images must have matching dimensions.");
                    ApplyOrmChannels(source, occlusion, neutralOcclusion: false,
                        neutralMetallicRoughness: false);
                }
                else if (!use.Semantics.Contains("occlusion"))
                    ApplyOrmChannels(source, occlusion: source, neutralOcclusion: true,
                        neutralMetallicRoughness: false);
            }
            var hash = Hash(bytes).Substring(0, 12).ToLowerInvariant();
            var modelName = SafeName(Path.GetFileNameWithoutExtension(request.LogicalPath));
            var fileName = $"{modelName}-m{use.MaterialIndex}-{semantic}-{hash}.png";
            var logical = request.TextureOutputDirectory.TrimEnd('/') + "/" + fileName;
            var texture = new PreparedTexture(pair.Key, logical, semantic, mime, source.Width, source.Height,
                mime.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase), source);
            result.Add(texture);
            image.Remove("bufferView");
            image.Remove("mimeType");
            image["uri"] = logical;
        }
        return result;
    }

    private static byte[] ReadImageBytes(JsonObject image, JsonObject root, IReadOnlyList<byte[]> buffers,
        Model3DAssetCookRequest request, List<DependencyBytes> dependencies, out string mime)
    {
        mime = image["mimeType"]?.GetValue<string>() ?? string.Empty;
        byte[] bytes;
        var uri = image["uri"]?.GetValue<string>();
        if (uri != null)
        {
            bytes = ReadUriBytes(uri, request.SourcePath, "image", MaximumImageBytes, out var dependency);
            if (dependency != null) dependencies.Add(dependency);
            if (mime.Length == 0) mime = MimeFromBytes(bytes);
        }
        else
        {
            var views = root["bufferViews"] as JsonArray ??
                throw new InvalidDataException("SMA1422: embedded image requires bufferViews.");
            var viewIndex = image["bufferView"]?.GetValue<int>() ?? -1;
            if (viewIndex < 0 || viewIndex >= views.Count || views[viewIndex] is not JsonObject view)
                throw new InvalidDataException("SMA1423: embedded image bufferView is invalid.");
            var bufferIndex = view["buffer"]?.GetValue<int>() ?? 0;
            var offset = view["byteOffset"]?.GetValue<int>() ?? 0;
            var length = view["byteLength"]?.GetValue<int>() ?? -1;
            if (bufferIndex < 0 || bufferIndex >= buffers.Count || offset < 0 || length <= 0 ||
                length > MaximumImageBytes || offset > buffers[bufferIndex].Length - length)
                throw new InvalidDataException("SMA1424: embedded image range is invalid or exceeds its limit.");
            bytes = buffers[bufferIndex].AsSpan(offset, length).ToArray();
            if (mime.Length == 0) mime = MimeFromBytes(bytes);
        }
        if (mime is not "image/png" and not "image/jpeg")
            throw new InvalidDataException($"SMA1425: texture MIME '{mime}' is not PNG or JPEG.");
        return bytes;
    }

    private static Bitmap DecodeImage(byte[] bytes, ref long decodedTotal)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        Image decoded;
        try
        {
            decoded = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
        }
        catch (Exception exception) when (exception is ArgumentException or ExternalException)
        {
            var signature = Convert.ToHexString(bytes.AsSpan(0, Math.Min(8, bytes.Length)));
            throw new InvalidDataException(
                $"SMA1436: image decoder rejected {bytes.Length} bytes with signature {signature}.", exception);
        }
        using (decoded)
        {
        if (decoded.Width is < 1 or > MaximumImageDimension || decoded.Height is < 1 or > MaximumImageDimension ||
            (long)decoded.Width * decoded.Height > MaximumImagePixels)
            throw new InvalidDataException("SMA1426: texture dimensions exceed the bounded 4096/16-megapixel profile.");
        decodedTotal = checked(decodedTotal + (long)decoded.Width * decoded.Height * 4);
        if (decodedTotal > MaximumDecodedTextureBytes)
            throw new InvalidDataException("SMA1427: decoded texture aggregate exceeds 256 MiB.");
        var result = new Bitmap(decoded.Width, decoded.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        graphics.DrawImageUnscaled(decoded, 0, 0);
        return result;
        }
    }

    private static void ApplyOrmChannels(Bitmap target, Bitmap occlusion, bool neutralOcclusion,
        bool neutralMetallicRoughness)
    {
        for (var y = 0; y < target.Height; y++)
            for (var x = 0; x < target.Width; x++)
            {
                var current = target.GetPixel(x, y);
                var sourceOcclusion = occlusion.GetPixel(x, y);
                target.SetPixel(x, y, Color.FromArgb(current.A,
                    neutralOcclusion ? 255 : sourceOcclusion.R,
                    neutralMetallicRoughness ? 255 : current.G,
                    neutralMetallicRoughness ? 0 : current.B));
            }
    }

    private static CookManifest BuildEntry(Model3DAssetCookRequest request, PreparedSource prepared,
        string entry, string key)
    {
        var inputs = Path.Combine(entry, "_inputs");
        Directory.CreateDirectory(inputs);
        for (var index = 0; index < prepared.Buffers.Count; index++)
            WriteAtomic(Path.Combine(inputs, "buffer-" + index + ".bin"), prepared.Buffers[index]);
        var textureOutputs = new List<ManifestOutput>();
        foreach (var texture in prepared.Textures)
        {
            var path = Path.Combine(entry, texture.LogicalPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var encoded = new MemoryStream();
            try
            {
                texture.Bitmap.Save(encoded, ImageFormat.Png);
            }
            catch (Exception exception) when (exception is ArgumentException or ExternalException)
            {
                throw new InvalidDataException(
                    $"SMA1439: PNG encoder rejected generated {texture.Semantic} texture '{texture.LogicalPath}'.",
                    exception);
            }
            WriteAtomic(path, encoded.ToArray());
            texture.Bitmap.Dispose();
            textureOutputs.Add(Output(entry, texture.LogicalPath, path, true, texture.Semantic,
                texture.SourceMimeType, texture.Width, texture.Height, texture.SourceWasLossy));
        }
        var modelJsonPath = Path.Combine(inputs, "model.gltf");
        WriteAtomic(modelJsonPath, Encoding.UTF8.GetBytes(prepared.Root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        })));
        var descriptor = PrepareDescriptor(request, inputs);
        var modelBytes = Sm3dV2.Convert(modelJsonPath, descriptor);
        var modelPath = Path.Combine(entry, request.LogicalPath.Replace('/', Path.DirectorySeparatorChar));
        WriteAtomic(modelPath, modelBytes);
        var inspection = Sm3dV2.Inspect(modelPath);
        ValidateProfile(request.Profile, inspection);
        var outputs = new List<ManifestOutput>
        {
            Output(entry, request.LogicalPath, modelPath, false, null, null, 0, 0, false)
        };
        outputs.AddRange(textureOutputs.OrderBy(output => output.LogicalPath, StringComparer.Ordinal));
        return new CookManifest
        {
            Version = 1,
            ConverterVersion = ConverterVersion,
            CacheKey = key,
            SourceSha256 = Hash(prepared.SourceBytes),
            DescriptorSha256 = request.DescriptorPath == null ? null : Hash(File.ReadAllBytes(request.DescriptorPath)),
            Identity = request.Identity,
            Profile = request.Profile,
            ProductionState = request.ProductionState,
            TextureOutputDirectory = request.TextureOutputDirectory,
            SampleRate = request.SampleRate,
            Inspection = inspection,
            Outputs = outputs
        };
    }

    private static string? PrepareDescriptor(Model3DAssetCookRequest request, string inputs)
    {
        if (request.DescriptorPath == null && request.SampleRate == null) return null;
        JsonObject descriptor;
        if (request.DescriptorPath == null)
            descriptor = new JsonObject { ["version"] = 1 };
        else
            descriptor = JsonNode.Parse(File.ReadAllBytes(request.DescriptorPath)) as JsonObject ??
                throw new InvalidDataException("SMA1428: animation descriptor root must be an object.");
        if (request.SampleRate != null) descriptor["sampleRate"] = request.SampleRate.Value;
        var path = Path.Combine(inputs, "descriptor.sm3d.json");
        WriteAtomic(path, Encoding.UTF8.GetBytes(descriptor.ToJsonString()));
        return path;
    }

    private static void ValidateProfile(string profile, string inspection)
    {
        var clips = InspectionValue(inspection, "Clips");
        var bones = InspectionValue(inspection, "Bones");
        if (profile.Equals("Static", StringComparison.OrdinalIgnoreCase) && (clips > 0 || bones > 0))
            throw new InvalidDataException("SMA1429: Static Model3DAsset profile rejects skeletons and animation clips.");
        if (profile.Equals("Character", StringComparison.OrdinalIgnoreCase) && (clips <= 0 || bones <= 0))
            throw new InvalidDataException("SMA1430: Character Model3DAsset profile requires a skeleton and animation clips.");
    }

    private static int InspectionValue(string inspection, string name)
    {
        var prefix = name + ": ";
        var line = inspection.Split('\n').FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
        return line != null && int.TryParse(line.Substring(prefix.Length).Trim(), out var value) ? value : 0;
    }

    private static ManifestOutput Output(string entry, string logical, string path, bool texture,
        string? semantic, string? sourceMime, int width, int height, bool lossy) => new()
    {
        LogicalPath = logical,
        RelativePath = logical,
        Sha256 = Hash(File.ReadAllBytes(path)),
        Length = new FileInfo(path).Length,
        IsTexture = texture,
        Generated = true,
        Semantic = semantic,
        SourceMimeType = sourceMime,
        Width = width,
        Height = height,
        SourceWasLossy = lossy
    };

    private static string BuildKey(Model3DAssetCookRequest request, PreparedSource prepared)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AddText(ConverterVersion);
        AddText(request.LogicalPath);
        AddText(request.TextureOutputDirectory);
        AddText(request.Profile);
        AddText(request.Identity ?? string.Empty);
        AddText(request.SampleRate?.ToString() ?? string.Empty);
        AddText(request.ProductionState);
        AddBytes(prepared.SourceBytes);
        if (request.DescriptorPath != null) AddBytes(File.ReadAllBytes(request.DescriptorPath));
        foreach (var dependency in prepared.Dependencies.OrderBy(value => value.Identity, StringComparer.Ordinal))
        {
            AddText(dependency.Identity);
            AddBytes(dependency.Bytes);
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();

        void AddText(string value) => AddBytes(Encoding.UTF8.GetBytes(value));
        void AddBytes(byte[] bytes)
        {
            hash.AppendData(BitConverter.GetBytes(bytes.Length));
            hash.AppendData(bytes);
        }
    }

    private static byte[] ReadUriBytes(string uri, string sourcePath, string kind, int maximum,
        out DependencyBytes? dependency)
    {
        dependency = null;
        if (uri.StartsWith("data:", StringComparison.Ordinal))
        {
            var comma = uri.IndexOf(',');
            if (comma < 0 || !uri.Substring(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SMA1431: {kind} data URI must use base64 encoding.");
            var bytes = Convert.FromBase64String(uri.Substring(comma + 1));
            if (bytes.Length > maximum) throw new InvalidDataException($"SMA1432: {kind} data URI exceeds its limit.");
            dependency = new DependencyBytes("data:" + Hash(bytes), bytes);
            return bytes;
        }
        if (Uri.TryCreate(uri, UriKind.Absolute, out _) || uri.Contains('\\'))
            throw new InvalidDataException($"SMA1433: {kind} URI must be a portable relative path.");
        var baseDirectory = Path.GetDirectoryName(sourcePath)!;
        var path = Path.GetFullPath(Path.Combine(baseDirectory, uri.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsContained(baseDirectory, path) || !File.Exists(path))
            throw new InvalidDataException($"SMA1434: {kind} URI escapes the model directory or is missing: '{uri}'.");
        if (new FileInfo(path).Length > maximum)
            throw new InvalidDataException($"SMA1435: {kind} dependency exceeds its byte limit: '{uri}'.");
        var result = File.ReadAllBytes(path);
        dependency = new DependencyBytes(uri, result);
        return result;
    }

    private static bool TryReadCache(string directory, string key, out CookManifest? manifest)
    {
        manifest = null;
        try
        {
            var path = Path.Combine(directory, "cook-manifest.json");
            if (!File.Exists(path)) return false;
            manifest = JsonSerializer.Deserialize<CookManifest>(File.ReadAllBytes(path), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (manifest == null || manifest.Version != 1 || manifest.CacheKey != key ||
                manifest.ConverterVersion != ConverterVersion || manifest.SourceSha256.Length != 64 ||
                manifest.Outputs.Count == 0 || manifest.Outputs.Any(output => !output.Generated))
                return false;
            foreach (var output in manifest.Outputs)
            {
                var file = Path.Combine(directory, output.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file) || new FileInfo(file).Length != output.Length ||
                    !Hash(File.ReadAllBytes(file)).Equals(output.Sha256, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            manifest = null;
            return false;
        }
    }

    private static void WriteManifest(string path, CookManifest manifest) =>
        WriteAtomic(path, JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

    private static void WriteAtomic(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       65536, FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string MimeFromBytes(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[^2] == 0xff && bytes[^1] == 0xd9)
            return "image/jpeg";
        return string.Empty;
    }

    private static bool IsContained(string rootPath, string path)
    {
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                   Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeName(string value)
    {
        var result = new string(value.Select(character => char.IsLetterOrDigit(character) || character == '-'
            ? character : '-').ToArray()).Trim('-');
        return result.Length == 0 ? "model" : result;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private sealed class PreparedSource : IDisposable
    {
        public PreparedSource(JsonObject root, IReadOnlyList<byte[]> buffers,
            IReadOnlyList<PreparedTexture> textures, IReadOnlyList<DependencyBytes> dependencies,
            byte[] sourceBytes)
        {
            Root = root;
            Buffers = buffers;
            Textures = textures;
            Dependencies = dependencies;
            SourceBytes = sourceBytes;
        }
        public JsonObject Root { get; }
        public IReadOnlyList<byte[]> Buffers { get; }
        public IReadOnlyList<PreparedTexture> Textures { get; }
        public IReadOnlyList<DependencyBytes> Dependencies { get; }
        public byte[] SourceBytes { get; }
        public void Dispose()
        {
            foreach (var texture in Textures) texture.Bitmap.Dispose();
        }
    }
    private sealed record DependencyBytes(string Identity, byte[] Bytes);
    private sealed class ImageUse
    {
        public ImageUse(int imageIndex, int materialIndex) { ImageIndex = imageIndex; MaterialIndex = materialIndex; }
        public int ImageIndex { get; }
        public int MaterialIndex { get; }
        public HashSet<string> Semantics { get; } = new(StringComparer.Ordinal);
        public HashSet<int> TextureIndexes { get; } = new();
        public int? OcclusionImageIndex { get; set; }
        public bool OcclusionOnly { get; set; }
    }
    private sealed record PreparedTexture(int ImageIndex, string LogicalPath, string Semantic,
        string SourceMimeType, int Width, int Height, bool SourceWasLossy, Bitmap Bitmap);
    private sealed class CookManifest
    {
        public int Version { get; set; }
        public string ConverterVersion { get; set; } = string.Empty;
        public string CacheKey { get; set; } = string.Empty;
        public string SourceSha256 { get; set; } = string.Empty;
        public string? DescriptorSha256 { get; set; }
        public string? Identity { get; set; }
        public string Profile { get; set; } = string.Empty;
        public string ProductionState { get; set; } = string.Empty;
        public string TextureOutputDirectory { get; set; } = string.Empty;
        public int? SampleRate { get; set; }
        public string Inspection { get; set; } = string.Empty;
        public List<ManifestOutput> Outputs { get; set; } = new();
    }
    private sealed class ManifestOutput
    {
        public string LogicalPath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public long Length { get; set; }
        public bool IsTexture { get; set; }
        public bool Generated { get; set; }
        public string? Semantic { get; set; }
        public string? SourceMimeType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool SourceWasLossy { get; set; }
    }
}

internal static class SpanByteTrimming
{
    public static ReadOnlySpan<byte> TrimEnd(this ReadOnlySpan<byte> value, params byte[] bytes)
    {
        var length = value.Length;
        while (length > 0 && bytes.Contains(value[length - 1])) length--;
        return value[..length];
    }
}

#pragma warning restore CA1416
