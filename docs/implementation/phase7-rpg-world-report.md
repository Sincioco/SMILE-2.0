# Phase 7 RPG world implementation report

> Phase 7.1 subsequently hardens the world-state and save invariants without beginning a later feature phase. See [the Phase 7.1 hardening report](phase7.1-world-state-invariant-hardening-report.md).

Phase 7 introduces `Smile.Game` 1.0.0 and advances `Smile.RPG` to 1.1.0. The permanent RPGSystems World option proves the complete reusable slice with original art: fixed-step walking, map and actor collision, exact camera clamping, wandering NPCs, menu-initiated multi-page/state-aware Talk, a walkable shop plus transactional purchase overlay, stats/inventory/party management, town/overworld scenes, visible followers, save/load, and a bounded Encounter Preview with exact return.

Focused state tests cover generation safety, invalid handles, capacity boundaries, one-shot/loop animation, small-map and edge camera math, collision helpers, required and optional map layers, coordinate conversions, transition destination validation, actor reservations, story enumeration, deterministic encounters, format-2 determinism, transactional rejection, persistent-only actors, and format-1 compatibility.

The language/runtime change generalizes `Load Text File` paths to `Text` expressions across native and Web. The parser also recognizes the reserved `Game` token only inside a dotted module name so the approved `Smile.Game.*` identity is usable without weakening statement-keyword rules.

The maximum save fixture encodes 32,436 bytes against a 36,864-byte package buffer and the existing one-MiB Phase 4 Data envelope. `.smilelib` remains deterministic format 5.

## RPGSystems World controls

- On the title screen, use Up/Down and Enter or Space to start, load slot 1, or exit.
- In a field scene, walk with the arrow keys or W/A/S/D. Enter or Space opens the command menu.
- In menus, use Up/Down, Enter or Space to accept, and Escape to cancel.
- Stand immediately beside an NPC, face them, open the command menu, and choose **Talk**. The same Talk action opens the merchant menu when facing a merchant.
- Walk through the west town doorway to enter the shop, through the south gate to enter the overworld, and back through transition regions to return.
- Enter, Space, or Escape advances/closes dialogue and information panels and returns from Encounter Preview to the exact saved overworld cell and facing.

Private source-comparison demos for two commercial reference games were built and exercised only outside the repository. Their assets, maps, projects, source logs, binaries, and screenshots are intentionally absent from Git, the tracked solution, Web publication, smoke artifacts, and release packaging.

## Validation summary

- The full repository smoke suite passed in 218.5 seconds, including 220 managed language/compiler/project/IDE tests, eight formatter integration tests, 39 native graphics/audio-focus checks, 38 native Text runtime checks, deterministic package builds, transactional rollback injection, project/package parity, and all existing native/Web game builds.
- The focused Phase 7 state fixture reports 274 checks and passes as a project and package consumer on native and Web. The tracked gallery builds on DirectX, GDI, and Web, publishes the same 12 original assets on every target, and runs without Web warnings or errors.
- Visual Studio 2026 Enterprise with VSIX 2.0.42 resolves the new references, builds the gallery, provides module/member completion and parameter Quick Info, navigates by F12 into library `.smile` source, debugs through `.smile` with F10, launches the native target, and builds and launches the Web target from the dedicated solution.
- A repository-safety audit compared all nine new public binary assets with 111 private reference binaries and found zero SHA-256 matches. No private project, asset, provenance URL, or evidence path is present in the solution, Git inputs, public smoke suite, artifacts, or Web output.

Full battle resolution, combat rewards, quest DSLs, behavior trees, physics, pathfinding, networking, and 3D remain later-phase work.
