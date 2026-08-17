using System;
using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

public enum SmileResolvedSymbolKind
{
    Module,
    Function,
    Subroutine,
    Variable,
    Constant,
    Array,
    Type,
    Field,
    Parameter,
    Local
}

public sealed class SmileResolvedSymbol
{
    internal SmileResolvedSymbol(SmileResolvedSymbolKind kind, string name, string qualifiedName,
        string alias, TextSpan referenceSpan, SourceLocation? declarationLocation, string providerIdentity,
        string moduleName, string signature, SmileDocumentation documentation, bool requiresGameWindow,
        RoutineSymbol? routine = null, VariableSymbol? variable = null, RecordTypeSymbol? type = null,
        RecordFieldSymbol? field = null, RecordTypeSymbol? containingType = null)
    {
        Kind = kind;
        Name = name;
        QualifiedName = qualifiedName;
        Alias = alias;
        ReferenceSpan = referenceSpan;
        DeclarationLocation = declarationLocation;
        ProviderIdentity = providerIdentity;
        ModuleName = moduleName;
        Signature = signature;
        Documentation = documentation;
        RequiresGameWindow = requiresGameWindow;
        Routine = routine;
        Variable = variable;
        Type = type;
        Field = field;
        ContainingType = containingType;
    }

    public SmileResolvedSymbolKind Kind { get; }
    public string Name { get; }
    public string QualifiedName { get; }
    public string Alias { get; }
    public TextSpan ReferenceSpan { get; }
    public SourceLocation? DeclarationLocation { get; }
    public string ProviderIdentity { get; }
    public string ModuleName { get; }
    public string Signature { get; }
    public SmileDocumentation Documentation { get; }
    public bool RequiresGameWindow { get; }
    internal RoutineSymbol? Routine { get; }
    internal VariableSymbol? Variable { get; }
    internal RecordTypeSymbol? Type { get; }
    internal RecordFieldSymbol? Field { get; }
    internal RecordTypeSymbol? ContainingType { get; }
}

public sealed class SmileParameterPresentation
{
    internal SmileParameterPresentation(string name, string signature, string description)
    {
        Name = name;
        Signature = signature;
        Description = description;
    }

    public string Name { get; }
    public string Signature { get; }
    public string Description { get; }
}

public sealed class SmileSymbolPresentation
{
    internal SmileSymbolPresentation(string signature, string summary,
        IReadOnlyList<SmileParameterPresentation> parameters, string returns, string remarks,
        string capability, string provider, string sourcePath, string alias)
    {
        Signature = signature;
        Summary = summary;
        Parameters = parameters;
        Returns = returns;
        Remarks = remarks;
        Capability = capability;
        Provider = provider;
        SourcePath = sourcePath;
        Alias = alias;
    }

    public string Signature { get; }
    public string Summary { get; }
    public IReadOnlyList<SmileParameterPresentation> Parameters { get; }
    public string Returns { get; }
    public string Remarks { get; }
    public string Capability { get; }
    public string Provider { get; }
    public string SourcePath { get; }
    public string Alias { get; }
}

public static class SmileSymbolDisplayService
{
    public static SmileSymbolPresentation Present(SmileResolvedSymbol symbol,
        SmileCompilationDependencyContext dependencies)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        if (dependencies == null)
            throw new ArgumentNullException(nameof(dependencies));

        var parameters = new List<SmileParameterPresentation>();
        if (symbol.Routine != null)
        {
            for (var index = 0; index < symbol.Routine.Parameters.Count; index++)
            {
                var parameter = symbol.Routine.Parameters[index];
                symbol.Documentation.Parameters.TryGetValue(parameter.Name, out var description);
                parameters.Add(new SmileParameterPresentation(parameter.Name,
                    FormatParameter(symbol.Routine, index), description ?? string.Empty));
            }
        }

