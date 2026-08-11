using System;
using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

public enum SmileType
{
    Error,
    Number,
    Boolean,
    Text
}

public sealed class VariableSymbol
{
    internal VariableSymbol(string name, SmileType type, IReadOnlyList<int> dimensions, SourceText source,
        int sourceOrdinal, TextSpan declarationSpan,
        bool isConstant = false, long constantValue = 0, string? routineName = null)
    {
        Name = name;
        Type = type;
        ArrayDimensions = dimensions;
        Source = source;
        SourceOrdinal = sourceOrdinal;
        DeclarationSpan = declarationSpan;
        IsConstant = isConstant;
        ConstantValue = constantValue;
        RoutineName = routineName;
        var total = 1;
        foreach (var dimension in dimensions)
            total *= dimension;
        ArraySize = dimensions.Count == 0 ? 0 : total;
    }

    public string Name { get; }
    public SmileType Type { get; }
    public bool IsArray => ArrayDimensions.Count != 0;
    public int ArraySize { get; }
    public int ArrayRank => ArrayDimensions.Count;
    public IReadOnlyList<int> ArrayDimensions { get; }
    public bool IsConstant { get; }
    public long ConstantValue { get; }
    public string? RoutineName { get; }
    public SourceText Source { get; }
    public int SourceOrdinal { get; }
    public TextSpan DeclarationSpan { get; }
    public SourceLocation DeclarationLocation => new(Source, DeclarationSpan);
}

public sealed class RoutineSymbol
{
    internal RoutineSymbol(RoutineDeclarationSyntax declaration, IReadOnlyList<VariableSymbol> parameters,
        SmileType returnType, SourceText source, int sourceOrdinal)
    {
        Declaration = declaration;
        Name = declaration.Identifier.Text;
        IsFunction = declaration.IsFunction;
        Parameters = parameters;
        ReturnType = returnType;
        Source = source;
        SourceOrdinal = sourceOrdinal;
        Locals = new Dictionary<string, VariableSymbol>(StringComparer.OrdinalIgnoreCase);
        FirstDeclarations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
            Locals[parameter.Name] = parameter;
    }

    public string Name { get; }
    public bool IsFunction { get; }
    public IReadOnlyList<VariableSymbol> Parameters { get; }
    public SmileType ReturnType { get; }
    public IReadOnlyDictionary<string, VariableSymbol> LocalSymbols => Locals;
    public RoutineDeclarationSyntax Declaration { get; }
    public SourceText Source { get; }
    public int SourceOrdinal { get; }
    public SourceLocation DeclarationLocation => new(Source, Declaration.Identifier.Span);
    internal Dictionary<string, VariableSymbol> Locals { get; }
    internal Dictionary<string, int> FirstDeclarations { get; }
}

public sealed class SemanticModel
{
    private readonly Dictionary<string, VariableSymbol> _symbols;
    private readonly Dictionary<string, RoutineSymbol> _routines;
    private readonly Dictionary<ExpressionSyntax, SmileType> _expressionTypes;

    internal SemanticModel(Dictionary<string, VariableSymbol> symbols, Dictionary<string, RoutineSymbol> routines,
        Dictionary<ExpressionSyntax, SmileType> expressionTypes)
    {
        _symbols = symbols;
        _routines = routines;
        _expressionTypes = expressionTypes;
    }

    public IReadOnlyDictionary<string, VariableSymbol> Symbols => _symbols;
    public IReadOnlyDictionary<string, RoutineSymbol> Routines => _routines;
    public bool TryGetSymbol(string name, out VariableSymbol symbol) => _symbols.TryGetValue(name, out symbol!);
    public bool TryGetRoutine(string name, out RoutineSymbol routine) => _routines.TryGetValue(name, out routine!);

    public bool TryResolveVariable(string name, string? routineName, out VariableSymbol symbol)
    {
        if (routineName != null && _routines.TryGetValue(routineName, out var routine) && routine.Locals.TryGetValue(name, out symbol!))
            return true;
        return _symbols.TryGetValue(name, out symbol!);
    }

    public SmileType GetType(ExpressionSyntax expression) =>
        _expressionTypes.TryGetValue(expression, out var type) ? type : SmileType.Error;
}

internal sealed class SemanticAnalyzer
{
    private readonly IReadOnlyList<SyntaxTree> _syntaxTrees;
    private readonly SyntaxTree _startupTree;
    private readonly Dictionary<SourceText, int> _sourceOrdinals = new();
    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<string, VariableSymbol> _symbols = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RoutineSymbol> _routines = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _globalFirstDeclarations = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _implicitGlobals = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<SyntaxToken> _acceptedProjectDeclarations = new();
    private readonly HashSet<SyntaxToken> _rejectedProjectDeclarations = new();
    private readonly Dictionary<string, ConstantDeclaration> _constantDeclarations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConstantResolutionState> _constantStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _constantResolutionStack = new();
    private readonly Dictionary<ExpressionSyntax, SmileType> _expressionTypes = new();
    private SourceText _currentSource = null!;
    private int _currentSourceOrdinal;
    private RoutineSymbol? _currentRoutine;
    private int _forDepth;
    private int _doDepth;
    private bool _hasGameWindow;
    private int _gameWindowCount;

