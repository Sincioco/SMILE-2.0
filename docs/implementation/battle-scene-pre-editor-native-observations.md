# Battle Scene Prerequisite Native Observation Record

Date: 5 September 2026

This is a textual observation record, not stored screenshot proof. The Windows capture automation rendered real point-in-time screenshots during the checks, but its operating contract forbids decoding or saving the returned screenshot payload. No repository screenshot path, dimensions or checksum can therefore be supplied without violating that contract. The automation limitation does not replace the executable native tests cited below.

## Build identity

- Source commit: `56513fbcf74ebe3f192b6e34f2d230ec161425f7`
- Compiler: `artifacts/compiler/smilec.exe`
- Compiler SHA-256: `BFB4AA71C967656C4C3BB735DD721E926E6160D31321693ACC4A865D5CE4F2FF`
- Final Viewer: `tools/Character3DViewer/bin/Character3DViewer.exe`
- Final Viewer SHA-256: `94CD91FB5A2F1D6A0D69E8E25E12A5C8C8F47CE6D712E7184E058A8C962B4041`
- Lightning Lab: `tools/AdvancedLightningVfxLab/bin/Debug/AdvancedLightningVfxLab.exe`
- Lightning Lab SHA-256: `7E10A2DBF9D6C938FAB4827D977603930A83106E5A0BE5F81C08BD8137FCC7C2`

The final Viewer hash above was rebuilt after the last compiler/VSIX build and then launched successfully. Earlier interaction checks used the same committed source in a prior deterministic-input build whose observed SHA-256 was `B6D23B95F9FDDD073F20C269C291BB1F764914822C6434E34FA750C0AE4911BF`.

## Character Viewer actions

- Launched the native release Viewer and observed live Party playback with Arin, Orin and Red Dragon assets, equipment, glows, fire and lightning presentation at interactive frame rates.
- Pressed bare Alt; playback continued and no orphaned system menu or modal appeared.
- Toggled Alt+Enter into 1920x1080 fullscreen and back to the 1420x1021 window. Rendering and controls remained responsive.
- Applied one moderate and one small viewport pan. Applied wheel input in both directions, changed horizontal and vertical orbit sliders, and used right-click reset. Motion remained fractional/bounded and reset restored the expected composition. At the high tested vertical pitch the moving Party temporarily left the frame; reset recovered immediately, so this is recorded as nonblocking composition review rather than user aesthetic approval.
- Independently froze Fire and Lightning, then resumed Fire while Lightning stayed frozen, followed by Lightning. The on-screen control labels changed independently between `Freeze` and `Play`; the other family and scene animation continued.
- Opened Arin and Orin individual tabs. Selected and allowed each accepted Death clip to settle with the Dragon hidden. Body, sword/hammer, shield and glow followed the grounded actor pose; no historical calibration was imported.
- Returned to Party playback and observed explicit guard, hit, KO and revive-on-own-turn presentation without a crash or stale effect burst.
- Opened the Dragon tab, disabled demo playback for direct inspection, exercised Head Aim/target controls, and observed the target label advance from `At Arin` to `At Orin`.
- Minimized and restored the Viewer. Rendering resumed normally. Closed it, confirmed that its window disappeared, relaunched the exact repository executable, and confirmed normal rendering again.
- After the final compiler build, rebuilt the Viewer to SHA-256 `94CD91FB5A2F1D6A0D69E8E25E12A5C8C8F47CE6D712E7184E058A8C962B4041`, launched it, observed normal Party rendering, and closed it cleanly.

The automation API did not provide a held-middle-button drag primitive. Direct MMB dragging is therefore not claimed. Native pointer capture, recovered press/release edges, cancellation and stale-gesture handling passed the 58-check native executable harness. Audio focus muting is also supported by that harness; the visual automation did not make an audibility claim.

## Lightning Lab actions

- Launched the native Advanced Lightning Lab and observed `Godstorm Ultra`, `Sky Strike`, `Forked Judgment` and `Charged Weapon` live presentation at interactive frame rates.
- Pressed bare Alt; no system menu or modal interrupted playback.
- Paused and resumed playback. The paused status appeared and cleared as expected.
- Minimized and restored the Lab; rendering resumed normally.
- Closed the Lab and confirmed that its window disappeared.
- Rebuilt the Lab with the final compiler to SHA-256 `7E10A2DBF9D6C938FAB4827D977603930A83106E5A0BE5F81C08BD8137FCC7C2`, launched it, observed normal `Sky Strike` rendering, and closed it cleanly.

## Automated companion evidence

- `pwsh -NoProfile -File scripts/test-character-3d-viewer-hardening.ps1 -NativeOnly`: PASS; 58 native graphics, pointer-input and audio-focus checks.
- `pwsh -NoProfile -File scripts/test-viewer-calibration-native.ps1`: PASS; isolated application identity and both current/previous-good Save Data envelopes validated.
- `pwsh -NoProfile -File scripts/test-lightning-vfx-foundation.ps1`: PASS native plus exact Web console parity.
- `pwsh -NoProfile -File scripts/test-native-thermal-fire.ps1`: PASS; 21 thermal checks, native GPU recovery and FireEmitter native/Web parity.
- `cmd.exe /d /c scripts/smoke-test.cmd`: PASS; ordinary repository smoke completed.

No user visual approval is claimed.
