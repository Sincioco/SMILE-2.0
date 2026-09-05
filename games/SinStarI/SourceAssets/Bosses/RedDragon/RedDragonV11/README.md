# Red Dragon v1.1

Animated preview revision for Sin Star I and the Character Viewer's Party battle.
The original Tripo3D GLB and cleaned static derivative are preserved alongside the
user's red cyber-dragon reference. `red-dragon-v1.1-rig.blend` is the editable rig;
`red-dragon-v1.1-animated.glb` and `RedDragonV11.sm3d.json` are the cooking sources.

The 24-bone rig has spine, neck, head, jaw, wing, leg, foot and tail chains. Six
original 30 Hz clips are Idle (4 s), Roar (3 s), FireBreath (4 s), ClawStrike (2.2 s)
Hit (0.8 s) and Fireball (5 s). Idle loops; the other clips play once. Root, Chest, Head and Mouth
sockets follow their respective bones; EyeLeft and EyeRight provide emissive anchors. The Mouth socket is attached to the jaw.

The 9,912 cleaned triangles, UVs and original packed texture are preserved. Source
coordinates are multiplied by two; Character3D scale 25,000 reproduces the former
static actor's world dimensions without increasing the runtime's scale bound.

Run Blender 5.2 with `--background --python scripts/rig-red-dragon.py` from the
repository. The builder imports the preserved static source, applies bounded
weights, creates the clips, checks first/middle/final poses for finite bounded
deformation and floor penetration, and exports the derivative, descriptor and
manifest. `rig-validation.json` contains the sampled bounds. No auto-rig service
or downloaded animation was needed. Original asset hashes appear in `checksums.json`.

The viewer advances Arin, Orin, then Dragon. Dragon turns cycle a mouth-socket
fire sweep, a claw strike and a charged fireball. The current preview includes stronger wings/arms, a held recoil, idle mouth heat,
and a charge/flight/explosion effect. Head tracking and cinematic composition remain
under development. Characters guard and play
their own Hit clips. Existing shared FireEmitter3D resources handle the breath;
no native game-specific helper or language syntax was added.

This is a lightweight preview rig, not a production cinematic rig. Original
angular wing surfaces and open-mouth topology remain. It does not include IK,
foot planting, facial controls, collision-driven damage, navigation or flight.
The rendered preview PNG is a Blender inspection render; the Party screenshot
under `docs/implementation/screenshots/orin-storm` is native runtime evidence.
