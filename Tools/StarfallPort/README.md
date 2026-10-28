# Starfall Port v01

Australia 2011-inspired revision, October 28, 2026: 4,836 m with a rounded end, narrower middle and stepped opposite end, retaining the spaceport setting and existing handling. Elevation is 22.5?79.1 m, maximum sampled grade 12.9%, minimum sampled radius 60.9 m, and timing ID StarfallPortV02. Native Unity road checks found no sampled obstructions, holes or buried road points; 95 Play-mode checks passed. The updated scene has 661 scenery renderers and 157,878 triangles. Full-lap handling, AI and sustained target-PC FPS remain to be tested. The route statistics below describe the original version.

First playable version of the proposed neon spaceport circuit, October 27, 2026.
Thomas approved Aurora Forge and reported pushing the project to GitHub before requesting it.

Allow Unity to finish importing, then select **STARFALL PORT** in the circuit menu or
open `Assets/PolygonRacing/Scenes/StarfallPort.unity` and enter Play. The scene is
enabled as the tenth build scene. The first import may take a little while.

Stop Play and choose **Polygon Racing > 19 - Create or Refresh Starfall Port** to rebuild;
the scene receives a timestamped backup first. **Polygon Racing > Debug - Starfall Port
Landmark Views** cycles through viewpoints. Enter Play after a rebuild before judging
geometry because the GPU Resident Drawer has shown stale edit-mode meshes in this project.

The 4,291 m route has a 20 m road, elevations of 22.5-79.1 m, maximum sampled grade
8.1%, and mild banking up to 5 degrees. The blue-hour port features docked spacecraft,
two animated cargo shuttles, a launch tower, bridges, hangars, illuminated gantries,
and a harbor. It uses an adapted chunked scenery kit with a separate lighting profile.
There are 646 scenery mesh renderers and 146,184 triangles, no added local real-time
lights or textures. Moving shuttles and turbines leave the scenery root before the
driver's static batching; they pause with the player.

The cockpit circuit list uses slightly smaller text to fit the tenth circuit. The
previous routes, car/driver settings, opponent tuning and previous scene files are
preserved. The right-hand panel of `concept-board.png` is the visual direction;
`Previews/` contains actual Unity renders and native test reports.

Verification: runtime/editor compilation and native Unity generation passed. Sampled
road checks every 6 m at -8/0/+8 m found zero overhead obstructions, surface holes or
buried road points. Native Play passed 95 checks including Forest -> Starfall Race,
countdown and three moving rivals, rotating machinery and two moving unbatched cargo
shuttles, Starfall -> Forest Time Trial -> Starfall Time Trial through the actual center
menu, same-track restart and 14-line cockpit menu fit. An independent spline analysis
preserved the nine prior route arrays. Before/after hashes matched for all nine previous
scene files, rival pace and PC renderer. The isolated headless Editor Search-index
startup exception was filtered by its specific Editor stack; game/engine errors fail.

Next: Thomas should drive a complete lap and review the spacecraft and waterfront
visibility, hills/dips, night cockpit and mirrors, rivals, pause/restart and sustained
target-PC FPS. No full-lap, standalone Windows build, sustained target-hardware FPS or
Quest test has been performed for this track. Native sampled checks do not establish
those results.

Source includes `PolygonStarfallPortArt.cs`, `PolygonStarfallPortMotion.cs`,
`PolygonStarfallShuttle.cs`, `PolygonStarfallPortSetup.cs`, shared circuit/race/look
and PC-profile integrations; generated data lives in `Generated/StarfallPort` and
`Generated/PCVisuals/StarfallPort`. `route-analysis.json`, test scripts and reports
record the checks. Use the Unity menu for subsequent rebuilds, not the one-time
source-scaffold or installation helpers.
