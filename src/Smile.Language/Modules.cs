using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Smile.Language;

public enum SmileCompilationKind
{
    Program,
    Library
}

public enum SmileProviderKind
{
    Project,
    Package,
    Loose
}

public static class SmileBuiltInLibraryCatalog
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "Smile.Game",
        "Smile.UI",
        "Smile.RPG"
    };

    public static bool IsBuiltIn(string libraryName) =>
        !string.IsNullOrWhiteSpace(libraryName) && Names.Contains(libraryName);
}

public sealed class SmileProviderDescriptor
{
    internal SmileProviderDescriptor(string identity, SmileProviderKind kind, string name, string version,
        string path)
    {
        Identity = SmileCompilationDependencyContext.Normalize(identity);
        Kind = kind;
        Name = name;
        Version = version;
        Path = path;
    }

    public string Identity { get; }
    public SmileProviderKind Kind { get; }
    public string Name { get; }
    public string Version { get; }
    public string Path { get; }
    public bool IsBuiltIn => SmileBuiltInLibraryCatalog.IsBuiltIn(Name);
    public string LogicalIdentity => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Version)
        ? string.Empty
        : Name + "@" + Version;

    internal string Describe() => Kind == SmileProviderKind.Loose
        ? "loose source root"
        : IsBuiltIn
            ? $"SMILE 2.0 built-in library '{Name}' {Version} at '{Path}'"
        : string.IsNullOrWhiteSpace(Name)
            ? $"project '{Path}'"
            : $"library '{Name}' {Version} at '{Path}'";
}

public sealed class SmileCompilationDependencyContext
{
    private readonly Dictionary<string, SmileProviderDescriptor> _providers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _directAccess =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _allowAll;

    private SmileCompilationDependencyContext(bool allowAll) => _allowAll = allowAll;

    public static SmileCompilationDependencyContext Unrestricted { get; } = new(allowAll: true);

    public bool CanAccess(string sourceProvider, string targetProvider)
    {
        if (_allowAll)
            return true;
        var source = Normalize(sourceProvider);
        var target = Normalize(targetProvider);
        return string.Equals(source, target, StringComparison.OrdinalIgnoreCase) ||
               (_directAccess.TryGetValue(source, out var accessible) && accessible.Contains(target));
    }

    public string DescribeInaccessibleImport(string moduleName, string sourceProvider, string targetProvider)
    {
        var source = Descriptor(sourceProvider).Describe();
        var target = Descriptor(targetProvider).Describe();
        return $"Module '{moduleName}' is provided by {target}, but {source} does not declare that provider as a direct reference.";
    }

    public bool TryGetProviderDescriptor(string providerIdentity, out SmileProviderDescriptor descriptor) =>
        _providers.TryGetValue(Normalize(providerIdentity), out descriptor!);

    internal static SmileCompilationDependencyContext Create() => new(allowAll: false);

