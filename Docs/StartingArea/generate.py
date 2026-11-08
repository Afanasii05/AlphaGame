"""
Cinder Sanctuary - starting map generator (sanctuary only).

Single source of truth for the layout. Running it writes, into the output folder:
  heights.r16   - 257x257 uint16 heightmap (row = z, col = x)
  splat.bytes   - 256x256x6 uint8 alphamap (interleaved layers)
  grass.bytes   - 256x256 uint8 grass density
  props.csv     - every prop (hand-placed + scattered)
  layout.json   - frame, spawn, zones
  GuideMap.png  - the guide map

Unity terrain: position (-64, -10, -62), size (128, 100, 128), 0.5 m per sample.
Layers: 0 Grass007, 1 Ground037 (dirt), 2 Gravel043 (path), 3 Rock064 (cliff), 4 Moss002, 5 Ground068 (dark soil)
"""
import json, math, os, sys
import numpy as np
from scipy.ndimage import map_coordinates, gaussian_filter

OUT = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(OUT, exist_ok=True)

# ----------------------------------------------------------------------------- terrain frame
OX, OY, OZ = -64.0, -10.0, -62.0
SX, SY, SZ = 128.0, 100.0, 128.0
HRES, ARES = 257, 256
NOISE_OX, NOISE_OZ, NOISE_S = -256.0, -226.0, 512.0   # fixed noise frame, keeps the hill shapes stable

# ----------------------------------------------------------------------------- layout
ARENA = dict(c=(0.0, 0.0), r=19.0, h=-0.12)                          # Cinder Amphitheater (floor top y = -0.10)
COURT = dict(c=(0.0, 19.0), half=(16.0, 11.0), rad=5.0)              # Tollgate Court
ENTRY = dict(c=(0.0, 32.5), half=(7.0, 6.5), rad=3.0)                # entry nook outside the arch (spawn)
GATE_APPROACH = dict(a=(0.0, -6.0), b=(0.0, -23.0), r=8.5)
GATE_COURT = dict(c=(0.0, -19.0), half=(23.0, 3.2), rad=2.5)         # flat foot of the south wall
TOWER = dict(c=(0.0, -46.0), h=28.0)                                 # Cinder Tower hill behind the gate
TIER_ANGLES = [0, 30, 120, 150, 180, 210, 330]                       # amphitheater seating (yaw = 195 - phi)
SPAWN = dict(pos=(0.0, 34.5), yaw=180.0)

ZONES = [(1, "Entry Nook", (0, 33), "#7d6aa8"),
         (2, "Tollgate Court", (0, 18), "#8f8f6a"),
         (3, "Cinder Amphitheater", (0, 0), "#d9822b"),
         (4, "Sanctuary Gate", (0, -24), "#c9b26b")]

# ----------------------------------------------------------------------------- hand-placed props
# (group, prefab, x, z, yaw, scale, ymode, y, rx, rz)
#   ymode: g = terrain height at pivot + y,  a = absolute y,  m = lowest terrain under footprint + y,
#          p = lower-quartile terrain height under footprint + y
G1, G2, G3, G4, G5 = "01_Entry", "02_TollgateCourt", "03_CinderAmphitheater", "04_SanctuaryGate", "05_Boundary"
P = []
def add(g, pf, x, z, yaw=0.0, s=1.0, ym="g", y=0.0, rx=0.0, rz=0.0):
    P.append((g, pf, float(x), float(z), float(yaw), float(s), ym, float(y), float(rx), float(rz)))

# 1 - Entry Nook: a quiet pocket outside the arch, two torches frame the way in
add(G1, "Torch_Standing", -3.4, 30.2); add(G1, "Torch_Standing", 3.4, 30.2)
add(G1, "Floor_Slab_2x2", 0.2, 31.6, 5, 1, "g", 0.02); add(G1, "Floor_Slab_2x2", -0.3, 33.8, -7, 1, "g", 0.02)
add(G1, "Rubble_Pile", 5.2, 35.5, 40); add(G1, "Boulder_A", -6.0, 36.8, 30, 1.2)
add(G1, "Pillar_Broken", -5.4, 31.8, 110)
add(G1, "Tree_Dead", 6.4, 38.5, 200, 1.4)
add(G1, "Bones_Pile", 2.6, 37.4, 70); add(G1, "Bush_Small", -2.5, 39.5); add(G1, "Bush_Medium", 7.5, 32.5, 50)