    public SemanticAnalyzer(IReadOnlyList<SyntaxTree> syntaxTrees, SyntaxTree startupTree)
    {
        _syntaxTrees = syntaxTrees;
        _startupTree = startupTree;
        for (var index = 0; index < syntaxTrees.Count; index++)
            _sourceOrdinals[syntaxTrees[index].Source] = index;
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.ToArray();

    public SemanticModel Analyze()
    {
        foreach (var statement in _startupTree.Root.Statements)
            _hasGameWindow |= statement is GameWindowStatementSyntax;

        InventoryProjectDeclarations();
        foreach (var tree in _syntaxTrees)
            CollectRoutineDeclarations(tree);
        CollectGlobalDeclarations();
        CollectFirstDeclarations(_startupTree.Root.Statements, _globalFirstDeclarations, skipRoutines: true);

        foreach (var tree in _syntaxTrees)
        {
            SetCurrentSource(tree.Source);
            if (tree.IsStartup)
                AnalyzeStatements(tree.Root.Statements, topLevel: true);
            else
                AnalyzeSupportTopLevel(tree.Root.Statements);
        }

        foreach (var routine in _routines.Values.OrderBy(routine => routine.SourceOrdinal).ThenBy(routine => routine.Declaration.Span.Start))
        {
            SetCurrentSource(routine.Source);
            _currentRoutine = routine;
            CollectFirstDeclarations(routine.Declaration.Statements, routine.FirstDeclarations, skipRoutines: false);
            AnalyzeStatements(routine.Declaration.Statements, topLevel: false);
            if (routine.IsFunction && !StatementsAlwaysReturn(routine.Declaration.Statements))
                Report("SML3017", routine.Declaration.Identifier.Span, $"FUNCTION '{routine.Name}' does not return a value on every path.");
        }
        _currentRoutine = null;
        return new SemanticModel(_symbols, _routines, _expressionTypes);
    }

    private void InventoryProjectDeclarations()
    {
        var candidates = new List<ProjectDeclarationCandidate>();
        foreach (var tree in _syntaxTrees)
        {
            var sourceOrdinal = _sourceOrdinals[tree.Source];
            foreach (var statement in tree.Root.Statements)
            {
                switch (statement)
                {
                    case ConstStatementSyntax constant:
                        candidates.Add(new ProjectDeclarationCandidate(constant.Identifier, ProjectDeclarationKind.Constant,
                            tree.Source, sourceOrdinal));
                        break;
                    case DimStatementSyntax dim:
                        candidates.Add(new ProjectDeclarationCandidate(dim.Identifier, ProjectDeclarationKind.Array,
                            tree.Source, sourceOrdinal));
                        break;
                    case RoutineDeclarationSyntax routine:
                        candidates.Add(new ProjectDeclarationCandidate(routine.Identifier, ProjectDeclarationKind.Routine,
                            tree.Source, sourceOrdinal));
                        break;
                    default:
                        if (tree.IsStartup)
                            CollectImplicitDeclarationCandidates(statement, tree.Source, sourceOrdinal, candidates);
                        break;
                }
            }
        }

        var declarations = new Dictionary<string, ProjectDeclarationCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates.OrderBy(candidate => candidate.SourceOrdinal)
                     .ThenBy(candidate => candidate.Identifier.Position))
        {
            var name = candidate.Identifier.Text;
            if (!declarations.TryGetValue(name, out var existing))
            {
                declarations[name] = candidate;
                _acceptedProjectDeclarations.Add(candidate.Identifier);
                continue;
            }

            if (candidate.Kind == ProjectDeclarationKind.ImplicitGlobal &&
                existing.Kind == ProjectDeclarationKind.ImplicitGlobal)
                continue;
            if (candidate.Kind == ProjectDeclarationKind.ImplicitGlobal &&
                existing.Kind is ProjectDeclarationKind.Constant or ProjectDeclarationKind.Array)
                continue;

            _rejectedProjectDeclarations.Add(candidate.Identifier);
            var code = candidate.Kind == ProjectDeclarationKind.Routine && existing.Kind == ProjectDeclarationKind.Routine
                ? "SML3015" : "SML3005";
            var message = code == "SML3015"
                ? $"Routine '{name}' is already declared."
                : $"Project-level name '{name}' is already declared as {DeclarationKindName(existing.Kind)}.";
            _diagnostics.Report(candidate.Source, code, candidate.Identifier.Span, message);
        }
    }

    private static void CollectImplicitDeclarationCandidates(StatementSyntax statement, SourceText source, int sourceOrdinal,
        List<ProjectDeclarationCandidate> candidates)
    {
        void Add(SyntaxToken identifier) => candidates.Add(new ProjectDeclarationCandidate(
            identifier, ProjectDeclarationKind.ImplicitGlobal, source, sourceOrdinal));

        switch (statement)
        {
            case AssignmentStatementSyntax assignment when !assignment.Target.IsArrayElement:
                Add(assignment.Target.Identifier);
                break;
            case GetKeyStatementSyntax getKey:
                Add(getKey.Identifier);
                break;
            case RandomStatementSyntax random:
                Add(random.Identifier);
                break;
            case LoadStatementSyntax load:
                Add(load.Identifier);
                break;
            case TextFileLoadStatementSyntax textFileLoad:
                Add(textFileLoad.CountIdentifier);
                break;
            case ForStatementSyntax forStatement:
                Add(forStatement.Identifier);
                foreach (var child in forStatement.Statements)
                    CollectImplicitDeclarationCandidates(child, source, sourceOrdinal, candidates);
                break;
            case IfStatementSyntax ifStatement:
                foreach (var clause in ifStatement.Clauses)
                    foreach (var child in clause.Statements)
                        CollectImplicitDeclarationCandidates(child, source, sourceOrdinal, candidates);
                foreach (var child in ifStatement.ElseStatements)
                    CollectImplicitDeclarationCandidates(child, source, sourceOrdinal, candidates);
                break;
            case DoStatementSyntax doStatement:
                foreach (var child in doStatement.Statements)
                    CollectImplicitDeclarationCandidates(child, source, sourceOrdinal, candidates);
                break;
            case SelectStatementSyntax select:
                foreach (var clause in select.Cases)
                    foreach (var child in clause.Statements)
                        CollectImplicitDeclarationCandidates(child, source, sourceOrdinal, candidates);
                break;
        }
    }

