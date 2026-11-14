# -*- coding: utf-8 -*-
"""Shot list v4 = v3 cut about a tenth faster and with compound, beat-timed camera moves.
The fast sections (both verses, the pre-chorus, the second half of the bridge, the last chorus tail) go from three shots a
phrase to four, one a bar. Each chorus gets one flight: a single camera path with a way-point on the beat.
Built from v3 by a list of edits so that what changed can be read off this file."""
from shots_v3 import SHOTS as V3

MOVE = {      # start moves to another beat
    (15, 2): (16, 0), (19, 2): (20, 0), (20, 2): (21, 0), (23, 2): (24, 0), (24, 2): (25, 0), (36, 2): (36, 0), (37, 2): (38, 0), (40, 2): (40, 0), (41, 2): (42, 0),
    (63, 2): (63, 0), (64, 2): (65, 0), (68, 2): (69, 0), (80, 2): (80, 0), (81, 2): (82, 0),
}
DROP = {(27, 2), (28, 2), (50, 2), (51, 2), (67, 0), (76, 2), (77, 2)}
EDIT = {      # camera (and sometimes lens, event) rewritten
    (26, 0): dict(cam='离她 2.35 m → 2.1 m，同时绕她走 14° 的弧；每拍一次小前冲，最后把焦点甩到她身后刚亮的窗上', event='lin 站在追光里，胸上，一只手停在胸前。她唱“活着”，身后的窗一声亮一扇'),
    (49, 0): dict(cam='离她 2.3 m，腰上；绕她走 12° 的弧，同时缓推 0.15 m，每拍一次小前冲'),
    (75, 0): dict(cam='离她 1.8 m → 2.2 m 后拉，同时上升 0.2 m；鸟起飞的那一拍镜头跟着上摇'),
    (38, 0): dict(event='红纸被换成一张干净的灰纸，正好 2 秒（1:21.2–1:23.2）'),
    (4, 0): dict(cam='离后墙 2.45 m，高 1.1 m；向前推 0.3 m，同时缓降 0.1 m，乐队进来的那一拍落定'),
    (8, 0): dict(cam='从玻璃外 1.0 m 直推到 0.30 m，同时转正 8°，屏幕占满画面时落在拍点上'),
    (9, 2): dict(cam='台口外 2.2 m；缓推 0.3 m，同时绕台心转 10°，每片楼滑到位的那一拍镜头顿一下'),
    (14, 0): dict(cam='台内贴地，高 1.1 m；横移 0.4 m，同时摇向纸人，它们到位的那一拍落定'),
    (18, 0): dict(cam='台口外 1.7 m，正面；缓推 0.3 m，每翻亮一张白纸片镜头前冲一下'),
    (22, 0): dict(cam='台口外 0.8 m；下降 0.5 m，同时跟着雨滴下摇'),
    (30, 0): dict(cam='纸人身后 1.5 m，高 1.3 m；横移 0.4 m，同时推 0.2 m，从两个头之间穿过去'),
    (43, 0): dict(cam='台内，高 1.2 m；上升 1.2 m 越过楼顶，同时前推 0.4 m'),
    (45, 0): dict(cam='台口外 2.4 m；缓拉 0.3 m，同时跟着纸太阳下摇'),
    (60, 2): dict(cam='离红纸 0.5 m；绕它转 30°，每折一下镜头顿一拍'),
    (62, 0): dict(cam='台内；随鸟上升 1.2 m，同时绕它转 20°'),
    (79, 0): dict(cam='从玻璃外 0.3 m 后拉到 1.2 m，同时下降 0.15 m，落在小节线上'),
}
NEW = [       # (start, type, lens, f, camera, event, focus, join)
 ((15, 0), 'A', '70 微距', '2.8', '贴地，离槽 0.4 m；跟着纸脚横移 0.3 m，同时下降 5 cm', '纸人的卡纸底座沿地板的槽滑过去，槽边磨出了毛边', '底座', '软切'),
 ((19, 0), 'A', '85', '2.8', '离纸手 0.9 m；每拍向右甩 0.3 m 到下一只手', '白纸片在一只只纸手里按拍子翻亮，一拍一张', '白纸片', '软切'),
 ((23, 0), 'T', '24', '4', '贴地仰拍，离吊杆 3.5 m；绕竖轴转 20°，同时缓升 0.1 m', '从下往上看：一串串纸雨滴顺着线落下来，后面是吊杆和木框', '雨滴', '软切'),
 ((27, 0), 'T', '35', '2.8', '沿楼面一条飞行路线：每两拍到一扇窗前，贴着阳台栏杆绕过去，最后翻上屋檐；带 5–7° 的倾斜', '一个镜头飞过四扇窗，镜头到哪扇，哪扇正好亮起来', '每一扇窗', '软切'),
 ((29, 0), 'T', '24', '4', '舞台上方吊杆下，俯拍；下降 0.55 m，同时绕街角转 16°', '从高处看整条街：两栋楼、路灯、六扇亮着的窗', '亮窗', '软切'),
 ((37, 0), 'T', '85 微距', '2.8', '离纸角 0.3 m；跟着纸角抬起上摇，同时推 3 cm', '黑纸手捏住红纸的一角，把它掀起来', '纸角', '软切'),
 ((41, 0), 'A', '50', '2.8', '离纸片 0.8 m；随纸片弹起快速上摇再落定', '两张对话纸片同时弹出来，撞在一起', '对话纸片', '纸带划过'),
 ((50, 0), 'T', '35', '2.8', '贴着远楼一条飞行路线：每拍掠过两扇窗，穿过路灯和电线之间，最后拉高', '窗跟着镜头一路亮过去，亮到 20 扇', '亮起的窗', '软切'),
 ((52, 0), 'T', '24', '4', '从街心贴地快速上升 2 m，同时后仰', '所有亮着的窗在镜头升起时一起进到画面里', '窗', '软切'),
 ((64, 0), 'T', '35', '4', '屋顶正上方俯拍；跟着影子横移 1.2 m，同时下降 0.3 m', '鸟的影子掠过两栋楼的瓦顶', '影子', '软切'),
 ((67, 0), 'T', '35', '2.8', '跟着鸟一条飞行路线：贴着暗窗滑过，绕过路灯，降到她的手边', '鸟飞过的窗一扇扇微微亮一下，最后飞向一只摊开的手', '鸟', '软切'),
 ((76, 0), 'T', '24', '2.8', '跟着鸟一条飞行路线：向上穿过台口的那圈灯泡，翻到台口外，回头，一路后拉到台口外 2.5 m', '鸟带着镜头飞出舞台，回头看见整座亮着的剧场', '鸟 → 整座舞台', '软切'),
 ((78, 0), 'T', '35', '4', '台口外 2.5 m；缓推 0.2 m，每拍一次小前冲', '所有的窗、动物头手里的小窗灯、台口的灯泡一起亮着', '窗', '软切'),
 ((81, 0), 'A', '28', '4', '纸人身后 1.5 m；横移 0.4 m，同时缓升 0.2 m', '从背后看这一排动物头，它们手里的小窗灯朝着她', '小窗灯', '软切'),
]


