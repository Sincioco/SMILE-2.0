# SMILE 2.0 Lightweight OOP Hardening Plan

## Status

Completed on August 17, 2026. The hardening work began from
`86016e815e1d71be195f29adcf4aa247386360a5` and preserved the existing lightweight-OOP
surface, package format 6, and official public library versions.

The governing model remains:

```text
Module = shared service, bounded engine, namespace, or intentional singleton
Type   = nominal inline deep-copy value
Class  = nominal reference object with identity
```

## Runtime Ownership

Native execution now maintains an active routine-frame chain independently from staged call
ownership. Each frame records its owned Text, Image, Type, Class, record, and clip resources.
Normal returns clean the current frame; `End Program`, runtime `Nothing`, and Class allocation
failure unwind every active frame newest-first. Staged receiver, property-right-hand-side,
constructor-argument, and partial-call values remain separately owned until transferred or
released, preventing both leaks and double releases.

Class cleanup is generated in one canonical reverse declaration order on native and Web.
Array-valued fields are cleared in reverse index order. This preserves deterministic nested
ownership while retaining reference-counted Class identity.

Class allocation failure is distinct from a `Nothing` dereference. Native tests may set the
internal `SMILE_CLASS_ALLOCATION_FAIL_AFTER` environment variable to fail a deterministic
allocation. The constructor body is skipped, all active and staged ownership is unwound, the
runtime emits a dedicated message, and the process exits with code 3. The mechanism is
diagnostic-only and is disabled by default.

## Library Lifecycle

`Smile.UI` Menu, MenuNavigator, and Dialogue remain generation-safe Class facades over bounded
private engines. Every successfully constructed facade must follow an explicit, idempotent
`Destroy()` path. ARC lifetime does not implicitly destroy the bounded engine. Official games,
galleries, and lifecycle fixtures now tear down successful facade allocations explicitly.

## Package and Tooling Boundaries

Format 6 remains the only accepted `.smilelib` format. Loading validates archive shape,
provider/source identity, public API metadata, visibility, hidden receiver/setter metadata,
constructors, properties, Optional defaults, capabilities, and source locations. Formats 1-5
remain rejected with rebuild guidance.

Parser recovery for malformed Enum, Type, Class, Property, constructor, Optional, named-argument,
and With constructs is bounded and preserves later declarations when a reliable boundary exists.
Formatter passes remain transactional and idempotent. Visual Studio analysis continues to use one
shared, unchanged-snapshot model; property Quick Info is static and never invokes a getter.

## Acceptance Policy

The focused hardening script, managed language/compiler suite, native runtime suites, formatter
suite, full smoke matrix, artifact verification, installed Visual Studio checks, and focused
native graphical interactions are required evidence. Subjective music/SFX quality and exhaustive
visual playthroughs remain human acceptance checks rather than automated assertions.

## Language-Surface Freeze

This hardening does not introduce a second object model or another public syntax wave. Keep the
current lightweight-OOP surface frozen while production usage accumulates. Any future addition
must start from a demonstrated game/library need and preserve the single language authority in
`src\Smile.Language`.

## Deferred Features

The following remain intentionally unsupported: inheritance, interfaces, generics, delegates,
lambdas, events, user finalizers, tracing garbage collection, Class-reference fields and cycles,
Class arrays, and wholesale migration of `Smile.RPG` state to Classes. A future deterministic
disposal construct should be considered only after repeated real-world demand is demonstrated.
