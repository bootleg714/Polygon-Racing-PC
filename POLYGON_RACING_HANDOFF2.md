# Polygon Racing VR — handoff v02

Goal: standalone Quest 2 arcade racing in Unity, with flat-shaded polygon visuals inspired by the user's open-wheel cockpit reference. Working title accepted by user: Polygon Racing VR.

Workflow: downloadable ZIP patches merged into the Windows Unity project. User compiles/tests in Unity and on Quest via USB Build And Run. This patch is based on the user's uploaded starter, Unity 6000.6.4f1, URP 17.6.0, Input System 1.20.0. No PC access or local Unity execution was available here.

First milestone: red-and-white editable polygon car, seated view, forest test circuit, desktop and Touch controls. See README_FIRST.md for exact installation/build steps. The user must run the scene generator once inside Unity before testing/building. No generated scene binary, APK or verified hardware results are claimed.

Architecture:
- PolygonWorld: editor-invoked mesh/object builder; dimensions in metres; ellipse centerline radii 150 x 100, road half width 9.
- PolygonDriver: kinematic flat-track bicycle steering; keyboard/Unity XR feature inputs; manual before-render HMD pose tracking; neutral seated pose; sequential 16-point laps; restart and pause; simple world-space TextMesh dashboard.
- PolygonEngineSound: procedural local engine oscillator on the audio thread.
- PolygonRacingSetup (Editor): creates/saves meshes, materials, PlayerCar prefab and ForestCircuit scene; sets shared build scene; configures Android/OpenXR; verifies selected settings.

Runtime scenery is statically batched at startup, leaving Edit Mode hierarchy editable. No artificial camera shake. Cockpit yaw follows car heading. Best lap is session-only. No opponent AI, virtual hands, reverse or full tire physics yet.

Next steps: receive first Unity Console results and desktop screenshot; resolve any compile/layout issues; confirm Quest stereo/seating/head tracking/controllers; tune grip, cornering and speed on hardware. Then refine the car/track visuals, add a ghost or AI car, and expand the circuit. Keep changes incremental and use headset feedback.

Polygon Racing VR — Development Handoff
Updated October 20, 2026
Project and workflow
Polygon Racing VR is a low-poly arcade racing game for Meta Quest 2, built with Unity 6000.6.4f1, URP, and OpenXR.

Development uses downloadable ZIP patches. Thomas copies the scripts into his Windows Unity project, compiles, and tests on the headset.

Project path previously used:

C:\Users\User1\Downloads\Polygon Racing VR

Obtain the latest project ZIP before making further changes. Earlier uploaded projects predate several working updates.

The previous chat’s coding environment became unavailable. Refreshing did not restore it. No slipstreaming patch was created.
Design direction
Thomas wants a fast arcade racer with:
- Large tracks.
- Long straights for drafting and overtaking.
- Wide, sweeping turns that maintain speed.
- Occasional tighter corners for variety.
- Strong speed sensation through scenery and sound.
- A steady VR cockpit camera.
The current circuit is a useful development track. A future full layout should be designed around this faster, flowing style.
Current playable features
- Seated cockpit with headset tracking and recentering.
- Arcade acceleration, braking, and steering.
- Editable procedural polygon car models.
- Three AI opponents: blue, yellow, and green.
- Corner braking, traffic checks, lane changes, and following behavior.
- Three-lap races with countdown, position, lap timing, and results.
- Race menu and restart controls.
- Solo Time Trial with a best lap saved locally.
- Player engine audio.
- Spatial opponent engine sounds.
- Tire squeal during hard cornering.
- Gravel rumble and gentle controller vibration.
- Procedural circuit, curbs, shoulders, guardrails, trees, hills, signs, braking boards, pits, grandstand, and timing tower.
Cars currently use route-following movement without physical car-to-car collision response.
Controls
Action	Quest control	Desktop
Steer	Left stick horizontal	A/D or arrows
Accelerate	Right trigger	W/up arrow
Brake	Left trigger	S/down arrow/Space
Select menu item	Left stick up/down	Up/down arrows
Confirm	A	Enter
Restart current mode	A	R
Return to menu	Y	M
Pause/resume	X	P
Recenter	B	C


Keep headset tracking independent of car movement. Preserve the manually adjusted cockpit and dashboard sizes.
Update history and test status
Version	Change	Status
v06	First blue opponent	Headset tested successfully
v07	Three-lap race, countdown, timing, results	Tested successfully
v08	New circuit, S-bend, landmarks, corner braking, rival black-panel fix	Circuit confirmed working and looking good in headset
v09	Three opponents; four-car grid	Headset tested successfully
v10	Spatial rival engines, tires, gravel sound and vibration	All effects confirmed working
v11	Race menu, Time Trial, improved position tracking	Multiple races tested; positions and Time Trial confirmed working
v12	Longer straight and faster AI	User tested and reported increased opponent speed
v13	All opponents matched to green’s pace	Delivered; explicit test confirmation still pending


