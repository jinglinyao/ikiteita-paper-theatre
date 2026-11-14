"""Cut-paper layers for the street-corner set (inside the television). Every layer is one PNG with alpha:
hand-cut wobbling edges, a pale cut rim (the paper's core showing), paper grain baked into the colour.
Scale: PPM pixels per metre, so a layer's size in the scene is its pixel size / PPM.

usage: python make_paper_street.py <out_dir>
"""
import sys, os, json, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

PPM = 384; SS = 2                      # pixels per metre; supersample
rng = np.random.default_rng(20261113)
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
META = {}


def wob(pts, amp=0.006, step=0.06):
    """subdivide a polygon's edges and push the points sideways a little: a hand with scissors, not a ruler (metres)"""
    res = []
    n = len(pts)
    for i in range(n):
        a, b = np.array(pts[i], float), np.array(pts[(i + 1) % n], float)
        L = np.linalg.norm(b - a); k = max(1, int(L / step))
        nrm = np.array([-(b - a)[1], (b - a)[0]]) / (L + 1e-9)
        offs = rng.normal(0, amp, k + 1); offs = np.convolve(offs, [.25, .5, .25], "same"); offs[0] = 0
        for j in range(k): res.append(tuple(a + (b - a) * j / k + nrm * offs[j]))
    return res


