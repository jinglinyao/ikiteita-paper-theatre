# -*- coding: utf-8 -*-
"""Assemble the film from its shots.
    python -P tools/finish_film.py <shots_dir> <out.mp4> [--from S] [--to S] [--crf N] [--captions file.json] [--label]
<shots_dir> is unity/Capture/<shots_dir>/Snn/f_#####.jpg (film frame numbers, 24 a second), every shot rendered with handles.
Joins come from docs/film_v5.json: a soft cut is a four-frame mix on the cut, a one-beat dissolve twelve frames, a matched
dissolve ten; the film fades in from black and out to black. The title sits in the first wide shot of the room; the last
seconds are black with the credits. Captions are optional and come from a file the user supplies (start, end, line 1, line 2);
they are static and only fade. The song is brought to -14 LUFS. --label burns the shot number into a corner (for review cuts)."""
import sys, os, json, subprocess, argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); sys.path.insert(0, os.path.join(ROOT, 'tools'))
ap = argparse.ArgumentParser(); ap.add_argument('shots'); ap.add_argument('out'); ap.add_argument('--from', dest='s0', type=int, default=1); ap.add_argument('--to', dest='s1', type=int, default=83)
ap.add_argument('--crf', type=int, default=17); ap.add_argument('--captions', default=''); ap.add_argument('--label', action='store_true'); a = ap.parse_args()
CAP = os.path.join(ROOT, 'unity', 'Capture', a.shots); FPS = 24
d = json.load(open(os.path.join(ROOT, 'docs', 'film_v5.json'), encoding='utf-8')); shots = d['shots']
HALF = {'软切': 2, '微距 软切': 2, '纸带划过': 2, '叠化一拍': 6, '对位叠化': 8}
TITLE = ('显像管的外侧', 9.13, 12.2)                                             # text, in, out (seconds): inside S04
TITLE_SUB = '生きていたんだよな'
CREDITS = ['「生きていたんだよな」 あいみょん', '非官方影像 · 纸剧场与角色动画为原创制作', '电视里的画面与部分纸面插图由 AI 生成（阿里云 万相）', '模型与贴图署名见随片文档']
# cuts that hurt: the picture tears for a few frames, like a tube losing its signal (shot number -> strength). Few, and only where the story breaks.
TEAR = {11: 1.0, 24: 0.55, 31: 0.6, 37: 0.8, 54: 0.7, 58: 0.45, 70: 0.5}
GRADE = {5: 0.60, 52: 0.60}                                                    # the two close shots of the desk come out a stop too bright next to the dark room
TAIL = 4.5                                                                     # seconds of black after the song for the credits and the helpline to be read
HELP = '如果你正在经历困难，请联系身边的人或当地的心理援助热线。'               # the number itself is added by hand before publishing


def font(names, size):
    for n in names:
        p = os.path.join('C:/Windows/Fonts', n)
        if os.path.exists(p): return ImageFont.truetype(p, size)
    raise SystemExit('no font among ' + str(names))


def smooth(u):
    u = min(1.0, max(0.0, u)); return u * u * (3 - 2 * u)


_cache = {}


def load(n, f):
    key = (n, f)
    if key in _cache: return _cache[key]
    p = os.path.join(CAP, 'S%02d' % n, 'f_%05d.jpg' % f)
    im = np.asarray(Image.open(p).convert('RGB')).astype(np.float32) / 255.0 if os.path.exists(p) else None
    if len(_cache) > 8: _cache.clear()
    _cache[key] = im; return im


