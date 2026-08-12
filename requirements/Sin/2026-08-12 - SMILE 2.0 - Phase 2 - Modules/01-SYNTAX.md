# Phase 2 Language Syntax

## 1. New shared tokens and keywords

Add shared language support for:

```text
MODULE
IMPORT
AS
PUBLIC
PRIVATE
.
```

Use one authoritative lexer/parser implementation in `src\Smile.Language`.

`END MODULE` closes a module.

SMILE remains case-insensitive.

---

# 2. Module source

A module source has this form:

```smile
MODULE Smile.Math.Extras

PUBLIC CONST VERSION_MAJOR = 1

PUBLIC FUNCTION Clamp(Value, MinimumValue, MaximumValue)
    RETURN MAX(MinimumValue, MIN(MaximumValue, Value))
END FUNCTION

PRIVATE FUNCTION Identity(Value)
    RETURN Value
END FUNCTION

END MODULE
```

Rules:

- `MODULE` is the first non-comment statement.
- The module name has one or more identifiers separated by dots.
- Exactly one module block is allowed per physical source.
- `END MODULE` is mandatory.
- Nothing except comments may follow `END MODULE`.
- Module sources cannot contain:
  - `GAME WINDOW`;
  - top-level executable statements;
  - `END PROGRAM`.
- A module may span several physical files by repeating the same module name.
- Every physical source retains its own path, lines, diagnostics, and debug locations.

---

# 3. Imports

Import syntax:

```smile
IMPORT Smile.Math.Extras AS Math
```

The alias is mandatory in Phase 2.

In a startup or legacy support source, imports appear before declarations or executable statements:

```smile
IMPORT Smile.Math.Extras AS Math
IMPORT Smile.Validation AS Validation

CONST MaximumValue = 100
```

In a module source, imports appear immediately after `MODULE` and before module declarations:

```smile
MODULE Smile.Game.Score

IMPORT Smile.Math.Extras AS Math

PUBLIC FUNCTION NormalizeScore(Value)
    RETURN Math.Clamp(Value, 0, 999999)
END FUNCTION

END MODULE
```

Imports are physical-source scoped. A second source repeats the import when it uses the alias.

Do not add:

```text
wildcard imports
implicit imports
global using/import files
multiple spellings
IMPORT without AS
```

---

# 4. Qualified member access

Supported forms:

```smile
Math.VERSION_MAJOR
Math.Clamp(Value, 0, 100)
CALL Save.Reset()
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
PUBLIC CONST
PRIVATE CONST
PUBLIC DIM
PRIVATE DIM
PUBLIC SUB
PRIVATE SUB
PUBLIC FUNCTION
PRIVATE FUNCTION
```

When omitted, module declarations are `PRIVATE`.

Example:

```smile
MODULE Smile.Math.Extras

CONST InternalVersion = 1

PUBLIC FUNCTION Clamp(Value, MinimumValue, MaximumValue)
    RETURN MAX(MinimumValue, MIN(MaximumValue, Value))
END FUNCTION

END MODULE
```

`InternalVersion` is private.

`PUBLIC` and `PRIVATE` are invalid in legacy unmoduled program/support files during Phase 2.

---

# 6. Legacy compatibility

Sources without `MODULE` retain the current model:

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
