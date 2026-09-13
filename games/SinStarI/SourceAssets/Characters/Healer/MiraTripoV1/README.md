# Mira — Tripo v1 authoring package

Sin supplied the Tripo HD GLB on September 13, 2026, to improve Mira's appearance in
the native Character Viewer. This package preserves that source independently of
MiraV1 (Pixal3D), MiraV2 (TRELLIS.2), and MiraV3 (Hunyuan3D). No Sin Star I game code
belongs to this work.

## Current state

Authoring/export, native model/animation checks, both Party cast fixtures and the
seven-mode native Water Lab checks have passed. The original has 1,885,636 triangles, 1,045,746 imported
vertices, and three embedded 4096 × 4096 JPEG maps: base color, tangent normal, and
metallic/roughness. It contains no skin or animation. The downloaded file's SHA-256
is `6812a9e42cdf90ee6715a9a2aae7b5f523580f81f16362ca7aa69f565596f7d9`.

`Source/mira-tripo-hd-original.glb` and `Blender/mira-tripo-hd-original.blend` are the
unchanged source and its imported scene. The latter is a Blender scene library;
append its scene to another .blend or use the source GLB for a normal import.
`Source/inspection.json` records the import facts.

The reduced body has 18,438 triangles and the removable staff has 1,344, for 19,782
equipped triangles. The body uses three 4K PBR maps; the staff uses three 2K maps.
Both original 4K masters are preserved. This fits the current model cooker’s 256 MiB
decoded texture budget at 240 MiB. Directional lighting is not baked into the color.

Mira has her own Mixamo rig and nine clips: Idle, Walk, Run, Attack, Defend, HealOne,
HealParty, Hit and Death. The generated rig contains 33 bones, including index-finger
chains; it does not contain a full finger rig. Two authored bones control cape and
staff. Held poses leave the left hand free to cast; Run carries the staff on her
back, and Death lets it fall beside her. The rear reference does not specify a mount,
so the back-carry placement is authored rather than traced from that image.

`mira-animation-checkpoint.glb` and `Mira.sm3d.json` are the portable runtime source.
The descriptor exposes nine sockets, including StaffGrip and StaffTip. The new
**Mira** tab precedes Mira1 and both party scenes select this revision. Native builds
and focused checks pass; earlier Mira packages remain intact.

## Native delivery and validation

The Character Viewer and Water Lab share water surfaces, GPU droplets, staff-head
shimmer, casting glow and the Mira/Orin lightning adapter. Mira cycles Torrent,
Heal One, Heal Party, Waterball, Tsunami and Tempest in both Party battles. Barriers
remain small individual discs; impact spray spreads across the struck disc and
curls around its rim as the surface gives slightly. No formation-sized dome remains.
The Lab defaults to 200% speed, with `+`/`-` and buttons controlling its single
presentation clock in 25-point steps from 25% to 400%.

Validation on September 13, 2026:

- Blender/GLB round-trip samples cover all nine clips. Held-clip wrist and staff-grip
  checks run through every sample; grounding reports record the final GLB hash.
- Native Viewer inspection covered held poses, Run back carry, settled Death and
  both parties. The final head shimmer followed back carry and hid with the weapon.
- The real-asset calibration fixture passed all six Mira actions in both parties,
  healing/contact policy, seek/restore, target-count cleanup and return to Arin.
- The Viewer hardening fixture passed, including 58 native graphics/input/audio
  checks. Water Lab passed all seven modes, seek, contact/recoil, head attachment,
  speed scaling, pause and complete owned-resource cleanup.
- Native GPU fast/fallback checks and small/normal/large valid-triangle regression
  passed. Full compiler/runtime/VSIX build and 323 shared language/compiler tests
  passed; formatter tests and the 453-file style check passed.
- Native `-` changed the live Lab from 325% to 300%; `+` restored 325%. A fresh Lab
  starts at 200%. Camera movement retains real-time speed and sound pitch is unchanged.

Build logs are working evidence in `D:\AI\Mira3D\MiraTripoV1`; reproduction commands
remain in the Viewer and Water Lab READMEs. `package-manifest.json` identifies the
portable source, rig and animation mapping; `checksums.sha256` covers this package.

The rebuilt VSIX 2.0.63 was installed after Sin requested closing Visual Studio.
Visual Studio closed normally through its stop-debugging confirmation. Installation
verified the extension DLL and all 35 compiler/language/library/template payload
hashes against the built VSIX. Visual Studio remains closed as requested.

Sin completed the remaining manual Water Lab camera check on September 13, 2026,
confirming that its manual controls, orbit, panning and zooming work as expected.
Native Mira and Water Lab delivery is complete. Water remains bounded real-time
VFX with screen-space reflection/refraction, not a fluid solver or ray tracing.
All Web adoption/publication remains on hold.

## Reproduction

Use the installed Blender 5.2 with `--background --python` and these scripts in order:

1. `Source/repair-mira-tripo-face.py` preserves facial identity, relaxes the generated
   nose/eye surface defects, reprojects existing facial UVs, and corrects skin response.
2. `Source/prepare-mira-tripo.py` welds coincident seams on a copy and reduces the body.
3. `Source/bake-mira-tripo.py` repairs pinched topology, unwraps the reduced surface,
   and bakes 4K color, normal, metallic/roughness, and local ambient occlusion.
4. `Source/preview-mira-tripo.py` makes matched HD/reduced inspection renders.
5. `Source/prepare-mira-staff.py` reduces the preserved staff and removes collapsed
   duplicate shells; `Source/bake-mira-staff.py` bakes its 2K PBR maps.
6. `Source/assemble-mira-animation.py` assembles Mira's own Mixamo files from
   `Source/Mixamo`, retaining metric joint positions and unit joint scales.
7. `Source/fit-mira-equipment.py` authors grip, back carry and a cape hinge.
8. `Source/ground-mira-animation.py` grounds her body, solves smooth cape clearance
   over each entire clip and settles the dropped staff during Death.
9. `Source/export-mira.py` validates mesh records, tangents and every animation
   frame, then packs the Blender checkpoint and exports the GLB.
10. `Source/check-mira-roundtrip.py` verifies neutral relative wrist rotation and
    staff grip throughout held clips, then compares the re-imported GLB with the
    Blender scene at first/middle/last samples of all nine clips.

Working files currently live in `D:\AI\Mira3D\MiraTripoV1`. Preview rendering also
uses the separately saved, user-accepted Blender lighting scene in that directory.
The accepted export, Blender checkpoint and authoring reports belong here.
Intermediate working copies are not runtime assets. `Source/mira-grounding-checkpoint.json`
records the exact asset hash, bind/Idle comparison, per-clip floor samples and topology.

## Appearance acceptance

Native inspection covers face, both sides of the cape, fingertips, staff grip,
Run back carry and settled Death, followed by both party scenes. Keep live highlights
responsive to lighting. The color-only face render
showed no painted tear fringe; uneven geometry and specular response caused the
unwanted white rims. Do not repaint the eyes or bake strong directional lighting into
the base color to conceal that defect. All Web work is on hold at Sin's direction.
