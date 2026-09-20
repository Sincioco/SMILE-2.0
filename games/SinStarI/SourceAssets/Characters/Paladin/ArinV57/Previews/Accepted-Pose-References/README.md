# Arin accepted pose references — September 20, 2026

These images inspect Sin's Viewer-exported JSON, unchanged at SHA-256
`8896989dd711ebba1cd2f62ffd1a840774287ed9d52e53663c66fe5b4e83e8dc`.
The JSON remains authoritative; later intentional Viewer exports supersede these
references. Do not copy pose values back from images or the test baseline.

Later on September 20, the approved Victory asset addition migrated only the
JSON's asset identity and added an empty Victory bank. All 24 authored keys and
the nine original frame-zero matrix rows were verified exactly unchanged. The
matrix inventory now also includes Victory; the existing images below continue
to describe the original nine clips. The current JSON hash is stored alongside
the matrices and in the package manifest.

Every image is a native capture at animation frame **0**, with equipment flames
disabled to expose the grips. Viewer images show the interactive editor from the
front. Game and Fire Lab images show the opposite side using temporary native
capture programs calling their actual production presentation/scene owners.
Those programs only select clips, pause time, set a review camera and label the
frame. They do not author corrections. Game glow, lighting and backgrounds differ
from the Fire Lab capture; compare wrists and equipment, not brightness.

| Animation | Character Viewer — front | Sin Star I — rear | Fire Lab — rear |
| --- | --- | --- | --- |
| Idle | [Image](CharacterViewer-Arin-Idle-Frame-000-Front.png) | [Image](SinStarI-Arin-Idle-Frame-000.png) | [Image](FireLab-Arin-Idle-Frame-000.png) |
| Sword Attack | [Image](CharacterViewer-Arin-SwordAttack-Frame-000-Front.png) | [Image](SinStarI-Arin-SwordAttack-Frame-000.png) | [Image](FireLab-Arin-SwordAttack-Frame-000.png) |
| Sword Attack 2 | [Image](CharacterViewer-Arin-SwordAttack2-Frame-000-Front.png) | [Image](SinStarI-Arin-SwordAttack2-Frame-000.png) | [Image](FireLab-Arin-SwordAttack2-Frame-000.png) |
| Defend | [Image](CharacterViewer-Arin-Defend-Frame-000-Front.png) | [Image](SinStarI-Arin-Defend-Frame-000.png) | [Image](FireLab-Arin-Defend-Frame-000.png) |
| Block Impact | [Image](CharacterViewer-Arin-BlockImpact-Frame-000-Front.png) | [Image](SinStarI-Arin-BlockImpact-Frame-000.png) | [Image](FireLab-Arin-BlockImpact-Frame-000.png) |
| Hit | [Image](CharacterViewer-Arin-Hit-Frame-000-Front.png) | [Image](SinStarI-Arin-Hit-Frame-000.png) | [Image](FireLab-Arin-Hit-Frame-000.png) |
| Walk | [Image](CharacterViewer-Arin-Walk-Frame-000-Front.png) | [Image](SinStarI-Arin-Walk-Frame-000.png) | [Image](FireLab-Arin-Walk-Frame-000.png) |
| Run | [Image](CharacterViewer-Arin-Run-Frame-000-Front.png) | [Image](SinStarI-Arin-Run-Frame-000.png) | [Image](FireLab-Arin-Run-Frame-000.png) |
| Death | [Image](CharacterViewer-Arin-Death-Frame-000-Front.png) | [Image](SinStarI-Arin-Death-Frame-000.png) | [Image](FireLab-Arin-Death-Frame-000.png) |

`frame-zero-transforms.json` records all twenty correction channels plus the
HandRight, HandLeft, SwordTip and ShieldCenter world transforms. Run
`scripts/test-viewer-calibration-native.ps1` to check the Viewer and
`tools/AdvancedFireVfxLab/Test.ps1` to compare the Fire Lab after accounting for
scene placement. Clip names, not asset enumeration indices, identify poses.
Refresh the baseline with `-AcceptPoseReference` only for an intentional accepted
JSON change, then recapture the affected images. Neither test modifies user saves.
