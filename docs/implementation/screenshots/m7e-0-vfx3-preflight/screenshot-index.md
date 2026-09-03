# M7E-0 VFX Generation 3 Preflight Evidence

All seven files are true PNG captures from the deterministic technical `AetherBladeActor` fixture. No Paladin candidate or Arin model binary was modified for M7E-0.

| File | Target/state | Dimensions | Bytes | SHA-256 |
|---|---|---:|---:|---|
| `01-energy-blade-idle-native.png` | Windows DirectX; idle socket blade | 960x540 | 21,353 | `1EB11245A2231735E8CECFB68B6BF0DA5A43BA618A7CC3F590A4D0A80F2B3CBE` |
| `02-energy-blade-swing-native.png` | Windows DirectX; animated swing/trail | 960x540 | 20,509 | `B11ACD630DE7D99B313B12ABC8BFA65F45C047EFDCB853719ABD6FEE3FA26A8A` |
| `03-energy-blade-idle-web.png` | WebGL2; idle socket blade | 960x540 | 22,817 | `B2FE7E7D17F8C37D1EE6BBC745BF74010CB5C34B55568FD0258B414170ABF1EF` |
| `04-energy-blade-swing-web.png` | WebGL2; animated swing/trail | 960x540 | 23,204 | `5907B6E25D708174B06F182E3D7ED7355B81A6D1741D6086FDD766BB05436DB9` |
| `05-cpu-fallback-vfx-lab.png` | Windows DirectX; visible CPU fallback | 960x540 | 22,137 | `877DD42307CAAFE6A16D49C5582726748EC253B5766AD366701A17113120BFBB` |
| `06-capability-diagnostics.png` | Windows DirectX; requested/effective modes | 960x540 | 21,357 | `E5CC39AF4CF9113E7CB1F0F7875FF42BBE4671933AE0E281777C504616C3AF7B` |
| `07-iphone-contact-sheet.png` | WebGL2 portrait idle and landscape swing | 900x900 | 39,109 | `625B64FF036C4D496F2B1156D2F0D38FF39693ABB1E08B1C43DADFCE75E5D6FB` |

The fixture is intentionally schematic: its purpose is to prove socket sampling, cross-target parity, capability reporting, bounded trail discontinuities, and the disabled-Generation-3 compatibility path independently of production-character topology.
