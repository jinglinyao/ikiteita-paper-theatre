# -*- coding: utf-8 -*-
"""The big Japanese lyric, after the six emotional typographies of the unravel film (engine/scenes/lyrics6.js), rebuilt for this one.
v2 (2026-11-14, the user: too small, not painful enough): every style larger (never under 6.4 % of the frame height), and the pain is in
the motion of the glyphs themselves:
    drum        heavy gothic; every glyph lands with a punch and a red flash, then cracks (a crimson fissure) and dims
    pain        mincho extra bold; each glyph arrives red-hot and settles to bone, is cut through and its halves keep drifting apart over
                an open crimson seam; a 12 fps shiver, colour fringes, scratches across the line, blood running from some glyphs
    prayer      old mincho bold; vertical columns, slow fade and rise, a pale tear running from a glyph or two
    depression  gothic medium; cold, wide tracking, comes into focus from blur, sinks; a glyph or two flickers like a dying tube
    helpless    hand-written (Klee); typed behind a caret, some glyphs droop, a glyph or two flickers
    anger       mincho black, very large, crimson, stamped on a torn sheet of paper with a zoom, a shake and a doubled outline
Two lines are wiped away rather than faded (the red that is wiped off in two seconds). A line is set where the picture is emptiest (left
column, right column, top band, low band), never over the Chinese line and the reading at the bottom, and not where the line before it is
still leaving. Everything is a function of (line, frame): the same frame always gives the same picture."""
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
FD = 'E:/sucai/_tools/mvkit/engine/fonts/'
FONT = {'drum': 'DelaGothicOne-Regular.ttf', 'pain': 'ShipporiMinchoB1-ExtraBold.ttf', 'prayer': 'ZenOldMincho-Bold.ttf', 'depression': 'ZenKakuGothicNew-Medium.ttf', 'helpless': 'KleeOne-SemiBold.ttf', 'anger': 'ZenOldMincho-Black.ttf'}
SIZE = {'drum': 0.096, 'pain': 0.090, 'prayer': 0.080, 'depression': 0.072, 'helpless': 0.078, 'anger': 0.135}
FLOOR = 0.064
TRACK = {'drum': 0.02, 'pain': 0.08, 'prayer': 0.16, 'depression': 0.26, 'helpless': 0.05, 'anger': 0.0}
EXIT = {'prayer': 0.6, 'depression': 0.6, 'pain': 0.45, 'helpless': 0.6, 'drum': 0.08, 'anger': 0.15}
CHROMA = {'pain': 0.020, 'anger': 0.024, 'drum': 0.010, 'depression': 0.008, 'helpless': 0.006, 'prayer': 0.0}
BONE, RED, INK, PAPER, BLOOD = (242, 236, 226), (200, 24, 46), (14, 12, 16), (232, 224, 210), (150, 8, 24)
STYLE_LINES = {'depression': [0, 2, 15, 23, 27, 28, 29, 36, 47], 'pain': [1, 3, 4, 10, 11, 12, 13, 14, 21, 22, 24, 31, 32, 37, 48], 'helpless': [6, 26, 38],
               'drum': [7, 17, 18, 25, 34, 35, 45, 46, 49, 50], 'anger': [5, 8, 9, 39], 'prayer': [16, 19, 20, 30, 33, 40, 41, 42, 43, 44, 51, 52, 53, 54]}
WIPE = {22, 24}
# the close shots of the singer: the side of the frame she leaves free (looked up on the frames of each shot)
FORCE = {24: 'R', 47: 'R', 55: 'R', 65: 'L', 70: 'T', 77: 'L', 79: 'L'}
ZONES = {'L': (0.035, 0.07, 0.28, 0.70), 'R': (0.685, 0.07, 0.28, 0.70), 'T': (0.05, 0.05, 0.90, 0.25), 'B': (0.05, 0.50, 0.90, 0.27)}


def smooth(u):
    u = min(1.0, max(0.0, u)); return u * u * (3 - 2 * u)


def h01(i, k):
    x = np.sin(i * 12.9898 + k * 78.233) * 43758.5453; return float(x - np.floor(x))


