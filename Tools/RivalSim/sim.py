"""Headless race bench for Polygon Racing opponents.

Mirrors the Unity code closely enough to compare AI designs:
- player handling limits from PolygonDriver (topSpeed 42, accel 13, steering 28 deg, wheelbase 2.85)
- shared slipstream rules from PolygonSlipstream
- 'old' rival = current PolygonRival (lanes +/-3.5, constant pace, metre-based catch-up, traffic cap)
- 'new' rival = proposed design (racing line, overtaking, personalities, time-based catch-up)
The player is a scripted bot with a chosen pace and line quality. Rivals have no colliders in the
game, so the player bot ignores traffic (it can drive through cars, as in Unity).
"""
import math
import random

import numpy as np

from track import Track

DT = 1 / 72.0  # Quest 2 frame rate
TOP, ACCEL, BRAKE, STEER = 42.0, 13.0, 24.0, 28.0
WHEELBASE, LOCK_FADE = 2.85, .065
LAPS = 3

# ---------------------------------------------------------------- shared helpers

def kmax(v, steer=STEER):
    return math.tan(math.radians(steer / (1 + max(v, 0) * LOCK_FADE))) / WHEELBASE


def v_corner(k, lock_use, steer=STEER):
    k = abs(k)
    if kmax(90, steer) * lock_use >= k:
        return 90.0
    lo, hi = 0.0, 90.0
    for _ in range(24):
        m = (lo + hi) / 2
        if kmax(m, steer) * lock_use >= k:
            lo = m
        else:
            hi = m
    return lo


class Slip:  # PolygonSlipstream.Charge
    def __init__(self):
        self.build = self.strength = self.carry = self.last = 0.0

    def advance(self, align, dt):
        if align > 0:
            self.build = min(1, self.build + dt / 1.25); self.carry = 1.1; self.last = align
            self.strength = self.build * align
        elif self.carry > 0:
            self.carry = max(0, self.carry - dt); self.strength = self.build * self.last
        else:
            self.build = max(0, self.build - dt / .55); self.strength = self.build * self.last
            if self.build <= 0: self.last = 0
        return self.strength


def slip_measure(L, half, f_s, f_lat, t_s, t_lat):
    """Route-space version of PolygonSlipstream.Measure (alignment checks pass on route)."""
    gap = (t_s - f_s) % L
    if gap < 4.5 or gap > 28: return 0.0
    if abs(f_lat) > half + 1 or abs(t_lat) > half + 1: return 0.0
    lat = abs(t_lat - f_lat)
    if lat > 5.2: return 0.0
    rng = min(1, max(0, (28 - gap) / (28 - 9)))
    al = min(1, max(0, (5.2 - lat) / (5.2 - 3.5)))
    return rng * al


def smoothstep(a, b, x):
    t = min(1, max(0, (x - a) / (b - a))) if b != a else 1.0
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------- racing line + speed plan

class Plan:
    """Per-layout precompute shared by all rivals (stations every STEP metres)."""
    STEP = 2.0

    def __init__(self, track, margin=2.1, iters=700):
        self.track = track
        L = track.length
        n = int(math.ceil(L / self.STEP)); self.n = n; self.step = L / n
        s = np.arange(n) * self.step
        C = np.array([track.at(x) for x in s]); S = np.array([track.side(x) for x in s])
        kc = np.array([track.curvature(x) for x in s])
        self.kc = kc
        lim = track.half_width - margin
        d = np.zeros(n)
        for _ in range(iters):  # elastic band: pull each point toward neighbour midpoint
            P = C + S * d[:, None]
            mid = (np.roll(P, 1, 0) + np.roll(P, -1, 0)) / 2
            d = np.clip(d + .6 * np.sum((mid - P) * S, 1), -lim, lim)
        # light smoothing of the offsets themselves keeps lateral rates gentle
        for _ in range(4):
            d = np.clip((np.roll(d, 1) + 2 * d + np.roll(d, -1)) / 4, -lim, lim)
        self.lat = d
        P = C + S * d[:, None]
        a = P - np.roll(P, 2, 0); b = np.roll(P, -2, 0) - P
        cross = a[:, 1] * b[:, 0] - a[:, 0] * b[:, 1]
        ang = np.arctan2(cross, np.sum(a * b, 1))
        seglen = (np.linalg.norm(a, axis=1) + np.linalg.norm(b, axis=1)) / 2
        self.kline = ang / np.maximum(seglen, 1e-6)
        self.line_len = float(np.sum(np.linalg.norm(np.roll(P, -1, 0) - P, axis=1)))

    def envelope(self, lock_use, decel):
        vc = np.array([v_corner(k, lock_use) for k in self.kline])
        v = vc.copy()
        for _ in range(2):  # backward pass twice around the loop
            for i in range(self.n - 1, -1, -1):
                j = (i + 1) % self.n
                v[i] = min(v[i], math.sqrt(v[j] ** 2 + 2 * decel * self.step))
        return v

    def sample(self, arr, s):
        x = (s % self.track.length) / self.step
        i = int(x); f = x - i
        return arr[i % self.n] * (1 - f) + arr[(i + 1) % self.n] * f


