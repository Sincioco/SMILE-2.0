# Phase 2 Autonomous Codex Execution

## 1. Operating rules

1. Work only in `D:\SMILE 2.0`.
2. Use one agent only.
3. Read root `AGENTS.md`.
4. Read all Phase 2 files in numeric order.
5. Inspect the latest repository before editing.
6. Preserve unrelated user work.
7. Do not stop after planning/scaffolding.
8. Do not ask for intermediate confirmation unless genuinely blocked.
9. Use focused tests and the normal smoke suite.
10. Commit and push only after mandatory tests are green.

---

# 2. Implementation order

Implement in this order:

## A. Shared syntax

```text
MODULE
END MODULE
IMPORT
AS
PUBLIC
PRIVATE
dotted names
Alias.Member
```

## B. Shared semantic model

```text
Program/Library compilation kinds
module symbols
visibility
imports/aliases
dependency graph
provider conflicts
bound identities
```

## C. Emitters

```text
native symbol mangling/emission
Web symbol emission
module constants/arrays/routines
```

## D. Project/package model

```text
.smilelibproj
shared project/reference parsing
deterministic .smilelib writer/reader
safe package cache
```

## E. Compiler CLI/build graph

```text
--target library
--project
--library
project references
cycle detection
incremental dependency build
```

## F. Visual Studio

```text
SMILE 2.0 Library template
library project behavior
References node
add/remove reference
live reference/source refresh
IntelliSense/diagnostics
```

## G. Proof and regression

```text
Smile.Math.Extras
LibraryConsumer
LocalModuleBasics
invalid fixtures
native/Web/debug live checks
documentation
```

---

# 3. Testing style

Assume the happy path.

Use:

- targeted unit/integration tests;
- normal build;
- smoke suite;
- one short native library-consumer run;
- one short Web library-consumer run;
- one focused Visual Studio reference/IntelliSense/debug interaction.

Do not add CI or run exhaustive game playthroughs.

---

# 4. VSIX

Advance VSIX, assembly, and file versions together from the current baseline.

Install into Visual Studio 2026 Enterprise.

Verify installed DLL path/version/hash.

Perform live checks in the actual IDE.

---

# 5. Commit

When complete:

```bat
cmd /c git status --short
cmd /c git diff --check
cmd /c git add -A
```

Review the staged diff.

Use a subject beginning exactly:

```text
Sin and Codex:
```

Suggested subject:

```text
Sin and Codex: feat(language): add modules imports and SMILE libraries
```

Include a detailed body:

```text
Summary:
Changes:
Validation:
Known limitations:
```

Push normally.

Do not amend, rebase, force-push, or discard user work.

---

# 6. Final report

Report:

```text
Commit:
Branch:
VSIX path/version:
Syntax added:
Semantic model:
Library project/package:
Compiler CLI:
References/build graph:
Visual Studio UX:
IntelliSense:
Native result:
Web result:
Debugger result:
Focused tests:
Build:
Smoke:
Legacy games:
Known limitations:
```

End with a prominent bold statement saying whether manual testing is requested from Sin.
