# -*- coding: utf-8 -*-
"""Contact sheets of Sets.CamCheck: one cell per camera = the frame as lit by the plan | what only the shadow-casting lights reach | the white model.
    python -P tools/cam_sheets.py stage|room [out_dir]"""
import sys, os, csv
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
which = sys.argv[1]; src = os.path.join(ROOT, 'unity', 'Capture', 'cams', which)
out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'work', 'review', 'cams'); os.makedirs(out, exist_ok=True)
rows = list(csv.DictReader(open(os.path.join(src, 'cams.csv'), encoding='utf-8')))
W, H, LAB = 400, 225, 26; font = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 15)
per = 8
for part in range(0, len(rows), per):
    chunk = rows[part:part + per]; sheet = Image.new('RGB', (W * 3 + 8, (H + LAB) * len(chunk)), (18, 18, 22)); d = ImageDraw.Draw(sheet)
    for i, r in enumerate(chunk):
        y = i * (H + LAB)
        d.text((6, y + 4), f'{r["camera"]}  [{r["state"]}]  shots {r["shots"]}   void {r["void_px"]}   ambient-only {r["ambient_only_pct"]}%   dead {r["dead_pct"]}%   bare {r["bare_pct"]}%', font=font, fill=(255, 80, 80) if int(r['void_px']) else (220, 220, 220))
        for k, kind in enumerate(('lit', 'shadowlights', 'white')):
            p = os.path.join(src, f'{r["camera"]}_{kind}.jpg')
            if os.path.exists(p): sheet.paste(Image.open(p).resize((W, H)), (k * (W + 4), y + LAB))
    name = os.path.join(out, f'cams_{which}_{part // per + 1}.jpg'); sheet.save(name, quality=86); print('wrote', name, sheet.size)