# ---------------------------------------------------------------- cars

class Car:
    def __init__(self, name, s, lat):
        self.name, self.s, self.lat, self.v = name, s, lat, 0.0
        self.lat_vel = 0.0
        self.slip = Slip(); self.draft = 0.0
        self.finish = None

    def kind(self): return "car"


class PlayerBot(Car):
    """Scripted 'human': pace factor of top speed, optional line quality and lapses."""

    def __init__(self, plan, pace=1.0, line=0.8, lapse_rate=0.0, seed=0):
        super().__init__("player", 0.0, 0.0)
        self.plan, self.pace, self.line, self.lapse_rate = plan, pace, line, lapse_rate
        self.rng = random.Random(seed); self.lapse = 0.0
        self.env = plan.envelope(.97, BRAKE * .8 + 1.3)

    def step(self, world, dt):
        p = self.plan
        target_lat = p.sample(p.lat, self.s) * self.line
        self.lat += (target_lat - self.lat) * min(1, dt * 1.5)
        if self.lapse <= 0 and self.rng.random() < self.lapse_rate * dt:
            self.lapse = self.rng.uniform(.8, 2.0)
        lift = .85 if self.lapse > 0 else 1.0
        self.lapse -= dt
        top = TOP * (1 + .08 * self.draft)
        tgt = min(top * self.pace * lift, p.sample(self.env, self.s + self.v * .3))
        acc = ACCEL * (1 + .12 * self.draft)
        if self.v < tgt: self.v = min(tgt, self.v + (acc - 1.3 - .025 * self.v) * dt)
        else: self.v = max(tgt, self.v - (BRAKE * .6) * dt)
        kc = p.sample(p.kc, self.s)
        self.s += self.v * dt / max(.3, 1 - kc * self.lat)


class OldRival(Car):
    """Faithful port of the current PolygonRival.Update."""

    def __init__(self, slot, plan, settings):
        lane = 3.5 if slot == 1 else -3.5
        super().__init__(["blue", "yellow", "green"][slot], 12.0 if slot == 0 else 24.0, lane)
        self.slot, self.plan, self.cfg = slot, plan, settings
        self.home = lane; self.goal = lane; self.adj = 0.0

    def gap_ahead(self, world, lane):
        g = 1000.0
        for c in world.cars:
            if c is self: continue
            rel = (c.s - self.s) % world.L
            if rel > world.L / 2: continue
            if 0 < rel and abs(c.lat - lane) < 3.2: g = min(g, rel)
        return g

    def lane_clear(self, world, lane):
        for c in world.cars:
            if c is self: continue
            rel = (c.s - self.s + world.L / 2) % world.L - world.L / 2
            if abs(rel) < 16 and abs(c.lat - lane) < 3.2: return False
        return True

    def step(self, world, dt):
        cfg = self.cfg
        gapm = world.player.s - self.s
        neutral, full = cfg["neutral_m"], cfg["full_m"]
        st = smoothstep(neutral, full, abs(gapm))
        target_adj = st * cfg["catch"] if gapm > 0 else -st * cfg["ease"]
        self.adj += (target_adj - self.adj) * (1 - math.exp(-dt / 4))
        mult = 1 + self.adj
        target = TOP * 1.01 * mult * (1 + .08 * self.draft)
        pref = self.goal
        gap = self.gap_ahead(world, self.lat)
        if gap < 18 and abs(self.lat - self.goal) < .2:
            alt = -3.5 if self.lat >= 0 else 3.5
            pref = alt if self.lane_clear(world, alt) else self.lat
        elif gap >= 18 and self.lane_clear(world, self.home):
            pref = self.home
        self.goal = pref
        if gap < 11: target = min(target, max(0, (gap - 4) * 2.5))
        acc = ACCEL * .98 * (1 + self.adj * .5) * (1 + .12 * self.draft)
        rate = 18 if self.v > target else acc
        self.v = target if abs(target - self.v) <= rate * dt else self.v + math.copysign(rate * dt, target - self.v)
        self.s += self.v * dt
        self.lat += max(-dt * 1.3, min(dt * 1.3, pref - self.lat))


