# Phase 2 Language Syntax

## 1. New shared tokens and keywords

Add shared language support for:

```text
Module
Import
As
Public
Private
.
```

Use one authoritative lexer/parser implementation in `src\Smile.Language`.

`End Module` closes a module.

SMILE remains case-insensitive.

---

# 2. Module source

A module source has this form:

```smile
Module Smile.Math.Extras

Public Const VERSION_MAJOR = 1

Public Function Clamp(Value, MinimumValue, MaximumValue)
    Return Max(MinimumValue, Min(MaximumValue, Value))
End Function

Private Function Identity(Value)
    Return Value
End Function

End Module
```

Rules:

- `Module` is the first non-comment statement.
- The module name has one or more identifiers separated by dots.
- Exactly one module block is allowed per physical source.
- `End Module` is mandatory.
- Nothing except comments may follow `End Module`.
- Module sources cannot contain:
  - `Game Window`;
  - top-level executable statements;
  - `End Program`.
- A module may span several physical files by repeating the same module name.
- Every physical source retains its own path, lines, diagnostics, and debug locations.

---

# 3. Imports

Import syntax:

```smile
Import Smile.Math.Extras As Math
```

The alias is mandatory in Phase 2.

In a startup or legacy support source, imports appear before declarations or executable statements:

```smile
Import Smile.Math.Extras As Math
Import Smile.Validation As Validation

Const MaximumValue = 100
```

In a module source, imports appear immediately after `Module` and before module declarations:

```smile
Module Smile.Game.Score

Import Smile.Math.Extras As Math

Public Function NormalizeScore(Value)
    Return Math.Clamp(Value, 0, 999999)
End Function

End Module
```

Imports are physical-source scoped. A second source repeats the import when it uses the alias.

Do not add:

```text
wildcard imports
implicit imports
global using/import files
multiple spellings
Import without As
```

---

# 4. Qualified member access

Supported forms:

```smile
Math.VERSION_MAJOR
Math.Clamp(Value, 0, 100)
Call Save.Reset()
Inventory.Items[ItemIndex]
```

Phase 2 supports exactly:

```text
Alias.Member
```

for imported module members.

Do not add arbitrary object/member chains such as:

```text
A.B.C.Member
```

The dotted module name exists only in declarations/imports; source use goes through its local alias.

---

# 5. Visibility modifiers

Allowed module declarations:

```smile
Public Const
Private Const
Public Dim
Private Dim
Public Sub
Private Sub
Public Function
Private Function
```

When omitted, module declarations are `Private`.

Example:

```smile
Module Smile.Math.Extras

Const InternalVersion = 1

Public Function Clamp(Value, MinimumValue, MaximumValue)
    Return Max(MinimumValue, Min(MaximumValue, Value))
End Function

End Module
```

`InternalVersion` is private.

`Public` and `Private` are invalid in legacy unmoduled program/support files during Phase 2.

---

# 6. Legacy compatibility

Sources without `Module` retain the current model:

- one startup owns executable top-level statements;
- ordinary support sources contribute legacy project-global declarations;
- alternate `StartupOnly` sources remain supported.

A project may contain:

```text
legacy startup/support sources
local module sources
referenced library modules
```

Do not force the ten existing games to adopt module syntax.
