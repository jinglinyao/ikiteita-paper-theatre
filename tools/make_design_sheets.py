# -*- coding: utf-8 -*-
"""Design sheets for the paper-theatre film: set plan, sections, theatre box, camera plan, master timeline, shot list.
Everything is drawn from the numbers in this file; the same numbers are written to docs/film_v1.json (the film's one clock).
    python tools/make_design_sheets.py      ->  docs/设计图纸_v1.html, docs/film_v1.json
"""
import json, math, os, wave
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FPS, DUR = 24, 193.09

# ------------------------------------------------------------------ the set (metres)
RW, RD, RH = 2.73, 3.64, 2.40                    # room: x across, z from the door wall to the window wall, y up
WIN = dict(x0=0.54, x1=2.19, y0=0.85, y1=2.00)   # the one window, in the wall z = RD
TV = dict(x0=0.075, x1=0.53, z0=1.94, z1=2.46, y0=0.551, y1=1.085)   # television 02 scaled 1.3: 52 x 53.4 x 45.5 cm
STAND = dict(x0=0.075, x1=0.525, z0=1.925, z1=2.475, y1=0.551)
GLASS = dict(x=0.53, z=2.20, y=0.88, w=0.396, h=0.298)
BED = dict(x0=1.875, x1=2.73, z0=1.21, z1=3.19, seat=0.42, back=1.127)
DESK = dict(x0=0.0, x1=0.546, z0=0.895, z1=1.607, top=0.74)
HER = dict(x=1.78, z=2.20, eye=1.05)
SUN_EL, SUN_AZ = 5.0, 12.0
SLOPE = math.tan(math.radians(SUN_EL)) / math.cos(math.radians(SUN_AZ))     # drop per metre of z
SHIFT = math.tan(math.radians(SUN_AZ))                                       # sideways drift per metre of z

# ------------------------------------------------------------------ the theatre inside the television (centimetres)
ST_W, ST_H, ST_D = 46.0, 34.0, 38.0              # inside the cabinet; opening 39.6 x 29.8 behind the glass
LAYERS = [(1.5, '台口黑框', 0), (4, '前排纸人', 8), (9, '后排纸人', 9), (14, '表演面：红纸、路灯', 19),
          (20, '近楼（有窗）', 24), (27, '远楼（有窗）', 28), (34, '天幕（半透明纸，背后打光）', 33), (38, '后墙（纸板）', 34)]

# ------------------------------------------------------------------ the film: cuts snapped to measured beats (work/beats.json)
CUTS = [0, 8.17, 16.30, 23.87, 29.81, 34.69, 39.01, 42.79, 47.65, 51.99, 55.24, 60.07, 64.41, 68.20, 71.98, 75.77, 82.25,
        84.42, 88.19, 92.53, 96.32, 101.70, 104.98, 109.83, 114.13, 118.47, 121.72, 124.97, 129.82, 133.61, 138.46,
        143.89, 148.72, 153.07, 157.38, 160.63, 170.37, 175.22, 180.84, 185.50, 190.00, DUR]
