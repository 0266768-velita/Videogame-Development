"""Builds the level: tile mask -> ground.png, water overlays, level.json (colliders, props, enemies)."""
from PIL import Image
import numpy as np, random, math, json

W, H = 80, 50; OX, OY = -40, -25          # world cell (x,y) -> grid ix=x-OX, iy=y-OY
GRASS, WATER, PIT, SOLID, PATH, FLOOR, VOID = range(7)
rnd = random.Random(11)

def build_mask():
    m = np.full((H, W), GRASS, dtype=np.uint8)
    def wc(x, y): return int(math.floor(x)) - OX, int(math.floor(y)) - OY
    def setk(x, y, k):
        ix, iy = wc(x, y)
        if 0 <= ix < W and 0 <= iy < H: m[iy, ix] = k
    # border cliffs
    m[0:2, :] = SOLID; m[-2:, :] = SOLID; m[:, 0:2] = SOLID; m[:, -2:] = SOLID
    # River 1 (horizontal, y ~ -12), width 2
    for ix in range(W):
        x = ix + OX + .5; cy = -12 + 1.4 * math.sin(x / 6 + .5)
        for iy in range(H):
            y = iy + OY + .5
            if abs(y - cy) < 1.0 and m[iy, ix] == GRASS: m[iy, ix] = WATER
    # River 2 (vertical, x ~ 14) from y=-12 to y=4
    for iy in range(H):
        y = iy + OY + .5
        if -12 <= y <= 4.5:
            xc = 14 + 1.5 * math.sin(y / 5)
            for ix in range(W):
                x = ix + OX + .5
                if abs(x - xc) < 1.0 and m[iy, ix] == GRASS: m[iy, ix] = WATER
    # Lake west
    for iy in range(H):
        for ix in range(W):
            x, y = ix + OX + .5, iy + OY + .5
            d = ((x + 21) / 8.5) ** 2 + ((y + 1) / 5.2) ** 2
            d += 0.12 * math.sin(x * .9) + 0.1 * math.cos(y * 1.3)
            if d < 1.0 and m[iy, ix] == GRASS: m[iy, ix] = WATER
    # small pond east
    for iy in range(H):
        for ix in range(W):
            x, y = ix + OX + .5, iy + OY + .5
            if ((x - 30) / 3.2) ** 2 + ((y + 14) / 2.4) ** 2 < 1.0 and m[iy, ix] == GRASS: m[iy, ix] = WATER
    # Ravine (pits), 2 cells wide, full width: the gate to the pyramid
    for ix in range(2, W - 2):
        x = ix + OX + .5; off = int(round(0.8 * math.sin(x / 5)))
        for dy in (5, 6):
            iy = dy - OY + off
            if m[iy, ix] in (GRASS, WATER): m[iy, ix] = PIT
    # a few isolated holes on the paths
    for (hx, hy) in ((-6, -4), (7, -6), (-2, -17), (22, 0), (-30, 1)):
        for dx in (0, 1):
            for dy in (0, 1):
                ix, iy = hx + dx - OX, hy + dy - OY
                if m[iy, ix] == GRASS: m[iy, ix] = PIT
    # Dense rock band north of the pyramid approach (y >= 11) except the pyramid itself
    for iy in range(H):
        y = iy + OY
        if y >= 11:
            for ix in range(W):
                if m[iy, ix] == GRASS: m[iy, ix] = SOLID
    # carve the doorway through the cliff band (the door object closes it)
    for y in (11, 12):
        for x in (-1, 0):
            m[y - OY, x - OX] = FLOOR
    # Boss room: x -37..-13, y 14..23 floor, walls around
    rx0, rx1, ry0, ry1 = -37, -13, 14, 23
    for iy in range(H):
        for ix in range(W):
            x, y = ix + OX, iy + OY
            if -40 <= x <= -10 and 11 <= y <= 24:
                m[iy, ix] = VOID
    for y in range(ry0 - 1, ry1 + 2):
        for x in range(rx0 - 1, rx1 + 2):
            ix, iy = x - OX, y - OY
            m[iy, ix] = SOLID if (x in (rx0 - 1, rx1 + 1) or y in (ry0 - 1, ry1 + 1)) else FLOOR
    # pyramid footprint and the paved approach
    for y in range(7, 11):
        for x in range(-4, 5):
            ix, iy = x - OX, y - OY
            if m[iy, ix] == GRASS: m[iy, ix] = FLOOR
    # dirt path from the start up to the pyramid (decorative; crossing the water still needs jumps)
    px, py = 0.0, -22.0
    while py < 7:
        px += math.sin(py / 4.0) * 0.45 - px * 0.02; py += 0.5
        for dx in (-1, 0, 1):
            ix, iy = int(math.floor(px + dx)) - OX, int(math.floor(py)) - OY
            if 0 <= ix < W and 0 <= iy < H and m[iy, ix] == GRASS: m[iy, ix] = PATH
    # start clearing is grass; mark pyramid body as solid (collider later)
    return m

