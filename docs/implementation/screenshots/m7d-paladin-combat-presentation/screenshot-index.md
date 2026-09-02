# M7D-A Paladin Combat Presentation Evidence

Every entry uses source asset SHA-256 `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3` and committed cook SHA-256 `B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394`. Times are the captured animation milliseconds; `~` marks a capture-timing approximation. The High quality profile was requested throughout. WebGL2 reports only the expected `SCENE_FALLBACK_FLAG_MSAA_REDUCED` because the Web backend is single-sample.

| PNG | Asset / cook SHA-256 | Clip / time | Event | Socket | Quality / target | Draws / triangles | Production implication | Known issue |
|---|---|---|---|---|---|---:|---|---|
| `01-idle-ready-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | Ready / ~533 ms | None | SwordTip diagnostic | High / native DirectX | 6 / 10,310 | Ready pose is technically usable by the candidate. | Production approval remains gated. |
| `02-run-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | Run / ~584 ms | FootstepRight 2002 | SwordTip diagnostic | High / native DirectX | 6 / 10,310 | Forward-motion clip and event timing are readable with subject-relative wide framing. | Authored forward travel requires a following/wide camera. |
| `03-sword-anticipation-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | SwordAttack / ~465 ms | SwordTrailOn 1001 | SwordTip | High / native DirectX | Anticipation and socket-following ribbon are technically accepted. | Production animation approval remains gated. |
| `04-sword-impact-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | SwordAttack / ~863 ms | SwordImpact 1002 | SwordTip / target contact | High / native DirectX | Presentation event aligns particles, light, hit-stop, and caller audio without damage authority. | None beyond production gate. |
| `05-shield-bash-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | ShieldBashCandidate / ~570 ms | ShieldImpact 1101 | ShieldCenter | High / native DirectX | Shield event/socket/VFX contract works. | Retain as candidate and revise before production approval. |
| `06-defend-block-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | BlockImpact / ~483 ms | None | ShieldCenter camera focus | High / native DirectX | Defensive presentation state is technically usable. | No frame-specific event is authored. |
| `07-hit-ko-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | KO / 2,333 ms | None | Foot sockets reviewed | High / native DirectX | Final held KO pose reaches the ground in the current candidate. | Final user deformation approval remains gated. |
| `08-victory-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | Victory / ~1,022 ms | None | SwordTip diagnostic | High / native DirectX | Victory presentation is technically usable. | Full 8.567-second artistic review remains a production gate. |
| `09-sockets-native.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | Ready / ~399 ms | None | All 10; SwordTip selected | High / native DirectX | Cooked socket enumeration and all-socket gizmo capacity are proven. | Gizmos are inspection-only. |
| `10-sword-impact-web.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | SwordAttack / ~800 ms | SwordImpact 1002 | SwordTip / target contact | High / WebGL2 | 8 / 10,476 | Web consumes the same cooked bytes and event/VFX mapping. | Expected MSAA-reduced fallback flag 128. |
| `11-shield-bash-web.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | ShieldBashCandidate / ~775 ms | ShieldImpact 1101 | ShieldCenter | High / WebGL2 | 7 / 10,390 | Web shield socket/event/VFX parity is proven. | Candidate motion still needs production acceptance; expected MSAA fallback. |
| `12-native-web-comparison.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | SwordAttack / impact comparison | SwordImpact 1002 | SwordTip / target contact | High / native + WebGL2 | 8 / 10,476 each | Direct visual parity artifact for the same cook and authored event. | Platform rasterization and Web MSAA differ as documented. |
| `13-material-channels.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | Ready / ~396 ms | None | All-socket overlay retained | High / native DirectX, Base Color | 7 / 10,330 | Imported base-color inspection is available independently of lighting. | 1K lossy source textures do not pass M7D-B. |
| `14-iphone-contact-sheet.png` | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3 / B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394` | Mixed required states | Mixed authored events | Mixed reviewed sockets | 1,170×2,532 mobile review sheet | Per source frame above | Mobile-readable index of the complete native/Web evidence set. | Contact sheet is review evidence, not a runtime viewport claim. |

All files are true PNGs. Native captures are 1,282×752; Web captures are 1,280×720; the comparison is 2,560×800; and the contact sheet is 1,170×2,532.

## File integrity

| PNG | Dimensions | Bytes | SHA-256 |
|---|---:|---:|---|
| `01-idle-ready-native.png` | 1,282×752 | 379,069 | `8EF29D0FA203CFC6780C9C1A41625444CC52539EAA0BBF43419F6E7E71A07786` |
| `02-run-native.png` | 1,282×752 | 372,915 | `F4C925B680CDFCA821D8D9F59E0C14FBCFD0810B90A7F434F138F91BDD79987F` |
| `03-sword-anticipation-native.png` | 1,282×752 | 381,497 | `03ABA334E2A18D21C595877B5DD3D269A1A14AFA83B08EA169AEFC413BD96B01` |
| `04-sword-impact-native.png` | 1,282×752 | 414,230 | `16664D55BA913A848A2B1AC5CC10476ABB267B0F24D82D292CEBBE6E80793213` |
| `05-shield-bash-native.png` | 1,282×752 | 402,466 | `1616D5B56A5DA8862D7E82BDB6151F241B5FAB3B1B56B6C877A0C8A695396B67` |
| `06-defend-block-native.png` | 1,282×752 | 360,994 | `C5C2C15D7121B3C68EBFDA407DB51CD052E951B54C80155DE0B7D5F329836F4D` |
| `07-hit-ko-native.png` | 1,282×752 | 346,671 | `83B3F3E41F4839B585EB1D8DFA2E28E705CD678789E9A7D00E888B8D2F650BD9` |
| `08-victory-native.png` | 1,282×752 | 385,616 | `32E0BED4364109460DEA828A83647D6DBEE541B6DB3BC8948AC2B8A6F665BF66` |
| `09-sockets-native.png` | 1,282×752 | 376,576 | `4FEA1A48E3BC0361DB7FA5C37A9C9AE86365425FC5308D200AFD3A9D28328031` |
| `10-sword-impact-web.png` | 1,280×720 | 397,885 | `484D1D9471253C6035EFB1C79717C81ECADC1B8AC53E299B37315CCFB731E306` |
| `11-shield-bash-web.png` | 1,280×720 | 396,973 | `6C513DE1E27EFF1E25F5E067270EC97C2EB5AAFD7A28E6D65BD0787DA2F5D522` |
| `12-native-web-comparison.png` | 2,560×800 | 960,306 | `67CC24ED2FB6D076D2D2474BD4904B9B5F5EC9BA70AA1E3D1E5F8CA0EDA056E7` |
| `13-material-channels.png` | 1,282×752 | 385,780 | `08BCDF34B9CF449FE79CB8642A029DD80B5C863AD6B124AA73E58750B7B60C48` |
| `14-iphone-contact-sheet.png` | 1,170×2,532 | 1,064,925 | `DD61E486E8C8F845819F04D812856F86BC355CF3115C272E3B10D16B0263CA35` |
