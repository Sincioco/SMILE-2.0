# M7D-B Paladin Production Acceptance and v5.5 Candidate Report

## September 3 v5.5 follow-up

The user supplied a new animation-free 2K Arin T-pose and a separate 2K Paladin
equipment model, and authorized a fresh Mixamo retarget. This follow-up preserves
those exact inputs, builds a deterministic v5.5 technical candidate, makes v5.5 the
default Character Viewer and Combat Lab asset, and retains v5.4 and Classic as
fallbacks. It does not promote Arin to production or begin M8.

- Branch: `main`.
- Starting commit: `938a50983574c0b7577e95fbc431a6b5307bff38`.
- Stable identity: `sin-star-i.character-1.paladin`.
- Candidate: v5.5.
- Production state: `Prototype`; review state: `Candidate`.
- Release enabled: No.
- Classic fallback: Yes.
- M8: blocked and not started.

### Preserved sources

| Input | Repository path | SHA-256 |
|---|---|---|
| Arin 2K T-pose | `games/SinStarI/SourceAssets/Characters/Paladin/arin-t-pose-2k.original.glb` | `E6CC71A93738B350DEED3CB677EF41DDF88593E227B0759065CD35B6BB322885` |
| Paladin 2K equipment | `games/SinStarI/SourceAssets/Characters/Paladin/paladin-equipment-2k.original.glb` | `9AD461C44E2C2EF173878EA223BE225EA67CEBC5E99B1201447321D45F753148` |

The body source has 30 mesh parts, 6,631 vertices, 9,974 triangles, one
41-joint skin, no animation, and three embedded 2,048-by-2,048 JPEG images. The
equipment source supplies the sword, shield, and dedicated closed sword-grip glove
from the same 2K equipment atlas. The body and all 11 With Skin animation sources
match the accepted Arin rest skeleton within `0.0000062491`, below the manifest
limit of `0.0001`.

The repository-owned build is defined by
`scripts/build-arin-v5-5-candidate.manifest.json` and
`scripts/build-arin-v5-5-candidate.py`. The current published Blend is
`games/SinStarI/SourceAssets/Characters/Paladin/arin-integrated-candidate-v5.5.blend`
with SHA-256 `73B8D8626B432EF80BEDB9A7C1CADFE9E72B7E81267DB5009674CF9843367DA5`.
Blender's compressed Blend bytes can vary between clean saves, so the determinism
gate compares independently rebuilt exported GLBs rather than claiming byte-stable
Blend containers.

### Fresh Mixamo With Skin set

The exact FBX sources live under
`games/SinStarI/SourceAssets/Characters/Paladin/MixamoV55`. Every source was
downloaded With Skin from the freshly auto-rigged 2K Arin upload and is pinned by
frame count and SHA-256 in the build manifest.

| Runtime clip | Mixamo selection | Frames | SHA-256 |
|---|---|---:|---|
| Idle | Sword And Shield Sword Play Idle | 227 | `50C8E7DA7D1147C36C1D5C863EB2566A497C891026A08BF61F4EA344A9962A0B` |
| Walk | Sword And Shield Walk | 34 | `203A28876C44F86534E207F73F63496D13D9A44315B18FF237BACCC8F1C06B87` |
| Run | Sword And Shield Run | 22 | `507310C07B9238ABD122E53EAB5DD80838D7324010465B63F99DAD5A2FE47823` |
| Ready | Sword And Shield Idle To Block | 21 | `C5A528A5D2C4C2D4EB2AA9B274B28879206E23313AFEC10E795E16BEEE85B8F5` |
| SwordAttack | Sword And Shield Power Slash | 74 | `543F0E57002C29A7703034AE080F8178002F4FB3CAC390B07E93314AE56F6756` |
| ShieldBashCandidate | Sword And Shield Hilt Melee | 31 | `D3626C00DC1A4489420F47EFFA604260DE0E314D6344FE1F9A8CE1A71179FDD0` |
| Defend | Sword And Shield Block Idle | 43 | `E8F9AAB222420479A9064AC2632F2E50B32A8FB773395F63CFFD287CB3BD64C9` |
| BlockImpact | Sword And Shield Blocked Impact | 24 | `E95A842ACD212903910AC9B221C0275055AF54E38FA96CE9746491DE40F25DF3` |
| Hit | Sword And Shield Unblocked Impact | 30 | `038301C04A304D217E7935D8AD4CE64E73412EE77C9C572EF95DDA59F7883FC8` |
| KO | Sword And Shield Falling Back Death | 70 | `E667359D760160AD3520CC73B03BFC69D615C77CC9D8AA44F48321C1EA9E885F` |
| Victory | Celebrating After A Win | 257 | `0E004E4A9026E3F946329153CDA16B0F0728DE9C1DC5E1591DC0D1DED7B4FCEE` |

