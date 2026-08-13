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
    Function,
    Module
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

        var qualifiedAlias = AliasBeforeDot(syntaxTree.Source.Text, position);
        if (qualifiedAlias != null && analysis.SemanticModel.GetImports(syntaxTree.Source)
            .TryGetValue(qualifiedAlias, out var importedModule))
        {
            return importedModule.PublicMembers.OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
                .Select(MemberCompletion).ToArray();
        }

        if (IsAfterAs(syntaxTree.Source.Text, position))
        {
            return new[] { "BOOLEAN", "NUMBER", "TEXT" }.Select(name =>
                new SmileCompletion(name, $"SMILE built-in type {name}", SmileCompletionKind.Keyword)).ToArray();
        }

        var completions = new Dictionary<string, SmileCompletion>(StringComparer.OrdinalIgnoreCase);
        foreach (var completion in LanguageCompletions)
            completions[completion.DisplayText] = completion;

        var currentModule = analysis.SemanticModel.Modules.Values.FirstOrDefault(module =>
            module.SyntaxTrees.Any(tree => ReferenceEquals(tree.Source, syntaxTree.Source)));
        foreach (var symbol in analysis.SemanticModel.Symbols.Values)
        {
            if ((symbol.ModuleName == null || string.Equals(symbol.ModuleName, currentModule?.Name,
                    StringComparison.OrdinalIgnoreCase)) &&
                !IsDeclarationBeingTyped(symbol, syntaxTree.Source, position))
                completions[symbol.Name] = VariableCompletion(symbol);
        }

        var currentRoutine = analysis.SemanticModel.Routines.Values.FirstOrDefault(routine =>
            ReferenceEquals(routine.Source, syntaxTree.Source) &&
            routine.Declaration.Span.Start <= position && position <= routine.Declaration.Span.End);
        if (currentRoutine != null)
        {
            foreach (var symbol in currentRoutine.LocalSymbols.Values)
            {
                if ((symbol.IsParameter || symbol.DeclarationSpan.Start <= position) &&
                    !IsDeclarationBeingTyped(symbol, syntaxTree.Source, position))
                    completions[symbol.Name] = VariableCompletion(symbol);
            }
        }

        foreach (var routine in analysis.SemanticModel.Routines.Values.Where(routine =>
                     routine.ModuleName == null || string.Equals(routine.ModuleName, currentModule?.Name,
                         StringComparison.OrdinalIgnoreCase)))
        {
            var kind = routine.IsFunction ? SmileCompletionKind.Function : SmileCompletionKind.Subroutine;
            var keyword = routine.IsFunction ? "FUNCTION" : "SUB";
            var parameters = string.Join(", ", routine.Parameters.Select(DescribeParameter));
            var returnType = routine.IsFunction ? $" AS {routine.ReturnType.ToString().ToUpperInvariant()}" : string.Empty;
            completions[routine.Name] = new SmileCompletion(
                routine.Name,
                $"{keyword} {routine.Name}({parameters}){returnType}",
                kind);
        }

        foreach (var import in analysis.SemanticModel.GetImports(syntaxTree.Source))
            completions[import.Key] = new SmileCompletion(import.Key,
                $"Import alias for module {import.Value.Name} from {import.Value.ProviderIdentity}", SmileCompletionKind.Module);

        if (IsAfterImport(syntaxTree.Source.Text, position))
        {
            completions.Clear();
            foreach (var module in analysis.SemanticModel.Modules.Values.Where(module =>
                         analysis.DependencyContext.CanAccess(syntaxTree.ProviderIdentity,
                             module.ProviderIdentity)))
                completions[module.Name] = new SmileCompletion(module.Name,
                    $"SMILE module from {module.ProviderIdentity}", SmileCompletionKind.Module);
            completions["AS"] = new SmileCompletion("AS", "Required import alias keyword", SmileCompletionKind.Keyword);
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
            return new SmileCompletion(symbol.Name, DescribeProvider(symbol, $"{type} array {symbol.Name}[{dimensions}]"), SmileCompletionKind.Array);
        }
        var noun = symbol.IsConstant ? "constant" : "variable";
        return new SmileCompletion(symbol.Name, DescribeProvider(symbol, $"{type} {noun} {symbol.Name}"), SmileCompletionKind.Variable);
    }

    private static SmileCompletion MemberCompletion(SmileModuleMember member)
    {
        if (member.Routine != null)
        {
            var parameters = string.Join(", ", member.Routine.Parameters.Select(DescribeParameter));
            var keyword = member.Routine.IsFunction ? "FUNCTION" : "SUB";
            var returnType = member.Routine.IsFunction
                ? $" AS {member.Routine.ReturnType.ToString().ToUpperInvariant()}" : string.Empty;
            return new SmileCompletion(member.Name,
                $"{keyword} {member.Name}({parameters}){returnType} from module {member.Routine.ModuleName} ({member.Routine.ProviderIdentity})",
                member.Routine.IsFunction ? SmileCompletionKind.Function : SmileCompletionKind.Subroutine);
        }
        if (member.Variable != null)
            return VariableCompletion(member.Variable);
        return new SmileCompletion(member.Name, $"Public module member {member.Name}", SmileCompletionKind.Variable);
    }

    private static string DescribeProvider(VariableSymbol symbol, string description) => symbol.ModuleName == null
        ? description
        : $"{description} from module {symbol.ModuleName} ({symbol.ProviderIdentity})";

    private static string DescribeParameter(VariableSymbol parameter)
    {
        var mode = parameter.ParameterMode == ParameterPassingMode.ByRef ? "BYREF " : string.Empty;
        return $"{mode}{parameter.Name} AS {parameter.Type.ToString().ToUpperInvariant()}";
    }

    private static string? AliasBeforeDot(string text, int position)
    {
        var index = Math.Min(position, text.Length) - 1;
        while (index >= 0 && char.IsWhiteSpace(text[index])) index--;
        while (index >= 0 && (char.IsLetterOrDigit(text[index]) || text[index] == '_')) index--;
        if (index < 0 || text[index] != '.')
            return null;
        var end = index--;
        while (index >= 0 && (char.IsLetterOrDigit(text[index]) || text[index] == '_')) index--;
        return end == index + 1 ? null : text.Substring(index + 1, end - index - 1);
    }

    private static bool IsAfterImport(string text, int position)
    {
        var start = Math.Min(position, text.Length);
        while (start > 0 && text[start - 1] is not ('\r' or '\n')) start--;
        return text.Substring(start, Math.Min(position, text.Length) - start)
            .TrimStart().StartsWith("IMPORT ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAfterAs(string text, int position)
    {
        var start = Math.Min(position, text.Length);
        while (start > 0 && text[start - 1] is not ('\r' or '\n')) start--;
        var before = text.Substring(start, Math.Min(position, text.Length) - start).TrimEnd();
        return before.EndsWith(" AS", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(before, "AS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeclarationBeingTyped(VariableSymbol symbol, SourceText source, int position) =>
        ReferenceEquals(symbol.Source, source) &&
        symbol.DeclarationSpan.Start <= position && position <= symbol.DeclarationSpan.End;
}