    private void CollectRoutineDeclarations(SyntaxTree tree)
    {
        SetCurrentSource(tree.Source);
        foreach (var statement in tree.Root.Statements)
        {
            if (statement is not RoutineDeclarationSyntax declaration)
                continue;
            if (!_acceptedProjectDeclarations.Contains(declaration.Identifier))
                continue;
            var name = declaration.Identifier.Text;
            if (declaration.Parameters.Count > 4)
                Report("SML3016", declaration.Identifier.Span, $"Routine '{name}' accepts at most four parameters.");

            var parameters = new List<VariableSymbol>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in declaration.Parameters)
            {
                if (!names.Add(parameter.Text))
                {
                    Report("SML3005", parameter.Span, $"Parameter '{parameter.Text}' is already declared.");
                    continue;
                }
                parameters.Add(new VariableSymbol(parameter.Text, SmileType.Number, Array.Empty<int>(),
                    _currentSource, _currentSourceOrdinal, parameter.Span, routineName: name));
            }
            var returnType = declaration.IsFunction ? InferRoutineReturnType(declaration.Statements) : SmileType.Error;
            _routines[name] = new RoutineSymbol(declaration, parameters, returnType, _currentSource, _currentSourceOrdinal);
        }
    }

    private void CollectGlobalDeclarations()
    {
        foreach (var tree in _syntaxTrees)
        {
            foreach (var constant in tree.Root.Statements.OfType<ConstStatementSyntax>())
            {
                if (_acceptedProjectDeclarations.Contains(constant.Identifier))
                    _constantDeclarations[constant.Identifier.Text] = new ConstantDeclaration(
                        constant, tree.Source, _sourceOrdinals[tree.Source]);
            }
        }

        foreach (var constant in _constantDeclarations.Values
                     .OrderBy(constant => constant.SourceOrdinal)
                     .ThenBy(constant => constant.Statement.Identifier.Position))
            ResolveConstant(constant.Statement.Identifier.Text);

        foreach (var tree in _syntaxTrees)
        {
            SetCurrentSource(tree.Source);
            foreach (var statement in tree.Root.Statements)
            {
                switch (statement)
                {
                    case DimStatementSyntax dim when _acceptedProjectDeclarations.Contains(dim.Identifier):
                        DeclareTopLevelArray(dim);
                        break;
                    default:
                        if (tree.IsStartup)
                            CollectImplicitGlobals(statement);
                        break;
                }
            }
        }
    }

    private bool ResolveConstant(string name)
    {
        if (_constantStates.TryGetValue(name, out var state))
        {
            if (state == ConstantResolutionState.Resolved)
                return true;
            if (state == ConstantResolutionState.Failed)
                return false;

            var cycleStart = _constantResolutionStack.FindIndex(item =>
                string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
            var cycle = _constantResolutionStack.Skip(Math.Max(0, cycleStart)).Concat(new[] { name }).ToArray();
            foreach (var cycleName in cycle)
                _constantStates[cycleName] = ConstantResolutionState.Failed;
            var declaration = _constantDeclarations[name];
            _diagnostics.Report(declaration.Source, "SML3029", declaration.Statement.Identifier.Span,
                $"Circular constant dependency detected: {string.Join(" -> ", cycle)}.");
            return false;
        }

        if (!_constantDeclarations.TryGetValue(name, out var constant))
            return false;
        _constantStates[name] = ConstantResolutionState.Resolving;
        _constantResolutionStack.Add(name);
        var resolved = TryEvaluateConstant(constant.Statement.Expression, out var value, out var type);
        _constantResolutionStack.RemoveAt(_constantResolutionStack.Count - 1);

        if (_constantStates.TryGetValue(name, out state) && state == ConstantResolutionState.Failed)
            return false;
        if (!resolved)
        {
            _constantStates[name] = ConstantResolutionState.Failed;
            _diagnostics.Report(constant.Source, "SML3013", constant.Statement.Expression.Span,
                "CONST initializer must be a compile-time scalar expression.");
            return false;
        }

        _symbols[name] = new VariableSymbol(constant.Statement.Identifier.Text, type, Array.Empty<int>(),
            constant.Source, constant.SourceOrdinal, constant.Statement.Identifier.Span,
            isConstant: true, constantValue: value);
        _expressionTypes[constant.Statement.Expression] = type;
        _constantStates[name] = ConstantResolutionState.Resolved;
        return true;
    }

    private void DeclareTopLevelArray(DimStatementSyntax dim)
    {
        if (_symbols.ContainsKey(dim.Identifier.Text))
        {
            Report("SML3005", dim.Identifier.Span, $"'{dim.Identifier.Text}' is already declared in the compilation.");
            return;
        }
        if (!TryGetArrayDimensions(dim, out var dimensions))
            return;
        _symbols[dim.Identifier.Text] = new VariableSymbol(dim.Identifier.Text, SmileType.Number, dimensions,
            _currentSource, _currentSourceOrdinal, dim.Identifier.Span);
    }

    private void CollectImplicitGlobals(StatementSyntax statement)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignment when !assignment.Target.IsArrayElement:
                DeclareImplicitGlobal(assignment.Target.Identifier, InferImplicitGlobalType(assignment.Expression));
                break;
            case GetKeyStatementSyntax getKey:
                DeclareImplicitGlobal(getKey.Identifier, SmileType.Number);
                break;
            case RandomStatementSyntax random:
                DeclareImplicitGlobal(random.Identifier, SmileType.Number);
                break;
            case LoadStatementSyntax load:
                DeclareImplicitGlobal(load.Identifier, SmileType.Number);
                break;
            case TextFileLoadStatementSyntax textFileLoad:
                DeclareImplicitGlobal(textFileLoad.CountIdentifier, SmileType.Number);
                break;
            case ForStatementSyntax forStatement:
                DeclareImplicitGlobal(forStatement.Identifier, SmileType.Number);
                foreach (var child in forStatement.Statements)
                    CollectImplicitGlobals(child);
                break;
            case IfStatementSyntax ifStatement:
                foreach (var clause in ifStatement.Clauses)
                    foreach (var child in clause.Statements)
                        CollectImplicitGlobals(child);
                foreach (var child in ifStatement.ElseStatements)
                    CollectImplicitGlobals(child);
                break;
            case DoStatementSyntax doStatement:
                foreach (var child in doStatement.Statements)
                    CollectImplicitGlobals(child);
                break;
            case SelectStatementSyntax select:
                foreach (var clause in select.Cases)
                    foreach (var child in clause.Statements)
                        CollectImplicitGlobals(child);
                break;
        }
    }

    private void DeclareImplicitGlobal(SyntaxToken identifier, SmileType type)
    {
        if (!_acceptedProjectDeclarations.Contains(identifier))
            return;
        if (_symbols.ContainsKey(identifier.Text) || type is SmileType.Text or SmileType.Error)
            return;
        _symbols[identifier.Text] = new VariableSymbol(identifier.Text, type, Array.Empty<int>(),
            _currentSource, _currentSourceOrdinal, identifier.Span);
        _implicitGlobals.Add(identifier.Text);
    }

    private SmileType InferImplicitGlobalType(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                return literal.Value is bool ? SmileType.Boolean : literal.Value is string ? SmileType.Text : SmileType.Number;
            case NameExpressionSyntax name when _symbols.TryGetValue(name.Identifier.Text, out var symbol):
                return symbol.Type;
            case ArrayAccessExpressionSyntax:
                return SmileType.Number;
            case ParenthesizedExpressionSyntax parenthesized:
                return InferImplicitGlobalType(parenthesized.Expression);
            case UnaryExpressionSyntax unary:
                return unary.OperatorToken.Kind == SyntaxKind.NotKeyword ? SmileType.Boolean : SmileType.Number;
            case BinaryExpressionSyntax binary when binary.OperatorToken.Kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or
                SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken or
                SyntaxKind.AndKeyword or SyntaxKind.OrKeyword:
                return SmileType.Boolean;
            case CallExpressionSyntax call when SyntaxFacts.IsBuiltInFunction(call.Identifier.Kind):
                return call.Identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword
                    ? SmileType.Boolean
                    : SmileType.Number;
            case CallExpressionSyntax call when _routines.TryGetValue(call.Identifier.Text, out var routine):
                return routine.ReturnType;
            default:
                return SmileType.Number;
        }
    }

    private void AnalyzeSupportTopLevel(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is RoutineDeclarationSyntax)
                continue;
            if (statement is ConstStatementSyntax or DimStatementSyntax)
            {
                AnalyzeStatement(statement, topLevel: true);
                continue;
            }

            var message = statement switch
            {
                GameWindowStatementSyntax => "GAME WINDOW is allowed only in the selected startup source.",
                EndProgramStatementSyntax => "END PROGRAM is allowed only in the selected startup source.",
                _ => "Executable top-level statements are not allowed in a support source; move the statement to the selected startup source or into a routine."
            };
            Report("SML3028", statement.Span, message);
        }
    }

    private void SetCurrentSource(SourceText source)
    {
        _currentSource = source;
        _currentSourceOrdinal = _sourceOrdinals[source];
    }

    private void Report(string code, TextSpan span, string message) =>
        _diagnostics.Report(_currentSource, code, span, message);

    private static SmileType InferRoutineReturnType(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is ReturnStatementSyntax { Expression: not null } valueReturn)
                return InferExpressionType(valueReturn.Expression);
            if (statement is IfStatementSyntax ifStatement)
            {
                foreach (var clause in ifStatement.Clauses)
                {
                    var type = InferRoutineReturnType(clause.Statements);
                    if (type != SmileType.Error)
                        return type;
                }
                var elseType = InferRoutineReturnType(ifStatement.ElseStatements);
                if (elseType != SmileType.Error)
                    return elseType;
            }
            if (statement is SelectStatementSyntax select)
            {
                foreach (var clause in select.Cases)
                {
                    var type = InferRoutineReturnType(clause.Statements);
                    if (type != SmileType.Error)
                        return type;
                }
            }
        }
        return SmileType.Number;
    }

    private static SmileType InferExpressionType(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal)
            return literal.Value is bool ? SmileType.Boolean : literal.Value is string ? SmileType.Text : SmileType.Number;
        if (expression is UnaryExpressionSyntax unary && unary.OperatorToken.Kind == SyntaxKind.NotKeyword)
            return SmileType.Boolean;
        if (expression is BinaryExpressionSyntax binary && binary.OperatorToken.Kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or
            SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken or
            SyntaxKind.AndKeyword or SyntaxKind.OrKeyword)
            return SmileType.Boolean;
        if (expression is CallExpressionSyntax call && call.Identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword)
            return SmileType.Boolean;
        return SmileType.Number;
    }

    private void CollectFirstDeclarations(IReadOnlyList<StatementSyntax> statements, Dictionary<string, int> declarations, bool skipRoutines)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case RoutineDeclarationSyntax when skipRoutines:
                    break;
                case AssignmentStatementSyntax assignment when !assignment.Target.IsArrayElement:
                    RecordFirst(declarations, assignment.Target.Identifier);
                    break;
                case DimStatementSyntax dim:
                    RecordFirst(declarations, dim.Identifier);
                    break;
                case ConstStatementSyntax constant:
                    RecordFirst(declarations, constant.Identifier);
                    break;
                case GetKeyStatementSyntax getKey:
                    RecordFirst(declarations, getKey.Identifier);
                    break;
                case RandomStatementSyntax random:
                    RecordFirst(declarations, random.Identifier);
                    break;
                case LoadStatementSyntax load:
                    RecordFirst(declarations, load.Identifier);
                    break;
                case TextFileLoadStatementSyntax textFileLoad:
                    RecordFirst(declarations, textFileLoad.CountIdentifier);
                    break;
                case ForStatementSyntax forStatement:
                    RecordFirst(declarations, forStatement.Identifier);
                    CollectFirstDeclarations(forStatement.Statements, declarations, skipRoutines);
                    break;
                case IfStatementSyntax ifStatement:
                    foreach (var clause in ifStatement.Clauses)
                        CollectFirstDeclarations(clause.Statements, declarations, skipRoutines);
                    CollectFirstDeclarations(ifStatement.ElseStatements, declarations, skipRoutines);
                    break;
                case DoStatementSyntax doStatement:
                    CollectFirstDeclarations(doStatement.Statements, declarations, skipRoutines);
                    break;
                case SelectStatementSyntax select:
                    foreach (var clause in select.Cases)
                        CollectFirstDeclarations(clause.Statements, declarations, skipRoutines);
                    break;
            }
        }
    }

    private static void RecordFirst(Dictionary<string, int> declarations, SyntaxToken identifier)
    {
        if (!declarations.TryGetValue(identifier.Text, out var position) || identifier.Position < position)
            declarations[identifier.Text] = identifier.Position;
    }

    private void AnalyzeStatements(IReadOnlyList<StatementSyntax> statements, bool topLevel)
    {
        foreach (var statement in statements)
        {
            if (statement is RoutineDeclarationSyntax routine)
            {
                if (!topLevel)
                    Report("SML3020", routine.Keyword.Span, "Routines cannot be nested.");
                continue;
            }
            AnalyzeStatement(statement, topLevel);
        }
    }

    private void AnalyzeStatement(StatementSyntax statement, bool topLevel)
    {
        switch (statement)
        {
            case ConstStatementSyntax constant: AnalyzeConstant(constant, topLevel); break;
            case AssignmentStatementSyntax assignment: AnalyzeAssignment(assignment); break;
            case DimStatementSyntax dim: AnalyzeDim(dim, topLevel); break;
            case PrintStatementSyntax print: AnalyzePrint(print); break;
            case GetKeyStatementSyntax getKey: EnsureNumberTarget(getKey.Identifier, "GET KEY"); break;
            case ClearScreenStatementSyntax: break;
            case WaitStatementSyntax wait: RequireType(wait.Duration, SmileType.Number, "SML3008", "WAIT duration must be NUMBER."); break;
            case RandomStatementSyntax random:
                RequireType(random.Minimum, SmileType.Number, "SML3008", "RANDOM minimum must be NUMBER.");
                RequireType(random.Maximum, SmileType.Number, "SML3008", "RANDOM maximum must be NUMBER.");
                EnsureNumberTarget(random.Identifier, "RANDOM");
                break;
            case IfStatementSyntax ifStatement:
                foreach (var clause in ifStatement.Clauses)
                {
                    RequireType(clause.Condition, SmileType.Boolean, "SML3004", "IF condition must be BOOLEAN.");
                    AnalyzeStatements(clause.Statements, false);
                }
                AnalyzeStatements(ifStatement.ElseStatements, false);
                break;
            case ForStatementSyntax forStatement:
                RequireType(forStatement.LowerBound, SmileType.Number, "SML3008", "FOR lower bound must be NUMBER.");
                RequireType(forStatement.UpperBound, SmileType.Number, "SML3008", "FOR upper bound must be NUMBER.");
                EnsureNumberTarget(forStatement.Identifier, "FOR");
                _forDepth++;
                AnalyzeStatements(forStatement.Statements, false);
                _forDepth--;
                break;
            case DoStatementSyntax doStatement:
                _doDepth++;
                AnalyzeStatements(doStatement.Statements, false);
                _doDepth--;
                if (doStatement.UntilCondition != null)
                    RequireType(doStatement.UntilCondition, SmileType.Boolean, "SML3004", "LOOP UNTIL condition must be BOOLEAN.");
                break;
            case CallStatementSyntax call: AnalyzeCall(call.Identifier, call.Arguments, requireFunction: false); break;
            case ReturnStatementSyntax returnStatement: AnalyzeReturn(returnStatement); break;
            case SelectStatementSyntax select: AnalyzeSelect(select); break;
            case ExitStatementSyntax exit: AnalyzeExit(exit); break;
            case EndProgramStatementSyntax: break;
            case GameWindowStatementSyntax gameWindow: AnalyzeGameWindow(gameWindow, topLevel); break;
            case ClearColorStatementSyntax clearColor:
                RequireGameWindow(clearColor.Span, "CLEAR color");
                RequireType(clearColor.Color, SmileType.Number, "SML3023", "CLEAR color must be NUMBER.");
                break;
            case GraphicsStatementSyntax graphics:
                RequireGameWindow(graphics.Span, "drawing statement");
                foreach (var argument in graphics.Arguments)
                    RequireType(argument, SmileType.Number, "SML3023", "Drawing arguments must be NUMBER values.");
                break;
            case ShowScreenStatementSyntax show:
                RequireGameWindow(show.Span, "SHOW SCREEN");
                break;
            case SoundStatementSyntax sound:
                RequireGameWindow(sound.Span, sound.IsStop ? "STOP SOUND" : "PLAY SOUND");
                if (!sound.IsStop && string.IsNullOrWhiteSpace(sound.Path?.Value as string))
                    Report("SML3024", sound.Span, "PLAY SOUND requires a non-empty WAV path literal.");
                break;
            case MusicStatementSyntax music:
                RequireGameWindow(music.Span, music.Operation switch
                {
                    MusicOperation.Play => "PLAY MUSIC",
                    MusicOperation.Pause => "PAUSE MUSIC",
                    MusicOperation.Resume => "RESUME MUSIC",
                    MusicOperation.Stop => "STOP MUSIC",
                    _ => "MUSIC VOLUME"
                });
                if (music.Operation == MusicOperation.Play && string.IsNullOrWhiteSpace(music.Path?.Value as string))
                    Report("SML3026", music.Span, "PLAY MUSIC requires a non-empty music path literal.");
                if (music.Operation == MusicOperation.SetVolume && music.Volume != null)
                    RequireType(music.Volume, SmileType.Number, "SML3026", "MUSIC VOLUME requires a NUMBER value.");
                break;
            case LoadStatementSyntax load:
                RequireType(load.DefaultValue, SmileType.Number, "SML3025", "LOAD DEFAULT must be NUMBER.");
                EnsureNumberTarget(load.Identifier, "LOAD");
                ValidateStorageKey(load.Key);
                break;
            case TextFileLoadStatementSyntax textFileLoad:
                AnalyzeTextFileLoad(textFileLoad);
                break;
            case SaveStatementSyntax save:
                if (!TryResolve(save.Identifier.Text, save.Identifier, out var saved) || saved.IsArray || saved.Type != SmileType.Number)
                    Report("SML3025", save.Identifier.Span, "SAVE value must be a NUMBER variable or constant.");
                ValidateStorageKey(save.Key);
                break;
        }
    }

    private void AnalyzeGameWindow(GameWindowStatementSyntax statement, bool topLevel)
    {
        _gameWindowCount++;
        if (!topLevel || _currentRoutine != null)
            Report("SML3022", statement.GameKeyword.Span, "GAME WINDOW must be a top-level statement.");
        if (_gameWindowCount > 1)
            Report("SML3022", statement.GameKeyword.Span, "Only one GAME WINDOW is allowed.");
        if (statement.Width == null || statement.Height == null)
            return;
        if (!TryEvaluateConstant(statement.Width, out var width, out var widthType) || widthType != SmileType.Number || width <= 0)
            Report("SML3023", statement.Width.Span, "GAME WINDOW width must be a positive compile-time NUMBER.");
        if (!TryEvaluateConstant(statement.Height, out var height, out var heightType) || heightType != SmileType.Number || height <= 0)
            Report("SML3023", statement.Height.Span, "GAME WINDOW height must be a positive compile-time NUMBER.");
    }

    private void RequireGameWindow(TextSpan span, string statementName)
    {
        if (!_hasGameWindow)
            Report("SML3023", span, $"{statementName} requires a GAME WINDOW statement.");
    }

    private void ValidateStorageKey(SyntaxToken key)
    {
        if (string.IsNullOrWhiteSpace(key.Value as string))
            Report("SML3025", key.Span, "Storage key must be a non-empty text literal.");
    }

    private void AnalyzeTextFileLoad(TextFileLoadStatementSyntax statement)
    {
        if (string.IsNullOrWhiteSpace(statement.Path.Value as string))
            Report("SML3027", statement.Path.Span, "LOAD TEXT FILE requires a non-empty path literal.");

        if (!TryResolveExisting(statement.Destination.Text, out var destination))
        {
            Report("SML3027", statement.Destination.Span,
                $"LOAD TEXT FILE destination '{statement.Destination.Text}' must be a declared one-dimensional NUMBER array.");
        }
        else if (!destination.IsArray || destination.ArrayRank != 1 || destination.Type != SmileType.Number)
        {
            Report("SML3027", statement.Destination.Span,
                $"LOAD TEXT FILE destination '{statement.Destination.Text}' must be a one-dimensional NUMBER array.");
        }

        EnsureNumberTarget(statement.CountIdentifier, "LOAD TEXT FILE COUNT");
    }

    private void AnalyzeConstant(ConstStatementSyntax constant, bool topLevel)
    {
        if (!topLevel || _currentRoutine != null)
        {
            Report("SML3013", constant.ConstKeyword.Span, "CONST declarations must be top-level.");
            return;
        }
        // Compilation-wide top-level constants were registered before any body is bound.
    }

    private void AnalyzeAssignment(AssignmentStatementSyntax assignment)
    {
        var valueType = AnalyzeExpression(assignment.Expression);
        var name = assignment.Target.Identifier.Text;
        if (assignment.Target.IsArrayElement)
        {
            foreach (var index in assignment.Target.Indices)
                RequireType(index, SmileType.Number, "SML3007", "Array index must be NUMBER.");
            if (!TryResolve(name, assignment.Target.Identifier, out var array))
                return;
            if (!array.IsArray)
            {
                Report("SML3009", assignment.Target.Identifier.Span, $"'{name}' is not an array.");
                return;
            }
            if (assignment.Target.Indices.Count != array.ArrayRank)
                Report("SML3014", assignment.Target.Span, $"Array '{name}' requires {array.ArrayRank} index value(s).");
            if (valueType != SmileType.Error && valueType != SmileType.Number)
                Report("SML3003", assignment.Expression.Span, "Array elements require NUMBER values.");
            return;
        }

        if (_currentRoutine == null && _rejectedProjectDeclarations.Contains(assignment.Target.Identifier))
            return;

        if (TryResolveExisting(name, out var existing))
        {
            if (existing.IsConstant)
            {
                Report("SML3012", assignment.Target.Identifier.Span, $"Constant '{name}' cannot be assigned.");
                return;
            }
            if (existing.IsArray)
            {
                Report("SML3009", assignment.Target.Identifier.Span, $"Array '{name}' requires an index.");
                return;
            }
            if (valueType != SmileType.Error && existing.Type != valueType)
                Report("SML3003", assignment.Expression.Span, $"Cannot assign {TypeName(valueType)} to {TypeName(existing.Type)} variable '{name}'.");
            return;
        }

        if (valueType == SmileType.Text)
        {
            Report("SML3010", assignment.Expression.Span, "General-purpose TEXT variables are not supported.");
            return;
        }
        if (valueType == SmileType.Error)
            return;
        DeclareVariable(name, valueType, Array.Empty<int>(), assignment.Target.Identifier.Span);
    }

    private void AnalyzeDim(DimStatementSyntax dim, bool topLevel)
    {
        if (topLevel && _currentRoutine == null)
            return;
        if (TryResolveExisting(dim.Identifier.Text, out _))
        {
            Report("SML3005", dim.Identifier.Span, $"'{dim.Identifier.Text}' is already declared.");
            return;
        }
        if (TryGetArrayDimensions(dim, out var dimensions))
            DeclareVariable(dim.Identifier.Text, SmileType.Number, dimensions, dim.Identifier.Span);
    }

    private bool TryGetArrayDimensions(DimStatementSyntax dim, out IReadOnlyList<int> dimensions)
    {
        var result = new List<int>();
        var valid = true;
        if (dim.Sizes.Count is < 1 or > 2)
        {
            Report("SML3014", dim.Span, "Arrays require one or two dimensions.");
            dimensions = result;
            return false;
        }
        long total = 1;
        foreach (var sizeExpression in dim.Sizes)
        {
            if (!TryEvaluateConstant(sizeExpression, out var value, out var type) || type != SmileType.Number || value <= 0 || value > int.MaxValue)
            {
                Report("SML3006", sizeExpression.Span, "Array dimension must be a positive compile-time NUMBER expression.");
                value = 1;
                valid = false;
            }
            total *= value;
            if (total > int.MaxValue)
            {
                Report("SML3006", dim.Span, "Total array storage exceeds the supported size.");
                valid = false;
            }
            result.Add((int)Math.Min(value, int.MaxValue));
        }
        dimensions = result;
        return valid;
    }

    private void AnalyzePrint(PrintStatementSyntax print)
    {
        foreach (var item in print.Items)
        {
            var type = AnalyzeExpression(item);
            if (type is not (SmileType.Error or SmileType.Text or SmileType.Number or SmileType.Boolean))
                Report("SML3011", item.Span, "Invalid PRINT item.");
        }
    }

    private void AnalyzeReturn(ReturnStatementSyntax statement)
    {
        if (_currentRoutine == null)
        {
            Report("SML3020", statement.ReturnKeyword.Span, "RETURN is only valid inside a SUB or FUNCTION.");
            return;
        }
        if (_currentRoutine.IsFunction)
        {
            if (statement.Expression == null)
            {
                Report("SML3020", statement.ReturnKeyword.Span, "FUNCTION RETURN requires a value.");
                return;
            }
            var type = AnalyzeExpression(statement.Expression);
            if (type != SmileType.Error && type != _currentRoutine.ReturnType)
                Report("SML3003", statement.Expression.Span, $"FUNCTION '{_currentRoutine.Name}' must return {TypeName(_currentRoutine.ReturnType)}.");
        }
        else if (statement.Expression != null)
        {
            AnalyzeExpression(statement.Expression);
            Report("SML3020", statement.Expression.Span, "SUB RETURN cannot include a value.");
        }
    }

    private void AnalyzeSelect(SelectStatementSyntax select)
    {
        var selectorType = AnalyzeExpression(select.Expression);
        if (selectorType is not (SmileType.Number or SmileType.Boolean or SmileType.Error))
            Report("SML3003", select.Expression.Span, "SELECT CASE expression must be NUMBER or BOOLEAN.");
        var values = new HashSet<long>();
        var sawElse = false;
        foreach (var clause in select.Cases)
        {
            if (clause.IsElse)
            {
                if (sawElse)
                    Report("SML3019", clause.CaseKeyword.Span, "SELECT CASE contains more than one CASE ELSE.");
                sawElse = true;
            }
            else if (clause.Value != null)
            {
                var caseType = AnalyzeExpression(clause.Value);
                if (caseType != SmileType.Error && selectorType != SmileType.Error && caseType != selectorType)
                    Report("SML3003", clause.Value.Span, "CASE value type must match SELECT CASE.");
                if (!TryEvaluateConstant(clause.Value, out var value, out _))
                    Report("SML3013", clause.Value.Span, "CASE value must be a compile-time scalar expression.");
                else if (!values.Add(value))
                    Report("SML3019", clause.Value.Span, $"Duplicate CASE value '{value}'.");
            }
            AnalyzeStatements(clause.Statements, false);
        }
    }

    private void AnalyzeExit(ExitStatementSyntax exit)
    {
        var valid = exit.TargetKeyword.Kind == SyntaxKind.ForKeyword ? _forDepth > 0 : _doDepth > 0;
        if (!valid)
            Report("SML3018", exit.Span, $"EXIT {SyntaxFacts.GetText(exit.TargetKeyword.Kind)} is not inside a matching loop.");
    }

    private void EnsureNumberTarget(SyntaxToken identifier, string statementName)
    {
        if (_currentRoutine == null && _rejectedProjectDeclarations.Contains(identifier))
            return;
        if (!TryResolveExisting(identifier.Text, out var symbol))
        {
            DeclareVariable(identifier.Text, SmileType.Number, Array.Empty<int>(), identifier.Span);
            return;
        }
        if (symbol.IsConstant || symbol.IsArray || symbol.Type != SmileType.Number)
            Report("SML3008", identifier.Span, $"{statementName} target '{identifier.Text}' must be a writable NUMBER variable.");
    }

    private void DeclareVariable(string name, SmileType type, IReadOnlyList<int> dimensions, TextSpan span)
    {
        if (_currentRoutine == null)
        {
            if (!_symbols.ContainsKey(name))
                _symbols[name] = new VariableSymbol(name, type, dimensions, _currentSource, _currentSourceOrdinal, span);
        }
        else if (_symbols.ContainsKey(name))
        {
            return;
        }
        else
            _currentRoutine.Locals[name] = new VariableSymbol(name, type, dimensions,
                _currentSource, _currentSourceOrdinal, span, routineName: _currentRoutine.Name);
    }

    private SmileType RequireType(ExpressionSyntax expression, SmileType requiredType, string code, string message)
    {
        var type = AnalyzeExpression(expression);
        if (type != SmileType.Error && type != requiredType)
            Report(code, expression.Span, message);
        return type;
    }

    private SmileType AnalyzeExpression(ExpressionSyntax expression)
    {
        if (_expressionTypes.TryGetValue(expression, out var cached))
            return cached;
        SmileType result;
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                result = literal.Value is bool ? SmileType.Boolean : literal.Value is string ? SmileType.Text : SmileType.Number;
                break;
            case NameExpressionSyntax name:
                if (!TryResolve(name.Identifier.Text, name.Identifier, out var symbol))
                    result = SmileType.Error;
                else if (symbol.IsArray)
                {
                    Report("SML3009", name.Span, $"Array '{name.Identifier.Text}' requires an index.");
                    result = SmileType.Error;
                }
                else
                    result = symbol.Type;
                break;
            case ArrayAccessExpressionSyntax array:
                foreach (var index in array.Indices)
                    RequireType(index, SmileType.Number, "SML3007", "Array index must be NUMBER.");
                if (!TryResolve(array.Identifier.Text, array.Identifier, out var arraySymbol))
                    result = SmileType.Error;
                else if (!arraySymbol.IsArray)
                {
                    Report("SML3009", array.Identifier.Span, $"'{array.Identifier.Text}' is not an array.");
                    result = SmileType.Error;
                }
                else
                {
                    if (array.Indices.Count != arraySymbol.ArrayRank)
                        Report("SML3014", array.Span, $"Array '{array.Identifier.Text}' requires {arraySymbol.ArrayRank} index value(s).");
                    result = SmileType.Number;
                }
                break;
            case ParenthesizedExpressionSyntax parenthesized:
                result = AnalyzeExpression(parenthesized.Expression);
                break;
            case UnaryExpressionSyntax unary:
                result = AnalyzeUnary(unary);
                break;
            case BinaryExpressionSyntax binary:
                result = AnalyzeBinary(binary);
                break;
            case CallExpressionSyntax call:
                result = AnalyzeCall(call.Identifier, call.Arguments, requireFunction: true);
                break;
            default:
                result = SmileType.Error;
                break;
        }
        _expressionTypes[expression] = result;
        return result;
    }

    private SmileType AnalyzeCall(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> arguments, bool requireFunction)
    {
        foreach (var argument in arguments)
        {
            var type = AnalyzeExpression(argument);
            if (type == SmileType.Text)
                Report("SML3003", argument.Span, "Routine arguments must be scalar NUMBER or BOOLEAN values.");
        }

        if (SyntaxFacts.IsBuiltInFunction(identifier.Kind))
            return AnalyzeBuiltInCall(identifier, arguments);

        if (!_routines.TryGetValue(identifier.Text, out var routine))
        {
            Report("SML3021", identifier.Span, $"Unknown routine or built-in function '{identifier.Text}'.");
            return SmileType.Error;
        }
        if (routine.Parameters.Count != arguments.Count)
            Report("SML3016", identifier.Span, $"Routine '{routine.Name}' expects {routine.Parameters.Count} argument(s), found {arguments.Count}.");
        if (requireFunction && !routine.IsFunction)
        {
            Report("SML3020", identifier.Span, $"SUB '{routine.Name}' cannot be used as an expression.");
            return SmileType.Error;
        }
        if (!requireFunction && routine.IsFunction)
            Report("SML3020", identifier.Span, $"FUNCTION '{routine.Name}' must be used in an expression.");
        return routine.IsFunction ? routine.ReturnType : SmileType.Error;
    }

    private SmileType AnalyzeBuiltInCall(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> arguments)
    {
        if (!SyntaxFacts.IsBuiltInFunction(identifier.Kind))
        {
            Report("SML3021", identifier.Span, $"Unknown built-in function '{identifier.Text}'.");
            return SmileType.Error;
        }
        var expected = SyntaxFacts.GetBuiltInFunctionParameters(identifier.Kind).Count;
        if (identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword && !_hasGameWindow)
            Report("SML3023", identifier.Span, $"Built-in '{identifier.Text}' requires GAME WINDOW.");
        if (arguments.Count != expected)
            Report("SML3016", identifier.Span, $"Built-in '{identifier.Text}' expects {expected} argument(s), found {arguments.Count}.");
        foreach (var argument in arguments)
            RequireType(argument, SmileType.Number, "SML3003", $"Built-in '{identifier.Text}' requires NUMBER arguments.");
        return identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword ? SmileType.Boolean : SmileType.Number;
    }

    private SmileType AnalyzeUnary(UnaryExpressionSyntax unary)
    {
        var operandType = AnalyzeExpression(unary.Operand);
        var required = unary.OperatorToken.Kind == SyntaxKind.NotKeyword ? SmileType.Boolean : SmileType.Number;
        if (operandType != SmileType.Error && operandType != required)
        {
            Report("SML3003", unary.Span, $"Operator '{unary.OperatorToken.Text}' requires {TypeName(required)}.");
            return SmileType.Error;
        }
        return operandType == SmileType.Error ? SmileType.Error : required;
    }

    private SmileType AnalyzeBinary(BinaryExpressionSyntax binary)
    {
        var leftType = AnalyzeExpression(binary.Left);
        var rightType = AnalyzeExpression(binary.Right);
        if (leftType == SmileType.Error || rightType == SmileType.Error)
            return SmileType.Error;
        switch (binary.OperatorToken.Kind)
        {
            case SyntaxKind.PlusToken:
            case SyntaxKind.MinusToken:
            case SyntaxKind.StarToken:
            case SyntaxKind.SlashToken:
            case SyntaxKind.ModKeyword:
                return RequireOperands(binary, leftType, rightType, SmileType.Number, SmileType.Number);
            case SyntaxKind.LessToken:
            case SyntaxKind.GreaterToken:
            case SyntaxKind.LessOrEqualsToken:
            case SyntaxKind.GreaterOrEqualsToken:
                return RequireOperands(binary, leftType, rightType, SmileType.Number, SmileType.Boolean);
            case SyntaxKind.EqualsToken:
            case SyntaxKind.NotEqualsToken:
                if (leftType != rightType || leftType == SmileType.Text)
                {
                    Report("SML3003", binary.Span, "Equality operands must have the same NUMBER or BOOLEAN type.");
                    return SmileType.Error;
                }
                return SmileType.Boolean;
            case SyntaxKind.AndKeyword:
            case SyntaxKind.OrKeyword:
                return RequireOperands(binary, leftType, rightType, SmileType.Boolean, SmileType.Boolean);
            default:
                return SmileType.Error;
        }
    }

    private SmileType RequireOperands(BinaryExpressionSyntax binary, SmileType leftType, SmileType rightType,
        SmileType required, SmileType result)
    {
        if (leftType != required || rightType != required)
        {
            Report("SML3003", binary.Span, $"Operator '{binary.OperatorToken.Text}' requires {TypeName(required)} operands.");
            return SmileType.Error;
        }
        return result;
    }

    private bool TryResolveExisting(string name, out VariableSymbol symbol)
    {
        if (_currentRoutine != null && _currentRoutine.Locals.TryGetValue(name, out symbol!))
            return true;
        return _symbols.TryGetValue(name, out symbol!);
    }

    private bool TryResolve(string name, SyntaxToken token, out VariableSymbol symbol)
    {
        if (TryResolveExisting(name, out symbol))
        {
            if (_currentRoutine == null && ReferenceEquals(_currentSource, _startupTree.Source) &&
                _implicitGlobals.Contains(name) && _globalFirstDeclarations.TryGetValue(name, out var firstPosition) &&
                token.Position < firstPosition)
            {
                Report("SML3002", token.Span, $"Variable '{name}' is used before its first assignment.");
                symbol = null!;
                return false;
            }
            return true;
        }
        var declarations = _currentRoutine?.FirstDeclarations ?? _globalFirstDeclarations;
        if (declarations.TryGetValue(name, out var position) && position > token.Position)
            Report("SML3002", token.Span, $"Variable '{name}' is used before its first assignment.");
        else
            Report("SML3001", token.Span, $"Unknown identifier '{name}'.");
        symbol = null!;
        return false;
    }

    private bool TryEvaluateConstant(ExpressionSyntax expression, out long value, out SmileType type)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.Value is long number:
                value = number; type = SmileType.Number; return true;
            case LiteralExpressionSyntax literal when literal.Value is bool boolean:
                value = boolean ? 1 : 0; type = SmileType.Boolean; return true;
            case NameExpressionSyntax name:
                if (!_symbols.TryGetValue(name.Identifier.Text, out var symbol) || !symbol.IsConstant)
                {
                    if (!ResolveConstant(name.Identifier.Text) ||
                        !_symbols.TryGetValue(name.Identifier.Text, out symbol) || !symbol.IsConstant)
                        break;
                }
                value = symbol.ConstantValue; type = symbol.Type; return true;
            case ParenthesizedExpressionSyntax parenthesized:
                return TryEvaluateConstant(parenthesized.Expression, out value, out type);
            case UnaryExpressionSyntax unary when TryEvaluateConstant(unary.Operand, out var operand, out var operandType):
                if (unary.OperatorToken.Kind == SyntaxKind.MinusToken && operandType == SmileType.Number)
                { value = -operand; type = SmileType.Number; return true; }
                if (unary.OperatorToken.Kind == SyntaxKind.NotKeyword && operandType == SmileType.Boolean)
                { value = operand == 0 ? 1 : 0; type = SmileType.Boolean; return true; }
                break;
            case BinaryExpressionSyntax binary when
                TryEvaluateConstant(binary.Left, out var left, out var leftType) &&
                TryEvaluateConstant(binary.Right, out var right, out var rightType):
                if (TryEvaluateBinary(binary.OperatorToken.Kind, left, right, leftType, rightType, out value, out type))
                    return true;
                break;
            case CallExpressionSyntax call when call.Identifier.Kind == SyntaxKind.AbsKeyword && call.Arguments.Count == 1 &&
                TryEvaluateConstant(call.Arguments[0], out var absValue, out var absType) && absType == SmileType.Number:
                value = absValue == long.MinValue ? long.MaxValue : Math.Abs(absValue); type = SmileType.Number; return true;
            case CallExpressionSyntax call when call.Identifier.Kind is SyntaxKind.MinKeyword or SyntaxKind.MaxKeyword && call.Arguments.Count == 2 &&
                TryEvaluateConstant(call.Arguments[0], out var first, out var firstType) &&
                TryEvaluateConstant(call.Arguments[1], out var second, out var secondType) && firstType == SmileType.Number && secondType == SmileType.Number:
                value = call.Identifier.Kind == SyntaxKind.MinKeyword ? Math.Min(first, second) : Math.Max(first, second);
                type = SmileType.Number; return true;
            case CallExpressionSyntax call when call.Identifier.Kind == SyntaxKind.RgbKeyword && call.Arguments.Count == 3 &&
                TryEvaluateConstant(call.Arguments[0], out var red, out _) &&
                TryEvaluateConstant(call.Arguments[1], out var green, out _) &&
                TryEvaluateConstant(call.Arguments[2], out var blue, out _):
                value = (red & 255) | ((green & 255) << 8) | ((blue & 255) << 16); type = SmileType.Number; return true;
        }
        value = 0;
        type = SmileType.Error;
        return false;
    }

    private static bool TryEvaluateBinary(SyntaxKind kind, long left, long right, SmileType leftType, SmileType rightType,
        out long value, out SmileType type)
    {
        value = 0;
        type = SmileType.Error;
        if (kind is SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.ModKeyword)
        {
            if (leftType != SmileType.Number || rightType != SmileType.Number || (right == 0 && kind is SyntaxKind.SlashToken or SyntaxKind.ModKeyword))
                return false;
            value = kind switch
            {
                SyntaxKind.PlusToken => left + right,
                SyntaxKind.MinusToken => left - right,
                SyntaxKind.StarToken => left * right,
                SyntaxKind.SlashToken => left / right,
                _ => left % right
            };
            type = SmileType.Number;
            return true;
        }
        if (kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken)
        {
            if (leftType != rightType)
                return false;
            value = kind == SyntaxKind.EqualsToken ? (left == right ? 1 : 0) : (left != right ? 1 : 0);
            type = SmileType.Boolean;
            return true;
        }
        if (kind is SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken && leftType == SmileType.Number && rightType == SmileType.Number)
        {
            value = kind switch
            {
                SyntaxKind.LessToken => left < right ? 1 : 0,
                SyntaxKind.GreaterToken => left > right ? 1 : 0,
                SyntaxKind.LessOrEqualsToken => left <= right ? 1 : 0,
                _ => left >= right ? 1 : 0
            };
            type = SmileType.Boolean;
            return true;
        }
        if (kind is SyntaxKind.AndKeyword or SyntaxKind.OrKeyword && leftType == SmileType.Boolean && rightType == SmileType.Boolean)
        {
            value = kind == SyntaxKind.AndKeyword ? ((left != 0 && right != 0) ? 1 : 0) : ((left != 0 || right != 0) ? 1 : 0);
            type = SmileType.Boolean;
            return true;
        }
        return false;
    }

    private static bool StatementsAlwaysReturn(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is ReturnStatementSyntax)
                return true;
            if (statement is IfStatementSyntax ifStatement && ifStatement.ElseStatements.Count != 0)
            {
                var all = StatementsAlwaysReturn(ifStatement.ElseStatements);
                foreach (var clause in ifStatement.Clauses)
                    all &= StatementsAlwaysReturn(clause.Statements);
                if (all)
                    return true;
            }
            if (statement is SelectStatementSyntax select)
            {
                var hasElse = false;
                var all = select.Cases.Count != 0;
                foreach (var clause in select.Cases)
                {
                    hasElse |= clause.IsElse;
                    all &= StatementsAlwaysReturn(clause.Statements);
                }
                if (hasElse && all)
                    return true;
            }
        }
        return false;
    }

    private static string DeclarationKindName(ProjectDeclarationKind kind) => kind switch
    {
        ProjectDeclarationKind.Constant => "CONST",
        ProjectDeclarationKind.Array => "DIM",
        ProjectDeclarationKind.Routine => "routine",
        _ => "implicit startup global"
    };

    private sealed class ProjectDeclarationCandidate
    {
        public ProjectDeclarationCandidate(SyntaxToken identifier, ProjectDeclarationKind kind, SourceText source,
            int sourceOrdinal)
        { Identifier = identifier; Kind = kind; Source = source; SourceOrdinal = sourceOrdinal; }

        public SyntaxToken Identifier { get; }
        public ProjectDeclarationKind Kind { get; }
        public SourceText Source { get; }
        public int SourceOrdinal { get; }
    }

    private sealed class ConstantDeclaration
    {
        public ConstantDeclaration(ConstStatementSyntax statement, SourceText source, int sourceOrdinal)
        { Statement = statement; Source = source; SourceOrdinal = sourceOrdinal; }

        public ConstStatementSyntax Statement { get; }
        public SourceText Source { get; }
        public int SourceOrdinal { get; }
    }

    private enum ProjectDeclarationKind { Constant, Array, Routine, ImplicitGlobal }
    private enum ConstantResolutionState { Resolving, Resolved, Failed }

    private static string TypeName(SmileType type) => type.ToString().ToUpperInvariant();
}
