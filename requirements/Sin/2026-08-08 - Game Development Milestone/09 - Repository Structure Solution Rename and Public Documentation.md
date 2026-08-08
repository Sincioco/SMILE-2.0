# Repository Structure, Solution Rename, and Public Documentation

## Rename

Rename:

```text
SMILE.sln
```

to:

```text
SMILE 2.0.sln
```

Update every script, path, README instruction, and configuration. Quote paths containing spaces.

## Target structure

```text
AGENTS.md
README.md
SMILE 2.0.sln

docs\
examples\
    ConsoleSnake.smile
    GraphicsBasics.smile

games\
    Snake\
    FallingBlocks\
    PaddleBall\
    BrickBreaker\

requirements\Sin\2026-08-08 - Game Development Milestone\

scripts\
src\
    Smile.Language\
    Smile.Compiler\
    Smile.NativeRuntime\
    Smile.VisualStudio\

artifacts\
    compiler\
    vsix\
    games\
```

Do not add empty folders merely to match the diagram.

## Expected artifacts

```text
artifacts\compiler\smilec.exe
artifacts\vsix\Smile.VisualStudio.vsix
artifacts\games\Snake\Snake.exe
artifacts\games\FallingBlocks\FallingBlocks.exe
artifacts\games\PaddleBall\PaddleBall.exe
artifacts\games\BrickBreaker\BrickBreaker.exe
```

## README

Explain:

- what SMILE 2.0 is;
- native Windows x64 compilation;
- shared architecture;
- prerequisites;
- building `SMILE 2.0.sln`;
- installing VSIX;
- creating projects;
- compiling loose files;
- building/running games;
- 960×540 and Alt+Enter;
- language features;
- limitations;
- original asset provenance.

## Documentation integrity

Documentation must describe only implemented syntax. Do not commit external reference images, logos, sounds, fonts, sprites, or code.
