# Polygon Racing VR — handoff v01

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
