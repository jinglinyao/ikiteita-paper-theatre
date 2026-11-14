# -*- coding: utf-8 -*-
"""contact sheets of one still a shot:  python -P tools/shot_sheet.py <stills_dir> <out_prefix> [cols] [per_sheet]"""
import sys, os, json
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = os.path.join(ROOT, 'unity', 'Capture', sys.argv[1]); out = sys.argv[2]; cols = int(sys.argv[3]) if len(sys.argv) > 3 else 4; per = int(sys.argv[4]) if len(sys.argv) > 4 else 16
d = json.load(open(os.path.join(ROOT, 'docs', 'film_v5.json'), encoding='utf-8')); shots = d['shots']
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 15); W, H, LAB = 480, 270, 40
have = [(i + 1, s) for i, s in enumerate(shots) if os.path.exists(os.path.join(src, 'S%02d.jpg' % (i + 1)))]
for part in range(0, len(have), per):
    chunk = have[part:part + per]; rows = (len(chunk) + cols - 1) // cols; sheet = Image.new('RGB', (W * cols, (H + LAB) * rows), (16, 16, 20)); dr = ImageDraw.Draw(sheet)
    for i, (n, s) in enumerate(chunk):
        x, y = (i % cols) * W, (i // cols) * (H + LAB); sheet.paste(Image.open(os.path.join(src, 'S%02d.jpg' % n)).resize((W, H)), (x, y + LAB))
        dr.text((x + 5, y + 1), f"{s['id']} {s['t0']:.1f}s {s['lens']}mm {s['type']}", font=font, fill=(255, 230, 140)); dr.text((x + 5, y + 19), str(s['event'])[:30], font=font, fill=(225, 222, 212))
    p = f'{out}_{part // per + 1}.jpg'; sheet.save(p, quality=86); print('wrote', p)
