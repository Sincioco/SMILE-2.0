# Phase 2 invalid fixtures

Each named folder isolates one required module, library, package, or dependency-graph failure. `MalformedSmileLibrary` is intentionally not a ZIP. `UnsafeSmileLibraryPath` records the malicious archive entry used by the package tests; the tests construct the ZIP in a temporary directory so no opaque binary fixture is committed.

Expected diagnostics are asserted by `Smile.Tests` and `scripts\smoke-test.cmd`.
