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

    internal string Describe() => Kind == SmileProviderKind.Loose
        ? "loose source root"
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
    Array,
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
    internal string SemanticName { get; }
}

public sealed class ModuleSymbol
{
    private readonly Dictionary<string, SmileModuleMember> _members =
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
    public IReadOnlyList<SyntaxTree> SyntaxTrees => _syntaxTrees;
    public IEnumerable<SmileModuleMember> PublicMembers =>
        _members.Values.Where(member => member.Visibility == ModuleVisibility.Public);

    internal Dictionary<string, SmileModuleMember> MutableMembers => _members;
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

    public void Link(SemanticModel model)
    {
        foreach (var module in Modules.Values)
        {
            foreach (var member in module.Members.Values)
            {
                if (member.Kind is SmileModuleMemberKind.Constant or SmileModuleMemberKind.Array)
                {
                    if (model.Symbols.TryGetValue(member.SemanticName, out var variable))
                    {
                        variable.ApplyModuleIdentity(member.Name, module.Name, member.Visibility,
                            module.ProviderIdentity, member.RuntimeIdentity);
                        member.Variable = variable;
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

    private void InventoryModules()
    {
        foreach (var tree in _trees)
        {
            var declarations = tree.Root.Statements.OfType<ModuleDeclarationSyntax>().ToArray();
            if (declarations.Length == 0)
            {
                if (_kind == SmileCompilationKind.Library)
                    Report(tree.Source, "SML3101", tree.Root.Span,
                        "Every library source must declare exactly one MODULE.");
                continue;
            }

            if (declarations.Length != 1 || tree.Root.Statements.Count != 1 ||
                !ReferenceEquals(tree.Root.Statements[0], declarations[0]))
            {
                Report(tree.Source, "SML3100", declarations[0].ModuleKeyword.Span,
                    "MODULE must be the first statement, exactly one module is allowed per source, and only comments may follow END MODULE.");
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
            if (statement is ImportStatementSyntax)
            {
                if (sawDeclaration)
                    Report(tree.Source, "SML3106", statement.Span,
                        "Module imports must appear immediately after MODULE and before declarations.");
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
                DimStatementSyntax array => (array.Identifier, SmileModuleMemberKind.Array),
                RoutineDeclarationSyntax routine when routine.IsFunction => (routine.Identifier, SmileModuleMemberKind.Function),
                RoutineDeclarationSyntax routine => (routine.Identifier, SmileModuleMemberKind.Subroutine),
                _ => (null, SmileModuleMemberKind.Constant)
            };
            if (identifier == null)
            {
                Report(tree.Source, "SML3101", statement.Span,
                    "Module sources may contain only IMPORT and CONST, DIM, SUB, or FUNCTION declarations.");
                continue;
            }

            if (module.MutableMembers.ContainsKey(identifier.Text))
            {
                Report(tree.Source, "SML3104", identifier.Span,
                    $"Module '{module.Name}' already declares member '{identifier.Text}'.");
                continue;
            }

            var runtimeIdentity = module.Name + "::" + identifier.Text;
            var semanticName = SemanticName(module.Name, identifier.Text);
            module.MutableMembers.Add(identifier.Text, new SmileModuleMember(identifier.Text, runtimeIdentity,
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
                if (statement is ImportStatementSyntax import)
                {
                    if (sawNonImport)
                        Report(tree.Source, "SML3106", import.Span,
                            "IMPORT statements must appear before declarations or executable statements.");
                    sourceImports.Add(import);
                }
                else
                {
                    sawNonImport = true;
                }

                if (statement is VisibilityDeclarationSyntax && !_moduleBySource.ContainsKey(tree.Source))
                    Report(tree.Source, "SML3101", statement.Span,
                        "PUBLIC and PRIVATE are valid only on declarations inside a MODULE.");
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
                    current.Members.ContainsKey(import.Alias.Text))
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
                .Where(statement => statement is not ImportStatementSyntax)
                .Select(statement => LowerModuleMember(statement, tree, module))
                .Where(statement => statement != null).Cast<StatementSyntax>().ToArray();
        }
        else
        {
            statements = tree.Root.Statements.Where(statement => statement is not ImportStatementSyntax)
                .Select(statement => LowerStatement(statement, tree, null, null)).ToArray();
        }
        return new SyntaxTree(tree.Source, new CompilationUnitSyntax(statements, tree.Root.EndOfFileToken),
            tree.Tokens, tree.IsStartup, tree.ProviderIdentity);
    }

    private StatementSyntax? LowerModuleMember(StatementSyntax statement, SyntaxTree tree, ModuleSymbol module)
    {
        if (statement is VisibilityDeclarationSyntax visible)
            statement = visible.Declaration;
        if (statement is not (ConstStatementSyntax or DimStatementSyntax or RoutineDeclarationSyntax))
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
                    dim.Sizes.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), dim.CloseBracket);
            case RoutineDeclarationSyntax routine:
            {
                var routineLocals = CollectRoutineLocals(routine, module);
                return new RoutineDeclarationSyntax(routine.Keyword, DeclarationToken(routine.Identifier, module),
                    routine.Parameters, routine.Statements.Select(item => LowerStatement(item, tree, module, routineLocals)).ToArray(),
                    routine.EndKeyword, routine.FinalKeyword);
            }
            case AssignmentStatementSyntax assignment:
            {
                var identifier = assignment.Target.IsQualified
                    ? QualifiedToken(tree, assignment.Target.Qualifier!, assignment.Target.Identifier,
                        member => member.Kind is SmileModuleMemberKind.Constant or SmileModuleMemberKind.Array)
                    : ReferenceToken(assignment.Target.Identifier, tree, module, locals);
                var target = new AssignmentTargetSyntax(identifier, assignment.Target.OpenBracket,
                    assignment.Target.Indices.Select(item => LowerExpression(item, tree, module, locals)).ToArray(),
                    assignment.Target.CloseBracket);
                return new AssignmentStatementSyntax(target, assignment.EqualsToken,
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
                    graphics.Text, graphics.Centered, graphics.Span.End);
            case MusicStatementSyntax music:
                return new MusicStatementSyntax(music.Keyword, music.MusicKeyword, music.Operation, music.Path,
                    music.LoopKeyword, music.Volume == null ? null : LowerExpression(music.Volume, tree, module, locals));
            case LoadStatementSyntax load:
                return new LoadStatementSyntax(load.LoadKeyword, ReferenceToken(load.Identifier, tree, module, locals),
                    load.Key, LowerExpression(load.DefaultValue, tree, module, locals));
            case TextFileLoadStatementSyntax load:
                return new TextFileLoadStatementSyntax(load.LoadKeyword, load.TextKeyword, load.FileKeyword, load.Path,
                    load.IntoKeyword, ReferenceToken(load.Destination, tree, module, locals), load.CountKeyword,
                    ReferenceToken(load.CountIdentifier, tree, module, locals));
            case SaveStatementSyntax save:
                return new SaveStatementSyntax(save.SaveKeyword, ReferenceToken(save.Identifier, tree, module, locals), save.Key);
            default:
                return statement;
        }
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
                return new NameExpressionSyntax(QualifiedToken(tree, name.Alias, name.Member,
                    member => member.Kind is SmileModuleMemberKind.Constant or SmileModuleMemberKind.Array));
            case QualifiedArrayAccessExpressionSyntax array:
                return new ArrayAccessExpressionSyntax(QualifiedToken(tree, array.Alias, array.Member,
                        member => member.Kind == SmileModuleMemberKind.Array),
                    array.Indices.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), array.CloseBracket);
            case QualifiedCallExpressionSyntax call:
                return new CallExpressionSyntax(QualifiedToken(tree, call.Alias, call.Member,
                        member => member.Kind is SmileModuleMemberKind.Subroutine or SmileModuleMemberKind.Function),
                    call.Arguments.Select(item => LowerExpression(item, tree, module, locals)).ToArray(), call.CloseParenthesis);
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
        return SemanticToken(token, member.SemanticName);
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
            Report(tree.Source, "SML3110", token.Span,
                $"Module '{module.Name}' cannot access undeclared or consuming-program name '{token.Text}'.");
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
        if (!module.Members.TryGetValue(member.Text, out var resolved))
        {
            Report(tree.Source, "SML3103", member.Span,
                $"Module '{module.Name}' does not contain member '{member.Text}'.");
            return SemanticToken(member, "__smile_missing_" + SafeIdentifier(member.Text));
        }
        if (resolved.Visibility != ModuleVisibility.Public)
        {
            Report(tree.Source, "SML3105", member.Span,
                $"Member '{module.Name}.{resolved.Name}' is PRIVATE and cannot be accessed through an import.");
            return SemanticToken(member, "__smile_private_" + SafeIdentifier(member.Text));
        }
        if (!expectedKind(resolved))
        {
            Report(tree.Source, "SML3103", member.Span,
                $"Member '{module.Name}.{resolved.Name}' cannot be used in this context.");
        }
        return SemanticToken(member, resolved.SemanticName);
    }

    private static HashSet<string> CollectRoutineLocals(RoutineDeclarationSyntax routine, ModuleSymbol? module)
    {
        var locals = new HashSet<string>(routine.Parameters.Select(parameter => parameter.Text),
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
                    case IfStatementSyntax conditional:
                        foreach (var clause in conditional.Clauses) Collect(clause.Statements);
                        Collect(conditional.ElseStatements); break;
                    case SelectStatementSyntax select:
                        foreach (var clause in select.Cases) Collect(clause.Statements); break;
                }
            }
        }
    }

    private static SyntaxToken SemanticToken(SyntaxToken original, string semanticName) =>
        new(SyntaxKind.IdentifierToken, original.Position, semanticName, spanLength: original.Span.Length);

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