### Export, hand correction, and cooked contract

The first v5.5 visual export exposed two issues that metadata-only validation could
not catch: the T-pose's open right hand faced backward in several weapon clips, and
applying the sword's local-X correction to the dedicated grip reversed the hand in
other clips. The final candidate therefore omits body part `tripo_part_3`, retains
the dedicated 534-vertex closed grip glove from the accepted v5.x integration,
applies the 180-degree local-X correction only to the sword, and preserves the
reviewed identity hand-space basis for the glove and shield. Corrections are
declarative and validated in
`scripts/export-arin-v5-5-viewer.manifest.json`; the shared exporter retains its
version-1 behavior for v5.4.

The result is not a per-animation prop offset. Sword and glove are rigidly weighted
to `R_Hand`, remain paired for every clip, use the reviewed v5.x hand-space basis,
and use the normal skeleton animation path. The shield remains rigidly weighted to
`L_Hand`. The clean export is
`games/Dragonfall/SourceAssets/Arin/arin-integrated-candidate-v5.5.glb`, SHA-256
`A6D2A7E4316FC8BF1F0E82AF1A4EF6F3139C5523D451C3E80465128149488E21`.
The committed cook is SHA-256
`37BB9F1540E8B87F577988A019B7FDFA56AF5A2A082B58999CAB2887200F6261`.

| Cooked property | v5.5 value |
|---|---:|
| Parts / vertices / triangles | 4 / 7,376 / 10,296 |
| Materials / texture references | 2 / 6 |
| Bones / nodes | 42 / 46 |
| Clips / events / sockets | 11 / 8 / 10 |
| Animation / static / total bytes | 403,532 / 480,100 / 883,632 |
| GLB bufferViews / accessors | 279 / 279 |

Both materials use 2K JPEG base-color, normal, and ORM sources. Arin body maps use
textures 01, 00, and 02 respectively; equipment maps use 04, 03, and 05. The
technical upgrade from 1K to 2K is real, but JPEG is still lossy and therefore does
not satisfy the lossless production-texture gate.

### Plan reconciliation and deviations

- v5.4 remains preserved and its deterministic exporter gate still passes.
- v5.5, not v5.4, is now the default technical candidate in the Viewer and Combat
  Lab projects.
- The supplied T-pose contained no animations, so all 11 motions were freshly
  retargeted rather than reusing its original three prototype clips.
- The visually incompatible open sword hand was replaced with the established
  dedicated grip attachment. This is a bounded asset-build correction, not a new
  SMILE animation or runtime feature.
- The alternate 227-frame Sword Play idle replaces the initial 77-frame idle because
  the shorter motion carried the sword behind Arin for most of the Viewer cycle.
- No compiler, native runtime, Web runtime, VSIX payload, PBR implementation, VFX
  system, or SMILE language syntax changed.

### Current acceptance status

The candidate passes deterministic rebuild/export/cook, native and Web compilation,
the 11-clip/8-event/10-socket gate, Character Viewer hardening, Renderer3D camera
hardening, retained v5.4 export hardening, and Paladin Combat Lab validation. Manual
native review covered every animation and a close sword-grip inspection. Exact final
commands and the full smoke result are recorded in the milestone commit body and
final task report.

Production acceptance remains blocked by the original mandatory evidence gates:
lossless 2K texture sources, complete creator/source-service and redistribution
rights provenance, final Shield Bash approval, final native/Web material and
deformation approval, and explicit release approval. M8 remains blocked.

## Original v5.4 result (retained history)

