# H6.1 Hardening and Web Parity Ledger

Scope: W0-W6 from the user-supplied H6.1 package. No Battle Scene Editor E0-E12
work is authorized or started. Historical H6 `PASS-NATIVE` remains historical;
H6.1 is **IN-PROGRESS**, with real-browser acceptance still outstanding.

## Baseline and preservation

Reviewed baseline: `902a7022c895bf97010d979ea578fc5361cdcbf4`. Actual branch is
`main`; no reset/rebase was performed. Package identity/hash-checked intake and
full current Arin/Orin package copies are retained under ignored
`artifacts/temp/codex-handoff/2026-09-05-smile-2.0-h6-1-hardening-and-web-parity/`.
The `preservation-start-902a7022` copies retain the pre-edit assets and calibration.

Arin retains 23 keys and snapshot SHA-256
`1747367DD5E411D8230AB5159DE1309F221867C8DE6745661DA1396EAE6DB867`.
Orin retains zero saved keys, its distinct storage key and clip identities; the
explicit Jump Attack asset migration updates only its asset hashes/fingerprint.
The historical zero-key snapshot is not restored over a newer save.

## Validated milestones and current work

| Milestone | Commit | Actual commands/results | Remaining work |
| --- | --- | --- | --- |
| W1 safe camera math and urgent Orin shot fix | `5c2036afb4435fb7375d6515f15491746bdf5560` | Native/shared generated-Web reference tests passed; attack camera no longer follows unbounded animated-model bounds. | Real-browser camera evidence remains required. |
| W2 scene-owned comfort and frozen ownership | `b8fce49701738fcab3c45d7d5cdb343e1e4a9b33` | `scripts/test-character-3d-viewer-actor-isolation.ps1`: native active two-Orin/GPU and forced fallback plus generated-Web exact-output checks passed. | Real-browser integrated effects observations remain required. |
| User-reported Viewer regressions | Implementation commit containing this entry | `tools/Character3DViewer/Build.ps1 -Configuration Release`; `scripts/test-character-3d-viewer-hardening.ps1` **without** `-NativeOnly`: PASS, including seeded native calibration/tab loads, generated-Web hardening and 58 native graphics/input/audio checks. `scripts/test-character-3d-viewer-actor-isolation.ps1`: PASS after repaired Orin asset. Scoped formatter/style checks passed. | Direct off-window mouse scrub and comprehensive camera interaction evidence still required. |
| W3/W4 Web renderer parity | Uncommitted/in progress | Compiler build, `scripts/test-renderer3d-gpu-particle-webgl2.ps1` and `scripts/test-native-thermal-fire.ps1` passed during implementation. Viewer and both Labs published to ignored `artifacts/web/h6-1`. | Real shader/drawing checks, full workflows, current documentation, final VSIX installation and W6 remain. |

## Viewer regression checkpoint

- Jump Attack: surgical GLB Root translation replacement grounds sample 37 through
  71 at the measured Idle sole height (3/1000 model units), preserving samples
  0-36 and other clip/mesh/material/skin data. A broad rebuild changed accepted
  Death data and was rejected; only the surgical repair is retained.
- Repaired Orin GLB SHA-256:
  `3100F1ACB2E9B1F3776E8A94C113BB23C0A90A7141590A0936958991CC99DC0C`.
  Cooked SM3D: `1741564CDBD4F3ADB305AACD1AAD295B373672D34E1490C103601E8BD4BF8DAF`.
  Current zero-key snapshot: `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.
- Fixed stale Orin runtime fingerprint after migration. The old test's empty-save
  fixture missed companion rejection. It now seeds isolated copies of current
  canonical Arin/Orin saves and loads Arin, Orin, Dragon and Party.
- All tabs retain above-solid-floor camera placement; grid-only permits underside
  inspection. Timeline scrubbing captures outside-window motion/release exclusively.
- Party rotates Arin's two and Orin's three attacks. Boss-first/extra guard beats
  and one latched dragon aim/hit/KO target improve presentation consistency.
  No live combat damage, MP, rewards or game-save mutations are introduced.
- Native `tools/Character3DViewer/bin/Character3DViewer.exe` relaunched through
  `Launch.ps1`; agent observed populated Dragon and Party scenes. Sin then explicitly
  reported: “I can visually see all 4 tabs work again.” This confirms tab loading,
  not blanket artistic approval or completion of the browser gate.

Both calibration exports are required again immediately before this milestone's
commit/push. Web compiler work is deliberately kept out of the Viewer bug-fix commit
until actual browser validation. The final affected VSIX rebuild/install/verification
is still outstanding and must not be reported complete from the historical H6 run.
