# Phase 3A.1 hardening fixtures

These sources preserve the Phase 3A grammar while proving invocation-local native compiler temporaries, cleanup-safe owned `Text`, exact native/Web behavior, and Unicode output.

- `RecursiveFor.smile` reproduces the Phase 3A static-limit defect: the reviewed baseline prints `3`; reentrant frame storage prints `6`.
- `RecursiveTextSelect.smile`, `ExitCleanup.smile`, `NestedCleanup.smile`, and `EndProgramCleanup.smile` exercise recursive selectors and non-local cleanup through `Return`, `Exit For`, `Exit Do`, and `End Program`.
- `Unicode.smile` proves redirected UTF-8 and Web logical output for Latin, Greek, Japanese, Chinese, and an emoji outside the BMP.
- `WebParity.smile` covers Text copy/concatenation/inequality/constants/arrays/returns, `ByVal`, `ByRef` scalars and array elements, and a five-parameter call. `Phase3ABasics` supplies the 0/1/4/8/16-parameter and library-reference matrix.

Each console source has a matching LF UTF-8 `.expected.txt` file for exact normalized native/Web comparison.
