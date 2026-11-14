# -*- coding: utf-8 -*-
"""The design blueprint of the finished film as ONE long image (blueprint/显像管的外侧_设计图纸.jpg).
    python -P tools/make_blueprint.py            (needs the rendered frames in unity/Capture/cut5 for the thumbnails, node + Playwright + Edge)
Everything drawn comes from the project itself: the bar table (FilmBars.cs), the stage states, lights and state track (FilmClock.cs),
the measured positions of the set (docs/blueprint_geo.json, written by Sets.BlueprintGeo), the shot list (docs/film_v5.json), the
caption times (docs/字幕.json or the template), the caption styles (tools/lyric_type.py) and the finish settings (tools/finish_film.py).
No lyric text is drawn: caption samples use a neutral sample phrase."""
import json, os, re, sys, math, shutil, subprocess, html
import numpy as np
from PIL import Image, ImageFilter
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); sys.path.insert(0, os.path.join(ROOT, 'tools'))
OUT = os.path.join(ROOT, 'blueprint'); BUILD = os.path.join(OUT, '_build'); TH = os.path.join(BUILD, 'th')
CAP = os.path.join(ROOT, 'unity', 'Capture', 'cut5'); FPS = 24; DUR = 193.09
os.makedirs(TH, exist_ok=True)
esc = html.escape

# ---------------------------------------------------------------- data out of the project
film = json.load(open(os.path.join(ROOT, 'docs', 'film_v5.json'), encoding='utf-8')); SHOTS = film['shots']
BARS = [float(x) for x in re.search(r'T = \{([^}]*)\}', open(os.path.join(ROOT, 'unity', 'Assets', 'Stage', 'FilmBars.cs'), encoding='utf-8').read()).group(1).replace('f', '').split(',')]
def B(bar, beat=0.0):
    b = max(1, min(int(bar), len(BARS) - 2)); f = (bar - b) * 4 + beat; return BARS[b] + (BARS[b + 1] - BARS[b]) * f / 4
clock = open(os.path.join(ROOT, 'unity', 'Assets', 'Stage', 'FilmClock.cs'), encoding='utf-8').read()
BASE = {k: float(v) for k, v in re.findall(r'\{ "(\w+)", ([\d.]+)f \}', clock[clock.index('BASE ='):clock.index('};', clock.index('BASE ='))])}
STATE = {}
for name, body in re.findall(r'\{ "(\w+)", L\((.*?)\) \}', clock):
    it = [x.strip() for x in body.split(',')]; STATE[name] = {it[i].strip('"'): float(it[i + 1]) for i in range(0, len(it) - 1, 2)}
TRACK = [(B(float(a), float(b)), s, float(f)) for a, b, s, f in re.findall(r'new Key\(([\d.]+), ([\d.]+)f?, "(\w+)", ([\d.]+)f\)', clock)]
geo = json.load(open(os.path.join(ROOT, 'docs', 'blueprint_geo.json'), encoding='utf-8')); P = geo['pieces']; LIGHTS = {l['name']: l for l in geo['lights']}
capf = os.path.join(ROOT, 'docs', '字幕.json'); capf = capf if os.path.exists(capf) else os.path.join(ROOT, 'docs', '字幕_待填.json'); CAPS = json.load(open(capf, encoding='utf-8'))
from lyric_type import STYLE_LINES, FONT, SIZE, FORCE, WIPE, Lyrics
STYLE_OF = {i: s for s, l in STYLE_LINES.items() for i in l}
fin = open(os.path.join(ROOT, 'tools', 'finish_film.py'), encoding='utf-8').read()
HALF = eval(re.search(r'^HALF = (\{.*\})', fin, re.M).group(1)); TEAR = eval(re.search(r'^TEAR = (\{.*\})', fin, re.M).group(1))
def tc(t): return f'{int(t // 60)}:{t % 60:04.1f}'

TYPE = {'R': ('房间', '#8a6d4b'), 'X': ('进出电视', '#6f6f8c'), 'T': ('纸剧场', '#3d5a80'), 'A': ('动物头纸人', '#7a4f8a'), 'L': ('lin', '#c1443a'), '-': ('片尾', '#444')}
SCOL = {'off': '#111', 'pre': '#3b3b46', 'build': '#4b5d86', 'night': '#2f3f73', 'spot': '#1d1d24', 'dim': '#3a3a48', 'dusk': '#b8693a', 'empty': '#5a4630', 'bright': '#d9a441', 'dark': '#20202a'}
SNAME = {'off': '关', 'pre': '开幕前', 'build': '搭景', 'night': '夜', 'spot': '只剩光圈', 'dim': '布景回来，灯没亮', 'dusk': '黄昏', 'empty': '空台·值班灯', 'bright': '最亮', 'dark': '熄灯'}
STY = {'drum': ('鼓点', '#d08a2e'), 'pain': ('伤痛', '#b0283a'), 'prayer': ('祈祷', '#5a7fb0'), 'depression': ('抑郁', '#6c7a86'), 'helpless': ('无能', '#8a8a5a'), 'anger': ('愤怒', '#e0301e')}
SECTIONS = [(0, B(12), '前奏'), (B(12), B(14), '念白'), (B(14), B(22), '主歌一'), (B(22), B(26), '导歌'), (B(26), B(34), '副歌一'), (B(34), B(35), '间'), (B(35), B(45), '主歌二'),
            (B(45), B(49), '导歌二'), (B(49), B(57), '副歌二'), (B(57), B(58), '停'), (B(58), B(66), '桥段'), (B(66), B(67), '停'), (B(67), B(75), '副歌三（最轻）'), (B(75), B(83), '副歌四（最强）'), (B(83), 186.6, '再见'), (186.6, DUR, '尾')]

# ---------------------------------------------------------------- images
def shot_frame(n, u=0.5):
    s = SHOTS[n - 1]; f = int(s['f0'] + (s['f1'] - s['f0']) * u); d = os.path.join(CAP, f'S{n:02d}')
    if not os.path.isdir(d): return None
    fs = sorted(os.listdir(d)); best = min(fs, key=lambda x: abs(int(x[2:7]) - f)); return os.path.join(d, best)
