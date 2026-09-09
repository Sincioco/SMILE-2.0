# Vrax V1 Creation And Repair Journey

## September 10: Original Unity Audio

The original `SciFiBeast01.cs` controller plays Sound14 at attack start,
Sound13 after 0.2 seconds, and Sound15 on death. Scene AudioSource references
resolve these to Beast Vocalisation 08, Blunt Swing 01 and Beast Vocalisation 09.
`unity-audio-import.json` records the source GUIDs, original checksums and runtime
copies. `Import-UnityAudio.ps1` repeats that read-only import from the installed
Unity project, preserving original bytes under `Private/Audio/Originals` and
producing PCM16 stereo 44100 Hz copies. No waveform remix or VFX import occurs.

Standalone and Party Vrax call `ViewerDragon.UpdateVraxAudio`, with independent
caller-owned cue state. Attack through Attack6 trigger voice on channel 4 at clip
start and swoosh on channel 5 at 200 ms of clip time. Death and Death2 use the
original death voice on channel 4. The controller did not define a separate Hit
sound, so none was invented. Playback rate changes cue timing through the existing
clip clock, while sample pitch stays original. Pause/hidden boss stops the new
channels; scrubbing consumes cues without delayed catch-up. Teardown stops them
and resets state. Audio does not depend on VFX visibility.

The normal project stages checksummed audio alongside the permanent private
roster. Public-roster Web builds omit the Vrax and Zara audio wildcards. The
licensed files remain ignored in Git; models, materials, all 24 animations,
grounding, sockets and accepted scale are unchanged.

Native and Web builds passed. Native standalone/Party playback was launched and
inspected. Chrome verified growl/swoosh/death AudioBuffer starts with nonzero
samples, separate channels, attack cues near 0/200 ms, pause stops and Party
playback, with no page errors. This is playback validation, not a subjective
listening or mix approval. The prior Dragon trial and Original restore stay separate.

## September 10: Party movement and complete clips

Party Vrax keeps the standalone boss rate of 100 and plays each selected attack
through its actual duration. His Hit reaction also completes instead of being
overwritten on the next choreography step. The party has independent rates and
cycles defenders through Arin, Orin and Zara; Valor is temporarily hidden there.

The boss's 650-ms close-up leads to an 800-ms Run approach, with a 260-unit
standoff from the selected defender. Body facing, a bounded head-node aim and
the existing fire/arm-lightning target follow that defender. The camera holds
Beat 3 for the whole attack, skips Beat 3a, then shows the frontal aftermath and
800-ms return. Camera 2 follows Camera 1's continuous orbit direction and rate,
except the later reference-driven Vrax Beats 3/4: their camera is planted behind
the defender, 55 units above the floor, with a fixed 26-degree lens. Its position
does not follow Vrax forward/backward or orbit; only the look-at target follows
his attack/return. The low frontal shot retains the defender's full back view
and emphasizes Vrax's size and mouth-fire/arm-lightning effects.
Party Dragon retains its separate historical placement/camera policy.

The Web PBR face-normal orientation was corrected to agree with the native mesh
winding, including the reflection pass. This repairs metallic lighting without
adding lights, draw calls, textures or a quality/performance downgrade. It does
not alter the model, materials, clips, sockets, grounding or accepted scale.

This package owns the validated local Unity character import. Native and Chrome
inspection, full clip inventory and measured floor-contact validation are recorded
in the Unity import checkpoint. Original files and full-weight sources are preserved.

Original selected-prefab dependencies, textures and animation FBXs are preserved
under `Private/Originals`. The full animation Blender source and GLB derivative
are beside them. `Private` is ignored pending asset redistribution permission.
The original Unity project has not been migrated or edited.

Conversion transfers evaluated poses to the model's own bind rig, keys quaternion
rotations explicitly and preserves the armature transform. Removing Valor's single
armature object corrupted exported root animation; retain it, and place skinned
mesh nodes at scene identity. Do not correct that defect with Viewer rotation.

Zara and Vrax use approximately 45% of their original triangle count. Valor keeps
his original triangle budget. Derivatives use 1024-pixel textures and four skin
influences. Tiny triangles and undefined tangents are cleaned in the derivative.
Valor Attack3 has zero-scale shoulder keys; its derivative uses 0.000001 scale.

Use the animation manifest and grounding audit for this revision. Never copy
Arin/Orin grounding constants. Frame zero of every clip and full Block/Hit/Death contact curves have been
measured. Individual and Party native/Chrome previews were inspected. Corrections
resolve by clip name and preserve authored jumps; see grounding-validation.json.

The common image cooker must use pixel coordinates, independent of PNG DPI;
otherwise these texture atlases are enlarged and cropped despite correct UVs.

## Viewer Handoff

The descriptor contains the complete animation set; the inspector exposes nine
clips per page. Four VFX sockets now follow the head and forearm bones; imported
equipment stays part of the original animated mesh. There are no pose-calibration
channels, and Arin/Orin pose saves are not used for this character. Source emission
textures remain intact.

Private/Previews contains native inspection and Party screenshots. The private
Blender source retains full skin weights; the Viewer derivative keeps the strongest
four, with measured losses in skin-weight-audit.json.

Pipeline scripts in Private/Pipeline preserve the local conversion recipe. Their
working-directory paths refer to artifacts/temp/unity-import; stage a working copy
there before rerunning. convert_full.py requires the original Unity project for
GUID/material lookup. Do not rerun the one-shot grounding code generator over an
already generated Profiles.smile. The canonical descriptor, GLB checksum and
grounding corrections must be updated together after any re-export.

Vrax now uses 20000% scale, twice the first preview capped at 10000%. The same
profile applies in his tab and Party. Orin effects must not depend on a Vrax Chest
socket: the Party owner uses current world bounds as the fallback target.
Valor and Zara's individual Viewer tabs also instantiate this same Vrax package as
their battle-preview opponent. Their arena camera uses twice the standard opponent
distance so the accepted 20000% Vrax scale and the active hero remain visible.

## Mouth Fire And Arm Lightning — September 9, 2026

The canonical descriptor owns VraxMouth/VraxMouthAim on `head`, and VraxLeftArm/
VraxRightArm on `lowerarm_l`/`lowerarm_r`. The original rig's Root has 0.01 scale;
socket translations are in bone-local source units, not Viewer world units.
The two head points establish the animated forward direction. Forearm offsets
place the bolts outside the arm surfaces. Native and Chrome inspection confirmed
the fire origin inside the open mouth and both bolts following the arms.

Attack through Attack6 use their own duration, active from one-sixth to four-fifths
of clip time. `ViewerDragon` owns the effect policy and independent caller state;
the same operation runs in the individual tab and four-hero Party. Existing shared
FireEmitter3D and LightningVfx3D resources provide fire and blue-white bolts.
The model, textures, all 24 animations, grounding corrections and 20000% transform
scale are unchanged. The GLB remains SHA-256
`ec863933e6898fb81672aaa979eada343b3b2eedcdc433a8b2c65bfe7a8a84fb`.
See the equipment VFX checkpoint for validation and whole-scene budgets.

## Expanded Attack Presentation — September 9, 2026

The same single mouth emitter now uses radius 24 and intensity 320, with a
420-unit forward endpoint and 900-unit velocity. This makes the emitted stream
and target contact read substantially larger without adding an emitter, changing
the twelve-emitter scene budget or altering either arm-lightning lease. The model,
textures, sockets, animations and grounding remain unchanged.
