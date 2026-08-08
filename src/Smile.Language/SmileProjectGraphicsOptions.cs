using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Smile.Language;

public enum SmileGraphicsBackend
{
    Auto = 0,
    GDI = 1,
    DirectX = 2
}

public sealed class SmileProjectGraphicsOptions
{
    public SmileProjectGraphicsOptions(SmileGraphicsBackend graphicsBackend, bool vSync)
    {
        GraphicsBackend = graphicsBackend;
        VSync = vSync;
    }

    public SmileGraphicsBackend GraphicsBackend { get; }
    public bool VSync { get; }

    public static SmileProjectGraphicsOptions Parse(XElement? propertyGroup)
    {
        var backendText = Value(propertyGroup, "GraphicsBackend");
        var vSyncText = Value(propertyGroup, "VSync");
        var backend = SmileGraphicsBackend.Auto;
        var vSync = true;

        if (backendText != null &&
            (!Enum.TryParse(backendText, true, out backend) || !Enum.IsDefined(typeof(SmileGraphicsBackend), backend)))
        {
            throw new InvalidDataException(
                $"Unknown GraphicsBackend value '{backendText}'. Expected Auto, GDI, or DirectX.");
        }

        if (vSyncText != null && !bool.TryParse(vSyncText, out vSync))
        {
            throw new InvalidDataException(
                $"Unknown VSync value '{vSyncText}'. Expected true or false.");
        }

        return new SmileProjectGraphicsOptions(backend, vSync);
    }

    public static SmileProjectGraphicsOptions Load(string projectPath)
    {
        var document = XDocument.Load(projectPath, LoadOptions.SetLineInfo);
        var root = document.Root;
        if (root == null || root.Name.LocalName != "SmileProject")
            throw new InvalidDataException("A .smileproj file must have a SmileProject root element.");
        var properties = root.Elements().FirstOrDefault(element => element.Name.LocalName == "PropertyGroup");
        return Parse(properties);
    }

    private static string? Value(XElement? group, string name) =>
        group?.Elements().FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim()
            is { Length: > 0 } value ? value : null;
}