        return new SmileSymbolPresentation(symbol.Signature, symbol.Documentation.Summary, parameters,
            symbol.Kind == SmileResolvedSymbolKind.Function ? symbol.Documentation.Returns : string.Empty,
            symbol.Documentation.Remarks,
            symbol.RequiresGameWindow ? "Requires Game Window." : string.Empty,
            DescribeProvider(symbol.ProviderIdentity, dependencies),
            symbol.DeclarationLocation?.FilePath ?? string.Empty, symbol.Alias);
    }

    public static string FormatRoutineSignature(RoutineSymbol routine, bool includeModuleName = true)
    {
        if (routine == null)
            throw new ArgumentNullException(nameof(routine));
        var name = includeModuleName && !string.IsNullOrWhiteSpace(routine.ModuleName)
            ? routine.ModuleName + "." + routine.Name
            : routine.Name;
        var parameters = string.Join(", ", Enumerable.Range(0, routine.Parameters.Count)
            .Select(index => FormatParameter(routine, index)));
        var returnType = routine.IsFunction
            ? " As " + FormatDeclaredType(routine.Source, routine.Declaration.ReturnTypeToken, routine.ReturnType)
            : string.Empty;
        return (routine.IsFunction ? "Function " : "Sub ") + name + "(" + parameters + ")" + returnType;
    }

    public static string FormatParameter(RoutineSymbol routine, int index)
    {
        var parameter = routine.Parameters[index];
        var mode = parameter.ParameterMode == ParameterPassingMode.ByRef ? "ByRef " : string.Empty;
        var typeToken = index < routine.Declaration.Parameters.Count
            ? routine.Declaration.Parameters[index].TypeToken : null;
        return mode + parameter.Name + " As " + FormatDeclaredType(routine.Source, typeToken, parameter.Type);
    }

    public static string DescribeProvider(string providerIdentity,
        SmileCompilationDependencyContext dependencies)
    {
        if (string.IsNullOrWhiteSpace(providerIdentity))
            return string.Empty;
        return dependencies.TryGetProviderDescriptor(providerIdentity, out var descriptor) &&
               !string.IsNullOrWhiteSpace(descriptor.LogicalIdentity)
            ? descriptor.IsBuiltIn
                ? "SMILE 2.0 built-in library " + descriptor.LogicalIdentity
                : descriptor.LogicalIdentity
            : providerIdentity;
    }

    internal static string FormatVariableSignature(VariableSymbol variable)
    {
        var keyword = variable.IsConstant ? "Const " : variable.IsParameter ? "Parameter " : "Dim ";
        var name = string.IsNullOrWhiteSpace(variable.ModuleName)
            ? variable.Name : variable.ModuleName + "." + variable.Name;
        var dimensions = variable.IsArray ? "[" + string.Join(", ", variable.ArrayDimensions) + "]" : string.Empty;
        return keyword + name + dimensions + " As " + FormatType(variable.Type);
    }

    internal static string FormatTypeSignature(RecordTypeSymbol type)
    {
        var name = string.IsNullOrWhiteSpace(type.ModuleName) ? type.Name : type.ModuleName + "." + type.Name;
        return "Type " + name;
    }

    internal static string FormatFieldSignature(RecordTypeSymbol owner, RecordFieldSymbol field)
    {
        var ownerName = string.IsNullOrWhiteSpace(owner.ModuleName)
            ? owner.Name : owner.ModuleName + "." + owner.Name;
        return "Field " + ownerName + "." + field.Name + " As " + FormatType(field.Type);
    }

    private static string FormatDeclaredType(SourceText source, SyntaxToken? token, SmileType fallback)
    {
        if (token != null && token.Span.Start >= 0 && token.Span.End <= source.Length)
        {
            var declared = source.Substring(token.Span.Start, token.Span.Length).Trim();
            if (!string.IsNullOrWhiteSpace(declared))
                return declared;
        }
        return FormatType(fallback);
    }

    private static string FormatType(SmileType type) => type.Kind == SmileTypeKind.Record
        ? (string.IsNullOrWhiteSpace(type.ModuleName) ? type.Name : type.ModuleName + "." + type.Name)
        : type.Name;
}

public static class SmileSymbolService
{
    public static bool TryResolve(SmileAnalysisResult analysis, SyntaxTree syntaxTree, int position,
        out SmileResolvedSymbol symbol)
    {
        symbol = null!;
        if (analysis == null || syntaxTree == null)
            return false;
        if (!TrySelectNameToken(syntaxTree, position, out var token, out var tokenIndex))
            return false;

        return TryResolveToken(analysis, syntaxTree, token, tokenIndex, out symbol);
    }

