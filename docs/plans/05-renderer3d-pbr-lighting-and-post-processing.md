# Renderer3D PBR, Lighting, and Post-Processing

## Goal

Make imported mid-poly characters look materially and spatially modern while retaining a bounded, understandable renderer.

## Staged delivery

Do not combine all rendering upgrades into one edit.

### M2 — PBR-lite direct rendering

Implement:

- tangent input;
- base-color map;
- normal map;
- packed ORM map;
- emissive map;
- texture color-space semantics;
- mipmaps;
- anisotropic filtering when supported;
- bounded ambient/directional/point/spot lights;
- a matched native/Web PBR-lite shader;
- old simple material compatibility.

### M5 — shadows and post-processing

Implement:

- one selected shadow-casting directional or spot light;
- a bounded shadow map;
- optional HDR scene target;
- tone mapping;
- bloom;
- quality profiles;
- capability fallbacks.

## Material model

Use a metallic-roughness PBR model with a compact Cook-Torrance/GGX-style direct-light implementation.

The shader should include:

- normalized tangent-space normal mapping;
- Schlick-style Fresnel;
- GGX or equivalent bounded normal-distribution term;
- Smith-style geometry attenuation or an equivalent matched approximation;
- Lambertian or energy-aware diffuse;
- metallic and roughness factors;
- ambient/hemisphere contribution;
- emissive contribution.

The exact math must be documented and implemented equivalently in HLSL and GLSL.

Do not pursue photorealistic subsurface skin, anisotropic hair, clearcoat, transmission, or material graphs in M2.

## Texture semantics

| Channel | Color space | Default |
|---|---|---|
| Base color | sRGB | white |
| Normal | linear | flat normal |
| ORM R = occlusion | linear | 1 |
| ORM G = roughness | linear | material factor |
| ORM B = metallic | linear | material factor |
| Emissive | sRGB | black |

### Native

Use appropriate sRGB shader-resource-view formats for color textures and linear formats for data textures. Generate mipmaps only through a path supported by the resource flags and format.

### Web

Use WebGL2 sRGB internal formats for color textures where available and ordinary linear formats for data textures. Reuse the current browser-decoded image assets. Call `generateMipmap` only for valid dimensions/formats and document fallback behavior.

### Filtering

Add public/internal filter modes without changing existing numeric values:

- nearest;
- linear;
- trilinear/mip-linear;
- anisotropic.

Clamp anisotropy to backend capability.

Old textures continue using their current behavior unless the caller or loaded v2 material requests the new filter.

## Tangent handling

Preferred order:

1. use valid glTF tangents;
2. otherwise generate tangents deterministically offline;
3. reject a normal-mapped material only if tangents cannot be made valid;
4. never derive full tangents per frame.

The converter must handle mirrored UV handedness correctly.

## Bounded light model

Suggested initial limits:

- one ambient or hemispherical light record;
- one primary directional light;
- up to four additional point or spot lights;
- one shadow caster selected from the supported lights.

A light record should include:

- type;
- enabled;
- color;
- intensity;
- position or direction;
- range;
- cone values for a spot light;
- shadow request flag.

The renderer may cull or select the most important fixed number of lights. It must do so deterministically.

## Low-level public API direction

Exact names may be adjusted to current conventions.

Illustrative operations:

```basic
Call Graphics3D.SetAmbientLight3D(28, 32, 46, 65)

Call Graphics3D.SetDirectionalLight3D(
    0,
    -30,
    -80,
    25,
    255,
    232,
    205,
    125,
    True
)

Call Graphics3D.SetPointLight3D(
    0,
    120,
    180,
    40,
    255,
    96,
    24,
    140,
    650
)
```

The high-level `Scene3D` module should normally configure these through named presets.

## PBR material API direction

Preserve current `Material3D`.

Either extend the record with new handles/factors in a source-compatible way or add a distinct `PbrMaterial3D` record. Choose the option that causes the least breakage after inspecting current user code.

Needed low-level operations:

