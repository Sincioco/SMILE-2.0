# Paladin v5.4 Viewer/Exporter Hardening Evidence

- Stable asset identity: `sin-star-i.character-1.paladin`
- Candidate version: `v5.4`
- Source GLB SHA-256: `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3`
- Published SM3D SHA-256: `508063F78C08B97DBD44ED19DC3A0D8C1DAAEF1A093D8F19E5A6929456993023`
- Model budget: 4 parts, 10,296 triangles, 4 materials, 9 textures, 42 bones, 46 nodes, 11 clips, 6 sockets.
- Normal scene budget: 6 draw calls / 10,378 submitted triangles.
- Socket selection budget: four axis objects; all-socket origins share one optional particle batch.
- Native captures: current DirectX renderer and cooked candidate at 1280x720 unless composed otherwise.
- Web captures: current WebGL2 renderer at 1280x720 with responsive-window and cooked-texture orientation parity enabled.
- `09-web-360-orbit.png` is the returned-front checkpoint after 72 deterministic 5-degree vertical orbit inputs (360 degrees total).
- `10-responsive-layouts.png` compares 800x540 minimum and 1440x700 wide layouts.
- `11-grid-gizmo-resource-counts.png` records the single-grid-draw and bounded-gizmo diagnostics.
- Shield Bash remains candidate evidence, not production approval.

| File | Dimensions | Bytes | SHA-256 |
| --- | ---: | ---: | --- |
| `01-native-idle-front.png` | 1280x720 | 307303 | `33565D3487B652907D50886EB63A1DC64C78C3AD7FF04E082D591F7B847F2276` |
| `02-native-sword-attack.png` | 1280x720 | 344336 | `ED620E767326B4AAADA9AFC5FBFF90EDB432DA2BF1A1243FA911167AA8F84E1A` |
| `03-native-shield-bash-candidate.png` | 1280x720 | 335015 | `39431FF17A6CFE8CBA66CEEC81F166E8FE7759479A867553180873EA78D017B7` |
| `04-native-ko-grounding.png` | 1280x720 | 182133 | `D8083CD62CB04FFB386BC4C9AAB81A1974A5CAD32E551458A44099946D213F03` |
| `05-native-socket-gizmos.png` | 1280x720 | 308344 | `E3C811AC22709F5671165B1D4FDD0DA0AF30BD55BB12A877009A31CE3977693D` |
| `06-native-material-channels.png` | 1280x720 | 344486 | `54A0959ADBFEDD8FDD4BB2266214CEB084CF959FABE789651AE3E733DD818B74` |
| `07-web-idle-front.png` | 1280x720 | 856484 | `B041120E85F5FAAD3DBBD11C5150F23FA753FB919EA8399707494CA1910B286B` |
| `08-web-sword-attack.png` | 1280x720 | 816127 | `92584E45E19DA288E9E47FD84E95741F78E9B81B1A971320696FAA8974EEE5D2` |
| `09-web-360-orbit.png` | 1280x720 | 860286 | `E77283594386C23B4C26FCB45483D83C8EC7EF8D96AA2212EC62FD8513514D49` |
| `10-responsive-layouts.png` | 1440x457 | 452552 | `D2D5887C93C88E921893FF92E34444D9E579D2F9C0262DDF06C46A84A739B0E7` |
| `11-grid-gizmo-resource-counts.png` | 1280x720 | 286990 | `35C60CFB606344C4DB845554C2E06A6E5CA5340F92249534D83D807E2116DF3D` |
| `12-iphone-contact-sheet.png` | 1280x2400 | 1646247 | `D9BD7DC284DCB98AC6E58000DDCEB2DFAF427170413750DA414BEB155BF08F49` |