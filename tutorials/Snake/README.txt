SMILE 2.0 (SinBASIC) — Snake Visual Tutorial Website
==========================================================

INSTALLATION
------------
Place this folder at:

<SMILE-2.0-root>\tutorials\Snake

The official game should remain at:

<SMILE-2.0-root>\games\Snake

Here, <SMILE-2.0-root> means whatever folder contains the student's SMILE 2.0 repository. It may be on any drive and may have any parent folder name.

OPEN THE TUTORIAL
-----------------
Open this file in a modern browser:

<SMILE-2.0-root>\tutorials\Snake\index.html

RELATIVE GAME LINKS
-------------------
The website uses this relative path:

../../games/Snake

That keeps project and sound links portable instead of assuming a development-machine drive letter.

LEARNING SOURCE
---------------
The tutorial follows:

games\Snake\Program-NoDemo.smile

The full source page and checkpoint fragments show matching canonical line numbers. Copy Code copies only clean source text and never includes the visible line numbers.

FILES
-----
index.html                         Tutorial home
01-get-ready.html through
20-graduation.html                 Topic pages
assets\css\tutorial.css          Shared design
assets\js\tutorial.js            Pure JavaScript interaction
assets\images                     High-resolution PNG and SVG visuals
assets\code                       Reference source snapshots
tutorial-manifest.json             Package metadata

NOTES
-----
- Keep the tutorials and games folders at the same repository depth.
- The left navigation preserves its scroll position while moving between topics.
- Completion progress and navigation state are browser-local conveniences.
- Sound players use the existing files under games\Snake\Assets.
- Footer links are muted until hovered or keyboard-focused.
