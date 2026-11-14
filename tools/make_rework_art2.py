# -*- coding: utf-8 -*-
"""Second round of small textures:
  sil_plant.png    a potted plant on the sill (replaces the umbrella, which hanging in a window read as a hanging figure)
  sil_guitar.png   a guitar leaning in the window (replaces the rucksack, which read as a hooded figure)
  room_poster.png  the picture over the television, redrawn: a wall of small windows, a few of them lit, one red
    python -P tools/make_rework_art2.py"""
import os, math, random
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'unity', 'Assets', 'Paper', 'stage2')
random.seed(7); rng = np.random.default_rng(7)


def save_sil(mask, name):
    out = np.zeros((mask.height, mask.width, 4), np.uint8); out[..., :3] = 255; out[..., 3] = np.asarray(mask); Image.fromarray(out).save(os.path.join(ART, name + '.png'))
    return mask.width / mask.height


S = 4
# ---- potted plant: a tapering pot, five stems, leaves along them
W, H = 440 * S, 520 * S; im = Image.new('L', (W, H), 0); d = ImageDraw.Draw(im)
d.polygon([(150 * S, 400 * S), (290 * S, 400 * S), (270 * S, 510 * S), (170 * S, 510 * S)], fill=255); d.rectangle((138 * S, 380 * S, 302 * S, 404 * S), fill=255); d.rectangle((60 * S, 508 * S, 380 * S, 520 * S), fill=255)
for k, (dx, top) in enumerate([(-120, 150), (-60, 70), (0, 30), (70, 80), (130, 160)]):
    x0, y0, x1, y1 = 220, 385, 220 + dx, top
    for i in range(30):
        t = i / 29.0; x = x0 + (x1 - x0) * t ** 0.8; y = y0 + (y1 - y0) * t
        d.ellipse(((x - 4) * S, (y - 4) * S, (x + 4) * S, (y + 4) * S), fill=255)
        if i % 5 == 3:
            for side in (-1, 1):
                a = math.atan2(y1 - y0, x1 - x0) + side * 1.0; L = 46 - 18 * t; cx, cy = x + math.cos(a) * L * 0.5, y + math.sin(a) * L * 0.5
                leaf = Image.new('L', (W, H), 0); ImageDraw.Draw(leaf).ellipse(((cx - L * 0.55) * S, (cy - L * 0.22) * S, (cx + L * 0.55) * S, (cy + L * 0.22) * S), fill=255)
                leaf = leaf.rotate(-math.degrees(a), center=(cx * S, cy * S)); im = Image.fromarray(np.maximum(np.asarray(im), np.asarray(leaf))); d = ImageDraw.Draw(im)
a_plant = save_sil(im.resize((440, 520), Image.LANCZOS), 'sil_plant')

# ---- guitar, leaning a little
W, H = 300 * S, 560 * S; im = Image.new('L', (W, H), 0); d = ImageDraw.Draw(im)
d.ellipse((60 * S, 330 * S, 250 * S, 540 * S), fill=255); d.ellipse((85 * S, 230 * S, 225 * S, 380 * S), fill=255)
d.rectangle((143 * S, 60 * S, 167 * S, 260 * S), fill=255); d.polygon([(132 * S, 14 * S), (178 * S, 14 * S), (172 * S, 66 * S), (138 * S, 66 * S)], fill=255)
for k in range(3):
    d.ellipse((122 * S, (20 + 16 * k) * S, 134 * S, (30 + 16 * k) * S), fill=255); d.ellipse((176 * S, (20 + 16 * k) * S, 188 * S, (30 + 16 * k) * S), fill=255)
d.ellipse((135 * S, 300 * S, 175 * S, 340 * S), fill=0); d.rectangle((120 * S, 440 * S, 190 * S, 452 * S), fill=0)
im = im.rotate(-8, center=(150 * S, 540 * S), expand=False); d = ImageDraw.Draw(im); d.rectangle((20 * S, 540 * S, 290 * S, 560 * S), fill=255)
a_guitar = save_sil(im.resize((300, 560), Image.LANCZOS), 'sil_guitar')

txt = os.path.join(ART, 'stage_art.txt'); lines = [l for l in open(txt, encoding='utf-8').read().splitlines() if not (l.startswith('sil sil_plant') or l.startswith('sil sil_guitar'))]
lines += ['sil sil_plant %.4f' % a_plant, 'sil sil_guitar %.4f' % a_guitar]; open(txt, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')

# ---- the picture: a wall of windows
PW, PH = 720, 1080; im = Image.new('RGB', (PW, PH), (40, 54, 84)); d = ImageDraw.Draw(im); cols, rows = 6, 9; mx, my = 70, 90; cw, ch = (PW - 2 * mx) / cols, (PH - 2 * my) / rows
red = (random.randrange(cols), random.randrange(2, rows - 2))
for j in range(rows):
    for i in range(cols):
        x0, y0 = mx + i * cw + 14, my + j * ch + 14; x1, y1 = x0 + cw - 28, y0 + ch - 28
        col = (196, 52, 40) if (i, j) == red else (236, 184, 104) if random.random() < 0.22 else (24, 32, 54)
        d.rectangle((x0, y0, x1, y1), fill=col); d.line((x0, (y0 + y1) / 2, x1, (y0 + y1) / 2), fill=(40, 54, 84), width=4); d.line(((x0 + x1) / 2, y0, (x0 + x1) / 2, y1), fill=(40, 54, 84), width=4)
d.rectangle((0, 0, PW - 1, PH - 1), outline=(226, 214, 190), width=26)
im = Image.fromarray((np.asarray(im).astype(np.float32) + rng.normal(0, 5, (PH, PW, 1))).clip(0, 255).astype(np.uint8)); im.save(os.path.join(ROOT, 'unity', 'Assets', 'Gen', 'room_poster.png'))
print('wrote sil_plant %.3f, sil_guitar %.3f, room_poster' % (a_plant, a_guitar))
