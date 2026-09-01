# M6.1 Mobile Review Notes

These screenshots were captured from the validated M6.1 Renderer3D VFX Lab build. The same SMILE source drives the native Direct3D 11 and generated WebGL2 applications.

- `m6-1-01-native-vfx-lab.jpg` shows the native Direct3D 11 lab with alpha/additive particles, the socket ribbon, HDR/bloom, and live bounded-resource diagnostics.
- `m6-1-02-web-vfx-lab.jpg` shows the matching WebGL2 lab and its current draw, triangle, upload, memory, quality, and fixed-step diagnostics.
- `m6-1-03-sword-ribbon.jpg` shows the Holy Sword Strike attached to the character's `HandTip` socket. Invalid actor/socket attachment now stops the effect safely instead of retaining a stale attachment.
- `m6-1-04-particle-stress.jpg` shows the bounded stress path. Thousands of particles are submitted through fixed particle batches rather than one `Object3D` per particle.
- `m6-1-mobile-contact-sheet.jpg` combines all four views into one phone-readable image.

HDR/bloom is used when the target supports it; the existing direct-LDR fallback remains coherent. VFX reads scene depth without writing depth, does not enter the shadow pass, and Renderer2D remains post-processing exempt for crisp diagnostics and HUD text.

| File | Dimensions | Bytes | SHA-256 |
| --- | ---: | ---: | --- |
| `m6-1-01-native-vfx-lab.jpg` | 962 x 572 | 72,590 | `A1636EBFC417D6795879F1469C73252390F941B600D99CD575B189769644F560` |
| `m6-1-02-web-vfx-lab.jpg` | 1280 x 720 | 73,843 | `904E5688775E355896914769154234CCCA5E762E159197A2AF947B4FE408974B` |
| `m6-1-03-sword-ribbon.jpg` | 1280 x 720 | 71,308 | `7F7A31D66DF670950476F51ACF62A641DB3ADD2630D11424278F78444FFF68C7` |
| `m6-1-04-particle-stress.jpg` | 1280 x 720 | 72,427 | `FA371466F9DF469E47A6975B00B53F0ACD030E653E549F9F7C51B93A58FA4202` |
| `m6-1-mobile-contact-sheet.jpg` | 960 x 632 | 112,046 | `A44BDC07BF0267C24F3563634B964825E00B11A45E5CD3233FA826BE1B71FC02` |