    public static bool TryResolveToken(SmileAnalysisResult analysis, SyntaxTree syntaxTree, SyntaxToken token,
        int tokenIndex, out SmileResolvedSymbol symbol)
    {
        symbol = null!;
        if (analysis == null || syntaxTree == null || token == null || tokenIndex < 0 ||
            tokenIndex >= syntaxTree.Tokens.Count || !ReferenceEquals(syntaxTree.Tokens[tokenIndex], token) ||
            !IsNameToken(token))
        {
            return false;
        }

        var currentModule = analysis.SemanticModel.Modules.Values.FirstOrDefault(module =>
            module.SyntaxTrees.Any(tree => ReferenceEquals(tree.Source, syntaxTree.Source)));
        var currentRoutine = analysis.SemanticModel.Routines.Values.FirstOrDefault(routine =>
            ReferenceEquals(routine.Source, syntaxTree.Source) &&
            routine.Declaration.Span.Start <= token.Span.Start && token.Span.Start <= routine.Declaration.Span.End);

        if (TryResolveDeclaration(analysis, syntaxTree, token, currentModule, out symbol))
            return true;

        var tokens = syntaxTree.Tokens;
        if (tokenIndex + 1 < tokens.Count && tokens[tokenIndex + 1].Kind == SyntaxKind.DotToken &&
            analysis.SemanticModel.GetImports(syntaxTree.Source).TryGetValue(token.Text, out var importedModule))
        {
            symbol = CreateModule(importedModule, token.Text, token.Span);
            return true;
        }

        if (tokenIndex >= 2 && tokens[tokenIndex - 1].Kind == SyntaxKind.DotToken &&
            IsNameToken(tokens[tokenIndex - 2]) &&
            analysis.SemanticModel.GetImports(syntaxTree.Source).TryGetValue(tokens[tokenIndex - 2].Text,
                out importedModule) &&
            analysis.DependencyContext.CanAccess(syntaxTree.ProviderIdentity, importedModule.ProviderIdentity) &&
            TryGetAccessibleMember(importedModule, token.Text, out var importedMember))
        {
            symbol = CreateMember(importedMember, token.Span);
            return true;
        }

        if (analysis.SemanticModel.TryGetFieldUse(syntaxTree.Source, token.Position, out var boundField))
        {
            var boundOwner = analysis.SemanticModel.Types.Values.FirstOrDefault(type =>
                type.Fields.Contains(boundField));
            if (boundOwner != null)
            {
                symbol = CreateField(boundOwner, boundField, token.Span);
                return true;
            }
        }

        if (TryResolveFieldUse(analysis, syntaxTree, token, tokenIndex, currentRoutine, currentModule,
                out var owner, out var field))
        {
            symbol = CreateField(owner, field, token.Span);
            return true;
        }

        if (TryResolveVariable(analysis, currentRoutine, currentModule, token.Text, token.Span.Start,
                out var variable))
        {
            symbol = CreateVariable(variable, token.Span);
            return true;
        }

        if (TryResolveRoutine(analysis, currentModule, token.Text, out var routine))
        {
            symbol = CreateRoutine(routine, token.Span);
            return true;
        }

        if (TryResolveType(analysis, currentModule, token.Text, out var type))
        {
            symbol = CreateType(type, token.Span);
            return true;
        }

        return false;
    }

    private static bool TrySelectNameToken(SyntaxTree syntaxTree, int position, out SyntaxToken token,
        out int tokenIndex)
    {
        var clamped = Math.Max(0, Math.Min(position, syntaxTree.Source.Length));
        for (var index = 0; index < syntaxTree.Tokens.Count; index++)
        {
            var candidate = syntaxTree.Tokens[index];
            if (candidate.Span.End == clamped && IsNameToken(candidate))
            {
                token = candidate;
                tokenIndex = index;
                return true;
            }
        }
        for (var index = 0; index < syntaxTree.Tokens.Count; index++)
        {
            var candidate = syntaxTree.Tokens[index];
            if (candidate.Span.Start <= clamped && clamped < candidate.Span.End && IsNameToken(candidate))
            {
                token = candidate;
                tokenIndex = index;
                return true;
            }
        }
        token = null!;
        tokenIndex = -1;
        return false;
    }