def rects_from_mask(m, kinds):
    """Merge cells of the given kinds into rectangles (world coords, lower-left x,y,w,h)."""
    used = np.zeros_like(m, dtype=bool); out = []
    for iy in range(H):
        ix = 0
        while ix < W:
            if m[iy, ix] in kinds and not used[iy, ix]:
                x1 = ix
                while x1 + 1 < W and m[iy, x1 + 1] in kinds and not used[iy, x1 + 1]: x1 += 1
                y1 = iy
                while y1 + 1 < H and all(m[y1 + 1, x] in kinds and not used[y1 + 1, x] for x in range(ix, x1 + 1)): y1 += 1
                used[iy:y1 + 1, ix:x1 + 1] = True
                out.append({"x": ix + OX, "y": iy + OY, "w": x1 - ix + 1, "h": y1 - iy + 1})
                ix = x1 + 1
            else: ix += 1
    return out

# --------------------------------------------------------------------------- painting
def noise_cell(base, var, r):
    return tuple(max(0, min(255, base[i] + r.randint(-var, var))) for i in range(3))

def paint(m):
    img = Image.new('RGBA', (W * 16, H * 16)); px = img.load(); r = random.Random(5)
    pal = {
        GRASS: [(30, 48, 36), (26, 42, 32), (34, 54, 40)], PATH: [(78, 60, 48), (70, 54, 44), (84, 66, 52)],
        WATER: [(22, 56, 96), (26, 64, 108), (18, 48, 86)], PIT: [(8, 6, 14), (6, 4, 10), (10, 8, 18)],
        SOLID: [(44, 40, 54), (50, 46, 62), (38, 34, 48)], FLOOR: [(70, 64, 84), (62, 56, 76), (76, 70, 90)], VOID: [(5, 5, 9)] * 3,
    }
    def kind(ix, iy): return m[iy, ix] if 0 <= ix < W and 0 <= iy < H else SOLID
    for iy in range(H):
        for ix in range(W):
            k = m[iy, ix]; base = r.choice(pal[k])
            for py_ in range(16):
                for px_ in range(16):
                    c = noise_cell(base, 3 if k != VOID else 0, r)
                    px[ix * 16 + px_, (H - 1 - iy) * 16 + py_] = c + (255,)
            X0, Y0 = ix * 16, (H - 1 - iy) * 16
            if k == GRASS:
                for _ in range(5):
                    gx, gy = X0 + r.randint(1, 14), Y0 + r.randint(2, 14)
                    for t in range(r.randint(2, 3)): px[gx, gy - t] = (58 + r.randint(0, 20), 98 + r.randint(0, 20), 62, 255)
                if r.random() < .05:   # glowing flower
                    fx, fy = X0 + r.randint(2, 13), Y0 + r.randint(2, 13); px[fx, fy] = (120, 220, 230, 255)
            if k in (FLOOR, SOLID) and r.random() < .5:
                for t in range(r.randint(4, 9)): px[X0 + r.randint(0, 15), Y0 + r.randint(0, 15)] = (30, 26, 40, 255)
            if k == FLOOR and (ix + iy) % 2 == 0:    # checker slabs
                for t in range(16): px[X0 + t, Y0] = (88, 82, 104, 255); px[X0, Y0 + t] = (88, 82, 104, 255)
            if k == PATH:
                for _ in range(5): px[X0 + r.randint(0, 15), Y0 + r.randint(0, 15)] = (100, 80, 62, 255)
    # edges: shore mud/foam around water, rim around pits, cliff edges
    img2 = img.copy(); p2 = img2.load()
    for iy in range(H):
        for ix in range(W):
            k = m[iy, ix]; X0, Y0 = ix * 16, (H - 1 - iy) * 16
            for (dx, dy, edge) in ((0, 1, 'N'), (0, -1, 'S'), (-1, 0, 'W'), (1, 0, 'E')):
                nk = kind(ix + dx, iy + dy)
                if k == WATER and nk not in (WATER,):
                    for t in range(16):
                        for d in range(3):
                            sx, sy = {'N': (t, d), 'S': (t, 15 - d), 'W': (d, t), 'E': (15 - d, t)}[edge]
                            p2[X0 + sx, Y0 + (15 - sy if edge in ('N', 'S') and False else sy)] = (150, 200, 220, 255) if d == 0 else (60, 110, 150, 255)
                if k not in (WATER, PIT, SOLID, VOID, FLOOR) and nk == WATER:
                    for t in range(16):
                        for d in range(2):
                            sx, sy = {'N': (t, d), 'S': (t, 15 - d), 'W': (d, t), 'E': (15 - d, t)}[edge]
                            p2[X0 + sx, Y0 + sy] = (54, 44, 40, 255)
                if k == PIT and nk not in (PIT, VOID):
                    for t in range(16):
                        for d in range(3):
                            sx, sy = {'N': (t, d), 'S': (t, 15 - d), 'W': (d, t), 'E': (15 - d, t)}[edge]
                            p2[X0 + sx, Y0 + sy] = (30, 22, 32, 255) if d < 2 else (16, 12, 20, 255)
                if k not in (PIT, SOLID, VOID, WATER) and nk == PIT:
                    for t in range(16):
                        sx, sy = {'N': (t, 0), 'S': (t, 15), 'W': (0, t), 'E': (15, t)}[edge]
                        p2[X0 + sx, Y0 + sy] = (92, 80, 96, 255)
                if k in (GRASS, PATH, FLOOR) and nk == SOLID:
                    for t in range(16):
                        sx, sy = {'N': (t, 0), 'S': (t, 15), 'W': (0, t), 'E': (15, t)}[edge]
                        p2[X0 + sx, Y0 + sy] = (20, 16, 26, 255)
                if k == SOLID and nk in (GRASS, PATH, FLOOR):
                    for t in range(16):
                        sx, sy = {'N': (t, 0), 'S': (t, 15), 'W': (0, t), 'E': (15, t)}[edge]
                        p2[X0 + sx, Y0 + sy] = (84, 78, 100, 255)
    return img2

