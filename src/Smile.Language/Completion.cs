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
    Module,
    Type,
    Field
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

        var afterAs = IsAfterAs(syntaxTree.Source.Text, position);
        var qualifiedAlias = AliasBeforeDot(syntaxTree.Source.Text, position);
        var qualifiedTypeContext = qualifiedAlias != null && IsQualifiedTypeContext(syntaxTree.Source.Text, position, qualifiedAlias);
        if (qualifiedAlias != null && analysis.SemanticModel.GetImports(syntaxTree.Source)
            .TryGetValue(qualifiedAlias, out var importedModule))
        {
            return importedModule.PublicMembers.Where(member => !(afterAs || qualifiedTypeContext) || member.Kind == SmileModuleMemberKind.Type)
                .OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
                .Select(MemberCompletion).ToArray();
        }

        var fieldCompletions = TryGetFieldCompletions(analysis, syntaxTree, position);
        if (fieldCompletions != null)
            return fieldCompletions;

        if (afterAs)
        {
            var types = new Dictionary<string, SmileCompletion>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in new[] { "BOOLEAN", "NUMBER", "TEXT" })
                types[name] = new SmileCompletion(name, $"SMILE built-in type {name}", SmileCompletionKind.Type);
            foreach (var type in analysis.SemanticModel.Types.Values.Where(type => type.ModuleName == null ||
                         type.Source != null && ReferenceEquals(type.Source, syntaxTree.Source)))
                types[type.Name] = TypeCompletion(type);
            foreach (var import in analysis.SemanticModel.GetImports(syntaxTree.Source))
                types[import.Key] = new SmileCompletion(import.Key,
                    $"Import alias for record types in module {import.Value.Name}", SmileCompletionKind.Module);
            return types.Values.OrderBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase).ToArray();
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

        foreach (var type in analysis.SemanticModel.Types.Values.Where(type =>
                     type.ModuleName == null || string.Equals(type.ModuleName, currentModule?.Name,
                         StringComparison.OrdinalIgnoreCase)))
            completions[type.Name] = TypeCompletion(type);

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
        if (member.Type != null)
            return TypeCompletion(member.Type);
        return new SmileCompletion(member.Name, $"Public module member {member.Name}", SmileCompletionKind.Variable);
    }

    private static SmileCompletion TypeCompletion(RecordTypeSymbol type)
    {
        var fields = string.Join(", ", type.Fields.Select(field => $"{field.Name} AS {field.Type.Name}"));
        var provider = type.ModuleName == null ? string.Empty :
            $" from module {type.ModuleName} ({type.ProviderIdentity})";
        return new SmileCompletion(type.Name, $"TYPE {type.Name} ({fields}){provider}", SmileCompletionKind.Type);
    }

    private static IReadOnlyList<SmileCompletion>? TryGetFieldCompletions(SmileAnalysisResult analysis,
        SyntaxTree syntaxTree, int position)
    {
        var end = Math.Min(position, syntaxTree.Source.Text.Length);
        var start = end;
        while (start > 0 && syntaxTree.Source.Text[start - 1] is not ('\r' or '\n' or ' ' or '\t' or '=' or '(' or ','))
            start--;
        var suffix = syntaxTree.Source.Text.Substring(start, end - start).Trim();
        if (!suffix.EndsWith(".", StringComparison.Ordinal))
            return null;
        suffix = suffix.Substring(0, suffix.Length - 1);
        var parts = suffix.Split('.');
        if (parts.Length == 0)
            return null;
        var root = parts[0];
        var bracket = root.IndexOf('[');
        if (bracket >= 0)
            root = root.Substring(0, bracket);
        var routine = analysis.SemanticModel.Routines.Values.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Source, syntaxTree.Source) && candidate.Declaration.Span.Start <= position &&
            position <= candidate.Declaration.Span.End);
        SmileType type;
        var firstFieldIndex = 1;
        if (analysis.SemanticModel.GetImports(syntaxTree.Source).TryGetValue(root, out var imported) && parts.Length >= 2 &&
            imported.Members.TryGetValue(parts[1], out var importedMember) &&
            importedMember.Visibility == ModuleVisibility.Public && importedMember.Variable != null)
        {
            type = importedMember.Variable.Type;
            firstFieldIndex = 2;
        }
        else
        {
            if (!analysis.SemanticModel.TryResolveVariable(root, routine?.Name, out var symbol))
                return null;
            type = symbol.Type;
        }
        for (var index = firstFieldIndex; index < parts.Length; index++)
        {
            if (type is not RecordTypeSymbol record || !record.TryGetField(parts[index], out var field))
                return Array.Empty<SmileCompletion>();
            type = field.Type;
        }
        if (type is not RecordTypeSymbol target)
            return Array.Empty<SmileCompletion>();
        return target.Fields.OrderBy(field => field.Ordinal).Select(field => new SmileCompletion(field.Name,
            $"{field.Name} AS {field.Type.Name} field of TYPE {target.Name}", SmileCompletionKind.Field)).ToArray();
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

    private static bool IsQualifiedTypeContext(string text, int position, string alias)
    {
        var start = Math.Min(position, text.Length);
        while (start > 0 && text[start - 1] is not ('\r' or '\n')) start--;
        var before = text.Substring(start, Math.Min(position, text.Length) - start);
        var marker = before.LastIndexOf(" AS ", StringComparison.OrdinalIgnoreCase);
        if (marker < 0 && before.TrimStart().StartsWith("AS ", StringComparison.OrdinalIgnoreCase))
            marker = before.IndexOf("AS ", StringComparison.OrdinalIgnoreCase) - 1;
        var tail = marker < 0 ? string.Empty : before.Substring(marker + 4).TrimStart();
        return tail.StartsWith(alias + ".", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeclarationBeingTyped(VariableSymbol symbol, SourceText source, int position) =>
        ReferenceEquals(symbol.Source, source) &&
        symbol.DeclarationSpan.Start <= position && position <= symbol.DeclarationSpan.End;
}