def _key(s): return (s, 0.0) if isinstance(s, float) else (0.0 if False else 1e-9 + s[0] * 4 + s[1], 0.0)


SHOTS = []
for sh in V3:
    k = sh[0]
    if not isinstance(k, float):
        if k in DROP: continue
        if k in MOVE: k = MOVE[k]
    row = [k] + list(sh[1:])
    e = EDIT.get(k)
    if e:
        if 'cam' in e: row[4] = e['cam']
        if 'event' in e: row[5] = e['event']
        if 'lens' in e: row[2] = e['lens']
    SHOTS.append(tuple(row))
SHOTS += NEW
# order: the opening shot at 0 s first, then by bar and beat, then the two closing shots given in seconds
head = [s for s in SHOTS if isinstance(s[0], float) and s[0] < 1.0]; tail = sorted([s for s in SHOTS if isinstance(s[0], float) and s[0] >= 1.0], key=lambda s: s[0])
mid = sorted([s for s in SHOTS if not isinstance(s[0], float)], key=lambda s: s[0][0] * 4 + s[0][1])
SHOTS = head + mid + tail
assert len({s[0] for s in SHOTS}) == len(SHOTS), 'two shots start on the same beat'

_poses = ['手到胸前', '腰前相握', '垂手侧脸', '托鸟', '放飞', '垂手', '低头闭眼']; _short = ['胸前', '相握', '侧脸', '托鸟', '放飞', '垂手', '闭眼']
_li = [i for i, s in enumerate(SHOTS) if s[1] == 'L']
LIN_POSE = {i: _poses[n] for n, i in enumerate(_li)}; LIN_ROW = [(i, _short[n]) for n, i in enumerate(_li)]
CROWD_ROW = [(i, '') for i, s in enumerate(SHOTS) if s[1] == 'A']
FLIGHTS = [i for i, s in enumerate(SHOTS) if '飞行路线' in s[4]]
