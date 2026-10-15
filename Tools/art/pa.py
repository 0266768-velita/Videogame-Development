"""Tiny pixel-art helpers (PIL). All sprites are drawn in code so the project needs no external art."""
from PIL import Image, ImageDraw
import random, math

class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)
    def px(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h: self.img.putpixel((int(x), int(y)), c if len(c) == 4 else c + (255,))
    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1): self.px(x, y, c)
    def ell(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if ((x - cx) / max(rx, .1)) ** 2 + ((y - cy) / max(ry, .1)) ** 2 <= 1.0: self.px(x, y, c)
    def line(self, x0, y0, x1, y1, c):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(n + 1):
            t = i / max(1, n); self.px(round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t), c)
    def poly(self, pts, c):
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        for y in range(int(min(ys)), int(max(ys)) + 1):
            for x in range(int(min(xs)), int(max(xs)) + 1):
                if point_in_poly(x + .5, y + .5, pts): self.px(x, y, c)
    def outline(self, c=(14, 8, 18, 255)):
        src = self.img.copy(); sp = src.load()
        for y in range(self.h):
            for x in range(self.w):
                if sp[x, y][3] == 0:
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < self.w and 0 <= ny < self.h and sp[nx, ny][3] > 0:
                            self.img.putpixel((x, y), c); break
    def paste(self, other, x, y, flip=False):
        im = other.img.transpose(Image.FLIP_LEFT_RIGHT) if flip else other.img
        self.img.alpha_composite(im, (int(x), int(y)))

def point_in_poly(x, y, pts):
    inside = False; j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]; xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi: inside = not inside
        j = i
    return inside

def sheet(frames_rows, cw, ch):
    """frames_rows: list of rows, each a list of Canvas. Returns PIL image."""
    cols = max(len(r) for r in frames_rows)
    im = Image.new('RGBA', (cols * cw, len(frames_rows) * ch), (0, 0, 0, 0))
    for ry, row in enumerate(frames_rows):
        for cx, f in enumerate(row): im.alpha_composite(f.img, (cx * cw, ry * ch))
    return im

def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + ((c[3],) if len(c) > 3 else (255,))

def finish(c, pad=1, col=(14, 8, 18, 255)):
    n = Canvas(c.w + pad * 2, c.h + pad * 2)
    n.paste(c, pad, pad)
    n.outline(col)
    return n
