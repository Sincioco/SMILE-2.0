using System;
using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

public enum SmileCompletionKind
{
    Keyword,
    BuiltInFunction,
    BuiltInConstant,
    Variable,
    Array,
    Subroutine,
    Function
}

public sealed class SmileCompletion
{
    public SmileCompletion(string displayText, string description, SmileCompletionKind kind)
    {
        DisplayText = displayText;
        Description = description;
        Kind = kind;
    }

    public string DisplayText { get; }
    public string Description { get; }
    public SmileCompletionKind Kind { get; }
}

public static class SmileCompletionService
{
    private static readonly IReadOnlyList<SmileCompletion> LanguageCompletions = CreateLanguageCompletions();

    public static IReadOnlyList<SmileCompletion> GetCompletions(SmileAnalysisResult analysis, int position)
        => GetCompletions(analysis, analysis?.SyntaxTree ?? throw new ArgumentNullException(nameof(analysis)), position);

    public static IReadOnlyList<SmileCompletion> GetCompletions(SmileAnalysisResult analysis, string filePath, int position)
    {
        if (analysis == null)
            throw new ArgumentNullException(nameof(analysis));
        return GetCompletions(analysis, analysis.GetSyntaxTree(filePath), position);
    }

    public static IReadOnlyList<SmileCompletion> GetCompletions(SmileAnalysisResult analysis, SyntaxTree syntaxTree, int position)
    {
        if (analysis == null)
            throw new ArgumentNullException(nameof(analysis));
        if (syntaxTree == null)
            throw new ArgumentNullException(nameof(syntaxTree));

        var completions = new Dictionary<string, SmileCompletion>(StringComparer.OrdinalIgnoreCase);
        foreach (var completion in LanguageCompletions)
            completions[completion.DisplayText] = completion;

        foreach (var symbol in analysis.SemanticModel.Symbols.Values)
        {
            if (!IsDeclarationBeingTyped(symbol, syntaxTree.Source, position))
                completions[symbol.Name] = VariableCompletion(symbol);
        }

        var currentRoutine = analysis.SemanticModel.Routines.Values.FirstOrDefault(routine =>
            ReferenceEquals(routine.Source, syntaxTree.Source) &&
            routine.Declaration.Span.Start <= position && position <= routine.Declaration.Span.End);
        if (currentRoutine != null)
        {
            foreach (var symbol in currentRoutine.LocalSymbols.Values)
            {
                if (!IsDeclarationBeingTyped(symbol, syntaxTree.Source, position))
                    completions[symbol.Name] = VariableCompletion(symbol);
            }
        }

        foreach (var routine in analysis.SemanticModel.Routines.Values)
        {
            var kind = routine.IsFunction ? SmileCompletionKind.Function : SmileCompletionKind.Subroutine;
            var keyword = routine.IsFunction ? "FUNCTION" : "SUB";
            var parameters = string.Join(", ", routine.Parameters.Select(parameter => parameter.Name));
            completions[routine.Name] = new SmileCompletion(
                routine.Name,
                $"{keyword} {routine.Name}({parameters})",
                kind);
        }

        return completions.Values.OrderBy(completion => completion.DisplayText, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<SmileCompletion> CreateLanguageCompletions()
    {
        var completions = new List<SmileCompletion>();
        foreach (var name in SyntaxFacts.GetKeywordTexts())
        {
            var kind = SyntaxFacts.GetKeywordKind(name);
            if (SyntaxFacts.IsBuiltInFunction(kind))
            {
                var parameters = string.Join(", ", SyntaxFacts.GetBuiltInFunctionParameters(kind));
                completions.Add(new SmileCompletion(name, $"Built-in function {name}({parameters})", SmileCompletionKind.BuiltInFunction));
            }
            else if (SyntaxFacts.IsKeyword(kind))
            {
                completions.Add(new SmileCompletion(name, $"SMILE keyword {name}", SmileCompletionKind.Keyword));
            }
            else if (SyntaxFacts.IsBuiltInConstant(kind))
            {
                completions.Add(new SmileCompletion(name, $"Built-in constant {name}", SmileCompletionKind.BuiltInConstant));
            }
        }
        return completions.OrderBy(completion => completion.DisplayText, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static SmileCompletion VariableCompletion(VariableSymbol symbol)
    {
        var type = symbol.Type.ToString().ToUpperInvariant();
        if (symbol.IsArray)
        {
            var dimensions = string.Join(", ", symbol.ArrayDimensions);
            return new SmileCompletion(symbol.Name, $"{type} array {symbol.Name}[{dimensions}]", SmileCompletionKind.Array);
        }
        var noun = symbol.IsConstant ? "constant" : "variable";
        return new SmileCompletion(symbol.Name, $"{type} {noun} {symbol.Name}", SmileCompletionKind.Variable);
    }

    private static bool IsDeclarationBeingTyped(VariableSymbol symbol, SourceText source, int position) =>
        ReferenceEquals(symbol.Source, source) &&
        symbol.DeclarationSpan.Start <= position && position <= symbol.DeclarationSpan.End;
}
