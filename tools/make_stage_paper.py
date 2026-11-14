# -*- coding: utf-8 -*-
"""Cut-paper pieces for the life-size paper stage (first pipeline pass: shots S16-S17).
Flats have real holes for windows; each window is its own pane (warm translucent paper with a cut silhouette) set behind the hole,
so a pane can be lit on its own.   python -P tools/make_stage_paper.py <out_dir>
"""
import sys, os, json, math
out = sys.argv[1]
_src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'make_paper_street.py'), encoding='utf-8').read().split('# ---- far skyline')[0]
exec(_src)                                                    # Layer, wob, rng, META (the same hand-cut edge, rim and grain as the street set)

HOLE = (0, 0, 0, 0)
WINDOWS = []                                                  # (flat, cx, cy, w, h) in the flat's own metres


def seg(L, p0, p1, wid, col):
    (x0, y0), (x1, y1) = p0, p1; d = math.hypot(x1 - x0, y1 - y0) + 1e-9; nx, ny = -(y1 - y0) / d * wid / 2, (x1 - x0) / d * wid / 2
    L.poly([(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], col, True, 0.0015)


def ring(L, cx, cy, r, wid, col, bg):
    L.ellipse(cx, cy, r, r, col); L.ellipse(cx, cy, r - wid, r - wid, bg)


def facade(name, w, h, base, wins, ppm, roof=True):
    global PPM
    PPM = ppm
    L = Layer(name, w, h)
    L.rect(0.02, 0, w - 0.02, h - (0.16 if roof else 0.02), base, amp=0.006)
    if roof:
        L.rect(0.0, h - 0.20, w, h - 0.10, tuple(int(c * 0.82) for c in base[:3]) + (255,), amp=0.004)       # cornice
        L.rect(w * 0.60, h - 0.10, w * 0.78, h, tuple(int(c * 0.9) for c in base[:3]) + (255,), amp=0.004)   # water tank
    for k in range(1, int(h / 0.62)):                                                                          # floor lines, a shade darker
        L.rect(0.03, k * 0.62 - 0.012, w - 0.03, k * 0.62 + 0.012, tuple(int(c * 0.88) for c in base[:3]) + (255,), amp=0.002)
    for (cx, cy, ww, hh) in wins:
        L.rect(cx - ww / 2 - 0.035, cy - hh / 2 - 0.035, cx + ww / 2 + 0.035, cy + hh / 2 + 0.035, tuple(min(255, int(c * 1.5)) for c in base[:3]) + (255,), amp=0.003)   # frame, a paler paper
        L.rect(cx - ww / 2, cy - hh / 2, cx + ww / 2, cy + hh / 2, HOLE, amp=0.003)
        L.rect(cx - 0.012, cy - hh / 2 - 0.01, cx + 0.012, cy + hh / 2 + 0.01, tuple(min(255, int(c * 1.5)) for c in base[:3]) + (255,), amp=0.0015)                 # glazing bar
        L.rect(cx - ww / 2 - 0.06, cy - hh / 2 - 0.075, cx + ww / 2 + 0.06, cy - hh / 2 - 0.035, tuple(int(c * 0.8) for c in base[:3]) + (255,), amp=0.002)          # sill
        WINDOWS.append((name, cx, cy, ww, hh))
    L.save(grain=0.07)


def pane(name, draw, ppm=1024, w=0.44, h=0.52):
    global PPM
    PPM = ppm
    L = Layer(name, w, h)
    warm = (246, 208, 132, 255); ink = (58, 34, 22, 255)
    L.rect(0, 0, w, h, warm, amp=0.002, emit=1.0)
    L.rect(0, 0, w, 0.07, (202, 150, 84, 255), amp=0.003, emit=0.8)                     # the table or floor the thing stands on
    draw(L, w, h, ink, warm)
    L.save(grain=0.05, rim=False)


def d_lamp(L, w, h, ink, bg):
    L.ellipse(w * .42, .09, .075, .022, ink); seg(L, (w * .42, .09), (w * .50, .27), .016, ink); seg(L, (w * .50, .27), (w * .40, .37), .016, ink)
    L.poly([(w * .30, .34), (w * .52, .40), (w * .60, .30), (w * .26, .24)], ink, True, .002)


def d_clock(L, w, h, ink, bg):
    cx, cy = w * .5, .22
    L.ellipse(cx, cy, .10, .10, ink); L.ellipse(cx, cy, .078, .078, bg); seg(L, (cx, cy), (cx, cy + .055), .012, ink); seg(L, (cx, cy), (cx + .04, cy - .01), .012, ink)
    L.ellipse(cx - .075, cy + .095, .035, .028, ink); L.ellipse(cx + .075, cy + .095, .035, .028, ink)
    seg(L, (cx - .06, cy - .08), (cx - .09, .07), .016, ink); seg(L, (cx + .06, cy - .08), (cx + .09, .07), .016, ink)


def d_book(L, w, h, ink, bg):
    L.poly([(w * .16, .08), (w * .80, .11), (w * .78, .15), (w * .14, .12)], ink, True, .002)
    L.poly([(w * .22, .13), (w * .82, .16), (w * .80, .195), (w * .20, .165)], ink, True, .002)
    seg(L, (w * .30, .20), (w * .62, .36), .014, ink)                                     # a pencil left on top


def d_umbrella(L, w, h, ink, bg):
    cx = w * .5
    L.poly([(cx + .15 * math.cos(t), .30 + .13 * math.sin(t)) for t in [i * math.pi / 14 for i in range(15)]], ink, True, .002)
    for k in range(3): L.ellipse(cx - .10 + k * .10, .30, .05, .028, bg)
    ring(L, cx + .028, .115, .028, .012, ink, bg); L.rect(cx - .01, .115, cx + .07, .16, bg, amp=0.001); seg(L, (cx, .31), (cx, .115), .012, ink)


def d_bike(L, w, h, ink, bg):
    ring(L, w * .26, .155, .085, .014, ink, bg); ring(L, w * .74, .155, .085, .014, ink, bg)
    for a, b in [((w * .26, .155), (w * .44, .28)), ((w * .44, .28), (w * .66, .28)), ((w * .66, .28), (w * .74, .155)), ((w * .44, .28), (w * .52, .155)),
                 ((w * .52, .155), (w * .26, .155)), ((w * .66, .28), (w * .64, .34)), ((w * .60, .345), (w * .70, .335)), ((w * .44, .28), (w * .42, .32))]:
        seg(L, a, b, .012, ink)
    L.rect(w * .36, .32, w * .48, .335, ink, amp=0.001)


def d_bag(L, w, h, ink, bg):
    L.poly([(w * .30, .08), (w * .70, .08), (w * .72, .30), (w * .66, .34), (w * .34, .34), (w * .28, .30)], ink, True, .003)
    L.poly([(w * .30, .22), (w * .70, .22), (w * .70, .205), (w * .30, .205)], bg, True, .001)
    L.rect(w * .46, .14, w * .54, .19, bg, amp=0.001); ring(L, w * .5, .355, .05, .012, ink, bg); L.rect(w * .40, .30, w * .60, .345, ink, amp=0.001)


os.makedirs(out, exist_ok=True)
near_cols = [0.52, 1.24]; near_rows = [0.72, 1.36, 2.00]
facade('near_R', 1.80, 2.60, (46, 56, 86, 255), [(x, y, 0.34, 0.42) for y in near_rows for x in near_cols], 1024)
facade('near_L', 1.80, 2.30, (40, 50, 78, 255), [(x, y, 0.34, 0.42) for y in near_rows[:2] for x in [0.56, 1.28]], 768)

PPM = 512
L = Layer('far_row', 5.4, 3.0); x = 0.0
while x < 5.4:                                                 # the far row: paler and greyer, windows only painted
    w = rng.uniform(.55, 1.05); h = rng.uniform(1.7, 2.9); c = int(rng.uniform(58, 74))
    L.rect(x, 0, min(5.4, x + w), h, (c, c + 12, c + 30, 255))
    for r in range(int((h - 0.4) / 0.42)):
        for q in range(int(w / 0.3)):
            if rng.random() < 0.8: L.rect(x + 0.10 + q * 0.3, 0.3 + r * 0.42, x + 0.24 + q * 0.3, 0.52 + r * 0.42, (c - 20, c - 10, c + 6, 255), amp=0.002)
    x += w + rng.uniform(.0, .06)
L.save(grain=0.06)

PPM = 256
L = Layer('sky_drop', 5.6, 3.6)                                # the sky cloth: night blue, paler toward the horizon; a few pin-prick stars
import numpy as _np
from PIL import Image as _Im
g = _np.linspace(0, 1, L.H)[:, None] * _np.ones((1, L.W)); top, bot = _np.array([14, 22, 52.]), _np.array([44, 66, 112.])
rgb = top[None, None, :] * (1 - g[..., None] ** 1.6) + bot[None, None, :] * g[..., None] ** 1.6
L.im = _Im.fromarray(_np.dstack([rgb, _np.full((L.H, L.W), 255.)]).astype(_np.uint8), 'RGBA'); L.d = ImageDraw.Draw(L.im)
for _ in range(46):
    sx, sy = rng.uniform(0.1, 5.5), rng.uniform(1.5, 3.5); r = rng.uniform(0.006, 0.014)
    L.ellipse(sx, sy, r, r, (236, 232, 210, 255), n=8)
L.save(grain=0.05, rim=False)

for nm, fn in [('pane_lamp', d_lamp), ('pane_clock', d_clock), ('pane_book', d_book), ('pane_umbrella', d_umbrella), ('pane_bike', d_bike), ('pane_bag', d_bag)]:
    pane(nm, fn)
PPM = 512
L = Layer('pane_dark', 0.44, 0.52); L.rect(0, 0, 0.44, 0.52, (20, 26, 40, 255), amp=0.002); L.save(grain=0.05, rim=False)

json.dump(dict(layers=META, windows=WINDOWS), open(os.path.join(out, 'stage_layers.json'), 'w'), indent=1)
# a flat preview of the right-hand facade with its six panes behind the holes
prev = _Im.new('RGB', (1843, 2662), (10, 12, 20)); fac = _Im.open(os.path.join(out, 'near_R.png'))
names = ['pane_lamp', 'pane_clock', 'pane_book', 'pane_umbrella', 'pane_bike', 'pane_bag']
for i, (nm, cx, cy, ww, hh) in enumerate([w for w in WINDOWS if w[0] == 'near_R']):
    p = _Im.open(os.path.join(out, names[i] + '.png')).convert('RGB'); prev.paste(p, (int((cx - 0.22) * 1024), int(2662 - (cy + 0.26) * 1024)))
prev.paste(fac, (0, 0), fac); prev.resize((614, 887)).save(os.path.join(out, '_near_R_preview.jpg'), quality=90)
print('windows', len(WINDOWS))
