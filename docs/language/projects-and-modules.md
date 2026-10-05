# Projects and modules

[Language reference](README.md) · Source composition, provider boundaries and package milestones.

## Modules and imports

`Module dotted.name` ... `End Module` declares a module. Declarations are private unless prefixed with `Public`; `Private` is available when explicit intent helps. A physical source imports a module with `Import dotted.name As Alias`, then accesses exported constants, arrays, functions, subroutines, record types, Class types, and enums through that alias. Imports are scoped to that physical source. One module may span files from one provider. Inside a module, an unqualified nominal type must be built in or owned by that same module, including a type declared in another physical module source; external module types require explicit `Alias.Type` qualification. Project-global and ambient sibling-library types never enter a module's unqualified type scope. Duplicate providers, import cycles, private access, unknown members, and module access to consumer globals are diagnosed by the shared binder.

```smile
Import Smile.Math.Extras As Math
Print Math.Clamp(150, 0, 100)
```

Library compilation requires every source to declare a module. Application projects may also contain local module sources without packaging them.

## Multi-file programs

A compilation may contain one selected startup source and any number of support sources. Every file is parsed separately and retains its real path, lines, tokens, diagnostics, and debug locations; all files share one case-insensitive value/routine model and a separate type namespace.

The startup source owns executable top-level statements, `Game Window`, and `End Program`. A support source may contain only top-level `Const`, `Dim`, `Type`, `Class`, `Enum`, `Sub`, and `Function` declarations. Routine bodies retain the complete normal statement surface. The command-line form is:

```text
smilec Program.smile --source GameState.smile --source Drawing.smile -o Program.exe
smilec Program.smile --source GameState.smile --source Drawing.smile --target web --output-dir Web
```

In a `.smileproj`, ordinary sources become support sources. Complete alternative programs stay visible but are excluded unless selected through `<StartupFile>`:

```xml
<SmileSource Include="Program.smile" StartupOnly="true" />
<SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
<SmileSource Include="GameState.smile" />
```

In Visual Studio, use **Set as Startup** on either complete program; the project system changes `<StartupFile>`, retains both alternatives as `StartupOnly="true"`, refreshes the editor workspace, and marks the selection with `(Startup)`. Editing the XML directly remains valid for automation. When an unselected alternative is open, the editor analyzes it as a hypothetical startup plus the ordinary support files, excluding the selected complete program so its diagnostics remain meaningful. `examples\MultiFileBasics` demonstrates startup-to-support calls, support-to-support calls, shared constants and arrays, and a support routine reading a startup global on both Windows and Web.

## Package versions and milestone guides

The phase guides retain the rationale and evidence for each milestone. Current package writers and readers require format 7; an older guide describing format 6 does not authorize loading that obsolete format. Rebuild old libraries with the current compiler. See [SmileLibraries.cs](../../src/Smile.Language/SmileLibraries.cs).

Phase 5 adds `Text_Length`, `Text_Code_At`, and `Text_Slice`. Their zero-based indices and counts use Unicode scalar values rather than native UTF-8 bytes or Web UTF-16 code units. `Text_Code_At` returns `-1` outside the value, while `Text_Slice` safely clamps and returns empty text for negative starts, nonpositive counts, or starts beyond the end. Routine analysis also records direct and transitive `requiresGameWindow` capability; a Console consumer receives one `SML3704` at its own call site instead of diagnostics cascading from library source. Phase 5.2 adds the Unicode-safe menu overflow/marker/geometry and hierarchical-navigation foundation. Smile.UI 2.0 keeps those bounded engines private behind `Menu`, `MenuNavigator`, and `Dialogue` Class facades with constructors, methods, properties, named/default arguments, and idempotent destruction. Full details are in [phase5-ui.md](phase5-ui.md).

Phase 6 adds optional stable `ApplicationId` project identity and the source-authored `Smile.RPG` data/management package without adding syntax or RPG runtime helpers. Phase 6.1 advances Smile.RPG to 1.0.1 with save-boundary, rollback, asset-manifest identity, formatter-context, and Shop-result hardening. Phase 6.2 advances Smile.RPG to 1.0.2 with observational `SaveGames.Exists` query behavior while preserving SRPG format 1. See [phase6-rpg.md](phase6-rpg.md).

Phase 7 adds the ordinary `Smile.Game` source package and advances `Smile.RPG` with world, story, encounter-preview, and format-2 persistence modules. Smile.Game 2.0.0 now applies the shared Enum and Type-member language features to its cardinal movement and camera values without adding RPG-specific syntax or a Smile.RPG dependency. `Load Text File` accepts a `Text` expression path and dotted module names may contain the reserved `Game` segment. See [phase7-rpg-world.md](phase7-rpg-world.md).

Phase 7.1 advances `Smile.RPG` to 1.1.1 with world-state invariant and transactional save hardening. It changes no language syntax, SMILE-MAP fields, SRPG fields, or `.smilelib` package format.

Phase 8 adds no language syntax, native runtime primitive, package API, or file-format revision. It demonstrates dungeon exploration by composing the existing source-authored packages in the `RPGSystems` Dungeon option; see [phase8-rpg-dungeons.md](phase8-rpg-dungeons.md).

Phase 9 advances `Smile.RPG` to 1.2.0 with four ordinary deterministic battle modules and no new language syntax, compiler/runtime helper, rendering primitive, or persistence format. Active battles block Save/Load and remain transient. See [phase9-rpg-battles.md](phase9-rpg-battles.md).

The lightweight-OOP compatibility release rebuilds that unchanged fifteen-Module
surface as `Smile.RPG` 1.3.0. Current builds use deterministic `.smilelib` format 7. Version
1.3.0 adds opt-in progression-aware basic-Attack accuracy and miss cues. It adds no
RPG Class façade or Enum conversion, no Smile.UI/Smile.Game dependency, and no
SRPG save-payload revision.

The executable examples are the most precise usage guide: `LanguageBasics.smile`, `StructuredLanguageBasics.smile`, `GraphicsBasics.smile`, `MultiFileBasics`, and the projects under `games`. These include Dungeon Star I's external-map parser and quadrilateral-based pseudo-3D renderer, Dungeon Star II's fixed-point DDA raycaster, and Maze Muncher's arc-composed neon maze. Each demo game also includes a complete player-focused `Program-NoDemo.smile` teaching source.
