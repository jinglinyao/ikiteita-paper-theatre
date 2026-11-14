# -*- coding: utf-8 -*-
"""The caption sheet from the text the user supplied.
    python -P tools/captions_build.py
work/lyrics/user_paste.txt  (the user's own paste: a Japanese line, its reading in Latin letters, its Chinese line; a few quoted lines come as
                             two Japanese / Chinese pairs with one reading)
E:/sucai/_assets/vrm/song2/lyrics_kana.json   (68 sung lines with start and end, from the forced alignment of the other project)
-> docs/字幕.json : [start, end, Japanese, Chinese, reading]. The times come from matching each reading against the kana lines (monotone).
Nothing of the text is printed: only counts, times and how well each line matched."""
import os, re, json, difflib, unicodedata
import pykakasi
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
raw = [l.strip() for l in open(os.path.join(ROOT, 'work', 'lyrics', 'user_paste.txt'), encoding='utf-8').read().split('\n') if l.strip()]
KANA = re.compile(r'[\u3040-\u30ff]'); CJK = re.compile(r'[\u4e00-\u9fff]')
# the paste has some Japanese lines typed with simplified characters: back to the forms the language uses (Japanese lines only)
FORM = dict(zip('杀饵马绮丽谁证飞远长时风鸟叶侧离绞', '殺餌馬綺麗誰証飛遠長時風鳥葉側離絞'))


def kind(l):
    if KANA.search(l): return 'ja'
    if CJK.search(l): return 'zh'
    return 'ro'


ent = []
for l in raw:
    k = kind(l)
    if k == 'ja': ent.append({'ja': ''.join(FORM.get(c, c) for c in l), 'zh': '', 'ro': ''})
    elif ent:
        if k == 'zh': ent[-1]['zh'] = (ent[-1]['zh'] + ' ' + l).strip()
        else: ent[-1]['ro'] = (ent[-1]['ro'] + ' ' + l).strip()
# a quoted line split in two shares one reading: join such a pair
out = []; i = 0
while i < len(ent):
    e = dict(ent[i])
    while not e['ro'] and i + 1 < len(ent):
        i += 1; n = ent[i]; e['ja'] += n['ja']; e['zh'] = (e['zh'] + ' ' + n['zh']).strip(); e['ro'] = n['ro']
    out.append(e); i += 1
for e in out: e['ro'] = e['ro'].replace('‘', '').replace('’', '').replace("'", '').strip()

kks = pykakasi.kakasi()


def norm(s):
    s = unicodedata.normalize('NFKD', s.lower()); s = ''.join(c for c in s if c.isascii() and c.isalpha())
    return s.replace('ou', 'o').replace('oo', 'o').replace('uu', 'u').replace('wo', 'o').replace('nn', 'n').replace('mb', 'nb')


L = json.load(open('E:/sucai/_assets/vrm/song2/lyrics_kana.json', encoding='utf-8'))
# each sung line's own start and end from the per-mora forced alignment (the line list itself runs each line on from the one before)
MJ = json.load(open('E:/sucai/_assets/vrm/song2/morae_full_sorted.json', encoding='utf-8')); fixed = 0
for x, ml in zip(L, MJ['lines']):                                               # [index, morae, start, end, score, the old start]
    if abs(ml[5] - x['t']) < 0.05 and ml[3] > ml[2]: x['t'], x['end'] = float(ml[2]), float(ml[3]); fixed += 1
print('line times from the forced alignment:', fixed, 'of', len(L))
lr = [norm(''.join(x['hepburn'] for x in kks.convert(x['ja']))) for x in L]; er = [norm(e['ro']) for e in out]
n, m = len(out), len(lr); INF = 1e9
best = [[INF] * (m + 1) for _ in range(n + 1)]; back = [[0] * (m + 1) for _ in range(n + 1)]; best[0][0] = 0
for a in range(1, n + 1):
    for b in range(1, m + 1):
        for k in range(1, 5):
            if b - k < 0 or best[a - 1][b - k] >= INF: continue
            c = 1 - difflib.SequenceMatcher(None, er[a - 1], ''.join(lr[b - k:b])).ratio() + 0.02 * (k - 1)
            if best[a - 1][b - k] + c < best[a][b]: best[a][b] = best[a - 1][b - k] + c; back[a][b] = k
rows = []; b = m; spans = []
for a in range(n, 0, -1):
    k = back[a][b]; spans.append((b - k, b)); b -= k
spans.reverse(); worst = []
for e, (s0, s1) in zip(out, spans):
    c = 1 - difflib.SequenceMatcher(None, norm(e['ro']), ''.join(lr[s0:s1])).ratio(); worst.append(c)
    rows.append([round(float(L[s0]['t']), 2), round(float(L[s1 - 1]['end']), 2), e['ja'], e['zh'], e['ro']])
# a caption stays until it has been read, and leaves before the next one comes
for i, r in enumerate(rows):
    r[0] = round(r[0] - 0.06, 2); r[1] = max(r[1] + 0.45, r[0] + 1.3)
for i, r in enumerate(rows):
    if i + 1 < len(rows): r[1] = round(min(r[1], rows[i + 1][0] - 0.08), 2)
    else: r[1] = round(r[1] + 0.4, 2)
open(os.path.join(ROOT, 'docs', '字幕.json'), 'w', encoding='utf-8').write('[\n' + ',\n'.join(json.dumps(r, ensure_ascii=False) for r in rows) + '\n]\n')
print('captions', len(rows), 'from', len(raw), 'pasted lines and', m, 'timed lines; all timed lines used:', b == 0)
print('match cost: mean %.2f, worst %.2f; lines over 0.35:' % (sum(worst) / len(worst), max(worst)), [(i, round(c, 2), rows[i][0]) for i, c in enumerate(worst) if c > 0.35])
print('lines per caption:', ''.join(str(s1 - s0) for s0, s1 in spans)); print('first', rows[0][:2], 'last', rows[-1][:2], 'longest (chars ja/ro):', max(len(r[2]) for r in rows), max(len(r[4]) for r in rows))
missing = set()
try:
    from fontTools.ttLib import TTFont
    cm = TTFont('C:/Windows/Fonts/NotoSerifSC-VF.ttf', lazy=True).getBestCmap()
    for r in rows:
        for c in r[2] + r[3]:
            if ord(c) > 0x2000 and ord(c) not in cm: missing.add(hex(ord(c)))
except Exception as ex: print('font check failed', ex)
print('characters the caption font lacks:', len(missing), sorted(missing)[:10])
