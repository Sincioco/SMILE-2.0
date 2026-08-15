# Runtime Variable Hover And Built-In Library Color Report

Date: August 15, 2026

## Outcome

SMILE 2.0 students can inspect a variable's current value from the existing editor Quick Info while Visual Studio is stopped at a breakpoint. Official SMILE 2.0 library and module symbols also use a separate, customizable teal classification so students can distinguish them from symbols they create.

## Runtime Variable Hover

- Debug builds emit source-mapped native helper functions whose named parameters mirror the SMILE variables visible at each executable statement.
- Number, Boolean, Text, Image, array, and record values are bridged into native debugger-readable parameters without changing the program's behavior.
- The Visual Studio Quick Info source evaluates eligible variables only while the debugger is in break mode.
- Static educational details remain available outside debugging and remain the fallback whenever a runtime value is unavailable.
- F10 continues to step between `.smile` source statements rather than displaying generated compiler source.

## Built-In Library And Module Presentation

- `Smile.UI` is registered in the shared language model as an official SMILE 2.0 built-in library.
- Imported aliases, types, functions, and other symbols supplied by that library use the `SMILE 2.0 Built-in Module or Library` editor classification.
- The default color is Visual Studio teal (`#2B91AF`), distinct from the default green comment color, and students can customize it in Visual Studio's Fonts and Colors settings.
- Student-created library and module symbols keep the normal identifier classification.
- Completion and Quick Info identify official providers as `SMILE 2.0 built-in library`.
- Semantic color resolution is limited to the editor span Visual Studio requests and uses the already-selected syntax token, keeping large built-in modules responsive.

## Live Visual Studio Evidence

The installed VSIX 2.0.36 was exercised with `examples\MenuGallery\MenuGallery.slnx` on the Windows 64-bit executable target.

- Visual Studio stopped on `Program.smile` line 74.
- Quick Info for `FontHandle` showed `Current Value 101 (__int64)`.
- F10 moved the current statement from line 74 to line 75 in `Program.smile`.
- Quick Info for the `Menu` alias identified `SMILE 2.0 built-in library Smile.UI@1.1.2`.
- Official aliases and their supplied types rendered in teal while ordinary variables retained the normal identifier color.
- Navigating directly to line 700 of the large built-in `MenuNavigator.smile` module remained responsive after the classification optimization, with no performance warning.

## Automated Validation

- `dotnet run --project src\Smile.Tests\Smile.Tests.csproj --configuration Debug`
  - 197 language, compiler, project, completion, and timing tests passed.
- `dotnet build src\Smile.VisualStudio\Smile.VisualStudio.csproj --configuration Debug`
  - Build succeeded with no warnings or errors.
- `scripts\build.cmd`
  - Compiler, native runtime, games, and VSIX artifacts built successfully.
- `scripts\verify-artifacts.ps1`
  - Native game, asset, VSIX payload, version, viewport, and DPI checks passed.
- `scripts\install-vsix.cmd`
  - VSIX 2.0.36 installed and the installed assembly version and hash were verified.
