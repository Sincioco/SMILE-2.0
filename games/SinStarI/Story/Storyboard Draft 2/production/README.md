# Draft 2 Production

The delivered HTML runs without any production tools. These files preserve the
editable source, generation prompts, workflow parameters and delivery evidence.

## Ownership

- `build_site.py` owns sheet extraction, script placement and storyboard generation.
  It always reads the preserved sibling Draft 1. `movie_page.py` builds the movie
  gallery from `movies.json`. Neither changes Draft 1.
- `new-illustrations.json` owns 48 added images, their exact script anchors, editable
  dialogue, original prompts and correction provenance. `image-overrides.json`
  records the corrected original queen panel. `crops.json` records all 72 crop boxes.
- `illustration-index.json` is the generated 120-panel inventory.
- `asset/storyboard.css` owns storyboard/movie layout. `asset/reading.js` owns the
  small responsive contents behavior. The inherited full script retains its reader
  controls and inline presentation. Generated HTML is intentionally a full static
  reading artifact; no application framework or build-time package installation.
- `film_shots.json` owns the 44-shot highlight selection, intended movement and
  dialogue. `queue_film.py` appends LTX jobs and records receipts. `revise_film.py`
  appends only the six focused second takes. Both retain completed receipts to avoid
  accidentally adding duplicates when rerun.
- `edit_movies.py` owns reusable cards, normalization and music mixing.
  `finish_movies.py` selects reviewed takes, typesets dialogue, and assembles the
  4:38 story film and 30-second trailer. No browser upload is automated by these files.

## Rebuild the website

Run `python production/build_site.py` with an existing Python installation that
has Pillow. On Sin's machine, use the bundled general Python runtime rather than
the isolated ComfyUI embedded interpreter. Then run `python production/validate_site.py`.
The latter also uses the existing bundled Node runtime for JavaScript syntax checks.
The preserved sibling Draft 1 is required only for rebuilding and preservation checks.

## Reproduce the movie shots

`workflows` contains 44 original ComfyUI UI/API pairs and six reviewed take-2 pairs.
The API JSON is the exact generation graph; saved UI seed/strength values were
aligned with it after the review. These workflows use the existing LTX 2.5 distilled
22B INT8 model, Gemma encoder, video/audio VAEs and spatial upscaler configured on
Sin's machine. They do not download models or install components.

The local service is `http://127.0.0.1:8191/`. Inputs are copied to
`D:\AI\Mira3D\ComfyUI\input\SinStarI_Draft2`. Original output and first takes remain
in `D:\Sin - AI Prompt - Contents\Sin Star I - Draft 2 Movies`.

The original requests occupied queue numbers 77–120; corrections 121–126. Existing
jobs were preserved and the queue was verified in Chrome. Receipts include prompt
IDs. Successful completion was confirmed through ComfyUI history, and the outputs
were collected before editing.

## Edit decisions

Each story shot is 145 frames at 24 fps. The film opens with the supplied poster
for two seconds and ends with ten seconds of credits. It uses Starforge March,
Bloom and Starforge Ascend with transitions and dialogue ducking.

The trailer opens on the Aevos confrontation, uses eleven two-second excerpts in
nonchronological order, and ends with eight seconds of credits. Captions are:
“Defy the end.”, “Every life matters.”, “Find your people.”, “Stand together.”,
“Choose your own fate.” and “Fight for this universe.” Its music is Starforge Ascend.

Observed generated lettering was removed by framing the clean upper image area.
Scene 42 uses the full image to preserve Milo and the stretcher. Authored dialogue
captions are placed independently in a dark lower band. Silent action scenes use
only the supplied score. Six takes were replaced to correct additional figures,
robot identity drift and rescue framing. No claim is made that generative voices
match a fixed professional cast, or that the clips are executable game footage.

All movies contain the exact creator credit, SMILE language/tool-chain credit,
open-source project URL and In Development wording. The story film website copy
is 1280×720 H.264/AAC, CRF 23 with a 2.3 Mbps video ceiling; its 76.8 MB file stays
below GitHub's single-file limit. Both short local movies retain the 1080p export.
The separate 1080p story master is 258.0 MB and was uploaded to YouTube.

## Evidence

See `Validation.md`, `site-validation.json`, `movie-validation.json`,
`youtube-uploads.json`, `queue-receipts.json` and `revision-receipts.json`.
The user-supplied character reference images remain in their original source
locations; the illustration manifest records those paths and selected outputs.
No game, compiler, VSIX, character package or runtime implementation changed.
