# M6.1 VFX Hardening Screenshot Index

These committed PNG files provide phone-readable evidence for the completed M6.1 milestone. The native, Web, stress, sword-ribbon, and HDR images preserve the already validated screenshot pixels in PNG form. The direct-LDR image is a fresh WebGL2 capture from the same pushed build with HDR and bloom disabled; the browser warning/error log was empty.

| Screenshot | Dimensions | Bytes | SHA-256 | What it proves | Significance to Dragonfall |
| --- | ---: | ---: | --- | --- | --- |
| `m6-1-native-vfx-lab.png` | 962 x 572 | 374,255 | `FC0CCB9C6C8E4DE838D93A6E59A70D1573A84B1B15507E0C0D1761771117568F` | The current Direct3D 11 lab renders bounded alpha/additive particles, a ribbon, HDR/bloom, ground geometry, and live diagnostics. | Direct3D remains the primary production path for Dragonfall's modern battle presentation. |
| `m6-1-web-vfx-lab.png` | 1280 x 720 | 431,096 | `70F6A3C856A29E366D6077182F1FAC3CB0F2BCBAB9C2A846A9A06039CA00EB64` | The same SMILE source renders through WebGL2 with matching bounded batch/resource diagnostics. | Dragonfall's Generation 2 presentation remains available to browser users and students. |
| `m6-1-particle-stress-1024.png` | 1280 x 720 | 408,760 | `192041D9D46254438C2A9DF8F1EC473CC4AC280C94FF81F74ED050E6D3F6FCB9` | The stress scene shows 1,152 active particles using three particle draws and no particle-correlated Object3D growth. | Dense impact effects can remain bounded instead of turning every particle into an object or draw call. |
| `m6-1-sword-ribbon.png` | 1280 x 720 | 412,967 | `329D6776192485DC9E1BE7E04D4E061BF71025C68FB6786D13988F696C08FDEE` | The Holy Sword effect follows the character socket with one bounded ribbon batch. | It is the reusable trail mechanism M7A maps to Character3D fixture sockets and M7B will map to Arin's authored sword sockets. |
| `m6-1-web-hdr-bloom.png` | 1280 x 720 | 431,096 | `70F6A3C856A29E366D6077182F1FAC3CB0F2BCBAB9C2A846A9A06039CA00EB64` | The diagnostics show the HDR/bloom path active while particles and the ribbon remain post-integrated and the 2D HUD stays exempt. | Dragonfall needs coherent emissive impacts and crisp HUD composition, not geometry alone. |
| `m6-1-web-direct-ldr.png` | 1280 x 720 | 63,971 | `1FE29ABF720994A4751E225C0C5F145D6D32B92E67DEE186454C670193821A1D` | A fresh WebGL2 capture shows HDR, bloom, and HDR target format all at zero, reduced target storage, a coherent direct-LDR scene, and a clean browser console. | The battle remains usable on hosts that cannot or should not use HDR targets rather than losing its complete 3D presentation. |

The duplicate hash for `m6-1-web-vfx-lab.png` and `m6-1-web-hdr-bloom.png` is intentional: the same captured Web frame proves both the overall Web path and the active HDR/bloom state.