- Milestone: M7D-B — production Paladin acceptance.
- Attempted: September 3, 2026.
- Status: **Blocked by missing mandatory external evidence and user approval.**
- Branch: `main`.
- M7D-A commit: `6c13d2dfcf904fbc9298660bdc5a2730750fa2f6`, pushed and verified on `origin/main` before this gate began.
- Candidate: v5.4, stable asset ID `sin-star-i.character-1.paladin`.
- Production state: runtime project remains `Prototype`; review state remains `Candidate`.
- Release enabled: No.
- Classic fallback: Yes.
- M8: Not started.

No new candidate version was created, no asset was promoted, and no production acceptance commit was made.

## Scan scope and safety

The gate scanned:

1. the complete tracked repository by filename and relevant provenance/production content;
2. `C:\Users\louie\Downloads` recursively for ZIP, GLB, GLTF, FBX, Blend, image, Markdown, text, and PDF candidates;
3. `C:\Users\louie\OneDrive\Downloads`, which does not exist on this machine.

Downloads ZIPs were sorted by `LastWriteTime` descending. Every relevant Paladin/M7 ZIP was opened read-only through .NET ZIP APIs. Each entry was checked for an absolute path, drive-qualified path, UNC path, parent `..` segment, and extraction escape. All relevant ZIPs had zero unsafe entries and contained Markdown only. Nothing from a downloaded ZIP was executed.

The newest relevant ZIP was:

- `C:\Users\louie\Downloads\2026-09-02-04-smile-2.0-m7d-paladin-combat-presentation-and-production-acceptance.zip`
- 22,610 bytes
- SHA-256 `F2DE8DE409EF72FAA5DB7F4CE9569074E14D6A3EF761B1EF1DF94196D9633261`
- 10 entries, all `.md`, zero unsafe paths
- Purpose: M7D instructions and gates; it contains no production art or rights package.

The earlier M7C, M7B.1, and M7 handoff ZIPs are likewise Markdown-only instruction packages. No ZIP with a newer production Paladin source, textures, or provenance identity was found.

## Relevant material found

### Repository candidate

- Canonical Blender source SHA-256: `CD58B33AC94E7B3CFEEDB9A85B2603B49DB4935FE8D2590DE5B50BE371C4A35C`.
- Deterministic GLB SHA-256: `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3`.
- M7D-A cooked SM3D SHA-256: `B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394`.
- Geometry: 10,296 triangles and 42 bones. These are acceptable candidate counts and are not blockers by themselves.
- Technical contract: 11 clips, 8 presentation events, and 10 exact-name sockets pass the M7D-A native/Web gate.
- Shield Bash state: `ShieldBashCandidate`, retained for revision rather than production acceptance.

The current nine source images are JPEG, 1,024 by 1,024, and lossy:

| Source | SHA-256 |
|---|---|
| `texture-00.jpg` | `8DD49DE64C691DC5D286D9745F5BA1EB1C8FB3EAD186194D1D2E02BEB8FFA944` |
| `texture-01.jpg` | `CCD55F34588C8959E1F135C8F22BAEE87EA34C875A0DDE1CA85CC071316B67F4` |
| `texture-02.jpg` | `49C70F0D00F7A13C56BE2B2EC1954052F587664E3FCFD5437074FB744C377EA5` |
| `texture-03.jpg` | `7C739452638B14EC10DC81A28049B8ECB355AB075302F35F2E7CD8907B4B6CD3` |
| `texture-04.jpg` | `2CF18664E756727DB2C86D4D361A7BB1E6878AF4C8A1D3B445B1708EE438FDBC` |
| `texture-05.jpg` | `0E21C722F77209733F1B54274332C2693E4D9594B37AD206DAEF269E79E596D4` |
| `texture-06.jpg` | `E5EEAD8EC5D6A5A76691BD44D4233A605AD5550BEDFD2EEB3F05D21165E9106C` |
| `texture-07.jpg` | `E57234F219640FB4521A4BE6F4F3FF5E8ABEE4434E05C50A4BC00B6C7933143F` |
| `texture-08.jpg` | `73D6D44FAF5D31B1AFF9540E87F21C60EDC16AF37B32A42649CF4D285B2EB59D` |

PNG runtime publication cannot restore source detail or remove JPEG artifacts, so these do not pass the 2K lossless production gate.

### Downloads candidates

