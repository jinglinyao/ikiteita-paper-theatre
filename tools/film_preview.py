# -*- coding: utf-8 -*-
"""The preview of the stage timeline from the television camera: a video with the song, and contact sheets with one frame a bar.
    python -P tools/film_preview.py [frames_dir] [out_name]
Frames are unity/Capture/<frames_dir>/f_#####.jpg at 24 a second (default dir: tv)."""
import sys, os, json, subprocess, bisect
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = os.path.join(ROOT, 'unity', 'Capture', sys.argv[1] if len(sys.argv) > 1 else 'tv'); name = sys.argv[2] if len(sys.argv) > 2 else '全片时间表预览_v1'
d = json.load(open(os.path.join(ROOT, 'docs', 'film_v5.json'), encoding='utf-8')); bars = sorted((int(k), v) for k, v in d['bars'].items()); bt = [v for _, v in bars] + [d['duration']]
out_dir = os.path.join(ROOT, 'deliver', 'film'); os.makedirs(out_dir, exist_ok=True); rev = os.path.join(ROOT, 'work', 'review', 'film_' + (sys.argv[1] if len(sys.argv) > 1 else 'tv')); os.makedirs(rev, exist_ok=True)
TRACK = [(1, 'pre'), (8, 'build'), (12, 'night'), (25, 'spot'), (26, 'night'), (30, 'spot'), (34, 'night'), (45, 'dusk'), (49, 'night'), (57, 'empty'), (62, 'dim'), (68, 'spot'), (71, 'night'), (84, 'dark'), (86, 'off')]
shots = d['shots']


def state(bar): return [s for b, s in TRACK if b <= bar][-1]


def shot_at(t):
    for s in shots:
        if s['t0'] <= t < s['t1']: return s
    return shots[-1]


mp4 = os.path.join(out_dir, name + '.mp4')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', '24', '-i', os.path.join(src, 'f_%05d.jpg'), '-i', os.path.join(ROOT, 'audio', 'song.wav'),
                '-af', 'loudnorm=I=-14:TP=-1.5:LRA=11', '-ar', '48000', '-c:v', 'libx264', '-preset', 'medium', '-crf', '20', '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '192k', '-shortest', mp4], check=True)
print('wrote', mp4)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 17); W, H, LAB = 400, 300, 46; cols = 6; per = 30
picks = []
for b, t0 in bars:
    for beat in (0.5, 2.5):
        t = t0 + (bt[b] - t0) * beat / 4.0; f = min(int(round(t * 24)), d['frames'] - 1); picks.append((b, beat, t, f))
for part in range(0, len(picks), per):
    chunk = picks[part:part + per]; rows = (len(chunk) + cols - 1) // cols
    sheet = Image.new('RGB', (W * cols, (H + LAB) * rows), (16, 16, 20)); dr = ImageDraw.Draw(sheet)
    for i, (b, beat, t, f) in enumerate(chunk):
        x, y = (i % cols) * W, (i // cols) * (H + LAB); p = os.path.join(src, 'f_%05d.jpg' % f)
        if os.path.exists(p): sheet.paste(Image.open(p).resize((W, H)), (x, y + LAB))
        s = shot_at(t); dr.text((x + 6, y + 2), f'小节 {b} 第 {beat:.1f} 拍  {int(t // 60)}:{t % 60:04.1f}  [{state(b)}]', font=font, fill=(240, 236, 226))
        dr.text((x + 6, y + 23), f'{s["id"]} {s["event"][:19]}', font=font, fill=(170, 176, 190))
    sheet.save(os.path.join(rev, f'timeline_{part // per + 1}.jpg'), quality=84); print('wrote timeline sheet', part // per + 1, sheet.size)
