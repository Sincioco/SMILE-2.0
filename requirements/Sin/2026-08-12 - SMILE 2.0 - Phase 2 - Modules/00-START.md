# SMILE 2.0 Phase 2

## Modules, Imports, and Reusable Libraries

**Repository:** `D:\SMILE 2.0`
**GitHub:** `Sincioco/SMILE-2.0`
**Accepted baseline commit:** `d5736d797794aa946d9d41e25fb0f9994c92e15f`
**Accepted Visual Studio extension baseline:** `2.0.16`
**Execution:** autonomous, single-agent Codex

Phase 1 is accepted. The Visual Studio project system now supports multi-file projects, immediate source visibility, startup selection, cross-file IntelliSense, native source debugging, Windows native builds, and Web builds.

Phase 2 adds the minimum general-purpose language and tooling foundation for reusable components written in SMILE 2.0.

---

# Target outcome

A reusable library is written in SMILE:

```smile
Module Smile.Math.Extras

Public Function Clamp(Value, MinimumValue, MaximumValue)
    Return Max(MinimumValue, Min(MaximumValue, Value))
End Function

End Module
```

A SMILE application imports it through an explicit alias:

```smile
Import Smile.Math.Extras As Math

ClampedValue = Math.Clamp(150, 0, 100)

Print ClampedValue

End Program
```

The same library source works in:

```text
Windows native x64
Web
```

The library is developed independently, built as a target-neutral `.smilelib` package, and referenced by SMILE application or library projects.

---

# Phase 2 deliverables

1. `Module` and `End Module`.
2. `Import <dotted.module> As <alias>`.
3. `Public` and `Private` declarations in modules.
4. Qualified member access through `Alias.Member`.
5. Local modules in ordinary application projects.
6. `.smilelibproj` library projects.
7. Deterministic target-neutral `.smilelib` packages.
8. Project and package references.
9. Native and Web consumption from one semantic model.
10. Visual Studio library-project template and References node.
11. Module/import IntelliSense and diagnostics.
12. Native source debugging into project-referenced library source.
13. Proof libraries, consumers, invalid fixtures, documentation, tests, commit, and push.

---

# Permanent constraints

- Windows native remains priority 1.
- Web remains priority 2.
- `src\Smile.Language` remains authoritative.
- Do not add a second parser or semantic implementation.
- Do not turn `.smilelib` into a Windows DLL.
- Do not create an online package registry.
- Do not add game-specific runtime helpers.
- Keep the syntax line-oriented, case-insensitive, and beginner-friendly.
- Preserve all ten normal and no-demo games.
- Preserve live Solution Explorer refresh.
- Preserve breakpoints and F10.
- Preserve Web sound, DirectX/Direct2D, and GDI.

---

# Explicitly deferred

Do not implement these during Phase 2:

```text
Type
ByRef / ByVal
general mutable Text variables
dynamic arrays or collections
images and sprites
persistent data blocks
multiple sound-effect channels
Smile.UI.Menu
Smile.RPG.Inventory
Smile.RPG.Abilities
the Phantasy Star-inspired RPG
```

The first proof library is mathematical because the current scalar language surface can express it cleanly. Future UI and RPG libraries will build on the module/library foundation.

---

# Reading order

Read all files before editing:

1. `00-START.md`
2. `01-SYNTAX.md`
3. `02-SEMANTICS.md`
4. `03-LIB-FORMAT.md`
5. `04-COMPILER.md`
6. `05-VISUAL-STUDIO.md`
7. `06-EMIT-DEBUG-IDE.md`
8. `07-SAMPLES-TESTS.md`
9. `08-CODEX-RUN.md`
10. `09-ACCEPTANCE.md`

Then inspect all companion files under `repo`.

`CODEX.txt` contains the ready-to-paste handoff prompt.