Latest delivered patch: PolygonRacingVR_MatchGreenPace_v13.zip

Important earlier patches:
- PolygonRacingVR_MenuTimeTrial_v11.zip
- PolygonRacingVR_LongStraight_v12.zip
- PolygonRacingVR_RacingFeedback_v10.zip
v13 is an incremental patch containing only PolygonRival.cs; it is not a complete project.
Current circuit
The v12 layout has:
- 280 metres of exactly straight road on Pine Straight.
- Approximately 1,440 metres total lap length.
- Retained tighter corners and S-bend.
- An extended eastern section.
- Relocated hills and timing tower to clear the longer circuit.
Track geometry is a closed cubic B-spline shared by scenery, AI, and race progress.

Changing the route scripts does not automatically rebuild saved scenery.

After installing v12, the required editor command was:

Polygon Racing → 5 - Upgrade Long Straight v12

Without running that command, cars follow the new route while the visible road remains on the old layout. This happened during v08 and was resolved by running its scenery upgrade.

The upgrade preserves the existing player car and cockpit, replaces scenery, saves the scene, and creates a backup in PolygonRacingBackups beside Assets.

Do not use the original Create Circuit command when preserving the tuned cockpit.
Current AI pace
Thomas could catch the green car but found it appropriately fast. He requested that blue and yellow match it.

v13 gives all three opponents:
- Straight-line maximum: 98% of the player’s configured top speed.
- Acceleration: 90% of the player’s configured acceleration.
- Corner grip parameter: 9.8.
At the default player top speed of 42 m/s:
- Player theoretical maximum: approximately 151 km/h.
- Opponent target maximum: approximately 148 km/h.
Colors, starting positions, lane choices, and traffic avoidance remain distinct. Traffic can temporarily slow individual cars.

There is no dynamic catch-up adjustment or slipstream boost yet.
Important runtime scripts
Located in Assets/PolygonRacing/Runtime:
Script	Responsibility
PolygonDriver.cs	Player movement, controls, tracking, wheels, base engine integration
PolygonWorld.cs	Procedural scenery and car construction
PolygonCircuit.cs	Shared spline, distance lookup, curvature, nearest-route projection
PolygonRival.cs	AI cars, visuals, pace, traffic behavior, accumulated race distance
PolygonRace.cs	Menu, modes, countdown, timing, positions, results, dashboard
PolygonEngineSound.cs	Original synthesized player engine
PolygonRacingFeedback.cs	Tire/gravel audio, spatial rival engines, controller pulses


Editor script:

Assets/PolygonRacing/Editor/PolygonCircuitUpgrade.cs
Relevant architecture
- Rival and race components automatically spawn when a scene containing PolygonDriver loads.
- Rival visuals copy mesh transforms from the player, excluding tracking, cameras, text, dashboard, and race panel.
- PolygonRace runs after the driver and rivals.
- PolygonRacingFeedback runs afterward.
- Race positions use the player’s unwrapped projected route progress and rivals’ actual accumulated route distance.
- AI exposes RaceDistance and Participating.
- Time Trial hides and locks opponents.
- Race locks mute the new feedback effects.
- Rival model copying must continue excluding cockpit UI to prevent the previous black-square artifact.
Race positions and Time Trial
A reported issue prevented apparent position recovery after overtaking. v11 improved tracking, and Thomas confirmed that positions now work correctly.

Preserve:
- Accumulated rival route distance.
- Player progress across the start-line seam.
- Reverse-driving behavior.
- Progress rejection for large teleports and positions outside the course.
- Close-finish crossing-time interpolation.
- Clear lap-deficit and finished-opponent indicators.
+1L indicates a full lap deficit. Passing a car physically does not erase a full lap deficit.

FIN shows opponents that have finished.

Time Trial runs continuously and saves its best lap locally. Editor and headset records are separate.

The v12 longer layout uses this record key:

PolygonRacing.ForestCircuitV12.TimeTrialBest

Do not compare records from different track layouts.
Slipstream implementation (v14 work in progress)
The v13 source in the project was verified against PolygonRacingVR_MatchGreenPace_v13.zip; the rival script matches exactly.

The first slipstream pass is implemented in the current project:
- Player and participating AI can draft behind another eligible car during a race. Time Trial is excluded.
- A shared evaluator checks track-distance gap, same-direction heading, longitudinal separation, lateral alignment, and road proximity to reject cars alongside, ahead, off-course, or on nearby track sections.
- Draft charge takes about 1.25 seconds to build. Losing alignment preserves the current boost for 1.1 seconds, then fades it over about 0.55 seconds.
- Full draft adds up to 8% top speed and 12% acceleration. AI still obeys its corner-speed limit and traffic braking.
- The race dashboard shows a draft buildup gauge and labels a strong draft as BOOST.
- Draft charge is cleared on menu entry, restart, mode changes, and race finish. Pause suppresses the applied boost without advancing its state.
- Implementation is in PolygonSlipstream.cs, PolygonDriver.cs, PolygonRival.cs, and PolygonRace.cs.

