# Original thermal-fire assets

Original procedural artwork authored for SMILE 2.0 by Sin and Codex, 2026-09-04.
No downloaded artwork, proprietary effects, network resources, or third-party image input.
The repository license applies.

Regenerate with `scripts\generate-thermal-fire-assets.ps1`; verify without modifying
files with `-Check`. Equations, fixed phase offsets, palette stops, and PNG encoding
are in that script. There is no random seed, clock, user path, or machine metadata in
the images. Both Windows PowerShell and PowerShell 7 are supported.

| Asset | Size | Meaning | SHA-256 |
| --- | --- | --- | --- |
| fire-shape-atlas.png | 1024 x 1024; 413,743 bytes | 4 x 4 irregular flame tongues with warped multiscale wisps | 84c9a9e4f47e43aad9b2d403262f4ad17fe9fa3982d78b380ebcab9818799bd7 |
| smoke-shape-atlas.png | 1024 x 1024; 328,232 bytes | 4 x 4 soft smoke shapes with billowing detail | cd8c8547ee24ce66371b727b0d80a982f68f03632a4061e9a320fa820be28d48 |
| ember-shape.png | 64 x 64; 1,425 bytes | soft radial spark | 8631834068bcc031eeca3d556b95f932ddfb6703c1b14faf1abd70f50e66dd0f |
| thermal-gradient-lut.png | 256 x 4; 938 bytes | red/orange/gold/white-hot palette | 8d83c97e1f2cf84452f8dfb1286c944d4fd29ae8e9a1096c3eb235f42e08e93c |

Atlases use straight white RGB with alpha coverage and three transparent texels at
each cell border. Clamp/linear sampling and fixed per-particle cells avoid texture
bleed and flickering atlas animation. The LUT is an inspectable palette reference;
the native shader evaluates its five identical sRGB palette stops analytically,
then the existing VFX pixel shader converts to linear HDR before bloom. It does not
sample a second texture or double-apply gamma. CPU Low uses a basic warm fade.
