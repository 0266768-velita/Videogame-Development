from pa import *
import random, math

def tree(variant, seed):
    r = random.Random(seed); c = Canvas(32, 48)
    TR = (70, 46, 36, 255); TRD = (44, 28, 26, 255)
    c.rect(13, 28, 18, 44, TR); c.rect(13, 28, 14, 44, TRD); c.rect(11, 42, 20, 45, TR); c.rect(10, 44, 21, 46, TRD)   # trunk + roots
    if variant == 0:     # dark leafy
        for cx, cy, rx, ry, col in ((16, 17, 12, 9, (24, 62, 44)), (9, 22, 7, 6, (28, 72, 50)), (23, 21, 7, 6, (22, 56, 42)), (16, 11, 8, 6, (34, 84, 58)), (16, 24, 11, 6, (20, 48, 38))):
            c.ell(cx, cy, rx, ry, col + (255,))
        for _ in range(40): c.px(r.randint(5, 26), r.randint(6, 26), (52, 120, 80, 255) if r.random() < .5 else (16, 40, 30, 255))
    else:                # dead twisted
        c.line(16, 30, 8, 14, TR); c.line(16, 30, 24, 12, TR); c.line(16, 24, 16, 4, TR); c.line(9, 17, 3, 12, TRD); c.line(23, 16, 29, 9, TRD); c.line(16, 12, 11, 6, TRD); c.line(16, 9, 21, 4, TRD)
        c.line(15, 30, 7, 14, TR); c.line(17, 30, 25, 12, TR)
        for _ in range(10): c.px(r.randint(3, 29), r.randint(4, 20), (90, 40, 110, 255))
    return finish(c)

def bush(seed):
    r = random.Random(seed); c = Canvas(16, 12)
    for cx, cy, rx, ry, col in ((8, 7, 7, 4, (22, 58, 40)), (4, 7, 4, 3, (28, 70, 48)), (12, 7, 4, 3, (26, 64, 44)), (8, 5, 5, 3, (36, 88, 58))):
        c.ell(cx, cy, rx, ry, col + (255,))
    for _ in range(14): c.px(r.randint(2, 13), r.randint(3, 9), (60, 130, 86, 255))
    for _ in range(3): c.px(r.randint(3, 12), r.randint(4, 8), (200, 50, 70, 255))
    return finish(c)

def rock(size, seed):
    r = random.Random(seed); w, h = {0: (12, 9), 1: (18, 13), 2: (28, 20)}[size]
    c = Canvas(w, h); base = (84, 80, 98, 255)
    c.ell(w / 2, h / 2 + 1, w / 2 - .5, h / 2 - 1, base)
    c.ell(w / 2 - 1, h / 2 - 1, w / 3, h / 3, (118, 114, 134, 255))
    c.rect(1, h - 3, w - 2, h - 2, (52, 48, 66, 255))
    for _ in range(w): c.px(r.randint(2, w - 3), r.randint(2, h - 3), (60, 56, 74, 255))
    return finish(c)

def torch(frame):
    c = Canvas(8, 18)
    c.rect(3, 8, 4, 16, (96, 64, 40, 255)); c.rect(2, 7, 5, 8, (50, 40, 40, 255))
    f = [(255, 210, 80, 255), (255, 140, 40, 255), (220, 60, 30, 255)]
    off = frame % 2
    c.ell(3.5, 4, 2.2 + off * .4, 3.6, f[2]); c.ell(3.5, 4.6, 1.5, 2.6, f[1]); c.ell(3.5, 5.2, 0.8, 1.6, f[0])
    c.px(3 + off, 0, f[1])
    return finish(c)

def glow(size=64, col=(255, 150, 60)):
    c = Canvas(size, size); cx = size / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x + .5 - cx, y + .5 - cx) / cx
            a = max(0, 1 - d) ** 2
            c.px(x, y, col + (int(a * 150),))
    return c

def skull_pile():
    c = Canvas(16, 10)
    for x, y in ((4, 6), (10, 6), (7, 3)):
        c.ell(x, y, 3, 2.6, BONE := (226, 216, 190, 255)); c.px(x - 1, y, (40, 30, 40, 255)); c.px(x + 1, y, (40, 30, 40, 255))
    c.line(1, 8, 14, 8, (180, 170, 150, 255)); c.line(3, 9, 12, 7, (200, 190, 170, 255))
    return finish(c)

