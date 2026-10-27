"""Port of PolygonCircuit geometry (XZ plane) parsed from the project's C# source."""
import math
import re

import numpy as np

SAMPLES = 1024
import os
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "PolygonRacing", "Runtime", "PolygonCircuit.cs")
HALF_WIDTH = {"forest": 9.0, "coastal": 11.0, "downtown": 10.0, "riviera": 7.5, "sakura": 9.0, "nordwald": 7.0}
LAYOUTS = list(HALF_WIDTH)


def _parse_knots(name):
    text = open(SRC).read()
    m = re.search(name + r"Knots\s*=\s*\{(.*?)\};", text, re.S)
    nums = re.findall(r"Vector3\(\s*([-\d.]+)f?\s*,\s*([-\d.]+)f?\s*,\s*([-\d.]+)f?\s*\)", m.group(1))
    return np.array([[float(x), float(z)] for x, _, z in nums])


class Track:
    def __init__(self, name):
        self.name = name
        self.knots = _parse_knots(name)
        self.half_width = HALF_WIDTH[name]
        ang = np.arange(SAMPLES + 1) * 2 * math.pi / SAMPLES
        self.points = np.array([self.evaluate(a) for a in ang])
        seg = np.linalg.norm(np.diff(self.points, axis=0), axis=1)
        self.dist = np.concatenate([[0.0], np.cumsum(seg)])
        self.length = float(self.dist[-1])

    def evaluate(self, angle):
        k = self.knots
        n = len(k)
        u = (angle / (2 * math.pi)) % 1.0 * n
        i = int(math.floor(u))
        t = u - i
        t2, t3 = t * t, t * t * t
        p0, p1, p2, p3 = k[(i - 1) % n], k[i % n], k[(i + 1) % n], k[(i + 2) % n]
        return (p0 * (1 - 3 * t + 3 * t2 - t3) + p1 * (3 * t3 - 6 * t2 + 4)
                + p2 * (-3 * t3 + 3 * t2 + 3 * t + 1) + p3 * t3) / 6

    def angle_at(self, s):
        s = s % self.length
        lo = int(np.searchsorted(self.dist, s, side="right") - 1)
        lo = min(max(lo, 0), SAMPLES - 1)
        f = (s - self.dist[lo]) / max(self.dist[lo + 1] - self.dist[lo], 1e-9)
        return (lo + f) * 2 * math.pi / SAMPLES

    def at(self, s):
        return self.evaluate(self.angle_at(s))

    def tangent(self, s):
        a = self.angle_at(s)
        d = self.evaluate(a + .0005) - self.evaluate(a - .0005)
        return d / np.linalg.norm(d)

    def side(self, s):
        # Unity: Cross(up, tangent) with XZ = (x, z) -> (tz, -tx)
        t = self.tangent(s)
        return np.array([t[1], -t[0]])

    def curvature(self, s):
        # Unity Vector3.SignedAngle(a, b, up) / 4m: positive = turning right (clockwise from above)
        a, b = self.tangent(s - 2), self.tangent(s + 2)
        cross_y = a[1] * b[0] - a[0] * b[1]  # (a x b).y in Unity XZ
        ang = math.atan2(cross_y, float(np.dot(a, b)))
        return ang / 4.0
