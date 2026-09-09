# Unity Character Import Checkpoint

## Implemented Local Roster

The normal Viewer project permanently declares Arin, Orin, Valor, Zara, Dragon,
Vrax and Party tabs. The three purchased character exports remain in their complete
private versioned packages and are checksum-verified into ignored project-local
cooking mirrors. Party owns four independent hero
actors and uses Vrax as its opponent. Dragon's tab, package and animations remain
available. Valor and Zara also use Vrax as the opponent in their individual arena
previews, while Arin and Orin keep the Dragon. The normal animation demo and clip
buttons continue to drive the selected hero. A profile-owned 2x camera-distance
policy frames the enlarged Vrax and hero together. No Sin Star I wave/game
implementation changed.

| Actor | Package Under games/SinStarI/SourceAssets | Original / Viewer Triangles | Bones | Clips |
|---|---|---:|---:|---:|
| Valor | Characters/Knight/ValorV1 | 48,777 / 48,626 | 174 | 31 |
| Zara | Characters/Warrior/ZaraV1 | 106,578 / 47,810 | 69 | 26 |
| Vrax | Bosses/Vrax/VraxV1 | 122,890 / 55,291 | 150 | 24 |

Each package preserves selected-prefab dependencies, FBXs, textures, full-animation
Blender source and GLB, conversion scripts, checksums, clip manifest and grounding
evidence. Restricted originals/derivatives are in ignored Private folders. A normal
build requires all three licensed exports and fails preparation with an actionable
error if one is missing. `Build.ps1 -Target Web -PublicRoster` remains an explicit
compatibility publication for checkouts without those packages.

The inspector presents nine clips per page. Nonlooping actions hold their final
frame; locomotion and Idle loop. Party cycles Vrax, Arin, Orin, Valor and Zara using
each actor's own clips and durations. Four-member Party uses a wider orbit view.
Vrax's original auto-fit clamped at 10000%; his profile now permits 20000%, exactly
twice the previous actor transform, in inspection and Party. Precise auto-fit has an
optional maximum scale bounded by Character3D's existing 25000% limit; existing
callers retain their 10000% default.

## Source Applications

Unity 2022.3.42f1 was visibly open on Assets/Scenes/BattleSystem.unity in
D:\Knights of Bits and Bytes. Hierarchy and serialized references agree on three
actors: ValorPlayer, ZaraPlayer and SciFiBeastEnemy. Unity MCP at localhost:8080
refused the connection; MCP control was not verified. Unity 6 is installed, but
migration was unnecessary. The original project/editor version was preserved.
Blender 5.2.1 LTS performed CLI conversion; its configured MCP socket was not
connected. The user's unsaved interactive Blender scene was preserved.

## Corrected Defects

- Zara's scrambled texture came from DPI-sensitive DrawImageUnscaled. Explicit
  source/destination pixel rectangles and SourceCopy now preserve RGBA pixels.
  Cooker v4 invalidates stale caches; all four base-color atlases match exactly.
- Valor's sideways pose came from removing its armature object during glTF export.
  Retaining that transform and placing skinned mesh nodes at scene identity fixes
  the export. Idle joint world positions match Blender within 0.00000104 metres.
- Orin's Party VFX update returned early because Vrax has no Chest socket. Missing
  sockets now use the boss's precise world-bounds center. Equipment effects update
  normally. Orin's accepted assets, calibration and three VFX styles are preserved.
- Valor/Vrax weighted ancestry exceeds 128 bones. Shared cooker and native/Web
  palettes now admit 192; 193 is rejected. The procedural 32-bone API is unchanged.
  Source buffer length is bounded at 64 MiB and source accessor/buffer-view counts
  at 16384 for full animation sets. GLB, aggregate-buffer and cooked limits remain.
- Tiny invalid triangles, undefined tangents and Valor's authored zero-scale
  shoulder keys are repaired only in derivatives. Zero scales use 0.000001.

## Grounding And Reduction

Frame zero of all 81 clips was measured by CPU skinning, excluding equipment/VFX.
Bind/Idle minima and asset hashes are stored per package. Full Block/Hit/Death
curves cover 1,959 samples. Clip-name corrections remove initial placement errors
and downward penetration; jumps preserve authored lift, and Death maintains body
contact. Compressed correction error stays below 0.0025 source metres, below
0.5 world unit even at Vrax's 20000% scale.

Zara/Vrax retain about 45% of their original triangle budgets with 1024-pixel texture
derivatives. Four-weight export affects 78/54730 Valor, 90/26895 Zara and 41/31091
Vrax Blender vertices. Maximum discarded normalized weights are 13.01%, 4.84% and
9.82%; means are 0.00487%, 0.00385% and 0.00155%. This is an approximation, not a
lossless deformation claim. Original full-weight sources are preserved.

## Validation

Detailed local logs are under artifacts/temp/unity-import:

- Full compiler/runtime/asset-tool/VSIX builds: complete-set-build.log.
- 192-bone native/Web acceptance, weighted bone 191 and 193 rejection:
  bone-capacity-tests.log.
- Exact-pixel 72/144-DPI cooking, cache and native/Web publication:
  texture-dpi-tests.log.
- Exact 16384 source-table boundary and 16385 rejection: boundary gate passed.
- Normal foundation smoke: import-foundation-smoke.log.
- Native Viewer/calibration checks including exact 2x Vrax auto-fit:
  final-viewer-hardening.log.
- Native/local Web roster builds: final-viewer-build.log.
- VSIX 2.0.61 installed; all 35 payload hashes verified: import-vsix-install.log.
  Smile.VisualStudio.dll SHA-256:
  28A980E7ACE9A0A1E245736A71F6913657511BDE9143D16D4E80C440ACDBAD07.
- Native individual/Party clips, Orin individual/Party VFX and doubled Vrax were
  visually inspected. Inline JPEG progress screenshots were saved.
- Chrome displayed correct Zara materials, upright Valor, large Vrax and Party;
  clip-page navigation and Zara's Block/settled Death were inspected on localhost.
- Chrome rendered Valor/Vrax and Zara/Vrax individual attack previews from the
  rebuilt Web publication with the complete enlarged opponent in frame.

Native/Web Release builds passed in release-roster-build.log. The final native
executable is tools/Character3DViewer/bin/Release/Character3DViewer.exe; local Web
output is its sibling Web folder. VSIX output is artifacts/vsix/Smile.VisualStudio.vsix.
No language syntax was added. The final 434-file formatting check passed. Native
and Chrome slow/moderate pan, fine/larger orbit adjustments, wheel zoom in/out,
keyboard orbit and camera reset were exercised. The native Release launched through
Launch.ps1 with calibration watchers and was left foreground. Arin's 24 keys and
Orin's zero-key snapshot were exported; canonical calibration content and Dragon
assets have no changes. Repository commit/push evidence is recorded in Git and chat.

## Boundaries

Valor/Zara now have measured equipment VFX sockets and separate weapon/shield
visibility through the shared Viewer. See the [equipment VFX checkpoint](character-equipment-vfx-checkpoint.md)
for their white/blue appearance and validation. Original equipment remains part
of each animated import; pose/socket editing is still unavailable for these rigs.
Arin/Orin editing and calibration remain independent.

Private assets have not been publicly deployed or redistributed. Machine operation
permission does not establish commercial-asset redistribution rights. Public
repository delivery includes code and package metadata only.
