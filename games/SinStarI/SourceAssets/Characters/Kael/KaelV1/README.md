# Kael v1 — native sword boss candidate

The canonical package for Sin's September 18, 2026 Kael request. The original body
and sword remain unchanged in `Source`. The Viewer and Sin Star I both consume
`kael-v1-animation-checkpoint.glb` with `KaelV1.sm3d.json`.

| Property | Runtime checkpoint |
| --- | --- |
| Body / sword triangles | 42,000 / 6,996 |
| Exported vertices per part | 31,133 / 7,045 |
| Skeleton | 41 Mixamo bones + KaelSword |
| Body / sword PBR maps | 4K / 2K |
| Animations / sockets | 10 / 10 |
| GLB bytes | 28,098,500 |
| Solo / boss scale | 134-unit equipped bind height / exactly twice the solo scale |

The equipped bind height includes the upright sword; the body itself is about
100 world units tall at solo scale and 200 at boss scale. The blade was flattened
along its depth axis, preserving its front outline and UVs. Its measured source
blade depth went from 98.0 mm to 4.8 mm before the 72% equipment fitting scale.
The ornamental crossguard and pommel retain their intended depth.

The native Viewer exposes **Kael** and **Kael Party**. Kael Party uses Arin, Orin,
Zara and Mira against Kael, with the existing turn scheduler, healing and effects.
Sin Star I exposes **Characters → Kael** and **Battle Simulations → Kael Party**.
Kael has his own camera/head storage identity; his sword uses the normal Weapon/W
visibility control. No runtime pose-calibration bank is added for this version.

Read [the creation and repair journey](KAEL-CREATION-AND-REPAIR-JOURNEY.md) before
changing geometry, rigging, equipment or grounding. `Source/export-validation.json`
records every-frame floor checks and the model checksum; `Source/roundtrip-validation.json`
checks the exported GLB's clip starts and final Defend/Hit/Death poses.
`Previews/accepted-*` show the final equipped checkpoint.

Native validation passed: both Release builds, all twelve Sin Star I presentation
entries (including the exact Kael Party roster), 58 Viewer graphics/pointer/audio
checks, 42 calibration-transfer checks, 13 formatter groups and the 464-file style
gate. The cooked Kael SM3D files match between applications. Visible inspection
covered both native Kael tabs, sword animation, battle playback/pause and the
Sin Star I menu routes. `Source/native-validation.json` records this evidence.

Web adoption and browser validation remain on hold.
