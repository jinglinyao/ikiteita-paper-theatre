# -*- coding: utf-8 -*-
"""Small textures for the rework after the comparison with The Edge:
  pane_washi.png   fibrous window paper, hot in the middle and falling to deep amber at the edges (emission map of a lit pane)
  sil_*.png        the cut-paper silhouettes become white-on-alpha, so that the card can be tinted brown and take light
  sil_cat.png      a cat on the sill, to replace the open book that read as a pair of boots
  room_poster.png  the picture in the frame over the television: a red paper sun over the roofs of the paper street
    python -P tools/make_rework_art.py"""
import os, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'unity', 'Assets', 'Paper', 'stage2')
rng = np.random.default_rng(20261114); random.seed(20261114)

# ---- window paper
N = 768; yy, xx = np.mgrid[0:N, 0:N].astype(np.float32); cx, cy = N * 0.5, N * 0.56
r = np.sqrt(((xx - cx) / (N * 0.62)) ** 2 + ((yy - cy) / (N * 0.62)) ** 2)
fall = np.clip(1.0 - 0.62 * r ** 1.6, 0.18, 1.0)
noise = rng.normal(0, 1, (N, N)).astype(np.float32); fine = np.asarray(Image.fromarray((noise * 40 + 128).clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))).astype(np.float32) / 255.0
cloud = np.asarray(Image.fromarray((rng.normal(0, 1, (N, N)) * 60 + 128).clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(26))).astype(np.float32) / 255.0
fib = Image.new('L', (N, N), 0); d = ImageDraw.Draw(fib)
for _ in range(520):
    x0, y0 = random.uniform(0, N), random.uniform(0, N); a = random.uniform(0, math.pi); L = random.uniform(18, 70)
    d.line((x0, y0, x0 + math.cos(a) * L, y0 + math.sin(a) * L), fill=random.randint(40, 120), width=1)
fib = np.asarray(fib.filter(ImageFilter.GaussianBlur(0.7))).astype(np.float32) / 255.0
v = fall * (0.90 + 0.10 * (fine - 0.5) * 2 + 0.16 * (cloud - 0.5) * 2) * (1.0 - 0.22 * fib)
rgb = np.stack([v * 1.00, v * (0.66 + 0.10 * fall), v * (0.34 + 0.16 * fall)], -1)
Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8)).save(os.path.join(ART, 'pane_washi.png'))

# ---- silhouettes: white on alpha
for n in ('sil_bike', 'sil_clock', 'sil_book', 'sil_umbrella', 'sil_lamp', 'sil_bag'):
    p = os.path.join(ART, n + '.png'); im = Image.open(p).convert('RGBA'); a = np.asarray(im)[..., 3]
    out = np.zeros((im.height, im.width, 4), np.uint8); out[..., :3] = 255; out[..., 3] = a; Image.fromarray(out).save(p)

# ---- a cat on the sill
S = 4; W, H = 420 * S, 520 * S; cat = Image.new('L', (W, H), 0); d = ImageDraw.Draw(cat)
d.ellipse((90 * S, 230 * S, 330 * S, 510 * S), fill=255)                                   # haunches and body
d.ellipse((120 * S, 150 * S, 300 * S, 380 * S), fill=255)                                  # chest
d.ellipse((135 * S, 60 * S, 285 * S, 200 * S), fill=255)                                   # head
d.polygon([(140 * S, 110 * S), (150 * S, 22 * S), (205 * S, 80 * S)], fill=255); d.polygon([(280 * S, 110 * S), (270 * S, 22 * S), (215 * S, 80 * S)], fill=255)   # ears
pts = [(300 + 60 * math.sin(t * 2.4) * t, 500 - 150 * t) for t in [i / 40 for i in range(41)]]
for (x0, y0), (x1, y1) in zip(pts, pts[1:]): d.line((x0 * S + 40, y0 * S, x1 * S + 40, y1 * S), fill=255, width=22 * S)
d.rectangle((60 * S, 500 * S, 380 * S, 520 * S), fill=255)
cat = cat.resize((420, 520), Image.LANCZOS); out = np.zeros((520, 420, 4), np.uint8); out[..., :3] = 255; out[..., 3] = np.asarray(cat)
Image.fromarray(out).save(os.path.join(ART, 'sil_cat.png'))
txt = os.path.join(ART, 'stage_art.txt'); lines = [l for l in open(txt, encoding='utf-8').read().splitlines() if not l.startswith('sil sil_cat')]
lines.append('sil sil_cat %.4f' % (420 / 520)); open(txt, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')

# ---- the picture over the television
PW, PH = 720, 1080; im = Image.new('RGB', (PW, PH), (226, 214, 190)); d = ImageDraw.Draw(im)
d.ellipse((PW * 0.30, PH * 0.16, PW * 0.80, PH * 0.16 + PW * 0.50), fill=(196, 52, 40))
x = 0
while x < PW:
    w = random.randint(70, 150); h = random.randint(int(PH * 0.22), int(PH * 0.46)); col = random.choice([(34, 44, 72), (46, 58, 92), (28, 36, 60)])
    d.rectangle((x, PH - h, x + w, PH), fill=col)
    for wy in range(PH - h + 30, PH - 30, 58):
        for wx in range(x + 16, x + w - 22, 34):
            if random.random() < 0.28: d.rectangle((wx, wy, wx + 14, wy + 24), fill=(238, 176, 84))
    x += w
grain = rng.normal(0, 5, (PH, PW, 1)); im = Image.fromarray((np.asarray(im).astype(np.float32) + grain).clip(0, 255).astype(np.uint8))
os.makedirs(os.path.join(ROOT, 'unity', 'Assets', 'Gen'), exist_ok=True); im.save(os.path.join(ROOT, 'unity', 'Assets', 'Gen', 'room_poster.png'))
print('wrote pane_washi.png, six white silhouettes, sil_cat.png, room_poster.png')
