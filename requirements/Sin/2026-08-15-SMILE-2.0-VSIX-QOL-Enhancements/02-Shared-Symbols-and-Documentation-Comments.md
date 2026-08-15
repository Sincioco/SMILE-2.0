# SMILE 2.0 Shared Symbols and Educational Documentation Comments

**Applies to:** `src\Smile.Language`, consumed by the compiler and Visual Studio extension
**Principle:** one authoritative language implementation

## 1. Why this belongs in Smile.Language

The current semantic model already owns:

- modules and import aliases;
- public module members;
- routines and parameters;
- variables and constants;
- record types and fields;
- provider identities;
- declaration `SourceText`, `TextSpan`, and `SourceLocation`.

Quick Info and F12 are two presentations of the same semantic question:

> What symbol is under this source position, how should it be described, and where was it declared?

That question must be answered once in `src\Smile.Language`. The VSIX must not parse `Menu.Create` independently or maintain separate symbol tables.

## 2. Recommended shared result model

Names may be adjusted to fit repository conventions, but the shared API should be equivalent to:

```csharp
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
    public SmileResolvedSymbolKind Kind { get; }
    public string Name { get; }
    public string QualifiedName { get; }
    public TextSpan ReferenceSpan { get; }
    public SourceLocation? DeclarationLocation { get; }
    public string ProviderIdentity { get; }
    public string ModuleName { get; }
    public string Signature { get; }
    public SmileDocumentation Documentation { get; }
    public bool RequiresGameWindow { get; }
}

public sealed class SmileDocumentation
{
    public string Summary { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
    public string Returns { get; }
    public string Remarks { get; }
}
```

The actual implementation may hold direct references to `RoutineSymbol`, `VariableSymbol`, `SmileModuleMember`, `ModuleSymbol`, `RecordTypeSymbol`, or `RecordFieldSymbol` internally. Avoid copying semantic state unnecessarily.

A small public service is recommended:

```csharp
public static class SmileSymbolService
{
    public static bool TryResolve(
        SmileAnalysisResult analysis,
        SyntaxTree syntaxTree,
        int position,
        out SmileResolvedSymbol symbol);
}
```

A separate presentation helper is acceptable:

```csharp
public static class SmileSymbolDisplayService
{
    public static SmileSymbolPresentation Present(
        SmileResolvedSymbol symbol,
        SmileCompilationDependencyContext dependencies);
}
```

The purpose is a stable editor-facing contract, not a large object hierarchy.

## 3. Token selection

Resolve only from the authoritative `SyntaxTree.Tokens`.

Rules:

1. Clamp the supplied position to `0..Source.Length`.
2. Prefer the token whose span contains the position.
3. If the position equals the end of an identifier, retry at `position - 1`.
4. Reject `CommentToken`, string tokens, newline, punctuation, bad tokens, and end-of-file.
5. Preserve the selected token's span as `ReferenceSpan`.
6. SMILE matching remains case-insensitive.

Do not scan raw text with an editor-only regular expression.

## 4. Required resolution rules

### 4.1 Import alias / module

For this source:

```smile
IMPORT Smile.UI.Menu AS Menu
MainMenu = Menu.Create(...)
```

When the selected token is `Menu` immediately to the left of a dot:

1. Obtain `analysis.SemanticModel.GetImports(syntaxTree.Source)`.
2. Resolve alias `Menu` to its `ModuleSymbol`.
3. Return a module symbol result.
4. Resolve the declaration location from the module's declaring syntax tree.

`ModuleSymbol` currently exposes `SyntaxTrees` but not a direct declaration location. Use the smallest of these approaches:

- derive the first deterministic `ModuleDeclarationSyntax` and its identifier span from `ModuleSymbol.SyntaxTrees`; or
- add a minimal `DeclarationLocation`/declaration collection to `ModuleSymbol` during module inventory.

Do not infer a module declaration from a filename.

### 4.2 Qualified member

When the selected token is `Create` to the right of a dot:

1. Resolve the immediate receiver token as an import alias.
2. Resolve the alias to a `ModuleSymbol`.
3. Look in the module's accessible member/type dictionaries using SMILE's case-insensitive rules.
4. Respect visibility and `SmileCompilationDependencyContext`.
5. Return the member's existing declaration source/span:
   - `SmileModuleMember.DeclarationLocation`;
   - or the attached routine/variable/type declaration location where more precise.

The result must distinguish function, subroutine, constant, variable, array, and type.

### 4.3 Unqualified routine

Resolve in this order:

1. current routine/local scope when relevant;
2. current module;
3. project/global scope;
4. directly imported symbols only where SMILE syntax currently allows them.

Do not create new name-resolution precedence. Reuse semantic-model behavior.

### 4.4 Variables, parameters, locals, constants, arrays, types, and fields

Use existing semantic symbols and declaration locations:

- `VariableSymbol.DeclarationLocation`;
- `RoutineSymbol.DeclarationLocation`;
- `SmileType.DeclarationLocation`;
- `RecordFieldSymbol.DeclarationLocation`;
- `SmileModuleMember.DeclarationLocation`.

For fields, prefer the bound field already recorded by `SemanticModel.TryGetField(...)` when the syntax node is available. If mapping position to an expression is not available without a large syntax-tree visitor, field navigation may use the receiver's resolved record type and matching field name.

### 4.5 Binary-only package symbols

A symbol can be semantically known while its physical source is absent. Preserve the symbol and signature for Quick Info, but allow `DeclarationLocation` to be null or point to a non-existing source.

The Visual Studio layer decides whether navigation is possible. Do not fabricate a source file.

## 5. Shared signature presentation

Current completion code already formats routine parameters, `BYREF`, return types, module/provider text, and `RequiresGameWindow`.

Avoid creating conflicting signatures such as one format in completion and a different semantic interpretation in Quick Info.

Preferred implementation:

1. Extract the current routine/member/type display formatting into a small shared presentation helper in `Smile.Language`.
2. Use it from:
   - `SmileCompletionService`;
   - Quick Info;
   - future signature help.
3. Preserve existing completion text unless improving it is required for correctness.

Presentation may differ visually in Quick Info, but names, modes, types, return types, and capability notes must come from the same semantic objects.

## 6. Documentation comment convention

### 6.1 Syntax

Use the existing single-apostrophe SMILE comment syntax. A line beginning with three apostrophes is an educational documentation comment:

```smile
''' Creates a menu instance.
''' @param Style: Visual settings used to draw and lay out the menu.
''' @param X: Left edge in game-window coordinates.
''' @param Y: Top edge in game-window coordinates.
''' @param Width: Menu width in pixels.
''' @param Height: Menu height in pixels.
''' @param VisibleRows: Requested number of menu rows visible at once.
''' @returns: A positive menu handle, or 0 when creation fails.
PUBLIC FUNCTION Create(...)
```

Supported forms:

```text
''' free-form summary line
''' continuation of summary
''' @param ParameterName: explanation
''' @returns: explanation
''' @remarks: additional explanation
```

### 6.2 Parsing rules

- Documentation lines must be contiguous and immediately precede the declaration, allowing only whitespace between the final documentation line and the declaration.
- Ordinary comments beginning with one apostrophe are not documentation.
- Strip the leading `'''` and at most one following space.
- Consecutive untagged lines form the summary, joined with sensible line breaks/spaces.
- Tags are case-insensitive.
- Parameter names are matched case-insensitively to the declaration.
- `@param` syntax requires a parameter name followed by `:`.
- `@returns:` is valid for functions. It may be ignored for a subroutine without error.
- `@remarks:` may span subsequent untagged documentation lines until another recognized tag.
- Repeated `@param` entries for the same name may be joined or the first may win; choose one deterministic rule and test it.
- Unknown tags are ignored or treated as remarks. They must not create compiler diagnostics.
- Malformed comments never break compilation, completion, Quick Info, or navigation.
- Documentation is descriptive metadata only. It does not change parsing, binding, visibility, overload resolution, code generation, or runtime behavior.

### 6.3 Location of parser