# 2 - Tollgate Court: the pinch point; through the narrow arch the amphitheater and gate are revealed
add(G2, "SM_Tollgate", 0, 24, 0, 1, "a", 0.0)
add(G2, "SM_RuinWall_Straight", -8.7, 24.3, 0, 1, "g", -0.05)
add(G2, "SM_RuinWall_Broken", -13.1, 24.6, 6, 1, "g", -0.1)
add(G2, "SM_RuinWall_Low", -17.4, 25.4, 14, 1, "g", -0.1)
add(G2, "SM_RuinWall_Straight", 8.7, 24.3, 180, 1, "g", -0.05)
add(G2, "SM_RuinWall_Straight", 13.1, 24.5, 183, 1, "g", -0.1)
add(G2, "SM_RuinWall_Broken", 17.4, 25.3, 168, 1, "g", -0.15)
add(G2, "Brazier", -4.2, 21.2); add(G2, "Brazier", 4.2, 21.2)
add(G2, "Banner", -5.4, 26.7, 0); add(G2, "Banner", 5.4, 26.7, 0)
for x, z, yw in [(0.1, 29.4, 8), (-0.4, 27.3, -5), (0.3, 19.2, 12), (-0.2, 16.9, 3), (0.4, 13.6, -8)]:
    add(G2, "Floor_Slab_2x2", x, z, yw, 1, "g", 0.02)
add(G2, "Pillar", -8.5, 14, 0); add(G2, "Pillar_Broken", 8.8, 15.5, 140)
add(G2, "Rubble_Pile", 5.5, 19, 30); add(G2, "Rubble_Pile", -6.0, 17.0, 0, 0.9)
add(G2, "SM_Bridge_Rubble_B", 10, 11, 25)
add(G2, "Barrel", -10.6, 21.4); add(G2, "Crate", -9.2, 21.9, 15); add(G2, "Crate_Small", -9.3, 21.9, 40, 1, "g", 0.8)
add(G2, "Sacks", -11.8, 20.5, 60); add(G2, "Pot_Amphora", -8.3, 20.8, 0)
add(G2, "Bush_Medium", -15, 20.5); add(G2, "Bush_Small", 14.5, 20); add(G2, "Bush_Large", 16.5, 12.5, 70); add(G2, "Bush_Small", -16, 13)

# 3 - Cinder Amphitheater: the arena. Shrine at the centre, ring of tiers, weeping guardians at the gate
add(G3, "SM_Amphi_Floor", 0, 0, 0, 1, "a", 0.0)
add(G3, "SM_CinderShrine", 0, 0, 0, 1, "a", 0.0)
for phi in TIER_ANGLES:
    add(G3, "SM_Amphi_Segment", 0, 0, 195 - phi, 1, "a", -0.05)
r = 12.5; add(G3, "SM_Bridge_Rubble_A", r * math.cos(math.radians(60)), r * math.sin(math.radians(60)), 30, 1.1, "g", -0.1)
add(G3, "Rubble_Pile", 7.6, 10.4, 10, 1.2); add(G3, "Boulder_C", 4.8, 12.6, 50, 1.2)
for phi, pf, yw in [(72, "Pillar", 0), (108, "Pillar", 0), (240, "Pillar_Broken", 30), (300, "Pillar", 0)]:
    rr = 11.0 if phi < 180 else 12.0
    add(G3, pf, rr * math.cos(math.radians(phi)), rr * math.sin(math.radians(phi)), yw, 1, "g", -0.05)
add(G3, "SM_WeepingStatue", 6.8, -17, -12); add(G3, "SM_WeepingStatue_Broken", -6.8, -17, 18)
add(G3, "SM_StatueHead_Fallen", -4.6, -14.2, 40, 1, "g", 0.05, 0, 22)
for phi in (45, 135, 225, 315):
    add(G3, "Candles", 3.4 * math.cos(math.radians(phi)), 3.4 * math.sin(math.radians(phi)), phi, 1, "a", -0.1)
add(G3, "Bones_Pile", -4.6, 4.8, 70, 1, "a", -0.1)

