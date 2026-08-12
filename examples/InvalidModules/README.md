# Phase 2 invalid fixtures

Each named folder isolates one required module, library, package, or dependency-graph failure. `MalformedSmileLibrary` is intentionally not a ZIP. `UnsafeSmileLibraryPath` records the malicious archive entry used by the package tests; the tests construct the ZIP in a temporary directory so no opaque binary fixture is committed. The temporary Phase 2B fixtures in `Smile.Tests` also cover ambient and transitive provider imports, undeclared package imports, direct-reference completion changes, stale/foreign project-library outputs, and missing transitive reference recovery without committing generated packages.

Expected diagnostics are asserted by `Smile.Tests` and `scripts\smoke-test.cmd`.
