# SMILE 2.0 libraries

A `.smilelibproj` declares `ProjectKind` `Library`, `LibraryName`, semantic `Version`, optional `OutputName`, `SmileSource` items, and optional `SmileProjectReference` or `SmileLibraryReference` items. It has no startup file and builds to `bin\Debug` or `bin\Release` as a `.smilelib`.

The package is a deterministic ZIP with a fixed timestamp, stable entry order, LF UTF-8 source, no compression, `manifest.json`, `api/public-symbols.json`, and `src/` entries. The manifest records format version 1, library identity, exported module names, dependency identities, source paths, and SHA-256 source hashes. Loading rejects duplicate, undeclared, executable, absolute, traversal, or malformed payloads before content enters analysis. Extracted source is cached under `obj\Smile\Libraries\<name>\<version>\<package-hash>`; changing package bytes selects a new cache directory.

Application and library projects may reference another `.smilelibproj` or a built `.smilelib`. Project references build dependency-first, reject cycles, and retain real source paths for native project-reference debugging. Package dependencies must be supplied explicitly at matching versions.

In Visual Studio 2026, create **SMILE 2.0 Library** from File > New > Project. Every SMILE project shows References; **Add SMILE 2.0 Library Reference...** and **Remove Reference** refresh Solution Explorer and IntelliSense immediately. Library F5 reports that the project is non-runnable. `IMPORT` offers reachable modules and `Alias.` offers public members only.

Future phases may add `Smile.UI.Window`, `Smile.UI.Menu`, `Smile.RPG.Inventory`, and `Smile.RPG.Abilities`. They are not implemented in Phase 2. Future RPG APIs and documentation use Magic Points (MP).
