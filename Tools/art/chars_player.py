from pa import *
SKIN = (200, 138, 90, 255); SKIN_D = (150, 98, 62, 255); HAIR = (28, 22, 36, 255)
BLUE = (52, 98, 200, 255); BLUE_L = (96, 150, 240, 255); BLUE_D = (30, 56, 140, 255)
WHITE = (238, 232, 215, 255); BROWN = (120, 76, 44, 255); OBS = (34, 32, 50, 255); OBS_L = (110, 110, 150, 255)
GOLD = (244, 196, 64, 255); RED = (200, 50, 50, 255); SANDAL = (150, 100, 55, 255)

def weapon(c, x, y, down=True):
    # macuahuitl: wooden paddle with obsidian blades
    c.rect(x, y, x, y + 6, BROWN); c.rect(x - 1, y + 1, x - 1, y + 5, BROWN)
    for yy in (y + 1, y + 3, y + 5): c.px(x - 2, yy, OBS); c.px(x + 1, yy, OBS_L)

def front(step, bob):
    c = Canvas(16, 24); b = bob
    # legs
    lo = (1 if step == 1 else 0); ro = (1 if step == 3 else 0)
    c.rect(5, 19 - 0, 6, 21 - lo + 1, SKIN); c.rect(9, 19, 10, 21 - ro + 1, SKIN)
    c.rect(4, 22 - lo, 6, 22 - lo + 0, SANDAL); c.rect(9, 22 - ro, 11, 22 - ro, SANDAL)
    # tunic
    c.rect(3, 11 + b, 12, 18 + b, BLUE); c.rect(3, 17 + b, 12, 18 + b, BLUE_D)
    c.rect(3, 18 + b, 12, 18 + b, WHITE)      # trim
    c.rect(7, 11 + b, 8, 17 + b, BLUE_L)      # center stripe
    c.rect(5, 12 + b, 10, 12 + b, GOLD)       # collar
    # arms
    la = 1 if step == 3 else 0; ra = 1 if step == 1 else 0
    c.rect(1, 12 + b + la, 2, 16 + b + la, SKIN); c.rect(13, 12 + b + ra, 14, 16 + b + ra, SKIN)
    c.rect(1, 12 + b + la, 2, 13 + b + la, BLUE)
    c.rect(13, 12 + b + ra, 14, 13 + b + ra, BLUE)
    weapon(c, 14, 14 + b + ra)
    # head
    c.ell(7.5, 6 + b, 5.6, 5.0, SKIN)
    c.rect(2, 1 + b, 13, 4 + b, HAIR); c.rect(3, 0 + b, 12, 1 + b, HAIR); c.rect(2, 5 + b, 3, 6 + b, HAIR); c.rect(12, 5 + b, 13, 6 + b, HAIR)
    c.rect(3, 4 + b, 12, 4 + b, BLUE_D); c.rect(3, 3 + b, 12, 3 + b, BLUE)   # headband
    c.px(12, 1 + b, WHITE); c.px(13, 0 + b, WHITE); c.px(13, 1 + b, RED)     # feather
    c.px(5, 7 + b, HAIR); c.px(10, 7 + b, HAIR)                               # eyes
    c.rect(4, 8 + b, 6, 8 + b, BLUE_L); c.rect(9, 8 + b, 11, 8 + b, BLUE_L)   # blue face paint
    c.px(7, 9 + b, SKIN_D); c.px(8, 9 + b, SKIN_D)
    return finish(c)

def back(step, bob):
    c = Canvas(16, 24); b = bob
    lo = (1 if step == 1 else 0); ro = (1 if step == 3 else 0)
    c.rect(5, 19, 6, 21 - lo + 1, SKIN); c.rect(9, 19, 10, 21 - ro + 1, SKIN)
    c.rect(4, 22 - lo, 6, 22 - lo, SANDAL); c.rect(9, 22 - ro, 11, 22 - ro, SANDAL)
    c.rect(2, 11 + b, 13, 19 + b, BLUE); c.rect(2, 17 + b, 13, 19 + b, BLUE_D)   # cape
    c.rect(2, 19 + b, 13, 19 + b, WHITE)
    c.rect(7, 12 + b, 8, 18 + b, BLUE_L)
    la = 1 if step == 1 else 0; ra = 1 if step == 3 else 0
    c.rect(1, 12 + b + la, 2, 15 + b + la, SKIN); c.rect(13, 12 + b + ra, 14, 15 + b + ra, SKIN)
    c.ell(7.5, 6 + b, 5.6, 5.2, HAIR)
    c.rect(3, 4 + b, 12, 4 + b, BLUE_D); c.rect(3, 3 + b, 12, 3 + b, BLUE)
    c.rect(5, 9 + b, 10, 10 + b, SKIN_D)   # neck
    c.px(12, 1 + b, WHITE); c.px(13, 0 + b, WHITE); c.px(13, 1 + b, RED)
    return finish(c)

def side(step, bob):
    c = Canvas(16, 24); b = bob
    # legs (front/back swing)
    sw = [0, 2, 0, -2][step]
    c.rect(7 + sw, 19, 8 + sw, 21, SKIN); c.rect(7 - sw, 19, 8 - sw, 21, SKIN_D)
    c.rect(7 + sw, 22, 10 + sw, 22, SANDAL); c.rect(7 - sw, 22, 10 - sw, 22, SANDAL)
    c.rect(4, 11 + b, 11, 18 + b, BLUE); c.rect(4, 17 + b, 11, 18 + b, BLUE_D); c.rect(4, 18 + b, 11, 18 + b, WHITE)
    c.rect(3, 12 + b, 5, 18 + b, BLUE_D)   # cape edge
    c.rect(6, 12 + b, 6, 17 + b, BLUE_L)
    aw = [0, 1, 0, -1][step]
    c.rect(7 + aw, 12 + b, 8 + aw, 16 + b, SKIN); c.rect(7 + aw, 12 + b, 8 + aw, 13 + b, BLUE)
    weapon(c, 10 + aw, 13 + b)
    c.ell(7.5, 6 + b, 5.2, 5.0, SKIN)
    c.rect(2, 1 + b, 11, 4 + b, HAIR); c.rect(2, 5 + b, 5, 7 + b, HAIR); c.rect(3, 0 + b, 10, 1 + b, HAIR)
    c.rect(2, 4 + b, 12, 4 + b, BLUE_D); c.rect(2, 3 + b, 12, 3 + b, BLUE)
    c.px(2, 1 + b, WHITE); c.px(1, 0 + b, WHITE); c.px(1, 1 + b, RED)
    c.px(10, 7 + b, HAIR); c.rect(9, 8 + b, 11, 8 + b, BLUE_L); c.px(12, 7 + b, SKIN_D)
    return finish(c)

def build():
    rows = []
    for fn in (front, back, side):
        rows.append([fn(s, [0, -1, 0, -1][s] * 0 + (1 if s in (1, 3) else 0) * 0) for s in range(4)])
    return sheet(rows, 18, 26)

if __name__ == '__main__':
    import sys; im = build(); im.save(sys.argv[1]); print(im.size)
