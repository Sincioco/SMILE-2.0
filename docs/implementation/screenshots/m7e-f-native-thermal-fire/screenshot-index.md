# Native thermal fire — screenshot index

All inputs are actual Windows application captures. The capture service supplied JPEG pixels; these were transcoded to genuine PNG without retouching or synthetic scene content. The two contact sheets use fitted copies of those captures. No video or same-frame A/B equivalence is claimed. Captures span the incremental native builds; earlier images predate the uppercase header and latest controls.

## 01-thermal-fire-core-and-outer.png

Native hot core and outer flame in the updated arena.

- Dimensions: 1920 × 1032
- Bytes: 1944884
- SHA-256: `952fb16efda987bb05a0d9e753d4657e27a87bcb549af2ecdc45311ed87959dd`

## 02-turbulence-off-on.png

Two real captures composed side by side; not identical simulation instants.

- Dimensions: 1280 × 390
- Bytes: 345991
- SHA-256: `0f709d0262b97f76491d0063efcb21f5888d3608d33686f873cc73bc38766c49`

## 02a-turbulence-off.png

Turbulence disabled; fixed camera.

- Dimensions: 1920 × 1032
- Bytes: 1874404
- SHA-256: `490106cdfaa7f917b5a425923a1bbddd0a8253590b8eafd9ac96da801b904271`

## 02b-turbulence-on.png

Turbulence enabled; fixed camera.

- Dimensions: 1920 × 1032
- Bytes: 1877962
- SHA-256: `c0f962492033c10aaba85b6d287665613d448e1c0d2fa1bea2975faf5b162ece`

## 03-wind-blown-torch.png

Windy Torch preset; native GPU selected.

- Dimensions: 1920 × 1032
- Bytes: 1939527
- SHA-256: `b176c0e3c23aac993921d472cb80b61da2dba3e0403f59ce7fab4f2e9c204b4b`

## 04-smoke-soft-depth.png

Brazier with reduced smoke and soft depth enabled.

- Dimensions: 1920 × 1032
- Bytes: 2168138
- SHA-256: `ea20bef0d7511f91a924ba84672f9d66610cc16782243515bd31f8ecdc9a20a6`

## 05-heat-distortion.png

Same native capture as 02b: heat enabled, four scene AA samples. A still image cannot prove motion or quantify refraction.

- Dimensions: 1920 × 1032
- Bytes: 1877962
- SHA-256: `c0f962492033c10aaba85b6d287665613d448e1c0d2fa1bea2975faf5b162ece`

## 06-hdr-bloom.png

Native HDR/bloom presentation.

- Dimensions: 1282 × 752
- Bytes: 1115454
- SHA-256: `23c6a370edbc2fa76275e0be77b61425a980e0f6107fb0b79bef6e1799c1766e`

## 07-direct-ldr-fallback.png

Native direct-LDR presentation; small window has crowded controls.

- Dimensions: 915 × 541
- Bytes: 598241
- SHA-256: `12ea7c8fd8697af9fee4527a4cefac2801b6dbed2366228928a7cee3bc0bdbeb`

## 08-moving-line-emitter.png

Fire along a moving world-space segment; captured during live playback.

- Dimensions: 1920 × 1032
- Bytes: 2197301
- SHA-256: `82fd6e69920d23be3d9685d9cae4317b657ef3eed7ca51c9fa88b3cafb5f4544`

## 09-cpu-gpu-comparison.png

CPU Low on the left and native GPU on the right; intentionally different quality.

- Dimensions: 1920 × 1032
- Bytes: 2047617
- SHA-256: `478190a3858d2ad38fc5712df9f9fb9b562e4e8ccf30993f1855641ff4543694`

## 10-iphone-contact-sheet.png

Contact sheet composed only from captures 01, 03–09.

- Dimensions: 1280 × 1560
- Bytes: 2056242
- SHA-256: `1177932f418c40ae4cc915ba000ba733d5ccfb02c7e7988c29f00c80d7346ae1`

## 11-viewer-panels-hidden.png

Viewer first backtick tap: panels hidden, header/timeline/helpers retained.

- Dimensions: 1426 × 678
- Bytes: 1054548
- SHA-256: `b8a8a380ab6f948ee13d39c38d98db8a9b5affa96bec7f49d2b1d22f5029d9c8`

## 12-viewer-all-ui-hidden.png

Viewer second tap: all UI hidden.

- Dimensions: 1426 × 678
- Bytes: 778400
- SHA-256: `029715dddd9fa700a45d3b1919e29768c9f0cecfbebff35298f0b9a5972df975`

## 13-viewer-ui-restored.png

Viewer third tap: UI restored. Startup-background fix was compiled after these Viewer captures.

- Dimensions: 1426 × 678
- Bytes: 1181439
- SHA-256: `68b4b9138477d2e7e2225992a085fff2fbbeb5364d49c4aceb440f39379f3a57`

## 14-fire-lab-panels-hidden.png

Fire Lab first tap: panels hidden; uppercase header and helpers retained.

- Dimensions: 1425 × 379
- Bytes: 835991
- SHA-256: `9d8e463cb477d847854df39982d90d875a3b754084407a3e87048444925df525`

## 15-fire-lab-all-ui-hidden.png

Fire Lab second tap: all UI hidden.

- Dimensions: 1425 × 379
- Bytes: 720021
- SHA-256: `9cd656855c7471d24c5e86ee045871acb0e32294e4b52bd662d607bcfd01c93d`

## Runtime evidence

`native-validation.txt` preserves the successful native dynamics, GPU/recovery/performance and high-level native/Web-fallback run. Performance entries are CPU submit+present observations, not GPU timers. Artistic acceptance, motion continuity, and the remaining editor controls remain manual review items.
