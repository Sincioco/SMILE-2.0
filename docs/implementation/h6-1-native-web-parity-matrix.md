# H6.1 native/Web parity matrix

Endpoint: `bc6f607bec5a60df1e72a0d3541156bc9175fe82`. PASS means the recorded scoped
checks passed, not exhaustive platform/visual identity. VM means generated JavaScript
with simulated browser/graphics objects. Real browser means actual visible Chrome
or specifically identified historical Edge actions. Firefox is NOT-RUN throughout,
explicitly removed from the required baseline by Sin, not silently waived.

| ID / area | Native | Generated Web / VM | Actual Chromium evidence | Evidence / differences |
|---|---|---|---|---|
| P01 Language, console, text | PASS | PASS | Visible tool labels/status and exact two-actor fixture output | E-regression, E-actors; 300 shared tests; not every console program manually replayed |
| P02 2D images, text, clipping | PASS | PASS | Viewer/Lab overlays and backgrounds render | E-rendering, E-regression; ordinary 2D fixtures use native/VM proof |
| P03 UI/input/layout | PASS | PASS | Fullscreen, reset, focused keyboard, tab/inspector and user physical navigation | E-input, E-camera; no physical mobile-device claim |
| P04 Safe camera and anchoring | PASS | PASS | Actual scene navigation plus Sin's Desktop/Chrome input check | E-camera; checked integer arithmetic retained |
| P05 Assets/PBR/skinning/equipment | PASS | PASS | Four Viewer tabs, accepted poses and hidden equipment | E-rendering, E-effects; platform rasterization need not be pixel-identical |
| P06 Backdrop/post/MSAA | PASS | PASS | Anchored background; Fire High AA4; HDR/LDR and AA-only render | E-rendering; capability-selected 4/2/1 fallback |
| P07 Thermal Fire | PASS | PASS | High/GPU visible; supported path and forced fallback separated | E-thermal, E-lifecycle |
| P08 Lightning/comfort | PASS | PASS | Ultra GPU and live two-context GPU/CPU scenes | E-actors, E-thermal; scene comfort authoritative |
| P09 Freeze/visibility/disposal | PASS | PASS | Frozen hide/show, Dragon cut and reseed | E-effects; scene-owned clock and explicit cut identity |
| P10 Two live same-model actors | PASS | PASS | Actual Chrome and historical Edge normal/forced fallback | E-actors; independent animators/corrections/effects, not effects-disabled mocks |
| P11 Audio overlap/focus | PASS fixtures | PASS | Real activated sources2→0 on blur; no catch-up | E-lifecycle; scheduling is not physical audibility |
| P12 Calibration/storage/transfer | PASS isolated | PASS failure injection | Native/Chrome transfer/Undo; actual distinct import persists/re-exports | E-storage; browser origin storage cannot directly synchronize D: files |
| P13 Context/device recovery | PASS existing fixture/source checks | PASS loss/restore regression | Actual extension loss/restore; all three tools recover | E-lifecycle; Viewer selection recovery, Labs automatic; no native physical device removal |
| P14 RPG/existing games | PASS normal smoke | PASS normal smoke | Not manually replayed exhaustively | E-regression; gameplay/manual device coverage remains a limitation, not a fake browser pass |

Evidence IDs refer to sections of
[native/browser observations](h6-1-native-and-browser-observations.md). The complete
[issue register](h6-1-known-web-issues.json) reconciles all F01–F05 and W01–W21.
The [report](h6-1-hardening-and-web-parity-report.md) declares package, saves,
scope overrides, builds and final acceptance. The chronological ledger preserves
intermediate failures rather than replacing them with final pass labels.
