using System.Globalization;
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
            // Fieldless classes are opaque pointer targets. C rejects an empty
            // struct body; keep its forward declaration without inventing storage.
            if (type is ClassTypeSymbol { InstanceSize: 0 }) continue;
            output.Append($"struct {Name(type)} {{\n");
            var offset = 0;
            var fields = Fields(type).OrderBy(field => field.Offset).ToArray();
            var names = FieldNames(fields);
            for (var ordinal = 0; ordinal < fields.Length; ordinal++)
            {
                var field = fields[ordinal];
                if (field.Offset > offset) output.Append($"unsigned char pad{offset}[{field.Offset - offset}];\n");
                output.Append(FieldType(field.Type)).Append(' ').Append(names[ordinal]);
                if (field.IsArray) output.Append('[').Append(field.ElementCount).Append(']');
                output.Append("; /* SMILE: ").Append(field.Name).Append(" */\n");
                offset = field.Offset + field.Type.Size * field.ElementCount;
            }
            var size = type is ClassTypeSymbol instance ? instance.InstanceSize : type.Size;
            if (size > offset) output.Append($"unsigned char pad{offset}[{size - offset}];\n");
            output.Append("};\n");
        }
        return output.ToString();
    }

    private static string[] FieldNames(IInstanceFieldSymbol[] fields)
    {
        static bool IsAscii(char value) => char.IsAsciiLetterOrDigit(value) || value == '_';
        var names = fields.Select(field => "Field_" +
            string.Concat(field.Name.Select(value => IsAscii(value) ? value : '_'))).ToArray();
        // Reserve every original ASCII spelling before disambiguating Unicode,
        // including user fields that look like a generated ordinal suffix.
        var used = new HashSet<string>(fields.Where(field => field.Name.All(IsAscii))
            .Select(field => "Field_" + field.Name), StringComparer.Ordinal);
        for (var ordinal = 0; ordinal < fields.Length; ordinal++)
        {
            if (fields[ordinal].Name.All(IsAscii)) continue;
            names[ordinal] += "_" + ordinal.ToString(CultureInfo.InvariantCulture);
            while (!used.Add(names[ordinal])) names[ordinal] += "_";
        }
        return names;
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