- `paladin-mixamo-upload.fbx`: 1,357,324 bytes, SHA-256 `7D4C3830C8E9816F089751F76E64583B1204BCA9A38E12068B6A8DC79717B8BB`. It has a valid Kaydara binary FBX signature, but no companion production textures, provenance, rights statement, revised Shield Bash identification, or acceptance record. It is not a complete production package and was not imported or executed.
- `Sin Star - Character 1 - Paladin - Purple Background.png`: 1,672 by 941, 1,976,978 bytes, SHA-256 `A01CAD5CF81C3116C06BDCF3764EB9C76110427E5B2068C6D6644DF13C494C3F`. It is a visual reference, not a source model, texture package, provenance record, or production approval.
- `ChatGPT Image Sep 2, 2026, 11_57_40 PM.png`: 1,536 by 1,024, SHA-256 `BF2B6D3EB28C51FE60C6E56FE4F51C8C14D16FF4C77FD7AF8500DC97F85690D2`. Visual inspection shows a dragon concept sheet, not the Paladin production package, so it was excluded.

No GLB, GLTF, Blend, 2K texture set, or Paladin provenance/license document remains in Downloads.

## Mandatory gate matrix

| Gate | Evidence found | Result |
|---|---|---|
| 2K lossless base color, tangent normal, and ORM | Only nine 1K lossy JPEG sources | Fail |
| Per-material texture codec/dimension/hash/semantic record | Candidate hashes and 1K dimensions exist, but no lossless production sources | Fail |
| Creator/operator | Not supplied | Fail |
| Tripo/source-service project or export ID | Not supplied | Fail |
| Account/tier or applicable rights | Not supplied | Fail |
| Export date | Not supplied | Fail |
| Reference-image ownership | Not supplied | Fail |
| AI-generation disclosure | Tripo is named, but no project-specific disclosure/evidence package exists | Incomplete |
| Third-party inputs | Not supplied | Fail |
| Modification rights | Not supplied | Fail |
| Runtime redistribution rights | Not supplied | Fail |
| Raw GLB/Blend redistribution rights | Not supplied | Fail |
| Public repository distribution permission | Not supplied | Fail |
| Attribution requirements | Not supplied | Fail |
| Revised or explicitly accepted Shield Bash | Current motion remains `ShieldBashCandidate`; no newer source or approval found | Fail |
| Final socket corrections/attachment acceptance | Ten-socket technical contract passes, but no newer authored correction package or final production acceptance exists | Pending |
| Final native/Web deformation acceptance | M7D-A technical evidence exists; explicit final production acceptance is absent | Pending |
| Explicit user production approval | Not supplied | Fail |

No missing legal fact was inferred, and this report does not make a legal guarantee.

## Technical validation retained

M7D-A immediately preceding this scan passed the full repository smoke suite and all focused metadata, animation/event/socket, Combat Lab, Viewer/export, Renderer3D, Battle3D, Dragonfall, and Simple3D gates. No asset or runtime code changed during M7D-B, so those exact pushed results remain the applicable technical baseline. The 14 true-PNG native/Web evidence files and machine-readable review remain under `docs\implementation\screenshots\m7d-paladin-combat-presentation` and `docs\implementation\paladin-combat-presentation-m7d-a.review.json`.

## User approval checklist

- Visual design approved: Pending.
- Shield Bash approved: Pending.
- Deformation approved: Pending.
- Materials approved: Pending.
- Native result approved: Pending.
- Web result approved: Pending.
- Rights evidence accepted: Pending.
- Release enablement approved: Pending.

Technical success does not set any of these fields automatically.

## Required next input

To resume M7D-B, provide one versioned production package containing:

1. 2K lossless base-color, tangent-space normal, and ORM sources, with a material/semantic map;
2. the complete creator, source-service project/export, rights, redistribution, public-repository, and attribution record;
3. a revised Shield Bash source or explicit acceptance of the current candidate after review;
4. any final socket/attachment corrections and their authored source;
5. native/Web deformation and material approval; and
6. explicit approval to enable release after every earlier gate passes.

Until then, Arin v5.4 remains Candidate, `ProductionState` remains `Prototype`, `releaseEnabled` remains false, Classic remains available, and M8 remains blocked.
