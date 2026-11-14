# -*- coding: utf-8 -*-
"""Three upload covers, each at 16:9 (1920x1080) and 4:3 (1440x1080), from clean frames of the film (unity/Capture/cut5, no captions).
    python -P tools/make_covers.py  ->  publish/covers/封面_{A,B,C}_{16x9,4x3}.jpg and publish/covers/总览.jpg
A  她也曾活过啊      the singer, close, singing (S24)
B  两秒钟            the stagehand's black legs over the red sheet (S36): the two seconds it takes to wipe her away
C  显像管的外侧      the ghost lamp burning, the singer far on the empty stage (S56)
No blood, no fall, no body on any cover: the covers say what the film is about without showing it."""
import os, numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); OUT = os.path.join(ROOT, 'publish', 'covers'); os.makedirs(OUT, exist_ok=True)
MV = 'E:/sucai/_tools/mvkit/engine/fonts/'; WF = 'C:/Windows/Fonts/'
BONE, RED, INK = (244, 238, 228), (205, 28, 48), (12, 10, 14)


def serif_sc(px, w=900):
    f = ImageFont.truetype(WF + 'NotoSerifSC-VF.ttf', px); f.set_variation_by_axes([w]); return f


def sans_sc(px, w=700):
    f = ImageFont.truetype(WF + 'NotoSansSC-VF.ttf', px)
    try: f.set_variation_by_axes([w])
    except Exception: pass
    return f


def ja(px, name='ShipporiMinchoB1-ExtraBold.ttf'): return ImageFont.truetype(MV + name, px)


def frame(shot, f, aspect, cx):
    im = Image.open(os.path.join(ROOT, 'unity', 'Capture', 'cut5', f'S{shot:02d}', f'f_{f:05d}.jpg')).convert('RGB'); W, H = im.size
    if aspect == '4x3':
        w = int(H * 4 / 3); x0 = int(min(max(0, cx * W - w / 2), W - w)); im = im.crop((x0, 0, x0 + w, H)).resize((1440, 1080), Image.LANCZOS)
    else: im = im.resize((1920, 1080), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32) / 255.0; a = np.clip((a - 0.5) * 1.08 + 0.5 + 0.01, 0, 1)                       # a touch more contrast
    h, w = a.shape[:2]; yy, xx = np.mgrid[0:h, 0:w]; r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2); a *= (1 - 0.32 * np.clip(r - 0.55, 0, 1))[..., None]
    return a


def shade(a, kind, x0=0.0, x1=1.0, y0=0.0, y1=1.0, strength=0.75):
    """darken a side of the picture so the words read: 'right', 'left', 'bottom', 'top'"""
    h, w = a.shape[:2]; yy, xx = np.mgrid[0:h, 0:w] / np.array([h, w])[:, None, None]
    if kind == 'right': g = np.clip((xx - x0) / max(1e-3, x1 - x0), 0, 1)
    elif kind == 'left': g = np.clip((x1 - xx) / max(1e-3, x1 - x0), 0, 1)
    elif kind == 'bottom': g = np.clip((yy - y0) / max(1e-3, y1 - y0), 0, 1)
    else: g = np.clip((y1 - yy) / max(1e-3, y1 - y0), 0, 1)
    g = g * g * (3 - 2 * g); return a * (1 - strength * g)[..., None]