    private static bool IsNameToken(SyntaxToken token)
    {
        if (token.Kind is SyntaxKind.CommentToken or SyntaxKind.StringToken or SyntaxKind.NewLineToken or
            SyntaxKind.BadToken or SyntaxKind.EndOfFileToken or SyntaxKind.NumberToken ||
            string.IsNullOrWhiteSpace(token.Text) || !(char.IsLetter(token.Text[0]) || token.Text[0] == '_'))
            return false;
        return token.Text.All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    private static bool TryResolveDeclaration(SmileAnalysisResult analysis, SyntaxTree syntaxTree, SyntaxToken token,
        ModuleSymbol? currentModule, out SmileResolvedSymbol symbol)
    {
        foreach (var module in analysis.SemanticModel.Modules.Values)
        {
            foreach (var tree in module.SyntaxTrees.Where(tree => ReferenceEquals(tree.Source, syntaxTree.Source)))
            {
                var declaration = tree.Root.Statements.OfType<ModuleDeclarationSyntax>().FirstOrDefault();
                if (declaration != null && declaration.Name.Identifiers.Any(identifier => SameSpan(identifier.Span, token.Span)))
                {
                    symbol = CreateModule(module, string.Empty, token.Span);
                    return true;
                }
            }
        }

        foreach (var import in Statements(syntaxTree.Root.Statements).OfType<ImportStatementSyntax>())
        {
            if (SameSpan(import.Alias.Span, token.Span) && analysis.SemanticModel.GetImports(syntaxTree.Source)
                    .TryGetValue(import.Alias.Text, out var module))
            {
                symbol = CreateModule(module, import.Alias.Text, token.Span);
                return true;
            }
        }

        foreach (var type in analysis.SemanticModel.Types.Values.Where(type => ReferenceEquals(type.Source, syntaxTree.Source)))
        {
            if (SameSpan(type.DeclarationSpan, token.Span))
            {
                symbol = CreateType(type, token.Span);
                return true;
            }
            var field = type.Fields.FirstOrDefault(candidate => SameSpan(candidate.DeclarationSpan, token.Span));
            if (field != null)
            {
                symbol = CreateField(type, field, token.Span);
                return true;
            }
        }

        foreach (var routine in analysis.SemanticModel.Routines.Values.Where(routine =>
                     ReferenceEquals(routine.Source, syntaxTree.Source)))
        {
            if (SameSpan(routine.DeclarationLocation.Span, token.Span))
            {
                symbol = CreateRoutine(routine, token.Span);
                return true;
            }
            var parameter = routine.Parameters.FirstOrDefault(candidate => SameSpan(candidate.DeclarationSpan, token.Span));
            if (parameter != null)
            {
                symbol = CreateVariable(parameter, token.Span);
                return true;
            }
            var local = routine.LocalSymbols.Values.FirstOrDefault(candidate => SameSpan(candidate.DeclarationSpan, token.Span));
            if (local != null)
            {
                symbol = CreateVariable(local, token.Span);
                return true;
            }
        }

        foreach (var variable in analysis.SemanticModel.Symbols.Values.Where(variable =>
                     ReferenceEquals(variable.Source, syntaxTree.Source) && SameSpan(variable.DeclarationSpan, token.Span)))
        {
            symbol = CreateVariable(variable, token.Span);
            return true;
        }

        if (currentModule != null && TryGetAccessibleMember(currentModule, token.Text, out var member) &&
            SameSpan(member.DeclarationSpan, token.Span))
        {
            symbol = CreateMember(member, token.Span);
            return true;
        }

        symbol = null!;
        return false;
    }

    private static IEnumerable<StatementSyntax> Statements(IEnumerable<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            yield return statement;
            if (statement is ModuleDeclarationSyntax module)
                foreach (var nested in module.Statements)
                    yield return nested;
        }
    }

    private static bool TryGetAccessibleMember(ModuleSymbol module, string name, out SmileModuleMember member)
    {
        if ((module.Members.TryGetValue(name, out member!) || module.Types.TryGetValue(name, out member!)) &&
            member.Visibility == ModuleVisibility.Public)
            return true;
        member = null!;
        return false;
    }