# Personalities: small differences so the pack stays close but races differently.
PERSONALITIES = [
    dict(name="blue", top=.995, lock=.93, aggression=.45, defend=.35, consistency=.90, mistake=.05),
    dict(name="yellow", top=1.003, lock=.97, aggression=.90, defend=.70, consistency=.75, mistake=.09),
    dict(name="green", top=1.010, lock=.95, aggression=.65, defend=.60, consistency=.95, mistake=.03),
]


class NewRival(Car):
    CAR_W = 2.4          # lateral spacing that counts as "same lane"
    PASS_W = 3.1         # lateral offset used to pull alongside

    def __init__(self, slot, plan, cfg, seed=0):
        lane = 3.5 if slot == 1 else -3.5
        super().__init__(PERSONALITIES[slot]["name"], 12.0 if slot == 0 else 24.0, lane)
        self.slot, self.plan, self.cfg = slot, plan, cfg
        self.p = PERSONALITIES[slot]
        self.rng = random.Random(seed * 31 + slot)
        decel = BRAKE * .8 + 1.3
        self.env_lo = plan.envelope(.85, decel)
        self.env_hi = plan.envelope(1.0, decel)
        self.adj = 0.0
        self.pass_lat = None; self.pass_car = None
        self.defend_lat = 0.0; self.defend_until = -1.0
        self.mistake_until = -1.0; self.mistake_kind = 0; self.next_corner_check = 0.0
        self.phase = self.rng.uniform(0, 100)
        self.next_pass_check = 0.0
        self.t = 0.0

    # catch-up in seconds of gap; positive gap = this rival is behind the player
    def update_pace(self, world, dt):
        cfg = self.cfg
        gap_m = world.player.s - self.s
        gap_s = gap_m / max(20.0, self.v)
        st = smoothstep(cfg["neutral_s"], cfg["full_s"], abs(gap_s))
        target = st * cfg["catch"] if gap_s > 0 else -st * cfg["ease"]
        self.adj += (target - self.adj) * (1 - math.exp(-dt / cfg["response"]))
        return gap_s

    def step(self, world, dt):
        p, plan, L = self.p, self.plan, world.L
        self.t += dt
        gap_s = self.update_pace(world, dt)
        # slow, smooth pace wander (+/-) gives natural gap changes; consistent drivers wander less
        wander = (1 - p["consistency"]) * .05 * (math.sin(self.t * .21 + self.phase) * .6 + math.sin(self.t * .057 + self.phase * 1.7) * .4)
        skill = min(1, max(0, (p["lock"] - .85) / .15 + self.adj * 3))
        env = plan.sample(self.env_lo, self.s + self.v * .25) * (1 - skill) + plan.sample(self.env_hi, self.s + self.v * .25) * skill
        top = TOP * self.cfg["top"] * p["top"] * (1 + self.adj + wander) * (1 + .08 * self.draft)
        target = min(top, env)

        # mistakes: rolled once per noticeable bend; more likely when comfortably ahead of the player
        kc_ahead = max(abs(plan.sample(plan.kline, self.s + x)) for x in (20, 40, 60))
        if kc_ahead > 1 / 160 and self.t >= self.next_corner_check:
            self.next_corner_check = self.t + 6
            chance = p["mistake"] * (1 + 4 * max(0, -self.adj) / max(1e-6, self.cfg["ease"]))
            if self.rng.random() < chance:
                self.mistake_kind = 1 if self.rng.random() < .5 else 2
                self.mistake_until = self.t + self.rng.uniform(1.2, 2.2)
        mistake = self.t < self.mistake_until
        if mistake and self.mistake_kind == 1: target *= .9

        line_lat = plan.sample(plan.lat, self.s)
        desired = line_lat
        if mistake and self.mistake_kind == 2:  # run wide: drift toward the outside of the bend
            k = plan.sample(plan.kline, self.s + 30)
            desired = line_lat - math.copysign(2.0, k)

        # ---------------- traffic
        lim = world.track.half_width - 2.0
        ahead, ahead_rel = None, 1e9
        look = 10 + self.v * 1.1
        for c in world.cars:
            if c is self: continue
            rel = (c.s - self.s + L / 2) % L - L / 2
            if 0 < rel < look and abs(c.lat - self.lat) < self.CAR_W and rel < ahead_rel:
                ahead, ahead_rel = c, rel
        if self.pass_car is not None:
            rel = (self.pass_car.s - self.s + L / 2) % L - L / 2
            if rel < -6 or rel > look + 15:
                self.pass_car = None; self.pass_lat = None; self.next_pass_check = self.t + 2.5
        if ahead is not None and self.pass_car is None and self.t >= self.next_pass_check:
            # decide twice a second: commit only with a real speed advantage (draft or pace)
            self.next_pass_check = self.t + .5
            closing = self.v > ahead.v + 1.0 - .8 * p["aggression"] and ahead_rel < 12 + 10 * p["aggression"]
            if closing and self.rng.random() < .3 + .7 * p["aggression"]:
                side = self.choose_side(world, ahead, ahead_rel, lim)
                if side is not None:
                    self.pass_car, self.pass_lat = ahead, side - ahead.lat
        if self.pass_car is not None:
            desired = max(-lim, min(lim, self.pass_car.lat + self.pass_lat))
        elif ahead is not None:
            follow = 7 + .15 * self.v
            target = min(target, ahead.v + max(-6, min(4, (ahead_rel - follow) * .6)))

        # defend: car behind closing before a bend -> take the inside once, never into an overlapping car
        if self.pass_car is None and p["defend"] > 0 and self.t > self.defend_until:
            for c in world.cars:
                if c is self: continue
                rel = (c.s - self.s + L / 2) % L - L / 2
                if abs(rel) < 8: break  # someone alongside: never start a blocking move
                if -22 < rel < -6 and c.v > self.v - 1:
                    k = plan.sample(plan.kline, self.s + 90)
                    if abs(k) > 1 / 250 and self.rng.random() < p["defend"] * dt * 2:
                        inside = math.copysign(min(lim, abs(line_lat) + 2.5), k)
                        self.defend_lat = inside - line_lat; self.defend_until = self.t + 3.5
                    break
        if self.t < self.defend_until and self.pass_car is None:
            desired = max(-lim, min(lim, line_lat + self.defend_lat))

        # side-by-side spacing: never squeeze into a car that overlaps us
        for c in world.cars:
            if c is self: continue
            rel = (c.s - self.s + L / 2) % L - L / 2
            if abs(rel) < 7.5:
                dl = desired - c.lat
                if abs(dl) < 3.0:
                    away = (self.lat - c.lat) or (1.0 if self.slot % 2 else -1.0)
                    cand = c.lat + math.copysign(3.0, away)
                    if abs(cand) > lim: cand = c.lat - math.copysign(3.0, away)  # no room: other side
                    desired = cand
            # never drive into the back of an overlapping car, even mid-pass
            if 0 < rel < 9 and abs(self.lat - c.lat) < 2.2:
                target = min(target, c.v - (9 - rel) * .8)
        desired = max(-lim, min(lim, desired))

        # ---------------- lateral (smooth spring, limited rate)
        w = 1.4
        acc_lat = w * w * (desired - self.lat) - 2 * w * self.lat_vel
        acc_lat = max(-6, min(6, acc_lat))
        self.lat_vel = max(-4, min(4, self.lat_vel + acc_lat * dt))
        for c in world.cars:  # do not slide sideways into a car that is beside us
            if c is self: continue
            rel = (c.s - self.s + L / 2) % L - L / 2
            if abs(rel) < 6 and abs(c.lat - self.lat) < 3.0 and (c.lat - self.lat) * self.lat_vel > 0:
                self.lat_vel = 0.0
        self.lat += self.lat_vel * dt
        if abs(self.lat) > lim + .5: self.lat = math.copysign(lim + .5, self.lat); self.lat_vel = 0

        # ---------------- speed
        acc = ACCEL * self.cfg["accel"] * (1 + .12 * self.draft) * (1 + max(0, self.adj) * .5)
        if self.v < target:
            self.v = min(target, self.v + (acc - 1.3 - .025 * self.v) * dt)
        else:
            dec = 1.3 + .025 * self.v if self.v - target < 1.5 else BRAKE * .8 + 1.3
            self.v = max(target, self.v - dec * dt)
        kc = plan.sample(plan.kc, self.s)
        fwd = math.sqrt(max(0, self.v ** 2 - self.lat_vel ** 2))
        self.s += fwd * dt / max(.3, 1 - kc * self.lat)

    def choose_side(self, world, ahead, rel, lim):
        L = world.L
        k = sum(self.plan.sample(self.plan.kline, self.s + x) for x in (40, 80, 120, 160))
        best, best_score = None, -1e9
        for sgn in (-1, 1):
            cand = ahead.lat + sgn * self.PASS_W
            if abs(cand) > lim: continue
            free = True
            for c in world.cars:
                if c is self or c is ahead: continue
                r = (c.s - self.s + L / 2) % L - L / 2
                if -8 < r < rel + 14 and abs(c.lat - cand) < self.CAR_W: free = False
            if not free: continue
            score = -abs(cand - self.lat) * .3 + (2 if sgn * k > 0 else 0)
            if score > best_score: best, best_score = cand, score
        return best


