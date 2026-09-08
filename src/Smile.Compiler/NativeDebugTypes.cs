using System.Security.Cryptography;
using System.Text;
using Smile.Language;

namespace Smile.Compiler;

/// <summary>Debugger-only aggregate views use the shared field offsets and types.</summary>
internal static class NativeDebugTypes
{
    public static string Name(SmileType type) => "SmileDebug_" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(type.RuntimeIdentity)))[..16];

    public static string Declarations(IEnumerable<SmileType> roots)
    {
        var visited = new HashSet<SmileType>();
        var ordered = new List<SmileType>();
        void Visit(SmileType type)
        {
            if (type is not InstanceTypeSymbol || !visited.Add(type)) return;
            foreach (var field in Fields(type)) Visit(field.Type);
            ordered.Add(type);
        }
        foreach (var root in roots) Visit(root);
        var output = new StringBuilder();
        foreach (var type in ordered) output.Append($"typedef struct {Name(type)} {Name(type)};\n");
        foreach (var type in ordered)
        {
            output.Append($"struct {Name(type)} {{\n");
            var offset = 0;
            foreach (var field in Fields(type).OrderBy(field => field.Offset))
            {
                if (field.Offset > offset) output.Append($"unsigned char pad{offset}[{field.Offset - offset}];\n");
                var name = "Field_" + string.Concat(field.Name.Select(c => char.IsAsciiLetterOrDigit(c) || c == '_' ? c : '_'));
                output.Append(FieldType(field.Type)).Append(' ').Append(name);
                if (field.IsArray) output.Append('[').Append(field.ElementCount).Append(']');
                output.Append(";\n");
                offset = field.Offset + field.Type.Size * field.ElementCount;
            }
            var size = type is ClassTypeSymbol instance ? instance.InstanceSize : type.Size;
            if (size > offset) output.Append($"unsigned char pad{offset}[{size - offset}];\n");
            output.Append("};\n");
        }
        return output.ToString();
    }

    private static IEnumerable<IInstanceFieldSymbol> Fields(SmileType type) => type switch
    {
        RecordTypeSymbol record => record.Fields,
        ClassTypeSymbol instance => instance.Fields,
        _ => Array.Empty<IInstanceFieldSymbol>()
    };

    private static string FieldType(SmileType type) => type.Kind switch
    {
        SmileTypeKind.Double => "double",
        SmileTypeKind.Number or SmileTypeKind.Boolean or SmileTypeKind.Enum => "long long",
        SmileTypeKind.Text => "const SmileDebugText*",
        SmileTypeKind.Record => Name(type),
        SmileTypeKind.Class => "const " + Name(type) + "*",
        _ => "const void*"
    };
}
