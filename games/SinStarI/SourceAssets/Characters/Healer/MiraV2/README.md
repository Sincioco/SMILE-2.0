# Mira2 — TRELLIS.2 comparison

Mira2 is a separate locally generated candidate from Microsoft TRELLIS.2 BF16,
using the supplied T-pose reference in ComfyUI on the RTX 5090. Mira1 remains
preserved in MiraV1 and on its own native Character Viewer tab.

The equipped model has **19,652 triangles**: 18,100 body and 1,552 staff. Both source
meshes have zero boundary and non-manifold edges. The body has baked 4096-pixel
base color, normal and roughness maps. The separate staff uses a small shared-color
palette. Both meshes use Mira2's own 25-bone skin; Weapon/W toggles only the staff.

Animations: Idle, Walk, Run, Attack, Defend, HealOne, HealParty, Hit and Death.
Mixamo provides the action sources; Idle transfers neutral rotations while retaining
Mira2's bone lengths. Existing hand geometry curls around the staff without added
fingers or copied body geometry. The final model, rigged Blender source, unrigged
bake source, original exports, workflows, references and measurements live here.

## Validation and remaining work

The repaired native comparison tab loads all nine clips and seven sockets. Native
cooking, the Viewer build and the focused hardening checks pass. The real-asset
fixture also passes all three casts in both battles, target cleanup, heal reactions,
Mira's Dragon hit, and Beat Preview restoration. Source checks cover closed
topology, four skin influences, bind/Idle placement and all frames of the nine clips.
`Source/mira2-grounding-checkpoint.json` identifies the exact exported model checksum.
Common placement is corrected before clip-specific root keys. Idle/Walk retain
contact; positive airborne height in Run is preserved. No Arin/Orin offsets or live
calibrations are reused.

This is a **comparison candidate, not a completed healer milestone**. Face fidelity,
cloth deformation and staff contact during expressive clips remain comparison concerns.
The eyes currently read too dark in native lighting; refine the eye surface/texture
before final appearance acceptance. Death's backward movement can leave the default
Idle framing, so inspect its settled pose with the normal camera controls.
Mira2 now participates in Party Dragon (alongside Arin/Orin) and Party Vrax
(alongside Arin/Orin/Zara). Her turns cycle Water Attack, Heal One and Heal Party.
She casts from her formation position; blue water particles follow her left hand
and the recipients' final actor transforms. Heal One targets Arin; Heal Party
surrounds the current party. The Viewer demonstrates restoration through its
existing presentation states; this does not add game health or combat rules.
Original water/chime sounds are included under Audio, with their reproducible
standard-library authoring script in Source. Water remains visible while paused;
seeking is silent and leaving a cast clears its particles. Mira has her own Beat
Camera identity and preview restores her clip/time. Sin Star I game code is unchanged.

## Reproduction

Read `MIRA-CREATION-AND-REPAIR-JOURNEY.md` before changing the package. Source scripts
record the local Blender stages and their D:\AI staging paths; adapt those paths when
reproducing elsewhere. Model weights are external authoring inputs, not SMILE build
dependencies. The Viewer builds from the canonical GLB and SM3D descriptor alone.
`checksums.sha256` covers model, Blender, rig/animation, reference and upload assets;
editable documentation/workflow text is versioned by Git rather than byte checksums.

`Source/mira2-mixamo-input.obj` is the actual successful upload. The similarly named
FBX is retained as a rejected export; Mixamo did not rig it. Pro Magic animation
sources and the With Skin HealOne FBX belong to Mira2's own auto-rigged character.

Generator: [Microsoft TRELLIS.2](https://github.com/microsoft/TRELLIS.2),
[ComfyUI weights](https://huggingface.co/Comfy-Org/TRELLIS.2).
Original detailed geometry and the compressed raw generation are retained in Source.
