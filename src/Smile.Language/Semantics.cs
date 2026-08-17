using System;
using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

public enum SmileTypeKind
{
    Error,
    Number,
    Boolean,
    Text,
    Image,
    Enum,
    Record
}

public class SmileType
{
    private SmileType(SmileTypeKind kind, string name)
    {
        Kind = kind;
        Name = name;
        SemanticName = name;
        RuntimeIdentity = name;
        ContainsOwnedText = kind == SmileTypeKind.Text;
        ContainsOwnedImage = kind == SmileTypeKind.Image;
    }

    protected SmileType(SmileTypeKind kind, string name, SourceText source, int sourceOrdinal,
        TextSpan declarationSpan)
    {
        Kind = kind;
        Name = name;
        SemanticName = name;
        RuntimeIdentity = name;
        Source = source;
        SourceOrdinal = sourceOrdinal;
        DeclarationSpan = declarationSpan;
    }

    public static SmileType Error { get; } = new(SmileTypeKind.Error, "ERROR");
    public static SmileType Number { get; } = new(SmileTypeKind.Number, "Number");
    public static SmileType Boolean { get; } = new(SmileTypeKind.Boolean, "Boolean");
    public static SmileType Text { get; } = new(SmileTypeKind.Text, "Text");
    public static SmileType Image { get; } = new(SmileTypeKind.Image, "Image");

    public SmileTypeKind Kind { get; }
    public string Name { get; protected set; }
    public string SemanticName { get; }
    public string RuntimeIdentity { get; protected set; }
    public string? ModuleName { get; protected set; }
    public ModuleVisibility Visibility { get; protected set; } = ModuleVisibility.Public;
    public string ProviderIdentity { get; protected set; } = string.Empty;
    public SourceText? Source { get; }
    public int SourceOrdinal { get; }
    public TextSpan DeclarationSpan { get; }
    public SourceLocation? DeclarationLocation => Source == null ? null : new SourceLocation(Source, DeclarationSpan);
    public bool IsRecord => Kind == SmileTypeKind.Record;
    public bool IsEnum => Kind == SmileTypeKind.Enum;
    public virtual int Size { get; internal set; } = 8;
    public virtual int Alignment { get; internal set; } = 8;
    public virtual bool ContainsOwnedText { get; internal set; }
    public virtual bool ContainsOwnedImage { get; internal set; }
    public bool RequiresCleanup => ContainsOwnedText || ContainsOwnedImage;
    public override string ToString() => Name;
}

public abstract class NominalTypeSymbol : SmileType
{
    protected NominalTypeSymbol(SmileTypeKind kind, string name, SourceText source, int sourceOrdinal,
        TextSpan declarationSpan)
        : base(kind, name, source, sourceOrdinal, declarationSpan)
    {
    }

    internal void ApplyModuleIdentity(string name, string moduleName, ModuleVisibility visibility,
        string providerIdentity, string runtimeIdentity)
    {
        Name = name;
        ModuleName = moduleName;
        Visibility = visibility;
        ProviderIdentity = providerIdentity;
        RuntimeIdentity = runtimeIdentity;
    }
}

public sealed class RecordFieldSymbol
{
    internal RecordFieldSymbol(string name, SyntaxToken typeToken, SourceText source, TextSpan declarationSpan, int ordinal)
    {
        Name = name;
        TypeToken = typeToken;
        Source = source;
        DeclarationSpan = declarationSpan;
        Ordinal = ordinal;
        Type = SmileType.Error;
    }

    public string Name { get; }
    public SyntaxToken TypeToken { get; }
    public SmileType Type { get; internal set; }
    public int Ordinal { get; }
    public int Offset { get; internal set; }
    public SourceText Source { get; }
    public TextSpan DeclarationSpan { get; }
    public SourceLocation DeclarationLocation => new(Source, DeclarationSpan);
}

public sealed class RecordTypeSymbol : NominalTypeSymbol
{
    private readonly List<RecordFieldSymbol> _fields = new();
    private readonly Dictionary<string, RecordFieldSymbol> _fieldsByName = new(StringComparer.OrdinalIgnoreCase);

    internal RecordTypeSymbol(string name, TypeDeclarationSyntax declaration, SourceText source, int sourceOrdinal)
        : base(SmileTypeKind.Record, name, source, sourceOrdinal, declaration.Identifier.Span)
    {
        Declaration = declaration;
    }

    public TypeDeclarationSyntax Declaration { get; }
    public IReadOnlyList<RecordFieldSymbol> Fields => _fields;
    public override int Size { get; internal set; } = 8;
    public override bool ContainsOwnedText { get; internal set; }
    public override bool ContainsOwnedImage { get; internal set; }
    public bool TryGetField(string name, out RecordFieldSymbol field) => _fieldsByName.TryGetValue(name, out field!);

    internal bool AddField(RecordFieldSymbol field)
    {
        if (_fieldsByName.ContainsKey(field.Name))
            return false;
        _fieldsByName[field.Name] = field;
        _fields.Add(field);
        return true;
    }

}

public sealed class EnumMemberSymbol
{
    internal EnumMemberSymbol(string name, long value, EnumTypeSymbol containingType,
        EnumMemberDeclarationSyntax declaration, SourceText source, int ordinal)
    {
        Name = name;
        Value = value;
        ContainingType = containingType;
        Declaration = declaration;
        Source = source;
        Ordinal = ordinal;
    }

    public string Name { get; }
    public long Value { get; }
    public EnumTypeSymbol ContainingType { get; }
    public EnumMemberDeclarationSyntax Declaration { get; }
    public SourceText Source { get; }
    public int Ordinal { get; }
    public TextSpan DeclarationSpan => Declaration.Identifier.Span;
    public SourceLocation DeclarationLocation => new(Source, DeclarationSpan);
}

public sealed class EnumTypeSymbol : NominalTypeSymbol
{
    private readonly List<EnumMemberSymbol> _members = new();
    private readonly Dictionary<string, EnumMemberSymbol> _membersByName =
        new(StringComparer.OrdinalIgnoreCase);

    internal EnumTypeSymbol(string name, EnumDeclarationSyntax declaration, SourceText source,
        int sourceOrdinal)
        : base(SmileTypeKind.Enum, name, source, sourceOrdinal, declaration.Identifier.Span)
    {
        Declaration = declaration;
    }

    public EnumDeclarationSyntax Declaration { get; }
    public IReadOnlyList<EnumMemberSymbol> Members => _members;
    public override int Size { get; internal set; } = 8;
    public override int Alignment { get; internal set; } = 8;
    public bool TryGetMember(string name, out EnumMemberSymbol member) =>
        _membersByName.TryGetValue(name, out member!);

    internal bool AddMember(EnumMemberSymbol member)
    {
        if (_membersByName.ContainsKey(member.Name))
            return false;
        _membersByName[member.Name] = member;
        _members.Add(member);
        return true;
    }
}

public enum WithStorageKind
{
    ValueLocation,
    ObjectReference
}

public sealed class WithTargetBinding
{
    internal WithTargetBinding(WithStatementSyntax statement, RecordTypeSymbol targetType,
        WithStorageKind storageKind, int depth, TextSpan activeBodySpan)
    {
        Statement = statement;
        TargetType = targetType;
        StorageKind = storageKind;
        Depth = depth;
        ActiveBodySpan = activeBodySpan;
    }

    public WithStatementSyntax Statement { get; }
    public RecordTypeSymbol TargetType { get; }
    public WithStorageKind StorageKind { get; }
    public int Depth { get; }
    public TextSpan ActiveBodySpan { get; }
}

public sealed class WithMemberBinding
{
    internal WithMemberBinding(WithStatementSyntax receiverStatement, RecordTypeSymbol containingType,
        RecordFieldSymbol field)
    {
        ReceiverStatement = receiverStatement;
        ContainingType = containingType;
        Field = field;
    }

    public WithStatementSyntax ReceiverStatement { get; }
    public RecordTypeSymbol ContainingType { get; }
    public RecordFieldSymbol Field { get; }
}

internal sealed class InvalidWithScope
{
    public InvalidWithScope(int depth, TextSpan activeBodySpan)
    {
        Depth = depth;
        ActiveBodySpan = activeBodySpan;
    }

    public int Depth { get; }
    public TextSpan ActiveBodySpan { get; }
}

public enum ParameterPassingMode
{
    ByVal,
    ByRef
}

[Flags]
public enum RoutineCapability
{
    None = 0,
    RequiresGameWindow = 1
}

public class VariableSymbol
{
    internal VariableSymbol(string name, SmileType type, IReadOnlyList<int> dimensions, SourceText source,
        int sourceOrdinal, TextSpan declarationSpan,
        bool isConstant = false, object? constantValue = null, string? routineName = null,
        ParameterPassingMode? parameterMode = null, bool hasDeclaredType = true,
        EnumMemberSymbol? constantEnumMember = null)
    {
        Name = name;
        SemanticName = name;
        RuntimeIdentity = name;
        Type = type;
        ArrayDimensions = dimensions;
        Source = source;
        SourceOrdinal = sourceOrdinal;
        DeclarationSpan = declarationSpan;
        IsConstant = isConstant;
        ConstantValue = constantValue ?? 0L;
        ConstantEnumMember = constantEnumMember;
        RoutineName = routineName;
        ParameterMode = parameterMode;
        HasDeclaredType = hasDeclaredType;
        long total = 1;
        foreach (var dimension in dimensions)
            total *= dimension;
        ArraySize = dimensions.Count == 0 ? 0 : (int)Math.Min(total, int.MaxValue);
    }

    public string Name { get; private set; }
    public string SemanticName { get; }
    public string RuntimeIdentity { get; private set; }
    public string? ModuleName { get; private set; }
    public ModuleVisibility Visibility { get; private set; } = ModuleVisibility.Public;
    public string ProviderIdentity { get; private set; } = string.Empty;
    public SmileType Type { get; internal set; }
    public bool IsArray => ArrayDimensions.Count != 0;
    public int ArraySize { get; }
    public int ArrayRank => ArrayDimensions.Count;
    public IReadOnlyList<int> ArrayDimensions { get; }
    public bool IsConstant { get; }
    public object ConstantValue { get; }
    public EnumMemberSymbol? ConstantEnumMember { get; }
    public string? RoutineName { get; }
    public ParameterPassingMode? ParameterMode { get; }
    public bool IsParameter => ParameterMode.HasValue;
    public bool HasDeclaredType { get; }
    public SourceText Source { get; }
    public int SourceOrdinal { get; }
    public TextSpan DeclarationSpan { get; }
    public SourceLocation DeclarationLocation => new(Source, DeclarationSpan);

    internal void ApplyModuleIdentity(string name, string moduleName, ModuleVisibility visibility,
        string providerIdentity, string runtimeIdentity)
    {
        Name = name;
        ModuleName = moduleName;
        Visibility = visibility;
        ProviderIdentity = providerIdentity;
        RuntimeIdentity = runtimeIdentity;
    }
}

public sealed class ParameterSymbol : VariableSymbol
{
    internal ParameterSymbol(ParameterSyntax declaration, SmileType type, SourceText source, int sourceOrdinal,
        string routineName, ParameterPassingMode mode, bool hasDeclaredType)
        : base(declaration.Identifier.Text, type, Array.Empty<int>(), source, sourceOrdinal,
            declaration.Identifier.Span, routineName: routineName, parameterMode: mode,
            hasDeclaredType: hasDeclaredType)
    {
        Declaration = declaration;
    }

    public ParameterSyntax Declaration { get; }
    public bool IsOptional => Declaration.IsOptional;
    public bool HasDefaultValue { get; private set; }
    public object DefaultValue { get; private set; } = 0L;
    public EnumMemberSymbol? DefaultEnumMember { get; private set; }

    internal void BindDefault(object value, EnumMemberSymbol? enumMember)
    {
        DefaultValue = value;
        DefaultEnumMember = enumMember;
        HasDefaultValue = true;
    }
}

public sealed class BoundCallArgument
{
    internal BoundCallArgument(ParameterSymbol parameter, int parameterIndex, ArgumentSyntax? syntax,
        int sourceIndex)
    {
        Parameter = parameter;
        ParameterIndex = parameterIndex;
        Syntax = syntax;
        SourceIndex = sourceIndex;
    }

    public ParameterSymbol Parameter { get; }
    public int ParameterIndex { get; }
    public ArgumentSyntax? Syntax { get; }
    public ExpressionSyntax? Expression => Syntax?.Expression;
    public int SourceIndex { get; }
    public bool IsDefault => Syntax == null;
    public object DefaultValue => Parameter.DefaultValue;
    public EnumMemberSymbol? DefaultEnumMember => Parameter.DefaultEnumMember;
}

public sealed class BoundCall
{
    internal BoundCall(RoutineSymbol routine, IReadOnlyList<BoundCallArgument> sourceArguments,
        IReadOnlyList<BoundCallArgument> parameterArguments)
    {
        Routine = routine;
        SourceArguments = sourceArguments;
        ParameterArguments = parameterArguments;
    }

    public RoutineSymbol Routine { get; }
    public IReadOnlyList<BoundCallArgument> SourceArguments { get; }
    public IReadOnlyList<BoundCallArgument> ParameterArguments { get; }
}

public sealed class RoutineSymbol
{
    internal RoutineSymbol(RoutineDeclarationSyntax declaration, IReadOnlyList<ParameterSymbol> parameters,
        SmileType returnType, bool hasDeclaredReturnType, SourceText source, int sourceOrdinal)
    {
        Declaration = declaration;
        Name = declaration.Identifier.Text;
        SemanticName = Name;
        RuntimeIdentity = Name;
        IsFunction = declaration.IsFunction;
        Parameters = parameters;
        ReturnType = returnType;
        HasDeclaredReturnType = hasDeclaredReturnType;
        Source = source;
        SourceOrdinal = sourceOrdinal;
        DisplayName = declaration.Identifier.Value as string ??
            source.Substring(declaration.Identifier.Span.Start, declaration.Identifier.Span.Length);
        Locals = new Dictionary<string, VariableSymbol>(StringComparer.OrdinalIgnoreCase);
        FirstDeclarations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
            Locals[parameter.Name] = parameter;
    }

