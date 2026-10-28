# Aurora Forge v01

South Korea-inspired route revision, October 28, 2026: long straight, hairpin and a technical return sector, retaining the Arctic scenery and existing handling. The revised route is 4,489 m with elevation 32.0?63.7 m, maximum sampled grade 12.9%, and minimum sampled corner radius 32.6 m. Timing ID is AuroraForgeV02. Native Unity road checks found no sampled obstructions, holes or buried road points; 90 Play-mode checks passed. Revised scenery has 737 renderers and 155,980 triangles. Full-lap handling, AI and target-PC FPS remain to be tested. Notes below describe the original route and verification.

First playable arctic circuit, October 27, 2026. Thomas approved Ion Rift's visuals,
driving and solid 72 FPS before requesting this track.

Open `Assets/PolygonRacing/Scenes/AuroraForge.unity` and enter Play, or select **AURORA
FORGE** from the race/time-trial circuit menu. It is enabled as the ninth build scene.
Allow Unity to finish its first import before selecting it in the menu.

Stop Play and choose **Polygon Racing > 18 - Create or Refresh Aurora Forge** to rebuild.
The existing scene receives a backup first. **Polygon Racing > Debug - Aurora Forge
Landmark Views** cycles through viewpoints. Enter Play before judging geometry after
a rebuild because this project's GPU Resident Drawer can show stale edit-mode geometry.

The 4,171 m route has a 20 m-wide road, elevations of 23.3-98.7 m, a maximum sampled
grade of 10%, and two mild banked sections (maximum 5 degrees). Snow-covered faceted
ice, an industrial forge, bridges and tunnels use an adapted spatially chunked scenery
kit; the arctic palette and nighttime PC profile are independent of Ion Rift and Neon
Meridian. An animated shader draws green and violet northern-light curtains without
textures or per-frame CPU updates. 736 scenery renderers contain 153,248 triangles;
there are no added real-time local lights. The aurora uses transparent geometry, so
its performance still needs measuring on the target hardware.

The route and old user-approved track arrays are separate. Car handling, opponent
tuning, and the old scene files are preserved. The circuit menu text is slightly
smaller while the circuit list is open so the ninth entry and controls fit the existing
cockpit panel; other menu text retains its previous size.

Verification: runtime/editor compilation and native Unity generation passed. Road
samples every 6 m at -8/0/+8 m found zero overhead obstructions, missing surface or
buried road points. The native Play test passed 90 checks covering Forest to Aurora
Race, countdown and three opponents, rotor animation/pause, Aurora to Forest Time Trial
and back to Aurora through the center-menu Enter path, same-track restart, and menu text
fit. Independent spline analysis preserves the eight previous route arrays. All eight
existing scene files, the rival settings and PC renderer matched pre/post install hashes.
The isolated headless Unity Editor Search-index startup exception was excluded by its
specific Editor stack; game and engine errors still fail the test.

`Previews/` has actual Unity renders and reports; the middle panel of `concept-board.png`
is the approved artistic direction. `route-analysis.json` has the numerical route check.
Source includes `PolygonAuroraForgeArt.cs`, `PolygonAuroraForgeMotion.cs`,
`PolygonAuroraRibbon.shader` and `PolygonAuroraForgeSetup.cs`; generated data is in
`Generated/AuroraForge` and `Generated/PCVisuals/AuroraForge`.

Next: Thomas should drive a full lap and review the northern lights, snow and ice,
crest/dip comfort, forge visibility, cockpit/mirrors, rivals, pause/restart, and sustained
PC FPS. No full-lap, standalone Windows player build, target-PC FPS or Quest test has
been performed for this circuit. Native sampled checks do not establish those results.
