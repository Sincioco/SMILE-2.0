# Phase 1 — True Multi-File Compilation Formal Specification

## 1. Purpose

This phase changes SMILE 2.0 from a compiler that analyzes exactly one source document into a compiler that can analyze several source documents as one program.

It introduces no new SMILE keyword. It introduces a compilation/source model, command-line support, project metadata, emitter support, project-wide tooling, and multi-file debug locations.

The implementation must remain small enough to understand and strong enough to support Phase 2 modules and reusable libraries.

---

# 2. Terms

## Compilation

A **compilation** is an ordered set of independently parsed SMILE source documents that share one semantic model and produce one output program.

## Startup source

Exactly one source document is the **startup source**.

The startup source owns the program’s executable top-level statements and entry behavior.

Examples include:

```text
Program.smile
Program-NoDemo.smile
```

Only the selected startup source participates. An alternate complete program source is not automatically compiled as a support source.

## Support source

A **support source** contributes declarations and routine implementations to the same program but does not provide an independent entry program.

Examples include:

```text
GameState.smile
Drawing.smile
MapHelpers.smile
BattleMath.smile
```

## Source identity

Each document retains its own:

- normalized full path when available;
- source text;
- syntax tree;
- token list;
- line/column mapping;
- diagnostics;
- declaration locations;
- debug locations.

Never concatenate file text and pretend it was one physical source file.

---

# 3. Language API model

Keep the existing single-source API working:

```csharp
SmileLanguage.Analyze(sourceText, filePath)
```

It must behave as a one-document compilation whose document is the startup source.

Add a small public multi-source entry point. Exact type names may follow repository conventions, but the model should be conceptually similar to:

```csharp
public sealed class SmileSourceDocument
{
    public string Text { get; }
    public string FilePath { get; }
    public bool IsStartup { get; }
}

public static SmileAnalysisResult Analyze(
    IReadOnlyList<SmileSourceDocument> sources)
```

Required invariants:

- at least one document;
- exactly one startup document;
- no duplicate normalized source path;
- deterministic input order;
- single-source wrapper remains source-compatible for current consumers/tests;
- no target-specific behavior in `Smile.Language`.

`SmileAnalysisResult` must expose all syntax trees and the startup syntax tree.

For compatibility, retaining `SyntaxTree` as an alias for the startup tree is acceptable and encouraged if it avoids unnecessary breakage. Add a clear all-tree property such as `SyntaxTrees` and a lookup method/property suitable for tooling.

The semantic model is compilation-wide.

---

# 4. Source locations

A `TextSpan` alone is not a complete location in a multi-file compilation.

Introduce or consistently use a source-aware location concept containing at least:

```text
SourceText or source-document identity
TextSpan
file path
line
column
```

Symbols and routines must retain the source in which they were declared.

The compiler and tooling must be able to determine the source document for:

- every diagnostic;
- every global declaration;
- every routine declaration;
- every executable statement used for native debug mapping;
- every Web-target support error;
- completion position queries.

Current single-source APIs may remain as convenience aliases, but new multi-source code must not guess that every span belongs to the startup file.

---

# 5. Parsing

Each source document is lexed and parsed independently with the existing shared lexer/parser.

The result is one `SyntaxTree` per physical source file.

Required behavior:

- parser errors identify the correct file;
- line and column are local to that file;
- one malformed support file does not cause another file’s positions to be used;
- all parser diagnostics are aggregated deterministically by source order and position;
- no textual include/preprocessor mechanism is added.

---

# 6. Phase 1 top-level rules

## Startup source

The startup source may contain everything currently legal at top level, including:

- constants;
- arrays;
- routines;
- assignments and other executable statements;
- `Game Window`;
- the main loop;
- `End Program`.

Existing single-file programs must retain their behavior.

## Support source

At top level, a support source may contain only:

- `Const` declarations;
- `Dim` declarations;
- `Sub ... End Sub` declarations;
- `Function ... End Function` declarations;
- comments and blank lines.

The bodies of support-file routines may use all normally valid statements.

A support source must not contain an executable top-level statement such as:

```smile
Score = 0
Call StartGame()
Print "Hello"
Game Window "Other Program"
End Program
```

Report a clear semantic diagnostic in the support file explaining that executable top-level statements belong in the selected startup source. Do not silently ignore or reorder them.

This restriction is intentional. Until Phase 3 adds proper scalar declarations and later phases define module initialization, reusable support files should expose explicit initialization routines such as:

```smile
Sub InitializeMenu()
    ' Initialize component state.
End Sub
```

## Game window

`Game Window` is legal only in the startup source and retains the existing program-wide rules. A support-file `Game Window` is an error.

## Program termination

`End Program` is legal only in the startup source. A support-file `End Program` is an error.

---

# 7. Semantic analysis

Use a compilation-wide, order-independent declaration/binding strategy rather than sequentially analyzing one file at a time as isolated programs.

