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
    Field,
    EnumMember,
    Parameter
}

public sealed class SmileCompletion
{
    public SmileCompletion(string displayText, string description, SmileCompletionKind kind,
        string? insertionText = null)
    {
        DisplayText = displayText;
        Description = description;
        Kind = kind;
        InsertionText = insertionText ?? displayText;
    }

    public string DisplayText { get; }
    public string Description { get; }
    public SmileCompletionKind Kind { get; }
    public string InsertionText { get; }
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

        var parameterCompletions = TryGetNamedArgumentCompletions(analysis, syntaxTree, position, currentModule);

        var qualifiedAlias = AliasBeforeDot(syntaxTree.Source.Text, position);
        var qualifiedTypeContext = qualifiedAlias != null && IsQualifiedTypeContext(syntaxTree.Source.Text, position, qualifiedAlias);
        if (qualifiedAlias != null && analysis.SemanticModel.GetImports(syntaxTree.Source)
            .TryGetValue(qualifiedAlias, out var importedModule))
        {
            var typeContext = afterAs || qualifiedTypeContext;
            return importedModule.PublicMembers.Where(member => typeContext
                    ? member.Kind is SmileModuleMemberKind.Type or SmileModuleMemberKind.Enum
                    : member.Kind != SmileModuleMemberKind.Type)
                .OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
                .Select(member => MemberCompletion(member, analysis.DependencyContext)).ToArray();
        }