def thumb(n, w=400, u=0.5, name=None, grade=1.0):
    p = shot_frame(n, u); name = name or f'S{n:02d}.jpg'
    if p is None: return None
    im = Image.open(p).convert('RGB'); im = im.resize((w, int(w * im.height / im.width)), Image.LANCZOS)
    if grade != 1.0: im = Image.fromarray(np.clip(np.asarray(im) * grade, 0, 255).astype(np.uint8))
    im.save(os.path.join(TH, name), quality=86); return 'th/' + name
GRADE = {5: 0.6, 52: 0.6}
for i, s in enumerate(SHOTS[:-1]): thumb(i + 1, grade=GRADE.get(i + 1, 1.0))
for n in (24, 47, 55, 65, 66, 70, 77, 79, 16, 18, 10, 11, 31, 36, 37, 57, 58, 64, 72, 73, 75, 56, 4): thumb(n, 720, name=f'big_S{n:02d}.jpg', grade=GRADE.get(n, 1.0))
for k, (n, u) in enumerate([(10, 0.12), (10, 0.30), (10, 0.42), (11, 0.6)]): thumb(n, 360, u, name=f'fall{k}.jpg')
for v, t in (('v3_office', 2.5), ('v4_roof', 2.5), ('v1_news', 2.5)):
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-ss', str(t), '-i', os.path.join(ROOT, 'gen', 'video', v + '.mp4'), '-frames:v', '1', '-vf', 'scale=360:-1', '-pix_fmt', 'yuvj420p', os.path.join(TH, v + '.jpg')])
for k in list(range(6)) + [f'p{j}' for j in range(4)]:
    src = os.path.join(ROOT, 'unity', 'Assets', 'Gen', 'art', (f'card{k}' if isinstance(k, int) else f'pane{k[1:]}') + '.png'); im = Image.open(src).convert('RGB'); im.thumbnail((260, 260)); im.save(os.path.join(TH, f'art_{k}.jpg'), quality=88)
# caption style samples: each style on a dimmed frame of the film, with a neutral sample phrase (no lyric)
bg = Image.open(shot_frame(29)).convert('RGB').resize((1280, 720), Image.LANCZOS).filter(ImageFilter.GaussianBlur(2)); bga = np.asarray(bg).astype(np.float32) / 255 * 0.55
for st in STY:
    L = Lyrics([[0.0, 3.0, '見本の文字です' if st != 'anger' else '見本の文字', '', '']], 1280, 720); L.lines[0]['style'] = st
    t = {'drum': 2.6, 'pain': 2.7, 'prayer': 2.6, 'depression': 2.6, 'helpless': 2.8, 'anger': 2.6}[st]
    img = bga
    for tt in np.arange(-0.1, t, 1 / 24): img2 = L.draw(bga, float(tt), 0)
    Image.fromarray((np.clip(img2, 0, 1) * 255).astype(np.uint8)).resize((480, 270), Image.LANCZOS).save(os.path.join(TH, f'sty_{st}.jpg'), quality=88)

