using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text.Json;
using Smile.Language;

namespace Smile.Compiler;

// Works exclusively inside the compiler's unpublished staging directory. PNG
// keeps alpha and data channels; model/animation/calibration bytes are untouched.
internal static class WebImageOptimizer
{
    internal const string ManifestName = "smile-web-quality.json";

    internal static IReadOnlyDictionary<string, int[]> Optimize(string stagingDirectory,
        IReadOnlyList<string> assetPaths, SmileWebQuality quality)
    {
        var originalSizes = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
        if (quality == SmileWebQuality.Full)
            return originalSizes;
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            throw new InvalidDataException("Optimized Web image publication requires the Windows SMILE build host.");

        var divisor = quality == SmileWebQuality.Low ? 4.0 : quality == SmileWebQuality.Medium ? 2.0 : 4.0 / 3.0;
        var maximumEdge = quality == SmileWebQuality.Low ? 512 : quality == SmileWebQuality.Medium ? 1024 : 2048;
        var images = new List<object>();
        long originalBytes = 0, publishedBytes = 0;
        var root = Path.GetFullPath(stagingDirectory) + Path.DirectorySeparatorChar;
        foreach (var logicalPath in assetPaths.OrderBy(path => path, StringComparer.Ordinal))
        {
            if (!logicalPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;
            var path = Path.GetFullPath(Path.Combine(stagingDirectory, logicalPath));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Optimized image path escapes the staging directory.");
            if (new FileInfo(path).Length > 64 * 1024 * 1024)
                throw new InvalidDataException($"Optimized Web PNG exceeds the 64 MiB input bound: {logicalPath}");
            var sourceBytes = File.ReadAllBytes(path);
            if (sourceBytes.Length < 24 || !sourceBytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                throw new InvalidDataException($"Optimized Web image is not a PNG: {logicalPath}");
            var headerWidth = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(sourceBytes.AsSpan(16, 4));
            var headerHeight = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(sourceBytes.AsSpan(20, 4));
            if (headerWidth == 0 || headerHeight == 0 || (ulong)headerWidth * headerHeight > 64 * 1024 * 1024)
                throw new InvalidDataException($"Optimized Web PNG exceeds the decoded pixel bound: {logicalPath}");
            using var input = new MemoryStream(sourceBytes, writable: false);
            using var source = Image.FromStream(input, useEmbeddedColorManagement: false, validateImageData: true);
            if (source.Width < 1 || source.Height < 1 || (long)source.Width * source.Height > 64 * 1024 * 1024)
                throw new InvalidDataException($"Optimized Web PNG exceeds the decoded pixel bound: {logicalPath}");

            // Small icons/atlases remain exact. Larger images preserve aspect ratio.
            if (Math.Max(source.Width, source.Height) <= 256)
                continue;
            var ratio = Math.Min(1.0 / divisor, maximumEdge / (double)Math.Max(source.Width, source.Height));
            var width = Math.Max(1, (int)Math.Round(source.Width * ratio));
            var height = Math.Max(1, (int)Math.Round(source.Height * ratio));
            using var resized = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(resized))
            using (var attributes = new ImageAttributes())
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(source, new Rectangle(0, 0, width, height), 0, 0,
                    source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            using var encoded = new MemoryStream();
            resized.Save(encoded, ImageFormat.Png);
            var candidate = encoded.ToArray();
            if (candidate.Length >= sourceBytes.Length)
                continue;
            File.WriteAllBytes(path, candidate);
            originalSizes.Add(logicalPath, new[] { source.Width, source.Height });
            originalBytes += sourceBytes.Length;
            publishedBytes += candidate.Length;
            images.Add(new
            {
                path = logicalPath, sourceWidth = source.Width, sourceHeight = source.Height,
                width, height, sourceBytes = sourceBytes.Length, bytes = candidate.Length,
                sourceSha256 = Convert.ToHexString(SHA256.HashData(sourceBytes)),
                sha256 = Convert.ToHexString(SHA256.HashData(candidate))
            });
        }
        File.WriteAllText(Path.Combine(stagingDirectory, ManifestName), JsonSerializer.Serialize(new
        {
            schemaVersion = 1, quality = quality.ToString(), maximumEdge,
            policy = "Web-only PNG downsampling; alpha retained, logical image coordinates unchanged; small/non-beneficial images untouched.",
            originalBytes, publishedBytes, images
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Web Optimized {quality}: {images.Count} PNGs, {originalBytes} -> {publishedBytes} bytes; canonical/native assets unchanged.");
        return originalSizes;
    }
}
