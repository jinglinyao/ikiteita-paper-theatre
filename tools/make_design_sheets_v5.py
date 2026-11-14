# -*- coding: utf-8 -*-
"""Design sheets v2, laid out as the nine sheets of the film this one follows for process:
request, pipeline, set, stage and close cameras, timeline, shots, how the screen works, rendering, finish and sound.
Shots sit on measured bar lines (work/bars.json).  python -P tools/make_design_sheets_v2.py -> docs/设计图纸_v2.html, docs/film_v2.json
"""
import json, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_design_sheets import line, rect, text, poly, dim_h, dim_v, arrow, pill, esc, tc, loudness

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FPS, DUR = 24, 193.09
BARS = {int(k): v for k, v in json.load(open(os.path.join(ROOT, 'work', 'bars.json')))['bars'].items()}


def frame(t): return int(round(t * FPS))


# ------------------------------------------------------------------ the room (metres): x along the back wall, z out from it, y up
RW, RD, RH = 3.64, 2.73, 2.40
WIN = dict(z0=0.55, z1=2.20, y0=0.85, y1=2.00)                 # the one window, in the left wall x = 0
TV = dict(x0=1.56, x1=2.08, z0=0.06, z1=0.515, y0=0.551, y1=1.085)
GLASS = dict(x=1.82, z=0.53, y=0.88, w=0.396, h=0.298)
DESK = dict(x0=2.52, x1=3.23, z0=0.05, z1=0.60, top=0.74)
PLANT = dict(x=0.50, z=0.80, r=0.30, h=1.35)
BED = dict(x0=2.78, x1=3.64, z0=0.76, z1=2.73)
CAM = dict(x=1.78, z0=2.45, z1=2.05, y=1.10)
AZ, EL_SUN, EL_MOON = 25.0, 5.0, 35.0                          # light leaves the window heading +x, 25 degrees toward the back wall
TAZ = math.tan(math.radians(AZ))

# ------------------------------------------------------------------ the stage (metres): x across, z upstage from the proscenium, y up
SW, SH, SD = 5.2, 3.6, 4.5                                     # inside; the opening is 4.0 x 3.0 (4:3, the television's picture)
LAYERS = [(0.0, '台口：圆角框、红幕、灯泡、脚灯', 3.0), (0.6, '警戒带和前景纸片', 1.1), (1.4, '表演面：lin、红纸、路灯', 2.4),
          (2.2, '动物头纸人，一排六个', 1.35), (3.0, '近楼（有窗）', 2.6), (3.7, '远楼（有窗）', 3.0), (4.2, '天幕（半透明纸，背后打光）', 3.4), (4.5, '后墙（胶合板）和裸灯泡', 3.6)]
CROWD = [(-2.0, 2.25, '鬣狗'), (-1.2, 2.4, '秃鹫'), (-0.4, 2.5, '乌鸦'), (0.4, 2.5, '狼'), (1.2, 2.4, '鳄鱼'), (2.0, 2.25, '野猪')]
LIN = [(-0.5, 1.3, '①'), (0.0, 1.9, '②'), (0.0, 0.7, '③')]
RED = (0.9, 1.55); LAMP = (1.5, 1.7)

from shots_v5 import SHOTS, LIN_POSE, LIN_ROW, CROWD_ROW, FLIGHTS
BEATS = json.load(open(os.path.join(ROOT, 'work', 'beats.json')))['beats']
B26 = min(range(len(BEATS)), key=lambda i: abs(BEATS[i] - BARS[26]))      # the beat that opens bar 26


def start_of(s):
    if isinstance(s, float): return s
    bar, beat = s
    i = B26 + 4 * (bar - 26) + beat
    return BEATS[i] if i < len(BEATS) else BEATS[-1] + (i - len(BEATS) + 1) * (BEATS[-1] - BEATS[-2])      # the tracker stops just before the last bar ends


CUTS = [start_of(s[0]) for s in SHOTS] + [DUR]
assert all(b > a for a, b in zip(CUTS, CUTS[1:])), 'shots out of order'
TYPE = {'R': '房间', 'X': '进出电视', 'T': '纸剧场', 'A': '动物头纸人', 'L': 'lin', '-': '—'}

SECTIONS = [(0, BARS[12], '前奏'), (BARS[12], BARS[14], '念白'), (BARS[14], BARS[22], '主歌一（快）'), (BARS[22], BARS[26], '导歌'), (BARS[26], BARS[34], '副歌一'),
            (BARS[34], BARS[35], '间'), (BARS[35], BARS[45], '主歌二（快）'), (BARS[45], BARS[49], '导歌二'), (BARS[49], BARS[57], '副歌二'), (BARS[57], BARS[58], '停'),
            (BARS[58], BARS[66], '桥段：静到最强'), (BARS[66], BARS[67], '停'), (BARS[67], BARS[75], '副歌三（最轻）'), (BARS[75], BARS[83], '副歌四（最强）'),
            (BARS[83], 186.6, '再见'), (186.6, DUR, '尾')]