def water_overlays(m):
    frames = []
    for f in range(2):
        img = Image.new('RGBA', (W * 16, H * 16), (0, 0, 0, 0)); px = img.load(); r = random.Random(100 + f)
        for iy in range(H):
            for ix in range(W):
                if m[iy, ix] == WATER:
                    X0, Y0 = ix * 16, (H - 1 - iy) * 16
                    for _ in range(3):
                        rx, ry = r.randint(1, 11), r.randint(2, 13)
                        for t in range(4): px[X0 + rx + t, Y0 + ry] = (120, 180, 220, 150)
        frames.append(img)
    return frames

# --------------------------------------------------------------------------- level data
def build_level(m):
    def free(x, y, margin=0, kinds=(GRASS,)):
        for dx in range(-margin, margin + 1):
            for dy in range(-margin, margin + 1):
                ix, iy = int(math.floor(x + dx)) - OX, int(math.floor(y + dy)) - OY
                if not (0 <= ix < W and 0 <= iy < H) or m[iy, ix] not in kinds: return False
        return True
    data = {"bounds": {"x": OX, "y": OY, "w": W, "h": H}}
    data["playerStart"] = {"x": 0.5, "y": -21.0}
    data["water"] = rects_from_mask(m, (WATER,)); data["pits"] = rects_from_mask(m, (PIT,))
    solid = rects_from_mask(m, (SOLID, VOID)); data["solid"] = solid
    spawns = [("Dog", -12, -18), ("Dog", 14, -19), ("Dog", -6, -8), ("Dog", 6, -6),
              ("Witch", -28, -5), ("Witch", -12, 3), ("Witch", 4, 2),
              ("Giant", 24, -5), ("Giant", 30, 3), ("Giant", 22, 9)]
    data["enemies"] = [{"type": t, "x": x + .5, "y": y + .5} for t, x, y in spawns]
    data["bossSpawn"] = {"x": -19.5, "y": 18.5}; data["roomEntry"] = {"x": -34.5, "y": 18.5}
    data["door"] = {"x": 0.0, "y": 12.1}; data["doorExit"] = {"x": 0.5, "y": 9.0}
    data["pyramid"] = {"x": 0.0, "y": 11.0}
    # pyramid body collider (leave the door gap)
    data["pyramidBody"] = []
    # props
    keep_out = [(0, -21, 5)] + [(s[1] + .5, s[2] + .5, 3) for s in spawns] + [(0, 8, 6), (-35, 18, 4)]
    def ok(x, y, rad):
        return all(math.hypot(x - kx, y - ky) > max(kr, rad) for kx, ky, kr in keep_out)
    trees, bushes, rocks, mush, skulls = [], [], [], [], []
    r = random.Random(21); tries = 0
    while len(trees) < 120 and tries < 6000:
        tries += 1; x, y = r.uniform(-38, 38), r.uniform(-24, 24)
        if y > 10.5 and not (-40 <= x <= -9): pass
        if free(x, y, 1) and ok(x, y, 2.2) and not (-4.5 < x < 4.5 and -22 < y < 8):
            trees.append({"x": round(x, 2), "y": round(y, 2), "v": r.randint(0, 2)})
    # forest above the ravine so the pyramid stands in a clearing
    for _ in range(80):
        x, y = r.uniform(-38, 38), r.uniform(7, 10.8)
        if free(x, y, 0, (GRASS, FLOOR)) and abs(x) > 6 and m[int(math.floor(y)) - OY, int(math.floor(x)) - OX] == GRASS:
            trees.append({"x": round(x, 2), "y": round(y, 2), "v": r.randint(0, 2)})
    for _ in range(260):
        x, y = r.uniform(-38, 38), r.uniform(-24, 10)
        if free(x, y, 0) and ok(x, y, 1.2): bushes.append({"x": round(x, 2), "y": round(y, 2), "v": r.randint(0, 1)})
    for _ in range(90):
        x, y = r.uniform(-38, 38), r.uniform(-24, 10)
        if free(x, y, 1) and ok(x, y, 1.8): rocks.append({"x": round(x, 2), "y": round(y, 2), "v": r.choices([0, 1, 2], [5, 3, 1])[0]})
    for _ in range(40):
        x, y = r.uniform(-38, 38), r.uniform(-24, 10)
        if free(x, y, 0): mush.append({"x": round(x, 2), "y": round(y, 2)})
    for _ in range(14):
        x, y = r.uniform(-38, 38), r.uniform(-24, 10)
        if free(x, y, 0) and ok(x, y, 2): skulls.append({"x": round(x, 2), "y": round(y, 2)})
    data["trees"], data["bushes"], data["rocks"], data["mushrooms"], data["skulls"] = trees, bushes, rocks, mush, skulls
    data["torches"] = [{"x": -3.0, "y": 10.6}, {"x": 3.0, "y": 10.6}] + \
        [{"x": float(x), "y": 23.4} for x in range(-35, -12, 5)] + [{"x": -36.4, "y": 16.0}, {"x": -36.4, "y": 21.0}, {"x": -13.6, "y": 16.0}, {"x": -13.6, "y": 21.0}]
    data["pillars"] = [{"x": float(x), "y": float(y)} for x in (-30, -24) for y in (16, 21)]
    return data

def build():
    m = build_mask()
    return m, paint(m), water_overlays(m), build_level(m)
