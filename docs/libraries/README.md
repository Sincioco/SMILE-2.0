# SMILE 2.0 libraries

A `.smilelibproj` declares `ProjectKind` `Library`, `LibraryName`, semantic `Version`, optional `OutputName`, `SmileSource` items, and optional `SmileProjectReference` or `SmileLibraryReference` items. It has no startup file and builds to `bin\Debug` or `bin\Release` as a `.smilelib`.

The package is a deterministic ZIP with a fixed timestamp, stable entry order, LF UTF-8 source, no compression, `manifest.json`, `api/public-symbols.json`, and `src/` entries. The manifest records format version 1, library identity, exported module names, dependency identities, source paths, and SHA-256 source hashes. Loading rejects duplicate, undeclared, executable, absolute, traversal, or malformed payloads before content enters analysis. Extracted source is cached under `obj\Smile\Libraries\<name>\<version>\<package-hash>`; changing package bytes selects a new cache directory.

Application and library projects may reference another `.smilelibproj` or a built `.smilelib`. One resolver in `Smile.Language` treats project sources and packages as exact providers, builds a deterministic dependency graph, validates dependent packages with all explicitly supplied dependencies present, and rejects implicit restore or provider fallback. Project compilations and loose `smilec --library` builds use this same resolver. Project references build dependency-first, reject cycles, and retain real source paths for native project-reference debugging. Package dependencies must be supplied explicitly at matching `major.minor.patch` versions.

Expected project and library failures use these shared diagnostics in the compiler and editor:

| Code | Meaning |
| --- | --- |
| `SML3200` | A referenced project, package, or required provider is missing. |
| `SML3201` | A normalized package path, library identity, or physical source has conflicting providers. |
| `SML3202` | The supplied provider version does not exactly match the dependency version. |
| `SML3203` | One manifest declares the same dependency more than once. |
| `SML3204` | A library declares itself as a dependency. |
| `SML3205` | A package dependency or project-reference graph contains a cycle; the message includes the cycle path. |
| `SML3206` | Project or package structure is malformed or unsupported. |
| `SML3207` | Package-owned sources, module metadata, or public API metadata fail authoritative dependency-aware validation. |
| `SML3209` | Project or package data cannot be read. |
| `SML3299` | Visual Studio recovered from an unexpected analysis failure and logged the exception. |

Dependency-resolution messages identify the dependent library and version, required provider and exact version, actual provider when present, and relevant physical paths. Package API validation compares only modules owned by that package, so dependency modules never leak into its public metadata.

In Visual Studio 2026, create **SMILE 2.0 Library** from File > New > Project. Every SMILE project shows References; **Add SMILE 2.0 Library Reference...** and **Remove Reference** refresh Solution Explorer and IntelliSense immediately. Focused watchers invalidate dependent analysis when reachable project files, project sources, or packages change, including restoration of a missing direct reference. Broken references keep local editing and diagnostics alive through the shared error above. Before a build or F5, Visual Studio saves every open physical source reachable through project references; extracted package-cache sources are never saved. Library F5 reports that the project is non-runnable. `IMPORT` offers reachable modules and `Alias.` offers public members only. The project file format advertises both `.smileproj` and `.smilelibproj`.

`examples\Phase2AHardening` contains the Base, Dependent, package-only consumer, and mixed project/package consumer fixture used for focused validation.

Future phases may add `Smile.UI.Window`, `Smile.UI.Menu`, `Smile.RPG.Inventory`, and `Smile.RPG.Abilities`. They are not implemented in Phase 2. Future RPG APIs and documentation use Magic Points (MP).