# where: R room, P paper theatre, X crosses the glass.  (where, lens, f-stop, camera, what happens, focus, face?)
SHOTS = [
 ('R', '50', '2.8', '玻璃外 45 cm，略低 5°；缓推 3 cm', '黑。电视“嗒”一声通电，一条白线撑开成画面。里面是纸屑做的雪花，逐格下落。玻璃上映着台灯的一点暖光', '台口黑框 → 纸雪', 0),
 ('R', '24', '4', '门边，离电视 2.4 m，高 1.25 m；向前推 0.5 m', '房间第一次完整出现：左前是亮着台灯的书桌，左中是电视，右边她坐在床沿，身后是拉着的窗帘。片名 0:10–0:15', '电视', 0),
 ('R', '50', '2', '她右肩后 0.55 m，高 1.15 m；向左横移 20 cm', '越过她的头发看电视。屏幕里纸街角的布景一片片滑进来', '发梢 → 屏幕', 0),
 ('R', '85', '2.2', '窗一侧，离她的脸 0.9 m，与眼同高；漂移 3 cm', '她的侧脸，电视的冷光在脸上闪。0:27 眼睛从下方抬到屏幕。不动嘴', '近侧的眼睛', 1),
 ('X', '35', '2.8', '从玻璃外 45 cm 直推到入内 2 cm（0:32 过玻璃）', '镜头穿过玻璃，扫描线淡掉，纸街角的纵深打开：路灯、楼、远处的天幕', '路灯下的地面', 0),
 ('P', '24', '4', '入内 1 cm，正对台心，仰 2°；微推 1 cm', '黄纸带贴着镜头横拉过去；两串纸人从左右侧幕滑入，在前景成了虚的大剪影，逐格', '表演面', 0),
 ('P', '50', '2.8', '入内 1 cm；贴着纸人向右横移 9 cm', '纸人举起白纸片，按拍子一张张亮。前后两排错开', '后排纸人', 0),
 ('P', '70 微距', '2.8', '离红纸 14 cm → 9 cm，高度从 12 cm 降到 5 cm', '路灯下的一小张红纸。前景的纸人虚成光斑', '红纸', 0),
 ('P', '50', '2', '入内 16 cm，回头朝玻璃拍', '从纸人空白的背面之间望出去：纸偶她站在台口最前沿，双手遮眼。她身后隔着玻璃，是暗房间里巨大的、虚掉的她', '纸偶她', 0),
 ('X', '50', '2', '同上一镜机位；只移焦点，末尾前推 2 cm', '焦点从纸偶移到玻璃外真正的她：同一个姿势，手从嘴边慢慢放下。脸隔着玻璃和反光', '纸偶 → 玻璃外 1.4 m 的她', 0),
 ('P', '35', '2.8', '离近楼 14 cm；沿楼面上升 8 cm', '每唱一声“活着”亮一扇窗，共 5 扇。每扇里一件剪影：台灯、闹钟、笔记本、雨伞、自行车', '近楼的窗', 0),
 ('R', '35', '2.8', '她左肩后 0.5 m → 0.8 m，后拉', '第 6 扇窗亮。电视的光由冷转暖，漫到她身上和墙上。她身体前倾，手到胸口', '屏幕', 0),
 ('P', '35', '4', '入内 0 → 5 cm，缓推', '布景暗下，只剩一圈追光打在红纸的位置。圈里没有人', '追光圈', 0),
 ('P', '85', '2.8', '入内 12 cm，朝玻璃拍，离纸偶 9 cm；微移 1 cm', '纸偶她的脸。她第一次“唱”：五张替换嘴逐格换', '嘴', 0),
 ('P', '50', '2.8', '离手 10 cm；跟着手横移 6 cm', '穿黑衣的检场人的纸手从右侧幕伸进来（歌舞伎里换景的黑衣）', '手', 0),
 ('P', '70 微距', '3.5', '红纸正上方 14 cm，俯 80°；缓降 2 cm', '红纸被换成一张干净的灰纸。换的过程正好 2 秒（1:20.7–1:22.7）', '纸面', 0),
 ('R', '70 微距', '2.8', '离她腿上的手 0.3 m；固定，微移 1 cm', '她放在腿上的手指收紧。手上的光从暖变回冷', '指节', 0),
 ('R', '35', '4', '玻璃外 38 cm；后拉 2 cm', '整块屏幕：两条黄纸带在台口交叉；纸人头上弹出空白的对话纸片。玻璃上映着她的轮廓', '纸带', 0),
 ('P', '24', '4', '入内 2 cm；上升 16 cm，同时下俯到 55°', '越过布景的顶沿，看到背面：木条、吊线、胶带、铅笔写的编号', '布景顶沿', 0),
 ('X', '35', '2.8', '入内 30 cm、离台面 26 cm，朝玻璃拍；固定，微移 1 cm', '“在远处”：越过布景的背面和吊线，玻璃外很远的地方坐着一个小小的她', '吊线 → 她', 0),
 ('R', '85', '2', '同 S04 机位，离脸 0.8 m；推 4 cm', '电视的光在她脸上从白变橙。眼睛湿，眨眼。不动嘴', '眼睛', 1),
 ('R', '35', '4', '门一侧，离电视和她的连线 1.6 m，高 1.0 m；固定', '左边电视，右边她的侧影，中间是窗帘。电视里纸太阳沿杆滑下，天幕由橙变深蓝，纸路灯亮；墙上的光跟着变', '她', 0),
 ('P', '50', '2.8', '离远楼 22 cm；向左横移 10 cm', '窗一拍一扇，亮到 20 扇', '远楼的窗', 0),
 ('P', '70', '2.8', '入内 1 cm，离纸偶 13 cm；推 3 cm', '纸偶她正面，带关节的手臂逐格张开，替换嘴唱副歌。身后的亮窗虚成一片', '脸', 0),
 ('R', '70 微距', '2.8', '离桌上的东西 0.3 m；沿桌横移 12 cm', '台灯下她自己的笔记本和闹钟，和刚才窗里的剪影一模一样', '笔记本 → 闹钟', 0),
 ('R', '85', '2', '她左前方低位，离手 0.6 m；上摇到下巴', '她低头看自己摊开的手。只拍到嘴以下', '手', 0),
 ('R', '35', '4', '玻璃外 38 cm（同 S18）；后拉 3 cm', '所有纸片向上飞走、向两侧滑走，露出光秃的纸板后墙。房间一级一级暗下去', '后墙', 0),
 ('X', '35', '2.8', '玻璃外 8 cm → 6 cm', '一盏裸灯泡“嗒”地亮。纸偶她独自站在空台上，正面，不做手势，只换嘴。后墙上是她清楚的硬影子', '纸偶她', 0),
 ('P', '70 微距', '4', '离红纸 10 cm；绕它转 20°', '红纸自己立起来，一拍一折，六折成一只鸟', '鸟', 0),
 ('P', '35', '2.8', '入内 0 cm；随鸟上升 14 cm', '纸云挂在线上一层层降下，红纸鸟穿云而上', '鸟', 0),
 ('X', '50', '2', '玻璃外 30 cm；鸟掠过后右摇 60°', '鸟迎面飞来，穿过玻璃，掠过镜头飞进暗房间。全片只有这一下不讲物理', '鸟', 0),
 ('R', '35', '2', '她右后方 1.1 m，高 1.3 m；跟摇', '鸟绕她的头飞一圈（半径 0.4 m，4 秒）。她的头慢慢跟着转，头发在动', '鸟', 0),
 ('R', '70 微距', '2.8', '离她的手 0.28 m；固定，微移 1 cm', '鸟落在她摊开的掌心，纸被光照透。背景里窗帘缝第一次透出蓝光', '鸟', 0),
 ('R', '85', '2', '她右前方，离脸 0.87 m；推 3 cm', '她低头看鸟，极轻地摇头。全片最近的一次脸', '眼睛', 1),
 ('R', '70 微距', '2.8', '离笔记本 0.3 m；横移 5 cm', '她的手把鸟放到台灯下的笔记本上', '鸟', 0),
 ('X', '24', '2.8', '从入内 6 cm 一路后拉到玻璃外 1.6 m（9.7 秒）', '所有的窗全亮，纸人手里的白纸片换成了小窗灯。镜头退出玻璃，退过她站着的剪影，整个房间被电视照成暖色', '窗 → 她的轮廓', 0),
 ('X', '35', '2.8', '入内 18 cm，朝玻璃拍；微推 1 cm', '前景是那圈空的追光。玻璃外她走近，手掌贴上玻璃，从里面看有半个舞台那么大', '手掌', 0),
 ('R', '70 微距', '2.8', '离电视开关 0.25 m；固定，微移 1 cm', '手指按下开关（2:58.0）。画面缩成一条线，再成一个点。房间只剩台灯和窗帘缝的蓝', '指尖 → 光点', 0),
 ('R', '24', '4', '回到 S02 结束时的机位；固定', '她站在窗前的剪影。3:03 拉开窗帘，仰角 5° 的晨光横穿房间，照到电视的玻璃和书桌，光里有灰尘', '她', 0),
 ('R', '70 微距', '2.8', '离鸟 0.3 m；缓推 3 cm', '晨光里的红纸鸟，拖着长影子。淡出', '鸟', 0),
 ('-', '—', '—', '—', '黑场。歌曲与素材署名；一行求助热线（发布前按平台要求核实）', '—', 0),
]
assert len(SHOTS) == len(CUTS) - 1, (len(SHOTS), len(CUTS))

SECTIONS = [(0, 23.87, '前奏'), (23.87, 29.81, '念白'), (29.81, 47.65, '主歌一（快）'), (47.65, 55.24, '导歌'), (55.24, 71.98, '副歌一'),
            (71.98, 75.77, '间奏'), (75.77, 96.32, '主歌二（快）'), (96.32, 104.98, '导歌二'), (104.98, 121.72, '副歌二'), (121.72, 124.97, '停'),
            (124.97, 143.89, '桥段：静到最强'), (143.89, 160.63, '副歌三（最轻）'), (160.63, 178.0, '副歌四（最强）'), (178.0, 188.0, '再见'), (188.0, DUR, '尾')]
