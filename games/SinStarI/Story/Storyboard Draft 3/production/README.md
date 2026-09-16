# Draft 3 Production

The delivered HTML works directly from disk without production tools, a server,
external fonts, package downloads or an internet connection.

## Ownership

- `build_draft3.py` adds poster, cast figures and preview wrappers to the preserved
  Draft 2 HTML in `source-pages`. It leaves narrative text untouched.
- `site_shell.py` owns the shared header and 53-scene sidebar. It replaces the two
  old navigation shells and permits packaged styles, scripts and videos in the
  Full Script's content security policy.
- `asset/site-theme.js` applies the theme before painting. `asset/site-shell.js`
  owns the theme preference, scene search, current-scene links and print action.
  `asset/site-shell.css` owns the shared shell and both palettes.
- `asset/animated-pictures.js` owns the single active preview and its lifecycle.
  It cancels stale play requests, unloads departed clips, pauses hidden/offscreen
  media, respects reduced motion and supports touch/keyboard activation.
  Its companion stylesheet owns video overlays and controls.
- `asset/media-preferences.js` owns shared audio and remembered clip choices.
  It persists them in local storage and carries them in local view URLs when
  file storage is restricted. Clip controls use the same picture IDs in both views.
- `hover-media.json` maps all 129 pictures to preview, source, provenance and edits.
  Optional `variants` add clips; `default_clip` selects the starting take when no
  preference is saved. `media_catalog.py` expands this inventory for queueing,
  collection, HTML generation and validation. There are currently 130 clips.
  `cast-portraits.json` records the seven new portraits.
- `queue_hover.py` appends missing LTX 2.5 jobs to the existing local queue.
  `collect_hover.py` collects jobs and exports H.264/AAC previews with original audio.
  `validate_media.py` fully decodes new or changed clips once.
- `validate_draft3.py` validates links, exact image/video pairings, shared navigation,
  script text, cast/poster placement, security policy, JavaScript syntax and hashes
  of both earlier drafts. `html_document.py` supplies its small HTML inventory.

## Media decisions

The edition reuses 45 existing LTX renders: the 44-shot film selection, with its
reviewed second takes, and the armorer shot from Room for One. Existing edited
movie framing removes generated lettering where needed. The lower authored caption
band is cropped out for hover use. Seven reviewed clips retain full framing
to protect foreground characters; two of these use shorter clean excerpts.
Wider previews are contained within the original illustration area without stretching
or clipping additional faces. Each source and edit is recorded in the media map.

Another 84 pictures have new animation: 76 story illustrations, the supplied
poster and seven new action portraits. Ordinary clips use 97 frames at 24 fps with
restrained image-conditioned motion. The additional Aevos planet-throw illustration
in C09-S03 has a dedicated eight-second, 193-frame action sequence. Its new caption
and dialogue accompany the figure; all previously accepted script prose is preserved.
Saved API/UI workflow pairs record LTX 2.5
distilled 22B INT8, existing local encoders/VAEs/upscaler, fixed seeds, and full
initial-image conditioning. No models or packages were installed.

The planet-throw scene also has a revised take that keeps Mira's head and body
aligned through the impact. Clip 1 retains the original picture sequence; Clip 2
is the new default. Both use the same illustration and remain selectable.

All 130 previews now include audio. The carry-together scene uses the original
LTX audio because its edited-film intermediate was silent; `audio_source` records
that correction. Other clips retain their selected source soundtracks. First-visit
audio is off; one header click enables it. A rejected sound autoplay request falls
back to muted video with an explicit Play With Sound action. See the official
[Chrome autoplay policy](https://developer.chrome.com/blog/autoplay/) for the
browser interaction requirement. Muting stops sound immediately without reloading.

The original poster title remains visible over the top portion of its animation,
so its lettering stays exact. Static artwork remains underneath each preview,
including while media loads or if playback is unavailable.

The Orin/father hover excerpt ends before an unwanted generated embrace. The first
2.5 seconds of `new-c01-s04` take 2 keep Arin's sword lowered during the quiet conversation.
First takes and original movie files remain untouched.

## Rebuild and validate

With the existing Python/Pillow installation, run:

```text
python production/queue_hover.py
python production/collect_hover.py
python production/build_draft3.py
python production/validate_draft3.py
python production/validate_media.py
```

Queueing is needed only for missing renders. Receipts prevent duplicate submissions.
Use `collect_hover.py --refresh-all` to re-export existing clips after export changes.
Collection reads `http://127.0.0.1:8191` and retains original renders in the separate
`Sin Star I - Draft 3 Animations` output folder. Saved workflows also open in ComfyUI.

Use the general bundled Python runtime for production scripts. The isolated
ComfyUI interpreter is not required to view or rebuild the pages. Existing FFmpeg
and Node installations are used for media export/validation and syntax checks.
No application build is required.

Sibling Drafts 1 and 2 are needed only for preservation validation. HTML rebuilds
use this edition's included baseline `source-pages`; those files are production
inputs, not additional website entrypoints.

## Validation scope

See `site-validation.json`, `media-validation.json`, `render-status.json` and
`Validation.md` for final evidence. Isolated behavior checks exercise playback,
reduced motion, theme transfer, navigation and search; they are not a browser
visual test. The ComfyUI queue was checked in the user's existing Chrome tab.