        if (afterAs)
        {
            var types = new Dictionary<string, SmileCompletion>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in new[] { "Boolean", "Image", "Number", "Text" })
                types[name] = new SmileCompletion(name, name == "Image"
                    ? "SMILE opaque loaded 2D image resource"
                    : $"SMILE built-in type {name}", SmileCompletionKind.Type);
            var availableTypes = currentModule == null
                ? analysis.SemanticModel.NominalTypes.Values.Where(type => type.ModuleName == null)
                : currentModule.Types.Values.Where(member => member.Type != null).Select(member => member.Type!);
            foreach (var type in availableTypes)
                types[type.Name] = TypeCompletion(type, analysis.DependencyContext);
            foreach (var import in analysis.SemanticModel.GetImports(syntaxTree.Source))
                types[import.Key] = new SmileCompletion(import.Key,
                    $"Import alias for nominal types in module {import.Value.Name}", SmileCompletionKind.Module);
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
            completions[routine.Name] = new SmileCompletion(
                routine.Name,
                SmileSymbolDisplayService.FormatRoutineSignature(routine, includeModuleName: false),
                kind);
        }

        var visibleEnums = currentModule == null
            ? analysis.SemanticModel.EnumTypes.Values.Where(type => type.ModuleName == null)
            : currentModule.Types.Values.Where(member => member.Type is EnumTypeSymbol)
                .Select(member => (EnumTypeSymbol)member.Type!);
        foreach (var enumType in visibleEnums)
            completions[enumType.Name] = TypeCompletion(enumType, analysis.DependencyContext);

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
            completions["As"] = new SmileCompletion("As", "Required import alias keyword", SmileCompletionKind.Keyword);
        }

        if (parameterCompletions != null)
        {
            foreach (var completion in parameterCompletions)
                completions[completion.DisplayText] = completion;
        }

        return completions.Values.OrderBy(completion => completion.DisplayText, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<SmileCompletion>? TryGetNamedArgumentCompletions(SmileAnalysisResult analysis,
        SyntaxTree syntaxTree, int position, ModuleSymbol? currentModule)
    {
        var tokens = syntaxTree.Tokens.Where(token => token.Span.Start < position &&
            token.Kind != SyntaxKind.EndOfFileToken).ToArray();
        var openStack = new List<int>();
        for (var index = 0; index < tokens.Length; index++)
        {
            if (tokens[index].Kind is SyntaxKind.OpenParenthesisToken or SyntaxKind.OpenBracketToken)
            {
                openStack.Add(index);
                continue;
            }
            if (tokens[index].Kind is not (SyntaxKind.CloseParenthesisToken or SyntaxKind.CloseBracketToken) ||
                openStack.Count == 0)
                continue;
            var expected = tokens[index].Kind == SyntaxKind.CloseParenthesisToken
                ? SyntaxKind.OpenParenthesisToken : SyntaxKind.OpenBracketToken;
            if (tokens[openStack[openStack.Count - 1]].Kind == expected)
                openStack.RemoveAt(openStack.Count - 1);
        }

        if (openStack.Count == 0 ||
            tokens[openStack[openStack.Count - 1]].Kind != SyntaxKind.OpenParenthesisToken)
            return null;
        var openIndex = openStack[openStack.Count - 1];
        var nameIndex = PreviousSignificantToken(tokens, openIndex - 1);
        if (nameIndex < 0 || !IsCompletionNameToken(tokens[nameIndex]))
            return null;
        var declarationIndex = PreviousSignificantToken(tokens, nameIndex - 1);
        if (declarationIndex >= 0 && tokens[declarationIndex].Kind is SyntaxKind.SubKeyword or SyntaxKind.FunctionKeyword)
            return null;

        RoutineSymbol? routine = null;
        if (SyntaxFacts.IsBuiltInFunction(tokens[nameIndex].Kind))
            return null;
        var dotIndex = PreviousSignificantToken(tokens, nameIndex - 1);
        var aliasIndex = dotIndex >= 0 && tokens[dotIndex].Kind == SyntaxKind.DotToken
            ? PreviousSignificantToken(tokens, dotIndex - 1) : -1;
        if (aliasIndex >= 0 && analysis.SemanticModel.GetImports(syntaxTree.Source)
                .TryGetValue(tokens[aliasIndex].Text, out var imported) &&
            imported.Members.TryGetValue(tokens[nameIndex].Text, out var importedMember))
        {
            routine = importedMember.Visibility == ModuleVisibility.Public ? importedMember.Routine : null;
        }
        else if (currentModule?.Members.TryGetValue(tokens[nameIndex].Text, out var ownMember) == true)
        {
            routine = ownMember.Routine;
        }
        else
        {
            routine = analysis.SemanticModel.Routines.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, tokens[nameIndex].Text, StringComparison.OrdinalIgnoreCase) &&
                (candidate.ModuleName == null || string.Equals(candidate.ModuleName, currentModule?.Name,
                    StringComparison.OrdinalIgnoreCase)));
        }
        if (routine == null)
            return null;

        var segments = new List<List<SyntaxToken>> { new() };
        var depth = 0;
        for (var index = openIndex + 1; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (token.Kind is SyntaxKind.OpenParenthesisToken or SyntaxKind.OpenBracketToken)
            {
                if (depth == 0)
                    segments[segments.Count - 1].Add(token);
                depth++;
                continue;
            }
            if (token.Kind is SyntaxKind.CloseParenthesisToken or SyntaxKind.CloseBracketToken)
            {
                depth = Math.Max(0, depth - 1);
                if (depth == 0)
                    segments[segments.Count - 1].Add(token);
                continue;
            }
            if (depth == 0 && token.Kind == SyntaxKind.CommaToken)
            {
                segments.Add(new List<SyntaxToken>());
                continue;
            }
            if (depth == 0 && token.Kind != SyntaxKind.NewLineToken)
                segments[segments.Count - 1].Add(token);
        }

        var currentSegment = segments[segments.Count - 1];
        if (currentSegment.Any(token => token.Kind == SyntaxKind.ColonEqualsToken) ||
            currentSegment.Count > 1 ||
            currentSegment.Count == 1 && !IsCompletionNameToken(currentSegment[0]))
            return null;

        var supplied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var positionalCount = 0;
        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            var colonEquals = segment.FindIndex(token => token.Kind == SyntaxKind.ColonEqualsToken);
            if (colonEquals > 0)
                supplied.Add(segment[colonEquals - 1].Text);
            else if (index < segments.Count - 1 && segment.Count != 0)
                positionalCount++;
        }
        for (var index = 0; index < Math.Min(positionalCount, routine.Parameters.Count); index++)
            supplied.Add(routine.Parameters[index].Name);

        return routine.Parameters.Select((parameter, index) => (parameter, index))
            .Where(item => !supplied.Contains(item.parameter.Name))
            .Select(item => new SmileCompletion(item.parameter.Name + ":=",
                SmileSymbolDisplayService.FormatParameter(routine, item.index),
                SmileCompletionKind.Parameter, item.parameter.Name + ":="))
            .ToArray();
    }

    private static int PreviousSignificantToken(IReadOnlyList<SyntaxToken> tokens, int index)
    {
        while (index >= 0 && tokens[index].Kind == SyntaxKind.NewLineToken)
            index--;
        return index;
    }

    private static bool IsCompletionNameToken(SyntaxToken token) =>
        token.Text.Length != 0 && (char.IsLetter(token.Text[0]) || token.Text[0] == '_') &&
        token.Text.All(character => char.IsLetterOrDigit(character) || character == '_');

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
                    SyntaxKind.TextLengthKeyword => "Text_Length(Value As Text) As Number - Unicode scalar count",
                    SyntaxKind.TextCodeAtKeyword => "Text_Code_At(Value As Text, Index As Number) As Number - zero-based Unicode scalar value",
                    SyntaxKind.TextSliceKeyword => "Text_Slice(Value As Text, Start As Number, Count As Number) As Text - Unicode scalar slice",
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
        var type = symbol.Type.Name;
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
            var capability = member.Routine.RequiresGameWindow ? " - requires Game Window" : string.Empty;
            return new SmileCompletion(member.Name,
                $"{SmileSymbolDisplayService.FormatRoutineSignature(member.Routine, includeModuleName: false)}" +
                $"{capability} from module {member.Routine.ModuleName} " +
                $"({DescribeProvider(member.Routine.ProviderIdentity, dependencyContext)})",
                member.Routine.IsFunction ? SmileCompletionKind.Function : SmileCompletionKind.Subroutine);
        }
        if (member.Variable != null)
            return VariableCompletion(member.Variable, dependencyContext);
        if (member.Type != null)
            return TypeCompletion(member.Type, dependencyContext);
        return new SmileCompletion(member.Name, $"Public module member {member.Name}", SmileCompletionKind.Variable);
    }

    private static SmileCompletion TypeCompletion(NominalTypeSymbol type,
        SmileCompilationDependencyContext dependencyContext)
    {
        var provider = type.ModuleName == null ? string.Empty :
            $" from module {type.ModuleName} ({DescribeProvider(type.ProviderIdentity, dependencyContext)})";
        var description = type switch
        {
            RecordTypeSymbol record => $"Type {record.Name} ({string.Join(", ", record.Fields.Select(field => $"{field.Name} As {field.Type.Name}"))})",
            EnumTypeSymbol enumType => $"Enum {enumType.Name} ({string.Join(", ", enumType.Members.Select(member => $"{member.Name} = {member.Value}"))})",
            _ => $"Type {type.Name}"
        };
        return new SmileCompletion(type.Name, description + provider, SmileCompletionKind.Type);
    }

    private static IReadOnlyList<SmileCompletion>? TryGetFieldCompletions(SmileAnalysisResult analysis,
        SyntaxTree syntaxTree, int position, ModuleSymbol? currentModule)
    {
        var tokens = syntaxTree.Tokens.Where(token => token.Kind is not (SyntaxKind.EndOfFileToken or SyntaxKind.NewLineToken) &&
                                                      token.Span.End <= position).ToArray();
        if (tokens.Length == 0 || tokens[tokens.Length - 1].Kind != SyntaxKind.DotToken)
            return null;
        if (TryGetLeadingReceiverParts(syntaxTree, tokens, tokens.Length - 1, out var leadingParts))
        {
            if (!analysis.SemanticModel.TryGetInnermostWithScope(syntaxTree.Source, position, out var scope))
                return Array.Empty<SmileCompletion>();
            SmileType leadingType = scope.TargetType;
            foreach (var part in leadingParts)
            {
                if (leadingType is not RecordTypeSymbol record || !record.TryGetField(part, out var field))
                    return Array.Empty<SmileCompletion>();
                leadingType = field.Type;
            }
            return leadingType is RecordTypeSymbol targetType
                ? FieldCompletions(targetType, analysis.DependencyContext)
                : Array.Empty<SmileCompletion>();
        }
        var parts = ReceiverParts(tokens, tokens.Length - 1);
        if (parts.Count == 0)
            return null;
        if (TryResolveEnumReceiver(analysis, syntaxTree, currentModule, parts, out var enumReceiver))
            return EnumMemberCompletions(enumReceiver, analysis.DependencyContext);
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
        return FieldCompletions(target, analysis.DependencyContext);
    }

    private static IReadOnlyList<SmileCompletion> FieldCompletions(RecordTypeSymbol target,
        SmileCompilationDependencyContext dependencyContext) =>
        target.Fields.OrderBy(field => field.Ordinal).Select(field => new SmileCompletion(field.Name,
            $"{field.Name} As {field.Type.Name} field of Type {target.Name}" +
            (target.ModuleName == null ? string.Empty :
                $" from module {target.ModuleName} ({DescribeProvider(target.ProviderIdentity, dependencyContext)})"),
            SmileCompletionKind.Field)).ToArray();

    private static IReadOnlyList<SmileCompletion> EnumMemberCompletions(EnumTypeSymbol target,
        SmileCompilationDependencyContext dependencyContext) =>
        target.Members.OrderBy(member => member.Ordinal).Select(member => new SmileCompletion(member.Name,
            $"{target.Name}.{member.Name} = {member.Value}" +
            (target.ModuleName == null ? string.Empty :
                $" from module {target.ModuleName} ({DescribeProvider(target.ProviderIdentity, dependencyContext)})"),
            SmileCompletionKind.EnumMember)).ToArray();

    private static bool TryResolveEnumReceiver(SmileAnalysisResult analysis, SyntaxTree syntaxTree,
        ModuleSymbol? currentModule, IReadOnlyList<string> parts, out EnumTypeSymbol enumType)
    {
        enumType = null!;
        if (parts.Count == 1)
        {
            if (currentModule?.Types.TryGetValue(parts[0], out var ownMember) == true &&
                ownMember.Type is EnumTypeSymbol ownEnum)
            {
                enumType = ownEnum;
                return true;
            }
            enumType = analysis.SemanticModel.EnumTypes.Values.FirstOrDefault(candidate =>
                candidate.ModuleName == null && string.Equals(candidate.Name, parts[0],
                    StringComparison.OrdinalIgnoreCase))!;
            return enumType != null;
        }
        if (parts.Count == 2 && analysis.SemanticModel.GetImports(syntaxTree.Source)
                .TryGetValue(parts[0], out var imported) &&
            imported.Types.TryGetValue(parts[1], out var importedMember) &&
            importedMember.Visibility == ModuleVisibility.Public && importedMember.Type is EnumTypeSymbol importedEnum)
        {
            enumType = importedEnum;
            return true;
        }
        return false;
    }

    private static bool TryGetLeadingReceiverParts(SyntaxTree syntaxTree, IReadOnlyList<SyntaxToken> tokens,
        int finalDotIndex, out IReadOnlyList<string> parts)
    {
        var text = syntaxTree.Source.Text;
        var finalDot = tokens[finalDotIndex];
        var lineStart = finalDot.Span.Start;
        while (lineStart > 0 && text[lineStart - 1] is not ('\r' or '\n'))
            lineStart--;
        var reversed = new List<string>();
        var index = finalDotIndex - 1;
        while (true)
        {
            if (index < 0 || tokens[index].Span.Start < lineStart)
            {
                reversed.Reverse();
                parts = reversed;
                return true;
            }
            if (tokens[index].Kind == SyntaxKind.CloseBracketToken)
            {
                parts = Array.Empty<string>();
                return false;
            }
            if (!IsMemberNameToken(tokens[index].Kind))
            {
                reversed.Reverse();
                parts = reversed;
                return true;
            }
            reversed.Add(tokens[index--].Text);
            if (index < 0 || tokens[index].Span.Start < lineStart || tokens[index].Kind != SyntaxKind.DotToken)
            {
                parts = Array.Empty<string>();
                return false;
            }
            index--;
        }
    }

    private static bool IsMemberNameToken(SyntaxKind kind) =>
        kind is SyntaxKind.IdentifierToken or SyntaxKind.KeyKeyword or SyntaxKind.WindowKeyword or
            SyntaxKind.SizeKeyword or SyntaxKind.DrawKeyword or SyntaxKind.LineKeyword or SyntaxKind.TextKeyword or
            SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword or SyntaxKind.NoneKeyword or SyntaxKind.UpKeyword or
            SyntaxKind.DownKeyword ||
        kind >= SyntaxKind.UnloadKeyword && kind <= SyntaxKind.ChannelKeyword;

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
            if (index < 0 || !IsMemberNameToken(tokens[index].Kind))
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
            ? descriptor.IsBuiltIn
                ? "SMILE 2.0 built-in library " + descriptor.LogicalIdentity
                : descriptor.LogicalIdentity
            : providerIdentity;

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
            .TrimStart().StartsWith("Import ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAfterAs(string text, int position)
    {
        var start = Math.Min(position, text.Length);
        while (start > 0 && text[start - 1] is not ('\r' or '\n')) start--;
        var before = text.Substring(start, Math.Min(position, text.Length) - start).TrimEnd();
        return before.EndsWith(" As", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(before, "As", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsQualifiedTypeContext(string text, int position, string alias)
    {
        var start = Math.Min(position, text.Length);
        while (start > 0 && text[start - 1] is not ('\r' or '\n')) start--;
        var before = text.Substring(start, Math.Min(position, text.Length) - start);
        var marker = before.LastIndexOf(" As ", StringComparison.OrdinalIgnoreCase);
        if (marker < 0 && before.TrimStart().StartsWith("As ", StringComparison.OrdinalIgnoreCase))
            marker = before.IndexOf("As ", StringComparison.OrdinalIgnoreCase) - 1;
        var tail = marker < 0 ? string.Empty : before.Substring(marker + 4).TrimStart();
        return tail.StartsWith(alias + ".", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDeclarationBeingTyped(VariableSymbol symbol, SourceText source, int position) =>
        ReferenceEquals(symbol.Source, source) &&
        symbol.DeclarationSpan.Start <= position && position <= symbol.DeclarationSpan.End;
}
