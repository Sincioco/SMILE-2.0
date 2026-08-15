# Phase 2 invalid fixtures

Each remaining named folder isolates a module, access, or source-validation failure that is still used by the automated checks. Package and dependency-graph failures are constructed in temporary directories by `Smile.Tests`, so generated packages and redundant manual fixtures are not committed.

Expected diagnostics are asserted by `Smile.Tests` and `scripts\smoke-test.cmd`.
