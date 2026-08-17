# Invalid Class Fixtures

These narrow sources keep the initial Class diagnostic contract executable. Standalone fixtures cover illegal Class members (`SML3450`), constructor/member namespace rules (`SML3451`), unsupported Class storage/layout (`SML3452`), invalid `New` targets (`SML3453`), invalid `Nothing` assignment (`SML3454`), Class equality in place of identity (`SML3455`), known-`Nothing` member access (`SML3457`), and the shared missing/private/accessor member diagnostics (`SML3443`, `SML3445`, and `SML3446`).

The capability projects consume `Smile.Lightweight.Oop.Proof` through both project and format-6 package references. A Game-window constructor, method, or getter must report exactly `SML3704` in a Console project. The corresponding safe setter projects compile and run on native/Web because their constructor and setter do not require a Game Window.

Runtime failure coverage lives in `examples\ClassRuntime\ClassNothingFailure.smile`; it must fail deterministically with `Object reference is Nothing` while leaving the native and Web Class live counts at zero.
