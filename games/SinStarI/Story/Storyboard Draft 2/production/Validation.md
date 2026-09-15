# Draft 2 Delivery Validation — September 16, 2026

## Completed checks

- Three HTML pages; 874 local file and fragment references resolve.
- All 53 scenes have navigation targets and mapped illustrations.
- All 120 script illustrations use their individual image file, sit inside the
  matching scene, and link back to the exact storyboard panel.
- Forty-eight new illustrations cover every one of the 47 main scenes.
- All 140 PNG files decode: 120 selected illustrations, the original queen crop,
  the supplied poster and 18 archived sheets.
- Script narrative and Canon.md are unchanged. All 30 Draft 1 files match their
  baseline SHA-256 checksums and its file inventory is unchanged.
- External and inline reading JavaScript passed two syntax checks.
- Three 1080p master movies and three website copies decoded completely without
  video/audio decoding errors. Their durations are 29.938, 277.855 and 30.020 seconds.
- Audio peaks remain below full scale. Measured peaks: Room for One −0.5 dB,
  story film −1.4 dB, trailer −2.8 dB. See the JSON report for codec details.
- The 44-shot sequence, new still artwork, corrected takes, trailer cuts and credit
  cards received sampled visual inspection. Six generated movie takes were replaced.
- All three YouTube uploads show Unlisted and ads off. Main film and trailer show
  completed SD/HD processing. AI-use disclosure is saved as Yes. Initial checks
  found no copyright issues; the trailer also showed completed Community Guidelines
  checks with no issues. The main film's completed detail page shows no notices.

## Corrections made during validation

- Corrected the uneven divider for original sheet 13's bottom panels.
- Replaced the queen's inconsistent appearance in original panel 09.2.
- Removed companions introduced before their script entrance and unwanted
  background robots; aligned Orin's identity and costume with the supplied reference.
- Replaced six movie takes for extra figures, robot identity and rescue framing.
- Removed generated lettering from final movie frames and typeset authored captions.
- Corrected the movie gallery's opening-scene link and its Room for One poster.
- Aligned saved ComfyUI UI workflow seeds and reference strength with actual API
  generation settings so the supplied workflow files describe the selected takes.

## Scope of evidence

This delivery uses focused static website checks, not a new Chrome layout test.
Visual review sampled movie frames and captions; it was not a complete human
listening pass of every generated spoken syllable. The art and motion are cinematic
concepts. Generative motion and voice interpretation can differ slightly between
shots. The complete unchanged script remains the story authority.

No .NET compilation, native application tests, VSIX installation or compiler
architecture checks apply: this change is a standalone static story/media edition.
The production modules have separate responsibilities and introduce no dependency
cycle. Generated HTML and JSON grow with authored content; application startup
or runtime source files are untouched.

ZIP integrity, source-byte comparison, file count and archive SHA-256 are recorded
in the companion delivery receipt beside the ZIP. Repository commit/push evidence
is supplied with the final delivery message and in Git history.

Open index.html after extraction. No rebuild or application restart is required;
use Ctrl+F5 in Chrome only when replacing a previously opened copy.
