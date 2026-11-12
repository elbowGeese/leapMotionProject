THUMBELINA SKATEPARK

Scene: Assets/Max/MaxScene.unity
Reusable model: Assets/Max/Skatepark/ThumbelinaSkatepark.prefab

The park is already built and saved as editable scene objects and mesh assets.
It does not need a runtime generator or any extra package.

Scale: 1 Unity unit = 1 metre. Paved footprint: approximately 84 x 62 metres.
Open flatground starting point: (-14, 0.65, -21).

01 Site: open practice plaza, connecting paths and slab joints. No visible text.
02 Street: enlarged funbox, side bank, kicker, manual pads, ledges, rails, six stairs,
hubbas, an extra bank platform and diagonal ledge line.
03 Flow: four curved quarter pipes, a spine transfer and enlarged pump rollers.
04 Bowls: enlarged 3 m and 1.8 m deep bowls, with deck ledges and drains.
05 Landscape: trees, planters, cedar benches, pavilion and lamps.

All riding surfaces have static colliders. Bowl openings are real openings;
there is no flat ground collider covering them. Concrete friction is 0.12.
Each skate facility is one selectable GameObject with a MeshFilter, a
multi-material MeshRenderer and its own non-convex MeshCollider. Separate
riding faces, sidewalls, end faces and related rails are consolidated.
Decorative details retain their original collision exclusion in the combined
collision mesh. The central funbox includes its side bank and rooftop rail.
Mesh curves and collision meshes match. Trees share one foliage mesh.
Current mesh, triangle and collider counts are recorded in validation.txt.

Validation evidence: Assets/Max/ParkBuild/validation.txt and physics-validation.txt.
The physics check used an isolated scene: 600 steps of 0.01 seconds, two bowl
drop tests and a rolling quarter-pipe test. Actual controller feel still needs
testing with the team's skateboard and Leap Motion implementation.

The one-time editor generation and preview-capture script has been removed.
The saved scene, prefab and meshes can be edited directly in Unity.

This deliverable is the environment model. Player controls, tricks, scoring
and game UI have not been added.
