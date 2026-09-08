using System.Globalization;
using System.Xml.Linq;

namespace Smile.Compiler;

// Created during compilation, never from the generated program's launch clock.
internal sealed record StartupBuildMetadata(string Version, DateTimeOffset CompiledAt)
{
    internal string Display => $"Compiled {CompiledAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)} | SMILE {Version}";

    internal static StartupBuildMetadata Create()
    {
        using var manifest = typeof(StartupBuildMetadata).Assembly.GetManifestResourceStream(
            "Smile.Compiler.ProductManifest.xml")
            ?? throw new InvalidOperationException("Missing authoritative SMILE product manifest.");
        var identity = XDocument.Load(manifest).Descendants().Single(element => element.Name.LocalName == "Identity");
        return new StartupBuildMetadata((string?)identity.Attribute("Version")
            ?? throw new InvalidDataException("The SMILE product manifest has no version."), DateTimeOffset.Now);
    }

    internal static byte[] LogoBytes()
    {
        using var stream = typeof(StartupBuildMetadata).Assembly.GetManifestResourceStream(
            "Smile.Compiler.StartupLogo.png")
            ?? throw new InvalidOperationException("Missing official SMILE startup logo.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }
}