class Canvas:
    def __init__(self, a):
        self.im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).convert('RGBA'); self.W, self.H = self.im.size
        self.txt = Image.new('RGBA', self.im.size, (0, 0, 0, 0)); self.sh = Image.new('L', self.im.size, 0)

    def text(self, xy, s, f, fill=BONE, anchor='la', stroke=0, stroke_fill=INK, shadow=True, spacing=0.0):
        d = ImageDraw.Draw(self.txt); ds = ImageDraw.Draw(self.sh); x, y = xy
        if spacing:
            widths = [d.textlength(c, font=f) for c in s]; total = sum(widths) + spacing * f.size * (len(s) - 1)
            if anchor[0] == 'm': x -= total / 2
            elif anchor[0] == 'r': x -= total
            for c, cw in zip(s, widths):
                if shadow: ds.text((x + f.size * 0.03, y + f.size * 0.05), c, font=f, fill=255, anchor='l' + anchor[1], stroke_width=max(stroke, f.size // 14), stroke_fill=255)
                d.text((x, y), c, font=f, fill=fill, anchor='l' + anchor[1], stroke_width=stroke, stroke_fill=stroke_fill); x += cw + spacing * f.size
            return
        if shadow: ds.text((x + f.size * 0.03, y + f.size * 0.05), s, font=f, fill=255, anchor=anchor, stroke_width=max(stroke, f.size // 14), stroke_fill=255)
        d.text((x, y), s, font=f, fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=stroke_fill)

    def vtext(self, x, y, s, f, fill=BONE, step=1.08, stroke=0):
        """a vertical line, top to bottom, centred on x"""
        for k, c in enumerate(s):
            cy = y + k * f.size * step + f.size / 2
            if c in 'ー〜': self.rot(x, cy, c, f, fill)
            else: self.text((x, cy), c, f, fill=fill, anchor='mm', stroke=stroke)

    def rot(self, x, y, c, f, fill):
        S = int(f.size * 1.6); g = Image.new('RGBA', (S, S), (0, 0, 0, 0)); ImageDraw.Draw(g).text((S / 2, S / 2), c, font=f, fill=fill, anchor='mm'); g = g.rotate(-90, resample=Image.BICUBIC)
        self.txt.alpha_composite(g, (int(x - S / 2), int(y - S / 2)))

    def bar(self, box, fill):
        ImageDraw.Draw(self.txt).rectangle(box, fill=fill)

    def done(self, path):
        sh = self.sh.filter(ImageFilter.GaussianBlur(self.H * 0.008)); base = Image.new('RGBA', self.im.size, (0, 0, 0, 0)); base.putalpha(sh.point(lambda v: int(v * 0.78)))
        out = Image.alpha_composite(Image.alpha_composite(self.im, base), self.txt).convert('RGB'); out.save(path, quality=93); return out


def cover_A(asp):
    a = frame(24, 1340, asp, 0.375)
    if asp == '16x9':
        a = shade(a, 'right', 0.42, 0.72, strength=0.80); c = Canvas(a); W, H = c.W, c.H
        c.vtext(W * 0.925, H * 0.075, '生きていたんだよな', ja(int(H * 0.085)), step=1.04)
        c.bar((W * 0.548, H * 0.385, W * 0.554, H * 0.585), RED)
        c.text((W * 0.57, H * 0.38), '她也曾', serif_sc(int(H * 0.115)), anchor='la'); c.text((W * 0.57, H * 0.495), '活过啊', serif_sc(int(H * 0.115)), anchor='la')
        c.text((W * 0.57, H * 0.67), 'あいみょん《生きていたんだよな》', ja(int(H * 0.034), 'ShipporiMinchoB1-Bold.ttf'), fill=(230, 224, 214))
        c.text((W * 0.57, H * 0.725), '纸剧场 MV · 中日双语字幕', sans_sc(int(H * 0.030)), fill=(225, 220, 210))
        c.text((W * 0.57, H * 0.08), '显像管的外侧', serif_sc(int(H * 0.040), 700), fill=(240, 234, 224))
    else:
        a = shade(a, 'right', 0.50, 0.78, strength=0.84); c = Canvas(a); W, H = c.W, c.H
        c.vtext(W * 0.925, H * 0.07, '生きていたんだよな', ja(int(H * 0.080)), step=1.04)
        c.bar((W * 0.588, H * 0.385, W * 0.595, H * 0.585), RED)
        c.text((W * 0.612, H * 0.38), '她也曾', serif_sc(int(H * 0.108)), anchor='la'); c.text((W * 0.612, H * 0.488), '活过啊', serif_sc(int(H * 0.108)), anchor='la')
        c.text((W * 0.612, H * 0.66), 'あいみょん', ja(int(H * 0.036), 'ShipporiMinchoB1-Bold.ttf'), fill=(232, 226, 216))
        c.text((W * 0.612, H * 0.71), '纸剧场 MV · 中日双语', sans_sc(int(H * 0.030)), fill=(225, 220, 210))
        c.text((W * 0.612, H * 0.08), '显像管的外侧', serif_sc(int(H * 0.040), 700), fill=(240, 234, 224))
    return c.done(os.path.join(OUT, f'封面_A_{asp}.jpg'))


def cover_B(asp):
    a = frame(36, 1925, asp, 0.42)
    if asp == '16x9':
        a = shade(a, 'right', 0.38, 0.72, strength=0.72); a = shade(a, 'top', y0=0.0, y1=0.35, strength=0.35); c = Canvas(a); W, H = c.W, c.H
        c.text((W * 0.94, H * 0.17), '2秒で拭き取られてしまう', ja(int(H * 0.040), 'ShipporiMinchoB1-Bold.ttf'), fill=(236, 230, 220), anchor='ra', spacing=0.06)
        c.text((W * 0.94, H * 0.47), '两秒钟', serif_sc(int(H * 0.215)), anchor='rs', fill=BONE)
        c.text((W * 0.94, H * 0.62), '她就被擦干净了', serif_sc(int(H * 0.082)), anchor='rs', fill=BONE)
        c.bar((W * 0.60, H * 0.655, W * 0.94, H * 0.662), RED)
        c.text((W * 0.94, H * 0.73), 'あいみょん《生きていたんだよな》 纸剧场 MV', sans_sc(int(H * 0.030)), anchor='ra', fill=(228, 222, 212))
        c.text((W * 0.94, H * 0.785), '在显像管的外侧，我们都是看客', sans_sc(int(H * 0.030), 500), anchor='ra', fill=(210, 204, 194))
    else:
        a = shade(a, 'right', 0.30, 0.70, strength=0.75); c = Canvas(a); W, H = c.W, c.H
        c.text((W * 0.94, H * 0.15), '2秒で拭き取られてしまう', ja(int(H * 0.040), 'ShipporiMinchoB1-Bold.ttf'), fill=(236, 230, 220), anchor='ra', spacing=0.06)
        c.text((W * 0.94, H * 0.46), '两秒钟', serif_sc(int(H * 0.215)), anchor='rs', fill=BONE)
        c.text((W * 0.94, H * 0.61), '她就被擦干净了', serif_sc(int(H * 0.082)), anchor='rs', fill=BONE)
        c.bar((W * 0.47, H * 0.645, W * 0.94, H * 0.652), RED)
        c.text((W * 0.94, H * 0.72), 'あいみょん《生きていたんだよな》', sans_sc(int(H * 0.032)), anchor='ra', fill=(228, 222, 212))
        c.text((W * 0.94, H * 0.775), '纸剧场 MV · 中日双语字幕', sans_sc(int(H * 0.030), 500), anchor='ra', fill=(210, 204, 194))
    return c.done(os.path.join(OUT, f'封面_B_{asp}.jpg'))


def cover_C(asp):
    a = frame(56, 3095, asp, 0.52)
    a = shade(a, 'right', 0.70, 0.95, strength=0.55); a = shade(a, 'bottom', y0=0.70, y1=1.0, strength=0.6); c = Canvas(a); W, H = c.W, c.H
    big = int(H * 0.118); x = W * (0.915 if asp == '16x9' else 0.905)
    c.vtext(x, H * 0.06, '显像管的外侧', serif_sc(big), step=1.10)
    c.bar((x - big * 0.75, H * 0.06 + big * 0.2, x - big * 0.75 + 5, H * 0.06 + big * 6.4), RED)
    c.text((W * 0.5 if asp == '16x9' else W * 0.46, H * 0.865), 'ブラウン管の外側で 生きていたんだよな', ja(int(H * 0.040), 'ShipporiMinchoB1-Bold.ttf'), anchor='ma', fill=(236, 230, 220))
    c.text((W * 0.5 if asp == '16x9' else W * 0.46, H * 0.93), 'あいみょん　纸剧场 × 全曲手K · Unity 原创动画 MV', sans_sc(int(H * 0.030)), anchor='ma', fill=(220, 214, 204))
    return c.done(os.path.join(OUT, f'封面_C_{asp}.jpg'))


ims = []
for fn in (cover_A, cover_B, cover_C):
    for asp in ('16x9', '4x3'): ims.append(fn(asp))
sheet = Image.new('RGB', (960 + 720 + 30, 3 * 540 + 40), (40, 40, 40))
for i in range(3):
    sheet.paste(ims[2 * i].resize((960, 540)), (10, 10 + i * 550)); sheet.paste(ims[2 * i + 1].resize((720, 540)), (980, 10 + i * 550))
sheet.save(os.path.join(OUT, '总览.jpg'), quality=88); print('covers:', sorted(f for f in os.listdir(OUT) if f.startswith('封面')))
