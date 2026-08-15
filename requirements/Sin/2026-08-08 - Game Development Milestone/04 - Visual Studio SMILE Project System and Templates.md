# Visual Studio SMILE Project System and Templates

## Goal

A student can use Visual Studio 2026's Create a new project screen to create, edit, build, and run SMILE programs.

Extend the existing VSIX. Do not replace the working editor integration.

## Templates

```text
SMILE 2.0 Console Application
SMILE 2.0 Game Application
```

## Console template

```text
MyConsoleApp\
├── MyConsoleApp.smileproj
└── Program.smile
```

```smile
Print "Hello World"
```

## Game template

```text
MyGame\
├── MyGame.smileproj
├── Program.smile
└── Assets\
```

```smile
Game Window "My SMILE Game"

Do
    Clear Rgb(20, 20, 35)
    Draw Text "Hello, SMILE!" At 480, 240 Size 42 Color YELLOW Centered
    Show Screen
    Wait 16 Milliseconds
Loop Until Game_Closed() = True
```

## Minimal project format

```xml
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Game</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>MyGame</OutputName>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" />
    <Asset Include="Assets\**\*" />
  </ItemGroup>
</SmileProject>
```

One startup source file for this milestone.

## Required project behavior

- Open `.smileproj`.
- Show project, Program.smile, and Assets in Solution Explorer.
- Use the existing SMILE editor/content type.
- Build through Ctrl+Shift+B.
- Run through Ctrl+F5.
- F5 may build/run without source debugging.
- Copy assets preserving relative paths.
- Report errors in Error List and SMILE Output.
- Keep Tools > Build SMILE File for loose files.

## Build outputs

```text
bin\Debug\<OutputName>.exe
bin\Release\<OutputName>.exe
```

Game output has no console. Console template produces a console executable.

## Shared language services

Highlighting and diagnostics come from `Smile.Language`. Do not maintain a second keyword list or parser.

## Packaging

Package the project system, templates, compiler, and required runtime components in the existing VSIX. Update VSIX version and description.

## Acceptance

Create both templates in a clean/experimental Visual Studio instance, edit code, see shared diagnostics, build, run, and verify asset copying.
