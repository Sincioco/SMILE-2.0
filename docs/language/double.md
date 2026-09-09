# Double in SMILE 2.0

`Double` is a distinct eight-byte IEEE binary64 type. `Number` keeps its existing
integer meaning. Continuous calculations use Double; IDs, handles, indices,
counts, enum values, event ordering and persistence counters stay integral.

```smile
Option Explicit

Dim PositionX As Double
Dim Speed As Double
Dim ElapsedMilliseconds As Number
Dim ElapsedSeconds As Double

PositionX = 0.0
Speed = 0.125
ElapsedMilliseconds = 16
ElapsedSeconds = ToDouble(ElapsedMilliseconds) / 1000.0
PositionX = PositionX + Speed * ElapsedSeconds
Print PositionX
```

The result is approximately `0.002` (this example uses absolute tolerance `1e-12`).
Plain integer literals remain Number. Decimal/exponent literals such as `0.125`
and `1e-3` are Double. A decimal point needs digits on both sides; `.5`, `1.`,
`1e`, `1e+` and nonfinite literals are errors. Unary signs are expressions.
The type name remains contextual: an existing `Math.Double(...)` routine works.
User routines retain precedence over newly introduced intrinsic names.

## Operations and errors

Double supports same-type `+`, `-`, `*`, `/`, unary signs and exact comparisons.
There is no implicit numeric conversion in assignment, comparison, arithmetic,
parameters or ByRef. `ToDouble(1 / 2)` is `0.0`; `ToDouble(1) / 2.0` is `0.5`.
Equality does not insert a tolerance. Signed zeros compare equal, but their signs
survive storage, calls and text round trips. The default is positive `0.0`.
Finite subnormal results and underflow to signed zero are permitted.

| Function | Argument/result contract |
|---|---|
| `ToDouble(Value)` | Number to Double; nearest binary64 rounding |
| `ToNumber(Value)` | Double to Number; truncate toward zero, then range check |
| `Abs(Value)`, `Min(First, Second)`, `Max(First, Second)` | Same-type Number or Double; same result type |
| `Clamp(Value, Minimum, Maximum)` | Double; rejects inverted bounds |
| `Sqrt(Value)` | Double; rejects negative input |
| `Sin(Value)`, `Cos(Value)`, `Atan2(Y, X)` | Double; radians |
| `Floor(Value)`, `Ceiling(Value)`, `Truncate(Value)` | Double result; mathematical floor/ceiling/truncation |
| `Round(Value)` | Double result; nearest, ties to even |
| `Text_From_Double(Value)` | Double to invariant, round-trip-capable Text |
| `Text_To_Double(Value)` | Complete invariant decimal/exponent Text to Double |

Min/Max preserve the first argument on a tie, including signed zero. Clamp
preserves its value when it is within the inclusive bounds. Atan2 uses signed-zero
quadrants: `Atan2(+0.0, +0.0)` is +0, `Atan2(-0.0, +0.0)` is -0,
and negative-zero X gives +pi or -pi according to Y's sign. Floor, ceiling,
truncate and round preserve a zero result's mathematical sign where applicable.
Scalar trig is in radians; public 3D rotations remain degrees through explicit
adapters. Intrinsics use positional arguments; ordinary routines also support
the existing Optional and named argument rules.

Nonfinite results, division by either zero, invalid domains and conversions fail
with the actual SMILE source location (`SML3902` on native/checked intrinsic
constant diagnostics; nonconstant constant-expression errors retain their code).
The failing result is checked before destination mutation. Earlier source-order
effects remain; this is not a transaction over the program. Runtime termination
uses normal staged-call/frame/global cleanup, including owned fields. Literal
errors use `SML3900`; mixed numeric expressions and exact numeric/precision
intrinsic argument errors use `SML3901`. These codes belong to the shared
`DoubleSemantics` definitions. ApplicationId retains `SML3800` (invalid identity),
`SML3801` (duplicate identity) and `SML3802` (identity in a library project).
Existing typed assignment, parameter and integral-only diagnostics retain their
own codes. Web runtime failures retain their message and source-location format
without a numeric code prefix.

