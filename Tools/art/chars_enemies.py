from pa import *
import random, math
BONE = (226, 216, 190, 255); BONE_D = (150, 140, 120, 255); DARK = (46, 38, 52, 255)
GLOW = (110, 255, 200, 255)

# ------------------------------------------------------------------ Itzcuintli (skeletal hound), faces RIGHT, 24x16
def dog(step):
    c = Canvas(24, 16); w = [0, 1, 0, -1][step]
    c.ell(10, 7, 6.5, 3.6, DARK)                       # torso gap
    for rx in (6, 8, 10, 12, 14):                       # ribs
        c.line(rx, 4, rx - 1 + (rx % 2), 9, BONE)
    c.line(4, 3, 16, 3, BONE_D)                         # spine
    c.rect(4, 9, 16, 9, BONE_D)
    # legs (bones)
    for lx, ph in ((5, 1), (8, -1), (14, -1), (17, 1)):
        o = w * ph
        c.line(lx, 9, lx + o, 13, BONE); c.px(lx + o - 1, 14, BONE); c.px(lx + o, 14, BONE); c.px(lx + o + 1, 14, BONE_D)
    # tail
    c.line(3, 3, 0, 1 + (step % 2), BONE); c.px(0, 0 + (step % 2), BONE_D)
    # skull
    c.ell(19.5, 5, 3.2, 2.8, BONE); c.rect(20, 5, 23, 7, BONE); c.rect(21, 8, 23, 8, BONE_D)
    c.px(23, 6, DARK); c.rect(20, 7, 22, 7, DARK)       # jaw gap
    c.px(21, 4, GLOW); c.px(20, 4, (30, 160, 140, 255))  # eye glow
    c.px(19, 2, BONE); c.px(18, 1, BONE); c.px(20, 2, BONE_D)   # ear
    c.px(22, 8, (255, 255, 255, 255)); c.px(23, 8, (255, 255, 255, 255))
    return finish(c)

# ------------------------------------------------------------------ Witch (Tlazolteotl acolyte), front view, 20x28
def witch(step):
    c = Canvas(20, 28); bob = [0, 1, 1, 0][step]; sway = [0, 1, 0, -1][step]
    ROBE = (104, 46, 128, 255); ROBE_D = (62, 24, 86, 255); ROBE_L = (150, 80, 170, 255)
    # hem (ragged, floating)
    for x in range(3, 17):
        hl = 24 + ((x * 7 + step * 3) % 3) + bob
        c.rect(x, 14 + bob, x, hl, ROBE if x % 3 else ROBE_D)
    c.rect(3, 14 + bob, 16, 15 + bob, ROBE_D)
    c.rect(5, 14 + bob, 14, 22 + bob, ROBE)
    c.rect(9, 15 + bob, 10, 23 + bob, ROBE_L)
    # beads necklace
    for i, x in enumerate(range(6, 14)): c.px(x, 13 + bob + (1 if 8 <= x <= 11 else 0), (240, 190, 60, 255) if i % 2 else (210, 60, 60, 255))
    # arms
    c.rect(2, 14 + bob, 4, 19 + bob, ROBE_D); c.rect(15, 14 + bob, 17, 19 + bob, ROBE_D)
    c.rect(2, 19 + bob, 3, 20 + bob, (176, 120, 90, 255)); c.rect(16, 19 + bob, 17, 20 + bob, (176, 120, 90, 255))
    # staff with skull (right side)
    c.rect(18, 8 + bob + sway, 18, 26, (110, 76, 44, 255))
    c.ell(18, 6 + bob + sway, 2, 2, BONE); c.px(17, 6 + bob + sway, DARK); c.px(19, 6 + bob + sway, DARK)
    c.px(17, 4 + bob + sway, (200, 50, 50, 255)); c.px(19, 3 + bob + sway, (60, 190, 90, 255))
    # hood + face
    c.ell(9.5, 8 + bob, 6, 6.5, ROBE); c.ell(9.5, 9 + bob, 4, 4.4, (24, 10, 34, 255))
    c.poly([(6, 4 + bob), (9.5, -1 + bob), (13, 4 + bob)], ROBE)      # pointed hood
    c.rect(7, 8 + bob, 8, 9 + bob, (255, 220, 70, 255)); c.rect(11, 8 + bob, 12, 9 + bob, (255, 220, 70, 255))  # glowing eyes
    c.px(9, 11 + bob, (230, 230, 230, 255)); c.px(10, 11 + bob, (230, 230, 230, 255))
    return finish(c)

