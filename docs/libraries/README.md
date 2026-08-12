# SMILE 2.0 libraries

A `.smilelibproj` declares `ProjectKind` `Library`, `LibraryName`, semantic `Version`, optional `OutputName`, `SmileSource` items, and optional `SmileProjectReference` or `SmileLibraryReference` items. It has no startup file and builds to `bin\Debug` or `bin\Release` as a `.smilelib`.

The package is a deterministic ZIP with a fixed timestamp, stable entry order, LF UTF-8 source, no compression, `manifest.json`, `api/public-symbols.json`, and `src/` entries. The manifest records format version 1, library identity, exported module names, dependency identities, source paths, and SHA-256 source hashes. Loading rejects duplicate, undeclared, executable, absolute, traversal, or malformed payloads before content enters analysis. Extracted source is cached under `obj\Smile\Libraries\<name>\<version>\<package-hash>`; changing package bytes selects a new cache directory.

Application and library projects may reference another `.smilelibproj` or a built `.smilelib`. One resolver in `Smile.Language` treats project sources and packages as exact providers, builds a deterministic dependency graph, validates dependent packages with all explicitly supplied dependencies present, and rejects implicit restore or provider fallback. The shared dependency context allows same-provider imports and direct project/package edges only. An application cannot import a transitive provider, a library project cannot use an ambient sibling provider, and package-owned sources can use only exact dependencies declared by that package manifest. A loose root and its `--source` files may use every package supplied directly with repeated `--library`, while package sources retain their manifest boundaries. `IMPORT` completion uses the same access decision.

Project compilations and loose `smilec --library` builds use this same resolver. Project references build dependency-first, reject cycles, and retain real source paths for native project-reference debugging. Package dependencies must be supplied explicitly at matching `major.minor.patch` versions. An existing project-reference output is reused only when its package format, library name/version, owned modules, normalized source entry paths and SHA-256 hashes, exact direct dependency identities, and derived public API metadata match the current project; timestamps alone are not authoritative.

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
| `SML3208` | An import resolves to a provider that is not the importing source's own provider or a declared direct reference. |
| `SML3209` | Project or package data cannot be read. |
| `SML3299` | Visual Studio recovered from an unexpected analysis failure and logged the exception. |

Dependency-resolution messages identify the dependent library and version, required provider and exact version, actual provider when present, and relevant physical paths. `SML3208` is attached to the physical `IMPORT` module-name span. Expected project and package failures use `<path>(1,1): error SML32xx: <message>` when no source span exists and return compiler exit code 1; exit code 2 is reserved for usage or infrastructure failures. Package API validation compares only modules owned by that package, so dependency modules never leak into its public metadata.

In Visual Studio 2026, create **SMILE 2.0 Library** from File > New > Project. Every SMILE project shows References; **Add SMILE 2.0 Library Reference...** and **Remove Reference** refresh Solution Explorer and IntelliSense immediately. Focused watchers use tolerant graph discovery and retain last-known participating paths, newly discovered paths, missing reference paths, and existing parent directories. Direct or transitive project/package restoration and atomic package replacement therefore reanalyze without a solution reload. Broken references keep local editing and the stable shared diagnostic alive. Visual Studio preflight and compiler diagnostics use the same path/line/code/message form in Output and Error List and clear stale build entries. Before a build or F5, Visual Studio saves every open physical source reachable through project references; extracted package-cache sources are never saved. Library F5 reports that the project is non-runnable. `IMPORT` offers only same-provider and directly accessible modules, while `Alias.` offers public members only. The project file format advertises both `.smileproj` and `.smilelibproj`.

`examples\Phase2AHardening` contains the Base, Dependent, package-only consumer, and mixed project/package consumer fixture used for focused validation. `Smile.Tests` adds Phase 2B ambient/transitive boundary, completion, package-manifest, fingerprint, diagnostic, and tolerant three-level recovery fixtures.

Future phases may add `Smile.UI.Window`, `Smile.UI.Menu`, `Smile.RPG.Inventory`, and `Smile.RPG.Abilities`. They are not implemented in Phase 2. Future RPG APIs and documentation use Magic Points (MP).
