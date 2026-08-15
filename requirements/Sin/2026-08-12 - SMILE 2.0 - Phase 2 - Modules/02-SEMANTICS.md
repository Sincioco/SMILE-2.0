# Phase 2 Semantic Model

## 1. Compilation kinds

Extend shared analysis with an explicit compilation kind:

```text
Program
Library
```

Program compilation:

- requires exactly one startup source;
- permits legacy support sources;
- permits local module sources;
- resolves referenced library modules.

Library compilation:

- has no startup source;
- requires every source to declare a module;
- rejects executable top-level statements;
- emits package/public API metadata instead of an executable.

---

# 2. Module symbols

Add a shared module symbol model containing:

- canonical case-insensitive module name;
- contributing syntax trees;
- imported aliases per source;
- public/private declarations;
- declaration source locations;
- package/project provider identity.

Several files declaring the same module merge into one module.

Example:

```text
Clamp.smile   -> Module Smile.Math.Extras
Range.smile   -> Module Smile.Math.Extras
```

They form one module named:

```text
Smile.Math.Extras
```

---

# 3. Module namespace

Each module has one case-insensitive declaration namespace across:

```text
Const
Dim
Sub
Function
```

Duplicate names across contributing files receive deterministic physical-file diagnostics.

Legacy program globals remain in the existing legacy program namespace.

Modules cannot directly read or mutate consuming-program globals.

Invalid module code:

```smile
Module Smile.Bad

Public Function ReadGameScore()
    Return Score
End Function

End Module
```

when `Score` belongs only to the consuming game.

Modules may reference:

- declarations in the same module;
- public declarations from imported modules;
- built-in constants/functions;
- routine locals and parameters.

---

# 4. Import aliases

Rules:

- aliases are case-insensitive;
- duplicate aliases in one source are rejected;
- importing the same module twice in one source is rejected;
- an alias cannot collide with a local declaration in an ambiguous way;
- missing module diagnostics point to the import;
- unknown member diagnostics point to the member;
- private-member access reports a visibility diagnostic, not a generic unknown-name error.

---

# 5. Public API

A module's public API includes:

```text
public constant name/type/value
public array name/rank/dimensions
public Sub name and parameters
public Function name, parameters, and return type
source metadata
```

Private declarations:

- are absent from consumer completion;
- are absent from public API metadata;
- remain available to all contributing files in the same module.

---

# 6. Dependency graph

Build a module import graph.

Reject circular imports deterministically:

```text
Smile.A imports Smile.B
Smile.B imports Smile.A
```

The diagnostic identifies the cycle and the physical import sites.

Several files contributing to one module are one graph node, not a cycle.

---

# 7. Provider conflicts

Reject:

- two referenced packages providing the same module;
- a local module and referenced module with the same canonical name;
- two versions/providers of the same module in one compilation.

Diagnostics identify both providers.

---

# 8. Stable bound identities

Create stable bound identities based on:

```text
module name
member name
member kind/signature when required
```

Native and Web emitters consume these bound identities.

Do not re-resolve imports independently in each emitter.