# ------------------------------------------------------------------ Quinametzin (giant), front view, 48x56
def giant(step):
    c = Canvas(48, 56); b = [0, 1, 0, 1][step]; la = [0, 1, 0, -1][step]
    SK = (150, 106, 86, 255); SK_D = (104, 70, 58, 255); SK_L = (190, 140, 112, 255)
    CL = (170, 36, 40, 255); CL_D = (110, 22, 30, 255)
    # legs
    c.rect(13, 36, 21, 52 + la, SK); c.rect(26, 36, 34, 52 - la, SK)
    c.rect(13, 36, 15, 52 + la, SK_D); c.rect(32, 36, 34, 52 - la, SK_D)
    c.rect(11, 52 + la, 22, 54 + la, (90, 60, 40, 255)); c.rect(25, 52 - la, 36, 54 - la, (90, 60, 40, 255))
    # loincloth
    c.rect(13, 32, 34, 44, CL); c.rect(13, 42, 34, 44, CL_D)
    for x in range(14, 34, 4): c.rect(x, 33, x + 1, 43, (240, 190, 60, 255))
    # torso
    c.ell(24, 22 + b, 14, 12, SK); c.rect(10, 18 + b, 37, 30 + b, SK)
    c.rect(10, 28 + b, 37, 31 + b, SK_D)
    c.ell(18, 21 + b, 5, 4, SK_L); c.ell(30, 21 + b, 5, 4, SK_L)             # pecs
    c.line(24, 24 + b, 24, 30 + b, SK_D)
    for sx, sy in ((14, 26), (32, 24), (19, 28)): c.line(sx, sy + b, sx + 3, sy + 2 + b, (200, 70, 70, 255))   # scars
    # arms
    c.ell(7, 22 + b, 5, 8, SK); c.ell(40, 22 + b, 5, 8, SK)
    c.rect(3, 26 + b, 9, 33 + b + la, SK); c.rect(38, 26 + b, 44, 33 + b - la, SK)
    # club in right hand
    c.rect(43, 6 + b, 46, 40 + b - la, (112, 78, 46, 255)); c.rect(41, 4 + b, 47, 20 + b - la, (130, 92, 54, 255))
    for yy in range(6, 20, 4): c.px(41, yy + b - la, (20, 20, 30, 255)); c.px(47, yy + b - la, (20, 20, 30, 255)); c.px(42, yy + b - la, (110, 110, 150, 255))
    # bone necklace
    for x in range(14, 34, 3): c.rect(x, 14 + b + (1 if 20 < x < 28 else 0), x + 1, 16 + b + (1 if 20 < x < 28 else 0), BONE)
    # head
    c.ell(24, 9 + b, 8, 7, SK); c.rect(17, 3 + b, 30, 5 + b, (30, 24, 36, 255))   # hair
    for x in range(16, 32, 3): c.rect(x, 0 + b, x + 1, 3 + b, (30, 24, 36, 255))   # spikes
    c.rect(18, 7 + b, 29, 8 + b, SK_D)                                              # brow
    c.rect(19, 9 + b, 21, 10 + b, (255, 200, 60, 255)); c.rect(26, 9 + b, 28, 10 + b, (255, 200, 60, 255))
    c.rect(20, 14 + b, 28, 15 + b, SK_D)                                             # jaw
    c.rect(19, 12 + b, 19, 15 + b, BONE); c.rect(29, 12 + b, 29, 15 + b, BONE)       # tusks
    return finish(c)