LIGHT = [(0, 1.18, '黑', 'k'), (1.18, BARS[26], '夜：窗外月光蓝，电视冷白，台灯暖', 'tv'), (BARS[26], BARS[34], '转暖：6 窗', 'lamp'), (BARS[34], BARS[45], '冷白', 'tv'),
         (BARS[45], BARS[49], '橙 → 深蓝', 'dusk'), (BARS[49], BARS[57], '暖：20 扇窗', 'lamp'), (BARS[57], BARS[66], '只有一盏裸灯泡', 'k'),
         (BARS[66], BARS[75], '暗蓝；窗外天色渐亮', 'blue'), (BARS[75], BARS[83], '全片最亮最暖', 'lamp'), (BARS[83], 186.6, '逐排熄灭', 'dim'), (186.6, 191.0, '日出', 'sun'), (191.0, DUR, '黑', 'k')]
STAGE = [(0, BARS[4], '纸雪'), (BARS[4], BARS[10], '幕布拉开'), (BARS[10], BARS[14], '搭街角'), (BARS[14], BARS[22], '纸人聚集、纸带、白纸片'), (BARS[22], BARS[26], '纸雨、追光'),
         (BARS[26], BARS[30], '亮 6 扇窗'), (BARS[30], BARS[35], '空追光、黑衣'), (BARS[35], BARS[39], '换纸 2 秒'), (BARS[39], BARS[45], '交叉纸带、对话片'), (BARS[45], BARS[49], '纸太阳落下'),
         (BARS[49], BARS[55], '亮到 20 扇'), (BARS[55], BARS[57], '灭一扇'), (BARS[57], BARS[58], '撤'), (BARS[58], BARS[60], '空台'), (BARS[60], BARS[62], '折鸟'), (BARS[62], BARS[67], '降云、飞'),
         (BARS[67], BARS[75], '鸟落在掌心'), (BARS[75], BARS[83], '所有窗全亮'), (BARS[83], 186.6, '熄灯')]
SFX = [(0.6, '通电“嗒”'), (1.8, '显像管静电声'), (16.3, '幕绳'), (24.96, '路灯“嗒”'), (33.6, '纸带拉出'), (37.94, '纸片翻亮'), (46.58, '纸雨'), (53.06, '追光“嗒”'), (72.52, '纸手滑入'),
       (80.7, '擦纸两秒'), (83.34, '纸带'), (96.32, '纸太阳滑杆'), (117.93, '一盏灯灭'), (122.25, '撤景的纸声'), (124.44, '灯泡“嗒”'), (128.73, '折纸 ×6'), (133.07, '拍翅'),
       (149.3, '落在掌心'), (163.3, '起飞'), (178.26, '窗灯逐排灭'), (184.51, '关机“嗒”'), (187.0, '远处的鸟叫')]


def lens_html(l):
    return '—' if l == '—' else (l.replace(' 微距', ' mm 微距') if '微距' in l else l + ' mm')