# ---------------------------------------------------------------- svg helpers
def L_(x1, y1, x2, y2, c='#222', w=1.2, extra=''): return f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{c}" stroke-width="{w}" {extra}/>'
def R_(x, y, w, h, fill='none', stroke='#222', sw=1.0, extra=''): return f'<rect x="{x:.1f}" y="{y:.1f}" width="{max(0.5, w):.1f}" height="{max(0.5, h):.1f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" {extra}/>'
def T_(x, y, s, size=13, c='#222', anchor='start', weight=400, extra=''): return f'<text x="{x:.1f}" y="{y:.1f}" font-size="{size}" fill="{c}" text-anchor="{anchor}" font-weight="{weight}" {extra}>{esc(str(s))}</text>'
def C_(x, y, r, fill='none', stroke='#222', sw=1.0): return f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{r:.1f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>'
def A_(x1, y1, x2, y2, c='#c1443a', w=1.6):
    a = math.atan2(y2 - y1, x2 - x1); h = 8
    return L_(x1, y1, x2, y2, c, w) + f'<polygon points="{x2:.1f},{y2:.1f} {x2 - h * math.cos(a - 0.4):.1f},{y2 - h * math.sin(a - 0.4):.1f} {x2 - h * math.cos(a + 0.4):.1f},{y2 - h * math.sin(a + 0.4):.1f}" fill="{c}"/>'
def svg(w, h, body): return f'<svg width="{w}" height="{h}" viewBox="0 0 {w} {h}" xmlns="http://www.w3.org/2000/svg" font-family="Microsoft YaHei, Noto Sans SC, sans-serif">{body}</svg>'

# ---------------------------------------------------------------- 1. the pipeline
def pipeline():
    boxes = [('歌曲', 'audio/song.wav\n112.3 BPM · 4/4\n小节 2.136 秒', 0), ('小节表', 'FilmBars.cs\n86 个小节的起点', 1), ('舞台时钟', 'FilmClock.cs\n布景/灯/窗/纸人/道具\n= 时间的函数', 2), ('83 个机位', 'FilmCams.cs\n镜头表 film_v5.json', 3),
             ('电视画面', 'FilmBaker\n1440×1080 全片一遍\n+ tv_clips.py（AI 视频）', 4), ('舞台镜头', 'ShotBaker（播放模式）\n2560×1440，前后各 8 帧', 5), ('房间镜头', 'Sets.RoomFilm\n电视屏幕贴上电视画面', 6), ('演唱', 'SingDriver + key_film.bin\n全曲手 K · 60 fps · 57 通道', 7), ('合成', 'finish_film.py\n软切/叠化/撕裂\n片名/字幕/片尾/−14 LUFS', 8)]
    W, H = 1520, 330; o = []
    pos = {0: (10, 20), 1: (200, 20), 2: (390, 20), 3: (580, 20), 7: (390, 180), 4: (790, 20), 5: (790, 130), 6: (790, 240), 8: (1100, 120)}
    for name, sub, k in boxes:
        x, y = pos[k]; w = 170 if k not in (4, 5, 6) else 250; h = 82 if k != 8 else 110; w = 330 if k == 8 else w
        o.append(R_(x, y, w, h, '#fffaf0' if k != 8 else '#f7e4dc', '#333', 1.4, 'rx="8"')); o.append(T_(x + 12, y + 24, name, 16, '#111', weight=700))
        for j, ln in enumerate(sub.split('\n')): o.append(T_(x + 12, y + 45 + j * 17, ln, 12.5, '#444'))
    for a, b in [((180, 61), (200, 61)), ((370, 61), (390, 61)), ((560, 61), (580, 61)), ((750, 61), (790, 61)), ((750, 61), (790, 171)), ((750, 61), (790, 281)), ((475, 180), (475, 102)), ((560, 221), (790, 171)),
                 ((1040, 61), (1100, 150)), ((1040, 171), (1100, 175)), ((1040, 281), (1100, 200))]:
        o.append(A_(a[0], a[1], b[0], b[1], '#444', 1.4))
    return svg(W, H, ''.join(o))

# ---------------------------------------------------------------- 2. the one clock
def timeline():
    K, LFT = 7.0, 120; rows = [('小节', 22), ('段落', 26), ('舞台状态', 26), ('镜头', 34), ('lin 在台上', 18), ('动物头纸人', 18), ('纸偶女孩', 18), ('红纸 / 纸鸟', 18), ('电视 AI 视频', 14), ('换镜撕裂', 14), ('字幕 55 句', 22)]
    ys = []; y = 24
    for _, h in rows: ys.append(y); y += h + 6
    W, H = int(LFT + DUR * K + 20), y + 10; X = lambda t: LFT + t * K; o = []
    for t in range(0, int(DUR) + 1, 10): o.append(L_(X(t), 14, X(t), H - 6, '#ddd', 1)); o.append(T_(X(t) + 2, 12, f'{t // 60}:{t % 60:02d}', 11, '#888'))
    for i, (r, h) in enumerate(rows): o.append(T_(LFT - 8, ys[i] + h / 2 + 5, r, 13, '#333', 'end', 600))
    for b in range(1, len(BARS) - 1):
        o.append(L_(X(BARS[b]), ys[0], X(BARS[b]), ys[0] + (rows[0][1] if b % 4 == 2 else 7), '#999', 1))
        if b % 4 == 2: o.append(T_(X(BARS[b]) + 2, ys[0] + 17, b, 10.5, '#666'))
    def blk(i, t0, t1, col, lab='', tc_='#fff'):
        h = rows[i][1]; w = (t1 - t0) * K
        o.append(R_(X(t0), ys[i], w - 0.8, h, col, 'none', 0, 'rx="3"'))
        if lab and len(lab) * 12 < w: o.append(T_(X(t0) + 4, ys[i] + h / 2 + 4.5, lab, 11.5, tc_))
    for t0, t1, lab in SECTIONS: blk(1, t0, t1, '#e9e1d2', lab, '#333')
    for k, (t, s, f) in enumerate(TRACK): blk(2, t, TRACK[k + 1][0] if k + 1 < len(TRACK) else DUR, SCOL[s], SNAME[s])
    for i, s in enumerate(SHOTS):
        t0, t1 = s['f0'] / FPS, s['f1'] / FPS; blk(3, t0, t1, TYPE[s['type']][1])
        if (t1 - t0) * K > 14: o.append(T_(X((t0 + t1) / 2), ys[3] + 21, i + 1, 10.5, '#fff', 'middle'))
    for a, b in [(B(26), B(30)), (B(49), B(53)), (B(58), B(60, 2)), (B(68), 185.6)]: blk(4, a, b, '#c1443a', '在台上')
    for a, b, lab in [(B(14), B(17), '入场'), (B(17), B(25), '翻纸片'), (B(29, 3.3), B(47, 2), '围观·对话卡'), (B(62, 2), B(75), '回来·抬头'), (B(75), B(83, 1), '举小窗灯')]: blk(5, a, b, '#7a4f8a', lab)
    from_ = B(10, 3); fall = B(12) + 0.12
    blk(6, from_, fall, '#555', '屋檐上'); blk(6, fall, fall + 0.92, '#b0283a'); blk(6, fall + 0.92, B(38, 1.3), '#7d2a2a', '躺在斑马线上，红纸片一拍一片')
    for a, b, lab, c in [(B(11), B(37, 2.5), '红纸', '#c1443a'), (B(38, 2.6), B(57), '灰纸', '#888'), (B(59, 3), B(61, 3), '折', '#e07a5f'), (B(61, 3), B(68), '纸鸟飞', '#c1443a'), (B(68), B(75, 2), '掌心', '#9b2d26'), (B(75, 2), DUR, '飞出台口', '#c1443a')]: blk(7, a, b, c, lab)
    blk(8, 14 / FPS, 345 / FPS, '#6f6f8c')
    for n, k in TEAR.items(): t = SHOTS[n - 1]['f0'] / FPS; o.append(R_(X(t) - 2, ys[9], 4, rows[9][1], '#e0301e', 'none', 0))
    for i, c in enumerate(CAPS): blk(10, c[0], max(c[0] + 0.3, c[1]), STY[STYLE_OF.get(i, 'depression')][1])
    return svg(W, H, ''.join(o))

# ---------------------------------------------------------------- 3. the stage in plan (measured)
def stage_plan():
    S = 190.0; x0, x1, z0, z1 = -3.3, 3.3, -1.2, 5.3; W, H = int((x1 - x0) * S) + 40, int((z1 - z0) * S) + 40
    X = lambda x: 20 + (x - x0) * S; Z = lambda z: 20 + (z1 - z) * S; o = []
    def box(p, fill, stroke='#333', lab=None, lc='#222'):
        a, b = P[p]['min'], P[p]['max']; o.append(R_(X(a[0]), Z(b[2]), (b[0] - a[0]) * S, (b[2] - a[2]) * S, fill, stroke, 1.2))
        if lab: o.append(T_(X((a[0] + b[0]) / 2), Z((a[2] + b[2]) / 2) + 5, lab, 13, lc, 'middle', 600))
    o.append(R_(X(-3.0), Z(4.9), 6.0 * S, 4.34 * S, '#eee6d6', '#bbb', 1))                                          # the street cloth
    box('Cyc', '#dfe6f2', '#99a', '天幕'); box('FarRow', '#b8c2d6', '#556', '远楼一排（20 扇窗）'); box('HouseR', '#c9cfdd', '#334', '近楼 右（高 3.62 m）'); box('HouseL', '#c9cfdd', '#334', '近楼 左（高 3.17 m）')
    box('WingR0', '#d8cbb5', '#665'); box('WingL0', '#d8cbb5', '#665')
    o.append(L_(X(-3.17), Z(0), X(-2.0), Z(0), '#222', 6) + L_(X(2.0), Z(0), X(3.17), Z(0), '#222', 6) + L_(X(-2.0), Z(0), X(2.0), Z(0), '#7aa', 2, 'stroke-dasharray="6 4"'))
    o.append(T_(X(0), Z(0) + 18, '台口 4.0 × 3.0 m（电视画面的 4:3）', 13, '#355', 'middle'))
    for i in range(6):
        a, b = P[f'Person{i}']['min'], P[f'Person{i}']['max']; cx, cz = (a[0] + b[0]) / 2, (a[2] + b[2]) / 2
        o.append(R_(X(cx) - 22, Z(cz) - 7, 44, 14, '#cdb6d6', '#634', 1.2, 'rx="4"')); o.append(T_(X(cx), Z(cz) - 12, ['秃鹫', '鬣狗', '乌鸦', '狼', '鳄鱼', '野猪'][i], 12, '#523', 'middle'))
    o.append(T_(X(0), Z(2.22) + 26, '纸人一排，沿地板槽 z = 2.22 进出', 12, '#523', 'middle'))
    lx, lz = 0.39, 2.32; o.append(C_(X(lx), Z(lz), 7, '#f3c35a', '#a70')); o.append(T_(X(lx) + 12, Z(lz) + 4, '路灯（灯高 2.74 m）', 12, '#850'))
    for (x, z, lab, anc, dx) in [(-0.5, 1.3, 'lin ①（小节 26–60）', 'end', -18), (0.05, 1.3, 'lin ②（小节 61 起）', 'start', 18)]:
        o.append(C_(X(x), Z(z), 12, '#f2b6ad', '#a33', 1.6)); o.append(R_(X(x) - 3, Z(z + 0.14) - 3, 6, 6, '#333', 'none')); o.append(T_(X(x) + dx, Z(z) + 5, lab, 12, '#a33', anc))
    o.append(T_(X(-0.22), Z(1.3) + 34, '小黑点：立麦（嘴前 14 cm）', 11, '#666', 'middle'))
    a, b = P['RedPaper']['min'], P['RedPaper']['max']; o.append(R_(X(a[0]), Z(b[2]), (b[0] - a[0]) * S, (b[2] - a[2]) * S, '#c1443a', 'none')); o.append(T_(X(a[0]) - 8, Z(1.52) - 10, '红纸 → 折纸鸟', 12, '#a22', 'end'))
    o.append(R_(X(1.05), Z(2.86) - 4, 0.4 * S, 8, '#7d2a2a', 'none')); o.append(T_(X(1.25), Z(2.86) - 10, '纸偶女孩站在屋檐上（高 2.76 m）', 12, '#7d2a2a', 'middle'))
    o.append(R_(X(0.78), Z(1.64) - 5, 1.36 * S, 10, '#7d2a2a', 'none', 0, 'opacity="0.55"')); o.append(T_(X(1.46), Z(1.64) + 24, '坠落后躺在斑马线上（头朝红纸）', 12, '#7d2a2a', 'middle'))
    o.append(C_(X(2.4), Z(5.02), 7, '#ffd27a', '#a60')); o.append(T_(X(2.4) - 10, Z(5.02) + 4, '值班灯', 12, '#850', 'end'))
    for n, l in LIGHTS.items():
        if n.startswith('Foot') or n.startswith('ProsRim') or n in ('LastBulb', 'BareBulb', 'GhostLight', 'ShotFill', 'LampGlow'): continue
        p, d = l['pos'], l['dir']; px, pz = max(x0 + 0.1, min(x1 - 0.1, p[0])), max(z0 + 0.1, min(z1 - 0.1, p[2])); clipped = (px, pz) != (p[0], p[2])
        dl = math.hypot(d[0], d[2]); ex, ez = (px + d[0] / max(dl, 1e-3) * 0.45, pz + d[2] / max(dl, 1e-3) * 0.45) if dl > 0.15 else (px, pz)
        o.append(A_(X(px), Z(pz), X(ex), Z(ez), '#d08a2e', 1.4) if dl > 0.15 else C_(X(px), Z(pz), 5, 'none', '#d08a2e', 1.6))
        o.append(T_(X(px) + 6, Z(pz) - 6, n + ('（图外）' if clipped else ''), 10.5, '#9a5a10'))
    for z in range(0, 6): o.append(T_(8, Z(z) + 4, f'z {z}', 10, '#999'))
    for x in range(-3, 4): o.append(T_(X(x), H - 6, f'x {x}', 10, '#999', 'middle'))
    return svg(W, H, ''.join(o))

# ---------------------------------------------------------------- 4. the stage in section
def stage_section():
    S = 175.0; z0, z1, y1 = -1.4, 5.4, 7.0; W, H = int((z1 - z0) * S) + 240, int(y1 * S) + 60
    Z = lambda z: 40 + (z - z0) * S; Y = lambda y: 20 + (y1 - y) * S; o = [L_(Z(z0), Y(0), Z(z1), Y(0), '#222', 2)]
    o.append(R_(Z(-0.58), Y(4.86), 0.64 * S, (4.86 - 3.0) * S, '#333', 'none')); o.append(R_(Z(-0.58), Y(0), 0.64 * S, 0.0 * S, '#333')); o.append(T_(Z(-0.3), Y(3.0) + 18, '台口 3.0', 11, '#355', 'middle'))
    for p, lab, c in [('HouseR', '近楼 右 3.62', '#c9cfdd'), ('HouseL', '近楼 左 3.17', '#b9c0d0'), ('FarRow', '远楼 1.48', '#b8c2d6'), ('Cyc', '天幕 5.33', '#dfe6f2')]:
        a, b = P[p]['min'], P[p]['max']; h = b[1] - a[1]; o.append(R_(Z(a[2]), Y(h), (b[2] - a[2]) * S, h * S, c, '#556', 1, 'opacity="0.8"')); o.append(T_(Z(b[2]) + 4, Y(h) + 14, lab, 11.5, '#334'))
    o.append(L_(Z(2.32), Y(0), Z(2.32), Y(2.74), '#a70', 3)); o.append(C_(Z(2.32), Y(2.74), 6, '#f3c35a', '#a70')); o.append(T_(Z(2.32) + 8, Y(2.74), '路灯 2.74', 11, '#850'))
    o.append(R_(Z(2.0), Y(1.86), 0.47 * S, 1.86 * S, '#cdb6d6', '#634', 1, 'opacity="0.85"')); o.append(T_(Z(2.2), Y(1.86) - 6, '纸人 1.7–1.86', 11, '#523', 'middle'))
    o.append(L_(Z(1.3), Y(0), Z(1.3), Y(1.48), '#a33', 3)); o.append(C_(Z(1.3), Y(1.50), 9, '#f2b6ad', '#a33')); o.append(T_(Z(1.3) - 12, Y(1.62), 'lin 1.55', 11, '#a33', 'end'))
    o.append(L_(Z(1.44), Y(0), Z(1.44), Y(1.42), '#333', 1.5)); o.append(T_(Z(1.44) + 4, Y(1.42), '麦 1.42', 10.5, '#333'))
    o.append(R_(Z(2.83), Y(2.76 + 1.47), 6, 1.47 * S, '#7d2a2a', 'none')); o.append(T_(Z(2.83) - 8, Y(3.8), '纸偶女孩（屋檐 2.76）', 11, '#7d2a2a', 'end'))
    o.append(A_(Z(2.80), Y(2.6), Z(2.15), Y(0.15), '#7d2a2a', 1.5)); o.append(T_(Z(2.4), Y(1.2), '坠落 0.92 秒', 11, '#7d2a2a'))
    for n, l in LIGHTS.items():
        if n in ('LastBulb', 'BareBulb', 'ShotFill', 'LampGlow') or n.startswith('Foot1') or n.startswith('ProsRim1'): continue
        p = l['pos']
        if not (z0 <= p[2] <= z1 and 0 <= p[1]): continue
        out = p[1] > y1 - 0.1; py = min(p[1], y1 - 0.1)
        o.append(C_(Z(p[2]), Y(py), 5, '#f0b44c', '#a60')); o.append(T_(Z(p[2]) + 7, Y(py) + 4 + (12 if n in ('GhostKey', 'SunLight', 'BackWork') else 0), n + (f'（{p[1]:.1f} m）' if out else ''), 11, '#9a5a10'))
    for z in range(-1, 6): o.append(T_(Z(z), Y(0) + 18, f'z {z}', 10, '#999', 'middle'))
    for y in range(0, 8): o.append(T_(Z(z0) - 6, Y(y) + 4, f'{y}', 10, '#999', 'end'))
    return svg(W, H, ''.join(o))

# ---------------------------------------------------------------- 5. lights and states (tables)
def light_table():
    names = [n for n in BASE if n in LIGHTS] + [n for n in ('LinKey', 'PoolBounce') if n in LIGHTS]
    states = ['pre', 'build', 'night', 'spot', 'dusk', 'empty', 'dim', 'bright', 'dark', 'off']
    h = ['<table class="t"><tr><th>灯</th><th>类型</th><th>位置 (x, y, z)</th><th>角度</th><th>颜色</th><th>基准强度</th>' + ''.join(f'<th class="v">{SNAME[s]}</th>' for s in states) + '</tr>']
    for n in names:
        l = LIGHTS[n]; c = l['col']; col = f'rgb({int(c[0] * 255)},{int(c[1] * 255)},{int(c[2] * 255)})'; p = l['pos']
        cells = ''.join(f'<td class="v">{("%.2g" % STATE[s][n]) if n in STATE[s] else ""}</td>' for s in states)
        h.append(f'<tr><td><b>{n}</b></td><td>{l["type"]}</td><td class="num">{p[0]:.2f}, {p[1]:.2f}, {p[2]:.2f}</td><td class="num">{l["angle"]:.0f}°</td><td><span class="sw" style="background:{col}"></span></td><td class="num">{BASE.get(n, "—")}</td>{cells}</tr>')
    h.append('</table>'); return ''.join(h)

def state_track():
    rows = ''.join(f'<tr><td class="num">{tc(t)}</td><td>小节 {next(b for b in range(len(BARS)) if BARS[b] >= t - 0.6)}</td><td><span class="sw" style="background:{SCOL[s]}"></span> {SNAME[s]}（{s}）</td><td class="num">{f:g} 秒</td></tr>' for t, s, f in TRACK)
    return f'<table class="t"><tr><th>时间</th><th>约在</th><th>状态</th><th>过渡</th></tr>{rows}</table>'

# ---------------------------------------------------------------- 6. shots
def shot_wall():
    out = ['<div class="wall">']
    for i, s in enumerate(SHOTS[:-1]):
        n = i + 1; nm, col = TYPE[s['type']]; img = f'th/S{n:02d}.jpg'
        ev = s.get('event') or ''; ev = ev[:46] + ('…' if len(ev) > 46 else '')
        tear = ' · 撕裂' if n in TEAR else ''; join = s.get('join') or ''
        out.append(f'<div class="card"><img src="{img}"><div class="cap"><span class="tag" style="background:{col}">{nm}</span><b>S{n:02d}</b> <span class="num">{tc(s["f0"] / FPS)}–{tc(s["f1"] / FPS)}</span>'
                   f' <span class="mut">{s["lens"]}{" mm" if str(s["lens"]).isdigit() else ""} · {join}{tear}</span><div class="ev">{esc(ev)}</div></div></div>')
    out.append('</div>'); return ''.join(out)

# ---------------------------------------------------------------- 7. captions
def zones():
    W, H = 640, 360; o = [R_(0, 0, W, H, '#2b2f3a', 'none')]
    from lyric_type import ZONES
    for z, (x, y, w, h) in ZONES.items():
        o.append(R_(x * W, y * H, w * W, h * H, 'none', '#f0c060', 1.6, 'stroke-dasharray="6 4"')); o.append(T_(x * W + w * W / 2, y * H + h * H / 2 + 5, {'L': '左列（竖排）', 'R': '右列（竖排）', 'T': '上带（横排）', 'B': '下带（横排）'}[z], 13, '#f0c060', 'middle'))
    o.append(R_(W * 0.3, H * 0.845, W * 0.4, H * 0.035, '#fff', 'none')); o.append(T_(W / 2, H * 0.835, '中文 微软雅黑粗体 2.5%·画高', 11, '#fff', 'middle'))
    o.append(R_(W * 0.36, H * 0.925, W * 0.28, H * 0.022, '#ddd', 'none')); o.append(T_(W / 2, H * 0.985, '罗马音 Segoe UI Semibold，屏幕最下方', 11, '#ddd', 'middle'))
    return svg(W, H, ''.join(o))

def style_table():
    desc = {'drum': '每个字落在演唱时刻，1.75→1 倍冲击、红闪，落地后裂出一道血红缝，变暗；硬切退场', 'pain': '先红后白；被斜切成两半并慢慢裂开，露出血红的缝；12 fps 颤抖、色差、划痕、字下流血；退场时两半散落',
            'prayer': '竖排，慢慢浮现并上升，一两个字下挂着淡色泪痕；退场时依次升起淡出', 'depression': '冷色、宽字距，由虚到实，缓慢下沉；一两个字像坏掉的显像管一样闪；一条长细线',
            'helpless': '手写体，光标逐字打出，个别字歪落、闪烁；退场时滑落散开', 'anger': '超大红字盖章：1.95→1 倍、震动、双层描边，压在一张撕开的纸条上'}
    rows = []
    for st, (nm, col) in STY.items():
        idx = sorted(STYLE_LINES[st]); rows.append(f'<tr><td><img class="sty" src="th/sty_{st}.jpg"></td><td><span class="tag" style="background:{col}">{nm}</span><br><span class="mut">{FONT[st].replace(".ttf", "")}<br>字高 {SIZE[st] * 100:.1f}% 画高</span></td><td>{desc[st]}<br><span class="mut">第 {", ".join(str(i + 1) for i in idx)} 句</span></td></tr>')
    return '<table class="t sty">' + ''.join(rows) + '</table>'

# ---------------------------------------------------------------- page
def section(num, title, body, note=''):
    return f'<section><h2><span class="no">{num:02d}</span>{esc(title)}</h2>{f"<p class=note>{note}</p>" if note else ""}{body}</section>'

def imgs(names, w=360, caps=None):
    return '<div class="row">' + ''.join(f'<figure style="width:{w}px"><img src="th/{n}"><figcaption>{esc(caps[i]) if caps else ""}</figcaption></figure>' for i, n in enumerate(names)) + '</div>'

nt = {k: sum(1 for s in SHOTS if s['type'] == k) for k in TYPE}; secs = {k: sum((s['f1'] - s['f0']) for s in SHOTS if s['type'] == k) / FPS for k in TYPE}
joins = {}
for s in SHOTS: joins[s.get('join') or '—'] = joins.get(s.get('join') or '—', 0) + 1
facts = [('时长', '3 分 18 秒（歌 193.09 秒 + 片尾 4.5 秒）'), ('画面', '2560×1440 · 24 fps · 4742 帧'), ('镜头', f'{len(SHOTS)} 个（纸剧场 {nt["T"]}、纸人 {nt["A"]}、lin {nt["L"]}、房间 {nt["R"]}、进出电视 {nt["X"]}）'),
         ('工具', 'Unity 6000.3.28f1 URP · Unity CLI · Python · ffmpeg'), ('歌', 'あいみょん《生きていたんだよな》 112.3 BPM 4/4'), ('角色', 'lin（VRoid，VRM 1.0）全曲手 K')]
body = []
body.append('<header><div class="h1">显像管的外侧</div><div class="h2">设计图纸 · 成片 v4 定稿</div><div class="facts">' + ''.join(f'<div><span>{k}</span>{esc(v)}</div>' for k, v in facts) + '</div>'
            f'<img class="hero" src="th/big_S04.jpg"><img class="hero" src="th/big_S73.jpg"><img class="hero" src="th/big_S56.jpg"></header>')
body.append(section(1, '制作管线', pipeline(), '一切都是“时间的函数”：同一个时间点永远得到同一帧。舞台由 FilmClock 按小节和拍驱动，镜头由 FilmCams 按镜头表取景，三条渲染线最后在合成脚本里拼起来。'))
body.append(section(2, '全片时间线', '<div class="scroll">' + timeline() + '</div>', '纸的东西一拍二（每秒 12 个位置）地动，灯光和机位连续地变。'
            '<br>' + ' '.join(f'<span class="tag" style="background:{c}">{n}</span>' for n, c in TYPE.values()) + '　字幕颜色：' + ' '.join(f'<span class="tag" style="background:{c}">{n}</span>' for n, c in STY.values())))
body.append(section(3, '83 个镜头', shot_wall(), '每张是该镜头中间的一帧。标签：类型 · 镜号 · 起止时间 · 焦距 · 怎么接进来。' + '　接法统计：' + '，'.join(f'{k} {v}' for k, v in joins.items()) +
            f'。合成规则：软切 = 切点两侧各 {HALF["软切"]} 帧的混合，叠化一拍 = 各 {HALF["叠化一拍"]} 帧，对位叠化 = 各 {HALF["对位叠化"]} 帧。'))
body.append(section(4, '舞台平面（实测）', stage_plan(), '单位米。x 横向（观众看过去右为正），z 从台口往里。橙色箭头是灯和它照的方向；坐标取自场景（docs/blueprint_geo.json）。'))
body.append(section(5, '舞台剖面', stage_section(), '从侧面看。台口开口 4.0×3.0 m，正好是电视的 4:3；橙点是灯。'))
body.append(section(6, '灯光：每盏灯和每个舞台状态', light_table() + '<h3>状态的时间表</h3>' + state_track(), '表里的数字是该状态下这盏灯相对基准强度的倍数；空格表示关。追光、窗光和逆光只在 lin 上台时亮。最亮状态（副歌四）把泛光提到 1.7 倍、场灯 1.5 倍、脚灯半亮。'))
body.append(section(7, '房间和电视', imgs(['big_S04.jpg', 'big_S56.jpg'], 720, ['S04 房间全景：老电视里是纸剧场', 'S56 值班灯：空台上只剩它']),
                    '房间只有一个全景机位，出现三次（S04、S46、S82）。电视画面是同一部片子从正面机位渲出的 1440×1080 帧，贴到显像管玻璃上；屏幕发的光按画面平均色实时变化。开场 0.6–14.4 秒的电视里是三段 AI 视频，频道之间夹 4 帧雪花。'))
body.append(section(8, 'lin：演唱与表演', imgs(['big_S24.jpg', 'big_S47.jpg', 'big_S65.jpg', 'big_S70.jpg', 'big_S77.jpg', 'big_S79.jpg'], 480, ['S24 副歌一：张开双臂', 'S47 副歌二', 'S65 鸟落在掌心，低头看它', 'S70 放飞：抬手、抬头目送', 'S77 正视镜头', 'S79 低头闭眼']) +
                    '<ul><li>动作：演唱工程的全曲手 K（60 fps，每帧 57 个控制量：骨盆、躯干、头、耸肩、双手腕位置/手指方向/掌心/手肘/五指、握麦、嘴贴麦）。</li>'
                    '<li>影片版（tools/lin_key_film.py）：小节 58–60 收手侧脸；小节 68–76 右手离麦、掌心朝上托鸟，放飞时抬手 17 cm、抬头 17°；倒数第二句正视镜头；小节 84.5 起低头闭眼。</li>'
                    '<li>立麦是她的子物体，跟着站位走：麦头在嘴前 14 cm、下 4.5 cm，话筒俯角 33°。</li><li>她只在四段时间里在台上：小节 26–30、49–53、58–60½、68 到片尾。</li></ul>'))
body.append(section(9, '动物头纸人', imgs(['big_S16.jpg', 'big_S18.jpg', 'big_S75.jpg'], 480, ['S16 六个一字排开，手里白纸片按拍翻亮', 'S18 乌鸦歪头', 'S75 野猪：手里的小窗灯照着脸']) +
                    '<table class="t"><tr><th>小节</th><th>动作</th></tr>' + ''.join(f'<tr><td class="num">{a}</td><td>{esc(b)}</td></tr>' for a, b in [
                        ('14–17', '沿地板槽从两侧滑入，z 前后错开 4 mm'), ('18', '白纸片按半拍依次举起'), ('19', '右边三个翻面'), ('20 第 3 拍', '乌鸦歪头'), ('25', '退场'), ('29 第 3.3 拍', '在“只剩光圈”的暗里回来'),
                        ('36', '前倾围观'), ('40–42', '头上依次弹出对话卡（卡上是 AI 插图：指人的手、举起的手机、偷看的眼睛……）'), ('47.5–48', '退场'), ('62.5', '回来'), ('71 第 3 拍', '依次抬起头'), ('73', '鬣狗放低纸片'),
                        ('75', '举起小窗灯（手臂 −160°），灯身保持竖直'), ('83', '小窗灯依次熄灭')]) + '</table>'))
body.append(section(10, '纸偶女孩、红纸和纸鸟', imgs(['fall0.jpg', 'fall1.jpg', 'fall2.jpg', 'fall3.jpg'], 360, ['S10 她站在屋檐上', '路灯亮的那一拍她落下', '翻转着落出画面', 'S11 斑马线上的红纸']) +
                    imgs(['big_S31.jpg', 'big_S37.jpg', 'big_S57.jpg', 'big_S64.jpg'], 360, ['S31 俯拍：躺着的她和红纸', 'S37 黑纸手掀起红纸，她被一起拖走', 'S57 红纸在追光里自己折', 'S64 纸鸟落向掌心']) +
                    '<ul><li>纸偶：白衬衫、深色裙、蓝色工牌挂绳，卡纸厚 3 mm。小节 10 第 3 拍出现在屋檐，小节 12 + 0.12 秒坠落（0.92 秒、平面内翻转 165°），落地后每拍从头下滑出一片红纸，共 5 片；小节 37 第 2.5 拍起随红纸被拖走。</li>'
                    '<li>红纸：小节 11 出现在路灯下的斑马线；小节 37 被黑纸手换成灰纸（正好 2 秒）。</li><li>折纸：小节 59 第 4 拍起空台上一张红纸落下，小节 61 每拍折一次，第 4 拍成鸟。</li>'
                    '<li>纸鸟：穿过纸云、掠过暗窗、落在 lin 掌心（小节 68）、小节 75 第 2 拍起飞，从台口那圈亮着的灯泡飞出去；翅膀 3 Hz，一拍二。</li></ul>'))
body.append(section(11, 'AI 生成的素材', imgs(['v3_office.jpg', 'v4_roof.jpg', 'v1_news.jpg'], 360, ['电视 1：办公室里被训斥（帧 14–100）', '电视 2：天台的鞋和工牌（104–215）', '电视 3：楼下的警戒带（219–337）']) +
                    imgs([f'art_{k}.jpg' for k in range(6)], 230, ['对话卡'] * 6) + imgs([f'art_p{j}.jpg' for j in range(4)], 170, ['远楼窗里'] * 4),
                    '视频：阿里云 wan3.0-video，调用 5 次选 3 段；图片：wan2.7-image，调用 15 次选 10 张，裁成比卡片和窗格小一圈。纸面布景贴图（楼、天空、窗里的剪影）由 MiniMax image-01 生成；动物头由 MiniMax 出图、百炼 Tripo 转三维。提示词都在 gen/。'))
body.append(section(12, '字幕', '<div class="row">' + zones() + '<div style="width:820px">' + style_table() + '</div></div>',
                    f'日文大字按句子的情绪分六种排法，逐字跟着演唱时间出现，自动放在画面最空的区；lin 的近景固定放在她空出的一侧（{", ".join(f"S{k}→{v}" for k, v in FORCE.items())}），切到近景时正在唱的那句跟过去。'
                    f'第 {", ".join(str(i + 1) for i in sorted(WIPE))} 句像被布擦掉一样退场。中文和罗马音固定在下方，只淡入淡出。示例图用的是“見本の文字です”，不是歌词。'))
body.append(section(13, '合成与片头片尾', '<ul>'
                    f'<li>片名“显像管的外侧”9.1–12.2 秒，在 S04 房间全景里，中文大字配日文歌名。</li><li>换镜撕裂（显像管失去信号）只在 7 个叙事断裂处：进入 {", ".join(f"S{k:02d}" for k in TEAR)}，切点前后各 3 帧。</li>'
                    '<li>S05、S52 两个书桌近景整体压暗到 0.6 倍，接进暗房间。</li><li>歌结束后 4.5 秒黑场：署名、AI 素材说明、心理援助提示。</li><li>声音只用原曲，标准化到 −14 LUFS、真峰值 −1.5 dBTP。</li></ul>'))
CSS = """
body{margin:0;background:#f4efe4;color:#1d1b18;font-family:'Microsoft YaHei','Noto Sans SC',sans-serif;width:1600px}
header{padding:56px 40px 20px;background:#1b1d24;color:#f4efe4}
.h1{font-family:'Noto Serif SC','SimSun',serif;font-size:76px;font-weight:900;letter-spacing:12px}.h2{font-size:24px;color:#d9a441;margin:6px 0 24px}
.facts{display:grid;grid-template-columns:repeat(3,1fr);gap:10px 30px;margin-bottom:26px}.facts div{font-size:16px}.facts span{display:inline-block;width:56px;color:#c1443a;font-weight:700}
.hero{width:492px;margin-right:12px;border:1px solid #444}
section{padding:34px 40px 26px;border-bottom:2px solid #d8ccb4}
h2{font-size:30px;margin:0 0 10px;font-family:'Noto Serif SC',serif}h2 .no{display:inline-block;background:#c1443a;color:#fff;font-size:18px;padding:2px 10px;margin-right:14px;vertical-align:middle;border-radius:3px}
h3{font-size:18px;margin:18px 0 8px}
.note{font-size:15px;color:#4a443a;line-height:1.7;margin:0 0 14px}
.wall{display:grid;grid-template-columns:repeat(6,1fr);gap:10px}.card{background:#fff;border:1px solid #d5c9b2;border-radius:4px;overflow:hidden}.card img{width:100%;display:block}
.cap{padding:5px 7px 7px;font-size:12px;line-height:1.45}.ev{color:#4a443a;margin-top:2px}
.tag{display:inline-block;color:#fff;font-size:11px;padding:0 6px;border-radius:3px;margin-right:5px}.mut{color:#857a66;font-size:12px}.num{font-family:Consolas,monospace;font-size:12px}
.row{display:flex;flex-wrap:wrap;gap:14px;margin:8px 0 12px}figure{margin:0}figure img{width:100%;border:1px solid #c9bda5;display:block}figcaption{font-size:13px;color:#4a443a;margin-top:4px}
table.t{border-collapse:collapse;font-size:12.5px;margin:6px 0}table.t th,table.t td{border:1px solid #d5c9b2;padding:4px 7px;text-align:left;vertical-align:top;background:#fffdf8}
table.t th{background:#efe5d2}td.v,th.v{text-align:center;width:46px}.sw{display:inline-block;width:16px;height:12px;border:1px solid #888;vertical-align:middle}
img.sty{width:300px;display:block}table.sty td{vertical-align:middle;font-size:13px;line-height:1.6}
ul{font-size:15px;line-height:1.75;margin:6px 0 0 0;padding-left:22px}.scroll{overflow:hidden}
footer{padding:22px 40px 40px;font-size:13px;color:#6b624f}
"""
page = f'<!doctype html><html><head><meta charset="utf-8"><title>显像管的外侧 设计图纸</title><style>{CSS}</style></head><body>{"".join(body)}<footer>由 tools/make_blueprint.py 从工程数据生成 · https://github.com/jinglinyao/ikiteita-paper-theatre · 歌曲《生きていたんだよな》版权归あいみょん及相关权利人</footer></body></html>'
open(os.path.join(BUILD, 'index.html'), 'w', encoding='utf-8').write(page)
print('html written', len(page))

# ---------------------------------------------------------------- render: Edge through Playwright, in chunks, stitched
MV = os.environ.get('MVKIT', 'E:/sucai/_tools/mvkit')
js = os.path.join(BUILD, 'shot.cjs')
open(js, 'w', encoding='utf-8').write(r"""
const { chromium } = require(process.argv[2] + '/node_modules/playwright');
(async () => {
  const b = await chromium.launch({ channel: 'msedge' }); const p = await b.newPage({ viewport: { width: 1600, height: 1200 }, deviceScaleFactor: 1 });
  await p.goto('file:///' + process.argv[3].replace(/\\/g, '/')); await p.waitForTimeout(800);
  const H = await p.evaluate(() => document.body.scrollHeight); const step = 6000; let i = 0;
  for (let y = 0; y < H; y += step, i++) await p.screenshot({ path: process.argv[4] + '/part_' + String(i).padStart(2, '0') + '.png', clip: { x: 0, y, width: 1600, height: Math.min(step, H - y) }, fullPage: true });
  console.log('height', H, 'parts', i); await b.close();
})();
""")
for f in os.listdir(BUILD):
    if f.startswith('part_'): os.remove(os.path.join(BUILD, f))
r = subprocess.run(['node', js, MV, os.path.join(BUILD, 'index.html'), BUILD], capture_output=True, text=True); print(r.stdout.strip(), r.stderr.strip()[-400:])
parts = sorted(f for f in os.listdir(BUILD) if f.startswith('part_')); ims = [Image.open(os.path.join(BUILD, f)).convert('RGB') for f in parts]
long = Image.new('RGB', (1600, sum(i.height for i in ims))); y = 0
for i in ims: long.paste(i, (0, y)); y += i.height
dst = os.path.join(OUT, '显像管的外侧_设计图纸.jpg'); long.save(dst, quality=86, optimize=True, progressive=True)
print('wrote', dst, long.size, round(os.path.getsize(dst) / 1e6, 1), 'MB')
