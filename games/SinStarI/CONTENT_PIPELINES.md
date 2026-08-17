# Sin Star I reusable content pipelines

This file is the permanent handoff for future Sin Star town and character work.
The authoritative Unreal pipeline index is:

`D:/Projects/Sin-Star-Asset-Lab-Unreal/SinStarI/Pipeline/REUSABLE_CONTENT.md`

## Create another modern town

1. Duplicate `Pipeline/Definitions/Town02_Modular.json` in the Unreal project.
2. Author isolated, grid-aligned Ground, Detail, and Foreground modules in an
   Unreal capture level. Keep buildings as large multi-cell assemblies rather
   than shrinking them into one tile.
3. Export 512 source pixels per logical cell with the locked orthographic
   camera. Ground is opaque; Detail and Foreground use black/white matte pairs.
4. Run the finalizer and generator:

```powershell
Set-Location 'D:/Projects/Sin-Star-Asset-Lab-Unreal/SinStarI'
& py -3.10 Pipeline/town2_finalize_captures.py <definition.json> --require-complete
& py -3.10 Pipeline/town2_tileset_pipeline.py <definition.json>
```

The active Town 2 reference uses 193 reusable IDs, a 72 x 56 world split into
four 36 x 28 maps, three visual layers, independent collision, 6 x 5 and 8 x 5
buildings, clear door approaches, four exits, service signs, and a live minimap.
See `Pipeline/TOWN02_MODULAR_PIPELINE.md` for the complete contract.

## Create another animated character

Use this canonical request in a future conversation:

> Run the Sin Star reusable character recapture pipeline for Character N using the approved Unreal mesh, then rebuild and verify the 4-direction sprite sheet.

The short alias is **Sin Star character recapture**. Future Codex should read
`D:/Projects/Sin-Star-Asset-Lab-Unreal/SinStarI/Pipeline/Definitions/Character_Mannequin_Fallback.json`
before touching Unreal. A character number plus an optional approved mesh or
material is enough; the manifest defines the repeatable frame sequence and
acceptance gates.

1. Preserve the approved concept art under `SourceArt/Characters`.
2. Generate transparent direction rows in the exact order up, right, down,
   left, with sixteen coherent phases per row for separate walk and run sheets.
3. Assemble a 4096 x 1536 RGBA sheet of 256 x 384 bottom-center-anchored frames.
4. Analyze and, when needed, reorder discontinuous side cycles:

```powershell
Set-Location 'D:/Projects/Sin-Star-Asset-Lab-Unreal/SinStarI'
& py -3.10 Pipeline/character_animation_refine.py <sheet.png> `
  --directions right left `
  --output-directory Pipeline/Verification/Characters/SideLoopCandidates
```

5. Publish the accepted sheet under `Assets/Characters`, record continuous
   Unreal animation and actual Town 2 traversal at 60 fps, and inspect both
   side-loop seams plus scene-relative stride. Reject direction-pose slideshows.
   Do not reset an actor's animation clock at each tile boundary; reset only when
   the actor becomes idle.
6. Upload the accepted H.264 proof to YouTube as Unlisted. Keep Unreal in the
   foreground while the upload proceeds in the background.

The reference proof is:

`D:/Projects/Sin-Star-Asset-Lab-Unreal/SinStarI/Pipeline/Verification/Characters/SinStarI_CharacterWalk_Final_Mobile.mp4`

See `Pipeline/CHARACTER_SPRITE_PIPELINE.md` for the complete contract.

## Shared engine capabilities used by these pipelines

- `Smile.Game.TileMap`: reusable atlas IDs, Ground/Detail/Foreground draw order,
  four-map world dispatch, runtime `SetTile`, runtime `SetCollision`, and binary
  collision checks.
- `Smile.Game.Animation.CurrentFrameInCycle`: maps an exact authored full-cycle
  duration across every registered frame. Town 2 Character 1 uses a 188-step
  (1.504-second) walk cycle with 40 movement steps per cell, and a separate
  221-step (1.768-second) run cycle with 21 movement steps per cell. The camera
  follows the same interpolated actor position used for drawing and collision.
- Shared input constants: arrows, WASD, Space, `KEY_1` through `KEY_4`, and
  `KEY_TAB` work in native and Web runtimes. In the Character gallery, held
  Space toggles the manually controlled character between its walk and genuine
  run sheets; autonomous cells periodically demonstrate running as well. Town 2
  retains held Space as its direct run modifier.

## Validation and operating rules

- Use a two-minute watchdog for Unreal and Python operations. If no progress is
  visible, inspect state before retrying; do not blindly repeat a failing call.
- Keep Unreal's viewport on the current stage for livestream visibility and
  collapse the Content Drawer when not in use.
- Pause Media Player before launching Sin Star I; resume it after the game exits.
- Build both Windows DirectX and Web targets.
- Never copy commercial Phantasy Star art. Original or properly licensed assets
  may use its broad optimistic science-fantasy era only as inspiration.

Current Character 1 Town 2 cycle-synchronized proof:

https://youtu.be/qGVl3pYex4c

Current four-character cycle-synchronized gallery proof:

https://youtu.be/BA1LMoA8WJg
