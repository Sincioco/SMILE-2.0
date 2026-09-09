using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Smile.Language;

/// <summary>The shared binary64 contract, independent of backend storage and calling conventions.</summary>
public static class DoubleSemantics
{
    // Numeric diagnostics are separate from the established SML3800-series project identities.
    public const string InvalidLiteralDiagnosticCode = "SML3900";
    public const string TypeMismatchDiagnosticCode = "SML3901";
    public const string CheckedFailureDiagnosticCode = "SML3902";

    public static int OperationCode(SyntaxKind kind) => kind switch
    {
        SyntaxKind.PlusToken => 1, SyntaxKind.MinusToken => 2, SyntaxKind.StarToken => 3,
        SyntaxKind.SlashToken => 4, SyntaxKind.AbsKeyword => 5, SyntaxKind.MinKeyword => 6,
        SyntaxKind.MaxKeyword => 7, SyntaxKind.ClampKeyword => 8, SyntaxKind.SqrtKeyword => 9,
        SyntaxKind.SinKeyword => 10, SyntaxKind.CosKeyword => 11, SyntaxKind.Atan2Keyword => 12,
        SyntaxKind.FloorKeyword => 13, SyntaxKind.CeilingKeyword => 14, SyntaxKind.TruncateKeyword => 15,
        SyntaxKind.RoundKeyword => 16, _ => 0
    };
    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    public static bool IsIntrinsic(SyntaxKind kind) =>
        kind >= SyntaxKind.ToDoubleKeyword && kind <= SyntaxKind.TextToDoubleKeyword;
    public static bool IsPolymorphic(SyntaxKind kind) =>
        kind is SyntaxKind.AbsKeyword or SyntaxKind.MinKeyword or SyntaxKind.MaxKeyword;
    public static SmileType LiteralType(object value) => value is double ? SmileType.Double
        : value is bool ? SmileType.Boolean : value is string ? SmileType.Text : SmileType.Number;
    public static SmileType ResultType(SyntaxKind kind) => kind == SyntaxKind.ToNumberKeyword
        ? SmileType.Number : kind == SyntaxKind.TextFromDoubleKeyword ? SmileType.Text : SmileType.Double;
    public static SmileType ArgumentType(SyntaxKind kind) => kind == SyntaxKind.ToDoubleKeyword
        ? SmileType.Number : kind == SyntaxKind.TextToDoubleKeyword ? SmileType.Text : SmileType.Double;
    public static IReadOnlyList<string> Parameters(SyntaxKind kind) => kind switch
    {
        SyntaxKind.ClampKeyword => new[] { "value", "minimum", "maximum" },
        SyntaxKind.Atan2Keyword => new[] { "y", "x" },
        _ => new[] { "value" }
    };
    public static string Signature(SyntaxKind kind)
    {
        var names = SyntaxFacts.GetBuiltInFunctionParameters(kind);
        var type = IsPolymorphic(kind) ? "Number Or Double" : ArgumentType(kind).Name;
        var parameters = new List<string>();
        foreach (var name in names)
            parameters.Add(char.ToUpperInvariant(name[0]) + name.Substring(1) + " As " + type);
        return SyntaxFacts.GetText(kind) + "(" + string.Join(", ", parameters) + ") As " +
            (IsPolymorphic(kind) ? "the same numeric type" : ResultType(kind).Name);
    }
    public static bool IsArithmetic(SyntaxKind kind) => kind is SyntaxKind.PlusToken or
        SyntaxKind.MinusToken or SyntaxKind.StarToken or SyntaxKind.SlashToken;
    public static bool IsComparison(SyntaxKind kind) => kind is SyntaxKind.EqualsToken or
        SyntaxKind.NotEqualsToken or SyntaxKind.LessToken or SyntaxKind.LessOrEqualsToken or
        SyntaxKind.GreaterToken or SyntaxKind.GreaterOrEqualsToken;

    public static string Format(double value)
    {
        if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (value == 0) return BitConverter.DoubleToInt64Bits(value) < 0 ? "-0.0" : "0.0";
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.IndexOf('.') < 0 && text.IndexOf('E') < 0 && text.IndexOf('e') < 0 ? text + ".0" : text;
    }

    public static bool TryParse(string text, out double value)
    {
        value = 0;
        if (!Regex.IsMatch(text, @"\A[\x09-\x0D ]*[+-]?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?[\x09-\x0D ]*\z"))
            return false;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !IsFinite(value))
            return false;
        if (value == 0 && text.TrimStart().StartsWith("-", StringComparison.Ordinal))
            value = BitConverter.Int64BitsToDouble(long.MinValue);
        return true;
    }

    public static bool TryBinary(SyntaxKind kind, double left, double right, out object value)
    {
        value = 0.0;
        if (IsComparison(kind))
        {
            value = kind switch
            {
                SyntaxKind.EqualsToken => left == right, SyntaxKind.NotEqualsToken => left != right,
                SyntaxKind.LessToken => left < right, SyntaxKind.LessOrEqualsToken => left <= right,
                SyntaxKind.GreaterToken => left > right, _ => left >= right
            };
            return true;
        }
        if (!IsArithmetic(kind) || kind == SyntaxKind.SlashToken && right == 0) return false;
        var result = kind switch
        {
            SyntaxKind.PlusToken => left + right, SyntaxKind.MinusToken => left - right,
            SyntaxKind.StarToken => left * right, _ => left / right
        };
        value = result;
        return IsFinite(result);
    }

    public static bool TryIntrinsic(SyntaxKind kind, IReadOnlyList<object> values, out object value)
    {
        value = 0.0;
        if (kind == SyntaxKind.ToDoubleKeyword && values.Count == 1 && values[0] is long integer)
        { value = (double)integer; return true; }
        if (kind == SyntaxKind.TextToDoubleKeyword && values.Count == 1 && values[0] is string text)
        { var ok = TryParse(text, out var parsed); value = parsed; return ok; }
        if (values.Count == 0 || values[0] is not double a) return false;
        if (kind == SyntaxKind.TextFromDoubleKeyword) { value = Format(a); return true; }
        if (kind == SyntaxKind.ToNumberKeyword)
        {
            var truncated = Math.Truncate(a);
            if (!IsFinite(a) || truncated < -9223372036854775808.0 || truncated >= 9223372036854775808.0)
                return false;
            value = (long)truncated; return true;
        }
        foreach (var item in values) if (item is not double) return false;
        var b = values.Count > 1 ? (double)values[1] : 0.0;
        var c = values.Count > 2 ? (double)values[2] : 0.0;
        if (kind == SyntaxKind.ClampKeyword && b > c || kind == SyntaxKind.SqrtKeyword && a < 0) return false;
        var result = kind switch
        {
            SyntaxKind.AbsKeyword => Math.Abs(a), SyntaxKind.MinKeyword => a <= b ? a : b,
            SyntaxKind.MaxKeyword => a >= b ? a : b, SyntaxKind.ClampKeyword => a < b ? b : a > c ? c : a,
            SyntaxKind.SqrtKeyword => Math.Sqrt(a), SyntaxKind.SinKeyword => Math.Sin(a),
            SyntaxKind.CosKeyword => Math.Cos(a), SyntaxKind.Atan2Keyword => Math.Atan2(a, b),
            SyntaxKind.FloorKeyword => Math.Floor(a), SyntaxKind.CeilingKeyword => Math.Ceiling(a),
            SyntaxKind.TruncateKeyword => Math.Truncate(a), SyntaxKind.RoundKeyword => Math.Round(a, MidpointRounding.ToEven),
            _ => double.NaN
        };
        value = result;
        return IsFinite(result);
    }
}