def text_layer(W, H, lines, y0, size, weight=1.0, spacing=1.5):
    """lines centred, a soft dark halo behind them; returns float RGBA"""
    f = font(['msyhbd.ttc', 'YuGothB.ttc', 'msyh.ttc'], int(H * size)); im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sh = Image.new('L', (W, H), 0); dr = ImageDraw.Draw(im); ds = ImageDraw.Draw(sh); y = H * y0
    for t in lines:
        w = dr.textlength(t, font=f); x = (W - w) / 2
        ds.text((x, y + 2), t, font=f, fill=255, stroke_width=max(2, int(H * 0.004)), stroke_fill=255); dr.text((x, y), t, font=f, fill=(244, 240, 230, 255)); y += H * size * spacing
    sh = sh.filter(ImageFilter.GaussianBlur(H * 0.008)); base = Image.new('RGBA', (W, H), (0, 0, 0, 0)); base.putalpha(sh.point(lambda v: int(v * 0.6 * weight)))
    return np.asarray(Image.alpha_composite(base, im)).astype(np.float32) / 255.0


def two_line(W, H, big, small, y0, sb, ss, weight=1.0, bigfont=('yumindb.ttf', 'YuGothB.ttc', 'msyhbd.ttc'), smallfont=('msyh.ttc', 'msyhbd.ttc')):
    """a large line (Japanese, mincho) with a small line (Chinese) under it, centred, a soft dark halo behind; float RGBA"""
    fb = font(list(bigfont), int(H * sb)); fs = font(list(smallfont), int(H * ss)); im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sh = Image.new('L', (W, H), 0); dr = ImageDraw.Draw(im); ds = ImageDraw.Draw(sh); y = H * y0
    for (t, f, col, adv) in ((big, fb, (246, 242, 232, 255), sb * 1.32), (small, fs, (222, 216, 204, 255), ss * 1.4)):
        if not t: continue
        w = dr.textlength(t, font=f); x = (W - w) / 2
        ds.text((x, y + 2), t, font=f, fill=255, stroke_width=max(2, int(H * 0.004)), stroke_fill=255); dr.text((x, y), t, font=f, fill=col); y += H * adv
    sh = sh.filter(ImageFilter.GaussianBlur(H * 0.008)); base = Image.new('RGBA', (W, H), (0, 0, 0, 0)); base.putalpha(sh.point(lambda v: int(v * 0.62 * weight)))
    return np.asarray(Image.alpha_composite(base, im)).astype(np.float32) / 255.0


