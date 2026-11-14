# -*- coding: utf-8 -*-
"""What every shot of film_v5.json needs before it can be rendered: which scene and state, which kind of camera position
(the kinds are the ones Sets.CamCheck verifies), and which props or animation do not exist yet.
    python -P tools/prep_table.py  ->  docs/全片镜头需求表_v1.md  and a summary on stdout"""
import json, os, re, bisect, collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
d = json.load(open(os.path.join(ROOT, 'docs', 'film_v5.json'), encoding='utf-8'))
bars = sorted((int(k), v) for k, v in d['bars'].items()); bt = [v for _, v in bars]; b0 = bars[0][0]


def bar_of(t): return b0 + max(0, bisect.bisect_right(bt, t + 0.02) - 1)


def tc(t): return '%d:%04.1f' % (int(t // 60), t % 60)


def state(sh, bar):
    if sh['type'] in 'RX': return '日出' if sh['t0'] > 180 else '夜'
    if 9 <= bar <= 11: return '搭景中'
    if bar == 25 or 31 <= bar <= 33: return '只剩追光'
    if 45 <= bar <= 47: return '黄昏'
    if 57 <= bar <= 60: return '空台'
    if 62 <= bar <= 63: return '云降下'
    if bar >= 83: return '熄灯'
    return '夜街二' if bar >= 49 else '夜街'


CAM = [(r'俯拍', '红纸俯拍 paper_top'), (r'离槽|槽', '滑槽微距 groove_macro'), (r'离红纸|离纸角|离掌心|离纸片', '红纸贴地 paper_low'), (r'离路灯', '路灯贴地 lamp_low'),
       (r'离她|腰上|胸上', 'lin 近景 lin_left / lin_right / lin_waist'), (r'离窗|离暗窗|离远楼|同一方向退', '窗 window_2…5 / 巷子 alley_far'), (r'纸人身后', '纸人身后 crowd_back'), (r'离纸人|离野猪|离乌鸦|离纸手', '纸人近景（机位待定）'),
       (r'侧幕', '侧幕缝 wing_gap'), (r'仰拍台口|灯泡', '仰拍台口 bulbs_up / look_out'), (r'最深处|背面', '布景背面 from_behind'), (r'离云|随鸟|离鸟', '云和鸟 cloud_close'),
       (r'台口外 2\.[2-8]|台口线上|正面全景', '台口正面全景 front_2200 / front_2800 / street_wide'), (r'台口外 1\.[0-7]', '台口正面 front_1500'), (r'台口外 0\.8', '台口内 front_800'), (r'台内|贴地', '台内低机位 inside_low'),
       (r'玻璃外 0\.[2-4]|屏幕占满', '电视玻璃 tv_glass'), (r'玻璃外', '推向电视 tv_push'), (r'离桌面|桌', '桌面微距 desk_macro'), (r'旋钮|机身', '电视机身 tv_body'), (r'S04|全景|离后墙', '房间全景 wide'), (r'离电视|电视顶', '电视顶 tv_top'), (r'叶影|植物', '叶影 leaf_shadow')]
NEED = [(r'纸人|动物头|鬣狗|秃鹫|乌鸦|狼|鳄鱼|野猪', '动物头纸人'), (r'红纸', '红纸'), (r'鸟', '纸鸟'), (r'纸带', '黄纸带'), (r'对话纸片', '对话纸片'), (r'雨', '纸雨'), (r'纸手|检场', '检场人的纸手'), (r'雪花|纸屑', '纸屑雪'),
        (r'白纸片', '白纸片'), (r'小窗灯', '纸人手里的小窗灯'), (r'降下来|滑入|搭起来|升起来|立起来', '搭景动画'), (r'飞走|滑走', '撤景动画'), (r'云', '云的升降'), (r'太阳|天幕由', '太阳和天幕变色'), (r'灯泡.*(灭|亮)|熄', '台口灯泡时间表'),
        (r'窗.*亮|亮.*窗', '窗的时间表'), (r'通电|熄了|关', '电视开关'), (r'幕布|纸幕', '纸幕拉开'), (r'影子', '影子（灯位已有）')]
rows = []; needs = collections.Counter(); cams = collections.Counter(); states = collections.Counter()
for sh in d['shots']:
    bar = bar_of(sh['t0']); st = state(sh, bar); text = sh['camera'] + ' ' + sh['event']
    cam = next((name for pat, name in CAM if re.search(pat, sh['camera'])), '机位待定')
    nd = [name for pat, name in NEED if re.search(pat, text)]
    if sh['type'] == 'L': nd.insert(0, 'lin 的姿势：' + (sh.get('pose') or ''))
    for n in nd: needs[n.split('：')[0]] += 1
    cams[cam] += 1; states[st] += 1
    rows.append(f"| {sh['id']} | {bar} | {tc(sh['t0'])} | {sh['t1'] - sh['t0']:.1f} | {'房间' if sh['type'] in 'RX' else '舞台'} | {st} | {cam} | {'、'.join(nd) if nd else '—'} |")
out = os.path.join(ROOT, 'docs', '全片镜头需求表_v1.md')
with open(out, 'w', encoding='utf-8') as f:
    f.write('# 全片镜头需求表 v1\n\n由 `tools/prep_table.py` 从 `docs/film_v5.json`（83 个镜头）按关键词生成。“机位种类”后面的英文名是已经跑过露空检查的机位；“还缺的东西”是这个镜头用到、但这一轮没有做的道具或动画。关键词匹配是粗的，开工前要逐镜核一遍。\n\n')
    f.write('| 镜号 | 小节 | 时间 | 秒 | 场景 | 状态 | 机位种类 | 还缺的东西 |\n|---|---|---|---|---|---|---|---|\n' + '\n'.join(rows) + '\n')
print('wrote', out)
print('states', dict(states)); print('needs', dict(needs.most_common())); print('cams', dict(cams.most_common()))
