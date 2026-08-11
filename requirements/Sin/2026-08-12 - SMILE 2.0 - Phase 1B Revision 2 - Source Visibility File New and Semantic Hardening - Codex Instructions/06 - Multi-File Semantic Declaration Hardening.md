# Multi-File Semantic Declaration Hardening

## Goal

Make project-level declarations independent of source-file order before modules and libraries are introduced.

The current source-visibility repair does not replace this compiler hardening.

---

# 1. Staged analysis

Implement the smallest clear stages:

```text
inventory project-level names
-> detect cross-category duplicates
-> resolve constants
-> detect constant cycles
-> resolve array dimensions
-> collect implicit startup globals
-> bind executable statements and routines
```

Do not sort filenames into a lucky order.

---

# 2. Cross-file constants

This must compile regardless of project order:

```smile
' Program.smile
DIM Party[MaximumPartySize]
```

```smile
' Settings.smile
CONST MaximumPartySize = BasePartySize + ExtraPartySlots
```

```smile
' BaseSettings.smile
CONST BasePartySize = 3
CONST ExtraPartySlots = 1
```

Resolve valid compile-time expression chains across files.

---

# 3. Circular constants

Detect and report deterministically:

```smile
CONST FirstValue = SecondValue + 1
CONST SecondValue = FirstValue + 1
```

Requirements:

- no infinite recursion;
- no stack overflow;
- real physical source location;
- clear circular-dependency message;
- deterministic diagnostics.

---

# 4. One project-level namespace

Project-level names are case-insensitive and shared across:

```text
CONST
DIM
implicit startup global
SUB
FUNCTION
```

Reject later collisions such as:

```smile
CONST Inventory = 64

FUNCTION inventory()
    RETURN 1
END FUNCTION
```

Preserve routine-local scope rules.

---

# 5. Required fixtures

Use the supplied:

```text
examples\MultiFileDeclarationHardening
examples\diagnostics\MultiFileCircularConstants
examples\diagnostics\MultiFileNameCollision
```

Compile the valid project for Windows and Web. Verify invalid projects fail with their intended diagnostics.