class Layer:
    def __init__(self, name, w, h):
        self.name, self.w, self.h = name, w, h
        self.W, self.H = int(w * PPM * SS), int(h * PPM * SS)
        self.im = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0)); self.d = ImageDraw.Draw(self.im)
        self.em = Image.new("RGB", (self.W, self.H), (0, 0, 0)); self.de = ImageDraw.Draw(self.em); self.has_em = False

    def px(self, pts): return [(x * PPM * SS, self.H - y * PPM * SS) for x, y in pts]      # y up, origin bottom-left

    def poly(self, pts, col, wobble=True, amp=0.006, emit=0.0):
        q = self.px(wob(pts, amp) if wobble else pts); self.d.polygon(q, fill=col)
        self.de.polygon(q, fill=tuple(int(v * emit) for v in col[:3]))             # whatever is cut over a lamp-lit window hides it again
        if emit > 0: self.has_em = True

    def rect(self, x0, y0, x1, y1, col, wobble=True, amp=0.004, emit=0.0): self.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], col, wobble, amp, emit)

    def ellipse(self, cx, cy, rx, ry, col, n=40, emit=0.0):
        self.poly([(cx + rx * math.cos(t), cy + ry * math.sin(t)) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)], col, True, 0.003, emit)

    def save(self, grain=0.05, rim=True):
        a = np.asarray(self.im).astype(np.float32) / 255.0
        rgb, al = a[..., :3], a[..., 3]
        if rim:                                                   # pale rim just inside the cut edge
            er = np.asarray(Image.fromarray((al * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(2 * (2 * SS) + 1))).astype(np.float32) / 255.0
            edge = np.clip(al - er, 0, 1)[..., None]
            rgb = rgb + (1 - rgb) * edge * 0.45
        g = rng.normal(0, 1, (self.H // 3 + 1, self.W // 3 + 1)).astype(np.float32)
        g = np.asarray(Image.fromarray(g).resize((self.W, self.H), Image.BICUBIC)) + rng.normal(0, .6, (self.H, self.W)).astype(np.float32)
        fib = rng.normal(0, 1, (self.H, self.W // 24 + 1)).astype(np.float32); fib = np.asarray(Image.fromarray(fib).resize((self.W, self.H), Image.BICUBIC))   # long fibres
        rgb = np.clip(rgb * (1 + grain * (0.6 * g + 0.5 * fib))[..., None], 0, 1)
        o = Image.fromarray((np.dstack([rgb, al]) * 255).astype(np.uint8), "RGBA").resize((self.W // SS, self.H // SS), Image.LANCZOS)
        o.save(os.path.join(out, self.name + ".png")); META[self.name] = dict(w=self.w, h=self.h, px=[self.W // SS, self.H // SS], emit=self.has_em)
        if self.has_em: self.em.filter(ImageFilter.GaussianBlur(SS)).resize((self.W // SS, self.H // SS), Image.LANCZOS).save(os.path.join(out, self.name + "_emit.png"))
        print(self.name, o.size)


def windows(L, x0, y0, x1, y1, cols, rows, lit=.35, wc=(232, 190, 110, 255), dark=(26, 34, 50, 255), mx=.25, my=.3):
    cw, ch = (x1 - x0) / cols, (y1 - y0) / rows
    for r in range(rows):
        for c in range(cols):
            on = rng.random() < lit
            L.rect(x0 + c * cw + cw * mx / 2, y0 + r * ch + ch * my / 2, x0 + (c + 1) * cw - cw * mx / 2, y0 + (r + 1) * ch - ch * my / 2,
                   (lambda k: (int(wc[0] * k), int(wc[1] * k), int(wc[2] * k * rng.uniform(.8, 1.0)), 255))(rng.uniform(.82, 1.0)) if on else dark, amp=0.002, emit=1.0 if on else 0.0)


# ---- far skyline (8 x 4 m)
L = Layer("far_buildings", 9.0, 4.0); x = 0.0
while x < 9.0:
    w = rng.uniform(.7, 1.5); h = rng.uniform(1.9, 3.7); c = int(rng.uniform(38, 54))
    L.rect(x, 0, x + w, h, (c, c + 12, c + 30, 255))
    if rng.random() < .5: L.rect(x + w * .3, h, x + w * .55, h + rng.uniform(.15, .3), (c - 6, c + 4, c + 20, 255))        # roof tank
    if rng.random() < .4: L.rect(x + w * .7, h, x + w * .73, h + rng.uniform(.3, .6), (c - 10, c, c + 14, 255))            # mast
    windows(L, x + .08, .5, x + w - .08, h - .15, max(2, int(w / .28)), max(3, int((h - .65) / .32)), lit=.22)
    x += w + rng.uniform(-.05, .12)
L.save()

# ---- the two near blocks, a gap between them for the street
for side, (x0, x1) in (("L", (0.0, 3.3)), ("R", (0.0, 3.3))):
    L = Layer("block_" + side, 3.3, 4.6)
    base = (74, 88, 108, 255) if side == "L" else (88, 84, 96, 255)
    L.rect(0.0, 0, 3.3, 4.3 if side == "L" else 3.7, base)
    top = 4.3 if side == "L" else 3.7
    L.rect(0.0, top - .12, 3.3, top, tuple(int(v * .8) for v in base[:3]) + (255,))                                         # parapet
    windows(L, .25, 1.75, 3.05, top - .35, 5, 4 if side == "L" else 3, lit=.4, mx=.34, my=.38)
    L.rect(.2, 1.42, 3.1, 1.62, (196, 60, 52, 255) if side == "L" else (60, 120, 126, 255))                                    # awning (no lettering anywhere)
    L.rect(.3, .0, 1.5, 1.38, (236, 214, 150, 255), emit=.8)                                                                            # lit shop window
    for k in range(4): L.rect(.36 + k * .28, .12, .56 + k * .28, 1.28, (250, 232, 176, 255), amp=.002, emit=1.0)
    L.rect(1.9, .0, 2.55, 1.3, (40, 48, 62, 255))                                                                              # door
    L.rect(2.7, .9, 3.05, 1.3, (228, 226, 214, 255))                                                                           # a blank notice sheet
    L.save()

# ---- street lamp
L = Layer("lamp", 1.2, 3.6)
L.rect(.26, 0, .34, 3.3, (30, 44, 52, 255), amp=.002); L.rect(.20, 0, .40, .18, (30, 44, 52, 255))
L.poly([(.30, 3.2), (.95, 3.42), (.95, 3.5), (.30, 3.3)], (30, 44, 52, 255))
L.poly([(.72, 3.40), (1.12, 3.40), (1.04, 3.22), (.80, 3.22)], (42, 58, 66, 255))
L.poly([(.82, 3.22), (1.02, 3.22), (1.0, 3.16), (.84, 3.16)], (255, 236, 170, 255), emit=1.0)                                           # the bulb, cut from yellow paper
L.save()

# ---- paper-doll chain: seven people, hands joined, folded like an accordion; each holds up a little white sheet
def chain(name, n=7, flip=False):
    pw = .46; L = Layer(name, n * pw + .2, 2.05)
    for i in range(n):
        cx = .1 + pw * (i + .5); hgt = rng.uniform(1.52, 1.72); sh = int(rng.uniform(-8, 8))
        col = (232 + sh, 222 + sh, 200 + sh, 255) if i % 2 == 0 else (218 + sh, 208 + sh, 188 + sh, 255)                       # alternate panels: the fold catches the light
        hr = .105
        L.ellipse(cx, hgt - hr, hr, hr * 1.08, col)
        L.poly([(cx - .15, hgt - .26), (cx + .15, hgt - .26), (cx + .19, .78), (cx - .19, .78)], col)                          # body
        L.poly([(cx - .17, .80), (cx - .03, .80), (cx - .05, 0), (cx - .16, 0)], col); L.poly([(cx + .03, .80), (cx + .17, .80), (cx + .16, 0), (cx + .05, 0)], col)
        L.poly([(cx - .15, hgt - .30), (cx - pw / 2 - .01, hgt - .62), (cx - pw / 2 - .01, hgt - .72), (cx - .16, hgt - .44)], col)   # arm to the neighbour
        up = (i % 3 != 1)
        if up:                                                                                                                 # the other arm is raised, holding the sheet
            L.poly([(cx + .13, hgt - .30), (cx + .26, hgt - .02), (cx + .31, hgt - .05), (cx + .17, hgt - .42)], col)
            L.rect(cx + .20, hgt - .02, cx + .34, hgt + .22, (250, 250, 246, 255), amp=.002, emit=.55)
        else:
            L.poly([(cx + .15, hgt - .30), (cx + pw / 2 + .01, hgt - .62), (cx + pw / 2 + .01, hgt - .72), (cx + .16, hgt - .44)], col)
    if flip: L.im = L.im.transpose(Image.FLIP_LEFT_RIGHT); L.em = L.em.transpose(Image.FLIP_LEFT_RIGHT)
    L.save()
chain("crowd_A"); chain("crowd_B", flip=True)

# ---- police tape, the small red paper, the moon, sky, ground
L = Layer("tape", 7.0, .12); L.rect(0, .01, 7.0, .11, (238, 196, 40, 255), amp=.002)
for k in range(46): L.poly([(k * .16, .01), (k * .16 + .05, .01), (k * .16 + .09, .11), (k * .16 + .04, .11)], (34, 34, 30, 255), False)
L.save(rim=False)
L = Layer("red_paper", .36, .30)
L.poly([(.03, .05), (.14, .02), (.30, .06), (.34, .17), (.27, .27), (.12, .28), (.02, .18)], (196, 30, 34, 255), amp=.008); L.save(grain=.08)
L = Layer("moon", .9, .9); L.ellipse(.45, .45, .40, .40, (244, 238, 214, 255), n=60, emit=.6); L.save()
L = Layer("sky", 10.0, 6.0)
for i in range(60):                                                                                                            # torn strips of blue paper, lighter toward the horizon
    t = i / 59; c = (int(20 + 34 * (1 - t)), int(34 + 46 * (1 - t)), int(64 + 44 * (1 - t)), 255)
    L.rect(0, 6.0 * t - .02, 10.0, 6.0 * (t + 1 / 59) + .02, c, wobble=False)
L.save(grain=.04, rim=False)
L = Layer("ground", 10.0, 8.0); L.rect(0, 0, 10.0, 8.0, (84, 88, 96, 255), wobble=False); L.save(grain=.10, rim=False)
json.dump(dict(ppm=PPM, layers=META), open(os.path.join(out, "layers.json"), "w"), indent=1)
