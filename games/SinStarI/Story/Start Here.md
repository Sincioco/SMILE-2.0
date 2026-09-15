# Sin Star I - Start Here

## Reading order

1. **[index.html](index.html)** - the illustrated HTML website, with dialogue
   directly beneath each panel and expandable scene/game notes.
2. **Storyboard-Draft-1/Storyboard.md** - editable panel dialogue, descriptions,
   purposes, growth and the scene-to-page index.
3. **Canon.md** - the accepted D01-D16 decisions and remaining open details.
4. **Sin-Star-I-Game-Script-v0.2.html** - the complete script with accepted-canon
   labels, revised D10 scene and updated review register. This local reading
   document includes its existing styles and controls; it needs no server.

## Scope and review

The comic is a condensed visualization of all 47 main scenes and six optional
scenes, not a line-for-line replacement for the full script. Pages 01-16 follow
the main story; pages 17-18 show the six optional character scenes. Panel labels
retain the original scene IDs. Give feedback such as `Page 09, panel 2` or
`C06-S05` to identify an exact beat.

D01-D16 are canon by Sin's September 15 acceptance. Dialogue, background designs,
camera staging and gameplay notes are reviewable drafts. The art sheets contain
no dialogue lettering; text is selectable in the HTML and kept editable in Markdown
and `storyboard.json`. `art-prompts.json` records the full image-generation prompts.
`art-revisions.json` records continuity corrections; `art-manifest.json` identifies
the selected outputs and their checksums. All 18 images are in `asset/images`.

The artwork is narrative concept art, not revised character packages or playable
game content. New environment choices, including Veyra's architecture, remain
visual proposals. Nothing here claims implementation of the suggested encounters.

## Open the website

Extract **Sin-Star-I-Storyboard-Draft-1-Website.zip**, then open **index.html**.
Keep `asset`, `Storyboard-Draft-1` and the companion files beside it. The website
also works directly from this source folder and can be served by any static web
server. It requires no installation, package download or internet connection.

Use **Contents** to jump to a sheet. Read panels left to right on a wide screen,
or top to bottom on a phone. Select an image to open the full art sheet; select a
scene number to read the complete dialogue in the revised script. Scene notes
explain purpose, character growth and the suggested game beat.

No .NET rebuild, VSIX installation or application restart is needed. Open
`index.html`; press Ctrl+F5 in Chrome only if replacing a previously opened copy.
Image and style URLs include content versions to prevent stale cached assets.
Studio and game Web adoption/publication remain on hold. Browser validation here
is limited to this explicitly requested, local storyboard website.

## Delivery checks

- All 47 main scenes and six optional scenes are mapped to 72 panels with dialogue.
- All 18 PNG images decode successfully and match the selected-output checksums.
- Both HTML documents have valid local files and fragment targets: 382 references.
- Chrome desktop and narrow-screen reading layouts were checked, including the
  contents menu, image placement, selectable dialogue and horizontal overflow.
- The ZIP is integrity-checked; every extracted file is compared with its source.

The HTML is a static reading edition, with CSS owning layout and a small script
closing the contents menu and opening linked draft notes. The storyboard JSON
owns the authored panel sequence; Markdown retains an editable reading copy.
No compiler, game, model, calibration or application-build files are changed.