# 4 - Sanctuary Gate: sealed double gate cut into the cliff, the Cinder Tower beacon rises behind it
add(G4, "SM_SanctuaryGate", 0, -24, 180, 1, "a", 0.0)
add(G4, "SM_SanctuaryGate_Leaf", -3.64, -24, 180, 1, "a", 0.0)
add(G4, "SM_SanctuaryGate_Leaf", 3.64, -24, 0, 1, "a", 0.0)
add(G4, "Brazier", -8.5, -20.5); add(G4, "Brazier", 8.5, -20.5)
add(G4, "SM_Cliff_A", -15.8, -27.5, 172, 1, "a", -1.0)
add(G4, "SM_Cliff_A", 15.8, -27.5, 188, -1, "a", -1.0)
add(G4, "SM_Cliff_Spire", -21, -33, 20, 1.1, "m", -1.0)
add(G4, "SM_Bridge_Pier", 0, -46, 0, 1, "g", 25.0)          # tower: pier base on the hilltop, top 19 m up
add(G4, "Brazier", 0, -46, 0, 2.2, "g", 19.0)

# 5 - Boundary rock silhouettes around the bowl
add(G5, "SM_Cliff_B", 24.5, -3, 93, 1.0, "a", -0.6)
add(G5, "SM_Cliff_B", -23, -13, 62, -1.0, "a", -0.6)
add(G5, "SM_Cliff_Low", -19.8, 15, 270, -1.0, "a", -0.3)
add(G5, "Wall_Ruin_Corner", -22.5, 4.0, 90, 1, "g", -0.1)

# ----------------------------------------------------------------------------- helpers
def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)

class Noise:
    def __init__(self, seed, cell):
        rng = np.random.default_rng(seed)
        self.cell = cell
        self.x0, self.z0 = NOISE_OX - 4 * cell, NOISE_OZ - 4 * cell
        n = int(NOISE_S / cell) + 10
        self.g = gaussian_filter(rng.standard_normal((n, n)), 0.6) * 1.6
    def __call__(self, x, z):
        cx = (np.asarray(x) - self.x0) / self.cell
        cz = (np.asarray(z) - self.z0) / self.cell
        return map_coordinates(self.g, [cz.ravel(), cx.ravel()], order=3, mode="nearest").reshape(np.shape(cx))

def fbm(x, z, seed, cell, octaves=4):
    s, a, tot = 0.0, 1.0, 0.0
    for i in range(octaves):
        s = s + a * Noise(seed + i * 17, cell / (2 ** i))(x, z)
        tot += a; a *= 0.5
    return s / tot

def d_circle(X, Z, c, r):
    return np.hypot(X - c[0], Z - c[1]) - r

def d_rbox(X, Z, c, half, rad):
    qx = np.abs(X - c[0]) - (half[0] - rad)
    qz = np.abs(Z - c[1]) - (half[1] - rad)
    return np.hypot(np.maximum(qx, 0), np.maximum(qz, 0)) + np.minimum(np.maximum(qx, qz), 0) - rad

def seg_dist(X, Z, a, b):
    ax, az = a; bx, bz = b
    vx, vz = bx - ax, bz - az
    L2 = vx * vx + vz * vz
    t = np.clip(((X - ax) * vx + (Z - az) * vz) / L2, 0, 1)
    return np.hypot(X - (ax + t * vx), Z - (az + t * vz)), t

