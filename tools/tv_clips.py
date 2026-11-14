# -*- coding: utf-8 -*-
"""The television's picture before the curtain: three generated clips, the channel switching between them.
    python -P tools/tv_clips.py <tv in> <tv out>
Copies unity/Capture/<tv in> to <tv out> and replaces frames 14..344 (1440x1080, the tube is 4:3 like the clips: nothing is cropped away
but a hair at the edges). The office -> the roof -> the street; four frames of snow between channels; then the stage's own picture."""
import sys, os, shutil, subprocess, numpy as np
from PIL import Image
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); CAP = os.path.join(ROOT, 'unity', 'Capture'); src, dst = sys.argv[1], sys.argv[2]
if not os.path.isdir(os.path.join(CAP, dst)): shutil.copytree(os.path.join(CAP, src), os.path.join(CAP, dst))
W, H = 1440, 1080
PLAN = [('v3_office', 0, 100, 0.9), ('v4_roof', 104, 215, 0.3), ('v1_news', 219, 337, 0.1)]   # clip, first frame, end frame, where in the clip it starts (s)
rng = np.random.default_rng(7)


def clip_frames(name, t0, n):
    p = os.path.join(ROOT, 'gen', 'video', name + '.mp4'); tmp = os.path.join(ROOT, 'gen', 'video', '_f_' + name); shutil.rmtree(tmp, ignore_errors=True); os.makedirs(tmp)
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-ss', f'{t0:.3f}', '-i', p, '-vf', f'fps=24,scale={W}:{H}:force_original_aspect_ratio=increase,crop={W}:{H}', '-frames:v', str(n), '-q:v', '2', os.path.join(tmp, 'f_%04d.jpg')], check=True)
    fs = sorted(os.listdir(tmp)); return [os.path.join(tmp, f) for f in fs]


def snow(k):
    g = rng.random((H // 4, W // 4)); g = np.repeat(np.repeat(g, 4, 0), 4, 1); a = (0.10 + 0.55 * g)[..., None] * np.array([0.95, 1.0, 1.08]); a[(np.arange(H) + k * 37) % 180 < 18] *= 1.5
    return np.clip(a * 255, 0, 255).astype(np.uint8)


def grade(im):
    a = np.asarray(im).astype(np.float32) / 255.0; l = a.mean(2, keepdims=True); a = l + (a - l) * 0.82; a = a * np.array([0.96, 1.0, 1.05]) * 0.66      # a little of the tube: less colour, cooler
    return np.clip(a * 255, 0, 255).astype(np.uint8)


written = 0
for (name, f0, f1, t0) in PLAN:
    fs = clip_frames(name, t0, f1 - f0)
    for i in range(f1 - f0):
        im = Image.open(fs[min(i, len(fs) - 1)]).convert('RGB'); Image.fromarray(grade(im)).save(os.path.join(CAP, dst, f'f_{f0 + i:05d}.jpg'), quality=92); written += 1
for a, b in ((100, 104), (215, 219), (337, 345)):
    for f in range(a, b): Image.fromarray(snow(f)).save(os.path.join(CAP, dst, f'f_{f:05d}.jpg'), quality=90); written += 1
print('tv clips:', written, 'frames into', dst)