# ---------------------------------------------------------------- race

class World:
    def __init__(self, track, plan, ai, player_cfg, rival_cfg, seed=0):
        self.track, self.plan, self.L = track, plan, track.length
        self.player = PlayerBot(plan, seed=seed, **player_cfg)
        mk = (lambda i: OldRival(i, plan, rival_cfg)) if ai == "old" else (lambda i: NewRival(i, plan, rival_cfg, seed))
        self.rivals = [mk(i) for i in range(3)]
        self.cars = [self.player] + self.rivals
        self.time = 0.0
        self.log = []

    def order(self):
        return sorted(self.cars, key=lambda c: (c.finish if c.finish is not None else 1e9, -c.s))

    def run(self, max_t=900):
        n = len(self.cars)
        ahead = {}  # pair -> which is ahead, changes only after a 3 m margin (real passes, no flicker)
        passes = {"player": 0, "rival": 0}
        close = steps = 0
        while self.time < max_t:
            for c in self.cars:
                if c.finish is not None: continue
                d = 0.0
                for o in self.cars:
                    if o is c or o.finish is not None: continue
                    d = max(d, slip_measure(self.L, self.track.half_width, c.s, c.lat, o.s, o.lat))
                c.draft = c.slip.advance(d, DT)
            for c in self.cars:
                if c.finish is None: c.step(self, DT)
            self.time += DT
            for c in self.cars:
                if c.finish is None and c.s >= LAPS * self.L: c.finish = self.time
            if self.player.finish is not None: break
            steps += 1
            if self.time > 8:  # ignore the start sort-out
                for i in range(n):
                    for j in range(i + 1, n):
                        a, b = self.cars[i], self.cars[j]
                        if a.finish or b.finish: continue
                        d = a.s - b.s
                        key = (i, j)
                        if key not in ahead: ahead[key] = d > 0
                        elif ahead[key] and d < -3: ahead[key] = False; passes["player" if i == 0 else "rival"] += 1
                        elif not ahead[key] and d > 3: ahead[key] = True; passes["player" if i == 0 else "rival"] += 1
            nearest = min(abs(r.s - self.player.s) for r in self.rivals)
            if nearest / max(self.player.v, 10) < 1.0: close += 1
        pf = self.player.finish or self.time
        gaps = []  # seconds: positive = rival finished ahead of the player
        for r in self.rivals:
            gaps.append(pf - r.finish if r.finish else -(self.player.s - r.s) / max(r.v, 10))
        place = 1 + sum(1 for g in gaps if g > 0)
        return dict(place=place, player_changes=passes["player"], rival_changes=passes["rival"],
                    close=close / max(1, steps), gaps=gaps, spread_s=max(gaps + [0]) - min(gaps + [0]), time=self.time)
