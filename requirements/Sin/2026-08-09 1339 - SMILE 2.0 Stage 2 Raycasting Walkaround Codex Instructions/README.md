# SMILE 2.0 Stage 2 Raycasting Walkaround Codex Package

Start with:

```text
Codex-Instructions\00 - START HERE - Dungeon Star II Raycasting Walkaround.md
```

This package is designed against SMILE 2.0 commit:

```text
b5c4c66834c2132b89273eb56c6fc52cbde0fe29
```

If the repository is newer, Codex must preserve newer work and adapt.

The package contains:

```text
5 numbered Markdown instruction/specification files
1 package README
2 repository-ready student Markdown guides
2 repository-ready 31 x 31 map files
1 SHA-256 manifest
```

Ready-to-copy project files are under:

```text
Repository-Files\games\DungeonStarII
```

The supplied maps were validated when this ZIP was created for:

```text
31 x 31 dimensions
legal symbols
solid border
one start
complete connectivity
valid door orientation
```

The design intentionally requires no new SMILE language syntax.

It uses the current:

```smile
LOAD TEXT FILE "path" INTO Array COUNT Variable
```

plus fixed-point integer camera and DDA mathematics in `Program.smile`.
