# Paladin Production Readiness — M7C-B

Date: September 2, 2026  
Milestone: M7C-B — Production Paladin intake  
Status: Integrated v5.4 viewer candidate accepted; production gate remains closed  
Canonical identity: `sin-star-i.character-1.paladin`  
Viewer candidate alias: `dragonfall.arin-v5-4`

## Reconciled outcome

The initial handoff found only the original three-clip prototype and the 76 MB one-animation T-pose export. Work during this task produced a new repository-owned Blender integration source and deterministic GLB candidate. The current branch therefore differs materially from the initial M7C-B scan: Arin v5.4 is now the default Character Viewer profile and has sword, shield, grip glove, and 11 named animation actions.

The v5.4 candidate is suitable for continued engine and viewer work, but it is not production-ready. Dragonfall release visuals remain Classic and M8 remains blocked by provenance, texture, event, socket, cross-target, and final production-approval gaps.

## Accepted repository candidate

| Artifact | Bytes | SHA-256 |
| --- | ---: | --- |
| `games\Dragonfall\SourceAssets\Arin\arin-integrated-candidate-v5.4.blend` | 5,384,967 | `CD58B33AC94E7B3CFEEDB9A85B2603B49DB4935FE8D2590DE5B50BE371C4A35C` |
| `games\Dragonfall\SourceAssets\Arin\arin-integrated-candidate-v5.4.glb` | 3,839,696 | `CAA8F8AD5A814E7763B895AA846E6BC528CD3728DA5C06E45FDE93A3B1DD66A6` |
| `games\Dragonfall\Assets\Generation2\ArinV54\ArinV54.sm3d` | 863,468 | `98BDB192AF42F62745E52717A718E44F27331D00DBEE03BE7944C18B1398A4A2` |

Cooked profile: 7,376 vertices, 30,888 indices, 10,296 triangles, four parts, four materials, nine texture references, 42 bones, 46 nodes, 11 clips, and six sockets. The GLB remains below the existing 1,024 accessor, buffer-view, node, skin-joint, and animation table limits; no converter-limit increase was required.

Named actions are Idle, Walk, Run, Ready, SwordAttack, ShieldBashCandidate, Defend, BlockImpact, Hit, KO, and Victory. Sword, shield, and the sword-grip glove are rigid one-bone skinned equipment attached to the hand skeleton. The candidate still has no authored combat events and only Root, Head, Chest, SwordBase, SwordTip, and ShieldCenter descriptor sockets.

## Deterministic Blender export contract

`scripts\export-arin-v5-4-viewer.py` performs the smallest reusable correction needed for SM3D intake:

1. Convert rigid equipment to one-bone skinned parts.
2. Insert an explicit `SMILE_Root` above the original skeleton root.
3. Bake armature-object motion into that root using source-world times reference-inverse order.
4. Remove non-pose armature-object curves and animation channels identical to bind transforms.
5. Compact accessors and buffer views and enforce current table limits.
6. Produce byte-identical output across repeated exports.

Blender source and re-imported optimized GLB comparison renders matched for Idle, SwordAttack, and KO apart from small antialiasing differences. The cooked runtime no longer applies the armature object transform twice, which removed the former deformation and mid-air attack/KO behavior.

The bind AABB extends approximately 0.104 model units below the animated Idle sole plane. The reusable `CharacterViewer.Profile.GroundOffset` records the measured per-asset correction; Arin v5.4 uses `-10` fitted-world units. This grounds the boots without editing animation root motion.

## Gate results

| Area | Current evidence | Result |
| --- | --- | --- |
| Identity | Arin display name and Paladin party role remain separate. | Pass |
| Geometry | 10,296 triangles and four deterministic parts. | Pass for viewer candidate |
| Skeleton/equipment | 42-bone root-baked skeleton with attached sword, shield, and grip glove. | Pass for viewer candidate; production review pending |
| Animations | 11 named actions; SwordAttack and KO visually improved; ShieldBash remains explicitly a candidate. | Pending production acceptance |
| Materials/textures | Four PBR materials and nine deterministic PNG outputs derived from 1K sources. | Fail production 2K/lossless requirement |
| Events | No SwordTrailOn, SwordImpact, SwordTrailOff, ShieldImpact, or footsteps are authored in the v5.4 descriptor. | Fail |
| Sockets | Six descriptor sockets; HandRight and HandLeft remain absent. | Fail |
| Native viewer | Default v5.4 load, animation buttons, auto cycle/orbit, grounding, and reset verified. | Pass for prototype viewer |
| Web viewer | Compiles and JavaScript syntax checks pass; final visual acceptance remains outstanding. | Pending |
| Provenance and rights | Repository use is authorized, but final export/license evidence is incomplete. | Fail production gate |
| Release enablement | Dragonfall remains Classic; no production flag enabled. | Correctly disabled |

## Character Viewer hardening

The repository-owned desktop viewer now uses Arin v5.4 as its default profile and treats the selected `-16 deg` framing as the authored startup, right-click reset, and ten-second idle-reset zoom. The responsive native canvas restores its last desktop x/y/width/height, fills the live client area without letterbox bars, keeps the header left aligned and controls right aligned, and uses 80% opacity for every remaining panel and button fill. The status panel reports live zoom, FPS, draw calls, and submitted triangles. `F` toggles the floor and grid together; `G` toggles only the grid.

Camera input remains character-neutral. Left-drag pans, middle-drag orbits at direct pointer sensitivity, wheel input eases toward a bounded zoom target without stopping auto-orbit, and right-click restores the complete presentation. Renderer3D numeric command 123 now carries the camera's explicit nonzero up direction after the source-compatible command 10 camera payload. `Interaction.ApplyCameraControls` rotates camera position and up direction together, eliminating the former fixed-world-up pole singularity and allowing a continuous 360-degree vertical orbit in native Direct3D and WebGL2.

## Remaining blockers

1. Complete project/export, account entitlement, reference ownership, modification, distribution, and AI-disclosure evidence.
2. Provide accepted 2K lossless base-color, tangent-normal, and ORM sources.
3. Replace or explicitly accept `ShieldBashCandidate` and perform final hand, wrist, shoulder, armor, weapon, and shield deformation review.
4. Author combat and footstep events at reviewed frames.
5. Add and visibly validate HandRight and HandLeft plus the complete production socket set.
6. Complete native and Web visual acceptance using the same cook and record the production evidence set.
7. Obtain Sin's explicit final-production approval.

## Decision

M7C-B now has a usable, deterministic v5.4 viewer candidate, but the production asset gate remains closed. M8 is not unblocked.