    internal void AddProvider(string identity, SmileProviderKind kind, string name, string version, string path)
    {
        var normalized = Normalize(identity);
        _providers[normalized] = new SmileProviderDescriptor(normalized, kind, name, version, path);
        if (!_directAccess.ContainsKey(normalized))
            _directAccess.Add(normalized, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    internal void AddDirectAccess(string sourceProvider, string targetProvider)
    {
        var source = Normalize(sourceProvider);
        if (!_directAccess.TryGetValue(source, out var accessible))
        {
            accessible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _directAccess.Add(source, accessible);
        }
        accessible.Add(Normalize(targetProvider));
    }

    internal SmileCompilationDependencyContext Copy()
    {
        var copy = Create();
        foreach (var provider in _providers.Values)
            copy.AddProvider(provider.Identity, provider.Kind, provider.Name, provider.Version, provider.Path);
        foreach (var edge in _directAccess)
            foreach (var target in edge.Value)
                copy.AddDirectAccess(edge.Key, target);
        return copy;
    }

    internal static string Normalize(string? providerIdentity) =>
        string.IsNullOrWhiteSpace(providerIdentity) ? "<local>" : providerIdentity!;

    private SmileProviderDescriptor Descriptor(string identity)
    {
        var normalized = Normalize(identity);
        if (_providers.TryGetValue(normalized, out var provider))
            return provider;
        return new SmileProviderDescriptor(normalized, SmileProviderKind.Project, string.Empty, string.Empty,
            normalized);
    }
}

public enum SmileModuleMemberKind
{
    Constant,
    Variable,
    Array,
    Type,
    Subroutine,
    Function
}

public sealed class SmileModuleMember
{
    internal SmileModuleMember(string name, string runtimeIdentity, string semanticName,
        SmileModuleMemberKind kind, ModuleVisibility visibility, SourceText source, TextSpan declarationSpan)
    {
        Name = name;
        RuntimeIdentity = runtimeIdentity;
        SemanticName = semanticName;
        Kind = kind;
        Visibility = visibility;
        Source = source;
        DeclarationSpan = declarationSpan;
    }

    public string Name { get; }
    public string RuntimeIdentity { get; }
    public SmileModuleMemberKind Kind { get; }
    public ModuleVisibility Visibility { get; }
    public SourceText Source { get; }
    public TextSpan DeclarationSpan { get; }
    public SourceLocation DeclarationLocation => new(Source, DeclarationSpan);
    public VariableSymbol? Variable { get; internal set; }
    public RoutineSymbol? Routine { get; internal set; }
    public RecordTypeSymbol? Type { get; internal set; }
    internal string SemanticName { get; }
}

public sealed class ModuleSymbol
{
    private readonly Dictionary<string, SmileModuleMember> _members =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SmileModuleMember> _types =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SyntaxTree> _syntaxTrees = new();

    internal ModuleSymbol(string name, string providerIdentity)
    {
        Name = name;
        ProviderIdentity = providerIdentity;
    }

    public string Name { get; }
    public string ProviderIdentity { get; }
    public IReadOnlyDictionary<string, SmileModuleMember> Members => _members;
    public IReadOnlyDictionary<string, SmileModuleMember> Types => _types;
    public IReadOnlyList<SyntaxTree> SyntaxTrees => _syntaxTrees;
    public IEnumerable<SmileModuleMember> PublicMembers =>
        _members.Values.Concat(_types.Values).Where(member => member.Visibility == ModuleVisibility.Public);

    internal Dictionary<string, SmileModuleMember> MutableMembers => _members;
    internal Dictionary<string, SmileModuleMember> MutableTypes => _types;
    internal IEnumerable<SmileModuleMember> AllMembers => _members.Values.Concat(_types.Values);
    internal List<SyntaxTree> MutableSyntaxTrees => _syntaxTrees;
}

internal sealed class ModuleProcessingResult
{
    public ModuleProcessingResult(IReadOnlyList<SyntaxTree> boundTrees,
        IReadOnlyDictionary<string, ModuleSymbol> modules,
        IReadOnlyDictionary<SourceText, IReadOnlyDictionary<string, ModuleSymbol>> imports,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        BoundTrees = boundTrees;
        Modules = modules;
        Imports = imports;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<SyntaxTree> BoundTrees { get; }
    public IReadOnlyDictionary<string, ModuleSymbol> Modules { get; }
    public IReadOnlyDictionary<SourceText, IReadOnlyDictionary<string, ModuleSymbol>> Imports { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public IReadOnlyList<Diagnostic> Link(SemanticModel model)
    {
        foreach (var module in Modules.Values)
        {
            foreach (var member in module.AllMembers)
            {
                if (member.Kind is SmileModuleMemberKind.Constant or SmileModuleMemberKind.Variable or SmileModuleMemberKind.Array)
                {
                    if (model.Symbols.TryGetValue(member.SemanticName, out var variable))
                    {
                        variable.ApplyModuleIdentity(member.Name, module.Name, member.Visibility,
                            module.ProviderIdentity, member.RuntimeIdentity);
                        member.Variable = variable;
                    }
                }
                else if (member.Kind == SmileModuleMemberKind.Type)
                {
                    if (model.Types.TryGetValue(member.SemanticName, out var type))
                    {
                        type.ApplyModuleIdentity(member.Name, module.Name, member.Visibility,
                            module.ProviderIdentity, member.RuntimeIdentity);
                        member.Type = type;
                    }
                }
                else if (model.Routines.TryGetValue(member.SemanticName, out var routine))
                {
                    routine.ApplyModuleIdentity(member.Name, module.Name, member.Visibility,
                        module.ProviderIdentity, member.RuntimeIdentity);
                    member.Routine = routine;
                }
            }
        }
        model.SetModules(Modules, Imports);
        var diagnostics = new List<Diagnostic>();
        foreach (var module in Modules.Values)
        {
            foreach (var member in module.PublicMembers)
            {
                if (member.Type != null)
                {
                    foreach (var field in member.Type.Fields.Where(field => IsInaccessible(field.Type)))
                        diagnostics.Add(new Diagnostic("SML3409", DiagnosticSeverity.Error,
                            $"Public Type '{module.Name}.{member.Name}' exposes inaccessible type '{field.Type.Name}' through field '{field.Name}'.",
                            field.Source, field.TypeToken.Span));
                }
                if (member.Variable != null && IsInaccessible(member.Variable.Type))
                    diagnostics.Add(new Diagnostic("SML3409", DiagnosticSeverity.Error,
                        $"Public member '{module.Name}.{member.Name}' exposes inaccessible type '{member.Variable.Type.Name}'.",
                        member.Source, member.DeclarationSpan));
                if (member.Routine != null)
                {
                    if (member.Routine.IsFunction && IsInaccessible(member.Routine.ReturnType))
                        diagnostics.Add(new Diagnostic("SML3409", DiagnosticSeverity.Error,
                            $"Public Function '{module.Name}.{member.Name}' returns inaccessible type '{member.Routine.ReturnType.Name}'.",
                            member.Source, member.Routine.Declaration.ReturnTypeToken?.Span ?? member.DeclarationSpan));
                    foreach (var parameter in member.Routine.Parameters.Where(parameter => IsInaccessible(parameter.Type)))
                        diagnostics.Add(new Diagnostic("SML3409", DiagnosticSeverity.Error,
                            $"Public routine '{module.Name}.{member.Name}' exposes inaccessible parameter type '{parameter.Type.Name}'.",
                            parameter.Source, parameter.DeclarationSpan));
                }
            }
        }
        return diagnostics;

        static bool IsInaccessible(SmileType type) =>
            type is RecordTypeSymbol record && record.ModuleName != null && record.Visibility != ModuleVisibility.Public;
    }
}

internal sealed class ModuleProcessor
{
    private readonly IReadOnlyList<SyntaxTree> _trees;
    private readonly SmileCompilationKind _kind;
    private readonly SmileCompilationDependencyContext _dependencyContext;
    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<string, ModuleSymbol> _modules = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<SourceText, ModuleSymbol> _moduleBySource = new();
    private readonly Dictionary<SourceText, Dictionary<string, ModuleSymbol>> _imports = new();
    private readonly Dictionary<SourceText, List<ImportStatementSyntax>> _importSyntax = new();

    public ModuleProcessor(IReadOnlyList<SyntaxTree> trees, SmileCompilationKind kind,
        SmileCompilationDependencyContext dependencyContext)
    {
        _trees = trees;
        _kind = kind;
        _dependencyContext = dependencyContext;
    }

    public ModuleProcessingResult Process()
    {
        ValidateOptionExplicit();
        InventoryModules();
        InventoryImports();
        DiagnoseCycles();

        var boundTrees = _trees.Select(LowerTree).ToList();
        if (_kind == SmileCompilationKind.Library)
        {
            var source = new SourceText(string.Empty, string.Empty);
            boundTrees.Insert(0, new SyntaxTree(source,
                new CompilationUnitSyntax(Array.Empty<StatementSyntax>(),
                    new SyntaxToken(SyntaxKind.EndOfFileToken, 0, string.Empty)),
                new[] { new SyntaxToken(SyntaxKind.EndOfFileToken, 0, string.Empty) },
                isStartup: true, providerIdentity: "<library-entry>"));
        }

        var imports = _imports.ToDictionary(item => item.Key,
            item => (IReadOnlyDictionary<string, ModuleSymbol>)item.Value);
        return new ModuleProcessingResult(boundTrees, _modules, imports, _diagnostics.ToArray());
    }

    private void ValidateOptionExplicit()
    {
        foreach (var tree in _trees)
        {
            var modules = tree.Root.Statements.OfType<ModuleDeclarationSyntax>().ToArray();
            var allowed = modules.Length == 1 && ReferenceEquals(tree.Root.Statements.FirstOrDefault(), modules[0])
                ? modules[0].Statements.FirstOrDefault() as OptionExplicitStatementSyntax
                : tree.Root.Statements.FirstOrDefault() as OptionExplicitStatementSyntax;
            foreach (var option in EnumerateOptions(tree.Root.Statements))
            {
                if (!ReferenceEquals(option, allowed))
                    Report(tree.Source, "SML3300", option.Span,
                        "Option Explicit must be the first statement in its physical source and may appear only once.");
            }
        }
    }

    private static IEnumerable<OptionExplicitStatementSyntax> EnumerateOptions(IEnumerable<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is OptionExplicitStatementSyntax option)
                yield return option;
            if (statement is ModuleDeclarationSyntax module)
                foreach (var nested in EnumerateOptions(module.Statements)) yield return nested;
            if (statement is RoutineDeclarationSyntax routine)
                foreach (var nested in EnumerateOptions(routine.Statements)) yield return nested;
            if (statement is IfStatementSyntax conditional)
            {
                foreach (var clause in conditional.Clauses)
                    foreach (var nested in EnumerateOptions(clause.Statements)) yield return nested;
                foreach (var nested in EnumerateOptions(conditional.ElseStatements)) yield return nested;
            }
            if (statement is ForStatementSyntax forStatement)
                foreach (var nested in EnumerateOptions(forStatement.Statements)) yield return nested;
            if (statement is DoStatementSyntax doStatement)
                foreach (var nested in EnumerateOptions(doStatement.Statements)) yield return nested;
            if (statement is SelectStatementSyntax select)
                foreach (var clause in select.Cases)
                    foreach (var nested in EnumerateOptions(clause.Statements)) yield return nested;
        }
    }

    private void InventoryModules()
    {
        foreach (var tree in _trees)
        {
            var declarations = tree.Root.Statements.OfType<ModuleDeclarationSyntax>().ToArray();
            if (declarations.Length == 0)
            {
                if (_kind == SmileCompilationKind.Library)
                    Report(tree.Source, "SML3101", tree.Root.Span,
                        "Every library source must declare exactly one Module.");
                continue;
            }

            if (declarations.Length != 1 || tree.Root.Statements.Count != 1 ||
                !ReferenceEquals(tree.Root.Statements[0], declarations[0]))
            {
                Report(tree.Source, "SML3100", declarations[0].ModuleKeyword.Span,
                    "Module must be the first statement, exactly one module is allowed per source, and only comments may follow End Module.");
            }

            var declaration = declarations[0];
            var name = declaration.Name.Name;
            var provider = string.IsNullOrWhiteSpace(tree.ProviderIdentity) ? "<local>" : tree.ProviderIdentity;
            if (!_modules.TryGetValue(name, out var module))
            {
                module = new ModuleSymbol(name, provider);
                _modules.Add(name, module);
            }
            else if (!string.Equals(module.ProviderIdentity, provider, StringComparison.OrdinalIgnoreCase))
            {
                Report(tree.Source, "SML3107", declaration.Name.Span,
                    $"Module '{name}' is provided by both '{module.ProviderIdentity}' and '{provider}'.");
            }

            module.MutableSyntaxTrees.Add(tree);
            _moduleBySource[tree.Source] = module;
            InventoryMembers(tree, declaration, module);
        }
    }

    private void InventoryMembers(SyntaxTree tree, ModuleDeclarationSyntax declaration, ModuleSymbol module)
    {
        var sawDeclaration = false;
        foreach (var statement in declaration.Statements)
        {
            if (statement is OptionExplicitStatementSyntax)
                continue;
            if (statement is ImportStatementSyntax)
            {
                if (sawDeclaration)
                    Report(tree.Source, "SML3106", statement.Span,
                        "Module imports must appear immediately after Module and before declarations.");
                continue;
            }

            sawDeclaration = true;
            var visibility = ModuleVisibility.Private;
            var member = statement;
            if (statement is VisibilityDeclarationSyntax visible)
            {
                visibility = visible.Visibility;
                member = visible.Declaration;
            }

            var (identifier, kind) = member switch
            {
                ConstStatementSyntax constant => (constant.Identifier, SmileModuleMemberKind.Constant),
                DimStatementSyntax variable => (variable.Identifier,
                    variable.IsArray ? SmileModuleMemberKind.Array : SmileModuleMemberKind.Variable),
                TypeDeclarationSyntax type => (type.Identifier, SmileModuleMemberKind.Type),
                RoutineDeclarationSyntax routine when routine.IsFunction => (routine.Identifier, SmileModuleMemberKind.Function),
                RoutineDeclarationSyntax routine => (routine.Identifier, SmileModuleMemberKind.Subroutine),
                _ => (null, SmileModuleMemberKind.Constant)
            };
            if (identifier == null)
            {
                Report(tree.Source, "SML3101", statement.Span,
                    "Module sources may contain only Import and Const, Dim, Type, Sub, or Function declarations.");
                continue;
            }

            var memberTable = kind == SmileModuleMemberKind.Type ? module.MutableTypes : module.MutableMembers;
            if (memberTable.ContainsKey(identifier.Text))
            {
                Report(tree.Source, "SML3104", identifier.Span,
                    $"Module '{module.Name}' already declares member '{identifier.Text}'.");
                continue;
            }

            var runtimeIdentity = module.Name + "::" + identifier.Text;
            var semanticName = SemanticName(module.Name, identifier.Text);
            memberTable.Add(identifier.Text, new SmileModuleMember(identifier.Text, runtimeIdentity,
                semanticName, kind, visibility, tree.Source, identifier.Span));
        }
    }

    private void InventoryImports()
    {
        foreach (var tree in _trees)
        {
            var sourceImports = new List<ImportStatementSyntax>();
            var statements = _moduleBySource.TryGetValue(tree.Source, out _)
                ? tree.Root.Statements.OfType<ModuleDeclarationSyntax>().First().Statements
                : tree.Root.Statements;
            var sawNonImport = false;
            foreach (var statement in statements)
            {
                if (statement is OptionExplicitStatementSyntax)
                    continue;
                if (statement is ImportStatementSyntax import)
                {
                    if (sawNonImport)
                        Report(tree.Source, "SML3106", import.Span,
                            "Import statements must appear before declarations or executable statements.");
                    sourceImports.Add(import);
                }
                else
                {
                    sawNonImport = true;
                }

                if (statement is VisibilityDeclarationSyntax && !_moduleBySource.ContainsKey(tree.Source))
                    Report(tree.Source, "SML3101", statement.Span,
                        "Public and Private are valid only on declarations inside a Module.");
            }

            var aliases = new Dictionary<string, ModuleSymbol>(StringComparer.OrdinalIgnoreCase);
            var importedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var import in sourceImports)
            {
                if (aliases.ContainsKey(import.Alias.Text))
                {
                    Report(tree.Source, "SML3106", import.Alias.Span,
                        $"Import alias '{import.Alias.Text}' is already declared in this source.");
                    continue;
                }
                if (!importedModules.Add(import.ModuleName.Name))
                {
                    Report(tree.Source, "SML3106", import.ModuleName.Span,
                        $"Module '{import.ModuleName.Name}' is already imported in this source.");
                    continue;
                }
                if (!_modules.TryGetValue(import.ModuleName.Name, out var imported))
                {
                    Report(tree.Source, "SML3102", import.ModuleName.Span,
                        $"Imported module '{import.ModuleName.Name}' was not found.");
                    continue;
                }
                if (!_dependencyContext.CanAccess(tree.ProviderIdentity, imported.ProviderIdentity))
                {
                    Report(tree.Source, "SML3208", import.ModuleName.Span,
                        _dependencyContext.DescribeInaccessibleImport(import.ModuleName.Name,
                            tree.ProviderIdentity, imported.ProviderIdentity));
                    continue;
                }
                if (_moduleBySource.TryGetValue(tree.Source, out var current) &&
                    (current.Members.ContainsKey(import.Alias.Text) || current.Types.ContainsKey(import.Alias.Text)))
                {
                    Report(tree.Source, "SML3106", import.Alias.Span,
                        $"Import alias '{import.Alias.Text}' conflicts with a declaration in module '{current.Name}'.");
                    continue;
                }
                aliases.Add(import.Alias.Text, imported);
            }
            _imports[tree.Source] = aliases;
            _importSyntax[tree.Source] = sourceImports;
        }
    }

    private void DiagnoseCycles()
    {
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<ModuleSymbol>();
        foreach (var module in _modules.Values.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            Visit(module);

        void Visit(ModuleSymbol module)
        {
            if (state.TryGetValue(module.Name, out var existing) && existing == 2)
                return;
            if (existing == 1)
                return;
            state[module.Name] = 1;
            stack.Add(module);
            var edges = module.SyntaxTrees.SelectMany(tree => _imports[tree.Source].Values)
                .Distinct().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var target in edges)
            {
                if (state.TryGetValue(target.Name, out var targetState) && targetState == 1)
                {
                    var start = stack.FindIndex(item => string.Equals(item.Name, target.Name,
                        StringComparison.OrdinalIgnoreCase));
                    var cycle = stack.Skip(Math.Max(0, start)).Select(item => item.Name)
                        .Concat(new[] { target.Name }).ToArray();
                    foreach (var tree in module.SyntaxTrees)
                    {
                        var site = _importSyntax[tree.Source].FirstOrDefault(import =>
                            string.Equals(import.ModuleName.Name, target.Name, StringComparison.OrdinalIgnoreCase));
                        if (site != null)
                            Report(tree.Source, "SML3108", site.ModuleName.Span,
                                $"Circular module import detected: {string.Join(" -> ", cycle)}.");
                    }
                    continue;
                }
                Visit(target);
            }
            stack.RemoveAt(stack.Count - 1);
            state[module.Name] = 2;
        }
    }

    private SyntaxTree LowerTree(SyntaxTree tree)
    {
        IReadOnlyList<StatementSyntax> statements;
        if (_moduleBySource.TryGetValue(tree.Source, out var module))
        {
            var declaration = tree.Root.Statements.OfType<ModuleDeclarationSyntax>().First();
            statements = declaration.Statements
                .Where(statement => statement is not (ImportStatementSyntax or OptionExplicitStatementSyntax))
                .Select(statement => LowerModuleMember(statement, tree, module))
                .Where(statement => statement != null).Cast<StatementSyntax>().ToArray();
        }
        else
        {
            statements = tree.Root.Statements.Where(statement => statement is not (ImportStatementSyntax or OptionExplicitStatementSyntax))
                .Select(statement => LowerStatement(statement, tree, null, null)).ToArray();
        }
        return new SyntaxTree(tree.Source, new CompilationUnitSyntax(statements, tree.Root.EndOfFileToken),
            tree.Tokens, tree.IsStartup, tree.ProviderIdentity, tree.OptionExplicit);
    }

    private StatementSyntax? LowerModuleMember(StatementSyntax statement, SyntaxTree tree, ModuleSymbol module)
    {
        if (statement is VisibilityDeclarationSyntax visible)
            statement = visible.Declaration;
        if (statement is not (ConstStatementSyntax or DimStatementSyntax or TypeDeclarationSyntax or RoutineDeclarationSyntax))
            return null;
        return LowerStatement(statement, tree, module, null);
    }

    private StatementSyntax LowerStatement(StatementSyntax statement, SyntaxTree tree, ModuleSymbol? module,
        HashSet<string>? locals)
    {
        switch (statement)
        {
            case ConstStatementSyntax constant:
                return new ConstStatementSyntax(constant.ConstKeyword,
                    DeclarationToken(constant.Identifier, module), constant.EqualsToken,
                    LowerExpression(constant.Expression, tree, module, locals));
            case DimStatementSyntax dim:
                return new DimStatementSyntax(dim.DimKeyword, DeclarationToken(dim.Identifier, module), dim.OpenBracket,
                    dim.Sizes.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), dim.CloseBracket,
                    dim.AsKeyword, LowerTypeToken(dim.TypeToken, tree, module));
            case TypeDeclarationSyntax type:
                return new TypeDeclarationSyntax(type.TypeKeyword, TypeDeclarationToken(type.Identifier, module),
                    type.Fields.Select(field => new RecordFieldDeclarationSyntax(field.Identifier, field.AsKeyword,
                        LowerTypeToken(field.TypeToken, tree, module)!)).ToArray(), type.EndKeyword, type.FinalTypeKeyword);
            case RoutineDeclarationSyntax routine:
            {
                var routineLocals = CollectRoutineLocals(routine, module);
                return new RoutineDeclarationSyntax(routine.Keyword, DeclarationToken(routine.Identifier, module),
                    routine.Parameters.Select(parameter => new ParameterSyntax(parameter.ModeKeyword, parameter.Identifier,
                        parameter.AsKeyword, LowerTypeToken(parameter.TypeToken, tree, module))).ToArray(), routine.AsKeyword,
                    LowerTypeToken(routine.ReturnTypeToken, tree, module),
                    routine.Statements.Select(item => LowerStatement(item, tree, module, routineLocals)).ToArray(),
                    routine.EndKeyword, routine.FinalKeyword);
            }
            case AssignmentStatementSyntax assignment:
            {
                return new AssignmentStatementSyntax(LowerTarget(assignment.Target, tree, module, locals), assignment.EqualsToken,
                    LowerExpression(assignment.Expression, tree, module, locals));
            }
            case PrintStatementSyntax print:
                return new PrintStatementSyntax(print.PrintKeyword,
                    print.Items.Select(item => LowerExpression(item, tree, module, locals)).ToArray(),
                    print.SuppressNewLine, print.Span.End);
            case GetKeyStatementSyntax getKey:
                return new GetKeyStatementSyntax(getKey.GetKeyword, getKey.KeyKeyword,
                    ReferenceToken(getKey.Identifier, tree, module, locals));
            case WaitStatementSyntax wait:
                return new WaitStatementSyntax(wait.WaitKeyword,
                    LowerExpression(wait.Duration, tree, module, locals), wait.MillisecondsKeyword);
            case RandomStatementSyntax random:
                return new RandomStatementSyntax(random.RandomKeyword,
                    ReferenceToken(random.Identifier, tree, module, locals), random.FromKeyword,
                    LowerExpression(random.Minimum, tree, module, locals), random.ToKeyword,
                    LowerExpression(random.Maximum, tree, module, locals));
            case IfStatementSyntax conditional:
                return new IfStatementSyntax(conditional.IfKeyword,
                    conditional.Clauses.Select(clause => new IfClauseSyntax(
                        LowerExpression(clause.Condition, tree, module, locals),
                        clause.Statements.Select(item => LowerStatement(item, tree, module, locals)).ToArray())).ToArray(),
                    conditional.ElseStatements.Select(item => LowerStatement(item, tree, module, locals)).ToArray(),
                    conditional.EndKeyword, conditional.FinalIfKeyword);
            case ForStatementSyntax loop:
                return new ForStatementSyntax(loop.ForKeyword, ReferenceToken(loop.Identifier, tree, module, locals),
                    LowerExpression(loop.LowerBound, tree, module, locals), loop.IsDescending,
                    LowerExpression(loop.UpperBound, tree, module, locals),
                    loop.Statements.Select(item => LowerStatement(item, tree, module, locals)).ToArray(), loop.FinalForKeyword);
            case DoStatementSyntax loop:
                return new DoStatementSyntax(loop.DoKeyword,
                    loop.Statements.Select(item => LowerStatement(item, tree, module, locals)).ToArray(), loop.LoopKeyword,
                    loop.UntilCondition == null ? null : LowerExpression(loop.UntilCondition, tree, module, locals));
            case CallStatementSyntax call:
                return new CallStatementSyntax(call.CallKeyword, ReferenceToken(call.Identifier, tree, module, locals),
                    call.Arguments.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), call.CloseParenthesis);
            case QualifiedCallStatementSyntax call:
                return new CallStatementSyntax(call.CallKeyword,
                    QualifiedToken(tree, call.Alias, call.Member,
                        member => member.Kind is SmileModuleMemberKind.Subroutine or SmileModuleMemberKind.Function),
                    call.Arguments.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), call.CloseParenthesis);
            case ReturnStatementSyntax value:
                return new ReturnStatementSyntax(value.ReturnKeyword,
                    value.Expression == null ? null : LowerExpression(value.Expression, tree, module, locals));
            case SelectStatementSyntax select:
                return new SelectStatementSyntax(select.SelectKeyword,
                    LowerExpression(select.Expression, tree, module, locals),
                    select.Cases.Select(clause => new SelectCaseClauseSyntax(clause.CaseKeyword,
                        clause.Value == null ? null : LowerExpression(clause.Value, tree, module, locals), clause.IsElse,
                        clause.Statements.Select(item => LowerStatement(item, tree, module, locals)).ToArray())).ToArray(),
                    select.EndKeyword, select.FinalSelectKeyword);
            case GameWindowStatementSyntax game:
                return new GameWindowStatementSyntax(game.GameKeyword, game.Title,
                    game.Width == null ? null : LowerExpression(game.Width, tree, module, locals),
                    game.Height == null ? null : LowerExpression(game.Height, tree, module, locals), game.Span.End);
            case ClearColorStatementSyntax clear:
                return new ClearColorStatementSyntax(clear.ClearKeyword,
                    LowerExpression(clear.Color, tree, module, locals));
            case GraphicsStatementSyntax graphics:
                return new GraphicsStatementSyntax(graphics.Keyword, graphics.Operation,
                    graphics.Arguments.Select(item => LowerExpression(item, tree, module, locals)).ToArray(),
                    graphics.TextExpression == null ? null : LowerExpression(graphics.TextExpression, tree, module, locals),
                    graphics.Centered, graphics.Span.End);
            case DrawImageStatementSyntax image:
                return new DrawImageStatementSyntax(image.DrawKeyword, image.ImageKeyword,
                    LowerExpression(image.Image, tree, module, locals),
                    LowerOptional(image.SourceX, tree, module, locals), LowerOptional(image.SourceY, tree, module, locals),
                    LowerOptional(image.SourceWidth, tree, module, locals), LowerOptional(image.SourceHeight, tree, module, locals),
                    LowerExpression(image.DestinationX, tree, module, locals),
                    LowerExpression(image.DestinationY, tree, module, locals),
                    LowerOptional(image.DestinationWidth, tree, module, locals),
                    LowerOptional(image.DestinationHeight, tree, module, locals),
                    LowerOptional(image.Opacity, tree, module, locals), image.Filter, image.Flip,
                    LowerOptional(image.AnchorX, tree, module, locals), LowerOptional(image.AnchorY, tree, module, locals),
                    image.Span.End);
            case ImageLoadStatementSyntax image:
                return new ImageLoadStatementSyntax(image.Keyword, image.ImageKeyword,
                    LowerTarget(image.Target, tree, module, locals), LowerOptional(image.Path, tree, module, locals));
            case ClipRectangleStatementSyntax clip:
                return new ClipRectangleStatementSyntax(clip.ClipKeyword,
                    clip.Arguments.Select(item => LowerExpression(item, tree, module, locals)).ToArray(),
                    clip.Statements.Select(item => LowerStatement(item, tree, module, locals)).ToArray(),
                    clip.EndKeyword, clip.FinalClipKeyword);
            case SoundStatementSyntax sound:
                return new SoundStatementSyntax(sound.Keyword, sound.SoundKeyword, sound.Path,
                    LowerOptional(sound.Channel, tree, module, locals));
            case MusicStatementSyntax music:
                return new MusicStatementSyntax(music.Keyword, music.MusicKeyword, music.Operation, music.Path,
                    music.LoopKeyword, music.Volume == null ? null : LowerExpression(music.Volume, tree, module, locals));
            case LoadStatementSyntax load:
                return new LoadStatementSyntax(load.LoadKeyword, ReferenceToken(load.Identifier, tree, module, locals),
                    load.Key, LowerExpression(load.DefaultValue, tree, module, locals));
            case TextFileLoadStatementSyntax load:
                return new TextFileLoadStatementSyntax(load.LoadKeyword, load.TextKeyword, load.FileKeyword,
                    LowerExpression(load.Path, tree, module, locals),
                    load.IntoKeyword, ReferenceToken(load.Destination, tree, module, locals), load.CountKeyword,
                    ReferenceToken(load.CountIdentifier, tree, module, locals));
            case DataLoadStatementSyntax load:
                return new DataLoadStatementSyntax(load.LoadKeyword, load.DataKeyword,
                    LowerExpression(load.Key, tree, module, locals),
                    ReferenceToken(load.Destination, tree, module, locals),
                    LowerTarget(load.CountTarget, tree, module, locals));
            case DataSaveStatementSyntax save:
                return new DataSaveStatementSyntax(save.SaveKeyword, save.DataKeyword,
                    ReferenceToken(save.Source, tree, module, locals),
                    LowerExpression(save.Count, tree, module, locals), LowerExpression(save.Key, tree, module, locals));
            case SaveStatementSyntax save:
                return new SaveStatementSyntax(save.SaveKeyword, ReferenceToken(save.Identifier, tree, module, locals), save.Key);
            default:
                return statement;
        }
    }

    private ExpressionSyntax? LowerOptional(ExpressionSyntax? expression, SyntaxTree tree, ModuleSymbol? module,
        HashSet<string>? locals) => expression == null ? null : LowerExpression(expression, tree, module, locals);

    private AssignmentTargetSyntax LowerTarget(AssignmentTargetSyntax target, SyntaxTree tree, ModuleSymbol? module,
        HashSet<string>? locals)
    {
        var importedQualifier = target.IsQualified && _imports.TryGetValue(tree.Source, out var aliases) &&
            aliases.ContainsKey(target.Qualifier!.Text);
        var identifier = importedQualifier
            ? QualifiedToken(tree, target.Qualifier!, target.Identifier,
                member => member.Kind is SmileModuleMemberKind.Constant or SmileModuleMemberKind.Variable or SmileModuleMemberKind.Array)
            : ReferenceToken(target.IsQualified ? target.Qualifier! : target.Identifier, tree, module, locals);
        var fields = importedQualifier ? target.Fields : target.IsQualified
            ? new[] { target.Identifier }.Concat(target.Fields).ToArray() : target.Fields;
        var fieldDots = importedQualifier ? target.FieldDots : target.IsQualified
            ? new[] { target.DotToken! }.Concat(target.FieldDots).ToArray() : target.FieldDots;
        return new AssignmentTargetSyntax(identifier, target.OpenBracket,
            target.Indices.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), target.CloseBracket,
            fieldDots: fieldDots, fields: fields);
    }

    private ExpressionSyntax LowerExpression(ExpressionSyntax expression, SyntaxTree tree, ModuleSymbol? module,
        HashSet<string>? locals)
    {
        switch (expression)
        {
            case NameExpressionSyntax name:
                return new NameExpressionSyntax(ReferenceToken(name.Identifier, tree, module, locals));
            case ArrayAccessExpressionSyntax array:
                return new ArrayAccessExpressionSyntax(ReferenceToken(array.Identifier, tree, module, locals),
                    array.Indices.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), array.CloseBracket);
            case CallExpressionSyntax call:
                return new CallExpressionSyntax(ReferenceToken(call.Identifier, tree, module, locals),
                    call.Arguments.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), call.CloseParenthesis);
            case QualifiedNameExpressionSyntax name:
                if (_imports.TryGetValue(tree.Source, out var aliases) && aliases.ContainsKey(name.Alias.Text))
                    return new NameExpressionSyntax(QualifiedToken(tree, name.Alias, name.Member,
                        member => member.Kind is SmileModuleMemberKind.Constant or SmileModuleMemberKind.Variable or SmileModuleMemberKind.Array));
                return new FieldAccessExpressionSyntax(
                    new NameExpressionSyntax(ReferenceToken(name.Alias, tree, module, locals)), name.DotToken, name.Member);
            case QualifiedArrayAccessExpressionSyntax array:
                return new ArrayAccessExpressionSyntax(QualifiedToken(tree, array.Alias, array.Member,
                        member => member.Kind == SmileModuleMemberKind.Array),
                    array.Indices.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), array.CloseBracket);
            case QualifiedCallExpressionSyntax call:
                return new CallExpressionSyntax(QualifiedToken(tree, call.Alias, call.Member,
                        member => member.Kind is SmileModuleMemberKind.Subroutine or SmileModuleMemberKind.Function),
                    call.Arguments.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), call.CloseParenthesis);
            case FieldAccessExpressionSyntax field:
                return new FieldAccessExpressionSyntax(LowerExpression(field.Receiver, tree, module, locals),
                    field.DotToken, field.Field);
            case ParenthesizedExpressionSyntax parenthesized:
                return new ParenthesizedExpressionSyntax(parenthesized.OpenParenthesis,
                    LowerExpression(parenthesized.Expression, tree, module, locals), parenthesized.CloseParenthesis);
            case UnaryExpressionSyntax unary:
                return new UnaryExpressionSyntax(unary.OperatorToken,
                    LowerExpression(unary.Operand, tree, module, locals));
            case BinaryExpressionSyntax binary:
                return new BinaryExpressionSyntax(LowerExpression(binary.Left, tree, module, locals),
                    binary.OperatorToken, LowerExpression(binary.Right, tree, module, locals));
            default:
                return expression;
        }
    }

    private SyntaxToken DeclarationToken(SyntaxToken token, ModuleSymbol? module)
    {
        if (module == null || !module.Members.TryGetValue(token.Text, out var member))
            return token;
        return SemanticToken(token, member.SemanticName, module.Name + "." + member.Name);
    }

    private SyntaxToken TypeDeclarationToken(SyntaxToken token, ModuleSymbol? module)
    {
        if (module == null || !module.Types.TryGetValue(token.Text, out var member))
            return token;
        return SemanticToken(token, member.SemanticName);
    }

    private SyntaxToken? LowerTypeToken(SyntaxToken? token, SyntaxTree tree, ModuleSymbol? module)
    {
        if (token == null || token.Kind is SyntaxKind.NumberKeyword or SyntaxKind.BooleanKeyword or SyntaxKind.TextKeyword or SyntaxKind.ImageKeyword)
            return token;
        if (string.IsNullOrWhiteSpace(token.Text))
            return token;
        var dot = token.Text.IndexOf('.');
        if (dot > 0)
        {
            var aliasText = token.Text.Substring(0, dot);
            var memberText = token.Text.Substring(dot + 1);
            var alias = new SyntaxToken(SyntaxKind.IdentifierToken, token.Position, aliasText,
                spanLength: aliasText.Length);
            var member = new SyntaxToken(SyntaxKind.IdentifierToken, token.Position + dot + 1, memberText,
                spanLength: memberText.Length);
            return QualifiedTypeToken(tree, alias, member);
        }
        if (module != null && module.Types.TryGetValue(token.Text, out var own))
            return SemanticToken(token, own.SemanticName);
        if (module != null)
        {
            Report(tree.Source, "SML3401", token.Span,
                $"Module '{module.Name}' cannot resolve unqualified record type '{token.Text}'. " +
                "Import the defining module and use Alias.Type for an external record type.");
            return SemanticToken(token, "__smile_missing_" + SafeIdentifier(token.Text));
        }
        return token;
    }

    private SyntaxToken ReferenceToken(SyntaxToken token, SyntaxTree tree, ModuleSymbol? module,
        HashSet<string>? locals)
    {
        if (SyntaxFacts.IsBuiltInFunction(token.Kind) || SyntaxFacts.IsBuiltInConstant(token.Kind))
            return token;
        if (locals != null && locals.Contains(token.Text))
            return token;
        if (module != null && module.Members.TryGetValue(token.Text, out var member))
            return SemanticToken(token, member.SemanticName);
        if (module != null && token.Kind is SyntaxKind.IdentifierToken or SyntaxKind.KeyKeyword)
            Report(tree.Source, module.Types.ContainsKey(token.Text) ? "SML3410" : "SML3110", token.Span,
                module.Types.ContainsKey(token.Text)
                    ? $"Type name '{token.Text}' cannot be used as a value."
                    : $"Module '{module.Name}' cannot access undeclared or consuming-program name '{token.Text}'.");
        return token;
    }

    private SyntaxToken QualifiedToken(SyntaxTree tree, SyntaxToken alias, SyntaxToken member,
        Func<SmileModuleMember, bool> expectedKind)
    {
        if (!_imports.TryGetValue(tree.Source, out var aliases) || !aliases.TryGetValue(alias.Text, out var module))
        {
            Report(tree.Source, "SML3102", alias.Span, $"Import alias '{alias.Text}' was not found in this source.");
            return SemanticToken(member, "__smile_missing_" + SafeIdentifier(member.Text));
        }
        SmileModuleMember? resolved = null;
        if (module.Members.TryGetValue(member.Text, out var valueCandidate) && expectedKind(valueCandidate))
            resolved = valueCandidate;
        else if (module.Types.TryGetValue(member.Text, out var typeCandidate) && expectedKind(typeCandidate))
            resolved = typeCandidate;
        if (resolved == null)
        {
            var wrongContext = module.Members.ContainsKey(member.Text) || module.Types.ContainsKey(member.Text);
            Report(tree.Source, wrongContext ? "SML3410" : "SML3103", member.Span,
                wrongContext
                    ? $"Member '{module.Name}.{member.Text}' cannot be used in this type or value context."
                    : $"Module '{module.Name}' does not contain member '{member.Text}'.");
            return SemanticToken(member, "__smile_missing_" + SafeIdentifier(member.Text));
        }
        if (resolved.Visibility != ModuleVisibility.Public)
        {
            Report(tree.Source, resolved.Kind == SmileModuleMemberKind.Type ? "SML3408" : "SML3105", member.Span,
                $"{(resolved.Kind == SmileModuleMemberKind.Type ? "Type" : "Member")} '{module.Name}.{resolved.Name}' is Private and cannot be accessed through an import.");
            return SemanticToken(member, "__smile_private_" + SafeIdentifier(member.Text));
        }
        return SemanticToken(member, resolved.SemanticName);
    }

    private SyntaxToken QualifiedTypeToken(SyntaxTree tree, SyntaxToken alias, SyntaxToken member)
    {
        if (!_imports.TryGetValue(tree.Source, out var aliases) || !aliases.TryGetValue(alias.Text, out var module))
        {
            Report(tree.Source, "SML3102", alias.Span, $"Import alias '{alias.Text}' was not found in this source.");
            return SemanticToken(member, "__smile_missing_" + SafeIdentifier(member.Text));
        }
        if (!module.Types.TryGetValue(member.Text, out var resolved))
        {
            var code = module.Members.ContainsKey(member.Text) ? "SML3410" : "SML3401";
            Report(tree.Source, code, member.Span, code == "SML3410"
                ? $"Member '{module.Name}.{member.Text}' is a value and cannot be used as a type."
                : $"Module '{module.Name}' does not contain record type '{member.Text}'.");
            return SemanticToken(member, "__smile_missing_" + SafeIdentifier(member.Text));
        }
        if (resolved.Visibility != ModuleVisibility.Public)
        {
            Report(tree.Source, "SML3408", member.Span,
                $"Type '{module.Name}.{resolved.Name}' is Private and cannot be accessed through an import.");
            return SemanticToken(member, "__smile_private_" + SafeIdentifier(member.Text));
        }
        return SemanticToken(member, resolved.SemanticName);
    }

    private static HashSet<string> CollectRoutineLocals(RoutineDeclarationSyntax routine, ModuleSymbol? module)
    {
        var locals = new HashSet<string>(routine.Parameters.Select(parameter => parameter.Identifier.Text),
            StringComparer.OrdinalIgnoreCase);
        Collect(routine.Statements);
        return locals;

        void Add(SyntaxToken token)
        {
            if (module == null || !module.Members.ContainsKey(token.Text))
                locals.Add(token.Text);
        }

        void Collect(IEnumerable<StatementSyntax> statements)
        {
            foreach (var statement in statements)
            {
                switch (statement)
                {
                    case AssignmentStatementSyntax assignment when !assignment.Target.IsQualified && !assignment.Target.IsArrayElement:
                        Add(assignment.Target.Identifier); break;
                    case DimStatementSyntax dim: Add(dim.Identifier); break;
                    case GetKeyStatementSyntax getKey: Add(getKey.Identifier); break;
                    case RandomStatementSyntax random: Add(random.Identifier); break;
                    case LoadStatementSyntax load: Add(load.Identifier); break;
                    case TextFileLoadStatementSyntax load: Add(load.CountIdentifier); break;
                    case ForStatementSyntax loop: Add(loop.Identifier); Collect(loop.Statements); break;
                    case DoStatementSyntax loop: Collect(loop.Statements); break;
                    case ClipRectangleStatementSyntax clip: Collect(clip.Statements); break;
                    case IfStatementSyntax conditional:
                        foreach (var clause in conditional.Clauses) Collect(clause.Statements);
                        Collect(conditional.ElseStatements); break;
                    case SelectStatementSyntax select:
                        foreach (var clause in select.Cases) Collect(clause.Statements); break;
                }
            }
        }
    }

    private static SyntaxToken SemanticToken(SyntaxToken original, string semanticName, string? displayName = null) =>
        new(SyntaxKind.IdentifierToken, original.Position, semanticName,
            value: displayName ?? original.Value, spanLength: original.Span.Length);

    private static string SemanticName(string moduleName, string memberName)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(moduleName.ToUpperInvariant()));
        var prefix = string.Concat(hash.Take(8).Select(value => value.ToString("x2")));
        return "__smile_module_" + prefix + "_" + SafeIdentifier(memberName);
    }

    private static string SafeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        return builder.Length == 0 ? "member" : builder.ToString();
    }

    private void Report(SourceText source, string code, TextSpan span, string message) =>
        _diagnostics.Report(source, code, span, message);
}
