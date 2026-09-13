# Mira3 — Hunyuan3D-2.1 comparison

Mira3 is the third independent model candidate. Sin subsequently selected Mira1 as
the native Viewer battle healer. Tencent Hunyuan3D-2.1 generated Mira3's shape and PBR textures locally on the
RTX 5090. Mira1 (Pixal3D) and Mira2 (TRELLIS.2) remain in their own packages and tabs.
Mira3 remains on her own tab with her earlier battle validation preserved below;
the current Party scenes load Mira1. Mira3's model and animation assets are unchanged.

The equipped model has **19,652 triangles**: 18,100 body and 1,552 staff. Both meshes
are closed, with no boundary/non-manifold edges. The body has 4096-pixel baked base
color, metallic/roughness and normal maps. A separate head paint pass increases face
detail. Her own Mixamo 25-bone skin has at most four influences per vertex and no
unweighted vertices. Body is part 0; staff is part 1. **Weapon/W** toggles the staff.

Nine clips are included: Idle, Walk, Run, Attack, Defend, HealOne, HealParty, Hit and
Death. One-handed casts use her free left hand. Baked right-arm poses hold the staff
upright during movement/casting; it turns horizontal as Death settles. The original
Mixamo exports are preserved. Neutral Idle rotations are retained as JSON and applied
to Mira3's own bone lengths. No Arin/Orin calibration or numeric floor offset is used.

In the earlier comparison milestone, Mira3 joined Arin and Orin in **Party Dragon**
and Arin, Orin and Zara in **Party Vrax**. She cycled Water Attack, Heal One (Arin) and Heal Party with blue water
VFX and original water/chime sounds. Her Beat Camera identity is independent of Vrax.
Healing uses Viewer presentation states; no Sin Star I game code or health rules change.

## Validation

Export checks pass for closed topology, triangle budget, nonzero tangents, skin
weights, bind/Idle floor agreement and sampled clip contacts. Full per-frame body
corrections and the exact exported checksum are in `Source`. The staff is above the
floor at every recorded first/middle/final clip sample, including settled Death.
The native build/cooker and focused Viewer hardening checks pass, including 58
native graphics/pointer/audio checks. The real-asset fixture passes all three
comparison tabs, all three casts in both battles, healing without boss damage,
recipient cleanup, Beat Preview restoration, and unchanged Arin/Orin JSON round-trips.
The final native launch was visually checked on Monitor 1 beside Codex. Idle, staff
hide/show, the left-handed HealOne pose and the settled Death floor contact were
inspected. Mira3 appeared in both Party battles, including blue HealParty effects
in Party Vrax. Native screenshots are retained in `Previews`. This is technical
validation of the comparison build, not Sin's final visual acceptance.

## Appearance limits

This remains an AI-generated **comparison candidate**. Fine costume ornament, hair
strands, the neck texture transition and cloth deformation are less faithful than
the reference image. The head pass has stronger makeup than the reference. The rigid
staff stays attached to the hand; it does not have a separate drop/release animation.
Death moves backward, so its settled pose may need camera reframing. These limitations
are visible asset quality concerns, not hidden compiler/runtime failures.

## Reproduction and ownership

The original shape, paint/geometry views, reference images, rig and animation exports,
authoring scripts, measurements, portable GLBs and rigged/unrigged Blender sources
are together in this package. `checksums.sha256` covers binary assets; editable source
and documentation are versioned by Git. Read `MIRA-CREATION-AND-REPAIR-JOURNEY.md`
before changing the package. Scripts record the local `D:\AI\Mira3D\Mira3` staging
layout; adapt paths when reproducing elsewhere. The Viewer builds from the canonical
GLB and SM3D descriptor without AI tools or external downloads.

Generator source: [Tencent Hunyuan3D-2.1](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1).
Authoring weights and isolated Python dependencies remain under `D:\AI`.
