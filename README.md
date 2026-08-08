# SMILE 2.0

SMILE 2.0 is a small structured BASIC-style language that compiles directly to native Windows x64 executables.

This repository contains one shared language implementation (`Smile.Language`), the native compiler (`smilec`), its native runtime, examples, and Visual Studio 2026 editing support.

## Prerequisites

- .NET SDK 10
- Visual Studio 2026 with Desktop development with C++ and Visual Studio extension development

## Build

Run `scripts\build.cmd` from a Developer Command Prompt or a normal command prompt. The script builds `SMILE 2.0.sln`, and generated files are placed under `artifacts`.

Build outputs:

- `artifacts\compiler\smilec.exe`
- `artifacts\vsix\Smile.VisualStudio.vsix`

## Compile

```text
artifacts\compiler\smilec.exe examples\Hello.smile
artifacts\compiler\smilec.exe examples\Snake.smile -o artifacts\games\Snake.exe
```

Use `--keep-temp` to retain generated MASM assembly and object files under `artifacts\temp`.

Run `scripts\smoke-test.cmd` to build the toolchain, compile and run Hello and the language basics example, and compile Snake. Snake gameplay remains a hands-on check.

## Visual Studio extension

Run `scripts\install-vsix.cmd` to rebuild the project and launch Visual Studio's extension installer for the generated VSIX.

The extension associates `.smile` files with the SMILE content type, highlights tokens produced by `Smile.Language`, reports the same syntax and semantic diagnostics as the command-line compiler, and adds **Tools > Build SMILE File**. The build command saves the active file and invokes the compiler bundled inside the VSIX.
