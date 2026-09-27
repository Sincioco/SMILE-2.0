# Neris Castle authoring operations

The versioned .blend files are the authoritative editable sources. These JavaScript modules
retain bounded bpy payloads sent to the existing Blender MCP, plus plain JavaScript
texture, contact-sheet and validation work. They never install or configure Blender.

Modeling payloads are stage operations, not a one-command castle generator. Load a
fresh copy of the preceding stage, read the intended owner and dependencies, and
preflight stable object IDs before dispatch. Guards reject completed-stage duplicates.
The shared window prototype was rebuilt twice with stable counts; whole-stage
idempotent replay is not claimed. Later ornament placements are retained in the
M05/M06 source; a transcript snippet is not a substitute for those files.

Owner boundaries: model-primitives supplies geometry helpers; m02-entry owns gate
and bridge; m03-facade/m03-towers own palace and tower details; m04-courtyard owns
paving, arcades, fountain and planting; m05-materials/heraldry/ornament own finish.
The M04 door-fitting correction is a deliberate Palace dependency of courtyard
route clearance. m06-export only creates disposable evaluated copies.

Resume by changing the named owner in a new source revision. Recheck bridge sweep
when Gatehouse/Bridge or adjacent ornaments change, and the route when Courtyard,
stairs or the palace threshold changes. Material-only changes do not rebuild Site.
Review cameras keep their original positions. Preserve older checkpoints.

Native delivery uses the existing SMILE project cooker. Its frozen payload contract
is enforced by ../prepare-native.mjs in the repository package. The complete portable
GLB and editable source retain the moat; native terrain owns the placed moat surface.
