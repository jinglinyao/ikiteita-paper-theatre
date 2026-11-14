# -*- coding: utf-8 -*-
"""Contact sheets of the closed-space probes written by Sets.Probe.
    python -P tools/probe_sheets.py A [out_dir]
One row per probe: the six views (front, right, back, left, up, down) with the plan's lights, then the same six in the detail pass.
Magenta-void views are outlined in red; the probe number, its position and the numbers from probe.csv are printed on the row."""
import sys, os, csv
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
space = sys.argv[1]; src = os.path.join(ROOT, 'unity', 'Capture', 'probe', space)
out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'work', 'review', 'probe'); os.makedirs(out, exist_ok=True)
rows = list(csv.DictReader(open(os.path.join(src, 'probe.csv'), encoding='utf-8')))
probes = sorted({int(r['probe']) for r in rows}); S = 192; LAB = 150
font = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 15)
per = 9
for part in range(0, len(probes), per):
    chunk = probes[part:part + per]
    sheet = Image.new('RGB', (LAB + S * 12 + 12, S * len(chunk)), (18, 18, 22)); d = ImageDraw.Draw(sheet)
    for ri, k in enumerate(chunk):
        rr = [r for r in rows if int(r['probe']) == k]; y = ri * S
        d.text((6, y + 8), f'#{k:02d}\nx {rr[0]["x"]}\ny {rr[0]["y"]}\nz {rr[0]["z"]}\nvoid {sum(int(r["void_px"]) for r in rr)}\nbare {sum(int(r["bare_blocks"]) for r in rr)}/384\nblk {max(float(r["black_pct"]) for r in rr):.0f}%', font=font, fill=(220, 220, 220))
        for di in range(6):
            for pi, kind in enumerate(('lit', 'detail')):
                p = os.path.join(src, f'p{k:02d}_{di}_{kind}.jpg')
                if os.path.exists(p): sheet.paste(Image.open(p).resize((S, S)), (LAB + (pi * 6 + di) * S + pi * 12, y))
            if int(rr[di]['void_px']) > 0: d.rectangle((LAB + di * S + 1, y + 1, LAB + (di + 1) * S - 2, y + S - 2), outline=(255, 40, 40), width=3)
    name = os.path.join(out, f'probe_{space}_{part // per + 1}.jpg'); sheet.save(name, quality=85); print('wrote', name, sheet.size)
tot = sum(int(r['void_px']) for r in rows); bare = sum(int(r['bare_blocks']) for r in rows)
print(f'space {space}: views {len(rows)}, void pixels {tot} ({100 * tot / (len(rows) * 65536):.2f}%), bare blocks {bare}/{len(rows) * 64} ({100 * bare / (len(rows) * 64):.1f}%), worst black {max(float(r["black_pct"]) for r in rows):.0f}%')
