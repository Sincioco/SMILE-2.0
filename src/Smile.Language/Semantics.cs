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
    internal VariableSymbol(string name, SmileType type, bool isArray, int arraySize, TextSpan declarationSpan)
    {
        Name = name;
        Type = type;
        IsArray = isArray;
        ArraySize = arraySize;
        DeclarationSpan = declarationSpan;
    }

    public string Name { get; }
    public SmileType Type { get; }
    public bool IsArray { get; }
    public int ArraySize { get; }
    public TextSpan DeclarationSpan { get; }
}

public sealed class SemanticModel
{
    private readonly Dictionary<string, VariableSymbol> _symbols;
    private readonly Dictionary<ExpressionSyntax, SmileType> _expressionTypes;

    internal SemanticModel(Dictionary<string, VariableSymbol> symbols, Dictionary<ExpressionSyntax, SmileType> expressionTypes)
    {
        _symbols = symbols;
        _expressionTypes = expressionTypes;
    }

    public IReadOnlyDictionary<string, VariableSymbol> Symbols => _symbols;

    public bool TryGetSymbol(string name, out VariableSymbol symbol) => _symbols.TryGetValue(name, out symbol!);

    public SmileType GetType(ExpressionSyntax expression) =>
        _expressionTypes.TryGetValue(expression, out var type) ? type : SmileType.Error;
}

internal sealed class SemanticAnalyzer
{
    private readonly DiagnosticBag _diagnostics;
    private readonly Dictionary<string, VariableSymbol> _symbols = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _firstDeclarations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ExpressionSyntax, SmileType> _expressionTypes = new();

    public SemanticAnalyzer(SourceText source) => _diagnostics = new DiagnosticBag(source);

