# -*- coding: utf-8 -*-
"""Motion checks for the singer in S16, from Capture/slice_probe.csv (one row per frame, 24 fps).
The same things the stuck performance pipeline failed on, now as hard numbers: arrival of the hand, frame-to-frame jitter,
backward lean, head speed; plus how much the hair moves.   python -P tools/slice_metrics.py <probe.csv>
"""
import sys, csv, math
import numpy as np

rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8')))
g = lambda k: np.array([float(r[k]) for r in rows])
t = g('t'); fps = 24.0; n = len(t)
R = np.stack([g('rx'), g('ry'), g('rz')], 1); L = np.stack([g('lx'), g('ly'), g('lz')], 1)
tip = np.stack([g('tipx'), g('tipy'), g('tipz')], 1); head = np.stack([g('headx'), g('heady'), g('headz')], 1)
lean, hy, hp = g('lean'), g('headyaw'), g('headpitch'); hips = np.stack([g('hx'), g('hz')], 1)

def speed(P): return np.linalg.norm(np.diff(P, axis=0), axis=1) * fps
def zigzag(P):
    """frames where the acceleration flips sign from one frame to the next on some axis: amplitude in mm per frame"""
    a = P[2:] - 2 * P[1:-1] + P[:-2]; flip = (a[1:] * a[:-1] < 0); amp = np.minimum(np.abs(a[1:]), np.abs(a[:-1])) * flip
    return amp.max() * 1000, int((amp.max(axis=1) > 0.0005).sum())

sr = speed(R); final = R[-24:].mean(0); dist = np.linalg.norm(R - final, axis=1)
POSE = 55.24
arrive = next((t[i] for i in range(n) if np.all(dist[i:] < 0.008)), float('nan'))
last10 = [t[i] for i in range(n) if dist[i] > 0.10]
print(f'frames {n}, {t[0]:.2f}-{t[-1]:.2f} s')
print(f'right wrist: path while arriving {dist[0] * 100:.1f} cm; within 8 mm of rest from {arrive:.2f} s ({arrive - POSE:+.2f} s against the bar line); top speed {sr.max():.3f} m/s; speed after 56.5 s max {sr[t[1:] > 56.5].max() * 100:.2f} cm/s')
za, zc = zigzag(R); print(f'right wrist frame-to-frame zig-zag: max {za:.2f} mm, frames over 0.5 mm: {zc}')
za, zc = zigzag(L); print(f'left wrist  frame-to-frame zig-zag: max {za:.2f} mm, frames over 0.5 mm: {zc}; top speed {speed(L).max() * 100:.2f} cm/s')
print(f'chest lean (+ forward, - back): {lean.min():+.2f} .. {lean.max():+.2f} deg')
print(f'hips: sideways {1000 * (hips[:, 0].max() - hips[:, 0].min()):.1f} mm, fore-aft {1000 * (hips[:, 1].max() - hips[:, 1].min()):.1f} mm')
hs = np.hypot(np.diff(hy), np.diff(hp)) * fps
print(f'head: yaw {hy.min():+.1f} .. {hy.max():+.1f} deg, pitch {hp.min():+.1f} .. {hp.max():+.1f} deg, top speed {hs.max():.1f} deg/s')
rel = tip - head; rs = speed(rel)
print(f'hair tip against the head: range {100 * np.linalg.norm(rel.max(0) - rel.min(0)):.1f} cm, mean speed {100 * rs.mean():.1f} cm/s')
