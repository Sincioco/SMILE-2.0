using System;
using System.Collections.Generic;

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
    internal VariableSymbol(string name, SmileType type, IReadOnlyList<int> dimensions, TextSpan declarationSpan,
        bool isConstant = false, long constantValue = 0, string? routineName = null)
    {
        Name = name;
        Type = type;
        ArrayDimensions = dimensions;
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
    public TextSpan DeclarationSpan { get; }
}

public sealed class RoutineSymbol
{
    internal RoutineSymbol(RoutineDeclarationSyntax declaration, IReadOnlyList<VariableSymbol> parameters, SmileType returnType)
    {
        Declaration = declaration;
        Name = declaration.Identifier.Text;
        IsFunction = declaration.IsFunction;
        Parameters = parameters;
        ReturnType = returnType;
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
    private readonly DiagnosticBag _diagnostics;
    private readonly Dictionary<string, VariableSymbol> _symbols = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RoutineSymbol> _routines = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _globalFirstDeclarations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ExpressionSyntax, SmileType> _expressionTypes = new();
    private RoutineSymbol? _currentRoutine;
    private int _forDepth;
    private int _doDepth;

    public SemanticAnalyzer(SourceText source) => _diagnostics = new DiagnosticBag(source);
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.ToArray();

    public SemanticModel Analyze(CompilationUnitSyntax root)
    {
        CollectRoutineDeclarations(root.Statements);
        CollectFirstDeclarations(root.Statements, _globalFirstDeclarations, skipRoutines: true);
        AnalyzeStatements(root.Statements, topLevel: true);

        foreach (var routine in _routines.Values)
        {
            _currentRoutine = routine;
            CollectFirstDeclarations(routine.Declaration.Statements, routine.FirstDeclarations, skipRoutines: false);
            AnalyzeStatements(routine.Declaration.Statements, topLevel: false);
            if (routine.IsFunction && !StatementsAlwaysReturn(routine.Declaration.Statements))
                _diagnostics.Report("SML3017", routine.Declaration.Identifier.Span, $"FUNCTION '{routine.Name}' does not return a value on every path.");
        }
        _currentRoutine = null;
        return new SemanticModel(_symbols, _routines, _expressionTypes);
    }

    private void CollectRoutineDeclarations(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is not RoutineDeclarationSyntax declaration)
                continue;
            var name = declaration.Identifier.Text;
            if (_routines.ContainsKey(name))
            {
                _diagnostics.Report("SML3015", declaration.Identifier.Span, $"Routine '{name}' is already declared.");
                continue;
            }
            if (declaration.Parameters.Count > 4)
                _diagnostics.Report("SML3016", declaration.Identifier.Span, $"Routine '{name}' accepts at most four parameters.");

            var parameters = new List<VariableSymbol>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in declaration.Parameters)
            {
                if (!names.Add(parameter.Text))
                {
                    _diagnostics.Report("SML3005", parameter.Span, $"Parameter '{parameter.Text}' is already declared.");
                    continue;
                }
                parameters.Add(new VariableSymbol(parameter.Text, SmileType.Number, Array.Empty<int>(), parameter.Span, routineName: name));
            }
            var returnType = declaration.IsFunction ? InferRoutineReturnType(declaration.Statements) : SmileType.Error;
            _routines[name] = new RoutineSymbol(declaration, parameters, returnType);
        }
    }

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
                    _diagnostics.Report("SML3020", routine.Keyword.Span, "Routines cannot be nested.");
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
            case DimStatementSyntax dim: AnalyzeDim(dim); break;
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
        }
    }

    private void AnalyzeConstant(ConstStatementSyntax constant, bool topLevel)
    {
        if (!topLevel || _currentRoutine != null)
        {
            _diagnostics.Report("SML3013", constant.ConstKeyword.Span, "CONST declarations must be top-level.");
            return;
        }
        if (_symbols.ContainsKey(constant.Identifier.Text))
        {
            _diagnostics.Report("SML3005", constant.Identifier.Span, $"'{constant.Identifier.Text}' is already declared.");
            return;
        }
        if (!TryEvaluateConstant(constant.Expression, out var value, out var type))
        {
            _diagnostics.Report("SML3013", constant.Expression.Span, "CONST initializer must be a compile-time scalar expression.");
            return;
        }
        _symbols[constant.Identifier.Text] = new VariableSymbol(constant.Identifier.Text, type, Array.Empty<int>(),
            constant.Identifier.Span, isConstant: true, constantValue: value);
        _expressionTypes[constant.Expression] = type;
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
                _diagnostics.Report("SML3009", assignment.Target.Identifier.Span, $"'{name}' is not an array.");
                return;
            }
            if (assignment.Target.Indices.Count != array.ArrayRank)
                _diagnostics.Report("SML3014", assignment.Target.Span, $"Array '{name}' requires {array.ArrayRank} index value(s).");
            if (valueType != SmileType.Error && valueType != SmileType.Number)
                _diagnostics.Report("SML3003", assignment.Expression.Span, "Array elements require NUMBER values.");
            return;
        }

        if (TryResolveExisting(name, out var existing))
        {
            if (existing.IsConstant)
            {
                _diagnostics.Report("SML3012", assignment.Target.Identifier.Span, $"Constant '{name}' cannot be assigned.");
                return;
            }
            if (existing.IsArray)
            {
                _diagnostics.Report("SML3009", assignment.Target.Identifier.Span, $"Array '{name}' requires an index.");
                return;
            }
            if (valueType != SmileType.Error && existing.Type != valueType)
                _diagnostics.Report("SML3003", assignment.Expression.Span, $"Cannot assign {TypeName(valueType)} to {TypeName(existing.Type)} variable '{name}'.");
            return;
        }

        if (valueType == SmileType.Text)
        {
            _diagnostics.Report("SML3010", assignment.Expression.Span, "General-purpose TEXT variables are not supported.");
            return;
        }
        if (valueType == SmileType.Error)
            return;
        DeclareVariable(name, valueType, Array.Empty<int>(), assignment.Target.Identifier.Span);
    }

    private void AnalyzeDim(DimStatementSyntax dim)
    {
        if (TryResolveExisting(dim.Identifier.Text, out _))
        {
            _diagnostics.Report("SML3005", dim.Identifier.Span, $"'{dim.Identifier.Text}' is already declared.");
            return;
        }
        if (dim.Sizes.Count is < 1 or > 2)
        {
            _diagnostics.Report("SML3014", dim.Span, "Arrays require one or two dimensions.");
            return;
        }
        var dimensions = new List<int>();
        long total = 1;
        foreach (var sizeExpression in dim.Sizes)
        {
            if (!TryEvaluateConstant(sizeExpression, out var value, out var type) || type != SmileType.Number || value <= 0 || value > int.MaxValue)
            {
                _diagnostics.Report("SML3006", sizeExpression.Span, "Array dimension must be a positive compile-time NUMBER expression.");
                value = 1;
            }
            total *= value;
            if (total > int.MaxValue)
                _diagnostics.Report("SML3006", dim.Span, "Total array storage exceeds the supported size.");
            dimensions.Add((int)Math.Min(value, int.MaxValue));
        }
        DeclareVariable(dim.Identifier.Text, SmileType.Number, dimensions, dim.Identifier.Span);
    }

    private void AnalyzePrint(PrintStatementSyntax print)
    {
        foreach (var item in print.Items)
        {
            var type = AnalyzeExpression(item);
            if (type is not (SmileType.Error or SmileType.Text or SmileType.Number or SmileType.Boolean))
                _diagnostics.Report("SML3011", item.Span, "Invalid PRINT item.");
        }
    }

    private void AnalyzeReturn(ReturnStatementSyntax statement)
    {
        if (_currentRoutine == null)
        {
            _diagnostics.Report("SML3020", statement.ReturnKeyword.Span, "RETURN is only valid inside a SUB or FUNCTION.");
            return;
        }
        if (_currentRoutine.IsFunction)
        {
            if (statement.Expression == null)
            {
                _diagnostics.Report("SML3020", statement.ReturnKeyword.Span, "FUNCTION RETURN requires a value.");
                return;
            }
            var type = AnalyzeExpression(statement.Expression);
            if (type != SmileType.Error && type != _currentRoutine.ReturnType)
                _diagnostics.Report("SML3003", statement.Expression.Span, $"FUNCTION '{_currentRoutine.Name}' must return {TypeName(_currentRoutine.ReturnType)}.");
        }
        else if (statement.Expression != null)
        {
            AnalyzeExpression(statement.Expression);
            _diagnostics.Report("SML3020", statement.Expression.Span, "SUB RETURN cannot include a value.");
        }
    }

    private void AnalyzeSelect(SelectStatementSyntax select)
    {
        var selectorType = AnalyzeExpression(select.Expression);
        if (selectorType is not (SmileType.Number or SmileType.Boolean or SmileType.Error))
            _diagnostics.Report("SML3003", select.Expression.Span, "SELECT CASE expression must be NUMBER or BOOLEAN.");
        var values = new HashSet<long>();
        var sawElse = false;
        foreach (var clause in select.Cases)
        {
            if (clause.IsElse)
            {
                if (sawElse)
                    _diagnostics.Report("SML3019", clause.CaseKeyword.Span, "SELECT CASE contains more than one CASE ELSE.");
                sawElse = true;
            }
            else if (clause.Value != null)
            {
                var caseType = AnalyzeExpression(clause.Value);
                if (caseType != SmileType.Error && selectorType != SmileType.Error && caseType != selectorType)
                    _diagnostics.Report("SML3003", clause.Value.Span, "CASE value type must match SELECT CASE.");
                if (!TryEvaluateConstant(clause.Value, out var value, out _))
                    _diagnostics.Report("SML3013", clause.Value.Span, "CASE value must be a compile-time scalar expression.");
                else if (!values.Add(value))
                    _diagnostics.Report("SML3019", clause.Value.Span, $"Duplicate CASE value '{value}'.");
            }
            AnalyzeStatements(clause.Statements, false);
        }
    }

    private void AnalyzeExit(ExitStatementSyntax exit)
    {
        var valid = exit.TargetKeyword.Kind == SyntaxKind.ForKeyword ? _forDepth > 0 : _doDepth > 0;
        if (!valid)
            _diagnostics.Report("SML3018", exit.Span, $"EXIT {SyntaxFacts.GetText(exit.TargetKeyword.Kind)} is not inside a matching loop.");
    }

    private void EnsureNumberTarget(SyntaxToken identifier, string statementName)
    {
        if (!TryResolveExisting(identifier.Text, out var symbol))
        {
            DeclareVariable(identifier.Text, SmileType.Number, Array.Empty<int>(), identifier.Span);
            return;
        }
        if (symbol.IsConstant || symbol.IsArray || symbol.Type != SmileType.Number)
            _diagnostics.Report("SML3008", identifier.Span, $"{statementName} target '{identifier.Text}' must be a writable NUMBER variable.");
    }

    private void DeclareVariable(string name, SmileType type, IReadOnlyList<int> dimensions, TextSpan span)
    {
        if (_currentRoutine == null || _symbols.ContainsKey(name))
            _symbols[name] = new VariableSymbol(name, type, dimensions, span);
        else
            _currentRoutine.Locals[name] = new VariableSymbol(name, type, dimensions, span, routineName: _currentRoutine.Name);
    }

    private SmileType RequireType(ExpressionSyntax expression, SmileType requiredType, string code, string message)
    {
        var type = AnalyzeExpression(expression);
        if (type != SmileType.Error && type != requiredType)
            _diagnostics.Report(code, expression.Span, message);
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
                    _diagnostics.Report("SML3009", name.Span, $"Array '{name.Identifier.Text}' requires an index.");
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
                    _diagnostics.Report("SML3009", array.Identifier.Span, $"'{array.Identifier.Text}' is not an array.");
                    result = SmileType.Error;
                }
                else
                {
                    if (array.Indices.Count != arraySymbol.ArrayRank)
                        _diagnostics.Report("SML3014", array.Span, $"Array '{array.Identifier.Text}' requires {arraySymbol.ArrayRank} index value(s).");
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
                _diagnostics.Report("SML3003", argument.Span, "Routine arguments must be scalar NUMBER or BOOLEAN values.");
        }

        if (SyntaxFacts.IsBuiltInFunction(identifier.Kind))
            return AnalyzeBuiltInCall(identifier, arguments);

        if (!_routines.TryGetValue(identifier.Text, out var routine))
        {
            _diagnostics.Report("SML3021", identifier.Span, $"Unknown routine or built-in function '{identifier.Text}'.");
            return SmileType.Error;
        }
        if (routine.Parameters.Count != arguments.Count)
            _diagnostics.Report("SML3016", identifier.Span, $"Routine '{routine.Name}' expects {routine.Parameters.Count} argument(s), found {arguments.Count}.");
        if (requireFunction && !routine.IsFunction)
        {
            _diagnostics.Report("SML3020", identifier.Span, $"SUB '{routine.Name}' cannot be used as an expression.");
            return SmileType.Error;
        }
        if (!requireFunction && routine.IsFunction)
            _diagnostics.Report("SML3020", identifier.Span, $"FUNCTION '{routine.Name}' must be used in an expression.");
        return routine.IsFunction ? routine.ReturnType : SmileType.Error;
    }

    private SmileType AnalyzeBuiltInCall(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> arguments)
    {
        var expected = identifier.Kind switch
        {
            SyntaxKind.TimerKeyword or SyntaxKind.GameClosedKeyword => 0,
            SyntaxKind.AbsKeyword or SyntaxKind.KeyHeldKeyword => 1,
            SyntaxKind.MinKeyword or SyntaxKind.MaxKeyword => 2,
            SyntaxKind.RgbKeyword => 3,
            _ => -1
        };
        if (expected < 0)
        {
            _diagnostics.Report("SML3021", identifier.Span, $"Unknown built-in function '{identifier.Text}'.");
            return SmileType.Error;
        }
        if (arguments.Count != expected)
            _diagnostics.Report("SML3016", identifier.Span, $"Built-in '{identifier.Text}' expects {expected} argument(s), found {arguments.Count}.");
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
            _diagnostics.Report("SML3003", unary.Span, $"Operator '{unary.OperatorToken.Text}' requires {TypeName(required)}.");
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
                    _diagnostics.Report("SML3003", binary.Span, "Equality operands must have the same NUMBER or BOOLEAN type.");
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
            _diagnostics.Report("SML3003", binary.Span, $"Operator '{binary.OperatorToken.Text}' requires {TypeName(required)} operands.");
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
            return true;
        var declarations = _currentRoutine?.FirstDeclarations ?? _globalFirstDeclarations;
        if (declarations.TryGetValue(name, out var position) && position > token.Position)
            _diagnostics.Report("SML3002", token.Span, $"Variable '{name}' is used before its first assignment.");
        else
            _diagnostics.Report("SML3001", token.Span, $"Unknown identifier '{name}'.");
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
            case NameExpressionSyntax name when _symbols.TryGetValue(name.Identifier.Text, out var symbol) && symbol.IsConstant:
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

    private static string TypeName(SmileType type) => type.ToString().ToUpperInvariant();
}
