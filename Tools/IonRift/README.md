# Ion Rift v01

Malaysia-inspired route revision, October 28, 2026: paired long straights, a final hairpin and a more technical middle sector in the existing neon canyon. The original route notes below are historical. The revised lap is 6,821 m, elevation 60.0?104.2 m, maximum sampled grade 6.1%, minimum sampled corner radius 26.7 m, and uses timing ID IonRiftV02. Unity verified road clearance and surfaces and passed 90 Play-mode checks; 775 scenery renderers and 208,913 triangles. Full-lap handling and AI behavior, visual quality and FPS on the target PC remain to be tested.

First playable version of the approved neon canyon concept, October 27, 2026.

Open `Assets/PolygonRacing/Scenes/IonRift.unity` and enter Play, or choose **ION RIFT**
from the race/time-trial circuit menu. The scene is generated and enabled as the eighth
build scene. Allow Unity to finish importing; the previous neon track's menu entry appeared
before its initial import finished, and selecting it worked after the import completed.

To rebuild, stop Play and choose **Polygon Racing > 17 - Create or Refresh Ion Rift**.
The generator backs up the existing scene first and rebuilds its dedicated generated assets.
Use **Polygon Racing > Debug - Ion Rift Landmark Views** to cycle through road views.
Enter Play after a rebuild before judging geometry because the GPU Resident Drawer has
previously displayed stale edit-mode meshes in this project.

The original 4,784 m route has a 20 m road, elevations from 18.3 to 140.5 m, a maximum
sampled grade of 13.1%, two mild banked sections (maximum 5 degrees), and no crossover.
The lap runs from Rim Station through the cliff ascent and high overlook, descends over
the canyon viaduct, enters Reactor Gorge and returns via a lower bridge. The canyon uses
142 faceted rock formations, 31 industrial structures, illuminated arches and power
towers, a flowing energy channel, spillways and rotating reactor turbines. It has 784
scenery mesh renderers and 170,722 scenery triangles, divided into spatial/material
chunks with 16-bit meshes. It adds no real-time local lights or texture dependencies.

Its dedicated night profile and material copies keep the approved Neon Meridian lighting
and existing car materials isolated. The existing engine, handling, cockpit and opponent
tuning remain unchanged. `concept-board.png` is the brainstorming board; the left-hand
panel is the approved Ion Rift direction. `Previews/` contains actual Unity renders and
native-check reports, while `route-analysis.json` records independent spline checks.

Verification completed:

- Runtime and editor assemblies compiled using Unity's bundled compiler. Native Unity
  generated and rebuilt the scene, validated mesh/material persistence and shaders, and
  sampled the road every 6 m at -8, 0 and +8 m laterally. Checks found zero overhead
  obstructions, road surface holes, or road samples buried inside solids.
- Native Play mode passed 90 checks with the project's domain and scene reload settings:
  Forest to Ion Rift Race via the shared menu handler; race countdown and three moving
  opponents; rotor movement and pause; Ion Rift to Forest Time Trial and back to Ion
  Rift via the center-menu Enter path; same-track restart and menu text fit.
- Independent analysis checked spline continuity, grade, corner radius, nonlocal road
  separation and exact preservation of the seven prior routes' control-point, height and
  banking arrays. Seven existing scene files, rival pace and the PC renderer matched
  before/after hashes during installation.
- The isolated native Play runner excluded a known Unity Editor Search-index startup
  exception specific to the headless probe; it failed on game or engine errors.

Still to test: a complete lap, crest/dip comfort, how well the canyon and reactor read
from the cockpit, rivals through the full lap, mirrors, pause/restart in the real game,
and sustained target-PC FPS. A standalone Windows player build and Quest performance
test were not performed. The native samples and smoke tests do not establish those results.

Source lives in `Assets/PolygonRacing/Runtime/PolygonIonRiftArt.cs`,
`PolygonIonRiftMotion.cs`, `Resources/PolygonIonEnergy.shader`, and
`Editor/PolygonIonRiftSetup.cs`, with integration in the shared circuit, race, look and
PC-profile files. Generated assets are under `Generated/IonRift` and
`Generated/PCVisuals/IonRift`. `build_checks.py`, `IonRiftProbe.cs` and
`IonRiftPlayProbe.cs` are development helpers for the isolated Unity test project;
`install_generated.py` is a guarded one-time transfer, not a refresh command.

Lessons: preserve the road's full three-dimensional spline when laying terrain and
bridge geometry; check for buried road samples as well as close overhead obstacles.
Keep moving machinery outside the scenery root before the driver's static-batching
step. Standard URP render requests with an sRGB output target provide representative
lighting in offline previews. Verify cross-scene selection, since the previous track's
initial smoke test began inside its own scene and missed the menu loading path.
