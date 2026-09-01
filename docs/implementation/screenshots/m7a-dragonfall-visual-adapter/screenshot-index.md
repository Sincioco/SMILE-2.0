# Dragonfall M7A Visual Adapter Evidence

These PNGs record the M7A technical integration seam. They deliberately use the repository-owned articulated Character3D fixture, not production Arin art. The lab labels that boundary on screen and keeps `Release Mode 1` (forced Classic) visible.

| File | Dimensions | Bytes | SHA-256 | What it proves | Significance to Dragonfall |
| --- | ---: | ---: | --- | --- | --- |
| `m7a-native-mixed-adapter.png` | 1280 x 720 | 581,530 | `F96395604948E0E752E29D4E774E470DAECE693533D17B2D0DEDCC729BA33F79` | Native Direct3D 11 draws a Classic rigid proxy and a Character3D actor through the same Dragonfall-local adapter, then maps the fixture's `Impact` event to bounded Effects3D output. | Proves the permanent mixed-scene seam without replacing current Dragonfall art or mechanics. |
| `m7a-web-mixed-adapter.png` | 1280 x 720 | 49,699 | `AE1AD571D99504D22875F9806E594D6EAEB6A56133AADA3A5690DA5F6D9BA868` | WebGL2 draws the same mixed visual pair in the idle interval with four submissions and 50 triangles. | Proves cross-target adapter parity and shows the non-production fixture clearly. |
| `m7a-web-event-effects-mapping.png` | 1280 x 720 | 55,696 | `4C5E016A64A66EA3B1544797E6D4FDCA379EE772A5DB9ABF1FF91C60F18EE41C` | The fixture's `Impact` event resolves through the adapter's technical `HandTip` socket alias and spawns the Holy Sword Strike preset; the frame reports six submissions and 340 triangles. | Proves animation events can synchronize presentation while battle mechanics remain authoritative and unchanged. |

The cube is intentionally only a Classic rigid-path proxy. The tiny articulated figure is intentionally only the deterministic M3/M4 technical fixture. Neither is presented as a visual-quality comparison with the user-supplied Paladin GLB.
