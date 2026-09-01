# Dragonfall Remaster Vertical Slice

## Objective

Prove the complete Generation 2 stack in the existing Dragonfall battle before converting the entire cast.

The vertical slice replaces one hero's visual implementation while leaving:

- BattleCore/ATB mechanics;
- commands;
- damage formulas;
- enemy policy;
- HUD;
- audio ownership;
- deterministic presentation timeline;
- the other current actors;

unchanged unless a small generic presentation integration is required.

## Candidate hero

Use an original paladin/sword-and-shield hero in the visual class of the supplied reference.

Recommended mapping:

- replace Arin's rigid visual rig first;
- keep Arin's battle identity and mechanics;
- use the actor to prove sword and shield sockets;
- use current attack, defend, hit, KO, and victory cues.

The character design must be original. Do not copy logos, exact armor motifs, or proprietary character details from a reference.

## Production asset gate

M7 requires a GLB satisfying `13-production-character-asset-handoff.md`.

If no approved production GLB exists:

- complete M0–M6 using deterministic fixtures;
- create the Dragonfall integration seam;
- use a visibly labeled original placeholder in a non-release Character Lab;
- do not claim M7 visual completion;
- do not attempt to procedurally subdivide the old rigid rig and call it a modern character.

## Preserve Classic during proof

Preferred migration approach:

1. Keep the current rigid scene path buildable.
2. Add the smallest scene-selection seam required for a Generation 2 actor.
3. Build a separate development/preview project or compile-time configuration only if current project conventions support it cleanly.
4. Preserve both `Program.smile` and `Program-NoDemo.smile`.
5. After acceptance, decide whether:
   - Generation 2 becomes default;
   - Classic remains as a separate project;
   - the temporary preview project is removed.

Do not duplicate the full large scene file solely to change one actor. Extract or adapt a small reusable actor-rendering seam.

## Hero content specification

### Geometry

- LOD0 total equipped: 10,000–15,000 triangles;
- one to three primary opaque material parts;
- sword and shield attached through sockets;
- clean silhouette at current Dragonfall camera distances;
- no detached rigid limb pieces for normal body deformation.

### Textures

High profile:

- 2K base color;
- 2K normal;
- 2K packed ORM;
- 1K/2K emissive where needed.

Medium/low variants may be generated later or supplied separately.

### Skeleton

- approximately 55–80 deformation bones;
- maximum four influences;
- stable root;
- hand and weapon attachment bones;
- head/eye support if present;
- no unnecessary Blender control rig in runtime export.

### Minimum clips

Required for M7:

- `Idle`;
- `Ready`;
- `Run` or approach;
- `SwordAttack`;
- `ShieldBash` or special;
- `Defend`;
- `BlockImpact`;
- `Hit`;
- `KO`;
- `Victory`.

Recommended:

- relaxed idle variation;
- heavy attack;
- cast/special anticipation;
- turn left/right;
- start/stop movement.

### Required events

At least:

- `SwordTrailOn`;
- `SwordImpact`;
- `SwordTrailOff`;
- `FootstepLeft`;
- `FootstepRight`;
- `ShieldImpact`.

### Required sockets

At least:

- `SwordBase`;
- `SwordTip`;
- `ShieldCenter`;
- `Head`;
- `Chest`.

## Scene presentation

### Lighting

Use an `EmberObservatory` scene preset with:

- warm orange lava/key contribution;
- cool blue/purple fill;
- readable character rim;
- emissive weapon/armor accents;
- one stable floor shadow.

The actor must remain readable against the volcanic environment.

### Camera

Reuse the existing bounded Battle3D camera system.

Adjust only shot framing that no longer fits the actor's bounds. Use bounds-driven framing rather than hard-coding for one mesh where possible.

Required shots:

- standard battle;
- attack anticipation;
- impact;
- defend;
- hit;
- victory;
- defeat.

### VFX

Required Generation 2 effects for Arin:

- sword ribbon;
- holy/blue energy arc;
- metal spark impact;
- shield block flash;
- small impact light;
- camera shake;
- screen flash through Renderer2D;
- dust or ground motes where appropriate.

At least one existing Dragonfall effect should remain on the Generation 1 path during the slice to prove coexistence.

## Scene-code migration

Current `DragonfallScene.smile` owns procedural rig construction, transforms, animation, cameras, and effects.

M7 should reduce coupling by introducing only the smallest useful seams, such as:

- actor visual kind: rigid or character;
- generic actor update/draw/destroy operations;
- generic anchor/socket resolution;
- event-to-effect mapping;
- generic material/lighting initialization.

Do not move battle formulas or Dragonfall narrative data into renderer modules.

## Startup and fallback

Both startup paths must continue:

```text
Program.smile
Program-NoDemo.smile
```

If the Generation 2 actor cannot load:

- produce a clear diagnostic;
- use a documented fallback:
  - rigid Arin, or
  - existing 2D battle fallback if Renderer3D is unavailable;
- never leave an invisible but mechanically active party member.

## Visual comparison capture

M7 should capture matching native screenshots for:

1. standard battle pose;
2. attack anticipation;
3. impact;
4. defend;
5. hit;
6. victory.

Capture equivalent Web screenshots where tooling permits.

Comparison criteria:

- silhouette detail;
- material differentiation;
- shadow grounding;
- deformation quality;
- transition smoothness;
- VFX layering;
- HUD clarity.

Do not claim pixel parity.

## M7 acceptance

### Functional

- all player commands still work;
- battle can win and lose;
- enrage transition still works;
- restart works repeatedly;
- crowd demo remains hands-free;
- no-demo contains no demo AI;
- audio events remain synchronized enough to be credible;
- mechanics expected output remains unchanged unless explicitly approved.

### Visual

- Arin is one skinned imported actor rather than a 56-part rigid actor;
- armor, cloth, skin/hair, and emissive areas are visually distinct;
- Idle -> Attack -> Idle has no hard pose snap;
- sword trail follows sword sockets;
- impact effect is layered and modern;
- floor shadow anchors the character;
- bloom does not wash out the HUD;
- medium/low profile remains coherent.

### Technical

- native Windows build;
- Web build;
- current rigid actors coexist;
- no impact-time load;
- bounded effect and animation resources;
- no resource leak after restart/shutdown;
- live object count is substantially lower for the converted hero than its old rigid visual;
- focused and adjacent tests pass.

## M8 full remaster sequence

After M7 approval:

1. convert Tor;
2. convert Lyra;
3. convert Mira;
4. convert Wave 1 enemies;
5. convert Ashwing;
6. tune full-scene light and VFX budgets;
7. remove only obsolete duplicated visual code;
8. retain Classic if it remains educationally valuable;
9. make the remaster default through a separate approved commit.
