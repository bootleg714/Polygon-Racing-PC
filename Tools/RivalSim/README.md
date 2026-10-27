# Rival AI simulation bench (host-only tool, not part of the game build)

A headless Python model of the race used to design and tune `PolygonRival` Racecraft v01.
It reads the track knots straight from `Assets/PolygonRacing/Runtime/PolygonCircuit.cs`,
mirrors the player's handling limits from `PolygonDriver`, the shared slipstream rules from
`PolygonSlipstream`, the old rival logic (for comparison) and the new rival logic.

Needs Python 3 with NumPy. Run from this folder:

    python tune.py '{}'                         # old AI vs new AI with the default settings
    python tune.py '{"ease":0.10}' '{"catch":0.06}'   # try settings (JSON keys below)
    python safety.py                            # road bounds, car overlaps, stalls on all six tracks

Setting keys: top (topSpeedFactor), accel (accelerationFactor), neutral_s, full_s,
catch (catchUpBoost), ease (leaderEase), response (responseSeconds).

The "player" is a scripted bot at four skill levels (casual, average, good, expert), so results
show how the field responds to different drivers. It is a model, not Unity: use it to compare
settings, then judge the feel in the game. Keep `sim.py` in step with `PolygonRival.cs` when the
AI logic changes.