Workspace code diagnostics report no errors. Unity compilation, desktop behavior, Quest performance, and headset feel are still unverified. Next, let Unity finish compiling, then test both sides of the draft, pulling out to pass, AI drafting, close traffic, pause/restart/results, and Time Trial. Tune only after headset feedback.

Selectable Coastal Expressway (new work)
The first layout pass is implemented without changing car handling or headset tracking:
- A separate CoastalExpressway scene is generated from the saved ForestCircuit scene by **Polygon Racing > 6 - Create Coastal Expressway Layout**. The existing ForestCircuit scene and cockpit are preserved. Run the editor command once before selecting the new circuit.
- The new route uses its own `PolygonCircuitSelector` profile. Forest and Coastal use the same spline, distance projection, AI route, race-position and slipstream systems, configured per scene.
- The route is about 3.95 km with a 22 m wide flat road, two approximately 670 m straights, broad bends and one slower technical section. Its design was checked against the spline for about a 53 m minimum radius and no nonlocal centerline crossings.
- **Polygon Racing > 7 - Refresh Coastal Expressway Scenery** rebuilds only the scenery in the existing Coastal scene, preserving its route, car, and handling. It adds a visible ocean horizon, beach and shoreline, low-poly palms and rocks, distant island silhouettes, start/finish spectator stands, and water beneath and beside the bridge. Scenery uses small shared meshes/materials and is covered by the existing startup static batching.
- The menu chooses Race or Time Trial, then Forest Circuit or Coastal Expressway. Changing circuits loads the corresponding scene and starts the selected mode.
- Forest retains its existing `PolygonRacing.ForestCircuitV12.TimeTrialBest` record key. Coastal records use `PolygonRacing.CoastalExpresswayV01.TimeTrialBest`.
- The editor command enables both scenes in the shared build scene list while preserving other entries. The Quest build profile currently uses that shared list; if its scene-list override is later enabled, add both scenes there.

Workspace diagnostics report no errors. Unity scene generation, Unity compilation, visual flow, AI, standings, Time Trial records and Quest behavior still need local validation. Generate Coastal from the saved ForestCircuit scene in Unity, then test both circuits in desktop Play Mode before building for Quest.

Suggested acceptance checks
- Draft activates only behind a suitably aligned car.
- It does not activate alongside, ahead of, or across nearby track sections.
- Pulling out provides a brief passing opportunity.
- AI benefits under the same rules.
- Braking and cornering remain predictable.
- Menu, restart, pause, results, and Time Trial still work.
- Position tracking remains correct.
- Headset performance remains smooth.
Future work
After slipstreaming:
- Tune following, overtaking, and traffic behavior.
- Use the results to design a larger, faster circuit with sweeping turns.
- Improve track readability and speed sensation through scenery.
- Consider physical car contact later; it is not part of the current implementation.
Future browser multiplayer port
Thomas wants to keep a three.js port as a future project.

Preferred experience:

GitHub Pages link → Create Room → generated code → friends join using the code.

His other three.js game already uses this flow. Inspect that game and reuse its multiplayer approach where suitable before choosing a new networking stack.

Potential scope:
- Desktop and browser VR.
- Private room codes.
- Human racers with AI filling empty slots.
- Start with two players, then expand.
- Initially retain cars passing through each other.
The browser port is deferred. Continue Unity Quest development first.
Unity and build lessons
The existing Quest build configuration is working. Preserve it.

Previously used packages included URP, Input System, XR Management, and Unity OpenXR. Optional Meta XR packages caused earlier simulator/compiler and duplicate OpenXR loader issues and were removed.

Important lessons:
- Do not reinstall Meta All-in-One merely to add gameplay features.
- Do not overwrite working Android or active build-profile settings.
- The Meta Quest build profile can have settings that differ from base project settings.
- Avoid rerunning old setup/configuration commands unnecessarily.
- Retain the working OpenXR configuration.
- Preserve the saved dashboard/cockpit scaling.
- Copy individual scripts into the exact folders.
- Keep existing .meta files when replacing scripts.
- Avoid duplicate scripts and accidental Assets/Assets nesting.
The project targets 72 fps, but a target setting is not a measured performance result. Thomas’s hardware reports have been positive; keep validating new features on the headset.
Starting the next chat
Continue Polygon Racing VR with the v14 slipstream implementation and Coastal Expressway. The scene is already generated separately from ForestCircuit; Forest Circuit remains preserved. Run **Polygon Racing > 7 - Refresh Coastal Expressway Scenery** in Unity to apply the updated coastal identity to the existing scene, then rebuild and test visual clarity and Quest 2 performance. Validate route flow, AI pace, standings, independent Time Trial records and drafting on both tracks. The three.js/GitHub Pages room-code multiplayer port remains deferred.