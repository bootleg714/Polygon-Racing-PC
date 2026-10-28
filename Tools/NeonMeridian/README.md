# Neon Meridian v01

Hungary-inspired route revision (October 28, 2026): 3,604 m with a long straight, tight first hairpin and linked bends. Neon city identity retained. Timing ID is NeonMeridianV02, so previous route records remain separate. The updated scene was generated and verified in an isolated Unity project: 1,052 scenery renderers, 266,454 triangles, no sampled road-corridor obstructions, and 34 Play-mode smoke checks passed. Full driven laps, AI behavior through tight turns, readability and sustained FPS on target hardware remain to be tested.

The historical notes below describe the original route and its original verification; their route statistics describe the original route; previews have been updated for the revision.

First playable implementation of the approved neon future-city concept (October 27, 2026).

Open `Assets/PolygonRacing/Scenes/NeonMeridian.unity` and enter Play mode, or choose
**NEON MERIDIAN** from the race/time-trial circuit menu. The scene is already generated
and enabled as the seventh build scene. Allow Unity to finish importing the new assets.
The first import can take a while: the menu entry may appear before the scene is ready.
Thomas confirmed loading works after this initial delay.

To regenerate it, stop Play mode and choose **Polygon Racing > 16 - Create or Refresh
Neon Meridian**. Existing Neon scenes receive a timestamped scene backup first. The
generator replaces only its own city meshes and refreshes named materials in place.
Use **Polygon Racing > Debug - Neon Meridian Landmark Views** to step through viewpoints.
After regeneration, enter Play mode before judging geometry; this project has previously
shown stale edit-mode geometry with the GPU Resident Drawer.

The 4,439 m route starts on Prism Boulevard, climbs Lantern Rise to the Crown, descends
through Cascade, enters the enclosed Undercity, and returns through an elevated skyline
section. Road elevation spans 5.8 to 91.2 m, with a maximum sampled grade of 14.2% and
two mild banked sections. The road is 20 m wide, with continuous illuminated barriers.
The route is original and has no crossover or jumps built into it.

The city contains 382 buildings, an illuminated architectural ring, animated opaque
data facades, high walkways, market stalls, a three-car elevated train, rotating tunnel
fans, team garages, directional signs and functioning countdown signals. It uses
331,300 scenery triangles across 1,318 scenery mesh renderers, spatial/material batching,
16-bit mesh chunks, shared materials and selective bloom. No additional real-time local
lights or texture dependencies were added. The night profile has its own material copies,
sky and volume; applying it does not retune the car materials shared by other circuits.

`concept-board.png` is the brainstorming artwork generated with the built-in image tool;
its exact prompt is in `concept-prompt.txt`. The images in `Previews/` are actual Unity
renders of the implementation. They represent a stylized first version, rather than a
promise to reproduce every feature of the concept artwork.

Verification completed:

- Runtime and editor assemblies compiled with Unity's bundled compiler.
- Native Unity 6000.6.4f1 generated and rebuilt the scene, saved persistent meshes and
  materials, checked finite/nondegenerate geometry and shaders, and sampled an 8 m-wide
  offset on each side of the racing line for 4.2 m of overhead clearance every 6 m.
  The checks found zero obstructions in that sampled corridor.
- Native Play mode passed 38 smoke checks covering the circuit menu, time trial, race
  countdown, three progressing rivals, moving train/fans, and animation isolation from
  the driver's static scenery batching. The test disables XR startup only in the
  isolated project; the main project's XR and Play-mode settings are unchanged.
- An independent route analysis checked grade, seam continuity, route separation and
  exact preservation of all six older layouts' control-point/height/bank arrays.
- The installed six existing scenes, rival settings and PC renderer match their hashes
  before installation. `Previews/report.txt`, `Previews/play-mode-report.txt` and
  `route-analysis.json` record the results.

Still to test: a complete driven lap, crest/dip comfort, night cockpit and mirror
readability, passing on the hills, restart/pause, visual density and sustained FPS on
the target PC. No standalone Windows player build or Quest performance test was done.
The geometry/native tests do not establish frame rate or full-lap opponent behavior.

Developer checks use `Tools/check_neon_source.py` and `analyze_route.py`. The optional
`Tools/prepare_neon_probe.py` creates an isolated Unity copy using this project's
existing local packages, preserving GUIDs. The two native runners here are copied into
that probe's `Assets/Editor` directory; they are intentionally outside the main Unity
Assets tree. `install_generated.py` is a guarded one-time transfer helper, not a refresh
command. Prefer the editor menu for subsequent rebuilds.

Lessons: use `RenderPipeline.StandardRequest` for standalone URP captures so per-frame
lighting, sky and volume globals run; a single-camera request produced misleadingly
dark screenshots. Use an sRGB output target. Keep moving transforms outside the static
scenery group before the driver's Start. Clear the actual cockpit sightline around a
landmark instead of relying on an aerial view. Unity's isolated headless Search index
bootstrap threw an Editor-only startup exception; the Play smoke runner excludes that
specific Editor stack while still failing on game/engine errors.