# ----------------------------------------------------------------------------- height field
def height_field(X, Z):
    prims = [
        (d_circle(X, Z, ARENA["c"], ARENA["r"]), ARENA["h"]),
        (d_rbox(X, Z, COURT["c"], COURT["half"], COURT["rad"]), 0.0),
        (d_rbox(X, Z, ENTRY["c"], ENTRY["half"], ENTRY["rad"]), 0.0),
        (seg_dist(X, Z, GATE_APPROACH["a"], GATE_APPROACH["b"])[0] - GATE_APPROACH["r"], 0.0),
        (d_rbox(X, Z, GATE_COURT["c"], GATE_COURT["half"], GATE_COURT["rad"]), 0.0),
    ]
    dmin = np.minimum.reduce([p[0] for p in prims])
    wsum = 0; hsum = 0
    for d, h in prims:
        w = np.exp(-np.clip(d - dmin, 0, 40) / 2.5)
        wsum = wsum + w; hsum = hsum + w * h
    target = hsum / wsum
    d = np.maximum(dmin, 0.0)

    n1 = fbm(X, Z, 11, 90.0, 3); n2 = fbm(X, Z, 23, 38.0, 3); n3 = fbm(X, Z, 31, 60.0, 2)
    ridged = 1.0 - np.abs(n2)
    R = 10.0 + 7.0 * n1 + 6.0 * ridged
    south = smoothstep(-12.0, -26.0, Z) * smoothstep(60.0, 25.0, np.abs(X))
    R = R + 16.0 * south
    R = np.maximum(R, 5.0)
    k = np.clip(10.0 + 6.0 * n3, 5.0, 18.0) * (1.0 - 0.45 * south)
    h = target + R * (1.0 - np.exp(-d / k))

    # outer ring closes the bowl and hides the terrain edges
    cd = np.hypot(X / 1.0, (Z - 2.0) / 1.08) + 5.0 * n2
    h = h + (36.0 + 10.0 * n1) * smoothstep(36.0, 62.0, cd)

    # Cinder Tower hill behind the gate (at least TOWER h, smooth top)
    tm = smoothstep(15.0, 7.0, np.hypot(X - TOWER["c"][0], Z - TOWER["c"][1]))
    top = max(TOWER["h"], 0.0)
    h = h * (1 - tm) + np.maximum(h * 0 + np.mean([top]), np.minimum(h, top + 12.0)) * tm

    # micro relief on walkable ground (kept flat where architecture sits)
    inside = smoothstep(2.0, -5.0, dmin)
    flat = np.maximum.reduce([
        smoothstep(19.5, 17.0, np.hypot(X, Z)),
        smoothstep(4.5, 2.5, np.abs(X)) * smoothstep(5.0, 8.0, Z) * smoothstep(38.0, 35.0, Z),
        smoothstep(8.0, 6.5, np.abs(X)) * smoothstep(-31.0, -28.0, Z) * smoothstep(-4.0, -7.0, Z),
        smoothstep(13.0, 11.0, np.abs(X)) * smoothstep(19.0, 21.0, Z) * smoothstep(28.0, 26.0, Z),
    ])
    h = h + 0.35 * fbm(X, Z, 81, 9.0, 2) * inside * (1 - flat)
    return np.clip(h, OY + 0.5, OY + SY - 0.5), dmin

hx = OX + np.arange(HRES) * (SX / (HRES - 1))
hz = OZ + np.arange(HRES) * (SZ / (HRES - 1))
HX, HZ = np.meshgrid(hx, hz)
H, DMIN = height_field(HX, HZ)
H = gaussian_filter(H, 0.6)
def pin(mask, value):
    global H
    H = np.where(mask, value, H)
pin(np.hypot(HX, HZ) < 17.0, ARENA["h"])
pin((np.abs(HX) < 3.2) & (HZ > 9) & (HZ < 36.5), 0.0)
pin((np.abs(HX) < 6.6) & (HZ > -30) & (HZ < -12), 0.0)
pin((np.abs(HX) < 11.5) & (HZ > 20.5) & (HZ < 27.5), 0.0)
(np.clip((H - OY) / SY, 0, 1) * 65535).astype("<u2").tofile(os.path.join(OUT, "heights.r16"))

# ----------------------------------------------------------------------------- splat
ax_ = OX + (np.arange(ARES) + 0.5) * (SX / ARES)
az_ = OZ + (np.arange(ARES) + 0.5) * (SZ / ARES)
AX, AZ = np.meshgrid(ax_, az_)
HA = 0.25 * (H[:-1, :-1] + H[1:, :-1] + H[:-1, 1:] + H[1:, 1:])
gz, gx = np.gradient(HA, SZ / ARES, SX / ARES)
slope = np.degrees(np.arctan(np.hypot(gx, gz)))
lines = [((0, 37.5), (0, 8.5)), ((0, -9.8), (0, -23.5))]
pd = np.full(AX.shape, 1e9)
for a, b in lines:
    pd = np.minimum(pd, seg_dist(AX, AZ, a, b)[0])
rA = np.hypot(AX, AZ)
nA = fbm(AX, AZ, 101, 6.0, 2); nB = fbm(AX, AZ, 103, 9.0, 2); nC = fbm(AX, AZ, 107, 14.0, 3)
nD = fbm(AX, AZ, 109, 11.0, 3); nE = fbm(AX, AZ, 113, 18.0, 3); nF = fbm(AX, AZ, 127, 5.0, 2)

W = np.zeros(AX.shape + (6,), np.float32); W[..., 0] = 1.0
def over(layer, a):
    global W
    a = np.clip(a, 0, 1)[..., None]
    one = np.zeros(6, np.float32); one[layer] = 1
    W = W * (1 - a) + one * a
