# -*- coding: utf-8 -*-
"""E: finish for the ten-second sample (bars 25-29, 53.0 s - 64.4 s): the room, then the first chorus on the stage.
    python -P tools/finish_sample.py <out.mp4>
Soft cuts of four frames on the beat, fade in and out, the bilingual lyric at the bottom (static, fades only), the song brought to
-14 LUFS. Halation, vignette and grain are already in the frames (each scene's post volume)."""
import sys, os, subprocess
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CAP = os.path.join(ROOT, 'unity', 'Capture')
out = sys.argv[1]
FPS = 24
F0, F1 = 1272, 1545
CUTS = [1326, 1377, 1403, 1429, 1454, 1481]          # room | lin | four windows | the street
SOFT = 4
SEGS = [os.path.join(CAP, 'room25'), os.path.join(CAP, 's21_24')] + [os.path.join(CAP, 's%d' % (22 + i)) for i in range(5)]
FADE = 12
LINES = [(55.48, 59.92, '生きて 生きて 生きて 生きて 生きて', '活过 活过 活过 活过 活过'),
         (59.92, 64.30, '生きて 生きて 生きていたんだよな', '活过 活过 她曾活过啊')]


def font(names, size):
    for n in names:
        p = os.path.join('C:/Windows/Fonts', n)
        if os.path.exists(p): return ImageFont.truetype(p, size)
    raise SystemExit('no font among ' + str(names))


def caption_layers(W, H):
    fj = font(['YuGothB.ttc', 'YuGothM.ttc', 'meiryob.ttc', 'msgothic.ttc'], int(H * 0.036)); fz = font(['msyhbd.ttc', 'msyh.ttc', 'simhei.ttf'], int(H * 0.030))
    layers = []
    for (_, _, jp, zh) in LINES:
        im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sh = Image.new('L', (W, H), 0); d = ImageDraw.Draw(im); ds = ImageDraw.Draw(sh)
        for txt, f, y in [(jp, fj, H * 0.855), (zh, fz, H * 0.908)]:
            w = d.textlength(txt, font=f); x = (W - w) / 2
            ds.text((x, y + 3), txt, font=f, fill=255, stroke_width=6, stroke_fill=255)
            d.text((x, y), txt, font=f, fill=(244, 240, 230, 255))
        sh = sh.filter(ImageFilter.GaussianBlur(H * 0.008))
        base = Image.new('RGBA', (W, H), (0, 0, 0, 0)); base.putalpha(sh.point(lambda v: int(v * 0.62)))
        layers.append(np.asarray(Image.alpha_composite(base, im)).astype(np.float32) / 255.0)
    return layers


def load(d, f):
    p = os.path.join(d, f'f_{f:05d}.jpg')
    return np.asarray(Image.open(p).convert('RGB')).astype(np.float32) / 255.0 if os.path.exists(p) else None


def smooth(u):
    u = min(1.0, max(0.0, u)); return u * u * (3 - 2 * u)


first = load(SEGS[0], F0); H, W = first.shape[:2]
caps = caption_layers(W, H)
audio = os.path.join(ROOT, 'audio', 'song.wav'); t0 = F0 / FPS; dur = (F1 - F0) / FPS
cmd = ['ffmpeg', '-y', '-loglevel', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', f'{W}x{H}', '-r', str(FPS), '-i', '-',
       '-ss', f'{t0:.4f}', '-t', f'{dur:.4f}', '-i', audio,
       '-af', f'loudnorm=I=-14:TP=-1.5:LRA=11,afade=t=in:st=0:d=0.45,afade=t=out:st={dur - 0.55:.3f}:d=0.55', '-ar', '48000',
       '-c:v', 'libx264', '-preset', 'slow', '-crf', '15', '-pix_fmt', 'yuv420p', '-colorspace', 'bt709', '-color_primaries', 'bt709', '-color_trc', 'bt709',
       '-c:a', 'aac', '-b:a', '256k', '-shortest', out]
p = subprocess.Popen(cmd, stdin=subprocess.PIPE)
miss = 0
for f in range(F0, F1):
    seg = sum(1 for c in CUTS if f >= c)
    img = load(SEGS[seg], f)
    for ci, c in enumerate(CUTS):
        if c - SOFT // 2 <= f < c + SOFT // 2:
            a, b = load(SEGS[ci], f), load(SEGS[ci + 1], f)
            if a is not None and b is not None: img = a + (b - a) * ((f - (c - SOFT // 2) + 0.5) / SOFT)
    if img is None: miss += 1; img = np.zeros((H, W, 3), np.float32)
    k = min(smooth((f - F0) / FADE), smooth((F1 - 1 - f) / FADE))
    img = img * k
    t = f / FPS
    for (c0, c1, _, _), lay in zip(LINES, caps):
        al = min(smooth((t - c0) / 0.25), smooth((c1 - t) / 0.25)) * k
        if al > 0: img = img * (1 - lay[..., 3:4] * al) + lay[..., :3] * lay[..., 3:4] * al
    p.stdin.write((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8).tobytes())
p.stdin.close(); p.wait()
print('wrote', out, 'frames', F1 - F0, 'missing', miss, 'size', W, H)
