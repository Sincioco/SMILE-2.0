# Native SMILE Source Stepping Correction

## Mission

Correct native Windows debugging so that Visual Studio can step through the actual SMILE source after stopping at a breakpoint.

Windows native debugging remains priority number one.

Browser `.smile` breakpoints remain out of scope.

## Required visible behavior

Given:

```smile
Sub MoveMarker(Amount)
    SharedState[STATE_X] = SharedState[STATE_X] + Amount
    SharedState[STATE_X] = Max(MinimumMarkerX, SharedState[STATE_X])
    SharedState[STATE_X] = Min(MaximumMarkerX, SharedState[STATE_X])
End Sub
```

When a breakpoint stops on the first assignment:

- `F10` advances to the next executed SMILE statement;
- the active document remains `GameState.smile`;
- the yellow instruction pointer remains on a valid SMILE line;
- Source Not Available does not appear;
- generated C/C++, MASM, runtime source, and disassembly do not become the active source view;
- repeated `F10` continues through the routine and returns to the caller's next SMILE statement.

## Cross-file behavior

For:

```text
Program.smile
    Call MoveMarker(-4)

GameState.smile
    Sub MoveMarker(Amount)
```

Required:

- `F10` on the call steps over the SMILE routine and stops at the caller's next executed SMILE statement;
- a breakpoint inside the routine still binds and hits;
- after a breakpoint inside the routine, `F10` and the eventual return remain in SMILE source;
- `F11`, if supportable with the existing native debugger without a large custom engine, enters the support routine;
- `Shift+F11`, if supportable, returns to the caller's SMILE source.

`F10` correctness is mandatory. Do not create a large custom debug engine merely to add perfect `F11` in this corrective milestone.

## Control-flow coverage

Test stepping through representative:

- assignment;
- `If` / `Else If`;
- `For`;
- `Do` / `Loop`;
- `Select Case`;
- `Call`;
- function call;
- `Return`;
- `Exit For`;
- `Exit Do`;
- `Show Screen`;
- `Wait`;
- a support routine calling another support routine.

The debugger should present only executable SMILE statements that are actually reached.

## Source identity

Every sequence point must distinguish:

```text
absolute physical source path
line
column when available
native instruction range or equivalent statement identity
```

These are different locations:

```text
Program.smile line 26
GameState.smile line 26
Drawing.smile line 26
```

Do not use a line-only identity.

Do not map support code to the startup file.

Do not create a concatenated fake source file.

## Required root-cause investigation

First reproduce using a native Debug build with generated artifacts retained:

```bat
cmd /c artifacts\compiler\smilec.exe ^
  examples\MultiFileBasics\Program.smile ^
  --source examples\MultiFileBasics\GameState.smile ^
  --source examples\MultiFileBasics\Drawing.smile ^
  -o artifacts\games\MultiFileBasics\MultiFileBasics.exe ^
  --debug ^
  --keep-temp
```

Inspect:

- generated MASM around each debug site;
- generated debug helper source;
- helper object;
- linked PDB;
- instruction pointer when the breakpoint hits;
- instruction pointer after `F10`;
- call stack;
- source document selected by Visual Studio.

Record the confirmed root cause in the final report.

## Likely issue to verify

The current design appears to create one generated C helper per executable SMILE statement, map the helper through `#line`, and call it from MASM.

That can make a breakpoint bind in the helper. After the helper returns, the actual MASM instruction range may have no `.smile` sequence point, causing Source Not Available.

Verify this rather than assuming it.

## Preferred implementation direction

Use the smallest reliable mechanism supported by the installed Visual Studio 2026 native toolchain.

Investigate in this order:

1. Emit direct CodeView/PDB source mappings for the generated MASM instruction ranges, using the real `.smile` files.
2. Prefer MASM source/line directives such as the current toolchain's `.cv_file` / `.cv_loc` equivalents when they provide reliable Visual Studio stepping.
3. If direct MASM mapping is insufficient, generate a focused companion debug object or mapping that covers the actual statement instruction ranges.
4. Retain a helper/trampoline design only if it supports correct repeated `F10` and never exposes generated implementation files.

Once direct source mappings work, remove obsolete per-statement helper calls and debug-helper artifacts rather than keeping unnecessary runtime overhead.

## Unacceptable fixes

Do not:

- tell the user to press Continue instead of F10;
- automatically show disassembly;
- suppress the Source Not Available page without correcting mappings;
- map every instruction to one line;
- map all files to `Program.smile`;
- disable stepping;
- generate a fake concatenated source;
- claim success after testing only that a breakpoint binds;
- regress Release output;
- regress Web output;
- create a large custom debug engine without first proving the native engine cannot satisfy this focused requirement.

## Build behavior

### Debug Windows

Produce:

```text
<OutputName>.exe
<OutputName>.pdb
```

with usable `.smile` source mappings.

### Release Windows

Preserve normal Release behavior and avoid unnecessary debug overhead.

### Web

Preserve current Web output. Browser source stepping is not part of this milestone.

## Focused automated coverage

Add tests for:

- source path plus line identity;
- same line number in different files;
- normalized path handling;
- generated mapping entries for startup and support files;
- no line-only debug collisions;
- valid Debug Windows emission;
- no Release/Web regression.

Static tests do not replace the live Visual Studio test.

## Required live Visual Studio test

1. Install the refreshed VSIX.
2. Open `examples\MultiFileBasics\MultiFileBasics.slnx`.
3. Select `Debug | Windows 64-bit .exe`.
4. Set a breakpoint in `GameState.smile` inside `MoveMarker`.
5. Press `F5`.
6. Trigger the breakpoint.
7. Press `F10` through at least three executed SMILE statements and through routine return.
8. Confirm Source Not Available never appears.
9. Repeat in `Program.smile`.
10. Repeat in `Drawing.smile`.
11. Confirm the active file and yellow instruction pointer are correct.
12. Close and run F5 again to preserve repeated-F5 reliability.

## Debugger definition of done

All must be true:

- startup breakpoints bind and hit;
- support breakpoints bind and hit;
- `F10` remains in physical `.smile` source;
- routine return remains in physical `.smile` source;
- identical line numbers in different files remain distinct;
- generated helper/MASM source is not displayed;
- Source Not Available does not appear in the tested flow;
- native output remains PE32+ x64 with no CLR header;
- the normal smoke suite remains green.
