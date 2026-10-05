# SMILE 2.0 language

SMILE stands for **Simple Modern and Intuitive Language for Everyone**.

Start here for the current language contract. The topic guides retain the detailed
semantics and examples formerly collected on this page; this is an index, not a
replacement specification.

[`src/Smile.Language`](../../src/Smile.Language) is the sole authority for source
documents, tokens, keyword facts, syntax, diagnostics, symbols, types and semantic
analysis. Both `smilec` and the Visual Studio extension consume the same
`SmileLanguage.Analyze` result for single-file and multi-file compilations.

## Choose a reference

| Topic | Read for |
| --- | --- |
| [Syntax and routines](syntax-and-routines.md) | Declarations, exact types, Optional and named arguments, control flow, multiline forms and formatting. |
| [Values and ownership](values-and-ownership.md) | Record and Class members, enums, `With`, captured locations, deep copies, identity and cleanup. |
| [Projects and modules](projects-and-modules.md) | Imports, visibility, providers, startup/support sources and package milestones. |
| [Game Window, drawing and input](game-window.md) | Loading and deferred close, the drawing surface, arcs, audio focus and key constants. |
| [Data and files](data-and-files.md) | Checked persistence, recovery, user-chosen UTF-8 files, executable-relative text input and native prompts. |
| [Language diagnostics](diagnostics.md) | Stable typed-language diagnostic codes and their meanings. |
| [Double](double.md) | Checked binary64 arithmetic, explicit conversions and fractional storage. `Number` remains integral. |

For code presentation, use the [authoritative formatting
conventions](../SMILE-2.0-Authoritative-Code-Formatting-Conventions.md). For deeper
object-model rationale and acceptance contracts, read the existing
[lightweight-OOP specification](SMILE-2.0-Lightweight-OOP.md) alongside the current
[values and ownership reference](values-and-ownership.md).

## Current boundaries

- Native Windows is the active implementation and validation priority. Web work
  remains paused; a documented Web contract or historical test does not resume it.
- Current `.smilelib` writers and readers require **format 7**. Older packages,
  including format 6, must be rebuilt. The [package guide](projects-and-modules.md#package-versions-and-milestone-guides)
  separates current requirements from milestone history.
- `Load Text File` accepts a non-empty `Text` expression path, not only a literal.
  Its byte-array bounds and missing-file behavior remain unchanged; see
  [text input](data-and-files.md#executable-relative-text-input).
- [Module-level Class initialization](values-and-ownership.md#module-level-class-initialization)
  runs before startup on native Windows. Its Web adoption remains open; do not
  infer parity from the shared syntax.
- The documented Class surface is deliberately bounded: no inheritance, virtual
  dispatch, user-defined finalizers, static Class members, indexed properties or
  Class arrays. See [Class references](values-and-ownership.md#class-references).
- Persistence does not merge or lock concurrent edits. Keep the recovery and
  failure semantics in [Data and files](data-and-files.md#recoverable-persistent-data)
  when implementing save workflows.

## Existing focused guides and evidence

These documents remain intact because they provide focused specifications or
milestone rationale. Version-specific claims describe that milestone; use the
current references above and shared source for present behavior.

| Guide | Scope |
| --- | --- |
| [Phase 4: media](phase4-media.md) | Images, persistent byte data and media contracts. |
| [Phase 5: UI](phase5-ui.md) | Unicode-safe text and the bounded `Smile.UI` surface. |
| [Phase 6: RPG](phase6-rpg.md) | Stable application identity and source-authored RPG data. |
| [Phase 7: RPG world](phase7-rpg-world.md) | World/story composition and `Smile.Game`. |
| [Phase 8: dungeons](phase8-rpg-dungeons.md) | Dungeon composition using existing packages. |
| [Phase 9: battles](phase9-rpg-battles.md) | Deterministic battle modules and transient battle state. |

The executable [examples](../../examples) and [games](../../games) show complete
usage, including external-map parsing and player-focused `Program-NoDemo.smile`
teaching sources. The [ownership guide](values-and-ownership.md#record-array-locations-and-ownership)
retains the focused compiled-fixture commands for location and cleanup changes.

## Links from the former single-page reference

Existing heading links remain valid here and lead to their current topic. New
links should target the topic guide directly.

- <a id="modules-and-imports"></a>[Modules and imports](projects-and-modules.md#modules-and-imports)
- <a id="explicit-declarations-and-built-in-types"></a>[Explicit declarations and built-in types](syntax-and-routines.md#explicit-declarations-and-built-in-types)
- <a id="optional-parameters-and-named-arguments"></a>[Optional parameters and named arguments](syntax-and-routines.md#optional-parameters-and-named-arguments)
- <a id="multiline-routine-declarations"></a>[Multiline routine declarations](syntax-and-routines.md#multiline-routine-declarations)
- <a id="record-types"></a>[Record types](values-and-ownership.md#record-types)
- <a id="type-methods-and-properties"></a>[Type methods and properties](values-and-ownership.md#type-methods-and-properties)
- <a id="class-references"></a>[Class references](values-and-ownership.md#class-references)
- <a id="module-level-class-initialization"></a>[Module-level Class initialization](values-and-ownership.md#module-level-class-initialization)
- <a id="enum-types"></a>[Enum types](values-and-ownership.md#enum-types)
- <a id="with-blocks"></a>[With blocks](values-and-ownership.md#with-blocks)
- <a id="multi-file-programs"></a>[Multi-file programs](projects-and-modules.md#multi-file-programs)
- <a id="structured-example"></a>[Structured example](syntax-and-routines.md#structured-example)
- <a id="recoverable-persistent-data"></a>[Recoverable persistent Data](data-and-files.md#recoverable-persistent-data)
- <a id="user-chosen-utf-8-files"></a>[User-chosen UTF-8 files](data-and-files.md#user-chosen-utf-8-files)
- <a id="multiline-parenthesized-expressions"></a>[Multiline parenthesized expressions](syntax-and-routines.md#multiline-parenthesized-expressions)
- <a id="smile-source-readability-style"></a>[SMILE source readability style](syntax-and-routines.md#smile-source-readability-style)
- <a id="game-surface"></a>[Game surface](game-window.md#game-surface)
- <a id="phase-3-diagnostics"></a>[Phase 3 and later diagnostics](diagnostics.md)
- <a id="arc-drawing"></a>[Arc drawing](game-window.md#arc-drawing)
- <a id="automatic-focus-behavior"></a>[Automatic focus behavior](game-window.md#automatic-focus-behavior)
- <a id="record-array-locations-and-ownership"></a>[Record-array locations and ownership](values-and-ownership.md#record-array-locations-and-ownership)
- <a id="native-text-prompts-and-editor-keys"></a>[Native text prompts and editor keys](data-and-files.md#native-text-prompts-and-editor-keys)