over(1, smoothstep(0.25, 0.65, nE) * 0.85)                                     # dirt patches
over(5, smoothstep(0.2, 0.6, nD) * 0.45)                                        # dark soil
over(4, smoothstep(0.1, 0.5, nC) * smoothstep(1.5, 6.0, HA) * 0.65)            # moss up the slopes
over(1, smoothstep(18.0, 28.0, slope) * 0.75)                                   # scree at the foot of slopes
hi = smoothstep(12.0, 30.0, HA)
over(4, hi * smoothstep(0.0, 0.5, nC) * 0.6)
over(3, smoothstep(31.0, 43.0, slope + 6 * nF) + hi * smoothstep(0.2, 0.7, nE) * 0.6)   # rock on cliffs
over(1, smoothstep(13.5, 12.0, rA) * smoothstep(9.0, 10.2, rA) * 0.75)        # worn ring round the arena floor
over(1, smoothstep(3.9, 2.0, pd + 0.9 * nB) * (slope < 30))                     # path fringe
over(2, smoothstep(2.3, 1.1, pd + 0.7 * nA) * (slope < 30))                     # gravel path
over(1, smoothstep(6.5, 4.0, np.hypot(AX - SPAWN["pos"][0], AZ - SPAWN["pos"][1])) * 0.5)
W = W / W.sum(-1, keepdims=True)
(np.clip(W * 255.0 + 0.5, 0, 255)).astype(np.uint8).tofile(os.path.join(OUT, "splat.bytes"))

# ----------------------------------------------------------------------------- scatter
def H_at(x, z):
    i = (np.asarray(x) - OX) / (SX / (HRES - 1)); j = (np.asarray(z) - OZ) / (SZ / (HRES - 1))
    return map_coordinates(H, [np.atleast_1d(j), np.atleast_1d(i)], order=1)
def S_at(x, z):
    i = (np.asarray(x) - OX) / (SX / ARES) - 0.5; j = (np.asarray(z) - OZ) / (SZ / ARES) - 0.5
    return map_coordinates(slope, [np.atleast_1d(j), np.atleast_1d(i)], order=1)
def D_at(x, z):
    i = (np.asarray(x) - OX) / (SX / (HRES - 1)); j = (np.asarray(z) - OZ) / (SZ / (HRES - 1))
    return map_coordinates(DMIN, [np.atleast_1d(j), np.atleast_1d(i)], order=1)
def PD_at(x, z):
    best = np.full(np.shape(x), 1e9)
    for a, b in lines:
        best = np.minimum(best, seg_dist(np.asarray(x), np.asarray(z), a, b)[0])
    return best

FOOT = {"SM_Cliff_B": 13.5, "SM_Cliff_A": 10.5, "SM_Cliff_Low": 7.5, "SM_Cliff_Spire": 4.5, "SM_Bridge_Pier": 4.0,
        "SM_Tollgate": 6.5, "SM_SanctuaryGate": 7.0, "SM_RuinWall_Straight": 2.3, "SM_RuinWall_Broken": 2.3,
        "SM_RuinWall_Low": 2.2, "Tree_Dead": 1.2, "Wall_Ruin_Corner": 1.8}
rng = np.random.default_rng(7)
cx, cz = np.meshgrid(np.arange(-40, 41, 4.0), np.arange(-40, 46, 4.0))
cx = cx.ravel() + rng.uniform(-1.8, 1.8, cx.size); cz = cz.ravel() + rng.uniform(-1.8, 1.8, cz.size)
h_, s_, d_, p_ = H_at(cx, cz), S_at(cx, cz), D_at(cx, cz), PD_at(cx, cz)
cl = fbm(cx, cz, 131, 16.0, 2)
hand = np.array([(p[2], p[3], FOOT.get(p[1], 0.0) * abs(p[5])) for p in P])
near = np.min(np.hypot(cx[:, None] - hand[None, :, 0], cz[:, None] - hand[None, :, 1]) - hand[None, :, 2], axis=1)
rr_ = np.hypot(cx, cz)
base_ok = (near > 3.2) & (p_ > 3.6) & (rr_ > 18.5) & ~((np.abs(cx) < 15) & (cz > 7) & (cz < 40)) \
          & ~((np.abs(cx) < 10) & (cz < -5) & (cz > -32))