def mushroom(seed):
    c = Canvas(8, 8)
    c.rect(3, 4, 4, 6, (200, 220, 220, 255)); c.ell(3.5, 3, 3, 2, (60, 200, 220, 255)); c.px(2, 2, (200, 255, 255, 255)); c.px(5, 3, (200, 255, 255, 255))
    return finish(c)

def key():
    c = Canvas(12, 12)
    c.ell(3.5, 3.5, 3, 3, (244, 196, 64, 255)); c.ell(3.5, 3.5, 1.2, 1.2, (0, 0, 0, 0))
    c.px(3, 3, (0, 0, 0, 0)); c.px(4, 3, (0, 0, 0, 0)); c.px(3, 4, (0, 0, 0, 0)); c.px(4, 4, (0, 0, 0, 0))
    c.rect(5, 5, 6, 10, (244, 196, 64, 255)); c.rect(7, 8, 8, 8, (244, 196, 64, 255)); c.rect(7, 10, 8, 10, (244, 196, 64, 255))
    c.px(2, 1, (255, 250, 200, 255)); c.px(6, 6, (30, 30, 50, 255))
    return finish(c)

def pyramid():
    W, H = 320, 208; c = Canvas(W, H)
    STONE = [(66, 60, 78), (78, 72, 92), (58, 52, 70)]; TRIM = (112, 104, 130); DARKL = (34, 30, 44)
    steps = 6; base_h = 30
    # stepped body (front view)
    for i in range(steps):
        w = W - i * 44; x0 = (W - w) // 2
        y1 = H - 1 - i * 26; y0 = y1 - 26 + 1
        c.rect(x0, y0, x0 + w - 1, y1, STONE[i % 3] + (255,))
        c.rect(x0, y0, x0 + w - 1, y0 + 2, TRIM + (255,))               # lit top lip
        c.rect(x0, y1 - 3, x0 + w - 1, y1, DARKL + (255,))              # shadow under step
        for gx in range(x0 + 6, x0 + w - 6, 14):                        # carved glyph blocks
            c.rect(gx, y0 + 9, gx + 5, y0 + 15, shade(STONE[i % 3] + (255,), 0.7))
            c.px(gx + 2, y0 + 12, TRIM + (255,))
    # temple on top
    tx = W // 2 - 32; ty = H - steps * 26 - 30
    c.rect(tx, ty, tx + 63, ty + 29, (88, 80, 104, 255)); c.rect(tx, ty, tx + 63, ty + 3, TRIM + (255,))
    c.rect(tx + 24, ty + 10, tx + 39, ty + 29, (14, 10, 20, 255))
    c.rect(tx + 4, ty + 8, tx + 12, ty + 16, (255, 170, 60, 255)); c.rect(tx + 51, ty + 8, tx + 59, ty + 16, (255, 170, 60, 255))
    # central staircase
    sx0 = W // 2 - 20
    for y in range(ty + 30, H - 20):
        c.rect(sx0, y, sx0 + 39, y, (96, 88, 112, 255) if (y // 3) % 2 == 0 else (70, 64, 86, 255))
    # doorway recess at the bottom center (the door object sits over it)
    c.rect(W // 2 - 16, H - 36, W // 2 + 15, H - 1, (10, 8, 16, 255))
    c.rect(W // 2 - 20, H - 40, W // 2 + 19, H - 37, TRIM + (255,)); c.rect(W // 2 - 20, H - 40, W // 2 - 17, H - 1, TRIM + (255,)); c.rect(W // 2 + 16, H - 40, W // 2 + 19, H - 1, TRIM + (255,))
    return finish(c, 1)

def door(open_):
    c = Canvas(32, 36)
    if open_:
        c.rect(0, 0, 31, 35, (6, 4, 12, 255))
        for y in range(0, 36, 6): c.rect(0, y, 31, y, (20, 14, 30, 255))
    else:
        c.rect(0, 0, 31, 35, (92, 86, 108, 255)); c.rect(0, 0, 31, 3, (130, 122, 150, 255))
        c.rect(0, 0, 2, 35, (60, 54, 74, 255)); c.rect(29, 0, 31, 35, (60, 54, 74, 255))
        c.line(15, 4, 15, 34, (40, 36, 52, 255)); c.line(16, 4, 16, 34, (40, 36, 52, 255))
        c.ell(15.5, 17, 5, 5, (30, 26, 44, 255)); c.ell(15.5, 17, 3, 3, (255, 120, 60, 255)); c.ell(15.5, 17, 1.2, 1.2, (255, 230, 150, 255))   # glowing lock
        c.rect(14, 22, 17, 28, (30, 26, 44, 255))
    return c

def blood_decal(seed, size=16):
    r = random.Random(seed); c = Canvas(size, size); cx = cy = size / 2
    cols = [(110, 8, 14, 255), (140, 12, 20, 255), (86, 6, 12, 255)]
    c.ell(cx, cy, size * .22, size * .2, cols[0])
    for _ in range(int(size * .8)):
        a = r.random() * 6.283; d = r.random() ** 0.6 * size * .42
        rr = max(.6, r.random() * size * .12)
        c.ell(cx + math.cos(a) * d, cy + math.sin(a) * d * .8, rr, rr * .8, cols[r.randint(0, 2)])
    for _ in range(6): c.px(r.randint(2, size - 3), r.randint(2, size - 3), (190, 24, 30, 255))   # wet highlights
    return c

def pool(seed, w=32, h=22):
    r = random.Random(seed); c = Canvas(w, h)
    for _ in range(26):
        c.ell(w / 2 + r.uniform(-w * .22, w * .22), h / 2 + r.uniform(-h * .2, h * .2), r.uniform(w * .12, w * .26), r.uniform(h * .14, h * .28), [(96, 6, 12, 255), (122, 10, 18, 255), (80, 4, 10, 255)][r.randint(0, 2)])
    for _ in range(8): c.px(r.randint(6, w - 7), r.randint(4, h - 5), (200, 30, 36, 255))
    return c

def gib(kind, seed):
    r = random.Random(seed); c = Canvas(8, 8)
    if kind == 0:   # flesh chunk
        c.ell(4, 4, 3, 2.5, (150, 18, 28, 255)); c.ell(3, 3, 1.5, 1.2, (200, 50, 60, 255)); c.px(5, 5, (90, 8, 14, 255))
    elif kind == 1: # bone
        c.line(1, 6, 6, 1, (232, 222, 196, 255)); c.px(0, 6, (232, 222, 196, 255)); c.px(1, 7, (232, 222, 196, 255)); c.px(6, 0, (232, 222, 196, 255)); c.px(7, 1, (232, 222, 196, 255)); c.px(3, 4, (170, 160, 140, 255))
    elif kind == 2: # organ
        c.ell(4, 4, 3, 2, (180, 40, 70, 255)); c.line(1, 4, 7, 5, (130, 24, 50, 255)); c.px(3, 3, (230, 120, 130, 255))
    else:           # cloth scrap (tintable)
        c.poly([(1, 1), (6, 2), (5, 6), (2, 5)], (200, 200, 200, 255)); c.px(3, 3, (150, 150, 150, 255))
    return finish(c, 0, (20, 4, 8, 255)) if kind != 3 else c

def build_all():
    out = {}
    out['tree_a'] = tree(0, 1); out['tree_b'] = tree(1, 2); out['tree_c'] = tree(0, 3)
    out['bush_a'] = bush(1); out['bush_b'] = bush(2)
    out['rock_s'] = rock(0, 1); out['rock_m'] = rock(1, 2); out['rock_l'] = rock(2, 3)
    out['torch_0'] = torch(0); out['torch_1'] = torch(1)
    out['glow'] = glow(); out['skulls'] = skull_pile(); out['mushroom'] = mushroom(1)
    out['key'] = key(); out['pyramid'] = pyramid(); out['door_closed'] = door(False); out['door_open'] = door(True)
    for i in range(6): out[f'blood_{i}'] = blood_decal(i, 14 + (i % 3) * 4)
    for i in range(3): out[f'pool_{i}'] = pool(i, 26 + i * 8, 18 + i * 4)
    for k in range(4):
        for i in range(3): out[f'gib_{k}_{i}'] = gib(k, i)
    return out