Implement documentation extraction in `src\Smile.Language`, for example:

```text
src\Smile.Language\Documentation.cs
```

The implementation can use:

- `SyntaxTree.Tokens`, which retain `CommentToken` entries; and/or
- the declaration's `SourceText` and declaration start.

Do not implement the convention only in `Smile.VisualStudio`.

### 6.4 Documentation association

Associate documentation with declaration identity rather than only a symbol name. The same name can exist in multiple modules or scopes.

Practical keys include:

- `SourceText` reference plus declaration span;
- normalized file path plus declaration span;
- direct symbol object reference within an analysis result.

Caching may be per `SmileAnalysisResult`. Avoid a global cache that can leak stale source text.

## 7. Required Menu documentation

As part of this milestone, add useful `'''` documentation comments to every `PUBLIC` routine in:

```text
libraries\Smile.UI\Menu.smile
```

At minimum, each public routine must have:

- a one- or two-sentence summary;
- one `@param` explanation for each parameter;
- `@returns:` for each function;
- important failure/sentinel values such as `0`, `-1`, `FALSE`, or `UI_EVENT_*`;
- a `GAME WINDOW` remark where drawing behavior needs it.

The documentation should teach, not merely restate names.

Bad:

```smile
''' @param Width: The width.
```

Better:

```smile
''' @param Width: Menu width in game-window pixels. Negative values are clamped to zero.
```

### 7.1 Required `Menu.Create` documentation meaning

The exact wording may be improved, but it must communicate:

```smile
''' Creates a menu instance using the supplied style, position, size, and requested visible-row count.
''' @param Style: Menu appearance, window, text, cursor, spacing, and overflow settings. Passed by reference and validated before creation.
''' @param X: Left edge of the menu in game-window coordinates.
''' @param Y: Top edge of the menu in game-window coordinates.
''' @param Width: Menu width in pixels. Values are clamped to the supported layout range.
''' @param Height: Menu height in pixels. Values are clamped to the supported layout range.
''' @param VisibleRows: Requested number of rows visible at once. The effective count may be reduced to fit the content area.
''' @returns: A positive generation-safe menu handle, or 0 when the style is invalid or no menu slot is available.
PUBLIC FUNCTION Create(BYREF Style AS Core.MenuStyle, X AS NUMBER, Y AS NUMBER, Width AS NUMBER, Height AS NUMBER, VisibleRows AS NUMBER) AS NUMBER
```

Preserve the routine's executable statements exactly unless a separate correctness issue is discovered.

## 8. Module-level documentation

Allow a documentation block immediately before `MODULE`:

```smile
''' Reusable menu creation, item management, input handling, and drawing services.
MODULE Smile.UI.Menu
```

Add a concise module summary to `libraries\Smile.UI\Menu.smile`.

`OPTION EXPLICIT` still follows the module declaration under current grammar. Documentation comments must not interfere with the existing rule that `OPTION EXPLICIT` is the first executable/declarative statement where required.

## 9. Quick Info presentation data

The shared presentation should expose enough structure for Visual Studio to render:

- signature line(s);
- summary;
- ordered parameters with name, type, passing mode, and documentation;
- return type and documentation;
- remarks;
- module/provider;
- source path;
- declaration location;
- capability notes.

Do not return Visual Studio WPF/editor types from `Smile.Language`.

## 10. Focused tests

Add shared-language tests that prove:

1. a contiguous documentation block binds to the following routine;
2. an ordinary `'` comment is not documentation;
3. a blank non-documentation line breaks association when appropriate;
4. tags and parameter names are case-insensitive;
5. malformed/unknown tags do not create compiler diagnostics;
6. absent parameter docs preserve the signature and leave prose empty;
7. `Menu` resolves to the imported module at the alias use;
8. `Create` resolves to the module member and exact declaration span;
9. case variations such as `menu.create` resolve correctly;
10. unresolved names return false without throwing;
11. a local routine/variable resolves to its declaration;
12. a record type/field resolves when supported by the existing semantic model.

Keep tests focused and deterministic. Do not introduce a separate editor parser test harness.