    public SemanticModel Analyze(CompilationUnitSyntax root)
    {
        CollectDeclarations(root.Statements);
        AnalyzeStatements(root.Statements);
        return new SemanticModel(_symbols, _expressionTypes);
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.ToArray();

    private void CollectDeclarations(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case AssignmentStatementSyntax assignment when !assignment.Target.IsArrayElement:
                    RecordDeclaration(assignment.Target.Identifier);
                    break;
                case DimStatementSyntax dim:
                    RecordDeclaration(dim.Identifier);
                    break;
                case GetKeyStatementSyntax getKey:
                    RecordDeclaration(getKey.Identifier);
                    break;
                case RandomStatementSyntax random:
                    RecordDeclaration(random.Identifier);
                    break;
                case ForStatementSyntax forStatement:
                    RecordDeclaration(forStatement.Identifier);
                    CollectDeclarations(forStatement.Statements);
                    break;
                case IfStatementSyntax ifStatement:
                    foreach (var clause in ifStatement.Clauses)
                        CollectDeclarations(clause.Statements);
                    CollectDeclarations(ifStatement.ElseStatements);
                    break;
                case DoUntilStatementSyntax doUntil:
                    CollectDeclarations(doUntil.Statements);
                    break;
            }
        }
    }

    private void RecordDeclaration(SyntaxToken identifier)
    {
        if (!_firstDeclarations.TryGetValue(identifier.Text, out var position) || identifier.Position < position)
            _firstDeclarations[identifier.Text] = identifier.Position;
    }

    private void AnalyzeStatements(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
            AnalyzeStatement(statement);
    }

    private void AnalyzeStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
                AnalyzeAssignment(assignment);
                break;
            case DimStatementSyntax dim:
                AnalyzeDim(dim);
                break;
            case PrintStatementSyntax print:
                AnalyzePrint(print);
                break;
            case GetKeyStatementSyntax getKey:
                EnsureNumberTarget(getKey.Identifier, "GET KEY");
                break;
            case ClearScreenStatementSyntax:
                break;
            case WaitStatementSyntax wait:
                RequireType(wait.Duration, SmileType.Number, "SML3008", "WAIT duration must be NUMBER.");
                break;
            case RandomStatementSyntax random:
                RequireType(random.Minimum, SmileType.Number, "SML3008", "RANDOM minimum must be NUMBER.");
                RequireType(random.Maximum, SmileType.Number, "SML3008", "RANDOM maximum must be NUMBER.");
                EnsureNumberTarget(random.Identifier, "RANDOM");
                break;
            case IfStatementSyntax ifStatement:
                foreach (var clause in ifStatement.Clauses)
                {
                    RequireType(clause.Condition, SmileType.Boolean, "SML3004", "IF condition must be BOOLEAN.");
                    AnalyzeStatements(clause.Statements);
                }
                AnalyzeStatements(ifStatement.ElseStatements);
                break;
            case ForStatementSyntax forStatement:
                AnalyzeFor(forStatement);
                break;
            case DoUntilStatementSyntax doUntil:
                AnalyzeStatements(doUntil.Statements);
                RequireType(doUntil.Condition, SmileType.Boolean, "SML3004", "LOOP UNTIL condition must be BOOLEAN.");
                break;
        }
    }

    private void AnalyzeAssignment(AssignmentStatementSyntax assignment)
    {
        var valueType = AnalyzeExpression(assignment.Expression);
        var name = assignment.Target.Identifier.Text;

        if (assignment.Target.IsArrayElement)
        {
            RequireType(assignment.Target.Index!, SmileType.Number, "SML3007", "Array index must be NUMBER.");
            if (!TryResolve(name, assignment.Target.Identifier, out var array))
                return;

            if (!array.IsArray)
            {
                _diagnostics.Report("SML3009", assignment.Target.Identifier.Span, $"'{name}' is not an array.");
                return;
            }

            if (valueType != SmileType.Error && valueType != SmileType.Number)
                _diagnostics.Report("SML3003", assignment.Expression.Span, "Array elements require NUMBER values.");
            return;
        }

        if (!_symbols.TryGetValue(name, out var symbol))
        {
            if (valueType == SmileType.Text)
            {
                _diagnostics.Report("SML3010", assignment.Expression.Span, "General-purpose TEXT variables are not supported in the MVP.");
                return;
            }

            if (valueType != SmileType.Error)
                _symbols[name] = new VariableSymbol(name, valueType, false, 0, assignment.Target.Identifier.Span);
            return;
        }

        if (symbol.IsArray)
        {
            _diagnostics.Report("SML3009", assignment.Target.Identifier.Span, $"Array '{name}' requires an index.");
            return;
        }

        if (valueType != SmileType.Error && symbol.Type != valueType)
            _diagnostics.Report("SML3003", assignment.Expression.Span, $"Cannot assign {TypeName(valueType)} to {TypeName(symbol.Type)} variable '{name}'.");
    }

    private void AnalyzeDim(DimStatementSyntax dim)
    {
        var name = dim.Identifier.Text;
        if (_symbols.ContainsKey(name))
        {
            _diagnostics.Report("SML3005", dim.Identifier.Span, $"'{name}' is already declared.");
            return;
        }

        var size = dim.Size.Value is long value ? value : 0;
        if (size <= 0 || size > int.MaxValue)
        {
            _diagnostics.Report("SML3006", dim.Size.Span, "Array size must be a positive integer literal.");
            size = 1;
        }

        _symbols[name] = new VariableSymbol(name, SmileType.Number, true, (int)size, dim.Identifier.Span);
    }

    private void AnalyzePrint(PrintStatementSyntax print)
    {
        foreach (var item in print.Items)
        {
            var type = AnalyzeExpression(item);
            if (type != SmileType.Error && type != SmileType.Text && type != SmileType.Number && type != SmileType.Boolean)
                _diagnostics.Report("SML3011", item.Span, "Invalid PRINT item.");
        }
    }

    private void AnalyzeFor(ForStatementSyntax forStatement)
    {
        RequireType(forStatement.LowerBound, SmileType.Number, "SML3008", "FOR lower bound must be NUMBER.");
        RequireType(forStatement.UpperBound, SmileType.Number, "SML3008", "FOR upper bound must be NUMBER.");
        EnsureNumberTarget(forStatement.Identifier, "FOR");
        AnalyzeStatements(forStatement.Statements);
    }

    private void EnsureNumberTarget(SyntaxToken identifier, string statementName)
    {
        var name = identifier.Text;
        if (!_symbols.TryGetValue(name, out var symbol))
        {
            _symbols[name] = new VariableSymbol(name, SmileType.Number, false, 0, identifier.Span);
            return;
        }

        if (symbol.IsArray || symbol.Type != SmileType.Number)
            _diagnostics.Report("SML3008", identifier.Span, $"{statementName} target '{name}' must be a NUMBER variable.");
    }

    private SmileType RequireType(ExpressionSyntax expression, SmileType requiredType, string code, string message)
    {
        var actualType = AnalyzeExpression(expression);
        if (actualType != SmileType.Error && actualType != requiredType)
            _diagnostics.Report(code, expression.Span, message);
        return actualType;
    }

    private SmileType AnalyzeExpression(ExpressionSyntax expression)
    {
        if (_expressionTypes.TryGetValue(expression, out var cached))
            return cached;

        SmileType result;
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                result = literal.Value switch
                {
                    bool => SmileType.Boolean,
                    string => SmileType.Text,
                    _ => SmileType.Number
                };
                break;
            case NameExpressionSyntax name:
                if (!TryResolve(name.Identifier.Text, name.Identifier, out var symbol))
                {
                    result = SmileType.Error;
                }
                else if (symbol.IsArray)
                {
                    _diagnostics.Report("SML3009", name.Span, $"Array '{name.Identifier.Text}' requires an index.");
                    result = SmileType.Error;
                }
                else
                {
                    result = symbol.Type;
                }
                break;
            case ArrayAccessExpressionSyntax arrayAccess:
                RequireType(arrayAccess.Index, SmileType.Number, "SML3007", "Array index must be NUMBER.");
                if (!TryResolve(arrayAccess.Identifier.Text, arrayAccess.Identifier, out var array))
                {
                    result = SmileType.Error;
                }
                else if (!array.IsArray)
                {
                    _diagnostics.Report("SML3009", arrayAccess.Identifier.Span, $"'{arrayAccess.Identifier.Text}' is not an array.");
                    result = SmileType.Error;
                }
                else
                {
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
            default:
                result = SmileType.Error;
                break;
        }

        _expressionTypes[expression] = result;
        return result;
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

    private SmileType RequireOperands(BinaryExpressionSyntax binary, SmileType leftType, SmileType rightType, SmileType required, SmileType result)
    {
        if (leftType != required || rightType != required)
        {
            _diagnostics.Report("SML3003", binary.Span, $"Operator '{binary.OperatorToken.Text}' requires {TypeName(required)} operands.");
            return SmileType.Error;
        }
        return result;
    }

    private bool TryResolve(string name, SyntaxToken token, out VariableSymbol symbol)
    {
        if (_symbols.TryGetValue(name, out symbol!))
            return true;

        if (_firstDeclarations.TryGetValue(name, out var declarationPosition) && declarationPosition > token.Position)
            _diagnostics.Report("SML3002", token.Span, $"Variable '{name}' is used before its first assignment.");
        else
            _diagnostics.Report("SML3001", token.Span, $"Unknown identifier '{name}'.");

        symbol = null!;
        return false;
    }

    private static string TypeName(SmileType type) => type.ToString().ToUpperInvariant();
}
