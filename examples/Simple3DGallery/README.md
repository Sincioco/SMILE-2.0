# SMILE 2.0 Simple3D Gallery

Open `Simple3DGallery.slnx`, select Windows or Web, and run. The same source displays a cube, sphere, pyramid, and donut through Smile.Simple3D and ordinary `Draw Line` output.

Controls:

- drag the object with mouse, pen, or touch; release while moving to throw it into inertial spin;
- mouse wheel zooms within safe limits;
- arrow keys rotate; 1–4 select cube, sphere, pyramid, or donut;
- A toggles automatic spin; Space pauses;
- virtual X toggles axes, Y toggles the floor grid, B toggles perspective/orthographic, and A resets the orbit;
- Escape exits.

`CreateGallery` creates each bounded mesh once. `DrawGallery` begins a 2,500-line frame budget, draws the optional grid, current model, axes, and a 2D text overlay. `DestroyGallery` releases every handle. This lifecycle is the pattern applications should follow.

To make a custom mesh, call `Mesh.CreateMesh`, add all vertices, then add edges using the returned zero-based vertex indices. Validate every return, destroy a partially built handle on failure, and never rebuild static geometry in the frame loop.

Advanced note: public code uses degrees and world units. FixedMath internally scales trigonometry by 16,384 because SMILE Number is currently an integer. Every edge is near-plane clipped before division and viewport clipped before the 2D renderer sees it.
