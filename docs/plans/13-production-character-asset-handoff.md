# Production Character Asset Handoff

## Purpose

This document defines what an artist, AI-assisted modeling workflow, Blender specialist, or external asset provider must deliver for the Dragonfall Generation 2 hero.

Codex implements the engine and validates the asset. Codex should not pretend that a single reference PNG is already a complete model.

## Rights and provenance

The asset must be:

- original work owned by the project; or
- licensed for source and compiled-game distribution under terms compatible with the repository.

The handoff must include a provenance note containing:

- creator/source;
- creation date;
- tools;
- license or ownership statement;
- third-party texture/material sources;
- whether AI tools were used;
- permitted repository distribution;
- required attribution.

Do not commit an asset with uncertain rights.

## Visual direction

Target:

- heroic stylized-realistic male paladin or knight;
- white/ivory armor;
- dark blue cloth;
- restrained gold trim;
- subtle blue emissive details;
- sword and shield;
- readable human face and hair;
- modern 2026 indie-JRPG quality;
- original insignia and armor design.

The supplied reference establishes quality and proportion, not exact design.

## Coordinate and scale convention

Codex must confirm actual Renderer3D convention during M0/M1.

Preferred authoring convention:

- Blender meters;
- character approximately 1.8 meters tall before engine scaling;
- Y or Z up according to the converter's documented glTF conversion;
- forward direction documented;
- origin at ground between feet;
- root bone at origin;
- applied transforms before export;
- no unintended negative scale;
- consistent sword/shield scale.

The converter should normalize only through explicit profile settings, not hidden guesswork.

## Geometry

### LOD0

- equipped total: 10,000–15,000 triangles;
- clean silhouette;
- adequate loops around shoulders, elbows, wrists, hips, knees, ankles, mouth, and eyes;
- no accidental internal faces;
- no non-manifold geometry unless deliberately safe;
- no extreme skinny triangles around deformation joints;
- controlled hard edges and UV seams;
- separate weapon/shield only when attachment or material behavior benefits.

### LOD1

- approximately 5,000–8,000 triangles;
- preserve armor silhouette and weapon readability;
- compatible skeleton and material names where possible.

### LOD2

- approximately 2,000–4,000 triangles;
- preserve broad silhouette;
- simplify face/hands/small trim.

LODs are not required to block the initial M7 proof if automatic LOD support is deferred, but source art should be prepared for them.

## Materials

Preferred maximum:

- body/armor/cloth combined material;
- head/hair material;
- weapon/shield material;
- limited additional transparent material only when necessary.

Each primary material should provide:

- base-color PNG;
- tangent-space normal PNG;
- packed ORM PNG:
  - R occlusion;
  - G roughness;
  - B metallic;
- emissive PNG when used.

High profile: 2K.

Provide 1K variants or source files allowing deterministic downscaling.

Avoid baking directional lighting into base color.

## Skeleton

Target:

- 55–80 deformation bones;
- maximum 128 runtime bones;
- maximum four weights per vertex;
- normalized weights;
- no unused weighted bones;
- no Blender control widgets/constraints required at runtime;
- clear hierarchy;
- full bind pose;
- root bone;
- pelvis/spine/chest/neck/head;
- clavicles/arms/hands;
- legs/feet/toes where useful;
- finger detail only within budget;
- optional eye/jaw bones;
- optional cloth accessory bones.

## T-pose or A-pose

A clean A-pose is often better for shoulder deformation, but either is acceptable if:

- bind pose is documented;
- arms and hands are not intersecting armor;
- shield/sword are separate or positioned consistently;
- palms/fingers are suitable for weapon grip;
- symmetry is clean before asymmetrical detailing.

## Required sockets

Use nodes or bones with stable names:

```text
socket-root
socket-head
socket-chest
socket-hand-right
socket-hand-left
socket-sword-base
socket-sword-tip
socket-shield-center
```

The SM3D descriptor may alias them to beginner-facing names.

## Required animation clips

Minimum M7 delivery:

| Clip | Loop | Notes |
|---|---|---|
| `Idle` | yes | breathing and weight shift |
| `Ready` | yes | battle stance |
| `Run` | yes | preferably in-place plus clean root data |
| `SwordAttack` | no | readable anticipation/contact/follow-through |
| `ShieldBash` | no | special/action alternative |
| `Defend` | yes/hold | shield covers body |
| `BlockImpact` | no | convincing recoil |
| `Hit` | no | front hit reaction |
| `KO` | no/hold | stable final pose |
| `Victory` | yes or hold | clear celebration |

Recommended extras:

- start/stop;
- turn left/right;
- heavy attack;
- cast;
- dodge;
- relaxed idle;
- two hit variations.

## Event timing

Descriptor events should be supplied for:

```text
SwordTrailOn
SwordImpact
SwordTrailOff
ShieldImpact
FootstepLeft
FootstepRight
```

Impact timing should match the visible contact frame.

## Deformation review

Before acceptance, inspect:

- shoulder raise;
- elbow bend;
- wrist rotation;
- shield arm guard;
- sword grip;
- hip flex;
- deep knee bend;
- ankle/boot;
- neck turn;
- jaw/face if present;
- armor intersections during attack;
- cloth collapsing or stretching.

A higher polygon count does not compensate for poor weights.

## Export profile

Preferred delivery:

```text
source .blend
export .glb
descriptor .sm3d.json
textures .png
preview renders/video
provenance .md
```

GLB export should include:

- selected mesh objects;
- armature;
- skinning;
- named actions;
- material assignments;
- tangents if valid;
- no cameras/lights unless intentionally needed only for review;
- no unrelated hidden source objects;
- applied object transforms;
- no unsupported compression extension for the first pipeline.

## Folder suggestion

```text
games/Dragonfall/Assets/Source/Arin/
    Arin.blend
    Arin.glb
    Arin.sm3d.json
    Arin-provenance.md
    Textures/
        Arin-base-color-2k.png
        Arin-normal-2k.png
        Arin-orm-2k.png
        Arin-emissive-2k.png
```

Compiled output:

```text
games/Dragonfall/Assets/Models/Arin.sm3d
```

Codex must use actual project asset conventions after reconciliation.

## Asset validation command

Expected shape:

```powershell
artifacts\assettool\smileasset.exe model `
    games\Dragonfall\Assets\Source\Arin\Arin.glb `
    --descriptor games\Dragonfall\Assets\Source\Arin\Arin.sm3d.json `
    --profile character `
    -o games\Dragonfall\Assets\Models\Arin.sm3d

artifacts\assettool\smileasset.exe inspect `
    games\Dragonfall\Assets\Models\Arin.sm3d
```

The inspection output must be saved in the M7 report.

## Acceptance checklist

- [ ] Provenance and license are documented.
- [ ] Design is original.
- [ ] Runtime triangle count is within approved budget.
- [ ] Material count is controlled.
- [ ] All textures are present and correctly classified.
- [ ] Bone count is within limit.
- [ ] No vertex has more than four influences.
- [ ] Required clips exist.
- [ ] Required events exist.
- [ ] Required sockets exist.
- [ ] Sword and shield stay attached.
- [ ] Shoulder, elbow, hip, and knee deformation is acceptable.
- [ ] No severe armor intersection in required clips.
- [ ] GLB converts deterministically.
- [ ] Native and Web Character Lab render successfully.
- [ ] Source and compiled assets are stored according to repository policy.