The implementation must support:

- a startup file calling a routine declared in any support file;
- a support routine calling a routine declared in another support file;
- a support routine referencing a valid global symbol declared by the startup source;
- any source referencing a global `Const` or `Dim` array declared in a support source;
- forward references across source order;
- case-insensitive symbol lookup across the compilation.

Preserve current SMILE rules for implicit startup globals and routine locals.

A practical implementation will normally:

1. Parse every document.
2. Collect compilation-wide declarations and implicit startup globals.
3. Register all routines before binding routine calls/bodies.
4. Diagnose duplicate names across files.
5. Bind/analyze the startup top level and every routine body using one semantic model.

Do not make semantic correctness depend on the order in which support files appear.

## Duplicate symbols

The global namespace remains one case-insensitive namespace in Phase 1.

Therefore these collide even when declared in separate files:

```text
Score
score
SCORE
```

Report the duplicate at the later declaration with the correct file/line/column. Mention the conflicting symbol name clearly. If practical, include the first declaration’s path/line in the message without creating a complex related-location framework solely for this phase.

## Routine ordering

Emitter ordering may be deterministic by source order and source span, but semantic visibility must not depend on physical order.

---

# 8. Command-line compiler contract

Keep all existing single-file commands working without changes.

Add a repeatable option for explicit support sources:

```text
--source <path.smile>
```

Examples:

```text
artifacts\compiler\smilec.exe \
  examples\MultiFileBasics\Program.smile \
  --source examples\MultiFileBasics\GameState.smile \
  --source examples\MultiFileBasics\Drawing.smile \
  -o artifacts\games\MultiFileBasics.exe
```

```text
artifacts\compiler\smilec.exe \
  examples\MultiFileBasics\Program.smile \
  --source examples\MultiFileBasics\GameState.smile \
  --source examples\MultiFileBasics\Drawing.smile \
  --target web \
  --output-dir artifacts\web\MultiFileBasics
```

Rules:

- the first positional `.smile` file remains the startup source;
- each `--source` value is a support source;
- `--source` may appear more than once;
- duplicate normalized paths are rejected clearly;
- the startup path repeated as `--source` is rejected clearly;
- missing/unreadable support files report the actual path;
- native/Web output option rules remain unchanged;
- single-source default output naming remains based on the startup path;
- `Tools > Build SMILE File` remains a deliberately single-file native command unless a later phase explicitly changes it.

Do not require `.smileproj` parsing inside `smilec` in this phase. Visual Studio may pass the selected startup file and repeated `--source` arguments. This keeps the compiler CLI small and avoids prematurely designing the Phase 2 project/reference model.

---

# 9. `.smileproj` source-item contract

A SMILE project continues to use:

```xml
<SmileSource Include="..." />
```

Add optional project metadata:

```xml
StartupOnly="true"
```

Example:

```xml
<ItemGroup>
  <SmileSource Include="Program.smile" StartupOnly="true" />
  <SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
  <SmileSource Include="GameState.smile" />
  <SmileSource Include="Drawing.smile" />
</ItemGroup>
```

Required behavior:

- `<StartupFile>` selects exactly one startup source.
- The selected startup source is always included exactly once.
- A non-selected source with `StartupOnly="true"` remains visible in Solution Explorer but is excluded from the compilation.
- A source without `StartupOnly="true"` is included as a support source unless it is the selected startup source.
- Missing `StartupOnly` means `false`.
- Boolean parsing is case-insensitive and rejects invalid values clearly.
- Duplicate normalized source paths are rejected.
- A missing selected startup file is an error.
- A missing support file is an error.
- Source order is deterministic and follows project order after the startup source has been identified.

Update the ten game projects so complete alternative program files cannot accidentally compile together. Normally both of these should be startup-only candidates:

```xml
<SmileSource Include="Program.smile" StartupOnly="true" />
<SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
```

Update console/game project templates to use the new metadata appropriately.

The existing student workflow remains simple: changing `<StartupFile>` chooses the normal or no-demo program. No other metadata change is required.

---

# 10. Program entry and emitters

## Startup execution

Only executable top-level statements from the startup syntax tree form the generated program entry body.

## Declarations and routines

All valid compilation-wide globals, constants, arrays, and routines from the startup/support sources are available to the generated output.

## Native emitter

Update the native emitter to:

- collect literals, built-in/runtime usage, storage, loops, and routines across all syntax trees;
- emit one global data layout;
- emit all routines exactly once;
- emit only the startup source’s executable top-level entry statements;
- keep the current ABI and native runtime behavior;
- preserve music shutdown and process-exit behavior;
- preserve DirectX/GDI/VSync selection.

Do not concatenate generated assembly from independent single-file emitters.

## Web emitter

Update the Web emitter to:

- assign collision-safe names across all compilation symbols/routines;
- emit all global declarations and routines exactly once;
- emit only startup top-level entry statements in `smileMain`;
- derive the page/game title from the startup source’s `Game Window`;
- retain current safe-integer, async, Canvas, audio, input, persistence, and asset semantics;
- report unsupported Web operations against the correct source document.

## Game detection

Determine whether the compilation is a game from the selected startup source/program semantics, not by assuming the first support file or concatenated source.

---

# 11. Multi-file Windows debug information

The current line-only debug-helper identity is insufficient because several files can contain the same line number.

Replace the line-only model with source-aware debug locations.

Required behavior:

- each executable statement maps to its physical `.smile` file and line;
- helper/native symbol names are unique even when two files both execute line 10;
- generated C debug source may use multiple `#line <line> "<path>"` directives;
- native PDB information retains the real source path;
- a breakpoint in `Program.smile` still binds and hits;
- a breakpoint in a support-file routine binds and hits that support file/line;
- Debug output contains debug support; Release remains free of unnecessary debug helpers as today.

A small `DebugLocation`/`DebugSite` model containing source, line, and unique helper identity is preferable to a `SortedSet<int>` of line numbers.

Do not generate temporary fake `.smile` filenames.

---

# 12. Visual Studio build behavior

The Visual Studio project system must:

- retain the parsed project source list and `StartupOnly` metadata;
- compute the startup/support compilation set;
- save every open source document participating in the build, not just the startup file;
- invoke `smilec` with the startup path and repeated `--source` paths;
- use the same source set for Windows and Web;
- preserve asset copying;
- preserve platform/configuration selection;
- preserve native F5 debugging;
- preserve Web F5/Ctrl+F5 republish/browser launch;
- preserve clean behavior.

When a non-selected startup-only file is open and dirty, it is not part of the current build and need not be force-saved solely for the build. Do not discard its changes.

Output-window command lines should visibly include the selected support sources so failures are diagnosable.

---

# 13. Project-aware Visual Studio analysis

The existing editor currently analyzes one buffer in isolation. Extend it so a `.smile` file belonging to a loaded project can receive a project-wide analysis context.

Required behavior:

- syntax coloring continues to use the current file’s tokens;
- diagnostics/squiggles displayed in one editor are filtered to that physical file;
- completion in the startup source includes visible globals, arrays, SUBs, and FUNCTIONs from support files;
- completion inside a support file includes compilation-wide globals/routines plus its current routine locals/parameters;
- a syntax/semantic error in a support file points to that support file;
- loose files opened outside a project retain the existing single-file analysis behavior;
- automatic completion, manual invocation, comment/string suppression, and current completion descriptions remain functional.

Use the latest unsaved text for the active buffer. Use safely available open-buffer text for other project files when practical; otherwise use their saved disk text in this first phase. A project build must save all participating open files before compilation, so emitted output never silently uses stale open source.

Do not replace the working content-type registration or normal editor-opening path.

---

# 14. Legacy projects and templates

Update every affected `.smileproj` in the repository, including all ten games and both Visual Studio templates.

The normal and no-demo sources must remain visible and selectable as startup files without being compiled together.

Do not split the legacy game source files merely to make them multi-file in Phase 1. They already prove single-source compatibility. The new `MultiFileBasics` example proves the new capability.

The smoke suite must continue to compile both normal and no-demo variants on Windows and Web through their intended paths.

---

# 15. Required example

Add the companion `examples\MultiFileBasics` project.

It must prove at least:

- startup source calls a support-file routine;
- one support-file routine calls/functions from another support file;
- support-file constants and arrays are visible across files;
- a support routine can read a startup global;
- native Windows output runs;
- Web output runs;
- the project has no assets or framework dependency;
- source files remain individually readable for teaching.

The supplied companion files are the starting point. Adjust only as required by the final implemented API/project format.

---

# 16. Required diagnostics

Add focused diagnostics for at least:

- zero startup sources in the multi-source API;
- multiple startup sources;
- duplicate source path;
- missing support source at the compiler/project layer;
- executable top-level statement in a support source;
- `Game Window` in a support source;
- `End Program` in a support source;
- duplicate global symbol across files;
- duplicate routine across files;
- Web-target error mapped to a support file.

Use the next appropriate codes in the repository’s existing diagnostic families. Do not renumber existing public diagnostics.

---

# 17. Explicit non-goals

Do not implement in Phase 1:

- `Module` or `End Module`;
- `Import`;
- `Public`/`Private`;
- `.smilelib` or `.smilelibproj`;
- project references;
- `Option Explicit`;
- user-defined `Type`;
- scalar `Dim ... As ...`;
- mutable `Text`;
- `ByRef`/`ByVal`;
- parameter-limit removal;
- images or sprites;
- clipping;
- persistent data blocks;
- multiple sound-effect channels;
- menu/inventory/RPG components;
- browser `.smile` breakpoints;
- a package manager;
- a broad project-system rewrite.

Prepare for these features; do not prematurely implement them.
