# -*- coding: utf-8 -*-
"""Turn the generated scenery sheets (work/gen_art) into stage pieces: cut away the sheet's blank ground, punch the black window
openings out as real holes (their glazing bars stay), enlarge, lay paper fibre over the print, and write what Unity needs:
<name>.png (RGBA), <name>_core.png (the same outline in bare card colour, for the cut edge) and stage_art.txt with sizes and
window rectangles in metres.     python -P tools/make_stage_art.py <gen_dir> <out_dir>
"""
import sys, os
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage as ndi

src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(7)
LINES = []


def cut_ground(rgb, tol=34):
    """alpha: everything that is not the blank sheet connected to the picture's border"""
    a = rgb.astype(np.int32); edge = np.concatenate([a[0], a[-1], a[:, 0], a[:, -1]]); bg = np.median(edge, axis=0)
    close = (np.abs(a - bg).sum(axis=2) < tol * 3) & (a.min(axis=2) > 150)
    lab, n = ndi.label(close)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    return ~ndi.binary_dilation(np.isin(lab, list(border)), iterations=2)


def find_windows(rgb, alpha, min_frac=0.004, dark_max=60):
    """black openings -> one rectangle per window (the panes of one window are joined across their glazing bars)"""
    dark = (rgb.max(axis=2) < dark_max) & alpha
    lab, n = ndi.label(ndi.binary_closing(dark, structure=np.ones((15, 15)))); H, W = dark.shape; rects = []
    for i, sl in enumerate(ndi.find_objects(lab), 1):
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if h * w < min_frac * H * W or (lab[sl] == i).mean() < 0.55 or w > 0.5 * W: continue
        rects.append([sl[1].start, sl[0].start, sl[1].stop, sl[0].stop])
    return dark, rects


