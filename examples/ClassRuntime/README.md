# Class Runtime Proof

This focused Console fixture exercises the initial SMILE `Class` reference model on the native and Web targets. `Program.smile` covers explicit and implicit constructors, optional and named constructor arguments, source-order global initialization, aliasing, `Nothing`, `Is`/`Is Not`, Class methods and properties, `With` identity capture, fixed one- and two-dimensional fields, collision-prone Web field names, and finalization of scalar/array Text and Type fields. Its exact output is in `ClassRuntime.expected.txt`.

`ClassEndProgramCleanup.smile` proves that staged and frame-owned Class references are released when `End Program` occurs in a nested method. `ClassWebOwnership.smile` proves that a Class-owned Type containing an Image releases the retained Image through the generated Class finalizer. `ClassNothingFailure.smile` proves deterministic native/Web failure when a member is accessed through `Nothing`.

Native lifetime validation sets `SMILE_CLASS_LIFETIME_DIAGNOSTICS=1` and requires `SMILE_CLASS_LIVE=0`; Text-owning cases also require `SMILE_TEXT_LIVE=0`. The Web runner requires both `smile.classLiveCount()` and `smile.mediaDiagnostics().classLiveCount` to be zero.
