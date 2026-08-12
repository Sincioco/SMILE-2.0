# Phase 2 Samples, Invalid Fixtures, Documentation, and Tests

## 1. Required proof library

Add:

```text
libraries\Smile.Math.Extras
```

The module spans at least two files:

```text
Clamp.smile
Range.smile
```

It exposes:

```text
VERSION_MAJOR
Clamp
IsBetween
```

and contains at least one private helper.

---

# 2. Required consumer

Add:

```text
examples\LibraryConsumer
```

The consumer imports:

```smile
IMPORT Smile.Math.Extras AS Math
```

It uses the same library for:

```text
Windows native
Web
```

Expected console values:

```text
100
TRUE
1
```

or the repository's normal numeric Boolean representation where applicable.

---

# 3. Local-module proof

Add:

```text
examples\LocalModuleBasics
```

The module source is inside the application project and imported without a `.smilelib`.

Compile native and Web.

---

# 4. Required invalid fixtures

Add focused invalid projects/files for:

```text
MissingModule
UnknownMember
PrivateMemberAccess
DuplicateAlias
DuplicateModuleProvider
ModuleImportCycle
LibraryTopLevelExecutable
LibrarySourceWithoutModule
MalformedSmileLibrary
UnsafeSmileLibraryPath
ProjectReferenceCycle
MissingLibraryReference
```

Assert diagnostic codes, physical files, and deterministic ordering.

---

# 5. Package tests

Test:

- deterministic package output;
- manifest fields;
- source hashes;
- public symbols metadata;
- private symbols excluded;
- safe extraction;
- malformed/unsafe package rejection;
- package hash invalidates cache.

---

# 6. Project-system tests

Test:

- `.smilelibproj` parsing;
- project factory recognition;
- library hierarchy;
- References node;
- add/remove references;
- missing-reference state;
- immediate live refresh;
- project close cleanup;
- build-order graph.

---

# 7. Regression suite

Run:

```bat
cmd /c scripts\build.cmd
cmd /c scripts\smoke-test.cmd
cmd /c git diff --check
```

Keep all ten normal/no-demo native and Web builds green.

Keep native graphics/audio checks green.

Keep Visual Studio source creation, startup selection, breakpoint binding, and F10 green.

---

# 8. Documentation

Update:

```text
AGENTS.md
root README
docs/language
compiler CLI usage
project/reference workflow
library package format
Visual Studio template/reference UX
sample READMEs
```

Document these future libraries without implementing them:

```text
Smile.UI.Window
Smile.UI.Menu
Smile.RPG.Inventory
Smile.RPG.Abilities
```

Use Magic Points/MP in future RPG documentation.