def tear(img, f, cut, k):
    """the picture loses its hold for a few frames around a cut: bands of lines slip sideways, the colours part, it dips"""
    d = abs(f - cut); a = k * max(0.0, 1 - d / 4.0)
    if a <= 0: return img
    H, W = img.shape[:2]; rng = np.random.default_rng(cut * 131 + f); out = img.copy(); y = 0
    while y < H:
        h = int(rng.integers(H // 40, H // 9)); sx = int((rng.random() - 0.5) * 2 * a * W * (0.035 if rng.random() < 0.4 else 0.006)); out[y:y + h] = np.roll(img[y:y + h], sx, axis=1); y += h
    c = int(a * W * 0.004) + 1; out[..., 0] = np.roll(out[..., 0], c, axis=1); out[..., 2] = np.roll(out[..., 2], -c, axis=1)
    return out * (1 - 0.18 * a) + np.array([0.05, 0.0, 0.0], np.float32) * a


def cap_layer(W, H, ja, zh, ro):
    """the small lines of a caption: the Chinese (YaHei Bold) and under it, at the very bottom of the screen, the reading in Latin letters.
    Solid white with a dark outline and a soft shadow, so that they read on any picture (v1 was thin and grey and all but vanished)."""
    fz = font(['msyhbd.ttc', 'msyh.ttc'], int(H * 0.036)); fr = font(['seguisb.ttf', 'segoeuib.ttf', 'arialbd.ttf'], int(H * 0.026))
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sh = Image.new('L', (W, H), 0); dr = ImageDraw.Draw(im); ds = ImageDraw.Draw(sh)
    for (t, f, col, yc, sw) in ((zh, fz, (252, 250, 245, 255), 0.862, max(3, int(H * 0.0028))), (ro, fr, (240, 237, 230, 255), 0.935, max(2, int(H * 0.0022)))):
        if not t: continue
        ds.text((W / 2, H * yc + H * 0.002), t, font=f, fill=255, anchor='mm', stroke_width=sw * 4, stroke_fill=255)
        dr.text((W / 2, H * yc), t, font=f, fill=col, anchor='mm', stroke_width=sw, stroke_fill=(10, 9, 12, 255))
    sh = sh.filter(ImageFilter.GaussianBlur(H * 0.006)); base = Image.new('RGBA', (W, H), (0, 0, 0, 0)); base.putalpha(sh.point(lambda v: int(v * 0.72)))
    return np.asarray(Image.alpha_composite(base, im)).astype(np.float32) / 255.0


def crop_layer(layer):
    """a full-frame RGBA layer kept as its own small rectangle (8 bit): 55 captions as whole float frames were three gigabytes"""
    al = layer[..., 3]; ys = np.where(al.max(1) > 0.004)[0]; xs = np.where(al.max(0) > 0.004)[0]
    if len(ys) == 0: return None
    y0, y1, x0, x1 = ys[0], ys[-1] + 1, xs[0], xs[-1] + 1; return (x0, y0, (np.clip(layer[y0:y1, x0:x1], 0, 1) * 255 + 0.5).astype(np.uint8))


def over_crop(frame, cl, k):
    if cl is None or k <= 0: return frame
    x0, y0, a8 = cl; a = a8.astype(np.float32) / 255.0; al = a[..., 3:4] * k; h, w = a.shape[:2]; frame = frame.copy(); frame[y0:y0 + h, x0:x0 + w] = frame[y0:y0 + h, x0:x0 + w] * (1 - al) + a[..., :3] * al
    return frame


def over(frame, layer, k):
    if k <= 0: return frame
    al = layer[..., 3:4] * k; return frame * (1 - al) + layer[..., :3] * al


sel = [s for i, s in enumerate(shots) if a.s0 <= i + 1 <= a.s1]; F0, F1 = sel[0]['f0'], sel[-1]['f1']
first = None
for i, s in enumerate(shots):
    if a.s0 <= i + 1 <= a.s1:
        first = load(i + 1, s['f0'] + 1)
        if first is not None: break
if first is None: raise SystemExit('no frames under ' + CAP)
H, W = first.shape[:2]; black = np.zeros((H, W, 3), np.float32)
title = two_line(W, H, TITLE[0], TITLE_SUB, 0.80, 0.058, 0.026, 0.85, ('STZHONGS.TTF', 'simsunb.ttf', 'msyhbd.ttc'), ('YuGothM.ttc', 'YuGothR.ttc', 'msyh.ttc')); credits = text_layer(W, H, CREDITS, 0.36, 0.026); helpl = text_layer(W, H, [HELP], 0.62, 0.032)
caps = []; t0s, t1s = F0 / 24.0, F1 / 24.0; lyr = None
if a.captions:
    from lyric_type import Lyrics
    crow = json.load(open(a.captions, encoding='utf-8')); lyr = Lyrics(crow, W, H, FPS)
    for c in crow:
        if c[1] > t0s - 1 and c[0] < t1s + 1 and any(c[2:]): caps.append((c[0], c[1], crop_layer(cap_layer(W, H, '', c[3] if len(c) > 3 else '', c[4] if len(c) > 4 else ''))))
labf = font(['consola.ttf', 'arial.ttf'], int(H * 0.035))
audio = os.path.join(ROOT, 'audio', 'song.wav'); t0 = F0 / FPS; dur = (F1 - F0) / FPS; TAILF = int(TAIL * FPS) if a.s1 >= len(shots) else 0
cmd = ['ffmpeg', '-y', '-loglevel', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', f'{W}x{H}', '-r', str(FPS), '-i', '-', '-ss', f'{t0:.4f}', '-t', f'{dur:.4f}', '-i', audio,
       '-af', f'loudnorm=I=-14:TP=-1.5:LRA=11,apad=pad_dur={TAILF / FPS:.3f}', '-ar', '48000', '-c:v', 'libx264', '-preset', 'slow', '-crf', str(a.crf), '-pix_fmt', 'yuv420p',
       '-colorspace', 'bt709', '-color_primaries', 'bt709', '-color_trc', 'bt709', '-c:a', 'aac', '-b:a', '256k', '-movflags', '+faststart', a.out]
pipe = subprocess.Popen(cmd, stdin=subprocess.PIPE); missing = {}
idx = {s['f0']: i for i, s in enumerate(shots)}
for f in range(F0, F1 + TAILF):
    k = max(i for i, s in enumerate(shots) if s['f0'] <= f); s = shots[k]; n = k + 1; t = f / FPS
    cur = load(n, f) if s['type'] in 'RXTAL' else black
    if cur is not None and n in GRADE: cur = cur * GRADE[n]
    if cur is None: missing[n] = missing.get(n, 0) + 1; cur = black
    img = cur
    # the join into this shot and the join out of it
    for (j, cut) in ((k, s['f0']), (k + 1, s['f1'])):
        if j <= 0 or j >= len(shots): continue
        join = shots[j].get('join') or ''; hw = HALF.get(join, 0)
        if join == '淡出': hw = 18
        if hw == 0 or abs(f - cut) >= hw: continue
        other_n = j if j != k else k                                           # 1-based number of the neighbour: before the cut it is shot j (index j-1)
        nb = shots[j - 1] if j == k else shots[j]; nb_n = j if j == k else j + 1
        ob = load(nb_n, f) if nb['type'] in 'RXTAL' else black
        if ob is not None and nb_n in GRADE: ob = ob * GRADE[nb_n]
        if ob is None: ob = black
        u = smooth((f - cut + hw) / (2.0 * hw))                                # 0 the earlier shot, 1 the later
        img = ob * (1 - u) + cur * u if j == k else cur * (1 - u) + ob * u
    if lyr is not None: img = lyr.draw(img, t, f, s['type'], n)                            # the big lyric is part of the picture: it tears with it
    for (tn, tk) in TEAR.items():
        tc = shots[tn - 1]['f0']
        if abs(f - tc) < 4 and a.s0 <= tn <= a.s1: img = tear(img, f, tc, tk)
    if f < 8: img = img * smooth(f / 8.0)                                    # out of black
    if TITLE[1] <= t <= TITLE[2] + 0.8: img = over(img, title, smooth((t - TITLE[1]) / 0.8) * (1 - smooth((t - TITLE[2]) / 0.8)))
    if s['type'] == '-':
        tt = (f - s['f0']) / FPS; kk = smooth((tt - 0.2) / 0.5) * (1 - smooth((tt - ((s['f1'] + TAILF - s['f0']) / FPS - 0.6)) / 0.5)); img = over(over(img, credits, kk), helpl, kk)
    for (c0, c1, lay) in caps:
        if c0 - 0.10 <= t <= c1 + 0.12: img = over_crop(img, lay, smooth((t - c0 + 0.10) / 0.18) * (1 - smooth((t - c1 + 0.06) / 0.18)))
    out8 = (np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)
    if a.label:
        pil = Image.fromarray(out8); ImageDraw.Draw(pil).text((int(W * 0.012), int(H * 0.015)), f"{s['id']}  {int(t // 60)}:{t % 60:05.2f}", font=labf, fill=(255, 235, 120), stroke_width=2, stroke_fill=(0, 0, 0)); out8 = np.asarray(pil)
    pipe.stdin.write(out8.tobytes())
pipe.stdin.close(); pipe.wait()
print('wrote', a.out, f'{W}x{H}', f'{dur:.2f} s', 'missing frames by shot:', missing if missing else 'none')