bush_ok = base_ok & (s_ < 24) & (h_ < 14) & (d_ > -3.5) & (d_ < 11) & (cl > -0.15)
rock_ok = base_ok & ~bush_ok & (s_ < 38) & (d_ > -1.0) & (d_ < 16) & (cl < 0.35)
bi = np.flatnonzero(bush_ok); ri = np.flatnonzero(rock_ok)
rng.shuffle(bi); rng.shuffle(ri)
for i in bi[:34]:
    pf = rng.choice(["Bush_Small", "Bush_Medium", "Bush_Large"], p=[0.45, 0.35, 0.2])
    add("06_Scatter", pf, cx[i], cz[i], rng.uniform(0, 360), rng.uniform(0.8, 1.3), "g", -0.1)
for i in ri[:26]:
    pf = rng.choice(["Rock_A", "Rock_B", "Rock_C", "Boulder_A", "Boulder_B", "Boulder_C"], p=[0.22, 0.2, 0.22, 0.14, 0.1, 0.12])
    sc = rng.uniform(0.7, 1.6)
    add("06_Scatter", pf, cx[i], cz[i], rng.uniform(0, 360), sc, "g", -0.15 * sc, rng.uniform(-8, 8), rng.uniform(-8, 8))

with open(os.path.join(OUT, "props.csv"), "w") as f:
    f.write("group;prefab;x;z;yaw;scale;ymode;y;rx;rz\n")
    for p in P:
        f.write(";".join([p[0], p[1]] + [f"{v:.3f}" for v in p[2:6]] + [p[6]] + [f"{v:.3f}" for v in p[7:]]) + "\n")
with open(os.path.join(OUT, "layout.json"), "w") as f:
    json.dump(dict(terrain=dict(origin=[OX, OY, OZ], size=[SX, SY, SZ], heightmap=HRES, alphamap=ARES,
                                layers=["Grass007", "Ground037", "Gravel043", "Rock064", "Moss002", "Ground068"]),
                   spawn=SPAWN, zones=[dict(id=z[0], name=z[1], at=z[2]) for z in ZONES],
                   tiers=TIER_ANGLES, prop_count=len(P)), f, indent=1)

# ----------------------------------------------------------------------------- grass detail
grass_w = W[..., 0] + 0.6 * W[..., 4]
gd = smoothstep(0.55, 0.85, grass_w) * smoothstep(26.0, 14.0, slope) * smoothstep(2.6, 4.5, pd)
gd = gd * smoothstep(19.5, 21.5, rA) * (0.55 + 0.45 * smoothstep(-0.4, 0.5, fbm(AX, AZ, 171, 7.0, 2)))
gd = gd * ~((np.abs(AX) < 13) & (AZ > 20.0) & (AZ < 28.0))
np.clip(np.round(gd * 4.0), 0, 4).astype(np.uint8).tofile(os.path.join(OUT, "grass.bytes"))

# ----------------------------------------------------------------------------- guide map image
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource
from matplotlib.patches import Circle, FancyArrowPatch, Rectangle

x0, x1, z0, z1 = -46, 46, -56, 46
i0, i1 = int((x0 - OX) / 0.5), int((x1 - OX) / 0.5)
j0, j1 = int((z0 - OZ) / 0.5), int((z1 - OZ) / 0.5)
Hc = H[j0:j1, i0:i1]; Wc = W[j0:j1, i0:i1]
pal = np.array([[0.42, 0.47, 0.27], [0.45, 0.36, 0.26], [0.66, 0.62, 0.55], [0.40, 0.40, 0.42], [0.30, 0.40, 0.22], [0.24, 0.20, 0.17]])
rgb = np.tensordot(Wc, pal, axes=([2], [0]))
shade = LightSource(azdeg=315, altdeg=38).hillshade(Hc, vert_exag=1.4, dx=0.5, dy=0.5)
img = np.clip(rgb * (0.35 + 0.85 * shade[..., None]), 0, 1)

fig, axm = plt.subplots(figsize=(10, 11.5), dpi=110)
fig.patch.set_facecolor("#1b1714")
axm.imshow(img, origin="lower", extent=[x0, x1, z0, z1])
axm.contour(np.linspace(x0, x1, Hc.shape[1]), np.linspace(z0, z1, Hc.shape[0]), Hc, levels=np.arange(-2, 80, 3),
            colors="k", linewidths=0.25, alpha=0.35)
