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

        var currentModule = analysis.SemanticModel.Modules.Values.FirstOrDefault(module =>
            module.SyntaxTrees.Any(tree => ReferenceEquals(tree.Source, syntaxTree.Source)));
        var afterAs = IsAfterAs(syntaxTree.Source.Text, position);

        var fieldCompletions = TryGetFieldCompletions(analysis, syntaxTree, position, currentModule);
        if (fieldCompletions != null)
            return fieldCompletions;

        var qualifiedAlias = AliasBeforeDot(syntaxTree.Source.Text, position);
        var qualifiedTypeContext = qualifiedAlias != null && IsQualifiedTypeContext(syntaxTree.Source.Text, position, qualifiedAlias);
        if (qualifiedAlias != null && analysis.SemanticModel.GetImports(syntaxTree.Source)
            .TryGetValue(qualifiedAlias, out var importedModule))
        {
            var typeContext = afterAs || qualifiedTypeContext;
            return importedModule.PublicMembers.Where(member => typeContext
                    ? member.Kind == SmileModuleMemberKind.Type
                    : member.Kind != SmileModuleMemberKind.Type)
                .OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
                .Select(member => MemberCompletion(member, analysis.DependencyContext)).ToArray();
        }

        if (afterAs)
        {
            var types = new Dictionary<string, SmileCompletion>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in new[] { "BOOLEAN", "IMAGE", "NUMBER", "TEXT" })
                types[name] = new SmileCompletion(name, name == "IMAGE"
                    ? "SMILE opaque loaded 2D image resource"
                    : $"SMILE built-in type {name}", SmileCompletionKind.Type);
            var availableTypes = currentModule == null
                ? analysis.SemanticModel.Types.Values.Where(type => type.ModuleName == null)
                : currentModule.Types.Values.Where(member => member.Type != null).Select(member => member.Type!);
            foreach (var type in availableTypes)
                types[type.Name] = TypeCompletion(type, analysis.DependencyContext);
            foreach (var import in analysis.SemanticModel.GetImports(syntaxTree.Source))
                types[import.Key] = new SmileCompletion(import.Key,
                    $"Import alias for record types in module {import.Value.Name}", SmileCompletionKind.Module);
            return types.Values.OrderBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        var completions = new Dictionary<string, SmileCompletion>(StringComparer.OrdinalIgnoreCase);
        foreach (var completion in LanguageCompletions)
            completions[completion.DisplayText] = completion;

        foreach (var symbol in analysis.SemanticModel.Symbols.Values)
        {
            if ((symbol.ModuleName == null || string.Equals(symbol.ModuleName, currentModule?.Name,
                    StringComparison.OrdinalIgnoreCase)) &&
                !IsDeclarationBeingTyped(symbol, syntaxTree.Source, position))
                completions[symbol.Name] = VariableCompletion(symbol, analysis.DependencyContext);
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
                    completions[symbol.Name] = VariableCompletion(symbol, analysis.DependencyContext);
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
                $"Import alias for module {import.Value.Name} from " +
                DescribeProvider(import.Value.ProviderIdentity, analysis.DependencyContext), SmileCompletionKind.Module);

        if (IsAfterImport(syntaxTree.Source.Text, position))
        {
            completions.Clear();
            foreach (var module in analysis.SemanticModel.Modules.Values.Where(module =>
                         analysis.DependencyContext.CanAccess(syntaxTree.ProviderIdentity,
                             module.ProviderIdentity)))
                completions[module.Name] = new SmileCompletion(module.Name,
                    $"SMILE module from {DescribeProvider(module.ProviderIdentity, analysis.DependencyContext)}",
                    SmileCompletionKind.Module);
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
                var description = kind switch
                {
                    SyntaxKind.TextLengthKeyword => "TEXT_LENGTH(Value AS TEXT) AS NUMBER - Unicode scalar count",
                    SyntaxKind.TextCodeAtKeyword => "TEXT_CODE_AT(Value AS TEXT, Index AS NUMBER) AS NUMBER - zero-based Unicode scalar value",
                    SyntaxKind.TextSliceKeyword => "TEXT_SLICE(Value AS TEXT, Start AS NUMBER, Count AS NUMBER) AS TEXT - Unicode scalar slice",
                    _ => $"Built-in function {name}({parameters})"
                };
                completions.Add(new SmileCompletion(name, description, SmileCompletionKind.BuiltInFunction));
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

    private static SmileCompletion VariableCompletion(VariableSymbol symbol,
        SmileCompilationDependencyContext dependencyContext)
    {
        var type = symbol.Type.ToString().ToUpperInvariant();
        if (symbol.IsArray)
        {
            var dimensions = string.Join(", ", symbol.ArrayDimensions);
            return new SmileCompletion(symbol.Name,
                DescribeProvider(symbol, $"{type} array {symbol.Name}[{dimensions}]", dependencyContext),
                SmileCompletionKind.Array);
        }
        var noun = symbol.IsConstant ? "constant" : "variable";
        return new SmileCompletion(symbol.Name,
            DescribeProvider(symbol, $"{type} {noun} {symbol.Name}", dependencyContext), SmileCompletionKind.Variable);
    }

    private static SmileCompletion MemberCompletion(SmileModuleMember member,
        SmileCompilationDependencyContext dependencyContext)
    {
        if (member.Routine != null)
        {
            var parameters = string.Join(", ", member.Routine.Parameters.Select(DescribeParameter));
            var keyword = member.Routine.IsFunction ? "FUNCTION" : "SUB";
            var returnType = member.Routine.IsFunction
                ? $" AS {member.Routine.ReturnType.ToString().ToUpperInvariant()}" : string.Empty;
            var capability = member.Routine.RequiresGameWindow ? " - requires GAME WINDOW" : string.Empty;
            return new SmileCompletion(member.Name,
                $"{keyword} {member.Name}({parameters}){returnType}{capability} from module {member.Routine.ModuleName} " +
                $"({DescribeProvider(member.Routine.ProviderIdentity, dependencyContext)})",
                member.Routine.IsFunction ? SmileCompletionKind.Function : SmileCompletionKind.Subroutine);
        }
        if (member.Variable != null)
            return VariableCompletion(member.Variable, dependencyContext);
        if (member.Type != null)
            return TypeCompletion(member.Type, dependencyContext);
        return new SmileCompletion(member.Name, $"Public module member {member.Name}", SmileCompletionKind.Variable);
    }

    private static SmileCompletion TypeCompletion(RecordTypeSymbol type,
        SmileCompilationDependencyContext dependencyContext)
    {
        var fields = string.Join(", ", type.Fields.Select(field => $"{field.Name} AS {field.Type.Name}"));
        var provider = type.ModuleName == null ? string.Empty :
            $" from module {type.ModuleName} ({DescribeProvider(type.ProviderIdentity, dependencyContext)})";
        return new SmileCompletion(type.Name, $"TYPE {type.Name} ({fields}){provider}", SmileCompletionKind.Type);
    }

    private static IReadOnlyList<SmileCompletion>? TryGetFieldCompletions(SmileAnalysisResult analysis,
        SyntaxTree syntaxTree, int position, ModuleSymbol? currentModule)
    {
        var tokens = syntaxTree.Tokens.Where(token => token.Kind is not (SyntaxKind.EndOfFileToken or SyntaxKind.NewLineToken) &&
                                                      token.Span.End <= position).ToArray();
        if (tokens.Length == 0 || tokens[tokens.Length - 1].Kind != SyntaxKind.DotToken)
            return null;
        var parts = ReceiverParts(tokens, tokens.Length - 1);
        if (parts.Count == 0)
            return null;
        var root = parts[0];
        var routine = analysis.SemanticModel.Routines.Values.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Source, syntaxTree.Source) && candidate.Declaration.Span.Start <= position &&
            position <= candidate.Declaration.Span.End);
        SmileType type;
        var firstFieldIndex = 1;
        if (analysis.SemanticModel.GetImports(syntaxTree.Source).TryGetValue(root, out var imported) && parts.Count >= 2 &&
            imported.Members.TryGetValue(parts[1], out var importedMember) &&
            importedMember.Visibility == ModuleVisibility.Public && importedMember.Variable != null)
        {
            type = importedMember.Variable.Type;
            firstFieldIndex = 2;
        }
        else
        {
            if (!analysis.SemanticModel.TryResolveVariable(root, routine?.Name, out var symbol))
            {
                if (currentModule?.Members.TryGetValue(root, out var moduleMember) != true ||
                    moduleMember.Variable == null)
                    return null;
                symbol = moduleMember.Variable;
            }
            type = symbol.Type;
        }
        for (var index = firstFieldIndex; index < parts.Count; index++)
        {
            if (type is not RecordTypeSymbol record || !record.TryGetField(parts[index], out var field))
                return Array.Empty<SmileCompletion>();
            type = field.Type;
        }
        if (type is not RecordTypeSymbol target)
            return Array.Empty<SmileCompletion>();
        return target.Fields.OrderBy(field => field.Ordinal).Select(field => new SmileCompletion(field.Name,
            $"{field.Name} AS {field.Type.Name} field of TYPE {target.Name}" +
            (target.ModuleName == null ? string.Empty :
                $" from module {target.ModuleName} ({DescribeProvider(target.ProviderIdentity, analysis.DependencyContext)})"),
            SmileCompletionKind.Field)).ToArray();
    }

    private static IReadOnlyList<string> ReceiverParts(IReadOnlyList<SyntaxToken> tokens, int finalDotIndex)
    {
        var parts = new List<string>();
        var index = finalDotIndex - 1;
        while (index >= 0)
        {
            if (tokens[index].Kind == SyntaxKind.CloseBracketToken)
            {
                var depth = 1;
                index--;
                while (index >= 0 && depth != 0)
                {
                    if (tokens[index].Kind == SyntaxKind.CloseBracketToken) depth++;
                    else if (tokens[index].Kind == SyntaxKind.OpenBracketToken) depth--;
                    index--;
                }
                if (depth != 0)
                    return Array.Empty<string>();
            }
            if (index < 0 || tokens[index].Kind is not (SyntaxKind.IdentifierToken or SyntaxKind.KeyKeyword))
                return Array.Empty<string>();
            parts.Add(tokens[index--].Text);
            if (index < 0 || tokens[index].Kind != SyntaxKind.DotToken)
                break;
            index--;
        }
        parts.Reverse();
        return parts;
    }

    private static string DescribeProvider(VariableSymbol symbol, string description,
        SmileCompilationDependencyContext dependencyContext) => symbol.ModuleName == null
        ? description
        : $"{description} from module {symbol.ModuleName} ({DescribeProvider(symbol.ProviderIdentity, dependencyContext)})";

    private static string DescribeProvider(string providerIdentity,
        SmileCompilationDependencyContext dependencyContext) =>
        dependencyContext.TryGetProviderDescriptor(providerIdentity, out var descriptor) &&
        !string.IsNullOrWhiteSpace(descriptor.LogicalIdentity)
            ? descriptor.LogicalIdentity
            : providerIdentity;

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