# ------------------------------------------------------------------ Quetzalcoatl (feathered serpent), 96x80
def quetzal(step):
    W, H = 96, 80
    c = Canvas(W, H); sway = [0, 1, 0, -1][step]
    G = (30, 150, 90); T = (40, 200, 175); GOLD = (244, 200, 70); RED = (214, 52, 60); BLUE = (50, 110, 220)
    # coil body: path from tail (bottom-left) spiralling to the neck (top-center)
    pts = []
    for i in range(0, 100):
        t = i / 99
        ang = 3.0 * math.pi * t + math.pi * 0.2
        r = 34 * (1 - 0.55 * t)
        x = 48 + math.cos(ang) * r * 1.15 + (t * 0)
        y = 56 + math.sin(ang) * r * 0.42 - t * 30
        pts.append((x, y, 8.5 - 3.5 * t if t < 0.8 else 5.0))
    # feather fringe first (behind), then scales
    for i, (x, y, r) in enumerate(pts):
        if i % 4 == 0:
            col = [RED, BLUE, T][(i // 4) % 3]
            c.line(x, y, x + (-1) ** (i // 4) * (r + 4), y + 3, col)
            c.ell(x + (-1) ** (i // 4) * (r + 4), y + 3, 2, 3, col)
    for i, (x, y, r) in enumerate(pts):
        c.ell(x, y, r, r * 0.9, G if i % 6 < 3 else T)
    for i, (x, y, r) in enumerate(pts):
        if i % 6 == 0: c.ell(x, y + r * 0.45, r * 0.6, r * 0.28, GOLD)    # golden belly bands
        if i % 3 == 0: c.px(x - r * 0.4, y - r * 0.4, (150, 255, 220))
    # neck up to head
    hx = 48 + sway * 2; hy = 18
    c.rect(hx - 4, hy + 6, hx + 4, 40, G)
    for yy in range(24, 40, 4): c.rect(hx - 4, yy, hx + 4, yy + 1, T); c.rect(hx - 2, yy + 2, hx + 2, yy + 3, GOLD)
    # crest of feathers (headdress) fanning above the head
    for k, ang in enumerate(range(-80, 81, 20)):
        a = math.radians(ang - 90); L = 22 + (6 if k % 2 == 0 else 0)
        ex = hx + math.cos(a) * L + sway * (k - 4) * 0.3; ey = hy + 4 + math.sin(a) * L
        col = [RED, GOLD, T, G][k % 4]
        c.line(hx, hy + 2, ex, ey, col); c.line(hx + 1, hy + 2, ex + 1, ey, shade(col, 0.8))
        c.ell(ex, ey, 2, 2.5, col)
    # head
    c.ell(hx, hy + 4, 12, 9, G); c.ell(hx, hy + 6, 10, 6, T)
    c.rect(hx - 9, hy + 10, hx + 9, hy + 16, G)                     # snout/jaw block
    c.rect(hx - 8, hy + 14, hx + 8, hy + 16, (110, 20, 30))         # open mouth
    for fx_ in (-7, -4, 4, 7): c.poly([(hx + fx_ - 1, hy + 13), (hx + fx_ + 1, hy + 13), (hx + fx_, hy + 19)], (250, 250, 240))   # fangs
    c.line(hx, hy + 16, hx, hy + 20, RED); c.line(hx, hy + 20, hx - 2, hy + 22, RED); c.line(hx, hy + 20, hx + 2, hy + 22, RED)   # tongue
    for ex in (-6, 6):
        c.ell(hx + ex, hy + 4, 3, 3, (255, 230, 70)); c.rect(hx + ex, hy + 2, hx + ex, hy + 6, (20, 10, 10))   # eyes (slit pupils)
        c.line(hx + ex - 3, hy, hx + ex + 3, hy + 2 if ex < 0 else hy - 0, (10, 70, 50))
    c.px(hx - 2, hy + 10, DARK); c.px(hx + 2, hy + 10, DARK)       # nostrils
    return finish(c)

def build_all():
    out = {}
    out['dog'] = sheet([[dog(s) for s in range(4)]], 26, 18)
    out['witch'] = sheet([[witch(s) for s in range(4)]], 22, 30)
    out['giant'] = sheet([[giant(s) for s in range(4)]], 50, 58)
    out['quetzal'] = sheet([[quetzal(s) for s in range(4)]], 98, 82)
    return out

if __name__ == '__main__':
    import sys
    from PIL import Image
    for k, im in build_all().items():
        bg = Image.new('RGBA', im.size, (36, 46, 40, 255)); bg.alpha_composite(im)
        s = 6 if k in ('dog', 'witch') else 4 if k == 'giant' else 3
        bg.resize((im.width * s, im.height * s), Image.NEAREST).save(f'/tmp/e_{k}.png'); print(k, im.size)
