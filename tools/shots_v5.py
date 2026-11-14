# -*- coding: utf-8 -*-
"""Shot list v5 = v4 without the flights and without cameras above, beside or behind the scenery.
Review of the v5 slice (2026-11-13): the flight along the house looked wrong, and by leaving the house side it showed what was
never built or lit (roof tops, the sides of the clouds, the back of the stage). So:
  - each flight becomes a run of soft cuts on the beat; in the first chorus one cut every two beats, each to the window that comes on
  - every camera stays in front of the scenery, below the eaves, looking up-stage
The one shot from behind the flats (bar 24) is deliberate and needs its own modelling of the backs before it is shot."""
from shots_v4 import SHOTS as V4

REPLACE = {   # start of a v4 shot -> the shots that take its place: (start, type, lens, f, camera, event, focus, join)
 (27, 0): [
  ((27, 0), 'T', '50', '2.8', '离窗 1.4 m，在窗的左下方；推 8 cm，窗纸亮透时落定', '二楼的小窗亮了，窗里是一本摊开的书的剪影', '窗纸', '软切'),
  ((27, 2), 'T', '50', '2.8', '离窗 1.5 m，在阳台左下方；推 8 cm', '隔着阳台栏杆，旁边的窗亮了：一把收起的伞', '窗纸', '软切'),
  ((28, 0), 'T', '60', '2.8', '离窗 1.6 m；推 7 cm', '三楼的小窗亮了：一盏台灯', '窗纸', '软切'),
  ((28, 2), 'T', '50', '2.8', '离窗 1.6 m，在阳台正下方略仰；推 8 cm', '最后一扇亮了：阳台栏杆后面是一只背包', '窗纸', '软切'),
 ],
 (29, 0): [((29, 0), 'T', '28', '4', '台口线上，高 1 m，略仰；缓推 0.28 m，同时升 0.1 m，落在小节线上', '整条街的正面：两栋楼、路灯、远处的楼，六扇窗都亮着', '亮窗', '软切')],
 (50, 0): [
  ((50, 0), 'T', '50', '2.8', '离远楼 1.6 m，正面；缓推 0.1 m', '远楼的一排窗从左到右亮过去，一拍一扇', '亮起的窗', '软切'),
  ((51, 0), 'T', '35', '2.8', '同一方向退到 2.4 m；缓推 0.15 m', '上面一层接着亮，亮到 20 扇', '亮起的窗', '软切'),
 ],
 (52, 0): [((52, 0), 'T', '24', '4', '台口线上，正面全景；缓推 0.3 m，同时升 0.1 m', '所有亮着的窗一起在画面里', '窗', '软切')],
 (67, 0): [
  ((67, 0), 'T', '50', '2.8', '离暗窗 1.5 m，正面；跟着鸟横移 0.3 m', '鸟贴着一排暗窗飞过，飞过的窗一扇扇微微亮一下', '鸟', '软切'),
  ((68, 0), 'T', '70', '2.8', '离她的手 0.9 m；固定，带 3 cm 微推', '一只摊开的手等在画面下沿，鸟朝它降下来', '手', '软切'),
 ],
 (76, 0): [
  ((76, 0), 'T', '35', '2.8', '台内，离台口那圈灯泡 1.5 m，略仰；缓推 0.2 m', '鸟向上飞，穿过台口那圈亮着的灯泡', '鸟', '软切'),
  ((77, 0), 'T', '24', '4', '台口外 2.5 m，正面；缓拉 0.3 m', '反打：鸟从台口飞出来，身后是整座亮着的剧场', '鸟 → 整座舞台', '软切'),
 ],
}
EDIT = {      # camera and event rewritten so that the camera stays on the house side
 (23, 0): dict(cam='台口外 1.2 m，略仰；缓升 0.1 m', event='纸雨滴一串串顺着线落下来，线从画面上沿垂进来'),
 (43, 0): dict(cam='台口线上，正面略仰；缓推 0.3 m，焦点从近楼移到远楼', event='“在远处”：焦点越过近处的楼，落到远楼上'),
 (64, 0): dict(cam='台口线上，正面；跟着影子横移 0.4 m', event='鸟的影子掠过两栋楼的正面，从一扇暗窗滑到下一扇'),
}

SHOTS = []
for sh in V4:
    k = sh[0]
    if not isinstance(k, float) and k in REPLACE: SHOTS += REPLACE[k]; continue
    row = list(sh); e = None if isinstance(k, float) else EDIT.get(k)
    if e: row[4] = e['cam']; row[5] = e['event']
    SHOTS.append(tuple(row))
head = [s for s in SHOTS if isinstance(s[0], float) and s[0] < 1.0]; tail = sorted([s for s in SHOTS if isinstance(s[0], float) and s[0] >= 1.0], key=lambda s: s[0])
mid = sorted([s for s in SHOTS if not isinstance(s[0], float)], key=lambda s: s[0][0] * 4 + s[0][1])
SHOTS = head + mid + tail
assert len({s[0] for s in SHOTS}) == len(SHOTS), 'two shots start on the same beat'

_poses = ['手到胸前', '腰前相握', '垂手侧脸', '托鸟', '放飞', '垂手', '低头闭眼']; _short = ['胸前', '相握', '侧脸', '托鸟', '放飞', '垂手', '闭眼']
_li = [i for i, s in enumerate(SHOTS) if s[1] == 'L']
LIN_POSE = {i: _poses[n] for n, i in enumerate(_li)}; LIN_ROW = [(i, _short[n]) for n, i in enumerate(_li)]
CROWD_ROW = [(i, '') for i, s in enumerate(SHOTS) if s[1] == 'A']
FLIGHTS = []