LIGHT = [(0, 1.5, '黑', 'k'), (1.5, 55.24, '电视冷白 ＋ 台灯', 'tv'), (55.24, 64.41, '转暖：6 窗', 'lamp'), (64.41, 75.77, '追光：暗', 'dim'),
         (75.77, 96.32, '电视冷白', 'tv'), (96.32, 104.98, '橙 → 深蓝', 'dusk'), (104.98, 118.47, '暖：20 扇窗', 'lamp'), (118.47, 124.97, '一级级暗', 'dim'),
         (124.97, 143.89, '只有一盏裸灯泡', 'k'), (143.89, 160.63, '近黑；2:28 帘缝透蓝', 'blue'), (160.63, 178.0, '全片最亮最暖', 'lamp'),
         (178.0, 183.0, '台灯＋蓝', 'blue'), (183.0, 190.0, '日出', 'sun'), (190.0, DUR, '黑', 'k')]
STAGE = [(0, 8.17, '开机、纸雪'), (8.17, 29.81, '布景滑入：纸街角'), (29.81, 47.65, '纸带、纸人、红纸'), (47.65, 55.24, '纸偶遮眼'), (55.24, 64.41, '亮 6 扇窗'),
         (64.41, 71.98, '空的追光'), (71.98, 82.7, '黑衣换纸'), (84.42, 96.32, '纸带、后台'), (96.32, 104.98, '纸太阳落下'),
         (104.98, 121.72, '亮到 20 扇窗'), (121.72, 124.97, '撤景'), (124.97, 129.82, '空台'), (129.82, 133.61, '折鸟'), (133.61, 138.46, '降云'), (138.46, 142.0, '飞出'),
         (142.0, 160.63, '空台，暗'), (160.63, 170.37, '所有窗全亮'), (170.37, 178.0, '空的追光'), (178.0, 180.84, '关')]
HER3D = [(8.17, 51.99, '坐在床沿看'), (51.99, 55.24, '手放下'), (60.07, 64.41, '前倾'), (82.25, 84.42, '握'), (96.32, 104.98, '侧脸，眼湿'),
         (118.47, 121.72, '看手'), (143.89, 148.72, '头跟鸟转'), (148.72, 157.38, '接鸟，低头看'), (157.38, 160.63, '放鸟'), (163.0, 175.22, '站到电视前'),
         (175.22, 180.84, '关电视'), (180.84, 190.0, '窗前，拉窗帘')]
PUPPET = [(47.65, 55.24, '遮眼'), (64.41, 71.98, '背影 → 唱'), (109.83, 114.13, '张臂唱'), (124.97, 129.82, '空台独唱')]
SFX = [(0.6, '通电“嗒”'), (1.6, '显像管静电声'), (32.0, '过玻璃的低鸣'), (34.7, '纸带拉出'), (39.0, '纸片翻亮'), (72.0, '纸手滑入'), (80.7, '擦纸两秒'), (84.4, '纸带'),
       (101.7, '纸路灯“嗒”'), (121.7, '撤景的纸声'), (125.0, '灯泡“嗒”'), (129.8, '折纸 ×6'), (138.5, '拍翅'), (141.0, '穿玻璃“叮”'), (150.0, '落在掌心'),
       (172.5, '手掌贴玻璃'), (178.0, '关机“嗒”'), (183.0, '窗帘环'), (185.0, '远处的鸟叫')]


