# Phase 4.2 unified asset publication

Baseline: `acbd5bdc0441e5384be46d98d4640e808585b3f8` on `main`.

## Recorded pre-fix evidence

- Direct `smilec --project examples\Phase4VisualSlice\Phase4VisualSlice.smileproj --target web` completed successfully but emitted only `game.js`, `index.html`, `smile-runtime.js`, and `smile.css`; all seven declared assets were absent.
- Visual Studio owned a separate recursive `CopyAssets` implementation after invoking the compiler.
- The old `ExpandAssetPaths` implementation enumerated every file beneath the prefix before the first wildcard without applying the requested pattern, so `Assets\UI\*.png` could admit non-PNG and nested files.
- A missing explicit asset passed through an `if (File.Exists(...))` branch and was silently omitted.
- There was no publication ownership manifest or stale-removal path, so removing an include left its prior output copy behind.

## Shared result

`SmileProjectAssetResolver` now parses every project asset include with XML line information and produces one deterministic `SmileProjectAssetManifest`. The compiler's embedded native/Web runtime paths, publisher, Visual Studio hierarchy, build validation, and narrow asset watchers consume that same manifest. `*`, `?`, and complete-segment `**` follow portable ordinal case-sensitive semantics; overlaps deduplicate and empty wildcard matches remain valid.

Stable diagnostics cover invalid patterns (`SML3600`), missing explicit files (`SML3601`), case mismatches (`SML3602`), portable destination collisions (`SML3603`), publication failures (`SML3604`), unsafe prior manifests (`SML3605` warning), and unsupported library-owned assets (`SML3606`). User-correctable validation errors retain compiler exit code 1 and project XML locations.

`SmileProjectAssetPublisher` publishes validated items after successful native linking or Web generation. It preserves logical paths and bytes, skips unchanged size/time pairs, copies through a destination-local temporary file, and atomically replaces the destination. Native writes `<executable-base>.smile-assets.json`; Web writes `smile-assets.json`. Only validated paths from the matching prior identity/target manifest may be removed as stale. Unsafe prior metadata is ignored without deletion, reports `SML3605`, and is replaced only after current publication succeeds.

Visual Studio no longer copies assets itself. Its native and Web builds use `smilec --project`, and Solution Explorer displays only resolved declared files plus the preserved empty game `Assets` folder. Watchers use the smallest fixed include root and recurse only when the pattern requires it, preserving immediate source/reference refresh behavior.

The existing Phase 4 visual-slice smoke build and all ten normal game builds now use project publication without `xcopy`. Loose-file no-demo builds keep their existing behavior because loose compilation intentionally has no project asset manifest.

## Focused proof

- `examples\Phase4AssetPublication` resolves and publishes exactly five assets from explicit, nonrecursive, recursive, empty, and overlapping includes.
- Native and Web output paths, SHA-256 values, publication manifests, and embedded runtime lists match exactly; undeclared files remain excluded.
- `examples\InvalidPhase4Assets` proves project-located missing-explicit and library-asset failures.
- Shared tests cover single-character `?`, exact case, unsupported/rooted/traversal patterns, synthetic case-only collision, overlap deduplication, hierarchy projection, safe stale removal, unrelated output preservation, and corrupt-manifest containment.
- `scripts\test-phase4-asset-publication.ps1` exercises direct native/Web CLI publication, embedded lists, exact hashes, exit codes, native/Web stale cleanup, and unsafe prior-manifest recovery.

## Recorded recommendations

The existing callback-mark/main-thread-reap SFX design now invokes its localized reaper from the normal Windows message/frame pump; callback threads still never destroy voices and generation safety is unchanged.

Before Phase 6, consider an explicit portable `ApplicationId` independent of `OutputName` for persistence isolation. Before reusable libraries own skins, fonts, sounds, or themes, choose explicitly between consumer-supplied resources and versioned target-neutral package resources. Neither future design is implemented in Phase 4.2.

## Live acceptance

- Visual Studio displayed exactly the five resolved fixture assets, refreshed immediately when a matching asset was added, ignored a nonmatching asset, removed a moved asset, and reported `SML3601` at the project XML location. Temporary source and project-reference additions appeared immediately and disappeared after cleanup.
- Visual Studio native and Web builds each reported `Published 5 project assets`; their output sets and SHA-256 values matched the sources and direct CLI outputs.
- Native debugging stopped at `CLEAR` on line 9 and F10 advanced to `DRAW IMAGE` on line 10.
- DirectX and GDI rendered the high-resolution visual slice with smooth scaling, alpha, clipping, painter order, animation, and opt-in pixel filtering.
- Chrome rendered the Web visual slice at a 200% device scale (`1820 x 1024` backing canvas for a `910 x 512` CSS canvas) with no console errors.
