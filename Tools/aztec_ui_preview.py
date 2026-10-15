"""Preview of the Aztec UI frames drawn at runtime by Assets/Scripts/UI/AztecUI.cs.
The C# code is a 1:1 port of this algorithm. Run: python3 aztec_ui_preview.py out.png"""
import sys
from PIL import Image

FRET = [
    "11111110",
    "10000010",
    "10111010",
    "10100010",
    "10111110",
]
B = 5          # band thickness (texels), same as the fret height

PAL = {
    'normal':    dict(fill=(26, 11, 11), fill2=(33, 15, 13), band=(62, 20, 17), bright=(214, 172, 52), mid=(150, 112, 32), edge=(8, 3, 3), line=(30, 10, 9), glint=None, stud=(62, 20, 17)),
    'highlight': dict(fill=(62, 17, 15), fill2=(72, 22, 18), band=(92, 28, 22), bright=(96, 232, 214), mid=(52, 160, 150), edge=(8, 3, 3), line=(40, 12, 10), glint=None, stud=(92, 28, 22)),
    'selected':  dict(fill=(124, 24, 26), fill2=(140, 30, 30), band=(150, 40, 30), bright=(255, 226, 110), mid=(220, 170, 60), edge=(8, 3, 3), line=(60, 14, 12), glint=(255, 150, 90), stud=(150, 40, 30)),
    'pressed':   dict(fill=(176, 66, 52), fill2=(188, 74, 58), band=(190, 80, 60), bright=(255, 244, 190), mid=(240, 200, 120), edge=(8, 3, 3), line=(90, 30, 24), glint=None, stud=(190, 80, 60)),
    'disabled':  dict(fill=(30, 28, 28), fill2=(34, 32, 32), band=(46, 44, 44), bright=(100, 98, 92), mid=(72, 70, 66), edge=(10, 9, 9), line=(24, 22, 22), glint=None, stud=(46, 44, 44)),
    'panel':     dict(fill=(16, 7, 8), fill2=(21, 10, 10), band=(44, 14, 13), bright=(176, 140, 40), mid=(112, 86, 26), edge=(6, 2, 2), line=(22, 8, 8), glint=None, stud=(44, 14, 13), alpha=236),
}


def hsh(x, y):
    h = (x * 73856093) ^ (y * 19349663)
    return (h ^ (h >> 7)) & 0xFF


def draw(tw, th, state):
    p = PAL[state]
    a = p.get('alpha', 255)
    px = [[None] * tw for _ in range(th)]

    def put(x, y, c):
        if 0 <= x < tw and 0 <= y < th:
            px[y][x] = (c[0], c[1], c[2], a if c is not None else 0)

    for y in range(th):
        for x in range(tw):
            put(x, y, p['fill2'] if hsh(x, y) % 6 == 0 else p['fill'])
    # outer edge
    for x in range(tw):
        put(x, 0, p['edge']); put(x, th - 1, p['edge'])
    for y in range(th):
        put(0, y, p['edge']); put(tw - 1, y, p['edge'])
    # band background
    for y in range(1, th - 1):
        for x in range(1, tw - 1):
            if min(x - 1, tw - 2 - x, y - 1, th - 2 - y) < B:
                put(x, y, p['band'])
    # inner line + glint
    for y in range(1 + B, th - 1 - B):
        for x in range(1 + B, tw - 1 - B):
            d = min(x - (1 + B), tw - 2 - B - x, y - (1 + B), th - 2 - B - y)
            if d == 0:
                put(x, y, p['line'])
            elif d == 1 and p['glint']:
                put(x, y, p['glint'])

    def fret_color(r, c):
        return p['bright'] if (r == 0 or c == 0) else p['mid']

    # corners
    for cx, cy in ((1, 1), (tw - 1 - B, 1), (1, th - 1 - B), (tw - 1 - B, th - 1 - B)):
        for r in range(B):
            for c in range(B):
                put(cx + c, cy + r, p['stud'] if (r == B // 2 and c == B // 2) else fret_color(r, c))

    # horizontal runs (top / bottom)
    x0, x1 = 1 + B, tw - 1 - B
    n = (x1 - x0) // 8
    off = x0 + ((x1 - x0) - n * 8) // 2
    for k in range(n):
        for r in range(B):
            for c in range(8):
                if FRET[r][c] == '1':
                    put(off + k * 8 + c, 1 + r, fret_color(r, c))
                    put(off + k * 8 + c, th - 2 - r, fret_color(r, c))
    # vertical runs (left / right)
    y0, y1 = 1 + B, th - 1 - B
    n = (y1 - y0) // 8
    off = y0 + ((y1 - y0) - n * 8) // 2
    for k in range(n):
        for r in range(B):
            for c in range(8):
                if FRET[r][c] == '1':
                    put(1 + r, off + k * 8 + c, fret_color(r, c))
                    put(tw - 2 - r, off + k * 8 + c, fret_color(r, c))
    img = Image.new('RGBA', (tw, th))
    for y in range(th):
        for x in range(tw):
            if px[y][x]:
                img.putpixel((x, y), px[y][x])
    return img


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else 'aztec_preview.png'
    S = 2
    sheet = Image.new('RGBA', (700, 560), (10, 10, 12, 255))
    y = 8
    for st in ('normal', 'highlight', 'selected', 'pressed', 'disabled'):
        im = draw(190, 27, st).resize((190 * S, 27 * S), Image.NEAREST)
        sheet.alpha_composite(im, (8, y)); y += 27 * S + 6
    for i, st in enumerate(('normal', 'selected')):
        im = draw(165, 20, st).resize((165 * S, 20 * S), Image.NEAREST)
        sheet.alpha_composite(im, (8, y)); y += 20 * S + 6
    im = draw(150, 133, 'panel').resize((150 * S, 133 * S), Image.NEAREST)
    sheet.alpha_composite(im, (400, 8))
    sheet.save(out)
