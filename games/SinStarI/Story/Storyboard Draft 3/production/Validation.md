# Draft 3 — Validation

## Delivered behavior

- 121 scene illustrations with matching LTX 2.5 previews in both views.
- The supplied poster directly below the Script title/version and seven new
  animated cast portraits directly below their character headings.
- One shared header, Script/Storyboard tabs, 53-scene sidebar, scene search,
  reading-position links, Print control, persistent light/dark themes and Audio
  On/Off control. Header clearance follows its actual height when controls wrap.
- A new eight-second final-battle preview in Chapter 9, Scene 3: Aevos releases
  a planet, it strikes the party's blue barrier, and the shockwave forces the
  companions down. The accompanying figure has new dialogue beneath it.
- A second eight-second planet-throw take corrects Mira's head/body alignment.
  The original remains Clip 1; the revised take is the default Clip 2. Clip choices
  and a Remember checkbox appear below the picture in both views. Remembered
  choices and audio preferences persist and follow local view links.
- Existing script prose, Canon, completed films and Unlisted links retained.
  Drafts 1 and 2 remain byte-for-byte unchanged.

## Checks performed

- `site-validation.json`: three HTML pages; all local file/anchor references;
  exact image/video assignments; poster/cast/boss-scene placement; identical
  navigation; unchanged original narrative; 310 preserved earlier-draft files;
  all PNG files decoded; all four site JavaScript files syntax-checked.
- `media-validation.json`: every preview fully decoded with FFmpeg, checked for
  H.264/YUV420p video at 24 fps and 960-pixel width, one AAC audio track per clip,
  non-silent audio peaks, and SHA-256 recorded. All 130 clips passed.
  Ordinary previews run about four seconds; selected reused excerpts vary.
  The planet-throw preview runs about eight seconds.
- `behavior-validation.json`: 23 isolated playback/variation checks, 18 theme,
  navigation and search checks, and 10 shared media-preference checks, run with the existing Node runtime and fake
  browser objects. These checks cover lazy loading, one active video, stale
  playback cancellation, keyboard/touch activation, reduced motion, file-storage
  restrictions, current-scene transfer and cross-view theme links. Added checks
  cover sound autoplay rejection, explicit sound activation, immediate muting,
  variation selection, remembered/cleared choices, cross-view URL propagation,
  reloads, bookmarks and live storage updates.
- `theme-contrast.json`: 24 text/background token pairs across light and dark
  palettes; every checked pair exceeds 4.5:1 (lowest 5.46:1).
- Sampled video-frame review for every preview. Targeted multi-frame review
  verified the planet windup, release, impact and aftermath at one-second
  intervals; it also checked corrected quiet-scene excerpts and cast portraits.
  Additional close-frame review checked Mira in the revised take during and after
  impact. The original Clip 1's decoded video hash matches the preceding delivery;
  audio was restored without changing its picture sequence.
- ComfyUI's existing Chrome tab showed the running queue. Saved API/UI workflows
  and prompt receipts identify the jobs; earlier queue entries were preserved.

## Corrections from review

- Fixed Script image wrappers that initially missed linked captions.
- Allowed the packaged styles, scripts and video through the Script's existing
  content security policy.
- Used clean edited movie framing to remove generated lettering where needed;
  retained reviewed full-frame excerpts when cropping would obscure the party.
- Trimmed the Orin/father preview before an unwanted embrace.
- Rendered a second take of the quiet Arin/Mira scene, then kept its first 2.5
  seconds to exclude a late sword lift. Both original takes remain archived.
- Corrected an extra dog head in the new planet illustration before rendering.
- Restored audio stripped by the first hover export. A volume check caught a
  silent edited-film intermediate; its original LTX soundtrack was recovered.
- Kept the original planet throw for comparison and rendered the requested
  revised take after the reported Mira anatomy defect.

## Scope and limits

This is a static portable website. No .NET build, VSIX installation, server,
new package or application restart is required. Content hashes version assets;
press Ctrl+F5 in Chrome when replacing an already-open edition.

Website browser interaction and visual layout testing were not performed.
The evidence above is static validation, isolated behavior checks and direct
media review; it does not establish browser/device coverage. Generated animation
can vary subtly from the still artwork. The new action sequence is a website
preview; the three previously completed movie edits and YouTube uploads remain
the credited Draft 2 versions.

## Ownership and change size

Preview behavior and theme/navigation have separate small JavaScript owners.
Media preferences have one shared owner used by both views; clip metadata stays
in the existing media inventory, expanded by the small `media_catalog.py` helper.
Shared shell styling is reused by the two reading views and the film gallery.
The HTML files are generated artifacts; production modules rebuild from preserved
Draft 2 inputs. No compiler, runtime, game implementation or architecture contracts
were changed. No architecture-rule exceptions or external dependencies were added.