    private static bool TryResolveVariable(SmileAnalysisResult analysis, RoutineSymbol? currentRoutine,
        ModuleSymbol? currentModule, string name, int position, out VariableSymbol variable)
    {
        if (currentRoutine != null && currentRoutine.LocalSymbols.TryGetValue(name, out variable!) &&
            (variable.IsParameter || variable.DeclarationSpan.Start <= position))
            return true;
        if (currentModule?.Members.TryGetValue(name, out var member) == true && member.Variable != null)
        {
            variable = member.Variable;
            return true;
        }
        variable = analysis.SemanticModel.Symbols.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (candidate.ModuleName == null || string.Equals(candidate.ModuleName, currentModule?.Name,
                StringComparison.OrdinalIgnoreCase)))!;
        return variable != null;
    }

    private static bool TryResolveRoutine(SmileAnalysisResult analysis, ModuleSymbol? currentModule, string name,
        out RoutineSymbol routine)
    {
        if (currentModule?.Members.TryGetValue(name, out var member) == true && member.Routine != null)
        {
            routine = member.Routine;
            return true;
        }
        routine = analysis.SemanticModel.Routines.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (candidate.ModuleName == null || string.Equals(candidate.ModuleName, currentModule?.Name,
                StringComparison.OrdinalIgnoreCase)))!;
        return routine != null;
    }

    private static bool TryResolveType(SmileAnalysisResult analysis, ModuleSymbol? currentModule, string name,
        out RecordTypeSymbol type)
    {
        if (currentModule?.Types.TryGetValue(name, out var member) == true && member.Type != null)
        {
            type = member.Type;
            return true;
        }
        type = analysis.SemanticModel.Types.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (candidate.ModuleName == null || string.Equals(candidate.ModuleName, currentModule?.Name,
                StringComparison.OrdinalIgnoreCase)))!;
        return type != null;
    }

    private static bool TryResolveFieldUse(SmileAnalysisResult analysis, SyntaxTree syntaxTree, SyntaxToken token,
        int tokenIndex, RoutineSymbol? currentRoutine, ModuleSymbol? currentModule,
        out RecordTypeSymbol owner, out RecordFieldSymbol field)
    {
        owner = null!;
        field = null!;
        if (tokenIndex == 0 || syntaxTree.Tokens[tokenIndex - 1].Kind != SyntaxKind.DotToken)
            return false;
        var receiver = ReceiverParts(syntaxTree.Tokens, tokenIndex - 1);
        if (receiver.Count == 0)
            return false;

        SmileType type;
        var firstField = 1;
        if (analysis.SemanticModel.GetImports(syntaxTree.Source).TryGetValue(receiver[0], out var imported) &&
            receiver.Count >= 2 && TryGetAccessibleMember(imported, receiver[1], out var importedMember) &&
            importedMember.Variable != null)
        {
            type = importedMember.Variable.Type;
            firstField = 2;
        }
        else
        {
            if (!TryResolveVariable(analysis, currentRoutine, currentModule, receiver[0], token.Span.Start,
                    out var variable))
                return false;
            type = variable.Type;
        }

        for (var index = firstField; index < receiver.Count; index++)
        {
            if (type is not RecordTypeSymbol record || !record.TryGetField(receiver[index], out var nestedField))
                return false;
            type = nestedField.Type;
        }
        if (type is not RecordTypeSymbol containingType || !containingType.TryGetField(token.Text, out field!))
            return false;
        owner = containingType;
        return true;
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
            if (index < 0 || !IsNameToken(tokens[index]))
                return Array.Empty<string>();
            parts.Add(tokens[index--].Text);
            if (index < 0 || tokens[index].Kind != SyntaxKind.DotToken)
                break;
            index--;
        }
        parts.Reverse();
        return parts;
    }

    private static SmileResolvedSymbol CreateModule(ModuleSymbol module, string alias, TextSpan referenceSpan)
    {
        var declaration = module.SyntaxTrees.Select(tree => new
            {
                Tree = tree,
                Declaration = tree.Root.Statements.OfType<ModuleDeclarationSyntax>().FirstOrDefault()
            })
            .FirstOrDefault(item => item.Declaration != null);
        var location = declaration?.Declaration == null ? null :
            new SourceLocation(declaration.Tree.Source, declaration.Declaration.Name.Span);
        var documentation = declaration?.Declaration == null ? SmileDocumentation.Empty :
            SmileDocumentationService.GetDocumentation(declaration.Tree.Source,
                declaration.Declaration.ModuleKeyword.Span.Start);
        return new SmileResolvedSymbol(SmileResolvedSymbolKind.Module, module.Name, module.Name, alias,
            referenceSpan, location, module.ProviderIdentity, module.Name, "Module " + module.Name,
            documentation, requiresGameWindow: false);
    }

    private static SmileResolvedSymbol CreateMember(SmileModuleMember member, TextSpan referenceSpan)
    {
        if (member.Routine != null)
            return CreateRoutine(member.Routine, referenceSpan);
        if (member.Variable != null)
            return CreateVariable(member.Variable, referenceSpan);
        if (member.Type != null)
            return CreateType(member.Type, referenceSpan);
        var kind = member.Kind switch
        {
            SmileModuleMemberKind.Constant => SmileResolvedSymbolKind.Constant,
            SmileModuleMemberKind.Array => SmileResolvedSymbolKind.Array,
            SmileModuleMemberKind.Type => SmileResolvedSymbolKind.Type,
            SmileModuleMemberKind.Function => SmileResolvedSymbolKind.Function,
            SmileModuleMemberKind.Subroutine => SmileResolvedSymbolKind.Subroutine,
            _ => SmileResolvedSymbolKind.Variable
        };
        return new SmileResolvedSymbol(kind, member.Name, member.Name, string.Empty, referenceSpan,
            member.DeclarationLocation, string.Empty, string.Empty, kind + " " + member.Name,
            SmileDocumentationService.GetDocumentation(member.Source, member.DeclarationSpan.Start), false);
    }

    private static SmileResolvedSymbol CreateRoutine(RoutineSymbol routine, TextSpan referenceSpan)
    {
        var kind = routine.IsFunction ? SmileResolvedSymbolKind.Function : SmileResolvedSymbolKind.Subroutine;
        var qualifiedName = string.IsNullOrWhiteSpace(routine.ModuleName)
            ? routine.Name : routine.ModuleName + "." + routine.Name;
        return new SmileResolvedSymbol(kind, routine.Name, qualifiedName, string.Empty, referenceSpan,
            routine.DeclarationLocation, routine.ProviderIdentity, routine.ModuleName ?? string.Empty,
            SmileSymbolDisplayService.FormatRoutineSignature(routine),
            SmileDocumentationService.GetDocumentation(routine.Source, routine.Declaration.Keyword.Span.Start),
            routine.RequiresGameWindow, routine: routine);
    }

    private static SmileResolvedSymbol CreateVariable(VariableSymbol variable, TextSpan referenceSpan)
    {
        var kind = variable.IsParameter ? SmileResolvedSymbolKind.Parameter :
            variable.RoutineName != null ? SmileResolvedSymbolKind.Local :
            variable.IsConstant ? SmileResolvedSymbolKind.Constant :
            variable.IsArray ? SmileResolvedSymbolKind.Array : SmileResolvedSymbolKind.Variable;
        var qualifiedName = string.IsNullOrWhiteSpace(variable.ModuleName)
            ? variable.Name : variable.ModuleName + "." + variable.Name;
        return new SmileResolvedSymbol(kind, variable.Name, qualifiedName, string.Empty, referenceSpan,
            variable.DeclarationLocation, variable.ProviderIdentity, variable.ModuleName ?? string.Empty,
            SmileSymbolDisplayService.FormatVariableSignature(variable),
            SmileDocumentationService.GetDocumentation(variable.Source, variable.DeclarationSpan.Start), false,
            variable: variable);
    }

    private static SmileResolvedSymbol CreateType(RecordTypeSymbol type, TextSpan referenceSpan)
    {
        var qualifiedName = string.IsNullOrWhiteSpace(type.ModuleName)
            ? type.Name : type.ModuleName + "." + type.Name;
        return new SmileResolvedSymbol(SmileResolvedSymbolKind.Type, type.Name, qualifiedName, string.Empty,
            referenceSpan, type.DeclarationLocation, type.ProviderIdentity, type.ModuleName ?? string.Empty,
            SmileSymbolDisplayService.FormatTypeSignature(type),
            SmileDocumentationService.GetDocumentation(type.Source!, type.DeclarationSpan.Start), false, type: type);
    }

    private static SmileResolvedSymbol CreateField(RecordTypeSymbol owner, RecordFieldSymbol field,
        TextSpan referenceSpan)
    {
        var ownerName = string.IsNullOrWhiteSpace(owner.ModuleName)
            ? owner.Name : owner.ModuleName + "." + owner.Name;
        return new SmileResolvedSymbol(SmileResolvedSymbolKind.Field, field.Name, ownerName + "." + field.Name,
            string.Empty, referenceSpan, field.DeclarationLocation, owner.ProviderIdentity,
            owner.ModuleName ?? string.Empty, SmileSymbolDisplayService.FormatFieldSignature(owner, field),
            SmileDocumentationService.GetDocumentation(field.Source, field.DeclarationSpan.Start), false,
            field: field, containingType: owner);
    }

    private static bool SameSpan(TextSpan left, TextSpan right) =>
        left.Start == right.Start && left.Length == right.Length;
}
