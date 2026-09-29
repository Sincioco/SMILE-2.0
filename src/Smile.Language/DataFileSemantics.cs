using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

/// <summary>Application-owned, asynchronous transfer of a checked Save Data envelope.</summary>
public static class DataFileSemantics
{
    public static bool IsIntrinsic(SyntaxKind kind) => kind >= SyntaxKind.DataFileStartKeyword &&
        kind <= SyntaxKind.LocalTimestampKeyword;
    public static SmileType ResultType(SyntaxKind kind) =>
        kind is SyntaxKind.DataFileMessageKeyword or SyntaxKind.LocalTimestampKeyword ? SmileType.Text : SmileType.Number;
    public static IReadOnlyList<string> Parameters(SyntaxKind kind) => kind switch
    {
        SyntaxKind.DataFileStartKeyword => new[] { "saving", "key", "path" },
        SyntaxKind.LocalTimestampKeyword => System.Array.Empty<string>(),
        _ => new[] { "job" }
    };
    public static SmileType ArgumentType(SyntaxKind kind, int index) => kind == SyntaxKind.DataFileStartKeyword
        ? (index == 0 ? SmileType.Boolean : SmileType.Text) : SmileType.Number;
    public static string Signature(SyntaxKind kind) => SyntaxFacts.GetText(kind) + "(" +
        string.Join(", ", Parameters(kind).Select((name, index) => char.ToUpperInvariant(name[0]) +
            name.Substring(1) + " As " + ArgumentType(kind, index).Name)) + ") As " + ResultType(kind).Name;
    public static string NativeName(SyntaxKind kind) => kind switch
    {
        SyntaxKind.DataFileStartKeyword => "smile_data_file_start",
        SyntaxKind.DataFileStatusKeyword => "smile_data_file_status",
        SyntaxKind.DataFileMessageKeyword => "smile_data_file_message",
        SyntaxKind.DataFileProgressKeyword => "smile_data_file_progress",
        _ => "smile_local_timestamp"
    };
}