def fibre(H, W, strength):
    g = rng.normal(0, 1, (H // 4 + 1, W // 4 + 1)).astype(np.float32); g = np.asarray(Image.fromarray(g).resize((W, H), Image.BICUBIC))
    f = rng.normal(0, 1, (H // 2 + 1, W // 40 + 1)).astype(np.float32); f = np.asarray(Image.fromarray(f).resize((W, H), Image.BICUBIC))     # long fibres along the sheet
    return 1 + strength * (0.45 * g + 0.5 * f + 0.35 * rng.normal(0, 1, (H, W)).astype(np.float32))


def save(name, rgb, alpha, scale, size_m, wins=()):
    h, w = alpha.shape
    big = Image.fromarray(rgb).resize((w * scale, h * scale), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=2, percent=60, threshold=2))
    al = Image.fromarray((alpha * 255).astype(np.uint8)).resize((w * scale, h * scale), Image.LANCZOS)
    col = np.asarray(big).astype(np.float32) / 255.0 * fibre(h * scale, w * scale, 0.045)[..., None]
    Image.fromarray((np.dstack([np.clip(col, 0, 1), np.asarray(al).astype(np.float32)[..., None] / 255.0]) * 255).astype(np.uint8), 'RGBA').save(os.path.join(out, name + '.png'))
    ch, cw = max(8, h * scale // 4), max(8, w * scale // 4)
    core = np.ones((ch, cw, 3), np.float32) * np.array([0.80, 0.74, 0.62]) * fibre(ch, cw, 0.06)[..., None]
    Image.fromarray((np.dstack([np.clip(core, 0, 1), np.asarray(al.resize((cw, ch), Image.LANCZOS)).astype(np.float32)[..., None] / 255.0]) * 255).astype(np.uint8), 'RGBA').save(os.path.join(out, name + '_core.png'))
    LINES.append(f'piece {name} {size_m[0]:.4f} {size_m[1]:.4f}')
    for r in wins: LINES.append(f'win {r[0]:.4f} {r[1]:.4f} {r[2]:.4f} {r[3]:.4f}')
    print(name, (w * scale, h * scale), f'{size_m[0]:.2f} x {size_m[1]:.2f} m', 'windows', len(wins))


def piece(name, file, crop, width_m=None, height_m=None, scale=3, holes=True, extra=()):
    im = Image.open(os.path.join(src, file)).convert('RGB')
    W0, H0 = im.size; x0, y0, x1, y1 = [int(v * (W0 if i % 2 == 0 else H0)) for i, v in enumerate(crop)]
    rgb = np.asarray(im.crop((x0, y0, x1, y1))); alpha = cut_ground(rgb)
    ys, xs = np.where(alpha); rgb, alpha = rgb[ys.min():ys.max() + 1, xs.min():xs.max() + 1], alpha[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    h, w = alpha.shape; ppm = w / width_m if width_m else h / height_m; rects = []
    if holes:
        dark, rects = find_windows(rgb, alpha)
        for (fx0, fy0, fx1, fy1) in extra: rects.append([int(fx0 * w), int(fy0 * h), int(fx1 * w), int(fy1 * h)])      # the ones the search missed, as fractions of the piece
        dk = (rgb.max(axis=2) < 80)
        for (a, b, c, d) in rects:
            win = np.zeros_like(alpha); win[b:d, a:c] = True
            alpha &= ~(win & ndi.binary_erosion(dk, iterations=1))             # only the black glass goes; bars, frames and railings stay as paper
    wins = [((a + (c - a) / 2) / ppm, (h - b - (d - b) / 2) / ppm, (c - a) / ppm, (d - b) / ppm) for (a, b, c, d) in rects]
    wins.sort(key=lambda r: (round(r[1] / 0.45), r[0]))                        # bottom row first, left to right
    save(name, rgb, alpha, scale, (w / ppm, h / ppm), wins)


def silhouette(name, file):
    """a black paper cut-out on white -> black card with alpha, trimmed"""
    g = np.asarray(Image.open(os.path.join(src, file)).convert('L')).astype(np.float32)
    a = np.clip((150 - g) / 60, 0, 1); ys, xs = np.where(a > 0.5)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]; h, w = a.shape
    col = np.ones((h, w, 3), np.float32) * np.array([0.075, 0.060, 0.055]) * fibre(h, w, 0.10)[..., None]
    Image.fromarray((np.dstack([np.clip(col, 0, 1), a[..., None]]) * 255).astype(np.uint8), 'RGBA').save(os.path.join(out, name + '.png'))
    LINES.append(f'sil {name} {w / h:.4f}'); print(name, (w, h))


E = os.path.exists
if E(src + '/facade_R.jpg'): piece('facade_R', 'facade_R.jpg', (0.10, 0.03, 0.90, 0.928), width_m=2.05, extra=[(0.552, 0.622, 0.819, 0.711)])
if E(src + '/facade_L.jpg'): piece('facade_L', 'facade_L.jpg', (0.08, 0.10, 0.90, 0.868), width_m=2.05, holes=False)
if E(src + '/far_row.jpg'): piece('far_row', 'far_row.jpg', (0.0, 0.36, 1.0, 0.63), width_m=5.6, scale=2, holes=False)
if E(src + '/lamp_post.jpg'): piece('lamp_post', 'lamp_post.jpg', (0.0, 0.0, 1.0, 1.0), height_m=3.0, scale=2, holes=False)
if E(src + '/sky.jpg'):
    im = Image.open(src + '/sky.jpg').convert('RGB'); im = im.crop((int(im.width * 0.01), 0, int(im.width * 0.985), int(im.height * 0.80))); im = im.resize((im.width * 2, im.height * 2), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32) / 255.0 * fibre(im.height, im.width, 0.03)[..., None]
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(out + '/sky.png'); LINES.append(f'piece sky 5.6000 {5.6 * im.height / im.width:.4f}')
for k in ('lamp', 'clock', 'book', 'umbrella', 'bike', 'bag'):
    if E(f'{src}/sil_{k}.jpg'): silhouette('sil_' + k, f'sil_{k}.jpg')
# backlit parchment for the rooms behind the windows
H, W = 512, 512; yy = np.linspace(0, 1, H)[:, None]
par = (np.array([0.98, 0.80, 0.50])[None, None, :] * (0.78 + 0.22 * (1 - np.abs(yy - 0.45) * 1.4))[..., None]) * fibre(H, W, 0.07)[..., None]
Image.fromarray((np.clip(par, 0, 1) * 255).astype(np.uint8)).save(out + '/parchment.png')
open(out + '/stage_art.txt', 'w').write('\n'.join(LINES) + '\n')
names = [n for n in ('facade_L', 'facade_R', 'lamp_post', 'sil_lamp', 'sil_bike') if E(f'{out}/{n}.png')]
tiles = [Image.open(f'{out}/{n}.png') for n in names]; Hh = 900
tiles = [t.resize((max(1, int(t.width * Hh / t.height)), Hh), Image.LANCZOS) for t in tiles]
sh = Image.new('RGB', (sum(t.width for t in tiles) + 20 * (len(tiles) + 1), Hh + 40), (190, 120, 40)); x = 20
for t in tiles: sh.paste(t, (x, 20), t); x += t.width + 20
sh.save(out + '/_check.jpg', quality=88)