# ------------------------------------------------------------------ sheet 03: the room in plan
S = 130.0
def room_plan():
    px, py = 84.0, 44.0
    def X(x): return px + x * S
    def Z(z): return py + z * S                                # the back wall is at the top of the drawing
    W, H = px + RW * S + 104, py + RD * S + 86
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="房间平面图">']
    o.append(rect(X(0) - 10, Z(0) - 10, RW * S + 20, RD * S + 20, 'wall')); o.append(rect(X(0), Z(0), RW * S, RD * S, 'floor'))
    # dawn: the beam from the window to the back wall
    o.append(poly([(X(0), Z(WIN['z0'])), (X(0), Z(WIN['z1'])), (X(RW), Z(max(0.0, WIN['z1'] - RW * TAZ))), (X(RW), Z(0)), (X(WIN['z0'] / TAZ), Z(0))], 'sunbeam'))
    o.append(poly([(X(TV['x0'] + 0.06), Z(GLASS['z'])), (X(TV['x1'] - 0.06), Z(GLASS['z'])), (X(2.75), Z(2.3)), (X(0.9), Z(2.3))], 'tvbeam'))
    o.append(f'<circle cx="{X(3.05):.1f}" cy="{Z(0.30):.1f}" r="{0.55 * S:.1f}" class="lampglow"/>')
    # window, curtain
    o.append(rect(X(0) - 10, Z(WIN['z0']), 10, (WIN['z1'] - WIN['z0']) * S, 'floor'))
    o.append(line(X(0) - 7, Z(WIN['z0']), X(0) - 7, Z(WIN['z1']), 'ln') + line(X(0) - 3, Z(WIN['z0']), X(0) - 3, Z(WIN['z1']), 'ln'))
    o.append('<polyline class="curtain" points="' + ' '.join(f'{X(0) + 7 + 3 * math.sin(i * 1.6):.1f},{Z(WIN["z0"] - 0.1 + i * (WIN["z1"] - WIN["z0"] + 0.2) / 39):.1f}' for i in range(40)) + '"/>')
    # furniture
    o.append(rect(X(BED['x0']), Z(BED['z0']), (BED['x1'] - BED['x0']) * S, (BED['z1'] - BED['z0']) * S, 'furn'))
    o.append(rect(X(1.545), Z(0.05), 0.55 * S, 0.45 * S, 'furn'))
    o.append(rect(X(TV['x0']), Z(TV['z0']), (TV['x1'] - TV['x0']) * S, (TV['z1'] - TV['z0']) * S, 'tvbox'))
    o.append(line(X(GLASS['x'] - GLASS['w'] / 2), Z(GLASS['z']), X(GLASS['x'] + GLASS['w'] / 2), Z(GLASS['z']), 'glass'))
    o.append(rect(X(1.82) - 4, Z(0.25) - 4, 8, 8, 'redf'))
    o.append(rect(X(DESK['x0']), Z(DESK['z0']), (DESK['x1'] - DESK['x0']) * S, (DESK['z1'] - DESK['z0']) * S, 'furn'))
    o.append(rect(X(2.62), Z(0.22), 0.30 * S, 0.21 * S, 'furn2'))
    o.append(f'<circle cx="{X(3.05):.1f}" cy="{Z(0.30):.1f}" r="7" class="lampdot"/>')
    o.append(f'<circle cx="{X(PLANT["x"]):.1f}" cy="{Z(PLANT["z"]):.1f}" r="{PLANT["r"] * S:.1f}" class="plant"/>')
    o.append(f'<circle cx="{X(PLANT["x"] + PLANT["z"] / TAZ):.1f}" cy="{Z(0) + 3:.1f}" r="16" class="shadow"/>')
    o.append(line(X(PLANT['x']), Z(PLANT['z']), X(PLANT['x'] + PLANT['z'] / TAZ), Z(0), 'sunray'))
    # the one wide camera
    o.append(arrow(X(CAM['x']), Z(CAM['z0']), X(CAM['x']), Z(CAM['z1']), 'cam', True)); o.append(arrow(X(CAM['x']), Z(CAM['z1']), X(CAM['x'] + 0.02), Z(1.45), 'cam'))
    o.append(pill(X(CAM['x']), Z(CAM['z0']), '04·46·82'))
    hw = 18 * 2.45 / 28
    o.append(line(X(CAM['x']), Z(CAM['z0']), X(CAM['x'] - hw), Z(0), 'dim') + line(X(CAM['x']), Z(CAM['z0']), X(CAM['x'] + hw), Z(0), 'dim'))
    o.append(pill(X(1.82), Z(1.22), '01·07·74·81')); o.append(arrow(X(1.82), Z(1.12), X(1.82), Z(0.62), 'cam'))
    o.append(pill(X(2.86), Z(0.92), '05·52')); o.append(arrow(X(2.86), Z(0.84), X(2.80), Z(0.46), 'cam'))
    o.append(text(X(1.0), Z(0) - 20, '后墙 3.64 m', 't', 'middle'))
    o.append(text(X(2.88), Z(0) - 20, '书桌、台灯', 'tm', 'middle'))
    o.append(dim_v(X(RW) + 26, Z(0), Z(RD), '2.73 m'))
    o.append(text(X(0) - 16, Z(1.375) + 4, '窗 1.65 m', 't', 'end'))
    o.append(text(X(PLANT['x']), Z(PLANT['z']) + 4, '植物', 't', 'middle'))
    o.append(text(X(1.82), Z(0.66) + 12, '电视', 't', 'middle'))
    o.append(text(X(3.21), Z(1.8), '床', 'tm', 'middle'))
    o.append(text(X(2.31), Z(0.24), '叶影', 'tsun', 'middle'))
    o.append(text(X(1.0), Z(1.72), '日出的光：出窗后', 'tsun', 'middle')); o.append(text(X(1.0), Z(1.72) + 15, '偏向后墙 25°', 'tsun', 'middle'))
    o.append(text(X(1.82), Z(RD) + 34, '全片的全景只有这一个机位，出现三次；28 mm 的视野是两条细线之间', 'tm', 'middle'))
    o.append(text(X(1.82), Z(RD) + 52, '红点：片尾纸鸟停在电视顶上', 'tred', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ sheet 03: the back wall as the wide camera sees it
def room_elev():
    px, py = 96.0, 30.0
    def X(x): return px + x * S
    def Y(y): return py + (RH - y) * S
    W, H = px + RW * S + 40, py + RH * S + 62
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="全景机位看到的后墙">']
    o.append(rect(X(0) - 9, Y(RH) - 9, RW * S + 18, RH * S + 18, 'wall')); o.append(rect(X(0), Y(RH), RW * S, RH * S, 'floor'))
    k = math.tan(math.radians(EL_SUN)) / math.cos(math.radians(AZ))            # drop per metre along the wall
    xa = WIN['z0'] / TAZ
    band = [(X(xa), Y(WIN['y1'] - k * xa)), (X(RW), Y(WIN['y1'] - k * RW)), (X(RW), Y(WIN['y0'] - k * RW)), (X(xa), Y(WIN['y0'] - k * xa))]
    o.append(poly(band, 'sunbeam'))
    hw = 18 * 2.45 / 28; hh = hw * 9 / 16
    o.append(rect(X(CAM['x'] - hw), Y(CAM['y'] + hh), 2 * hw * S, 2 * hh * S, 'dash'))
    o.append(rect(X(1.545), Y(0.551), 0.55 * S, 0.551 * S, 'furn'))
    o.append(rect(X(TV['x0']), Y(TV['y1']), (TV['x1'] - TV['x0']) * S, (TV['y1'] - TV['y0']) * S, 'tvbox'))
    o.append(rect(X(GLASS['x'] - GLASS['w'] / 2), Y(GLASS['y'] + GLASS['h'] / 2), GLASS['w'] * S, GLASS['h'] * S, 'screen', 'rx="9"'))
    o.append(poly([(X(1.80), Y(1.085)), (X(1.90), Y(1.085)), (X(1.93), Y(1.135)), (X(1.84), Y(1.115))], 'redf'))
    o.append(rect(X(DESK['x0']), Y(DESK['top']), (DESK['x1'] - DESK['x0']) * S, 0.035 * S, 'furn'))
    o.append(line(X(DESK['x0'] + 0.05), Y(DESK['top']), X(DESK['x0'] + 0.05), Y(0), 'ln') + line(X(DESK['x1'] - 0.05), Y(DESK['top']), X(DESK['x1'] - 0.05), Y(0), 'ln'))
    o.append(line(X(3.10), Y(0.775), X(3.02), Y(1.12), 'ln')); o.append(f'<circle cx="{X(3.02):.1f}" cy="{Y(1.13):.1f}" r="9" class="lampdot"/>')
    o.append(rect(X(2.62), Y(0.80), 0.30 * S, 0.025 * S, 'furn2'))
    o.append(rect(X(2.62), Y(1.95), 0.44 * S, 0.60 * S, 'furn'))
    o.append(rect(X(0.36), Y(0.32), 0.28 * S, 0.32 * S, 'furn'))
    for dx, dy, r in [(-0.12, 1.02, 26), (0.10, 1.20, 24), (-0.02, 0.78, 30), (0.16, 0.92, 20), (-0.2, 0.7, 18)]:
        o.append(f'<circle cx="{X(0.50 + dx):.1f}" cy="{Y(dy):.1f}" r="{r}" class="plant"/>')
    for dx, dy, r in [(-0.1, 1.18, 20), (0.1, 1.32, 17), (0.0, 1.02, 22)]:
        o.append(f'<circle cx="{X(2.26 + dx):.1f}" cy="{Y(dy):.1f}" r="{r}" class="shadow"/>')
    o.append(text(X(1.82), Y(1.24), '红纸鸟', 'tred', 'middle'))
    o.append(text(X(1.82), Y(0.24), '电视柜', 'tm', 'middle'))
    o.append(text(X(2.84), Y(2.02), '画框', 'tm', 'middle'))
    o.append(text(X(0.50), Y(1.48), '植物', 'tm', 'middle'))
    o.append(text(X(2.30), Y(1.52), '叶影', 'tsun', 'middle'))
    o.append(text(X(3.02), Y(1.30), '台灯', 'tm', 'middle'))
    o.append(text(X(2.9), Y(0.36), '书桌', 'tm', 'middle'))
    o.append(text(X(1.20), Y(2.12), '日出时的光带', 'tsun', 'middle'))
    o.append(dim_v(X(0) - 24, Y(RH), Y(0), '层高 2.40 m', left=True))
    o.append(dim_h(X(0), X(RW), Y(0) + 18, '3.64 m', above=False))
    o.append(text(X(1.82), Y(0) + 48, '虚线框是 28 mm 镜头在 2.45 m 处的画面；左墙的窗在画面外，只看到它投进来的光', 'tm', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ sheet 04: the stage in section
C = 96.0
def stage_section():
    px, py = 150.0, 46.0
    def Z(z): return px + z * C
    def Y(y): return py + (SH - y) * C
    W, H = px + SD * C + 300, py + SH * C + 84
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="纸舞台剖面">']
    o.append(rect(Z(0), Y(SH), SD * C, SH * C, 'floor')); o.append(rect(Z(0), Y(SH), SD * C, SH * C, 'ln'))
    o.append(rect(Z(-0.12), Y(SH), 0.12 * C, 0.6 * C, 'tvbox')); o.append(rect(Z(-0.12), Y(0.0), 0.12 * C, 0.0, 'tvbox'))
    o.append(line(Z(0), Y(0), Z(SD), Y(0), 'gloss'))
    o.append(poly([(Z(-0.1), Y(2.9)), (Z(-0.1), Y(0.05)), (Z(-1.35), Y(-0.2)), (Z(-1.35), Y(3.3))], 'tvbeam'))
    for (z, name, h) in LAYERS[1:]:
        o.append(line(Z(z), Y(0), Z(z), Y(h), 'layer'))
    for (z, name, h) in LAYERS:
        o.append(line(Z(z), Y(0), Z(z), Y(0) + 6, 'dim')); o.append(text(Z(z), Y(0) + 19, f'{z:g}', 'td', 'middle'))
    o.append(text(Z(SD / 2), Y(0) + 38, '离台口的距离（米）', 'tm', 'middle'))
    lx = Z(SD) + 30
    o.append(text(lx, Y(SH) + 12, '从前到后', 't'))
    for i, (z, name, h) in enumerate(LAYERS):
        o.append(text(lx, Y(SH) + 36 + i * 22, f'{z:g}', 'td')); o.append(text(lx + 28, Y(SH) + 36 + i * 22, name, 'tm'))
    # lin at the acting plane, 1.55 m
    o.append(line(Z(1.4), Y(0), Z(1.4), Y(1.40), 'herl')); o.append(f'<circle cx="{Z(1.4):.1f}" cy="{Y(1.46):.1f}" r="9" class="her"/>')
    o.append(text(Z(1.4), Y(1.66), 'lin 1.55 m', 'tm', 'middle'))
    o.append(rect(Z(1.55) - 5, Y(0) - 4, 10, 4, 'redf'))
    o.append(text(Z(2.2), Y(1.45), '纸人 1.35 m', 'tm', 'middle'))
    for (z, y) in [(0.05, 2.95), (0.12, 0.12), (0.5, 3.45), (0.9, 3.45), (1.7, 2.4), (4.35, 1.7), (4.42, 2.6)]:
        o.append(f'<circle cx="{Z(z):.1f}" cy="{Y(y):.1f}" r="4.5" class="lampdot"/>')
    for z in (1.0, 2.6, 3.4):
        o.append(f'<circle cx="{Z(z):.1f}" cy="{Y(3.5):.1f}" r="2.8" class="bar"/>')
    o.append(text(Z(2.1), Y(SH) - 12, '顶上：灯条、追光、三道吊杆（纸带、纸雨、纸云、窗灯）', 'tm', 'middle'))
    o.append(text(Z(-0.72), Y(1.9), '观众和', 'tm', 'middle')); o.append(text(Z(-0.72), Y(1.9) + 15, '正面机位', 'tm', 'middle'))
    o.append(text(Z(-0.72), Y(1.3), '台口高', 'tm', 'middle')); o.append(text(Z(-0.72), Y(1.3) + 15, '3.0 m', 'td', 'middle'))
    o.append(text(Z(2.1), Y(0) + 62, '从侧面剖开舞台。竖线是一层层立着的纸片；橙色圆点是灯', 'tm', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ sheet 04: the stage in plan with cameras, lin's three marks and the six paper people
def stage_plan():
    px, py = 60.0, 58.0
    def X(x): return px + (x + SW / 2) * C
    def Z(z): return py + (SD - z) * C
    W, H = px * 2 + SW * C, py + (SD + 2.9) * C + 26
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="舞台平面和机位">']
    o.append(rect(X(-SW / 2), Z(SD), SW * C, SD * C, 'floor')); o.append(rect(X(-SW / 2), Z(SD), SW * C, SD * C, 'ln'))
    o.append(line(X(-SW / 2), Z(0), X(-2.0), Z(0), 'thick') + line(X(2.0), Z(0), X(SW / 2), Z(0), 'thick'))
    o.append(line(X(-2.0), Z(0), X(2.0), Z(0), 'glass'))
    for (z, a, b) in [(0.6, -2.4, 2.4), (3.0, -2.6, -0.8), (3.0, 0.9, 2.6), (3.7, -2.6, 2.6), (4.2, -2.6, 2.6)]:
        o.append(line(X(a), Z(z), X(b), Z(z), 'layer'))
    for z in (0.6, 1.4, 2.2, 3.0, 3.7, 4.2):
        o.append(text(X(-SW / 2) - 8, Z(z) + 4, f'{z:g}', 'td', 'end'))
    o.append(f'<circle cx="{X(RED[0]):.1f}" cy="{Z(RED[1]):.1f}" r="{0.6 * C:.1f}" class="spot"/>')
    o.append(rect(X(RED[0]) - 5, Z(RED[1]) - 5, 10, 10, 'redf'))
    o.append(f'<circle cx="{X(LAMP[0]):.1f}" cy="{Z(LAMP[1]):.1f}" r="5" class="lampdot"/>')
    for x, z, n in CROWD:
        o.append(rect(X(x) - 13, Z(z) - 5, 26, 10, 'crowd', 'rx="3"')); o.append(text(X(x), Z(z) - 10, n, 'tm', 'middle'))
    for x, z, n in LIN:
        o.append(f'<circle cx="{X(x):.1f}" cy="{Z(z):.1f}" r="11" class="her"/>'); o.append(text(X(x), Z(z) + 4, n, 'tp', 'middle'))
    cams = [('16·67', 0.0, -1.7, 0.0, 1.6, None), ('24', -0.9, -1.05, -0.56, 0.9, None), ('47', 0.9, -0.95, -0.3, 1.0, None), ('55', 0.75, 0.0, 0.12, 1.72, None),
            ('79', -0.7, -1.65, -0.1, 0.4, None), ('22', -2.0, 4.0, -0.6, 0.3, None), ('30', 1.6, 3.9, 1.0, 1.9, None), ('12', -2.3, 0.95, -1.9, 1.9, None)]
    for lab, x, z, lx, lz, _ in cams:
        o.append(arrow(X(x), Z(z), X(lx), Z(lz), 'cam')); o.append(pill(X(x), Z(z), lab))
    o.append(arrow(X(2.2), Z(1.0), X(2.2), Z(-2.5), 'cam', True)); o.append(pill(X(2.2), Z(-1.2), '71 飞出'))
    o.append(pill(X(RED[0]) + 2, Z(RED[1]) + 22, '31·37 俯'))
    o.append(text(X(RED[0]) + 60, Z(RED[1]) + 4, '红纸', 'tred')); o.append(text(X(LAMP[0]) + 10, Z(LAMP[1]) - 8, '路灯', 'tm'))
    o.append(text(X(0), Z(SD) - 10, '后墙', 'tm', 'middle'))
    o.append(text(X(2.05), Z(0) + 16, '台口 4.0 m', 'ttv'))
    o.append(text(X(0), Z(-2.45), '电视画面用的正面机位在台口外 5.56 m（图外），50 mm，4:3', 'tm', 'middle'))
    o.append(text(X(0), Z(-2.75), '紫色圆点 ①②③ 是 lin 的三个站位：追光里、空台、台口', 'tm', 'middle'))
    o.append(dim_h(X(-SW / 2), X(SW / 2), Z(SD) - 26, '舞台宽 5.2 m'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ sheet 04: how one animal-headed paper person is built
def puppet():
    u = 150.0; px, py = 215.0, 30.0
    def X(x): return px + x * u
    def Y(y): return py + (1.5 - y) * u
    W, H = 660, py + 1.5 * u + 44
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="动物头纸人的构造">']
    o.append(line(X(-0.7), Y(0), X(1.6), Y(0), 'gloss'))
    o.append(poly([(X(-0.22), Y(0.0)), (X(0.22), Y(0.0)), (X(0.18), Y(0.93)), (X(-0.18), Y(0.93))], 'furn'))                       # body card
    o.append(poly([(X(0.16), Y(0.86)), (X(0.48), Y(0.98)), (X(0.50), Y(0.90)), (X(0.17), Y(0.74))], 'furn2'))                     # the arm that lifts
    o.append(rect(X(0.44), Y(1.16), 0.17 * u, 0.24 * u, 'card'))                                                                   # the white card it holds
    o.append(poly([(X(-0.21), Y(0.93)), (X(0.21), Y(0.93)), (X(0.24), Y(1.22)), (X(0.06), Y(1.35)), (X(-0.16), Y(1.33)), (X(-0.30), Y(1.14))], 'crowdf'))   # head
    o.append(poly([(X(-0.30), Y(1.14)), (X(-0.40), Y(1.08)), (X(-0.24), Y(1.02))], 'crowdf'))                                     # snout
    o.append(f'<circle cx="{X(-0.06):.1f}" cy="{Y(1.20):.1f}" r="5" class="lampdot"/>')
    o.append(f'<circle cx="{X(0.0):.1f}" cy="{Y(0.93):.1f}" r="5" class="bar"/>' + f'<circle cx="{X(0.16):.1f}" cy="{Y(0.83):.1f}" r="5" class="bar"/>')
    o.append(poly([(X(0.9), Y(0.0)), (X(1.02), Y(0.0)), (X(1.02), Y(1.30)), (X(0.99), Y(1.30))], 'furn'))                         # side view: the card
    o.append(poly([(X(1.02), Y(0.0)), (X(1.34), Y(0.0)), (X(1.02), Y(0.62))], 'furn2'))                                            # side view: the strut
    o.append(dim_v(X(-0.62), Y(1.35), Y(0), '高 1.35 m', left=True))
    o.append(dim_v(X(0.72), Y(1.35), Y(0.93), '头 0.42 m'))
    o.append(text(X(0.0), Y(0.45), '身体：普通人的衣服', 'tm', 'middle'))
    o.append(text(X(0.52), Y(1.44), '白纸片', 'tm', 'middle'))
    o.append(text(X(1.16), Y(1.36), '侧面', 'tm', 'middle')); o.append(text(X(1.30), Y(0.30), '背后的撑脚', 'tm'))
    o.append(text(X(0.45), Y(0) + 26, '黑点是两处关节：脖子能歪 15°，举纸片的手臂能抬放。眼睛两张替换（睁、闭）', 'tm', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ sheet 05: the one clock
def timeline(asr):
    K, L, TOP = 8.5, 100.0, 26.0
    rows = ['小节', '歌的段落', '响度（实测）', '镜头', '光', '舞台上', 'lin', '动物头纸人', '音效', '字幕 45 句']
    hs = [18.0, 28.0, 26.0, 40.0, 28.0, 28.0, 28.0, 28.0, 46.0, 16.0]
    ys = [TOP + sum(hs[:i]) + 4 * i for i in range(len(rows))]
    W, H = L + DUR * K + 14, ys[-1] + hs[-1] + 12
    def tx(t): return L + t * K
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" width="{W:.0f}" height="{H:.0f}" role="img" aria-label="全片总时间线">']
    for t in range(0, int(DUR) + 1, 10):
        o.append(line(tx(t), TOP - 6, tx(t), H - 8, 'grid')); o.append(text(tx(t) + 3, 13, f'{t // 60}:{t % 60:02d}', 'td'))
    for i, r in enumerate(rows):
        o.append(text(L - 8, ys[i] + hs[i] / 2 + 4, r, 'tm', 'end'))
    for n in sorted(BARS):
        o.append(line(tx(BARS[n]), ys[0], tx(BARS[n]), ys[0] + (hs[0] if n % 4 == 2 else 6), 'dim'))
        if n % 4 == 2: o.append(text(tx(BARS[n]) + 3, ys[0] + 13, str(n), 'td'))

    def blocks(i, items, cls):
        for it in items:
            t0, t1, lab = it[0], it[1], it[2]
            c2 = cls if len(it) < 4 else 'L' + it[3]
            w = (t1 - t0) * K - 1
            o.append(f'<g><title>{esc(lab)}　{tc(t0)}–{tc(t1)}</title>' + rect(tx(t0), ys[i], w, hs[i], 'blk ' + c2, 'rx="3"'))
            if 10.6 * len(lab) + 8 <= w: o.append(text(tx(t0) + 5, ys[i] + hs[i] / 2 + 4, lab, 'tb'))
            o.append('</g>')
    blocks(1, SECTIONS, 'Bsec')
    db = loudness(); lo, hi = -42.0, -9.0
    pts = [(tx(0), ys[2] + hs[2])] + [(tx(j * 0.5 + 0.25), ys[2] + hs[2] - max(0.0, min(1.0, (v - lo) / (hi - lo))) * hs[2]) for j, v in enumerate(db)] + [(tx(len(db) * 0.5), ys[2] + hs[2])]
    o.append(poly(pts, 'loud'))
    for k, sh in enumerate(SHOTS):
        t0, t1 = CUTS[k], CUTS[k + 1]; w = (t1 - t0) * K - 1
        o.append(f'<g><title>S{k + 1:02d}　{tc(t0)}–{tc(t1)}　{esc(sh[5])}</title>' + rect(tx(t0), ys[3], w, hs[3], 'blk V' + (sh[1] if sh[1] != '-' else 'N'), 'rx="3"') +
                 text(tx(t0) + w / 2, ys[3] + 16, f'{k + 1:02d}', 'tb', 'middle') + (text(tx(t0) + w / 2, ys[3] + 32, sh[2].split(' ')[0], 'td', 'middle') if sh[2] != '—' and w > 17 else '') + '</g>')
    blocks(4, LIGHT, ''); blocks(5, STAGE, 'Bstage')
    blocks(6, [(CUTS[k], CUTS[k + 1], lab) for k, lab in LIN_ROW], 'Bher')
    blocks(7, [(CUTS[k], CUTS[k + 1], lab) for k, lab in CROWD_ROW], 'Bcrowd')
    lanes = [-1e9, -1e9, -1e9]
    for t, lab in SFX:
        x = tx(t); kk = next((j for j in range(3) if lanes[j] + 6 <= x), 2); lanes[kk] = x + 6 + 10.6 * len(lab)
        o.append(line(x, ys[8], x, ys[8] + hs[8], 'sfx')); o.append(text(x + 4, ys[8] + 11 + kk * 14, lab, 'tb'))
    for s in asr:
        o.append(rect(tx(s['t0']), ys[9] + 3, max(2.0, (s['t1'] - s['t0']) * K - 1), hs[9] - 6, 'cap', 'rx="2"'))
    o.append('</svg>')
    return ''.join(o)


def shot_table():
    o = ['<table class="shots"><thead><tr><th>镜号</th><th>起点<br>小节·拍</th><th>时间</th><th>帧</th><th>类型</th><th>镜头</th><th>机位和运动</th><th>发生的一件事</th><th>焦点</th><th>怎么接进来</th></tr></thead><tbody>']
    inv = {v: k for k, v in BARS.items()}
    for k, sh in enumerate(SHOTS):
        t0, t1 = CUTS[k], CUTS[k + 1]
        bars = '—' if isinstance(sh[0], float) else (f'{sh[0][0]}' if sh[0][1] == 0 else f'{sh[0][0]}·{sh[0][1] + 1}')
        beats = int(round((t1 - t0) / 0.54))
        ty = sh[1] if sh[1] != '-' else 'N'
        lens = '—' if sh[2] == '—' else lens_html(sh[2]) + f'<br>f/{sh[3]}'
        pose = f'<br><span class="sub">姿势：{LIN_POSE[k]}</span>' if k in LIN_POSE else ''
        o.append(f'<tr><td class="num">S{k + 1:02d}</td><td class="num">{bars}</td><td class="num">{tc(t0)}<br><span class="sub">{t1 - t0:.1f} 秒，{beats} 拍</span></td>'
                 f'<td class="num">{frame(t0)}<br><span class="sub">{frame(t1) - frame(t0)} 帧</span></td><td><span class="tag y{ty}">{TYPE[sh[1]]}</span></td>'
                 f'<td class="num nowrap">{lens}</td><td>{esc(sh[4])}</td><td class="act">{esc(sh[5])}{pose}</td><td>{esc(sh[6])}</td><td class="nowrap">{esc(sh[7])}</td></tr>')
    o.append('</tbody></table>')
    return ''.join(o)


def main():
    asr = json.load(open(os.path.join(ROOT, 'work', 'asr.json'), encoding='utf-8'))
    n = len(SHOTS); secs = {}
    for k, sh in enumerate(SHOTS):
        secs[sh[1]] = secs.get(sh[1], 0.0) + CUTS[k + 1] - CUTS[k]
    pct = {k: 100 * v / DUR for k, v in secs.items()}
    film = dict(fps=FPS, duration=DUR, frames=frame(DUR), bars=BARS,
                shots=[dict(id=f'S{k + 1:02d}', t0=CUTS[k], t1=CUTS[k + 1], f0=frame(CUTS[k]), f1=frame(CUTS[k + 1]), type=s[1], lens=s[2], fstop=s[3], camera=s[4],
                            event=s[5], focus=s[6], join=s[7], pose=LIN_POSE.get(k)) for k, s in enumerate(SHOTS)],
                light=[dict(t0=a, t1=b, state=c, key=d) for a, b, c, d in LIGHT], stage=[dict(t0=a, t1=b, state=c) for a, b, c in STAGE], sfx=[dict(t=a, name=b) for a, b in SFX],
                room=dict(size=[RW, RD, RH], window=WIN, tv=TV, glass=GLASS, desk=DESK, plant=PLANT, camera=CAM, light_azimuth=AZ, sun=EL_SUN, moon=EL_MOON),
                stage_set=dict(inside=[SW, SH, SD], opening=[4.0, 3.0], layers=[dict(z=a, name=b, height=c) for a, b, c in LAYERS], crowd=CROWD, lin_marks=LIN, red_paper=RED, lamp=LAMP))
    json.dump(film, open(os.path.join(ROOT, 'docs', 'film_v5.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

    v1 = open(os.path.join(ROOT, 'tools', 'design_sheets_template.html'), encoding='utf-8').read()
    style = v1[v1.index('<style>') + 7:v1.index('</style>')]
    tpl = open(os.path.join(ROOT, 'tools', 'design_sheets_template_v5.html'), encoding='utf-8').read()
    durs = [CUTS[k + 1] - CUTS[k] for k in range(n - 1)]
    lens = [len([1 for d in durs if abs(d / 0.54 - m) < 0.6]) for m in (4, 6)]
    li = min(LIN_POSE); pilot_end = li + 6
    na = sum(1 for x in SHOTS if x[1] == 'A')
    rep = {'{{STYLE}}': style, '{{ROOMPLAN}}': room_plan(), '{{ROOMELEV}}': room_elev(), '{{STAGESEC}}': stage_section(), '{{STAGEPLAN}}': stage_plan(), '{{PUPPET}}': puppet(),
           '{{TIMELINE}}': timeline(asr), '{{SHOTS}}': shot_table(), '{{NSHOTS}}': str(n), '{{NFRAMES}}': f'{frame(DUR):,}', '{{AVG}}': f'{DUR / n:.1f}',
           '{{PT}}': f'{pct["T"]:.0f}', '{{PL}}': f'{pct["L"]:.0f}', '{{PA}}': f'{pct["A"]:.0f}', '{{PR}}': f'{pct["R"] + pct["X"]:.0f}',
           '{{SL}}': f'{secs["L"]:.1f}', '{{NL}}': str(sum(1 for s in SHOTS if s[1] == 'L')), '{{N4}}': str(lens[0]), '{{N6}}': str(lens[1]), '{{LONGEST}}': f'{max(durs):.1f}', '{{NA}}': str(na), '{{FLIGHTS}}': '、'.join(f'S{i + 1:02d}' for i in FLIGHTS), '{{N2}}': str(len([1 for d in durs if abs(d / 0.54 - 2) < 0.6])), '{{N8}}': str(len([1 for d in durs if abs(d / 0.54 - 8) < 0.6])), '{{PILOTSHOTS}}': f'S{li + 1:02d}–S{pilot_end:02d}',
           '{{PILOT}}': f'{tc(CUTS[li])}–{tc(CUTS[pilot_end])}', '{{PILOTLEN}}': f'{CUTS[pilot_end] - CUTS[li]:.1f}'}
    for k, v in rep.items():
        tpl = tpl.replace(k, v)
    assert '{{' not in tpl, tpl[tpl.index('{{'):tpl.index('{{') + 40]
    out = os.path.join(ROOT, 'docs', '设计图纸_v5.html')
    open(out, 'w', encoding='utf-8').write(tpl)
    print('wrote', out, len(tpl), 'chars;', n, 'shots; seconds by type', {k: round(v, 1) for k, v in secs.items()}, 'percent', {k: round(v, 1) for k, v in pct.items()})
    print('shots of 4 / 6 beats:', lens, ' longest', round(max(durs), 2), ' shortest', round(min(durs), 2), ' lin seconds', round(secs['L'], 1))
    print('numbers:', {k + 1: s[5][:12] for k, s in enumerate(SHOTS) if s[1] in 'LRX'})


if __name__ == '__main__':
    main()
