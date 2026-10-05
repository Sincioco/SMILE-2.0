# Viewer export: documents and photographs

[Start here](../README.md) · [Town files](town-editor.md#files-exports-and-recovery) · [R04 status](status.md#viewer-export-r04)

**R04 proposed design / work in progress.** The cold implementation is uncommitted
and its real-asset native fixture fails overall. This guide distinguishes the
established warm-photo contract from the working-tree design; it does not describe
cold capture as released or accepted behavior. Material admission, Metropolis
capture and inherited backdrop composition remain [open blockers](status.md#viewer-export-r04).
The October 3 rejection-only behavior remains
[historical evidence](archive/2026-10.md#october-3-2100-handoff-export-identity-and-native-acceptance).

## Established warm-photo contract

Queue acceptance freezes a matching photograph/document pair. Later edits, cache
replacement and eviction cannot redirect it. Save As retains source-photo identity;
later revisions stay dirty, and a PNG failure after `.town` success is explicit
partial success. The 17-slot warm identity fixture passes. The accepted frozen
revision with no matching photograph remains R04's unfinished case. The sections
below describe how the uncommitted implementation attempts to cover it.

## Proposed three-identity model

Save for Viewer and Save All freeze the clicked revision. Preparation may resolve
legacy defaults or ground authored NPC spawns before saving. Those accepted inputs,
prepared outputs and rendered inputs must remain distinguishable:

| Record | Meaning and owner |
| --- | --- |
| Request `.Input` | Accepted source document, serialized when queued; `TownSaveQueue` / `TownWorldPreviews` retain it for content identity and dirty-state decisions |
| Prepared saved output | `TownFileJobs.Pending` after `TownSavePreparation`; the `.town` bundle includes its checked terrain/road companions and uses the requested output name |
| Cold request `.Rendered` | Prepared document actually submitted by `TownExportScene`, under the source photograph name; written beside request `.Image` only after a successful checked capture |

`.Input` is never rewritten to disguise preparation changes. `.Rendered` is request
provenance, not the authoring authority or a new portable save format. These private
records are released with their bounded request slots. Name/revision alone is not
enough: `TownDocumentStore.MatchesDocument` compares serialized document content.

**Save As** writes the requested copy name but looks up and renders using the source
map's photograph identity. The job uses its own `Pending` copy for the source-name
comparison; it never temporarily renames the live editor document. Later edits must
not be marked saved merely because a queued name/revision still appears to match.

## Retain or regenerate

At queue acceptance, a matching warm image/input pair is copied into request-owned
storage. A transfer takes its own copy before the queue releases its slot. Later
edits, replacement of both mutable thumbnail generations or cache eviction cannot
substitute another image for that accepted pair.

After preparation, the job compares the prepared document under the source name
with accepted `.Input`. If it still matches and the warm image was retained, that
exact photograph is reused. It is not regenerated just because the live scene moved.
If preparation changes serialized content, such as grounding an NPC or resolving
legacy residents, the old warm image is invalid for that prepared output: the job
captures a new photograph of the prepared request. A missing warm image follows the
same cold path. Neither case polls a mutable live cache for a replacement revision.
Blender-only export has no photograph prerequisite.

## Proposed isolated cold scene

`TownExportScene.State` owns its document, terrain renderer/cache, catalog instances,
attachments, residents, campfires and Court/Spaceport/Horizon preview resources.
Catalog geometry/imported PBR are leased from `TownCatalogRenderer`'s reference-counted
immutable model bank. Each scene still owns its draw objects, wind animators and city
material overrides; the last model owner releases the shared resource. This reduces
duplicate admission without sharing mutable scene presentation.
The intended cold scene prepares one bounded catalog/terrain/landmark/resident step at a time, then draws
from the document's saved initial camera. A viewport never remains open across
updates. Existing pool limits apply; allocation, preparation or draw failure fails
the request before file output instead of publishing an incomplete photograph.

`TownSurfaceRenderer.State.CacheTown` belongs to that renderer instance. Cold terrain
reads the request's prepared terrain key; missing/invalid request pages fail rather
than rebuilding through the live map's cache. Landmark route owners expose complete
exchangeable contexts, including transforms, enablement and motion/entry state.
The cold scene exchanges them in only around its load/draw/release operations and
exchanges them back before returning to the live scene.

Cold composition is an authored scene pose: residents stand idle at their prepared
spawn/facing; only request-owned campfire emitters advance five 100 ms steps to a
500 ms flame pose. Admission is checked without advancing live emitters. The cold
scene omits the transient party and city flyovers; it does not reproduce the user's
current simulation instant. Existing beacon, firelight and Sphere effects may still
read wall time, so independent cold renders are not promised byte-identical PNGs.
Accepted warm copies remain byte-for-byte preserved. Saved map lighting and
starting-camera policy apply.
Capture restores the accepted camera and resets the viewport; the normal scene
reapplies its own frame lighting. No capture operation selects a different live tab,
changes editor camera/state, runs live resident errands or rewrites live recovery.
These are required isolation boundaries. The observed arena backdrop in a successful
small-map PNG shows that composition isolation is not yet established.

## Completion and cleanup

`.town` and PNG are separate verified writes. A PNG failure after town success is
explicit partial success and leaves the valid town file. A failed batch map does
not discard later maps; any batch failure keeps progress below 100%. Only complete
success reaches 100%. Source edits and later dirty revisions remain intact.

Cancellation releases pending queue payloads. Session teardown releases cold render
resources and cancels an unsubmitted request, clearing preparation/request state so
re-entry can retry. An already-submitted native transfer retains its own photograph
bytes until its town/PNG sequence finishes. Cleanup releases captures, dynamic
snapshot pages and owned scene resources; it does not erase live thumbnail storage.
Cold campfire cleanup uses `Destroy(..., False)`: it destroys the request emitters
while retaining the initialized shared Fire family. Normal Studio teardown owns
release of that bounded global family after the remaining emitters are gone.

## Acceptance boundary

The focused owners are `TownSaveQueue`, `TownFileJobs`, `TownWorldPreviews`,
`TownExportScene`, `TownSurfaceRenderer`, `TownResidents` and `TownCampfires`, plus
the three landmark route contexts. The [native validation guide](build-and-validation.md#focused-validation)
lists runners. Final evidence must distinguish the 17-slot warm identity regression
from real-asset cold export, prepared/output identity, Save As, cancellation/re-entry,
partial PNG failure and live-state/resource-isolation checks. A source review or
valid PNG file alone does not establish those native contracts. The current
[recorded results](status.md#viewer-export-r04) fail overall; the cold implementation
remains uncommitted work and R04 is not complete.
