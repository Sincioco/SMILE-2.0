# Original Neris residents — prototype V1

Five adults (Tessa, Maren, Ilan, Bram, Sera), three children (Pip, Nia, Tobin), and
Mochi the dog. Names are working NPC identities, not additions to story canon.
These are original, deliberately simple procedural characters, not final character
art matching Arin. No downloaded models, animations or dependencies are required.

`Source/build_residents.py` creates the nine skinned GLBs, external palette PNGs,
clip descriptor and `grounding.json`. The latter records each model SHA-256 and
the bind/animated minimum Y for Idle, Walk and Run. All sampled feet are grounded
at Y=0, including frame zero; locomotion is in place. Runtime movement supplies
world displacement and heading. Regenerate with the existing Python installation.

Each character uses one mesh primitive and one palette material. The full original
Neris scene initially exhausted the shared 512-material pool with the separate
clothing materials, despite passing an isolated character load check. Palette
textures fix this without changing runtime budgets. The full-scene native probe
now loads all nine: 13 character assets, 67 models, 500 materials, 525 objects.
There is limited material headroom for additional distinct assets in that scene.

`Source/preview_residents.py` imports the GLBs using installed Blender and writes
`Residents.blend` plus `cast-preview.png`. The preview is an asset presentation,
not a Studio screenshot. The blend keeps relative links to `Models/*-palette.png`.

## Studio behavior and ownership

- `TownResidents` owns the nine actors, incremental loading, animation transitions,
  road-spawn placement, proximity and cleanup when leaving original **Neris Town**.
- `TownResidentMovement` chooses bounded short errands on connected clear road
  segments, alternates waiting with walking or occasional running, and pauses
  near the party or during conversation. It does not reuse or cancel the party's
  minimap search. It is not a town-wide job/quest simulation.
- `TownResidentDialogue` owns names, greetings and the conversation panel. Approach
  a resident and press **E**. Humans say “Hi, my name is {name}.” Mochi barks and
  shows his collar name. E, Enter or Esc closes the greeting. Speaking residents
  face the leader and idle; party movement pauses.

Dialogue content has a separate owner for later quests and branching dialogue;
those later systems are not implemented. `TownResidentTests` checks road errands,
blocked water, conversation pauses and names. `TownSessionTests` checks the nine
actors against the complete scene resources. Live appearance and keyboard
interaction still require acceptance in the refreshed Studio build.

## Research

The [glTF 2.0 specification](https://github.com/KhronosGroup/glTF/blob/main/specification/2.0/Specification.adoc)
defines the skin, joint, animation and material data used by the existing importer.
The [Unreal behavior-tree overview](https://dev.epicgames.com/documentation/unreal-engine/behavior-tree-in-unreal-engine---overview?lang=en-US)
was reviewed for NPC behavior organization. This small prototype uses explicit
idle/travel/conversation states instead of introducing a behavior-tree framework.
