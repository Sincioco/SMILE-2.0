# Language diagnostics

[Language reference](README.md) · Stable, source-located diagnostic codes for typed language features.

## Typed declarations and routines

| Code | Meaning |
|---|---|
| `SML3300` | `Option Explicit` is late or duplicated. |
| `SML3301` | Reserved for compatibility with pre-record typed diagnostics. |
| `SML3302` | A scalar `Dim` omits `As Type`. |
| `SML3303` | `Option Explicit` requires a declaration. |
| `SML3304` | Assignment, argument, case, or return types do not match. |
| `SML3305` | A `ByRef` argument is not an exact-type writable location. |
| `SML3306` | A routine duplicates a parameter or local. |
| `SML3307` | A local is used before its `Dim`. |
| `SML3308` | `Text` is used with an unsupported or mixed-type operator. |
| `SML3309` | A legacy function has inconsistent inferred return types. |
| `SML3310` | A typed declaration or return-type context is unsupported. |

Record-specific diagnostics are stable and source-located:

| Code | Meaning |
|---|---|
| `SML3400` | A `Type` is duplicated. |
| `SML3401` | A record type reference is unknown, or a module uses an external type without explicit `Alias.Type` qualification. |
| `SML3402` | A field is duplicated or malformed. |
| `SML3403` | A `Type` is misplaced or a field uses an unsupported form. |
| `SML3404` | Nested value types form a recursive layout cycle. |
| `SML3405` | A field does not exist on the record type. |
| `SML3406` | Field access is applied to a non-record value. |
| `SML3407` | A whole record is used in an unsupported operation. |
| `SML3408` | An imported record type is private or inaccessible. |
| `SML3409` | A public API exposes an inaccessible nominal type. |
| `SML3410` | A type is used as a value, or a value as a type. |
| `SML3411` | A record layout exceeds the supported size. |
| `SML3412` | A `With` target has a record type but is not a stable writable location. |
| `SML3413` | Leading-dot member access is used outside `With...End With`. |
| `SML3414` | `Call .Member(...)` names no callable member on the active `With` target. |
| `SML3415` | A `With` target is neither a Type value nor a Class reference. |

Enum-specific diagnostics are stable and source-located. Exact assignment, argument, case, return, and `ByRef` mismatches continue to use the shared `SML3304` and `SML3305` diagnostics; duplicate numeric aliases in one `Select Case` use `SML3019`.

| Code | Meaning |
|---|---|
| `SML3420` | An enum nominal name is duplicated, or an `Enum` declaration is misplaced. |
| `SML3421` | An enum is empty, a member is malformed, or a case-insensitive member name is duplicated. |
| `SML3422` | An explicit member value is not a checked compile-time signed 64-bit `Number`, or an implicit successor overflows. |
| `SML3423` | A named member does not exist on the enum type. |
| `SML3424` | An enum is used with an unsupported operator or compared with a different enum type. |

Optional-parameter and named-argument diagnostics are stable and source-located. Exact argument-type and `ByRef` location mismatches continue to use `SML3304` and `SML3305`.

| Code | Meaning |
|---|---|
| `SML3430` | An Optional declaration is malformed, uses `ByRef`, omits its explicit type/default, or precedes a required parameter. |
| `SML3431` | An Optional default is unsupported or is not a compile-time value of the exact declared type. |
| `SML3432` | A positional argument follows a named argument. |
| `SML3433` | A named argument names no parameter, or a built-in function is called with a named argument. |
| `SML3434` | A parameter is supplied more than once. |
| `SML3435` | A required parameter is omitted. |

The Web target additionally reports `SML5102` when an Optional `Number` default is outside JavaScript's exact safe-integer range. Enum defaults use `BigInt` and retain the complete signed 64-bit range.

Type/Class member diagnostics are stable and source-located:

| Code | Meaning |
|---|---|
| `SML3440` | A Type member collides, a Type field is Private, or visibility syntax is malformed. |
| `SML3441` | A Type/Class Property or accessor is malformed. |
| `SML3442` | `Me` is used outside an instance member or as an assignable/`ByRef` whole value. |
| `SML3443` | An instance member is missing, noncallable, or used on a non-instance receiver. |
| `SML3444` | A Type method/property receiver is not an addressable stable location. |
| `SML3445` | A Property read lacks `Get`, or a Property assignment lacks `Set`. |
| `SML3446` | A Private member is accessed outside its exact containing Type or Class. |
| `SML3450` | A Class declaration or member statement is malformed or misplaced. |
| `SML3451` | A constructor is invalid, duplicated, Private, or collides in the Class member namespace. |
| `SML3452` | A Class field/layout or scalar-only Class storage form is unsupported. |
| `SML3453` | `New` or `Dim As New` does not name a constructible Class. |
| `SML3454` | `Nothing` is assigned or returned where the exact Class-compatible type is not allowed. |
| `SML3455` | Class identity operands are incompatible, or `=`/`<>` is used instead of `Is`/`Is Not`. |
| `SML3456` | Reserved for future Class storage diagnostics. |
| `SML3457` | Member access is known at compile time to use literal `Nothing`. |