Native ToNumber accepts the truncated interval `[-2^63, 2^63)`. Web accepts only
the existing Number safe-integer interval `[-9007199254740991, 9007199254740991]`.
Converting large native Number values to Double can lose integer precision.
Neither target accepts NaN or Infinity as SMILE values. Approximate results are
not suitable for decimal accounting. CRT and JavaScript transcendental results
need not be bit-identical; the bounded fixture uses `1e-14` for sin/cos at 0.5.

Text parsing permits surrounding ASCII space and U+0009 through U+000D, an optional
sign, leading digits, an optional fraction and exponent. It rejects empty/partial
text, hexadecimal, separators, NaN and Infinity. Formatting preserves `-0.0` and
uses enough precision for numeric round trips. Exponent spelling can differ
between native and Web; it is not a locale-sensitive persistence format.

## Support and compatibility

| Surface | Double support |
|---|---|
| Globals, locals, constants, arrays, nested fixed-array records | Supported, eight-byte native storage |
| Classes, properties, copies, calls, Optional/named arguments, exact ByRef | Supported with existing ownership and evaluation order |
| Print and Select Case | Supported; Select uses exact same-type comparisons and rejects duplicate zeros |
| Mod, For controls, dimensions/indices, enums, byte Data buffers | Number/Enum only; source-located errors reject Double |
| Save/Load values and Save/Load Data | Existing integer/storage contract unchanged; explicit conversion or explicit invariant Text serialization is required at a separate boundary |
| Old console Input/Data-list/Read statements | Not implemented in SMILE 2.0; existing parser diagnostics remain. Forward-porting is a separate feature, not part of Double |
| Formatter, completion, routine/field Quick Info and navigation | Shared semantic Double facts, typed return temporaries and Optional defaults |
| Native debugging | Scalar/array `double`, typed record/class views and eight-byte values; generated C/PDB source mapping retains original paths |

Package schema **7** adds the Double primitive and unambiguous invariant string
encoding for Double constants/defaults (including `-0.0`). Old schemas 1–6 and
unknown future schemas are explicitly rejected with `SML3206` and rebuild
guidance. Rebuild source-owned `.smilelibproj` packages with the current compiler;
do not relabel old packages or reuse stale fingerprints. Unchanged Number source
recompiles with its original meaning. SMILE 1.0 alignment should consume the
shared SMILE 2.0 core and explicit-conversion contract; no parallel compiler was added.

The native internal SMILE call convention still carries each scalar as an
eight-byte payload in its existing GP-register/stack/RAX slots. Double arithmetic
uses floating registers, never integer arithmetic. Typed external Windows x64
adapters use XMM registers by argument position and XMM0 for floating returns;
ByRef is a pointer. Hidden aggregate results/receivers retain the existing internal
layout. This transport is an implementation detail, not a source-level bit-cast.
Native debug aggregate members preserve ASCII names as `Field_<name>`. Unicode
members use an ASCII-safe spelling plus their field ordinal and, when needed,
additional underscores to avoid every literal or generated member name in that
aggregate. Generated C comments retain the original SMILE name. These helper
names are deterministic debugger presentation; source names, offsets, array
dimensions and executable values remain unchanged.
The numeric translation unit uses `/fp:strict`, with no new fast-math, contraction,
reassociation or flush-to-zero setting. Web composes small numeric helpers into
the same runtime; Number guards and Enum BigInt remain distinct.

Owners and current source/artifact validation evidence are recorded in the
[Double checkpoint](../implementation/double-precision-checkpoint.md).
`scripts/test-double.ps1` compiles actual native/Web programs and package consumers,
checks deterministic output, conversion limits and checked errors with live owned
resources. No generated assembly is patched by the tests.