    public string Name { get; private set; }
    public string SemanticName { get; }
    public string RuntimeIdentity { get; private set; }
    public string? ModuleName { get; private set; }
    public ModuleVisibility Visibility { get; private set; } = ModuleVisibility.Public;
    public string ProviderIdentity { get; private set; } = string.Empty;
    public bool IsFunction { get; }
    public IReadOnlyList<ParameterSymbol> Parameters { get; }
    public SmileType ReturnType { get; internal set; }
    public bool HasDeclaredReturnType { get; }
    public IReadOnlyDictionary<string, VariableSymbol> LocalSymbols => Locals;
    public RoutineDeclarationSyntax Declaration { get; }
    public SourceText Source { get; }
    public int SourceOrdinal { get; }
    public string DisplayName { get; private set; }
    public RoutineCapability Capabilities { get; internal set; }
    public bool RequiresGameWindow => (Capabilities & RoutineCapability.RequiresGameWindow) != 0;
    public SourceLocation DeclarationLocation => new(Source, Declaration.Identifier.Span);
    internal Dictionary<string, VariableSymbol> Locals { get; }
    internal Dictionary<string, int> FirstDeclarations { get; }

    internal void ApplyModuleIdentity(string name, string moduleName, ModuleVisibility visibility,
        string providerIdentity, string runtimeIdentity)
    {
        Name = name;
        ModuleName = moduleName;
        Visibility = visibility;
        ProviderIdentity = providerIdentity;
        RuntimeIdentity = runtimeIdentity;
        DisplayName = moduleName + "." + name;
        for (var index = 0; index < Parameters.Count; index++)
        {
            var parameter = Parameters[index];
            parameter.ApplyModuleIdentity(parameter.Name, moduleName, visibility, providerIdentity,
                runtimeIdentity + "::parameter::" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}

public sealed class SemanticModel
{
    private readonly Dictionary<string, VariableSymbol> _symbols;
    private readonly Dictionary<string, RoutineSymbol> _routines;
    private readonly Dictionary<ExpressionSyntax, SmileType> _expressionTypes;
    private readonly Dictionary<string, RecordTypeSymbol> _types;
    private readonly Dictionary<string, EnumTypeSymbol> _enumTypes;
    private readonly Dictionary<string, NominalTypeSymbol> _nominalTypes;
    private readonly Dictionary<ExpressionSyntax, RecordFieldSymbol> _fields;
    private readonly Dictionary<ExpressionSyntax, EnumMemberSymbol> _enumMembers;
    private readonly Dictionary<WithStatementSyntax, WithTargetBinding> _withTargets;
    private readonly Dictionary<LeadingMemberAccessExpressionSyntax, WithMemberBinding> _withMembers;
    private readonly Dictionary<SourceText, IReadOnlyList<WithTargetBinding>> _withScopes;
    private readonly Dictionary<SourceText, IReadOnlyList<InvalidWithScope>> _invalidWithScopes;
    private readonly Dictionary<SourceText, IReadOnlyDictionary<int, RecordFieldSymbol>> _fieldUses;
    private readonly Dictionary<SourceText, IReadOnlyDictionary<int, EnumMemberSymbol>> _enumMemberUses;
    private readonly Dictionary<SyntaxNode, BoundCall> _boundCalls;
    private readonly Dictionary<SourceText, IReadOnlyDictionary<int, ParameterSymbol>> _parameterUses;
    private IReadOnlyDictionary<string, ModuleSymbol> _modules =
        new Dictionary<string, ModuleSymbol>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<SourceText, IReadOnlyDictionary<string, ModuleSymbol>> _imports =
        new Dictionary<SourceText, IReadOnlyDictionary<string, ModuleSymbol>>();

    internal SemanticModel(Dictionary<string, VariableSymbol> symbols, Dictionary<string, RoutineSymbol> routines,
        Dictionary<ExpressionSyntax, SmileType> expressionTypes, Dictionary<string, RecordTypeSymbol> types,
        Dictionary<string, EnumTypeSymbol> enumTypes,
        Dictionary<ExpressionSyntax, RecordFieldSymbol> fields,
        Dictionary<ExpressionSyntax, EnumMemberSymbol> enumMembers,
        Dictionary<WithStatementSyntax, WithTargetBinding> withTargets,
        Dictionary<LeadingMemberAccessExpressionSyntax, WithMemberBinding> withMembers,
        Dictionary<SourceText, List<WithTargetBinding>> withScopes,
        Dictionary<SourceText, List<InvalidWithScope>> invalidWithScopes,
        Dictionary<SourceText, Dictionary<int, RecordFieldSymbol>> fieldUses,
        Dictionary<SourceText, Dictionary<int, EnumMemberSymbol>> enumMemberUses,
        Dictionary<SyntaxNode, BoundCall> boundCalls,
        Dictionary<SourceText, Dictionary<int, ParameterSymbol>> parameterUses)
    {
        _symbols = symbols;
        _routines = routines;
        _expressionTypes = expressionTypes;
        _types = types;
        _enumTypes = enumTypes;
        _nominalTypes = types.Values.Cast<NominalTypeSymbol>().Concat(enumTypes.Values)
            .ToDictionary(type => type.SemanticName, StringComparer.OrdinalIgnoreCase);
        _fields = fields;
        _enumMembers = enumMembers;
        _withTargets = withTargets;
        _withMembers = withMembers;
        _withScopes = withScopes.ToDictionary(item => item.Key,
            item => (IReadOnlyList<WithTargetBinding>)item.Value.ToArray());
        _invalidWithScopes = invalidWithScopes.ToDictionary(item => item.Key,
            item => (IReadOnlyList<InvalidWithScope>)item.Value.ToArray());
        _fieldUses = fieldUses.ToDictionary(item => item.Key,
            item => (IReadOnlyDictionary<int, RecordFieldSymbol>)item.Value);
        _enumMemberUses = enumMemberUses.ToDictionary(item => item.Key,
            item => (IReadOnlyDictionary<int, EnumMemberSymbol>)item.Value);
        _boundCalls = boundCalls;
        _parameterUses = parameterUses.ToDictionary(item => item.Key,
            item => (IReadOnlyDictionary<int, ParameterSymbol>)item.Value);
    }

    public IReadOnlyDictionary<string, VariableSymbol> Symbols => _symbols;
    public IReadOnlyDictionary<string, RoutineSymbol> Routines => _routines;
    public IReadOnlyDictionary<string, RecordTypeSymbol> Types => _types;
    public IReadOnlyDictionary<string, EnumTypeSymbol> EnumTypes => _enumTypes;
    public IReadOnlyDictionary<string, NominalTypeSymbol> NominalTypes => _nominalTypes;
    public IReadOnlyDictionary<string, ModuleSymbol> Modules => _modules;
    public bool TryGetSymbol(string name, out VariableSymbol symbol) => _symbols.TryGetValue(name, out symbol!);
    public bool TryGetRoutine(string name, out RoutineSymbol routine) => _routines.TryGetValue(name, out routine!);
    public bool TryGetType(string name, out RecordTypeSymbol type) => _types.TryGetValue(name, out type!);
    public bool TryGetEnumType(string name, out EnumTypeSymbol type) => _enumTypes.TryGetValue(name, out type!);
    public bool TryGetNominalType(string name, out NominalTypeSymbol type) =>
        _nominalTypes.TryGetValue(name, out type!);
    public bool TryGetField(ExpressionSyntax expression, out RecordFieldSymbol field) => _fields.TryGetValue(expression, out field!);
    public bool TryGetEnumMember(ExpressionSyntax expression, out EnumMemberSymbol member) =>
        _enumMembers.TryGetValue(expression, out member!);
    public bool TryGetWithTarget(WithStatementSyntax statement, out WithTargetBinding binding) =>
        _withTargets.TryGetValue(statement, out binding!);
    public bool TryGetWithMember(LeadingMemberAccessExpressionSyntax expression, out WithMemberBinding binding) =>
        _withMembers.TryGetValue(expression, out binding!);
    public bool TryGetFieldUse(SourceText source, int position, out RecordFieldSymbol field)
    {
        field = null!;
        return _fieldUses.TryGetValue(source, out var uses) && uses.TryGetValue(position, out field!);
    }

    public bool TryGetEnumMemberUse(SourceText source, int position, out EnumMemberSymbol member)
    {
        member = null!;
        return _enumMemberUses.TryGetValue(source, out var uses) && uses.TryGetValue(position, out member!);
    }

    public bool TryGetBoundCall(SyntaxNode syntax, out BoundCall call) =>
        _boundCalls.TryGetValue(syntax, out call!);

    public bool TryGetParameterUse(SourceText source, int position, out ParameterSymbol parameter)
    {
        parameter = null!;
        return _parameterUses.TryGetValue(source, out var uses) &&
               uses.TryGetValue(position, out parameter!);
    }

    public bool TryGetInnermostWithScope(SourceText source, int position, out WithTargetBinding binding)
    {
        binding = null!;
        if (_withScopes.TryGetValue(source, out var scopes))
        {
            foreach (var candidate in scopes)
            {
                if (position < candidate.ActiveBodySpan.Start || position >= candidate.ActiveBodySpan.End)
                    continue;
                if (binding == null || candidate.Depth > binding.Depth)
                    binding = candidate;
            }
        }

        var validDepth = binding?.Depth ?? -1;
        if (_invalidWithScopes.TryGetValue(source, out var invalidScopes) && invalidScopes.Any(candidate =>
                candidate.Depth > validDepth && position >= candidate.ActiveBodySpan.Start &&
                position < candidate.ActiveBodySpan.End))
        {
            binding = null!;
            return false;
        }

        return binding != null;
    }

    public bool TryResolveVariable(string name, string? routineName, out VariableSymbol symbol)
    {
        if (routineName != null)
        {
            var routine = _routines.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, routineName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.SemanticName, routineName, StringComparison.OrdinalIgnoreCase));
            if (routine != null && routine.Locals.TryGetValue(name, out symbol!))
                return true;
        }
        return _symbols.TryGetValue(name, out symbol!);
    }

    public bool TryResolveVariable(string name, RoutineSymbol? routine, out VariableSymbol symbol)
    {
        if (routine != null && routine.Locals.TryGetValue(name, out symbol!))
            return true;
        return _symbols.TryGetValue(name, out symbol!);
    }

    public SmileType GetType(ExpressionSyntax expression) =>
        _expressionTypes.TryGetValue(expression, out var type) ? type : SmileType.Error;

    public IReadOnlyDictionary<string, ModuleSymbol> GetImports(SourceText source) =>
        _imports.TryGetValue(source, out var imports)
            ? imports
            : new Dictionary<string, ModuleSymbol>(StringComparer.OrdinalIgnoreCase);

    internal void SetModules(IReadOnlyDictionary<string, ModuleSymbol> modules,
        IReadOnlyDictionary<SourceText, IReadOnlyDictionary<string, ModuleSymbol>> imports)
    {
        _modules = modules;
        _imports = imports;
    }
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
    private readonly Dictionary<string, long> _checkedNumberConstants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConstantResolutionState> _checkedNumberConstantStates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ExpressionSyntax, SmileType> _expressionTypes = new();
    private readonly Dictionary<string, RecordTypeSymbol> _types = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EnumTypeSymbol> _enumTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ExpressionSyntax, RecordFieldSymbol> _fields = new();
    private readonly Dictionary<ExpressionSyntax, EnumMemberSymbol> _enumMembers = new();
    private readonly Dictionary<WithStatementSyntax, WithTargetBinding> _withTargets = new();
    private readonly Dictionary<LeadingMemberAccessExpressionSyntax, WithMemberBinding> _withMembers = new();
    private readonly Dictionary<SourceText, List<WithTargetBinding>> _withScopes = new();
    private readonly Dictionary<SourceText, List<InvalidWithScope>> _invalidWithScopes = new();
    private readonly Dictionary<SourceText, Dictionary<int, RecordFieldSymbol>> _fieldUses = new();
    private readonly Dictionary<SourceText, Dictionary<int, EnumMemberSymbol>> _enumMemberUses = new();
    private readonly Dictionary<SyntaxNode, BoundCall> _boundCalls = new();
    private readonly Dictionary<SourceText, Dictionary<int, ParameterSymbol>> _parameterUses = new();
    private readonly List<WithTargetBinding?> _withStack = new();
    private readonly List<RoutineCallSite> _routineCalls = new();
    private SourceText _currentSource = null!;
    private int _currentSourceOrdinal;
    private RoutineSymbol? _currentRoutine;
    private int _forDepth;
    private int _doDepth;
    private bool _hasGameWindow;
    private int _gameWindowCount;
    private bool _optionExplicit;

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

        InventoryNominalTypes();
        InventoryProjectDeclarations();
        InventoryConstantDeclarations();
        BindEnumTypes();
        BindRecordTypes();
        foreach (var tree in _syntaxTrees)
            CollectRoutineDeclarations(tree);
        CollectGlobalDeclarations();
        BindOptionalParameterDefaults();
        InferLegacyRoutineReturnTypes();
        RefreshImplicitGlobalTypes();
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
                Report("SML3017", routine.Declaration.Identifier.Span, $"Function '{routine.Name}' does not return a value on every path.");
        }
        _currentRoutine = null;
        PropagateRoutineCapabilities();
        DiagnoseTopLevelRoutineCapabilities();
        return new SemanticModel(_symbols, _routines, _expressionTypes, _types, _enumTypes, _fields,
            _enumMembers, _withTargets, _withMembers, _withScopes, _invalidWithScopes, _fieldUses,
            _enumMemberUses, _boundCalls, _parameterUses);
    }

    private void InventoryNominalTypes()
    {
        foreach (var tree in _syntaxTrees)
        {
            foreach (var statement in tree.Root.Statements)
            {
                switch (statement)
                {
                    case TypeDeclarationSyntax declaration:
                    {
                        var name = declaration.Identifier.Text;
                        if (_types.ContainsKey(name) || _enumTypes.ContainsKey(name))
                        {
                            _diagnostics.Report(tree.Source, "SML3400", declaration.Identifier.Span,
                                $"Nominal type '{DisplaySourceText(tree.Source, declaration.Identifier)}' is already declared.");
                            break;
                        }
                        _types[name] = new RecordTypeSymbol(name, declaration, tree.Source,
                            _sourceOrdinals[tree.Source]);
                        break;
                    }
                    case EnumDeclarationSyntax declaration:
                    {
                        var name = declaration.Identifier.Text;
                        if (_types.ContainsKey(name) || _enumTypes.ContainsKey(name))
                        {
                            _diagnostics.Report(tree.Source, "SML3420", declaration.Identifier.Span,
                                $"Nominal type '{DisplaySourceText(tree.Source, declaration.Identifier)}' is already declared.");
                            break;
                        }
                        _enumTypes[name] = new EnumTypeSymbol(name, declaration, tree.Source,
                            _sourceOrdinals[tree.Source]);
                        break;
                    }
                }
            }
        }
    }

    private void BindEnumTypes()
    {
        foreach (var enumType in _enumTypes.Values.OrderBy(type => type.SourceOrdinal)
                     .ThenBy(type => type.DeclarationSpan.Start))
        {
            SetCurrentSource(enumType.Source!);
            if (enumType.Declaration.Members.Count == 0)
                Report("SML3421", enumType.Declaration.Identifier.Span,
                    $"Enum '{DisplaySourceText(_currentSource, enumType.Declaration.Identifier)}' must declare at least one member.");

            long previous = -1;
            var hasPrevious = false;
            for (var index = 0; index < enumType.Declaration.Members.Count; index++)
            {
                var declaration = enumType.Declaration.Members[index];
                long value;
                if (declaration.Value != null)
                {
                    if (!TryEvaluateEnumIntegral(declaration.Value, out value))
                    {
                        Report("SML3422", declaration.Value.Span,
                            "Enum member value must be a checked compile-time Int64 expression.");
                        value = 0;
                    }
                }
                else
                {
                    try
                    {
                        value = hasPrevious ? checked(previous + 1) : 0;
                    }
                    catch (OverflowException)
                    {
                        Report("SML3422", declaration.Identifier.Span,
                            $"Implicit value for Enum member '{declaration.Identifier.Text}' exceeds Int64.");
                        value = long.MaxValue;
                    }
                }

                var member = new EnumMemberSymbol(declaration.Identifier.Text, value, enumType,
                    declaration, _currentSource, index);
                if (!enumType.AddMember(member))
                {
                    Report("SML3421", declaration.Identifier.Span,
                        $"Enum member '{DisplaySourceText(_currentSource, declaration.Identifier)}' is already declared in Enum '{enumType.Name}'.");
                    continue;
                }
                previous = value;
                hasPrevious = true;
            }
        }
    }

    private bool TryEvaluateEnumIntegral(ExpressionSyntax expression, out long value)
    {
        try
        {
            switch (expression)
            {
                case LiteralExpressionSyntax { Value: long literal }:
                    value = literal;
                    return true;
                case NameExpressionSyntax name:
                    return TryResolveCheckedNumberConstant(name.Identifier.Text, out value);
                case ParenthesizedExpressionSyntax parenthesized:
                    return TryEvaluateEnumIntegral(parenthesized.Expression, out value);
                case UnaryExpressionSyntax { OperatorToken.Kind: SyntaxKind.MinusToken } unary
                    when TryEvaluateEnumIntegral(unary.Operand, out var operand):
                    value = checked(-operand);
                    return true;
                case BinaryExpressionSyntax binary
                    when TryEvaluateEnumIntegral(binary.Left, out var left) &&
                         TryEvaluateEnumIntegral(binary.Right, out var right):
                    value = binary.OperatorToken.Kind switch
                    {
                        SyntaxKind.PlusToken => checked(left + right),
                        SyntaxKind.MinusToken => checked(left - right),
                        SyntaxKind.StarToken => checked(left * right),
                        SyntaxKind.SlashToken when right != 0 => checked(left / right),
                        SyntaxKind.ModKeyword when right != 0 => left % right,
                        _ => throw new InvalidOperationException()
                    };
                    return true;
                case CallExpressionSyntax call when call.Identifier.Kind == SyntaxKind.AbsKeyword &&
                    call.Arguments.Count == 1 && TryEvaluateEnumIntegral(call.Arguments[0].Expression, out var absValue):
                    value = absValue == long.MinValue ? throw new OverflowException() : Math.Abs(absValue);
                    return true;
                case CallExpressionSyntax call when call.Identifier.Kind is SyntaxKind.MinKeyword or SyntaxKind.MaxKeyword &&
                    call.Arguments.Count == 2 && TryEvaluateEnumIntegral(call.Arguments[0].Expression, out var first) &&
                    TryEvaluateEnumIntegral(call.Arguments[1].Expression, out var second):
                    value = call.Identifier.Kind == SyntaxKind.MinKeyword ? Math.Min(first, second) : Math.Max(first, second);
                    return true;
                case CallExpressionSyntax call when call.Identifier.Kind == SyntaxKind.RgbKeyword &&
                    call.Arguments.Count == 3 && TryEvaluateEnumIntegral(call.Arguments[0].Expression, out var red) &&
                    TryEvaluateEnumIntegral(call.Arguments[1].Expression, out var green) &&
                    TryEvaluateEnumIntegral(call.Arguments[2].Expression, out var blue):
                    value = (red & 255) | ((green & 255) << 8) | ((blue & 255) << 16);
                    return true;
            }
        }
        catch (Exception exception) when (exception is OverflowException or DivideByZeroException or InvalidOperationException)
        {
        }
        value = 0;
        return false;
    }

    private bool TryResolveCheckedNumberConstant(string name, out long value)
    {
        if (_checkedNumberConstantStates.TryGetValue(name, out var state))
        {
            if (state == ConstantResolutionState.Resolved)
            {
                value = _checkedNumberConstants[name];
                return true;
            }
            value = 0;
            return false;
        }
        if (!_constantDeclarations.TryGetValue(name, out var declaration))
        {
            value = 0;
            return false;
        }

        _checkedNumberConstantStates[name] = ConstantResolutionState.Resolving;
        if (!TryEvaluateEnumIntegral(declaration.Statement.Expression, out value))
        {
            _checkedNumberConstantStates[name] = ConstantResolutionState.Failed;
            return false;
        }
        _checkedNumberConstants[name] = value;
        _checkedNumberConstantStates[name] = ConstantResolutionState.Resolved;
        return true;
    }

    private void BindRecordTypes()
    {
        foreach (var record in _types.Values.OrderBy(type => type.SourceOrdinal).ThenBy(type => type.DeclarationSpan.Start))
        {
            SetCurrentSource(record.Source!);
            if (record.Declaration.Fields.Count == 0)
                Report("SML3402", record.Declaration.Identifier.Span,
                    $"Type '{DisplaySourceText(_currentSource, record.Declaration.Identifier)}' must declare at least one field.");
            for (var index = 0; index < record.Declaration.Fields.Count; index++)
            {
                var declaration = record.Declaration.Fields[index];
                var field = new RecordFieldSymbol(declaration.Identifier.Text, declaration.TypeToken,
                    _currentSource, declaration.Identifier.Span, index);
                if (!record.AddField(field))
                {
                    Report("SML3402", declaration.Identifier.Span,
                        $"Field '{DisplaySourceText(_currentSource, declaration.Identifier)}' is already declared in Type '{record.Name}'.");
                    continue;
                }
                field.Type = ResolveType(declaration.TypeToken, SmileType.Error);
            }
        }

        var states = new Dictionary<RecordTypeSymbol, int>();
        foreach (var record in _types.Values.OrderBy(type => type.SourceOrdinal).ThenBy(type => type.DeclarationSpan.Start))
            LayoutRecord(record, states, new List<RecordTypeSymbol>());
    }

    private void LayoutRecord(RecordTypeSymbol record, Dictionary<RecordTypeSymbol, int> states,
        List<RecordTypeSymbol> stack)
    {
        if (states.TryGetValue(record, out var state))
        {
            if (state == 2)
                return;
            if (state == 1)
            {
                var start = stack.IndexOf(record);
                var cycle = stack.Skip(Math.Max(start, 0)).Concat(new[] { record }).Select(type => type.Name);
                _diagnostics.Report(record.Source!, "SML3404", record.Declaration.Identifier.Span,
                    $"Recursive record layout is not allowed: {string.Join(" -> ", cycle)}.");
            }
            return;
        }

        states[record] = 1;
        stack.Add(record);
        long offset = 0;
        var containsText = false;
        var containsImage = false;
        foreach (var field in record.Fields)
        {
            if (field.Type is RecordTypeSymbol nested)
                LayoutRecord(nested, states, stack);
            var aligned = Align(offset, field.Type.Alignment);
            if (aligned > int.MaxValue - Math.Max(8, field.Type.Size))
            {
                _diagnostics.Report(field.Source, "SML3411", field.DeclarationSpan,
                    $"Record layout for Type '{record.Name}' exceeds the supported size.");
                aligned = 0;
            }
            field.Offset = (int)aligned;
            offset = field.Offset + Math.Max(8, field.Type.Size);
            containsText |= field.Type.ContainsOwnedText;
            containsImage |= field.Type.ContainsOwnedImage;
        }
        stack.RemoveAt(stack.Count - 1);
        states[record] = 2;
        record.Alignment = 8;
        record.Size = (int)Math.Min(int.MaxValue & ~7, Align(Math.Max(offset, 8), record.Alignment));
        record.ContainsOwnedText = containsText;
        record.ContainsOwnedImage = containsImage;
    }

    private static int Align(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
    private static long Align(long value, int alignment) => (value + alignment - 1) / alignment * alignment;

    private static string DisplaySourceText(SourceText source, SyntaxToken token) =>
        token.Span.Start >= 0 && token.Span.End <= source.Text.Length
            ? source.Text.Substring(token.Span.Start, token.Span.Length)
            : token.Text;

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
                        candidates.Add(new ProjectDeclarationCandidate(dim.Identifier,
                            dim.IsArray ? ProjectDeclarationKind.Array : ProjectDeclarationKind.Variable,
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
                existing.Kind is ProjectDeclarationKind.Constant or ProjectDeclarationKind.Variable or ProjectDeclarationKind.Array)
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
        void Add(SyntaxToken identifier)
        {
            if (identifier.Text.StartsWith("__smile_module_", StringComparison.Ordinal))
                return;
            candidates.Add(new ProjectDeclarationCandidate(identifier, ProjectDeclarationKind.ImplicitGlobal,
                source, sourceOrdinal));
        }

        switch (statement)
        {
            case AssignmentStatementSyntax { Target.Location: NameExpressionSyntax name }:
                Add(name.Identifier);
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
            case WithStatementSyntax withStatement:
                foreach (var child in withStatement.Statements)
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
            var parameters = new List<ParameterSymbol>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sawOptional = false;
            foreach (var parameter in declaration.Parameters)
            {
                var parameterName = parameter.Identifier.Text;
                if (!names.Add(parameterName))
                {
                    Report("SML3306", parameter.Identifier.Span, $"Parameter '{parameterName}' is already declared.");
                    continue;
                }
                if (sawOptional && !parameter.IsOptional)
                    Report("SML3430", parameter.Identifier.Span,
                        "Required parameters must precede Optional parameters.");
                sawOptional |= parameter.IsOptional;
                if (parameter.IsOptional)
                {
                    if (parameter.ModeKeyword?.Kind == SyntaxKind.ByRefKeyword)
                        Report("SML3430", parameter.ModeKeyword.Span,
                            $"Optional parameter '{parameterName}' must be ByVal.");
                    if (parameter.TypeToken == null)
                        Report("SML3430", parameter.Identifier.Span,
                            $"Optional parameter '{parameterName}' requires an explicit As Type.");
                    if (parameter.DefaultValue == null || parameter.EqualsToken == null)
                        Report("SML3430", parameter.Identifier.Span,
                            $"Optional parameter '{parameterName}' requires a default value.");
                }
                else if (parameter.DefaultValue != null || parameter.EqualsToken != null)
                {
                    Report("SML3430", parameter.EqualsToken?.Span ?? parameter.Identifier.Span,
                        $"Parameter '{parameterName}' must be Optional to declare a default value.");
                }
                var parameterType = ResolveType(parameter.TypeToken, SmileType.Number);
                var mode = parameter.ModeKeyword?.Kind == SyntaxKind.ByRefKeyword
                    ? ParameterPassingMode.ByRef : ParameterPassingMode.ByVal;
                parameters.Add(new ParameterSymbol(parameter, parameterType, _currentSource,
                    _currentSourceOrdinal, name, mode, parameter.TypeToken != null));
            }
            var hasDeclaredReturnType = declaration.ReturnTypeToken != null;
            var returnType = declaration.IsFunction
                ? ResolveType(declaration.ReturnTypeToken, hasDeclaredReturnType ? SmileType.Error : SmileType.Number)
                : SmileType.Error;
            if (!declaration.IsFunction && declaration.ReturnTypeToken != null)
                Report("SML3310", declaration.ReturnTypeToken.Span, "Only a Function may declare a return type.");
            _routines[name] = new RoutineSymbol(declaration, parameters, returnType, hasDeclaredReturnType,
                _currentSource, _currentSourceOrdinal);
        }
    }

    private void InventoryConstantDeclarations()
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
    }

    private void CollectGlobalDeclarations()
    {
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
                        DeclareTopLevelVariable(dim);
                        break;
                    default:
                        if (tree.IsStartup && !tree.OptionExplicit)
                            CollectImplicitGlobals(statement);
                        break;
                }
            }
        }
    }

    private void BindOptionalParameterDefaults()
    {
        foreach (var routine in _routines.Values
                     .OrderBy(routine => routine.SourceOrdinal)
                     .ThenBy(routine => routine.Declaration.Span.Start))
        {
            SetCurrentSource(routine.Source);
            foreach (var parameter in routine.Parameters.Where(parameter => parameter.IsOptional))
            {
                var expression = parameter.Declaration.DefaultValue;
                if (expression == null || parameter.ParameterMode == ParameterPassingMode.ByRef ||
                    !parameter.HasDeclaredType || parameter.Type == SmileType.Error)
                    continue;

                if (parameter.Type != SmileType.Number && parameter.Type != SmileType.Boolean &&
                    parameter.Type != SmileType.Text && !parameter.Type.IsEnum)
                {
                    Report("SML3431", expression.Span,
                        $"Optional parameter '{parameter.Name}' has unsupported default type {TypeName(parameter.Type)}.");
                    continue;
                }

                if (!TryEvaluateOptionalDefault(expression, out var value, out var type, out var enumMember) ||
                    !ReferenceEquals(type, parameter.Type) ||
                    (parameter.Type.IsEnum && (enumMember == null ||
                                               !ReferenceEquals(enumMember.ContainingType, parameter.Type))))
                {
                    Report("SML3431", expression.Span,
                        $"Default for Optional parameter '{parameter.Name}' must be a compile-time {TypeName(parameter.Type)} literal, Const, or Enum member of the exact declared type.");
                    continue;
                }

                parameter.BindDefault(value, enumMember);
                _expressionTypes[expression] = type;
            }
        }
    }

    private bool TryEvaluateOptionalDefault(ExpressionSyntax expression, out object value, out SmileType type,
        out EnumMemberSymbol? enumMember)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax:
            case NameExpressionSyntax:
            case FieldAccessExpressionSyntax:
                return TryEvaluateConstant(expression, out value, out type, out enumMember);
            case ParenthesizedExpressionSyntax parenthesized:
                return TryEvaluateOptionalDefault(parenthesized.Expression, out value, out type, out enumMember);
            case UnaryExpressionSyntax { OperatorToken.Kind: SyntaxKind.MinusToken,
                Operand: LiteralExpressionSyntax { Value: long } }:
                return TryEvaluateConstant(expression, out value, out type, out enumMember);
            default:
                value = 0L;
                type = SmileType.Error;
                enumMember = null;
                return false;
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
        var previousSource = _currentSource;
        var previousSourceOrdinal = _currentSourceOrdinal;
        SetCurrentSource(constant.Source);
        bool resolved;
        object value;
        SmileType type;
        EnumMemberSymbol? enumMember;
        try
        {
            resolved = TryEvaluateConstant(constant.Statement.Expression, out value, out type, out enumMember);
        }
        finally
        {
            _currentSource = previousSource;
            _currentSourceOrdinal = previousSourceOrdinal;
        }
        _constantResolutionStack.RemoveAt(_constantResolutionStack.Count - 1);

        if (_constantStates.TryGetValue(name, out state) && state == ConstantResolutionState.Failed)
            return false;
        if (!resolved)
        {
            _constantStates[name] = ConstantResolutionState.Failed;
            _diagnostics.Report(constant.Source, "SML3013", constant.Statement.Expression.Span,
                "Const initializer must be a compile-time scalar expression.");
            return false;
        }

        _symbols[name] = new VariableSymbol(constant.Statement.Identifier.Text, type, Array.Empty<int>(),
            constant.Source, constant.SourceOrdinal, constant.Statement.Identifier.Span,
            isConstant: true, constantValue: value, constantEnumMember: enumMember);
        _expressionTypes[constant.Statement.Expression] = type;
        _constantStates[name] = ConstantResolutionState.Resolved;
        return true;
    }

    private void DeclareTopLevelVariable(DimStatementSyntax dim)
    {
        if (_symbols.ContainsKey(dim.Identifier.Text))
        {
            Report("SML3005", dim.Identifier.Span, $"'{dim.Identifier.Text}' is already declared in the compilation.");
            return;
        }
        var type = ResolveType(dim.TypeToken, SmileType.Number);
        if (type == SmileType.Error)
            return;
        if (!dim.IsArray && dim.TypeToken == null)
        {
            Report("SML3302", dim.Identifier.Span, $"Scalar Dim '{dim.Identifier.Text}' requires As Type.");
            return;
        }
        IReadOnlyList<int> dimensions = Array.Empty<int>();
        if (dim.IsArray && !TryGetArrayDimensions(dim, out dimensions))
            return;
        if (!ValidateRecordArrayStorage(dim, type, dimensions))
            return;
        _symbols[dim.Identifier.Text] = new VariableSymbol(dim.Identifier.Text, type, dimensions,
            _currentSource, _currentSourceOrdinal, dim.Identifier.Span);
    }

    private void CollectImplicitGlobals(StatementSyntax statement)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax { Target.Location: NameExpressionSyntax name } assignment:
                DeclareImplicitGlobal(name.Identifier, InferImplicitGlobalType(assignment.Expression));
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
            case WithStatementSyntax withStatement:
                foreach (var child in withStatement.Statements)
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

    private void RefreshImplicitGlobalTypes()
    {
        foreach (var statement in EnumerateStatements(_startupTree.Root.Statements))
        {
            if (statement is AssignmentStatementSyntax { Target.Location: NameExpressionSyntax name } assignment &&
                _implicitGlobals.Contains(name.Identifier.Text) &&
                _symbols.TryGetValue(name.Identifier.Text, out var symbol))
                symbol.Type = InferImplicitGlobalType(assignment.Expression);
        }
    }

    private void DeclareImplicitGlobal(SyntaxToken identifier, SmileType type)
    {
        if (!_acceptedProjectDeclarations.Contains(identifier))
            return;
        if (_symbols.ContainsKey(identifier.Text) || type == SmileType.Error)
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
            case ArrayAccessExpressionSyntax array when _symbols.TryGetValue(array.Identifier.Text,
                out var arraySymbol):
                return arraySymbol.Type;
            case ArrayAccessExpressionSyntax:
                return SmileType.Number;
            case FieldAccessExpressionSyntax field:
                if (field.Receiver is NameExpressionSyntax typeName &&
                    TryResolveEnumType(typeName.Identifier.Text, out var enumType) &&
                    enumType.TryGetMember(field.Field.Text, out _))
                    return enumType;
                var receiverType = InferImplicitGlobalType(field.Receiver);
                return receiverType is RecordTypeSymbol record && record.TryGetField(field.Field.Text, out var fieldSymbol)
                    ? fieldSymbol.Type : SmileType.Number;
            case ParenthesizedExpressionSyntax parenthesized:
                return InferImplicitGlobalType(parenthesized.Expression);
            case UnaryExpressionSyntax unary:
                return unary.OperatorToken.Kind == SyntaxKind.NotKeyword ? SmileType.Boolean : SmileType.Number;
            case BinaryExpressionSyntax binary when binary.OperatorToken.Kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or
                SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken or
                SyntaxKind.AndKeyword or SyntaxKind.OrKeyword:
                return SmileType.Boolean;
            case BinaryExpressionSyntax binary when binary.OperatorToken.Kind == SyntaxKind.PlusToken &&
                InferImplicitGlobalType(binary.Left) == SmileType.Text && InferImplicitGlobalType(binary.Right) == SmileType.Text:
                return SmileType.Text;
            case CallExpressionSyntax call when SyntaxFacts.IsBuiltInFunction(call.Identifier.Kind):
                return call.Identifier.Kind == SyntaxKind.TextSliceKeyword ? SmileType.Text
                    : call.Identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword or SyntaxKind.ImageLoadedKeyword
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
            if (statement is ConstStatementSyntax or DimStatementSyntax or TypeDeclarationSyntax or EnumDeclarationSyntax)
            {
                AnalyzeStatement(statement, topLevel: true);
                continue;
            }

            var message = statement switch
            {
                GameWindowStatementSyntax => "Game Window is allowed only in the selected startup source.",
                EndProgramStatementSyntax => "End Program is allowed only in the selected startup source.",
                _ => "Executable top-level statements are not allowed in a support source; move the statement to the selected startup source or into a routine."
            };
            Report("SML3028", statement.Span, message);
        }
    }

    private void SetCurrentSource(SourceText source)
    {
        _currentSource = source;
        _currentSourceOrdinal = _sourceOrdinals[source];
        _optionExplicit = _syntaxTrees[_currentSourceOrdinal].OptionExplicit;
    }

    private void Report(string code, TextSpan span, string message) =>
        _diagnostics.Report(_currentSource, code, span, message);

    private void InferLegacyRoutineReturnTypes()
    {
        for (var pass = 0; pass < Math.Max(1, _routines.Count); pass++)
        {
            var changed = false;
            foreach (var routine in _routines.Values.Where(item => item.IsFunction && !item.HasDeclaredReturnType))
            {
                SetCurrentSource(routine.Source);
                var localTypes = CollectDeclaredLocalTypes(routine);
                var returns = InferLegacyReturns(routine.Declaration.Statements, routine, localTypes,
                    Array.Empty<SmileType>()).ToArray();
                var inferred = returns.Select(item => item.Type)
                    .Where(type => type != SmileType.Error).Distinct().ToArray();
                var next = inferred.Length == 0 ? SmileType.Number : inferred[0];
                if (routine.ReturnType != next)
                {
                    routine.ReturnType = next;
                    changed = true;
                }
            }
            if (!changed)
                break;
        }

        foreach (var routine in _routines.Values.Where(item => item.IsFunction && !item.HasDeclaredReturnType))
        {
            SetCurrentSource(routine.Source);
            var localTypes = CollectDeclaredLocalTypes(routine);
            var typedReturns = InferLegacyReturns(routine.Declaration.Statements, routine, localTypes,
                    Array.Empty<SmileType>())
                .Where(item => item.Type != SmileType.Error).ToArray();
            var distinct = typedReturns.Select(item => item.Type).Distinct().ToArray();
            if (distinct.Length > 1)
                Report("SML3309", typedReturns.First(item => item.Type != distinct[0]).Return.Expression!.Span,
                    $"Function '{routine.Name}' has inconsistent inferred return types: {string.Join(" and ", distinct.Select(TypeName))}.");
        }
    }

    private Dictionary<string, SmileType> CollectDeclaredLocalTypes(RoutineSymbol routine)
    {
        var result = routine.Parameters.ToDictionary(item => item.Name, item => item.Type,
            StringComparer.OrdinalIgnoreCase);
        foreach (var dim in EnumerateStatements(routine.Declaration.Statements).OfType<DimStatementSyntax>())
            if (!result.ContainsKey(dim.Identifier.Text))
                result[dim.Identifier.Text] = ResolveType(dim.TypeToken, SmileType.Number);
        return result;
    }

    private SmileType InferLegacyExpressionType(ExpressionSyntax expression, RoutineSymbol routine,
        IReadOnlyDictionary<string, SmileType> locals, IReadOnlyList<SmileType>? withTypes = null)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                return literal.Value is bool ? SmileType.Boolean : literal.Value is string ? SmileType.Text : SmileType.Number;
            case NameExpressionSyntax name:
                if (locals.TryGetValue(name.Identifier.Text, out var localType)) return localType;
                if (_symbols.TryGetValue(name.Identifier.Text, out var symbol)) return symbol.Type;
                return SmileType.Error;
            case ArrayAccessExpressionSyntax array:
                if (locals.TryGetValue(array.Identifier.Text, out var elementType)) return elementType;
                if (_symbols.TryGetValue(array.Identifier.Text, out var arraySymbol)) return arraySymbol.Type;
                return SmileType.Error;
            case FieldAccessExpressionSyntax field:
                if (field.Receiver is NameExpressionSyntax enumName &&
                    TryResolveEnumType(enumName.Identifier.Text, out var enumType) &&
                    enumType.TryGetMember(field.Field.Text, out _))
                    return enumType;
                var receiverType = InferLegacyExpressionType(field.Receiver, routine, locals, withTypes);
                return receiverType is RecordTypeSymbol receiverRecord &&
                       receiverRecord.TryGetField(field.Field.Text, out var fieldSymbol)
                    ? fieldSymbol.Type : SmileType.Error;
            case LeadingMemberAccessExpressionSyntax leading:
                if (withTypes == null || withTypes.Count == 0 ||
                    withTypes[withTypes.Count - 1] is not RecordTypeSymbol withRecord ||
                    !withRecord.TryGetField(leading.Member.Text, out var withField))
                    return SmileType.Error;
                return withField.Type;
            case ParenthesizedExpressionSyntax parenthesized:
                return InferLegacyExpressionType(parenthesized.Expression, routine, locals, withTypes);
            case UnaryExpressionSyntax unary:
                return unary.OperatorToken.Kind == SyntaxKind.NotKeyword ? SmileType.Boolean : SmileType.Number;
            case BinaryExpressionSyntax binary:
                if (binary.OperatorToken.Kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or SyntaxKind.LessToken or
                    SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken or
                    SyntaxKind.AndKeyword or SyntaxKind.OrKeyword) return SmileType.Boolean;
                if (binary.OperatorToken.Kind == SyntaxKind.PlusToken &&
                    InferLegacyExpressionType(binary.Left, routine, locals, withTypes) == SmileType.Text &&
                    InferLegacyExpressionType(binary.Right, routine, locals, withTypes) == SmileType.Text) return SmileType.Text;
                return SmileType.Number;
            case CallExpressionSyntax call when call.Identifier.Kind == SyntaxKind.TextSliceKeyword:
                return SmileType.Text;
            case CallExpressionSyntax call when call.Identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword or SyntaxKind.ImageLoadedKeyword:
                return SmileType.Boolean;
            case CallExpressionSyntax call when _routines.TryGetValue(call.Identifier.Text, out var called):
                return called.ReturnType;
            default:
                return SmileType.Number;
        }
    }

    private IEnumerable<(ReturnStatementSyntax Return, SmileType Type)> InferLegacyReturns(
        IReadOnlyList<StatementSyntax> statements, RoutineSymbol routine,
        IReadOnlyDictionary<string, SmileType> locals, IReadOnlyList<SmileType> withTypes)
    {
        foreach (var statement in statements)
        {
            if (statement is ReturnStatementSyntax { Expression: not null } returnStatement)
            {
                yield return (returnStatement,
                    InferLegacyExpressionType(returnStatement.Expression, routine, locals, withTypes));
                continue;
            }
            if (statement is WithStatementSyntax withStatement)
            {
                var targetType = InferLegacyExpressionType(withStatement.Target, routine, locals, withTypes);
                var nestedTypes = withTypes.Concat(new[] { targetType }).ToArray();
                foreach (var item in InferLegacyReturns(withStatement.Statements, routine, locals, nestedTypes))
                    yield return item;
                continue;
            }
            if (statement is IfStatementSyntax conditional)
            {
                foreach (var clause in conditional.Clauses)
                    foreach (var item in InferLegacyReturns(clause.Statements, routine, locals, withTypes))
                        yield return item;
                foreach (var item in InferLegacyReturns(conditional.ElseStatements, routine, locals, withTypes))
                    yield return item;
                continue;
            }
            if (statement is ForStatementSyntax forStatement)
            {
                foreach (var item in InferLegacyReturns(forStatement.Statements, routine, locals, withTypes))
                    yield return item;
                continue;
            }
            if (statement is DoStatementSyntax doStatement)
            {
                foreach (var item in InferLegacyReturns(doStatement.Statements, routine, locals, withTypes))
                    yield return item;
                continue;
            }
            if (statement is ClipRectangleStatementSyntax clip)
            {
                foreach (var item in InferLegacyReturns(clip.Statements, routine, locals, withTypes))
                    yield return item;
                continue;
            }
            if (statement is SelectStatementSyntax select)
                foreach (var clause in select.Cases)
                    foreach (var item in InferLegacyReturns(clause.Statements, routine, locals, withTypes))
                        yield return item;
        }
    }

    private static IEnumerable<StatementSyntax> EnumerateStatements(IEnumerable<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            yield return statement;
            if (statement is IfStatementSyntax conditional)
            {
                foreach (var clause in conditional.Clauses)
                    foreach (var child in EnumerateStatements(clause.Statements)) yield return child;
                foreach (var child in EnumerateStatements(conditional.ElseStatements)) yield return child;
            }
            else if (statement is ForStatementSyntax forStatement)
                foreach (var child in EnumerateStatements(forStatement.Statements)) yield return child;
            else if (statement is WithStatementSyntax withStatement)
                foreach (var child in EnumerateStatements(withStatement.Statements)) yield return child;
            else if (statement is DoStatementSyntax doStatement)
                foreach (var child in EnumerateStatements(doStatement.Statements)) yield return child;
            else if (statement is ClipRectangleStatementSyntax clip)
                foreach (var child in EnumerateStatements(clip.Statements)) yield return child;
            else if (statement is SelectStatementSyntax select)
                foreach (var clause in select.Cases)
                    foreach (var child in EnumerateStatements(clause.Statements)) yield return child;
        }
    }

    private void CollectFirstDeclarations(IReadOnlyList<StatementSyntax> statements, Dictionary<string, int> declarations, bool skipRoutines)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case RoutineDeclarationSyntax when skipRoutines:
                    break;
                case AssignmentStatementSyntax { Target.Location: NameExpressionSyntax name }:
                    RecordFirst(declarations, name.Identifier);
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
                case WithStatementSyntax withStatement:
                    CollectFirstDeclarations(withStatement.Statements, declarations, skipRoutines);
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
            case TypeDeclarationSyntax type:
                if (!topLevel || _currentRoutine != null)
                    Report("SML3403", type.TypeKeyword.Span, "Type declarations must be project-global or direct module declarations.");
                break;
            case EnumDeclarationSyntax enumDeclaration:
                if (!topLevel || _currentRoutine != null)
                    Report("SML3420", enumDeclaration.EnumKeyword.Span,
                        "Enum declarations must be project-global or direct module declarations.");
                break;
            case AssignmentStatementSyntax assignment: AnalyzeAssignment(assignment); break;
            case DimStatementSyntax dim: AnalyzeDim(dim, topLevel); break;
            case PrintStatementSyntax print: AnalyzePrint(print); break;
            case GetKeyStatementSyntax getKey: EnsureNumberTarget(getKey.Identifier, "Get Key"); break;
            case ClearScreenStatementSyntax: break;
            case WaitStatementSyntax wait: RequireType(wait.Duration, SmileType.Number, "SML3008", "Wait duration must be Number."); break;
            case RandomStatementSyntax random:
                RequireType(random.Minimum, SmileType.Number, "SML3008", "Random minimum must be Number.");
                RequireType(random.Maximum, SmileType.Number, "SML3008", "Random maximum must be Number.");
                EnsureNumberTarget(random.Identifier, "Random");
                break;
            case IfStatementSyntax ifStatement:
                foreach (var clause in ifStatement.Clauses)
                {
                    RequireType(clause.Condition, SmileType.Boolean, "SML3004", "If condition must be Boolean.");
                    AnalyzeStatements(clause.Statements, false);
                }
                AnalyzeStatements(ifStatement.ElseStatements, false);
                break;
            case WithStatementSyntax withStatement:
                AnalyzeWith(withStatement);
                break;
            case ForStatementSyntax forStatement:
                RequireType(forStatement.LowerBound, SmileType.Number, "SML3008", "For lower bound must be Number.");
                RequireType(forStatement.UpperBound, SmileType.Number, "SML3008", "For upper bound must be Number.");
                EnsureNumberTarget(forStatement.Identifier, "For");
                _forDepth++;
                AnalyzeStatements(forStatement.Statements, false);
                _forDepth--;
                break;
            case DoStatementSyntax doStatement:
                _doDepth++;
                AnalyzeStatements(doStatement.Statements, false);
                _doDepth--;
                if (doStatement.UntilCondition != null)
                    RequireType(doStatement.UntilCondition, SmileType.Boolean, "SML3004", "Loop Until condition must be Boolean.");
                break;
            case CallStatementSyntax call: AnalyzeCall(call, call.Identifier, call.Arguments, requireFunction: false); break;
            case LeadingMemberCallStatementSyntax call: AnalyzeLeadingMemberCall(call); break;
            case ReturnStatementSyntax returnStatement: AnalyzeReturn(returnStatement); break;
            case SelectStatementSyntax select: AnalyzeSelect(select); break;
            case ExitStatementSyntax exit: AnalyzeExit(exit); break;
            case EndProgramStatementSyntax: break;
            case GameWindowStatementSyntax gameWindow: AnalyzeGameWindow(gameWindow, topLevel); break;
            case ClearColorStatementSyntax clearColor:
                RequireGameWindow(clearColor.Span, "Clear color");
                RequireType(clearColor.Color, SmileType.Number, "SML3023", "Clear color must be Number.");
                break;
            case GraphicsStatementSyntax graphics:
                RequireGameWindow(graphics.Span, "drawing statement");
                foreach (var argument in graphics.Arguments)
                    RequireType(argument, SmileType.Number, "SML3023", "Drawing arguments must be Number values.");
                if (graphics.Operation == GraphicsOperation.DrawText && graphics.TextExpression != null)
                    RequireType(graphics.TextExpression, SmileType.Text, "SML3304", "Draw Text requires a Text expression.");
                break;
            case DrawImageStatementSyntax image:
                RequireGameWindow(image.Span, "Draw Image");
                RequireType(image.Image, SmileType.Image, "SML3501", "Draw Image requires an Image expression.");
                foreach (var argument in ImageArguments(image))
                    RequireType(argument, SmileType.Number, "SML3503", "Draw Image rectangle, opacity, and anchor values must be Number.");
                break;
            case ImageLoadStatementSyntax image:
                if (_currentRoutine != null)
                    RequireGameWindow(image.Span, image.IsUnload ? "Unload Image" : "Load Image");
                var targetType = ResolveWritableTargetType(image.Target);
                if (targetType != SmileType.Error && targetType != SmileType.Image)
                    Report("SML3500", image.Target.Span, $"{(image.IsUnload ? "Unload" : "Load")} Image target must be Image.");
                if (!image.IsUnload && image.Path != null)
                    RequireType(image.Path, SmileType.Text, "SML3500", "Load Image path must be Text.");
                break;
            case ClipRectangleStatementSyntax clip:
                RequireGameWindow(clip.Span, "Clip Rectangle");
                foreach (var argument in clip.Arguments)
                    RequireType(argument, SmileType.Number, "SML3504", "Clip Rectangle arguments must be Number.");
                AnalyzeStatements(clip.Statements, false);
                break;
            case ShowScreenStatementSyntax show:
                RequireGameWindow(show.Span, "Show Screen");
                break;
            case SoundStatementSyntax sound:
                RequireGameWindow(sound.Span, sound.IsStop ? "Stop Sound" : "Play Sound");
                if (!sound.IsStop && string.IsNullOrWhiteSpace(sound.Path?.Value as string))
                    Report("SML3024", sound.Span, "Play Sound requires a non-empty WAV path literal.");
                if (sound.Channel != null)
                {
                    RequireType(sound.Channel, SmileType.Number, "SML3507", "Sound channel must be Number.");
                    if (TryEvaluateConstant(sound.Channel, out var channel, out var channelType) &&
                        channelType == SmileType.Number && channel is long channelNumber && (channelNumber < 0 || channelNumber >= 16))
                        Report("SML3507", sound.Channel.Span, "Sound channel must be from 0 through 15.");
                }
                break;
            case MusicStatementSyntax music:
                RequireGameWindow(music.Span, music.Operation switch
                {
                    MusicOperation.Play => "Play Music",
                    MusicOperation.Pause => "Pause Music",
                    MusicOperation.Resume => "Resume Music",
                    MusicOperation.Stop => "Stop Music",
                    _ => "Music Volume"
                });
                if (music.Operation == MusicOperation.Play && string.IsNullOrWhiteSpace(music.Path?.Value as string))
                    Report("SML3026", music.Span, "Play Music requires a non-empty music path literal.");
                if (music.Operation == MusicOperation.SetVolume && music.Volume != null)
                    RequireType(music.Volume, SmileType.Number, "SML3026", "Music Volume requires a Number value.");
                break;
            case LoadStatementSyntax load:
                RequireType(load.DefaultValue, SmileType.Number, "SML3025", "Load Default must be Number.");
                EnsureNumberTarget(load.Identifier, "Load");
                ValidateStorageKey(load.Key);
                break;
            case TextFileLoadStatementSyntax textFileLoad:
                AnalyzeTextFileLoad(textFileLoad);
                break;
            case DataLoadStatementSyntax loadData:
                AnalyzeDataLoad(loadData);
                break;
            case DataSaveStatementSyntax saveData:
                AnalyzeDataSave(saveData);
                break;
            case SaveStatementSyntax save:
                if (!TryResolve(save.Identifier.Text, save.Identifier, out var saved) || saved.IsArray || saved.Type != SmileType.Number)
                    Report("SML3025", save.Identifier.Span, "Save value must be a Number variable or constant.");
                ValidateStorageKey(save.Key);
                break;
        }
    }

    private static IEnumerable<ExpressionSyntax> ImageArguments(DrawImageStatementSyntax image)
    {
        foreach (var argument in new ExpressionSyntax?[] { image.SourceX, image.SourceY, image.SourceWidth,
                     image.SourceHeight, image.DestinationX, image.DestinationY, image.DestinationWidth,
                     image.DestinationHeight, image.Opacity, image.AnchorX, image.AnchorY })
            if (argument != null)
                yield return argument;
    }

    private SmileType ResolveWritableTargetType(AssignmentTargetSyntax target)
    {
        var type = AnalyzeExpression(target.Location);
        if (type != SmileType.Error && !IsWritableLocation(target.Location, type))
            Report("SML3305", target.Span, "Target must be a writable location.");
        return type;
    }

    private void AnalyzeWith(WithStatementSyntax statement)
    {
        var targetType = AnalyzeExpression(statement.Target);
        var isWritable = targetType != SmileType.Error && IsWritableLocation(statement.Target, targetType);
        if (!isWritable && targetType is RecordTypeSymbol)
            Report("SML3412", statement.Target.Span,
                "With target must be a stable writable record location.");
        if (targetType != SmileType.Error && targetType is not RecordTypeSymbol)
            Report("SML3415", statement.Target.Span,
                $"With target must have a record type; found {TypeName(targetType)}.");

        WithTargetBinding? binding = null;
        var bodySpan = TextSpan.FromBounds(statement.Target.Span.End, statement.EndKeyword.Span.Start);
        if (targetType is RecordTypeSymbol record)
        {
            binding = new WithTargetBinding(statement, record, WithStorageKind.ValueLocation,
                _withStack.Count, bodySpan);
            _withTargets[statement] = binding;
            if (!_withScopes.TryGetValue(_currentSource, out var scopes))
            {
                scopes = new List<WithTargetBinding>();
                _withScopes[_currentSource] = scopes;
            }
            scopes.Add(binding);
        }
        else
        {
            if (!_invalidWithScopes.TryGetValue(_currentSource, out var invalidScopes))
            {
                invalidScopes = new List<InvalidWithScope>();
                _invalidWithScopes[_currentSource] = invalidScopes;
            }
            invalidScopes.Add(new InvalidWithScope(_withStack.Count, bodySpan));
        }

        _withStack.Add(binding);
        AnalyzeStatements(statement.Statements, false);
        _withStack.RemoveAt(_withStack.Count - 1);
    }

    private void AnalyzeLeadingMemberCall(LeadingMemberCallStatementSyntax statement)
    {
        foreach (var argument in statement.Arguments)
        {
            if (argument.IsNamed)
                Report("SML3433", argument.Name!.Span,
                    "Named arguments are not available for record member calls.");
            AnalyzeExpression(argument.Expression);
        }
        if (_withStack.Count == 0)
        {
            Report("SML3413", statement.DotToken.Span,
                "Leading-dot member access is valid only inside With...End With.");
            return;
        }
        var receiver = _withStack[_withStack.Count - 1];
        if (receiver == null)
            return;
        var record = receiver.TargetType;
        Report("SML3414", statement.Member.Span,
            $"Type '{record.Name}' does not contain callable member '{statement.Member.Text}'; record methods are not declared by Type fields.");
    }

    private void AnalyzeDataLoad(DataLoadStatementSyntax statement)
    {
        RequireType(statement.Key, SmileType.Text, "SML3506", "Load Data key must be Text.");
        if (!TryResolveExisting(statement.Destination.Text, out var destination) || !destination.IsArray ||
            destination.ArrayRank != 1 || destination.Type != SmileType.Number)
            Report("SML3506", statement.Destination.Span,
                "Load Data destination must be a fixed one-dimensional Number array.");
        var countType = ResolveWritableTargetType(statement.CountTarget);
        if (countType != SmileType.Error && countType != SmileType.Number)
            Report("SML3506", statement.CountTarget.Span, "Load Data Count target must be Number.");
    }

    private void AnalyzeDataSave(DataSaveStatementSyntax statement)
    {
        if (!TryResolveExisting(statement.Source.Text, out var source) || !source.IsArray || source.ArrayRank != 1 ||
            source.Type != SmileType.Number)
            Report("SML3506", statement.Source.Span,
                "Save Data source must be a fixed one-dimensional Number array.");
        RequireType(statement.Count, SmileType.Number, "SML3506", "Save Data Count must be Number.");
        RequireType(statement.Key, SmileType.Text, "SML3506", "Save Data key must be Text.");
    }

    private void AnalyzeGameWindow(GameWindowStatementSyntax statement, bool topLevel)
    {
        _gameWindowCount++;
        if (!topLevel || _currentRoutine != null)
            Report("SML3022", statement.GameKeyword.Span, "Game Window must be a top-level statement.");
        if (_gameWindowCount > 1)
            Report("SML3022", statement.GameKeyword.Span, "Only one Game Window is allowed.");
        if (statement.Width == null || statement.Height == null)
            return;
        if (!TryEvaluateConstant(statement.Width, out var widthValue, out var widthType) ||
            widthType != SmileType.Number || widthValue is not long width || width <= 0)
            Report("SML3023", statement.Width.Span, "Game Window width must be a positive compile-time Number.");
        if (!TryEvaluateConstant(statement.Height, out var heightValue, out var heightType) ||
            heightType != SmileType.Number || heightValue is not long height || height <= 0)
            Report("SML3023", statement.Height.Span, "Game Window height must be a positive compile-time Number.");
    }

    private void RequireGameWindow(TextSpan span, string statementName)
    {
        if (_currentRoutine != null)
        {
            _currentRoutine.Capabilities |= RoutineCapability.RequiresGameWindow;
            return;
        }
        if (!_hasGameWindow)
            Report("SML3023", span, $"{statementName} requires a Game Window statement.");
    }

    private void RequireGameWindow(TextSpan span, string statementName, string diagnosticCode)
    {
        if (_currentRoutine != null)
        {
            _currentRoutine.Capabilities |= RoutineCapability.RequiresGameWindow;
            return;
        }
        if (!_hasGameWindow)
            Report(diagnosticCode, span, $"{statementName} requires a Game Window statement.");
    }

    private void ValidateStorageKey(SyntaxToken key)
    {
        if (string.IsNullOrWhiteSpace(key.Value as string))
            Report("SML3025", key.Span, "Storage key must be a non-empty text literal.");
    }

    private void AnalyzeTextFileLoad(TextFileLoadStatementSyntax statement)
    {
        RequireType(statement.Path, SmileType.Text, "SML3027", "Load Text File path must be Text.");
        if (TryEvaluateConstant(statement.Path, out var path, out var pathType) &&
            pathType == SmileType.Text && string.IsNullOrWhiteSpace(path as string))
            Report("SML3027", statement.Path.Span, "Load Text File requires a non-empty path.");

        if (!TryResolveExisting(statement.Destination.Text, out var destination))
        {
            Report("SML3027", statement.Destination.Span,
                $"Load Text File destination '{statement.Destination.Text}' must be a declared one-dimensional Number array.");
        }
        else if (!destination.IsArray || destination.ArrayRank != 1 || destination.Type != SmileType.Number)
        {
            Report("SML3027", statement.Destination.Span,
                $"Load Text File destination '{statement.Destination.Text}' must be a one-dimensional Number array.");
        }

        EnsureNumberTarget(statement.CountIdentifier, "Load Text File Count");
    }

    private void AnalyzeConstant(ConstStatementSyntax constant, bool topLevel)
    {
        if (!topLevel || _currentRoutine != null)
        {
            Report("SML3013", constant.ConstKeyword.Span, "Const declarations must be top-level.");
            return;
        }
        // Compilation-wide top-level constants were registered before any body is bound.
    }

    private void AnalyzeAssignment(AssignmentStatementSyntax assignment)
    {
        var valueType = AnalyzeExpression(assignment.Expression);
        if (assignment.Target.Location is NameExpressionSyntax nameExpression)
        {
            AnalyzeSimpleAssignment(nameExpression, valueType, assignment.Expression.Span);
            return;
        }

        var targetType = AnalyzeExpression(assignment.Target.Location);
        if (targetType != SmileType.Error && !IsWritableLocation(assignment.Target.Location, targetType))
            Report("SML3305", assignment.Target.Span, "Assignment target must be a writable location.");
        if (valueType != SmileType.Error && targetType != SmileType.Error && valueType != targetType)
        {
            var suffix = assignment.Target.Location switch
            {
                ArrayAccessExpressionSyntax array => $" array element '{array.Identifier.Text}'",
                FieldAccessExpressionSyntax field => $" field '{field.Field.Text}'",
                LeadingMemberAccessExpressionSyntax leading => $" field '{leading.Member.Text}'",
                _ => " location"
            };
            Report("SML3304", assignment.Expression.Span,
                $"Cannot assign {TypeName(valueType)} to {TypeName(targetType)}{suffix}.");
        }
    }

    private void AnalyzeSimpleAssignment(NameExpressionSyntax target, SmileType valueType, TextSpan valueSpan)
    {
        var name = target.Identifier.Text;
        if (_currentRoutine == null && _rejectedProjectDeclarations.Contains(target.Identifier))
            return;

        var futureLocal = _currentRoutine != null && !_currentRoutine.Locals.ContainsKey(name)
            ? FindLocalDeclaration(name) : null;
        if (futureLocal != null && futureLocal.Identifier.Position > target.Identifier.Position)
        {
            Report("SML3307", target.Identifier.Span, $"Local '{name}' is used before its Dim declaration.");
            return;
        }

        if (TryResolveExisting(name, out var existing))
        {
            _expressionTypes[target] = existing.Type;
            if (existing.IsConstant)
            {
                Report("SML3012", target.Identifier.Span, $"Constant '{name}' cannot be assigned.");
                return;
            }
            if (existing.IsArray)
            {
                Report("SML3009", target.Identifier.Span, $"Array '{name}' requires an index.");
                return;
            }
            if (valueType != SmileType.Error && existing.Type != valueType)
                Report("SML3304", valueSpan,
                    $"Cannot assign {TypeName(valueType)} to {TypeName(existing.Type)} variable '{name}'.");
            return;
        }

        if (_optionExplicit)
        {
            var later = FindLocalDeclaration(name);
            if (later != null && later.Identifier.Position > target.Identifier.Position)
            {
                Report("SML3307", target.Identifier.Span,
                    $"Local '{name}' is used before its Dim declaration.");
                return;
            }
            Report("SML3303", target.Identifier.Span,
                $"Variable '{name}' must be declared because Option Explicit is enabled for this source.");
            return;
        }
        if (valueType == SmileType.Error)
            return;
        DeclareVariable(name, valueType, Array.Empty<int>(), target.Identifier.Span);
        _expressionTypes[target] = valueType;
    }

    private SmileType BindField(SmileType receiverType, SyntaxToken fieldToken, TextSpan span,
        ExpressionSyntax? expression)
    {
        if (receiverType == SmileType.Error)
            return SmileType.Error;
        if (receiverType is not RecordTypeSymbol record)
        {
            Report("SML3406", span, $"Field access requires a record value; found {TypeName(receiverType)}.");
            return SmileType.Error;
        }
        if (!record.TryGetField(fieldToken.Text, out var field))
        {
            Report("SML3405", fieldToken.Span, $"Type '{record.Name}' does not contain field '{fieldToken.Text}'.");
            return SmileType.Error;
        }
        if (expression != null)
        {
            _fields[expression] = field;
            if (!_fieldUses.TryGetValue(_currentSource, out var uses))
            {
                uses = new Dictionary<int, RecordFieldSymbol>();
                _fieldUses[_currentSource] = uses;
            }
            uses[fieldToken.Position] = field;
        }
        return field.Type;
    }

    private bool TryBindEnumMember(FieldAccessExpressionSyntax expression, out SmileType type)
    {
        type = SmileType.Error;
        if (expression.Receiver is not NameExpressionSyntax typeName ||
            !TryResolveEnumType(typeName.Identifier.Text, out var enumType))
            return false;
        if (!enumType.TryGetMember(expression.Field.Text, out var member))
        {
            Report("SML3423", expression.Field.Span,
                $"Enum '{enumType.Name}' does not contain member '{expression.Field.Text}'.");
            return true;
        }

        RegisterEnumMemberUse(_currentSource, expression, member);
        type = enumType;
        return true;
    }

    private void RegisterEnumMemberUse(SourceText source, FieldAccessExpressionSyntax expression,
        EnumMemberSymbol member)
    {
        _enumMembers[expression] = member;
        if (!_enumMemberUses.TryGetValue(source, out var uses))
        {
            uses = new Dictionary<int, EnumMemberSymbol>();
            _enumMemberUses[source] = uses;
        }
        uses[expression.Field.Position] = member;
    }

    private bool TryResolveEnumMemberExpression(FieldAccessExpressionSyntax expression,
        out EnumMemberSymbol member)
    {
        if (_enumMembers.TryGetValue(expression, out member!))
            return true;
        return expression.Receiver is NameExpressionSyntax typeName &&
               TryResolveEnumType(typeName.Identifier.Text, out var enumType) &&
               enumType.TryGetMember(expression.Field.Text, out member!);
    }

    private void AnalyzeDim(DimStatementSyntax dim, bool topLevel)
    {
        if (topLevel && _currentRoutine == null)
            return;
        if (_currentRoutine == null)
            return;
        if (_currentRoutine.Locals.ContainsKey(dim.Identifier.Text))
        {
            Report("SML3306", dim.Identifier.Span, $"Local '{dim.Identifier.Text}' is already declared as a parameter or local.");
            return;
        }
        var type = ResolveType(dim.TypeToken, SmileType.Number);
        if (!dim.IsArray && dim.TypeToken == null)
        {
            Report("SML3302", dim.Identifier.Span, $"Scalar Dim '{dim.Identifier.Text}' requires As Type.");
            return;
        }
        IReadOnlyList<int> dimensions = Array.Empty<int>();
        if (dim.IsArray && !TryGetArrayDimensions(dim, out dimensions))
            return;
        if (!ValidateRecordArrayStorage(dim, type, dimensions))
            return;
        if (type != SmileType.Error)
            _currentRoutine.Locals[dim.Identifier.Text] = new VariableSymbol(dim.Identifier.Text, type, dimensions,
                _currentSource, _currentSourceOrdinal, dim.Identifier.Span, routineName: _currentRoutine.Name);
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
            if (!TryEvaluateConstant(sizeExpression, out var constantValue, out var type) ||
                type != SmileType.Number || constantValue is not long value || value <= 0 || value > int.MaxValue)
            {
                Report("SML3006", sizeExpression.Span, "Array dimension must be a positive compile-time Number expression.");
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

    private bool ValidateRecordArrayStorage(DimStatementSyntax dim, SmileType type,
        IReadOnlyList<int> dimensions)
    {
        if (!type.IsRecord || dimensions.Count == 0)
            return true;
        long count = 1;
        foreach (var dimension in dimensions)
            count *= dimension;
        if (count <= int.MaxValue / Math.Max(8, type.Size))
            return true;
        Report("SML3411", dim.Span, $"Record array '{dim.Identifier.Text}' exceeds the supported storage size.");
        return false;
    }

    private void AnalyzePrint(PrintStatementSyntax print)
    {
        foreach (var item in print.Items)
        {
            var type = AnalyzeExpression(item);
            if (type.IsRecord)
                Report("SML3407", item.Span, "Print does not support whole record values.");
            else if (type != SmileType.Error && type != SmileType.Text && type != SmileType.Number && type != SmileType.Boolean)
                Report("SML3011", item.Span, "Invalid Print item.");
        }
    }

    private void AnalyzeReturn(ReturnStatementSyntax statement)
    {
        if (_currentRoutine == null)
        {
            Report("SML3020", statement.ReturnKeyword.Span, "Return is only valid inside a Sub or Function.");
            return;
        }
        if (_currentRoutine.IsFunction)
        {
            if (statement.Expression == null)
            {
                Report("SML3020", statement.ReturnKeyword.Span, "Function Return requires a value.");
                return;
            }
            var type = AnalyzeExpression(statement.Expression);
            if (type != SmileType.Error && type != _currentRoutine.ReturnType)
                Report("SML3304", statement.Expression.Span, $"Function '{_currentRoutine.Name}' must return {TypeName(_currentRoutine.ReturnType)}.");
        }
        else if (statement.Expression != null)
        {
            AnalyzeExpression(statement.Expression);
            Report("SML3020", statement.Expression.Span, "Sub Return cannot include a value.");
        }
    }

    private void AnalyzeSelect(SelectStatementSyntax select)
    {
        var selectorType = AnalyzeExpression(select.Expression);
        if (selectorType.IsRecord)
            Report("SML3407", select.Expression.Span, "Select Case does not support whole record values.");
        else if (selectorType != SmileType.Number && selectorType != SmileType.Boolean &&
            selectorType != SmileType.Text && !selectorType.IsEnum && selectorType != SmileType.Error)
            Report("SML3304", select.Expression.Span,
                "Select Case expression must be Number, Boolean, Text, or Enum.");
        var values = new HashSet<string>(StringComparer.Ordinal);
        var sawElse = false;
        foreach (var clause in select.Cases)
        {
            if (clause.IsElse)
            {
                if (sawElse)
                    Report("SML3019", clause.CaseKeyword.Span, "Select Case contains more than one Case Else.");
                sawElse = true;
            }
            else if (clause.Value != null)
            {
                var caseType = AnalyzeExpression(clause.Value);
                if (caseType != SmileType.Error && selectorType != SmileType.Error && caseType != selectorType)
                    Report("SML3304", clause.Value.Span, "Case value type must match Select Case.");
                if (!TryEvaluateConstant(clause.Value, out var value, out _))
                    Report("SML3013", clause.Value.Span, "Case value must be a compile-time scalar expression.");
                else if (!values.Add((selectorType is NominalTypeSymbol nominal
                        ? nominal.RuntimeIdentity : selectorType.Name) + ":" +
                    Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)))
                    Report("SML3019", clause.Value.Span, $"Duplicate Case value '{value}'.");
            }
            AnalyzeStatements(clause.Statements, false);
        }
    }

    private void AnalyzeExit(ExitStatementSyntax exit)
    {
        var valid = exit.TargetKeyword.Kind == SyntaxKind.ForKeyword ? _forDepth > 0 : _doDepth > 0;
        if (!valid)
            Report("SML3018", exit.Span, $"Exit {SyntaxFacts.GetText(exit.TargetKeyword.Kind)} is not inside a matching loop.");
    }

    private void EnsureNumberTarget(SyntaxToken identifier, string statementName)
    {
        if (_currentRoutine == null && _rejectedProjectDeclarations.Contains(identifier))
            return;
        if (!TryResolveExisting(identifier.Text, out var symbol))
        {
            if (_optionExplicit)
            {
                var later = FindLocalDeclaration(identifier.Text);
                Report(later != null && later.Identifier.Position > identifier.Position ? "SML3307" : "SML3303",
                    identifier.Span, later != null && later.Identifier.Position > identifier.Position
                        ? $"Local '{identifier.Text}' is used before its Dim declaration."
                        : $"Variable '{identifier.Text}' must be declared because Option Explicit is enabled for this source.");
                return;
            }
            DeclareVariable(identifier.Text, SmileType.Number, Array.Empty<int>(), identifier.Span);
            return;
        }
        if (symbol.IsConstant || symbol.IsArray || symbol.Type != SmileType.Number)
            Report("SML3008", identifier.Span, $"{statementName} target '{identifier.Text}' must be a writable Number variable.");
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
                    RequireType(index, SmileType.Number, "SML3007", "Array index must be Number.");
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
                    result = arraySymbol.Type;
                }
                break;
            case FieldAccessExpressionSyntax field:
                result = TryBindEnumMember(field, out var enumMemberType)
                    ? enumMemberType
                    : BindField(AnalyzeExpression(field.Receiver), field.Field, field.Span, field);
                break;
            case LeadingMemberAccessExpressionSyntax leading:
                if (_withStack.Count == 0)
                {
                    Report("SML3413", leading.DotToken.Span,
                        "Leading-dot member access is valid only inside With...End With.");
                    result = SmileType.Error;
                }
                else
                {
                    var receiver = _withStack[_withStack.Count - 1];
                    if (receiver == null)
                        result = SmileType.Error;
                    else
                    {
                        result = BindField(receiver.TargetType, leading.Member, leading.Span, leading);
                        if (_fields.TryGetValue(leading, out var member))
                            _withMembers[leading] = new WithMemberBinding(receiver.Statement,
                                receiver.TargetType, member);
                    }
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
                result = AnalyzeCall(call, call.Identifier, call.Arguments, requireFunction: true);
                break;
            default:
                result = SmileType.Error;
                break;
        }
        _expressionTypes[expression] = result;
        return result;
    }

    private SmileType AnalyzeCall(SyntaxNode callSyntax, SyntaxToken identifier,
        IReadOnlyList<ArgumentSyntax> arguments, bool requireFunction)
    {
        if (SyntaxFacts.IsBuiltInFunction(identifier.Kind))
        {
            foreach (var argument in arguments.Where(argument => argument.IsNamed))
                Report("SML3433", argument.Name!.Span,
                    $"Built-in '{identifier.Text}' does not accept named arguments.");
            return AnalyzeBuiltInCall(identifier, arguments.Select(argument => argument.Expression).ToArray());
        }

        if (!_routines.TryGetValue(identifier.Text, out var routine))
        {
            foreach (var argument in arguments)
                AnalyzeExpression(argument.Expression);
            Report("SML3021", identifier.Span, $"Unknown routine or built-in function '{identifier.Text}'.");
            return SmileType.Error;
        }
        _routineCalls.Add(new RoutineCallSite(_currentRoutine, routine, _currentSource, identifier.Span));

        var parameterArguments = new BoundCallArgument?[routine.Parameters.Count];
        var sourceArguments = new List<BoundCallArgument>();
        var positionalIndex = 0;
        var sawNamed = false;
        var hasArgumentBindingError = false;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            var argumentType = AnalyzeExpression(argument.Expression);
            var parameterIndex = -1;
            if (argument.IsNamed)
            {
                sawNamed = true;
                parameterIndex = routine.Parameters
                    .Select((parameter, parameterIndex) => (parameter, parameterIndex))
                    .FirstOrDefault(item => string.Equals(item.parameter.Name, argument.Name!.Text,
                        StringComparison.OrdinalIgnoreCase)).parameterIndex;
                if (parameterIndex == 0 && !string.Equals(routine.Parameters.FirstOrDefault()?.Name,
                        argument.Name!.Text, StringComparison.OrdinalIgnoreCase))
                    parameterIndex = -1;
                if (parameterIndex < 0)
                {
                    hasArgumentBindingError = true;
                    Report("SML3433", argument.Name!.Span,
                        $"Routine '{routine.Name}' does not contain parameter '{argument.Name.Text}'.");
                    continue;
                }
                RegisterParameterUse(argument.Name!, routine.Parameters[parameterIndex]);
            }
            else
            {
                if (sawNamed)
                {
                    hasArgumentBindingError = true;
                    Report("SML3432", argument.Span,
                        "Positional arguments must precede named arguments.");
                    continue;
                }
                parameterIndex = positionalIndex++;
                if (parameterIndex >= routine.Parameters.Count)
                {
                    hasArgumentBindingError = true;
                    Report("SML3016", argument.Span,
                        $"Routine '{routine.Name}' received more positional arguments than declared parameters.");
                    continue;
                }
            }

            if (parameterArguments[parameterIndex] != null)
            {
                hasArgumentBindingError = true;
                var duplicateSpan = argument.Name?.Span ?? argument.Span;
                Report("SML3434", duplicateSpan,
                    $"Parameter '{routine.Parameters[parameterIndex].Name}' is supplied more than once.");
                continue;
            }

            var parameter = routine.Parameters[parameterIndex];
            var acceptsLegacyBoolean = !parameter.HasDeclaredType &&
                parameter.ParameterMode == ParameterPassingMode.ByVal &&
                parameter.Type == SmileType.Number && argumentType == SmileType.Boolean;
            if (argumentType != SmileType.Error && argumentType != parameter.Type && !acceptsLegacyBoolean)
                Report("SML3304", argument.Expression.Span,
                    $"Argument for parameter '{parameter.Name}' in '{routine.Name}' must be {TypeName(parameter.Type)}, found {TypeName(argumentType)}.");
            if (parameter.ParameterMode == ParameterPassingMode.ByRef &&
                !IsWritableLocation(argument.Expression, parameter.Type))
                Report("SML3305", argument.Expression.Span,
                    $"Argument for ByRef parameter '{parameter.Name}' must be a writable {TypeName(parameter.Type)} location.");

            var boundArgument = new BoundCallArgument(parameter, parameterIndex, argument, index);
            parameterArguments[parameterIndex] = boundArgument;
            sourceArguments.Add(boundArgument);
        }

        for (var index = 0; index < routine.Parameters.Count; index++)
        {
            if (parameterArguments[index] != null)
                continue;
            var parameter = routine.Parameters[index];
            if (parameter.IsOptional && parameter.HasDefaultValue)
            {
                parameterArguments[index] = new BoundCallArgument(parameter, index, syntax: null, sourceIndex: -1);
                continue;
            }
            if (parameter.IsOptional || hasArgumentBindingError)
                continue;
            Report("SML3435", identifier.Span,
                $"Required parameter '{parameter.Name}' is missing from call to '{routine.Name}'.");
        }

        _boundCalls[callSyntax] = new BoundCall(routine, sourceArguments,
            parameterArguments.Where(argument => argument != null).Select(argument => argument!).ToArray());
        if (requireFunction && !routine.IsFunction)
        {
            Report("SML3020", identifier.Span, $"Sub '{routine.Name}' cannot be used as an expression.");
            return SmileType.Error;
        }
        if (!requireFunction && routine.IsFunction)
            Report("SML3020", identifier.Span, $"Function '{routine.Name}' must be used in an expression.");
        return routine.IsFunction ? routine.ReturnType : SmileType.Error;
    }

    private void RegisterParameterUse(SyntaxToken token, ParameterSymbol parameter)
    {
        if (!_parameterUses.TryGetValue(_currentSource, out var uses))
        {
            uses = new Dictionary<int, ParameterSymbol>();
            _parameterUses[_currentSource] = uses;
        }
        uses[token.Position] = parameter;
    }

    private bool IsWritableLocation(ExpressionSyntax expression, SmileType requiredType)
    {
        switch (expression)
        {
            case NameExpressionSyntax name when TryResolveExisting(name.Identifier.Text, out var symbol):
                return !symbol.IsConstant && !symbol.IsArray && symbol.Type == requiredType;
            case ArrayAccessExpressionSyntax array when TryResolveExisting(array.Identifier.Text, out var symbol):
                return symbol.IsArray && symbol.Type == requiredType && array.Indices.Count == symbol.ArrayRank;
            case FieldAccessExpressionSyntax field:
                return AnalyzeExpression(field) == requiredType && IsWritableRecordReceiver(field.Receiver);
            case LeadingMemberAccessExpressionSyntax leading:
                return AnalyzeExpression(leading) == requiredType && _withMembers.ContainsKey(leading);
            default:
                return false;
        }
    }

    private bool IsWritableRecordReceiver(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case NameExpressionSyntax name when TryResolveExisting(name.Identifier.Text, out var symbol):
                return !symbol.IsConstant && !symbol.IsArray;
            case ArrayAccessExpressionSyntax array when TryResolveExisting(array.Identifier.Text, out var symbol):
                return symbol.IsArray && array.Indices.Count == symbol.ArrayRank;
            case FieldAccessExpressionSyntax field:
                return IsWritableRecordReceiver(field.Receiver);
            case LeadingMemberAccessExpressionSyntax leading:
                return _withMembers.ContainsKey(leading);
            default:
                return false;
        }
    }

    private SmileType AnalyzeBuiltInCall(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> arguments)
    {
        if (!SyntaxFacts.IsBuiltInFunction(identifier.Kind))
        {
            Report("SML3021", identifier.Span, $"Unknown built-in function '{identifier.Text}'.");
            return SmileType.Error;
        }
        var expected = SyntaxFacts.GetBuiltInFunctionParameters(identifier.Kind).Count;
        if (identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword)
            RequireGameWindow(identifier.Span, $"Built-in '{identifier.Text}'");
        if (arguments.Count != expected)
            Report("SML3016", identifier.Span, $"Built-in '{identifier.Text}' expects {expected} argument(s), found {arguments.Count}.");
        if (identifier.Kind is SyntaxKind.ImageWidthKeyword or SyntaxKind.ImageHeightKeyword or SyntaxKind.ImageLoadedKeyword)
        {
            if (arguments.Count != 0)
                RequireType(arguments[0], SmileType.Image, "SML3501", $"Built-in '{identifier.Text}' requires Image.");
            return identifier.Kind == SyntaxKind.ImageLoadedKeyword ? SmileType.Boolean : SmileType.Number;
        }
        if (identifier.Kind is SyntaxKind.TextWidthKeyword or SyntaxKind.TextHeightKeyword)
        {
            RequireGameWindow(identifier.Span, $"Built-in '{identifier.Text}'", "SML3505");
            if (arguments.Count > 0)
                RequireType(arguments[0], SmileType.Text, "SML3505", $"Built-in '{identifier.Text}' requires Text as its first argument.");
            if (arguments.Count > 1)
                RequireType(arguments[1], SmileType.Number, "SML3505", $"Built-in '{identifier.Text}' requires Number size.");
            return SmileType.Number;
        }
        if (identifier.Kind is SyntaxKind.TextLengthKeyword or SyntaxKind.TextCodeAtKeyword or SyntaxKind.TextSliceKeyword)
        {
            if (arguments.Count > 0)
                RequireType(arguments[0], SmileType.Text, "SML3700",
                    $"Built-in '{identifier.Text}' requires Text as its first argument.");
            for (var index = 1; index < arguments.Count; index++)
                RequireType(arguments[index], SmileType.Number, "SML3700",
                    $"Built-in '{identifier.Text}' requires Number index arguments.");
            return identifier.Kind == SyntaxKind.TextSliceKeyword ? SmileType.Text : SmileType.Number;
        }
        foreach (var argument in arguments)
            RequireType(argument, SmileType.Number, "SML3003", $"Built-in '{identifier.Text}' requires Number arguments.");
        return identifier.Kind is SyntaxKind.GameClosedKeyword or SyntaxKind.KeyHeldKeyword ? SmileType.Boolean : SmileType.Number;
    }

    private void PropagateRoutineCapabilities()
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var call in _routineCalls.Where(call => call.Caller != null))
            {
                var combined = call.Caller!.Capabilities | call.Callee.Capabilities;
                if (combined == call.Caller.Capabilities)
                    continue;
                call.Caller.Capabilities = combined;
                changed = true;
            }
        } while (changed);
    }

    private void DiagnoseTopLevelRoutineCapabilities()
    {
        if (_hasGameWindow)
            return;
        foreach (var call in _routineCalls.Where(call => call.Caller == null && call.Callee.RequiresGameWindow))
            _diagnostics.Report(call.Source, "SML3704", call.Span,
                $"Routine '{call.Callee.DisplayName}' requires a Game Window.");
    }

    private sealed class RoutineCallSite
    {
        public RoutineCallSite(RoutineSymbol? caller, RoutineSymbol callee, SourceText source, TextSpan span)
        {
            Caller = caller;
            Callee = callee;
            Source = source;
            Span = span;
        }

        public RoutineSymbol? Caller { get; }
        public RoutineSymbol Callee { get; }
        public SourceText Source { get; }
        public TextSpan Span { get; }
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
        if (leftType.IsEnum || rightType.IsEnum)
        {
            if (binary.OperatorToken.Kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken &&
                ReferenceEquals(leftType, rightType))
                return SmileType.Boolean;
            Report("SML3424", binary.Span,
                "Enum values support only '=' and '<>' with the exact same Enum type.");
            return SmileType.Error;
        }
        if (leftType.IsRecord || rightType.IsRecord || leftType == SmileType.Image || rightType == SmileType.Image)
        {
            Report(leftType == SmileType.Image || rightType == SmileType.Image ? "SML3509" : "SML3407", binary.Span,
                leftType == SmileType.Image || rightType == SmileType.Image
                    ? "Image values cannot be used with operators. Use Image_Loaded instead."
                    : "Whole records cannot be used with operators in Phase 3B.");
            return SmileType.Error;
        }
        switch (binary.OperatorToken.Kind)
        {
            case SyntaxKind.PlusToken:
                if (leftType == SmileType.Text && rightType == SmileType.Text)
                    return SmileType.Text;
                if (leftType == SmileType.Text || rightType == SmileType.Text)
                {
                    Report("SML3308", binary.Span, "Operator '+' requires two Text operands for concatenation or two Number operands for addition.");
                    return SmileType.Error;
                }
                return RequireOperands(binary, leftType, rightType, SmileType.Number, SmileType.Number);
            case SyntaxKind.MinusToken:
            case SyntaxKind.StarToken:
            case SyntaxKind.SlashToken:
            case SyntaxKind.ModKeyword:
                return RequireOperands(binary, leftType, rightType, SmileType.Number, SmileType.Number);
            case SyntaxKind.LessToken:
            case SyntaxKind.GreaterToken:
            case SyntaxKind.LessOrEqualsToken:
            case SyntaxKind.GreaterOrEqualsToken:
                if (leftType == SmileType.Text || rightType == SmileType.Text)
                {
                    Report("SML3308", binary.Span, "Text supports only '=', '<>', and '+' operators.");
                    return SmileType.Error;
                }
                return RequireOperands(binary, leftType, rightType, SmileType.Number, SmileType.Boolean);
            case SyntaxKind.EqualsToken:
            case SyntaxKind.NotEqualsToken:
                if (leftType != rightType)
                {
                    Report("SML3304", binary.Span, "Equality operands must have the same Number, Boolean, or Text type.");
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
        if (_currentRoutine != null && !_currentRoutine.Locals.ContainsKey(name) &&
            FindLocalDeclaration(name) is { } futureLocal && futureLocal.Identifier.Position > token.Position)
        {
            Report("SML3307", token.Span, $"Local '{name}' is used before its Dim declaration.");
            symbol = null!;
            return false;
        }
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
        if (_currentRoutine != null && FindLocalDeclaration(name) is { } local && local.Identifier.Position > token.Position)
            Report("SML3307", token.Span, $"Local '{name}' is used before its Dim declaration.");
        else if (declarations.TryGetValue(name, out var position) && position > token.Position)
            Report("SML3002", token.Span, $"Variable '{name}' is used before its first assignment.");
        else if (_types.ContainsKey(name) || _enumTypes.ContainsKey(name))
            Report("SML3410", token.Span, $"Type name '{name}' cannot be used as a value.");
        else
            Report(_optionExplicit ? "SML3303" : "SML3001", token.Span,
                _optionExplicit
                    ? $"Identifier '{name}' must be declared because Option Explicit is enabled for this source."
                    : $"Unknown identifier '{name}'.");
        symbol = null!;
        return false;
    }

    private DimStatementSyntax? FindLocalDeclaration(string name) => _currentRoutine == null
        ? null
        : EnumerateStatements(_currentRoutine.Declaration.Statements).OfType<DimStatementSyntax>()
            .FirstOrDefault(dim => string.Equals(dim.Identifier.Text, name, StringComparison.OrdinalIgnoreCase));

    private bool TryEvaluateConstant(ExpressionSyntax expression, out object value, out SmileType type) =>
        TryEvaluateConstant(expression, out value, out type, out _);

    private bool TryEvaluateConstant(ExpressionSyntax expression, out object value, out SmileType type,
        out EnumMemberSymbol? enumMember)
    {
        enumMember = null;
        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.Value is long number:
                value = number; type = SmileType.Number; return true;
            case LiteralExpressionSyntax literal when literal.Value is bool boolean:
                value = boolean; type = SmileType.Boolean; return true;
            case LiteralExpressionSyntax literal when literal.Value is string text:
                value = text; type = SmileType.Text; return true;
            case NameExpressionSyntax name:
                if (!_symbols.TryGetValue(name.Identifier.Text, out var symbol) || !symbol.IsConstant)
                {
                    if (!ResolveConstant(name.Identifier.Text) ||
                        !_symbols.TryGetValue(name.Identifier.Text, out symbol) || !symbol.IsConstant)
                        break;
                }
                value = symbol.ConstantValue;
                type = symbol.Type;
                enumMember = symbol.ConstantEnumMember;
                return true;
            case FieldAccessExpressionSyntax field when TryResolveEnumMemberExpression(field, out var resolvedEnumMember):
                RegisterEnumMemberUse(_currentSource, field, resolvedEnumMember);
                value = resolvedEnumMember.Value;
                type = resolvedEnumMember.ContainingType;
                enumMember = resolvedEnumMember;
                return true;
            case ParenthesizedExpressionSyntax parenthesized:
                return TryEvaluateConstant(parenthesized.Expression, out value, out type, out enumMember);
            case UnaryExpressionSyntax unary when TryEvaluateConstant(unary.Operand, out var operand, out var operandType):
                if (unary.OperatorToken.Kind == SyntaxKind.MinusToken && operandType == SmileType.Number && operand is long numberOperand)
                { value = -numberOperand; type = SmileType.Number; return true; }
                if (unary.OperatorToken.Kind == SyntaxKind.NotKeyword && operandType == SmileType.Boolean && operand is bool booleanOperand)
                { value = !booleanOperand; type = SmileType.Boolean; return true; }
                break;
            case BinaryExpressionSyntax binary when
                TryEvaluateConstant(binary.Left, out var left, out var leftType) &&
                TryEvaluateConstant(binary.Right, out var right, out var rightType):
                if (TryEvaluateBinary(binary.OperatorToken.Kind, left, right, leftType, rightType, out value, out type))
                    return true;
                break;
            case CallExpressionSyntax call when call.Identifier.Kind == SyntaxKind.AbsKeyword && call.Arguments.Count == 1 &&
                TryEvaluateConstant(call.Arguments[0].Expression, out var absObject, out var absType) && absType == SmileType.Number &&
                absObject is long absValue:
                value = absValue == long.MinValue ? long.MaxValue : Math.Abs(absValue); type = SmileType.Number; return true;
            case CallExpressionSyntax call when call.Identifier.Kind is SyntaxKind.MinKeyword or SyntaxKind.MaxKeyword && call.Arguments.Count == 2 &&
                TryEvaluateConstant(call.Arguments[0].Expression, out var firstObject, out var firstType) &&
                TryEvaluateConstant(call.Arguments[1].Expression, out var secondObject, out var secondType) &&
                firstType == SmileType.Number && secondType == SmileType.Number &&
                firstObject is long first && secondObject is long second:
                value = call.Identifier.Kind == SyntaxKind.MinKeyword ? Math.Min(first, second) : Math.Max(first, second);
                type = SmileType.Number; return true;
            case CallExpressionSyntax call when call.Identifier.Kind == SyntaxKind.RgbKeyword && call.Arguments.Count == 3 &&
                TryEvaluateConstant(call.Arguments[0].Expression, out var redObject, out _) && redObject is long red &&
                TryEvaluateConstant(call.Arguments[1].Expression, out var greenObject, out _) && greenObject is long green &&
                TryEvaluateConstant(call.Arguments[2].Expression, out var blueObject, out _) && blueObject is long blue:
                value = (red & 255) | ((green & 255) << 8) | ((blue & 255) << 16); type = SmileType.Number; return true;
        }
        value = 0L;
        type = SmileType.Error;
        enumMember = null;
        return false;
    }

    private static bool TryEvaluateBinary(SyntaxKind kind, object left, object right,
        SmileType leftType, SmileType rightType, out object value, out SmileType type)
    {
        value = 0L;
        type = SmileType.Error;
        if (kind is SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.ModKeyword)
        {
            if (kind == SyntaxKind.PlusToken && leftType == SmileType.Text && rightType == SmileType.Text &&
                left is string leftText && right is string rightText)
            {
                value = leftText + rightText;
                type = SmileType.Text;
                return true;
            }
            if (leftType != SmileType.Number || rightType != SmileType.Number || left is not long leftNumber ||
                right is not long rightNumber || (rightNumber == 0 && kind is SyntaxKind.SlashToken or SyntaxKind.ModKeyword))
                return false;
            value = kind switch
            {
                SyntaxKind.PlusToken => leftNumber + rightNumber,
                SyntaxKind.MinusToken => leftNumber - rightNumber,
                SyntaxKind.StarToken => leftNumber * rightNumber,
                SyntaxKind.SlashToken => leftNumber / rightNumber,
                _ => leftNumber % rightNumber
            };
            type = SmileType.Number;
            return true;
        }
        if (kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken)
        {
            if (leftType != rightType)
                return false;
            var equal = Equals(left, right);
            value = kind == SyntaxKind.EqualsToken ? equal : !equal;
            type = SmileType.Boolean;
            return true;
        }
        if (kind is SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken &&
            leftType == SmileType.Number && rightType == SmileType.Number && left is long leftRelation && right is long rightRelation)
        {
            value = kind switch
            {
                SyntaxKind.LessToken => leftRelation < rightRelation,
                SyntaxKind.GreaterToken => leftRelation > rightRelation,
                SyntaxKind.LessOrEqualsToken => leftRelation <= rightRelation,
                _ => leftRelation >= rightRelation
            };
            type = SmileType.Boolean;
            return true;
        }
        if (kind is SyntaxKind.AndKeyword or SyntaxKind.OrKeyword && leftType == SmileType.Boolean &&
            rightType == SmileType.Boolean && left is bool leftBoolean && right is bool rightBoolean)
        {
            value = kind == SyntaxKind.AndKeyword ? leftBoolean && rightBoolean : leftBoolean || rightBoolean;
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
            if (statement is WithStatementSyntax withStatement &&
                StatementsAlwaysReturn(withStatement.Statements))
                return true;
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
        ProjectDeclarationKind.Constant => "Const",
        ProjectDeclarationKind.Variable => "Dim",
        ProjectDeclarationKind.Array => "Dim",
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

    private enum ProjectDeclarationKind { Constant, Variable, Array, Routine, ImplicitGlobal }
    private enum ConstantResolutionState { Resolving, Resolved, Failed }

    private SmileType ResolveType(SyntaxToken? token, SmileType fallback)
    {
        if (token == null)
            return fallback;
        if (token.Text.StartsWith("__smile_missing_", StringComparison.Ordinal) ||
            token.Text.StartsWith("__smile_private_", StringComparison.Ordinal))
            return SmileType.Error;
        var type = token.Kind == SyntaxKind.NumberKeyword ? SmileType.Number
            : token.Kind == SyntaxKind.BooleanKeyword ? SmileType.Boolean
            : token.Kind == SyntaxKind.TextKeyword ? SmileType.Text
            : token.Kind == SyntaxKind.ImageKeyword ? SmileType.Image
            : _types.TryGetValue(token.Text, out var record) ? record
            : _enumTypes.TryGetValue(token.Text, out var enumType) ? enumType
            : SmileType.Error;
        if (type == SmileType.Error)
            Report("SML3401", token.Span,
                $"Unknown type '{DisplaySourceText(_currentSource, token)}'.");
        return type;
    }

    private bool TryResolveEnumType(string name, out EnumTypeSymbol type) =>
        _enumTypes.TryGetValue(name, out type!);

    private static string TypeName(SmileType type) => type.Name;
}
