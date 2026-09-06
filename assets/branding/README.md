# SMILE 2.0 branding

`smile-2.0-logo.png` is the current official SMILE 2.0 logo, designated by
Louiery R. Sincioco (Sin) on September 6, 2026. Use this canonical source for new
SMILE branding and Web loading screens.

It is an unchanged copy of `tutorials/Snake/assets/images/smile-2-logo.png`.
The tutorial copy remains in place for compatibility. SHA-256:
`43D695C36FAB50849ADD26330E2D857F18C60BFBA91AF0EAA0D02127E0009AC9`.

Generated deployment copies are disposable. Do not resize or overwrite this
canonical PNG when creating an optimized Web publication.

`smile-2.0-logo-web.png` is the authorized loading-screen derivative: 768 by 512,
RGBA PNG, 427,320 bytes (about 75.5% smaller). The canonical original is 1536 by
1024 and 1,747,311 bytes. Only the logo is resized; no character texture changed.
Derivative SHA-256:
`494F2B7A8476EA58702DEF84105ADEB52D77BCE3AA209A85A5507ECF5371A374`.

Regeneration command (FFmpeg, from repository root; no overwrite by default):

```powershell
ffmpeg.exe -hide_banner -loglevel error -n -i assets/branding/smile-2.0-logo.png -vf "scale=768:768:force_original_aspect_ratio=decrease:flags=lanczos" -frames:v 1 -update 1 -compression_level 9 assets/branding/smile-2.0-logo-web.png
```
