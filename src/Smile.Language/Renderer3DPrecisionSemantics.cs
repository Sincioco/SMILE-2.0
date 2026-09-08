using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

/// <summary>Typed compiler/runtime boundary; scene and resource ownership stay in Simple3D.</summary>
public static class Renderer3DPrecisionSemantics
{
    public static bool IsIntrinsic(SyntaxKind kind) => kind is SyntaxKind.Renderer3DDoubleKeyword or
        SyntaxKind.Renderer3DDoubleValueKeyword;
    public static SmileType ResultType(SyntaxKind kind) => kind == SyntaxKind.Renderer3DDoubleValueKeyword
        ? SmileType.Double : SmileType.Number;
    public static SmileType ArgumentType(SyntaxKind kind, int index) =>
        kind == SyntaxKind.Renderer3DDoubleKeyword && index >= 2 ? SmileType.Double : SmileType.Number;
    public static IReadOnlyList<string> Parameters(SyntaxKind kind) => kind == SyntaxKind.Renderer3DDoubleKeyword
        ? new[] { "command", "resource", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l" }
        : new[] { "command", "resource", "index", "component" };
    public static string Signature(SyntaxKind kind) => SyntaxFacts.GetText(kind) + "(" +
        string.Join(", ", Parameters(kind).Select((name, index) => char.ToUpperInvariant(name[0]) +
            name.Substring(1) + " As " + ArgumentType(kind, index).Name)) + ") As " + ResultType(kind).Name;
}