- create PBR material;
- set base-color texture;
- set normal texture;
- set ORM texture;
- set emissive texture;
- set metallic;
- set roughness;
- set normal strength;
- set emissive strength/color;
- set alpha mode/cutoff;
- query validity and references;
- destroy only when no object/model depends on it.

Loaded SM3D v2 character assets should construct these automatically.

## Render ordering

At minimum:

1. shadow casters;
2. opaque PBR/simple geometry;
3. cutout geometry;
4. alpha-blended geometry;
5. additive geometry and VFX;
6. post-processing;
7. Renderer2D.

Avoid a general render graph. Use the smallest explicit pass structure needed.

## Shadow path

M5 should start with one high-value shadow:

- one directional or spot light;
- one 2048×2048 high-quality shadow map;
- 1024×1024 medium;
- disabled or 512×512 low;
- depth comparison with small PCF filtering;
- adjustable depth/slope bias;
- actor and boss casting;
- floor/arena receiving.

No point-light cube shadows in the first implementation.

Shadow allocation or capability failure must disable shadows without losing the 3D scene.

## HDR and tone mapping

### Native

Preferred scene format:

```text
R16G16B16A16_FLOAT
```

Use an LDR fallback if creation fails.

### Web

Use an HDR framebuffer only when the required floating-point color-attachment capability is available. Otherwise render through the LDR fallback.

### Tone mapping

Choose one small documented operator, such as an ACES-inspired fit or a simple filmic curve. Native and Web should use matched equations.

Expose bounded exposure through `Scene3D`, normally via presets.

## Bloom

Bloom should:

- use an emissive/brightness threshold;
- run at half or quarter resolution;
- use a small fixed separable blur chain;
- composite before tone mapping or according to the documented pipeline;
- have bounded render-target count;
- be disabled cleanly in low quality.

Do not bloom the final 2D HUD.

## Quality profiles

Required profiles:

| Profile | Textures | Lighting | Shadows | Post |
|---|---|---|---|---|
| `LOW` | prefer 1K | ambient + key | off or minimal | LDR, no bloom |
| `MEDIUM` | 1K/2K | full bounded lights | 1024 | tone map, reduced bloom |
| `HIGH` | 2K | full bounded lights | 2048 | HDR where supported, full bounded bloom |
| `AUTO` | capability-based | capability-based | capability-based | capability-based |

The selected effective profile and disabled features must be queryable.

Do not use GPU model-name blacklists. Select by tested capabilities and safe resource creation.

## Required diagnostics

Add counters or values only when reusable:

- current effective profile;
- current scene target format class;
- shadow enabled;
- bloom enabled;
- active light count;
- draw calls;
- triangles submitted;
- PBR material count;
- texture bytes estimate where feasible;
- post-processing pass count;
- last fallback reason.

## Required tests

### M2

- old simple material path unchanged;
- PBR material with no maps uses defaults;
- base-color sRGB handling;
- normal map visibly/semantically affects lighting;
- ORM channels affect roughness/metallic/occlusion;
- emissive works without becoming a dynamic light;
- mip/filter modes validate;
- invalid maps and stale handles fail safely;
- native/Web material state parity;
- reset returns counts to zero.

### M5

- shadow caster selection;
- shadow target creation failure fallback;
- low/medium/high profile settings;
- HDR capability fallback;
- bloom threshold and disabled path;
- 2D HUD remains unaffected;
- resize recreates all required targets;
- device/context loss paths release and rebuild resources;
- repeated begin/end/resize leaves no resource growth.

## Visual acceptance

At a fixed camera:

- metal trim should show stronger specular response than cloth;
- painted armor should be less metallic and have readable roughness;
- normal-map details should respond to moving light;
- emissive blue accents should remain visible and contribute to bloom in supported profiles;
- the actor should cast a stable readable shadow;
- the HUD should remain crisp;
- low quality should remain visually coherent rather than broken.

## Performance acceptance

For the vertical slice:

- no shader compilation during an attack;
- no texture load during an attack;
- no render-target creation inside the frame loop;
- no unbounded per-frame vector/list growth;
- one skinned hero should use a small number of draw submissions, not dozens of rigid-part submissions;
- diagnostics must permit actual draw-call and triangle-count reporting.
