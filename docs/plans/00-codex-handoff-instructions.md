# Codex Handoff Instructions

## Copy-paste master instruction

Paste the following into Codex after placing this folder in or beside the SMILE 2.0 repository.

```text
Work in the current SMILE 2.0 repository at D:\SMILE 2.0.

You are implementing the SMILE 2.0 and Dragonfall Visual Generation 2 plan. Use exactly one Codex agent and do not create subagents.

Before changing anything:

1. Read the repository-root AGENTS.md completely. It is authoritative.
2. Confirm the current branch, HEAD commit, working-tree status, and remote status.
3. Do not discard, overwrite, reset, or reformat unrelated user changes.
4. Read every markdown file in:
   docs\plans\smile-2.0-dragonfall-visual-generation-2-plan
   in the order listed by read-me-first.md.
5. Reconcile all proposed paths, APIs, resource limits, and tasks against the current repository. Current code and AGENTS.md override stale assumptions.
6. Record material reconciliation changes in a short implementation note under docs\implementation before coding.
7. Begin with the exact required capability Flag notice whenever the requested reusable capability does not yet exist.

Architecture rules:

- Evolve the existing custom Renderer3D. Do not replace it with Three.js, Babylon.js, Unity, Unreal, or another engine.
- Keep Windows Direct3D 11 as priority one and browser WebGL2 as priority two.
- Use pure JavaScript in the generated Web runtime. Do not add TypeScript, a framework, npm, or a browser package manager.
- Preserve Renderer2D as the permanent final HUD/menu/text layer.
- Preserve GDI behavior, the educational wireframe Simple3D API, SM3D v1 compatibility, and current games.
- Keep glTF/GLB conversion offline in smileasset.exe. Do not add runtime glTF loading.
- Do not add Dragonfall-specific compiler or native-runtime shortcuts.
- Keep fixed capacities, generation-safe handles, ownership validation, useful diagnostics, deterministic update rules, and zero unbounded hot-path growth.
- Use the smallest correct implementation. Avoid speculative WebGPU, a general scene graph, physics, a shader language, editor infrastructure, or unrelated refactors.
- Follow existing SMILE formatting and Visual Basic-style capitalization.
- Preserve Program-NoDemo.smile for any demo/attract-mode game path.

Execution rule:

Implement only the milestone named at the bottom of this prompt. Complete that milestone rather than stopping at planning or scaffolding. Do not begin the next milestone.

For the selected milestone:

1. Inspect all affected code before editing.
2. Add or update focused native and Web tests.
3. Implement the reusable capability in the proper general layer.
4. Add a small deterministic fixture or example that proves the capability.
5. Update relevant API and architecture documentation.
6. Run the focused tests for the milestone.
7. Run existing adjacent regression gates.
8. Run formatting/style checks on touched SMILE sources.
9. Build both native Windows and Web outputs where the milestone affects rendering.
10. Manually inspect the visible result when the milestone has visual output.
11. Commit with a focused message and push to the active branch, as required by AGENTS.md.
12. Report the commit hash, changed files, commands run, results, remaining limitations, and the next milestone that is now unblocked.

Do not claim a visual or performance result that was not actually observed or measured.

SELECTED MILESTONE: <replace-with-one-of-M0-through-M8>
```

## Recommended first run

Use the dedicated **M0 prompt** from `14-codex-milestone-prompts.md`. M0 is a reconciliation and baseline milestone, but it must still produce useful committed outputs: implementation notes, deterministic fixtures where missing, baseline diagnostics, and a focused gate for the later work.

Do not start M1 until M0's report identifies the final exact code paths and confirms all adjacent tests are green.

## Required report format

Codex should end each milestone with:

```text
Milestone:
Status:
Branch:
Starting commit:
Ending commit:
Pushed:

Implemented:
- ...

Compatibility preserved:
- ...

Files changed:
- ...

Validation commands:
- ...

Validation results:
- ...

Manual visual checks:
- ...

Measured performance or resource counts:
- ...

Known limitations:
- ...

Plan deviations and rationale:
- ...

Next unblocked milestone:
```

## Handling repository drift

If current `main` is newer than the baseline in this package:

- do not reset to the baseline;
- inspect all intervening Renderer3D, Simple3D, Battle3D, AssetTool, compiler, Web runtime, and Dragonfall changes;
- update the implementation note with the new baseline;
- preserve newer decisions;
- adjust task file paths and names;
- continue only when the selected milestone still has a coherent, bounded scope.

If the working tree has unrelated changes:

- preserve them;
- avoid touching their files unless required;
- state any unavoidable overlap before editing;
- never use destructive cleanup commands.

## Dependency policy

A new dependency requires explicit proof that the current code cannot reasonably provide the capability. The default is **no new runtime dependency**.

Permitted standard platform technologies include:

- Direct3D 11 and existing Windows graphics facilities already used by the repository;
- WebGL2 and browser APIs already used by the generated runtime;
- .NET libraries already available to `Smile.AssetTool`;
- repository-owned pure JavaScript.

The plan does not authorize:

- Three.js;
- Babylon.js;
- Unity or Unreal runtime integration;
- Assimp;
- npm packages;
- TypeScript;
- WebGPU as a required backend;
- runtime Blender or glTF parsing.

## Asset policy

The final hero must be original or properly licensed. A single reference PNG is not a rigged production model.

Until an approved production GLB is available, Codex must:

- use generated, repository-owned, deterministic glTF/GLB fixtures for importer and animation tests;
- use a deliberately simple original skinned test character for the Character Lab;
- keep production-art integration isolated from engine completion;
- never silently download or commit third-party character assets.

See `13-production-character-asset-handoff.md`.
