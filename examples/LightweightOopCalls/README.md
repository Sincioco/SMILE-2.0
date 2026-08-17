# Lightweight OOP Call Proof

This reusable fixture proves Optional `ByVal` parameters, Visual Basic-style named arguments, and Type instance members through both a project reference and a built format-6 package reference. `LightweightOopLibrary.smilelibproj` publishes `Smile.Lightweight.Oop.Proof` 1.1.0 from the nested archive source `src/Library/Api.smile`.

`Program.smile` covers omitted Number, Boolean, Text, and Enum defaults; reordered named arguments; mixed positional and named arguments; and exact-once explicit argument evaluation in source order before declaration-order ABI mapping. The Enum default deliberately names an alias so package metadata must retain both the declared member and its numeric value.

`Counter` proves methods, Functions, `Me`, read/write and read-only properties, Optional/named member calls, exact public-member package metadata, and accessor-specific Game Window capabilities. The consumers also prove deep-copy assignment, `ByVal` isolation, nested-field and array-element receivers, a method receiver whose root is replaced while its explicit argument is evaluated, and a property setter whose right-hand side replaces its receiver root before the receiver is resolved. Private Type helpers remain absent from package and editor surfaces.

The library is intentionally independent of the six official packages. Later Class call and package metadata proofs can extend this fixture without changing an official public API merely for test coverage.