fx, fz = zip(*[(0, 34.5), (0, 24), (0, 10), (0, -9), (0, -21)])
axm.plot(fx, fz, color="#ffdf9e", lw=2.2, ls=(0, (4, 3)), alpha=0.95)
axm.add_patch(FancyArrowPatch((0, -12), (0, -21), arrowstyle="-|>", mutation_scale=18, color="#ffdf9e"))
axm.add_patch(Circle((0, 0), 9.65, fill=False, ec="#f2c27a", lw=1.4))
for phi in TIER_ANGLES:
    th = np.radians(np.linspace(phi - 15, phi + 15, 20))
    axm.plot(np.r_[9.5 * np.cos(th), 15.9 * np.cos(th[::-1]), 9.5 * np.cos(th[0])],
             np.r_[9.5 * np.sin(th), 15.9 * np.sin(th[::-1]), 9.5 * np.sin(th[0])], color="#e6c9a0", lw=1.1)
axm.add_patch(Rectangle((-6.5, 22.3), 13, 3.5, fc="#9c8f7a", ec="#2b2118", lw=0.8))
axm.add_patch(Rectangle((-6.9, -27.2), 13.8, 6.4, fc="#c9b26b", ec="#2b2118", lw=0.8))
for xw in (-8.7, -13.1, -17.4, 8.7, 13.1, 17.4):
    axm.add_patch(Rectangle((xw - 2.2, 23.6), 4.4, 1.6, fc="#7c7266", ec="none"))
marks = [((0, 34.5), "S", "#b58cff"), ((0, 0), "*", "#ff9a3c"), ((0, -46), "^", "#ff7a2a"), ((-21, -33), "^", "#bbbbbb")]
for (mx, mz), sym, col in marks:
    if sym == "S":
        axm.text(mx, mz, sym, color="k", ha="center", va="center", fontsize=8, fontweight="bold",
                 bbox=dict(boxstyle="circle,pad=0.25", fc=col, ec="k", lw=0.6))
    else:
        axm.plot(mx, mz, marker=sym, ms=9, color=col, mec="k", mew=0.6)
for bx, bz in [(-4.2, 21.2), (4.2, 21.2), (-3.4, 30.2), (3.4, 30.2), (-8.5, -20.5), (8.5, -20.5)]:
    axm.plot(bx, bz, marker="o", ms=5, color="#ffb04a", mec="k", mew=0.6)
offs = {1: (17, 6), 2: (24, -3), 3: (24, -6), 4: (19, -6)}
for zid, name, (lx, lz), col in ZONES:
    axm.annotate(f"{zid}  {name}", (lx, lz), (lx + offs[zid][0], lz + offs[zid][1]), color="white", fontsize=10,
                 fontweight="bold", ha="center", bbox=dict(boxstyle="round,pad=0.3", fc=col, ec="k", lw=0.6, alpha=0.92),
                 arrowprops=dict(arrowstyle="-", color="white", lw=0.8, alpha=0.7))
axm.text(0, -50, "Cinder Tower", color="#ffd9b0", fontsize=8, ha="center", style="italic")
axm.add_patch(FancyArrowPatch((38, 30), (38, 40), arrowstyle="-|>", mutation_scale=16, color="white"))
axm.text(38, 41.5, "N", color="white", ha="center", fontsize=11, fontweight="bold")
axm.plot([20, 40], [-52, -52], color="white", lw=3); axm.text(30, -50.5, "20 m", color="white", ha="center", fontsize=9)
axm.set_xlim(x0, x1); axm.set_ylim(z0, z1)
axm.set_xlabel("world X (m)", color="#ccc"); axm.set_ylabel("world Z (m)", color="#ccc")
axm.tick_params(colors="#aaa", labelsize=7)
axm.set_title("Cinder Sanctuary - starting map guide map", color="#f3e3c3", fontsize=15, fontweight="bold", pad=12)
fig.text(0.5, 0.02, "Spawn in the entry nook (1) -> through the tollgate arch (2) -> reveal of the amphitheater + shrine (3)\n"
                    "-> sealed Sanctuary Gate (4) with the Cinder Tower beacon above it. Hills and cliffs close the bowl on every side.",
         color="#e8dcc4", ha="center", fontsize=9)
plt.tight_layout(rect=(0, 0.05, 1, 1))
plt.savefig(os.path.join(OUT, "GuideMap.png"), facecolor=fig.get_facecolor())
print("props", len(P), "height range", float(H.min()), float(H.max()))