def tc(t):
    m = int(t // 60); s = t - 60 * m
    return f'{m}:{s:04.1f}'


def frame(t):
    return int(round(t * FPS))


def esc(s):
    return str(s).replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')


# ------------------------------------------------------------------ small SVG helpers
def line(x1, y1, x2, y2, cls='ln', extra=''):
    return f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" class="{cls}" {extra}/>'


def rect(x, y, w, h, cls, extra=''):
    return f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" class="{cls}" {extra}/>'


def text(x, y, s, cls='t', anchor='start', extra=''):
    return f'<text x="{x:.1f}" y="{y:.1f}" class="{cls}" text-anchor="{anchor}" {extra}>{esc(s)}</text>'


def poly(pts, cls, extra=''):
    return f'<polygon points="{" ".join(f"{x:.1f},{y:.1f}" for x, y in pts)}" class="{cls}" {extra}/>'


def dim_h(x1, x2, y, label, above=True):
    """a horizontal dimension line with end ticks and a centred label"""
    ty = y - 5 if above else y + 13
    return (line(x1, y, x2, y, 'dim') + line(x1, y - 4, x1, y + 4, 'dim') + line(x2, y - 4, x2, y + 4, 'dim') +
            text((x1 + x2) / 2, ty, label, 'td', 'middle'))


def dim_v(x, y1, y2, label, left=False):
    tx = x - 6 if left else x + 6
    return (line(x, y1, x, y2, 'dim') + line(x - 4, y1, x + 4, y1, 'dim') + line(x - 4, y2, x + 4, y2, 'dim') +
            text(tx, (y1 + y2) / 2 + 4, label, 'td', 'end' if left else 'start'))


def arrow(x1, y1, x2, y2, cls='cam', dashed=False):
    a = math.atan2(y2 - y1, x2 - x1); h = 6
    p = [(x2, y2), (x2 - h * math.cos(a - 0.45), y2 - h * math.sin(a - 0.45)), (x2 - h * math.cos(a + 0.45), y2 - h * math.sin(a + 0.45))]
    return line(x1, y1, x2, y2, cls, 'stroke-dasharray="4 3"' if dashed else '') + poly(p, cls + 'f')


def pill(x, y, label):
    w = 10 + sum(11.0 if ord(ch) > 255 else 6.4 for ch in label)
    return rect(x - w / 2, y - 8, w, 16, 'pill', 'rx="8"') + text(x, y + 3.6, label, 'tp', 'middle')


# ------------------------------------------------------------------ figure 1 and 4a: the room in plan
S, PX, PY = 150.0, 78.0, 58.0
def X(x): return PX + x * S
def Z(z): return PY + (RD - z) * S


def plan(cameras=False):
    W, H = PX * 2 + RW * S, PY + RD * S + 66
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="{"机位图" if cameras else "房间平面图"}">']
    o.append(rect(X(0) - 11, Z(RD) - 11, RW * S + 22, RD * S + 22, 'wall'))
    o.append(rect(X(0), Z(RD), RW * S, RD * S, 'floor'))
    if not cameras:
        # the sun after the curtain opens, and the television's throw
        p = [(X(WIN['x0']), Z(RD)), (X(WIN['x1']), Z(RD)), (X(WIN['x1'] - SHIFT * RD), Z(0)), (X(0), Z(0)), (X(0), Z(RD - WIN['x0'] / SHIFT))]
        o.append(poly(p, 'sunbeam'))
        o.append(poly([(X(GLASS['x']), Z(2.0)), (X(GLASS['x']), Z(2.4)), (X(2.05), Z(3.05)), (X(2.05), Z(1.35))], 'tvbeam'))
        o.append(f'<circle cx="{X(0.13):.1f}" cy="{Z(1.48):.1f}" r="{0.62 * S:.1f}" class="lampglow"/>')
    # window and curtain
    o.append(rect(X(WIN['x0']), Z(RD) - 11, (WIN['x1'] - WIN['x0']) * S, 11, 'floor'))
    o.append(line(X(WIN['x0']), Z(RD) - 7, X(WIN['x1']), Z(RD) - 7, 'ln') + line(X(WIN['x0']), Z(RD) - 3, X(WIN['x1']), Z(RD) - 3, 'ln'))
    cx = np.linspace(WIN['x0'] - 0.1, WIN['x1'] + 0.1, 40)
    o.append('<polyline class="curtain" points="' + ' '.join(f'{X(x):.1f},{Z(RD) + 7 + 3 * math.sin(i * 1.6):.1f}' for i, x in enumerate(cx)) + '"/>')
    # furniture
    o.append(rect(X(BED['x0']), Z(BED['z1']), (BED['x1'] - BED['x0']) * S, (BED['z1'] - BED['z0']) * S, 'furn'))
    o.append(rect(X(2.60), Z(BED['z1']), 0.13 * S, (BED['z1'] - BED['z0']) * S, 'furn2'))
    o.append(rect(X(STAND['x0']), Z(STAND['z1']), (STAND['x1'] - STAND['x0']) * S, (STAND['z1'] - STAND['z0']) * S, 'furn'))
    o.append(rect(X(TV['x0']), Z(TV['z1']), (TV['x1'] - TV['x0']) * S, (TV['z1'] - TV['z0']) * S, 'tvbox'))
    o.append(line(X(GLASS['x']), Z(GLASS['z'] - GLASS['w'] / 2), X(GLASS['x']), Z(GLASS['z'] + GLASS['w'] / 2), 'glass'))
    o.append(rect(X(DESK['x0']), Z(DESK['z1']), (DESK['x1'] - DESK['x0']) * S, (DESK['z1'] - DESK['z0']) * S, 'furn'))
    o.append(rect(X(0.20), Z(1.34), 0.21 * S, 0.30 * S, 'furn2'))                                   # notebook
    o.append(f'<circle cx="{X(0.13):.1f}" cy="{Z(1.48):.1f}" r="7" class="lampdot"/>')
    o.append(rect(X(0.27) - 3, Z(1.18) - 3, 6, 6, 'redf'))                                           # where the bird ends up
    # her
    o.append(f'<circle cx="{X(HER["x"]):.1f}" cy="{Z(HER["z"]):.1f}" r="{0.16 * S:.1f}" class="her"/>')
    o.append(poly([(X(HER['x'] - 0.16), Z(HER['z'] - 0.07)), (X(HER['x'] - 0.16), Z(HER['z'] + 0.07)), (X(HER['x'] - 0.27), Z(HER['z']))], 'herf'))
    if not cameras:
        o.append(text(X(1.365), Z(RD) - 24, '窗 1.65 × 1.15 m，窗台高 0.85 m；遮光窗帘', 't', 'middle'))
        o.append(dim_h(X(0), X(RW), Z(0) + 24, '2.73 m', above=False))
        o.append(dim_v(X(RW) + 32, Z(RD), Z(0), '3.64 m'))
        o.append(dim_h(X(GLASS['x']), X(HER['x'] - 0.12), Z(1.78), '眼到玻璃 1.25 m', above=False))
        o.append(text(X(0.30), Z(2.56), '电视（20 英寸）', 't', 'middle'))
        o.append(text(X(0.30), Z(1.80) + 4, '电视柜', 'tm', 'middle'))
        o.append(text(X(2.30), Z(2.95), '床', 't', 'middle'))
        o.append(text(X(HER['x']), Z(HER['z']) + 4, '她', 't', 'middle'))
        o.append(text(X(0.60), Z(1.50) + 4, '台灯', 'tm'))
        o.append(text(X(0.60), Z(1.18) + 4, '书桌：笔记本、闹钟；鸟最后放在这里', 'tm'))
        o.append(text(X(1.05), Z(0.42), '3:03 之后的阳光', 'tsun', 'middle'))
        o.append(text(X(1.365), Z(0) + 58, '门墙（全景机位在这一侧）', 'tm', 'middle'))
    else:
        cams = [  # (label, x, z, look x, look z, move-to x, z)
            ('02·39', 1.75, 0.30, 1.05, 2.5, 1.70, 0.80), ('22', 1.15, 0.62, 1.15, 2.2, None, None), ('25·35·40', 0.74, 1.24, 0.30, 1.24, None, None),
            ('26', 1.30, 1.90, 1.62, 2.14, None, None), ('38', 0.80, 1.93, 0.55, 2.00, None, None), ('01·18·27·31', 1.02, 2.20, 0.55, 2.20, None, None),
            ('17·33', 1.44, 2.56, 1.62, 2.30, None, None), ('34', 1.05, 2.82, 1.70, 2.24, None, None), ('04·21', 1.62, 3.08, 1.72, 2.36, None, None),
            ('32', 2.45, 3.05, 1.84, 2.30, None, None), ('03', 2.27, 2.47, 0.60, 2.22, 2.27, 2.27), ('12', 2.20, 1.92, 0.60, 2.18, 2.50, 1.80)]
        o.append(arrow(X(0.40), Z(2.20) + 13, X(2.13), Z(2.20) + 13, 'cam', True))
        for lab, x, z, lx, lz, mx, mz in cams:
            d = math.hypot(lx - x, lz - z); k = min(0.42, d * 0.8) / d
            if mx is not None: o.append(arrow(X(x), Z(z), X(mx), Z(mz), 'cam', True))
            o.append(arrow(X(x), Z(z), X(x + (lx - x) * k), Z(z + (lz - z) * k), 'cam'))
            o.append(pill(X(x), Z(z), lab))
        for (x, z, lab) in [(1.02, 2.20, None), (1.36, 3.32, '她站在窗前（39）')]:
            if lab:
                o.append(f'<circle cx="{X(x):.1f}" cy="{Z(z):.1f}" r="{0.14 * S:.1f}" class="her2"/>')
                o.append(text(X(x) + 26, Z(z) + 4, lab, 'tm'))
        o.append(text(X(0.86), Z(1.62), '她站到电视前（36、37）', 'tm'))
        o.append(text(X(HER['x']), Z(HER['z']) + 4, '她', 't', 'middle'))
        o.append(text(X(1.365), Z(0) + 30, '圆角标签是镜号，实线箭头是拍摄方向，虚线是机位的移动', 'tm', 'middle'))
        o.append(text(X(1.365), Z(0) + 48, '穿过她的长虚线是 S36：从玻璃里一直后拉到玻璃外 1.6 米', 'tm', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ figure 2a: elevation toward the window wall (x across, y up)
def elevation():
    px, py = 118.0, 34.0
    def ex(x): return px + x * S
    def ey(y): return py + (RH - y) * S
    W, H = px + RW * S + 124, py + RH * S + 44
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="朝窗墙看的立面图">']
    o.append(rect(ex(0) - 9, ey(RH) - 9, RW * S + 18, RH * S + 18, 'wall')); o.append(rect(ex(0), ey(RH), RW * S, RH * S, 'floor'))
    o.append(rect(ex(WIN['x0']), ey(WIN['y1']), (WIN['x1'] - WIN['x0']) * S, (WIN['y1'] - WIN['y0']) * S, 'win'))
    for i in range(12):
        x = WIN['x0'] - 0.06 + i * (WIN['x1'] - WIN['x0'] + 0.12) / 11
        o.append(line(ex(x), ey(WIN['y1'] + 0.06), ex(x), ey(WIN['y0'] - 0.10), 'curtain'))
    o.append(text(ex(1.365), ey(1.48), '窗帘（拉着）', 'tm', 'middle'))
    o.append(rect(ex(STAND['x0']), ey(STAND['y1']), (STAND['x1'] - STAND['x0']) * S, STAND['y1'] * S, 'furn'))
    o.append(rect(ex(TV['x0']), ey(TV['y1']), (TV['x1'] - TV['x0']) * S, (TV['y1'] - TV['y0']) * S, 'tvbox'))
    o.append(line(ex(GLASS['x']), ey(GLASS['y'] - GLASS['h'] / 2), ex(GLASS['x']), ey(GLASS['y'] + GLASS['h'] / 2), 'glass'))
    o.append(rect(ex(GLASS['x'] - 0.38), ey(GLASS['y'] + 0.17), 0.38 * S, 0.34 * S, 'dash'))
    o.append(rect(ex(BED['x0']), ey(BED['seat']), (BED['x1'] - BED['x0']) * S, BED['seat'] * S, 'furn'))
    o.append(rect(ex(2.60), ey(BED['back']), 0.13 * S, BED['back'] * S, 'furn2'))
    # her, seated on the edge of the bed, facing the television
    hx, hy = 1.76, 1.06
    o.append(f'<circle cx="{ex(hx):.1f}" cy="{ey(hy + 0.02):.1f}" r="{0.105 * S:.1f}" class="her"/>')
    body = [(1.80, 0.94), (1.90, 0.50), (1.62, 0.50), (1.46, 0.47), (1.44, 0.06), (1.33, 0.02), (1.33, 0.0), (1.52, 0.0), (1.55, 0.36), (1.70, 0.40), (1.70, 0.92)]
    o.append(poly([(ex(a), ey(b)) for a, b in body], 'her'))
    o.append(line(ex(hx - 0.10), ey(HER['eye']), ex(GLASS['x']), ey(GLASS['y']), 'sight'))
    o.append(text(ex(1.12), ey(1.02), '视线 1.25 m，向下约 8°', 'tm', 'middle'))
    o.append(dim_v(ex(RW) + 26, ey(RH), ey(0), '层高 2.40 m'))
    o.append(dim_v(ex(0) - 26, ey(GLASS['y']), ey(0), '屏幕中心 0.88', left=True))
    o.append(dim_v(ex(1.00), ey(WIN['y0']), ey(0), '窗台 0.85', left=True))
    o.append(text(ex(0.30), ey(1.16), '电视 52 × 53 cm', 't', 'middle'))
    o.append(text(ex(0.32), ey(0.26), '柜高 0.55', 'tm', 'middle'))
    o.append(text(ex(2.26), ey(0.18), '床沿 0.42', 'tm', 'middle'))
    o.append(text(ex(1.365), ey(0) + 34, '朝窗墙看：她坐在床沿，正对电视', 'tm', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ figure 2b: section along the room for the sun (z across, y up)
def sun_section():
    px, py = 64.0, 34.0
    def sz(z): return px + z * S
    def sy(y): return py + (RH - y) * S
    W, H = px * 2 + RD * S, py + RH * S + 60
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="日出时的光路剖面">']
    o.append(rect(sz(0) - 9, sy(RH) - 9, RD * S + 18, RH * S + 18, 'wall')); o.append(rect(sz(0), sy(RH), RD * S, RH * S, 'floor'))
    o.append(rect(sz(RD), sy(WIN['y1']), 9, (WIN['y1'] - WIN['y0']) * S, 'win'))
    lo0, hi0 = WIN['y0'], WIN['y1']
    o.append(poly([(sz(RD), sy(hi0)), (sz(RD), sy(lo0)), (sz(0), sy(lo0 - SLOPE * RD)), (sz(0), sy(hi0 - SLOPE * RD))], 'sunbeam'))
    for y0 in (lo0, hi0):
        o.append(line(sz(RD) + 40, sy(y0 + SLOPE * 40 / S), sz(0), sy(y0 - SLOPE * RD), 'sunray'))
    o.append(rect(sz(STAND['z0']), sy(STAND['y1']), (STAND['z1'] - STAND['z0']) * S, STAND['y1'] * S, 'furn'))
    o.append(rect(sz(TV['z0']), sy(TV['y1']), (TV['z1'] - TV['z0']) * S, (TV['y1'] - TV['y0']) * S, 'tvbox'))
    o.append(rect(sz(DESK['z0']), sy(DESK['top']), (DESK['z1'] - DESK['z0']) * S, 0.035 * S, 'furn'))
    o.append(line(sz(DESK['z0'] + 0.05), sy(DESK['top']), sz(DESK['z0'] + 0.05), sy(0), 'ln') + line(sz(DESK['z1'] - 0.05), sy(DESK['top']), sz(DESK['z1'] - 0.05), sy(0), 'ln'))
    o.append(f'<circle cx="{sz(1.48):.1f}" cy="{sy(1.05):.1f}" r="7" class="lampdot"/>' + line(sz(1.50), sy(1.03), sz(1.56), sy(0.775), 'ln'))
    o.append(rect(sz(1.18) - 4, sy(0.775) - 8, 9, 8, 'redf'))
    yd = lo0 - SLOPE * (RD - 1.18); yt = lo0 - SLOPE * (RD - 2.2)
    o.append(text(sz(2.20), sy(1.13), '电视', 't', 'middle'))
    o.append(text(sz(1.02), sy(1.14), '台灯', 'tm', 'end'))
    o.append(text(sz(0.90), sy(0.48), '桌面 0.74，', 'tm', 'end'))
    o.append(text(sz(0.90), sy(0.34), f'光的下沿 {yd:.2f}', 'tm', 'end'))
    o.append(text(sz(2.56), sy(0.50), f'这里光的下沿 {yt:.2f} m', 'tm'))
    o.append(text(sz(RD) - 10, sy(2.16), '太阳仰角 5°，偏向电视一侧 12°', 'tsun', 'end'))
    o.append(text(sz(0) + 10, sy(1.84), '光打到门墙 0.52–1.67 m', 'tsun'))
    o.append(dim_h(sz(0), sz(RD), sy(0) + 16, '3.64 m', above=False))
    o.append(text(sz(RD / 2), sy(0) + 50, '沿房间纵向剖开：拉开窗帘后，光先照到电视的顶和侧面，再擦过桌上的鸟', 'tm', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ figure 3: section through the television (w = depth behind the glass, v up), centimetres
def theatre_section():
    c, px, py = 11.0, 176.0, 44.0
    def tw(w): return px + w * c
    def tv(v): return py + (ST_H - v) * c
    W, H = px + ST_D * c + 262, py + ST_H * c + 86
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="电视里的纸剧场剖面">']
    o.append(rect(tw(-3), tv(ST_H) - 14, (ST_D + 5.5) * c, ST_H * c + 28, 'tvbox'))
    o.append(rect(tw(0), tv(ST_H), ST_D * c, ST_H * c, 'floor'))
    o.append(rect(tw(-3), tv(31.8), 3 * c, 29.8 * c, 'floor'))
    o.append(poly([(tw(-1), tv(31)), (tw(-1), tv(3)), (tw(-13.5), tv(-1.5)), (tw(-13.5), tv(35.5))], 'tvbeam'))
    o.append(f'<path d="M {tw(0):.1f} {tv(31.8):.1f} Q {tw(-1.6):.1f} {tv(16.9):.1f} {tw(0):.1f} {tv(2):.1f}" class="glass" fill="none"/>')
    o.append(line(tw(0), tv(0), tw(ST_D), tv(0), 'ln'))
    for (w, name, h) in LAYERS:
        if h: o.append(line(tw(w), tv(0), tw(w), tv(h), 'layer'))
        else: o.append(line(tw(w), tv(31.8), tw(w), tv(ST_H), 'layer') + line(tw(w), tv(0), tw(w), tv(2), 'layer'))
        o.append(line(tw(w), tv(0), tw(w), tv(0) + 6, 'dim')); o.append(text(tw(w), tv(0) + 19, f'{w:g}', 'td', 'middle'))
    o.append(text(tw(ST_D / 2), tv(0) + 38, '离玻璃的距离（厘米）', 'tm', 'middle'))
    lx = tw(ST_D) + 40
    o.append(text(lx, tv(31), '从前到后的 8 层', 't'))
    for i, (w, name, h) in enumerate(LAYERS):
        o.append(text(lx, tv(31) + 24 + i * 22, f'{w:g}', 'td')); o.append(text(lx + 30, tv(31) + 24 + i * 22, name, 'tm'))
    o.append(line(tw(3), tv(0), tw(3), tv(11), 'herl')); o.append(f'<circle cx="{tw(3):.1f}" cy="{tv(10.2):.1f}" r="5" class="her"/>')
    o.append(rect(tw(14) - 5, tv(0) - 4, 10, 4, 'redf'))
    for (w, v) in [(6, 33), (2, 32.6), (15.4, 19), (36, 16), (37.2, 25)]:
        o.append(f'<circle cx="{tw(w):.1f}" cy="{tv(v):.1f}" r="4.5" class="lampdot"/>')
    for w in (10, 22, 31):
        o.append(f'<circle cx="{tw(w):.1f}" cy="{tv(33.2):.1f}" r="2.6" class="bar"/>')
    o.append(text(tw(19), tv(ST_H) - 22, '顶上：灯条、追光、三道吊杆（纸带、纸云、窗灯）', 'tm', 'middle'))
    o.append(text(tw(-7.6), tv(19), '房间一侧', 't', 'middle'))
    o.append(text(tw(-7.6), tv(17), '光从这里', 'tm', 'middle')); o.append(text(tw(-7.6), tv(15.4), '漏出去', 'tm', 'middle'))
    o.append(text(tw(-7.6), tv(12.6), '台口高', 'tm', 'middle')); o.append(text(tw(-7.6), tv(11), '29.8 cm', 'td', 'middle'))
    o.append(text(tw(3.2), tv(12.4), '纸偶她', 'tm', 'middle')); o.append(text(tw(3.2), tv(13.9), '11 cm', 'td', 'middle'))
    o.append(text(tw(14), tv(1.6), '红纸', 'tred', 'middle'))
    o.append(text(tw(16.6), tv(18.6), '路灯', 'tm')); o.append(text(tw(35.2), tv(14), '背光', 'tm', 'end')); o.append(text(tw(36.4), tv(27), '裸灯泡', 'tm', 'end'))
    o.append(text(tw(19), tv(0) + 62, '从侧面剖开电视。竖线是一层层立着的纸片', 'tm', 'middle'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ figure 4b: the stage in plan with the cameras that work inside it
def theatre_plan():
    c, px, py = 9.0, 46.0, 30.0
    def tu(u): return px + (u + ST_W / 2) * c
    def tw(w): return py + (ST_D - w) * c
    W, H = px * 2 + ST_W * c, py + (ST_D + 15) * c + 30
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="纸剧场里的机位">']
    o.append(rect(tu(-ST_W / 2) - 8, tw(ST_D) - 8, ST_W * c + 16, ST_D * c + 8, 'tvbox'))
    o.append(rect(tu(-ST_W / 2), tw(ST_D), ST_W * c, ST_D * c, 'floor'))
    o.append(line(tu(-19.8), tw(0), tu(19.8), tw(0), 'glass'))
    for (w, u0, u1) in [(4, -10, 22), (9, -8, 23), (20, -23, -6), (20, 8, 23), (27, -23, 23), (34, -23, 23)]:
        o.append(line(tu(u0), tw(w), tu(u1), tw(w), 'layer'))
    for w, lab in [(4, '4'), (9, '9'), (14, '14'), (20, '20'), (27, '27'), (34, '34')]:
        o.append(text(tu(-ST_W / 2) - 12, tw(w) + 4, lab, 'td', 'end'))
    o.append(f'<circle cx="{tu(6):.1f}" cy="{tw(14):.1f}" r="{4 * c:.1f}" class="spot"/>')
    o.append(rect(tu(6) - 5, tw(14) - 5, 10, 10, 'redf'))
    o.append(f'<circle cx="{tu(9.5):.1f}" cy="{tw(15.5):.1f}" r="4" class="lampdot"/>')
    o.append(f'<circle cx="{tu(-12):.1f}" cy="{tw(3):.1f}" r="6" class="her"/>')
    o.append(text(tu(-13.4), tw(3) + 4, '纸偶她', 'tm', 'end'))
    o.append(text(tu(16.6), tw(14) + 4, '红纸、路灯', 'tm'))
    cams = [('09·10', -6, 16, -11.4, 4.6, None), ('14', -18, 11, -13.2, 4.4, None), ('11', 15, 6.5, 15, 18.6, None), ('20', 5, 30, 1.5, 3, None),
            ('37', 0, 21.5, 0, 2, None), ('15', 9, 5.5, 13.2, 12.6, None)]
    for lab, u, w, lu, lw, _ in cams:
        o.append(arrow(tu(u), tw(w), tu(lu), tw(lw), 'cam')); o.append(pill(tu(u), tw(w), lab))
    o.append(arrow(tu(-19), tw(-12.5), tu(-19), tw(1.5), 'cam', True)); o.append(pill(tu(-19), tw(-8.6), '05 推入'))
    o.append(arrow(tu(19), tw(5), tu(19), tw(-12.5), 'cam', True)); o.append(pill(tu(19), tw(-8.6), '36 后拉'))
    o.append(arrow(tu(-4), tw(26), tu(-14), tw(26), 'cam', True)); o.append(pill(tu(-1), tw(26), '23'))
    o.append(pill(tu(0.2), tw(14), '16 俯'))
    o.append(text(tu(0), tw(-2.4), '贴着玻璃内侧：06 07 08 13 19 24 30', 'tm', 'middle'))
    o.append(text(tu(0), tw(-4.6), '玻璃外 6–45 cm，正对屏幕：01 18 27 28 31', 'tm', 'middle'))
    o.append(text(tu(0), tw(-9.0), '房间一侧（她在这个方向 1.25 m 处）', 'tm', 'middle'))
    o.append(text(tu(ST_W / 2) + 4, tw(0) + 4, '玻璃', 'ttv'))
    o.append(text(tu(0), tw(36), '后墙', 'tm', 'middle'))
    o.append(dim_h(tu(-ST_W / 2), tu(ST_W / 2), tw(ST_D) - 16, '舞台宽 46 cm'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ figure 5: the one clock
def loudness():
    with wave.open(os.path.join(ROOT, 'audio', 'song.wav')) as w:
        n, sr, ch, sw = w.getnframes(), w.getframerate(), w.getnchannels(), w.getsampwidth()
        a = np.frombuffer(w.readframes(n), dtype={2: np.int16, 4: np.int32}[sw]).astype(np.float64) / float(2 ** (8 * sw - 1))
    a = a.reshape(-1, ch).mean(axis=1); hop = sr // 2
    r = [math.sqrt(float(np.mean(a[i:i + hop] ** 2)) + 1e-12) for i in range(0, len(a) - hop, hop)]
    return [20 * math.log10(v) for v in r]


def timeline(asr):
    K, L, RH_, TOP = 8.5, 96.0, 30.0, 26.0
    rows = ['歌的段落', '响度（实测）', '镜头', '房间里的光', '电视里的舞台', '房间里的她', '纸偶她', '音效', '字幕 45 句']
    hs = [RH_, RH_, RH_, RH_, RH_, RH_, RH_, 46.0, 18.0]
    ys = [TOP + sum(hs[:i]) + 4 * i for i in range(len(rows))]
    W, H = L + DUR * K + 14, ys[-1] + hs[-1] + 12
    def tx(t): return L + t * K
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" width="{W:.0f}" height="{H:.0f}" role="img" aria-label="全片总时间线">']
    for t in range(0, int(DUR) + 1, 10):
        o.append(line(tx(t), TOP - 6, tx(t), H - 8, 'grid')); o.append(text(tx(t) + 3, 13, f'{t // 60}:{t % 60:02d}', 'td'))
    for i, r in enumerate(rows):
        o.append(text(L - 8, ys[i] + hs[i] / 2 + 4, r, 'tm', 'end'))

    def blocks(i, items, cls):
        for it in items:
            t0, t1, lab = it[0], it[1], it[2]
            c2 = cls if len(it) < 4 else 'L' + it[3]
            w = (t1 - t0) * K - 1
            o.append(f'<g><title>{esc(lab)}　{tc(t0)}–{tc(t1)}</title>' + rect(tx(t0), ys[i], w, hs[i], 'blk ' + c2, 'rx="3"'))
            if 10.6 * len(lab) + 8 <= w: o.append(text(tx(t0) + 5, ys[i] + hs[i] / 2 + 4, lab, 'tb'))
            o.append('</g>')
    blocks(0, SECTIONS, 'Bsec')
    db = loudness(); lo, hi = -42.0, -9.0
    pts = [(tx(0), ys[1] + hs[1])] + [(tx(j * 0.5 + 0.25), ys[1] + hs[1] - max(0.0, min(1.0, (v - lo) / (hi - lo))) * hs[1]) for j, v in enumerate(db)] + [(tx(len(db) * 0.5), ys[1] + hs[1])]
    o.append(poly(pts, 'loud'))
    for k, sh in enumerate(SHOTS):
        t0, t1 = CUTS[k], CUTS[k + 1]; w = (t1 - t0) * K - 1
        o.append(f'<g><title>S{k + 1:02d}　{tc(t0)}–{tc(t1)}　{esc(sh[4])}</title>' + rect(tx(t0), ys[2], w, hs[2], 'blk W' + sh[0], 'rx="3"') +
                 text(tx(t0) + w / 2, ys[2] + hs[2] / 2 + 4, f'{k + 1:02d}', 'tb', 'middle') + '</g>')
    blocks(3, LIGHT, ''); blocks(4, STAGE, 'Bstage'); blocks(5, HER3D, 'Bher'); blocks(6, PUPPET, 'Bpup')
    lanes = [-1e9, -1e9, -1e9]
    for t, lab in SFX:
        x = tx(t); k = next((j for j in range(3) if lanes[j] + 6 <= x), 2); lanes[k] = x + 6 + 10.6 * len(lab)
        y = ys[7] + 11 + k * 14
        o.append(line(x, ys[7], x, ys[7] + hs[7], 'sfx')); o.append(text(x + 4, y, lab, 'tb'))
    for s in asr:
        o.append(rect(tx(s['t0']), ys[8] + 3, max(2.0, (s['t1'] - s['t0']) * K - 1), hs[8] - 6, 'cap', 'rx="2"'))
    o.append('</svg>')
    return ''.join(o)


# ------------------------------------------------------------------ the shot table
def shot_table():
    where = {'R': '房间', 'P': '电视里', 'X': '穿过玻璃', '-': '—'}
    o = ['<table class="shots"><thead><tr><th>镜号</th><th>时间</th><th>帧</th><th>在哪</th><th>镜头</th><th>机位与运动</th><th>画面里发生什么</th><th>焦点</th></tr></thead><tbody>']
    for k, sh in enumerate(SHOTS):
        t0, t1 = CUTS[k], CUTS[k + 1]
        lens = '—' if sh[1] == '—' else (sh[1].replace(' 微距', ' mm 微距') if '微距' in sh[1] else sh[1] + ' mm') + f'<br>f/{sh[2]}'
        face = ' <span class="chip">脸</span>' if sh[6] else ''
        o.append(f'<tr class="w{sh[0] if sh[0] != "-" else "N"}"><td class="num">S{k + 1:02d}</td><td class="num">{tc(t0)}<br><span class="sub">{t1 - t0:.1f} 秒</span></td>'
                 f'<td class="num">{frame(t0)}<br><span class="sub">{frame(t1) - frame(t0)} 帧</span></td><td><span class="tag t{sh[0] if sh[0] != "-" else "N"}">{where[sh[0]]}</span>{face}</td>'
                 f'<td class="num nowrap">{lens}</td><td>{esc(sh[3])}</td><td class="act">{esc(sh[4])}</td><td>{esc(sh[5])}</td></tr>')
    o.append('</tbody></table>')
    return ''.join(o)


def main():
    asr = json.load(open(os.path.join(ROOT, 'work', 'asr.json'), encoding='utf-8'))
    beats = json.load(open(os.path.join(ROOT, 'work', 'beats.json')))
    film = dict(fps=FPS, duration=DUR, frames=frame(DUR), bpm=round(beats['bpm'], 2),
                shots=[dict(id=f'S{k + 1:02d}', t0=CUTS[k], t1=CUTS[k + 1], f0=frame(CUTS[k]), f1=frame(CUTS[k + 1]), where=s[0], lens=s[1], fstop=s[2],
                            camera=s[3], action=s[4], focus=s[5], face=bool(s[6])) for k, s in enumerate(SHOTS)],
                light=[dict(t0=a, t1=b, state=c, key=d) for a, b, c, d in LIGHT], stage=[dict(t0=a, t1=b, state=c) for a, b, c in STAGE],
                her=[dict(t0=a, t1=b, state=c) for a, b, c in HER3D], puppet=[dict(t0=a, t1=b, state=c) for a, b, c in PUPPET],
                sfx=[dict(t=a, name=b) for a, b in SFX],
                set=dict(room=[RW, RD, RH], window=WIN, tv=TV, glass=GLASS, bed=BED, desk=DESK, her=HER, sun=dict(elevation=SUN_EL, azimuth=SUN_AZ),
                         stage_cm=[ST_W, ST_H, ST_D], layers_cm=[dict(depth=a, name=b, height=c) for a, b, c in LAYERS]))
    json.dump(film, open(os.path.join(ROOT, 'docs', 'film_v1.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

    inside = sum(1 for k, s in enumerate(SHOTS) if s[0] in 'PX' or (k + 1) in (1, 18, 27, 38))
    n = len(SHOTS)
    tpl = open(os.path.join(ROOT, 'tools', 'design_sheets_template.html'), encoding='utf-8').read()
    rep = {'{{PLAN}}': plan(False), '{{ELEV}}': elevation(), '{{SUN}}': sun_section(), '{{THEATRE}}': theatre_section(), '{{CAMPLAN}}': plan(True),
           '{{STAGEPLAN}}': theatre_plan(), '{{TIMELINE}}': timeline(asr), '{{SHOTS}}': shot_table(), '{{NSHOTS}}': str(n), '{{NFRAMES}}': f'{frame(DUR):,}',
           '{{NINSIDE}}': str(inside), '{{AVG}}': f'{DUR / n:.1f}', '{{BPM}}': f'{beats["bpm"]:.0f}', '{{NFACE}}': str(sum(s[6] for s in SHOTS))}
    for k, v in rep.items():
        tpl = tpl.replace(k, v)
    assert '{{' not in tpl, tpl[tpl.index('{{'):tpl.index('{{') + 40]
    out = os.path.join(ROOT, 'docs', '设计图纸_v1.html')
    open(out, 'w', encoding='utf-8').write(tpl)
    print('wrote', out, len(tpl), 'bytes;', n, 'shots;', inside, 'at or inside the glass; faces', rep['{{NFACE}}'])
    for k in range(n):
        if CUTS[k + 1] - CUTS[k] > 7.0 and '固定' in SHOTS[k][3] and '微' not in SHOTS[k][3]:
            print('  long locked shot: S%02d %.1f s' % (k + 1, CUTS[k + 1] - CUTS[k]))


if __name__ == '__main__':
    main()
