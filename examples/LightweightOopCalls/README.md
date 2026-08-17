# Lightweight OOP Call Proof

This reusable fixture proves Optional `ByVal` parameters and Visual Basic-style named arguments through both a project reference and a built format-6 package reference. `LightweightOopLibrary.smilelibproj` publishes `Smile.Lightweight.Oop.Proof` 1.0.0 from the nested archive source `src/Library/Api.smile`.

`Program.smile` covers omitted Number, Boolean, Text, and Enum defaults; reordered named arguments; mixed positional and named arguments; and exact-once explicit argument evaluation in source order before declaration-order ABI mapping. The Enum default deliberately names an alias so package metadata must retain both the declared member and its numeric value.

The library is intentionally independent of the six official packages. Later Type/Class call and package metadata proofs can extend this fixture without changing an official public API merely for test coverage.