def tint(mask, color, alpha):
    """an L mask as a flat-coloured RGBA image"""
    im = Image.new('RGBA', mask.size, color + (0,)); im.putalpha(mask.point(lambda v, a=alpha: int(v * a))); return im


class Lyrics:
    def __init__(self, rows, W, H, fps=24):
        self.W, self.H, self.fps = W, H, fps; self.lines = []; style = {}
        for s, l in STYLE_LINES.items():
            for i in l: style[i] = s
        for i, r in enumerate(rows):
            if not r[2]: continue
            self.lines.append({'i': i, 't0': r[0], 't1': r[1], 'ja': r[2], 'style': style.get(i, 'depression'), 'lay': None})
        self._font = {}; self._sprite = {}; self.zone_until = {}

    def font(self, style, px):
        k = (style, px)
        if k not in self._font: self._font[k] = ImageFont.truetype(FD + FONT[style], px)
        return self._font[k]

    def exit_len(self, ln):
        return 0.7 if ln['i'] in WIPE else EXIT[ln['style']]

    # ---- where: the emptiest of the zones this style may use, judged on the frame the line first appears over
    def place(self, ln, frame, stype='', shot=0):
        H, W = self.H, self.W; st = ln['style']; g = frame.mean(2); small = g[::8, ::8]; gy, gx = np.gradient(small); en = np.abs(gx) + np.abs(gy)
        allow = {'prayer': 'LR', 'depression': 'BTLR', 'pain': 'TBLR', 'drum': 'TB', 'helpless': 'BT', 'anger': 'TB'}[st]; best = None
        if stype == 'L': allow = 'LR'                                                  # a close shot of the singer: beside her, never across her face
        if shot in FORCE: allow = FORCE[shot]                                          # her free side only; a line still leaving it is hurried off
        for z in allow:
            x, y, w, h = ZONES[z]; a = en[int(y * H / 8):int((y + h) * H / 8), int(x * W / 8):int((x + w) * W / 8)]; lum = small[int(y * H / 8):int((y + h) * H / 8), int(x * W / 8):int((x + w) * W / 8)]
            score = float(a.mean()) + 0.05 * float(lum.std()) + (0.0015 if z != allow[0] else 0) + 0.0008 * h01(ln['i'], ord(z))
            if self.zone_until.get(z, -1) > ln['t0'] - 0.05: score += 1.0
            if shot in FORCE and z == FORCE[shot]: score -= 0.5                # the line before is still leaving there
            if best is None or score < best[0]: best = (score, z, float(lum.mean()))
        z = best[1]
        for o in self.lines:
            if o is not ln and o['lay'] is not None and o['lay']['zone'] == z and o['t1'] + self.exit_len(o) > ln['t0'] - 0.12: o['kill'] = min(o.get('kill', 1e9), ln['t0'] - 0.30)
        x, y, w, h = ZONES[z]; vertical = z in 'LR'; n = len(ln['ja']); px = int(H * SIZE[st]); tr = TRACK[st]
        while True:                                                                     # the largest size that fits, never under FLOOR
            step = px * (1 + tr)
            if vertical: per = max(1, int(h * H // step)); cols = (n + per - 1) // per; ok = cols * px * 1.45 <= w * W
            else: per = max(1, int(w * W // step)); cols = (n + per - 1) // per; ok = cols * px * 1.30 <= h * H
            if ok or px <= H * FLOOR: break
            px = max(int(H * FLOOR), int(px * 0.95))
        if not vertical and cols == 2: per = (n + 1) // 2                                   # two even rows rather than a full one and a stub
        pos = []; drip = []
        for k in range(n):
            c, r = divmod(k, per)
            if vertical:
                cx = (x + w / 2) * W + ((cols - 1) / 2 - c) * px * 1.45; top = y * H + max(0.0, h * H - min(n, per) * step) * (0.18 if st == 'prayer' else 0.5); cy = top + (r + 0.5) * step
                drip.append(r == per - 1 or k == n - 1)                                 # blood runs only from the foot of a column
            else:
                rowlen = min(per, n - c * per); left = (x + w / 2) * W - rowlen * step / 2
                if st == 'depression': left = x * W + (0.06 + 0.5 * h01(ln['i'], 3)) * max(0, w * W - rowlen * step)
                cx = left + (r + 0.5) * step; cy = (y + h / 2) * H + (c - (cols - 1) / 2) * px * 1.30
                drip.append(c == cols - 1)
            pos.append((cx, cy))
        ln['lay'] = {'px': px, 'pos': pos, 'drip': drip, 'vertical': vertical, 'zone': z, 'dark': best[2] > 0.60 and st != 'anger', 'shot': shot}
        self.zone_until[z] = ln['t1'] + self.exit_len(ln) + 0.1

    # ---- one glyph: its halo and fill, split in two halves along a cut for the pain style; the bare fill mask for fringes and flashes
    def glyph(self, ln, k, color):
        key = (ln['i'], k, color, ln['lay']['px'], ln['lay']['vertical'])
        if key in self._sprite: return self._sprite[key]
        st = ln['style']; lay = ln['lay']; px = lay['px']; f = self.font(st, px); ch = ln['ja'][k]; pad = int(px * 0.5); S = px + 2 * pad
        turn = lay['vertical'] and ch in 'ー〜「」'                                    # these lie along the line: in a column they are turned with it
        core = Image.new('L', (S, S), 0); ImageDraw.Draw(core).text((S / 2, S / 2), ch, font=f, fill=255, anchor='mm')
        hal = Image.new('L', (S, S), 0); ImageDraw.Draw(hal).text((S / 2, S / 2), ch, font=f, fill=255, anchor='mm', stroke_width=max(3, px // 10), stroke_fill=255)
        ink = None
        if st in ('drum', 'anger'): ink = Image.new('L', (S, S), 0); ImageDraw.Draw(ink).text((S / 2, S / 2), ch, font=f, fill=255, anchor='mm', stroke_width=max(2, px // 22), stroke_fill=255)
        if turn: core, hal = core.rotate(-90, resample=Image.BICUBIC), hal.rotate(-90, resample=Image.BICUBIC); ink = ink.rotate(-90, resample=Image.BICUBIC) if ink else None
        halo_col = PAPER if st == 'anger' else ((245, 240, 232) if lay['dark'] and color != RED else (0, 0, 0))
        img = tint(hal.filter(ImageFilter.GaussianBlur(px * 0.12)), halo_col, 0.80)
        if ink is not None: img = Image.alpha_composite(img, tint(ink, INK, 1.0))
        img = Image.alpha_composite(img, tint(core, color, 1.0)); g = {'core': core, 'S': S, 'seam': None}
        if st == 'pain':
            a = np.asarray(img).copy(); yy, xx = np.mgrid[0:S, 0:S]; sgn = 1 if h01(ln['i'], k) > 0.5 else -1; nrm = np.array([sgn * 0.55, 1.0]); nrm /= np.linalg.norm(nrm)
            d = (xx - S / 2) * nrm[0] + (yy - S / 2) * nrm[1]; A = a.copy(); B = a.copy(); A[d <= 0] = 0; B[d > 0] = 0
            cm = np.asarray(core.filter(ImageFilter.MaxFilter(5))) > 30; seam = np.zeros_like(a); m = (np.abs(d) < max(2.0, px / 26)) & cm; seam[m] = BLOOD + (255,)
            g['parts'] = [(Image.fromarray(A), (nrm[0], nrm[1])), (Image.fromarray(B), (-nrm[0], -nrm[1]))]; g['seam'] = Image.fromarray(seam).filter(ImageFilter.GaussianBlur(0.8))
        else: g['parts'] = [(img, (0.0, 0.0))]
        self._sprite[key] = g; return g

    def put(self, layer, img, cx, cy, sx=1.0, sy=1.0, rot=0.0, blur=0.0, alpha=1.0):
        if sx != 1.0 or sy != 1.0: img = img.resize((max(2, int(img.width * sx)), max(2, int(img.height * sy))), Image.BILINEAR)
        if rot != 0.0: img = img.rotate(rot, resample=Image.BILINEAR, expand=True)
        if blur > 0.6: img = img.filter(ImageFilter.GaussianBlur(blur))
        if alpha < 0.999: img = img.copy(); img.putalpha(img.getchannel('A').point(lambda v, al=alpha: int(v * al)))
        x0, y0 = int(cx - img.width / 2), int(cy - img.height / 2)
        if x0 >= self.W or y0 >= self.H or x0 + img.width <= 0 or y0 + img.height <= 0: return
        layer.alpha_composite(img, (max(0, x0), max(0, y0)), (max(0, -x0), max(0, -y0)))

    def drip(self, d, cx, cy, px, length, col, a):
        """blood (or a tear) running down from under a glyph: a tapered line with a drop at its end, wavering a little"""
        if length < px * 0.05 or a <= 0.01: return
        w0 = max(3.0, px * 0.075); n = 8; pts_l = []; pts_r = []
        for j in range(n + 1):
            u = j / n; y = cy + px * 0.40 + length * u; x = cx + px * 0.035 * np.sin(u * 3.1 + cx * 0.01); w = w0 * (1 - 0.7 * u)
            pts_l.append((x - w / 2, y)); pts_r.append((x + w / 2, y))
        d.polygon(pts_l + pts_r[::-1], fill=col + (int(255 * a),)); r = w0 * 0.75; x, y = pts_l[-1][0] + w0 * 0.15, pts_l[-1][1]
        d.ellipse([x - r, y - r * 0.8, x + r, y + r * 1.2], fill=col + (int(255 * a),))

    def draw(self, img, t, f, stype='', shot=0):
        """img: float RGB 0..1 (H, W, 3); returns it with the lyric of the moment on it"""
        act = [ln for ln in self.lines if ln['t0'] - 0.12 <= t <= ln['t1'] + 0.8]
        if not act: return img
        lo = min(ln['i'] for ln in act)
        if len(self._sprite) > 400: self._sprite = {k: v for k, v in self._sprite.items() if k[0] >= lo}      # the glyphs of lines long gone
        layer = None; t12 = int(t * 12)
        for ln in act:
            st = ln['style']
            if ln['lay'] is None or (shot in FORCE and ln['lay']['shot'] != shot and ln['lay']['zone'] != FORCE[shot]): self.place(ln, img, stype, shot)      # cut to a close shot of her: the line moves to her free side
            lay = ln['lay']; n = len(ln['ja']); px = lay['px']; sung = max(0.5, (ln['t1'] - ln['t0']) * 0.78); life = t - ln['t0']; i = ln['i']
            xin = self.exit_len(ln); out = max(smooth((t - ln['t1']) / xin), smooth((t - ln.get('kill', 1e9)) / 0.22))
            if out >= 1 or life < -0.12: continue
            if layer is None: layer = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0))
            d = ImageDraw.Draw(layer); A = 1 - out; base = INK if lay['dark'] else BONE
            xs_ = [p[0] for p in lay['pos']]; ys_ = [p[1] for p in lay['pos']]; bx0, bx1, by0, by1 = min(xs_) - px * 0.6, max(xs_) + px * 0.6, min(ys_) - px * 0.6, max(ys_) + px * 0.6
            if st == 'anger' and life > -0.02:
                # the torn sheet the words are stamped on: jagged all round, a gouge or two across it
                pa = smooth((life + 0.02) / 0.07) * A; pts = []; per = 2 * ((bx1 - bx0) + (by1 - by0)); m = max(12, int(per / (px * 0.28)))
                for j in range(m):
                    u = j / m * per; jit = (h01(i, j) - 0.5) * px * 0.22
                    if u < bx1 - bx0: pts.append((bx0 + u, by0 + jit))
                    elif u < (bx1 - bx0) + (by1 - by0): pts.append((bx1 + jit, by0 + u - (bx1 - bx0)))
                    elif u < 2 * (bx1 - bx0) + (by1 - by0): pts.append((bx1 - (u - (bx1 - bx0) - (by1 - by0)), by1 + jit))
                    else: pts.append((bx0 + jit, by1 - (u - 2 * (bx1 - bx0) - (by1 - by0))))
                d.polygon(pts, fill=PAPER + (int(238 * pa),))
                for j in range(2):
                    gy0 = by0 + (by1 - by0) * (0.25 + 0.45 * j); d.line([(bx0 + px * 0.2, gy0 + px * 0.25), (bx1 - px * 0.2, gy0 - px * 0.15)], fill=(120, 110, 100, int(150 * pa)), width=max(2, px // 30))
            for k in range(n):
                tk = ln['t0'] + (sung * k / max(1, n - 1) if n > 1 else 0) * (0.25 if st in ('prayer', 'depression') else 1.0); a = t - tk
                if a < 0: continue
                cx, cy = lay['pos'][k]; alpha = A; sx = sy = 1.0; rot = 0.0; blur = 0.0; col = base; flash = None; sep = 0.0; sh = 0.0
                if st == 'drum':
                    sx = sy = 1 + 0.75 * (1 - smooth(a / 0.11)); alpha = (1.0 if a < 0.35 else 0.86) * (1 if out < 0.5 else 0); flash = (RED, 0.85 * (1 - a / 0.07)) if a < 0.07 else None
                elif st == 'pain':
                    sx = sy = 1 + 0.22 * (1 - smooth(a / 0.12)); alpha = smooth(a / 0.06) * A; flash = (RED, 0.95 * (1 - smooth(a / 0.22))) if a < 0.22 else None
                    sep = px * (0.025 + 0.10 * smooth(a / 1.4) + 0.45 * out); sh = 0.022 + 0.06 * (1 - smooth(a / 0.3))
                    cy += out * px * 0.35 * (1 if k % 2 else -1) * 0.5 + out * out * px * 0.8
                elif st == 'prayer':
                    alpha = smooth(a / 0.7) * A; cy -= px * (0.10 * smooth(life / 4.0) + 0.5 * out * (0.4 + 0.6 * k / max(1, n)))
                elif st == 'depression':
                    alpha = smooth(a / 0.30) * A; blur = px * 0.18 * (1 - smooth(a / 0.30)) + px * 0.25 * out; cy += px * (0.10 * smooth(life / 5.0) + 0.5 * out)
                    if h01(i, k * 7) < 0.3 and h01(k, t12) < 0.2: alpha *= 0.30                # a dying tube
                elif st == 'helpless':
                    alpha = (1.0 if a > 0.05 else 0.0) * A
                    if h01(i, k * 3) < 0.22: rot = -5 + 10 * h01(k, 9); cy += px * 0.07
                    if h01(i, k * 5) < 0.18 and h01(k, int(t * 18)) < 0.3: alpha *= 0.3
                    cy += out * px * (0.3 + 0.6 * h01(k, 4)); rot += out * (20 * h01(k, 5) - 10)
                elif st == 'anger':
                    col = RED; sx = sy = 1 + 0.95 * (1 - smooth(a / 0.09)); alpha = A; sh = 0.10 * max(0, 1 - a / 0.3) + 0.012; flash = ((255, 246, 236), 0.8 * (1 - a / 0.05)) if a < 0.05 else None
                if i in WIPE and out > 0:                                               # wiped off, left to right, as if with a cloth
                    ok_ = smooth(out * 1.7 - 0.7 * k / max(1, n)); sx *= 1 + 2.6 * ok_; sy *= 1 - 0.35 * ok_; cx += px * 1.6 * ok_; alpha = (1 - ok_) * (1 if st != 'pain' else 1.0); sep *= 0.3
                if sh > 0: cx += (h01(k, t12) - 0.5) * px * sh * 2; cy += (h01(k + 7, t12) - 0.5) * px * sh * 2
                if alpha <= 0.003: continue
                g = self.glyph(ln, k, col)
                # blood under the glyph (pain, anger), a pale tear (prayer)
                if lay['drip'][k]:
                    if st in ('pain', 'anger') and h01(i, k * 13) < (0.55 if st == 'pain' else 0.35): self.drip(d, cx, cy, px, px * (0.12 + 1.5 * smooth((a - 0.2) / 2.2)), BLOOD, 0.95 * alpha)
                    if st == 'prayer' and h01(i, k * 11) < 0.28: self.drip(d, cx, cy, px, px * (0.08 + 0.9 * smooth((a - 0.7) / 3.2)), (226, 222, 216), 0.55 * alpha)
                # colour fringes: strong on arrival, a little left over
                fr = px * ((0.10 * (1 - smooth(a / 0.28)) if st != 'prayer' else 0.0) + CHROMA[st]) * (1 - 0.5 * out)
                if fr >= 1.0:
                    self.put(layer, tint(g['core'], (235, 40, 52), 0.55), cx - fr, cy, sx, sy, rot, blur, alpha); self.put(layer, tint(g['core'], (40, 205, 235), 0.42), cx + fr, cy + fr * 0.3, sx, sy, rot, blur, alpha)
                if g['seam'] is not None: self.put(layer, g['seam'], cx, cy, sx, sy, rot, blur, alpha)
                for (part, (ux, uy)) in g['parts']: self.put(layer, part, cx + ux * sep, cy + uy * sep, sx, sy, rot, blur, alpha)
                if flash is not None and flash[1] > 0.01: self.put(layer, tint(g['core'], flash[0], flash[1]), cx, cy, sx, sy, rot, blur, alpha)
                if st == 'drum' and a > 0.12 and out < 0.5:                            # it cracks where it landed
                    ang = (h01(i, k * 3 + 1) - 0.5) * 2.2 + (0.6 if k % 2 else -0.6); L = px * 0.36; p0 = (cx - np.cos(ang) * L, cy - np.sin(ang) * L); p2 = (cx + np.cos(ang) * L, cy + np.sin(ang) * L)
                    p1 = (cx + (h01(k, 77) - 0.5) * px * 0.2, cy + (h01(k, 78) - 0.5) * px * 0.2); grow = smooth((a - 0.12) / 0.12)
                    d.line([p0, p1, (p1[0] + (p2[0] - p1[0]) * grow, p1[1] + (p2[1] - p1[1]) * grow)], fill=BLOOD + (int(225 * A),), width=max(2, px // 26), joint='curve')
            # over the whole line
            if st == 'pain' and life > 0:
                sa = max(0.0, 1 - life / 0.45) * 0.85 + 0.20
                for j in range(3):
                    yy0 = by0 + (by1 - by0) * (0.2 + 0.3 * j) + (h01(i, j + 40) - 0.5) * px * 0.3; ln_ = smooth(life / 0.18)
                    xa = bx0 - px * 0.3; xb = xa + (bx1 - bx0 + px * 0.6) * ln_
                    d.line([(xa, yy0 + px * 0.25), (xb, yy0 - px * 0.25)], fill=((255, 214, 214) if life < 0.45 else BLOOD) + (int(255 * sa * A),), width=max(2, px // 34))
            if st == 'depression' and life > 0.3:
                x0 = min(xs_) - px * 0.3; y1 = max(ys_) + px * 0.95; w = self.W * 0.30 * smooth((life - 0.3) / 1.5); d.line([(x0, y1), (x0 + w, y1)], fill=base + (int(255 * A * 0.6),), width=max(2, self.H // 600))
            if st == 'helpless' and out < 0.5:
                shown = sum(1 for k in range(n) if t - (ln['t0'] + sung * k / max(1, n - 1)) >= 0); kk = min(n - 1, max(0, shown - 1)); cx, cy = lay['pos'][kk]
                if int(t * 2.6) % 2 == 0 or shown < n: d.line([(cx + px * 0.62, cy - px * 0.5), (cx + px * 0.62, cy + px * 0.5)], fill=base + (int(255 * A),), width=max(2, px // 16))
            if st == 'prayer' and life > 0.2:
                gx = (min(xs_) + max(xs_)) / 2; gy0 = min(ys_) - px * 0.9; d.line([(gx, gy0 - px * 0.5 * smooth(life / 1.2)), (gx, gy0)], fill=base + (int(255 * A * 0.55),), width=max(2, self.H // 600))
        if layer is None: return img
        bb = layer.getbbox()
        if bb is None: return img
        x0, y0, x1, y1 = bb; a = np.asarray(layer.crop(bb)).astype(np.float32) / 255.0; al = a[..., 3:4]; img = img.copy(); img[y0:y1, x0:x1] = img[y0:y1, x0:x1] * (1 - al) + a[..., :3] * al
        return img
