# -*- coding: utf-8 -*-
"""The film's version of lin's hand-keyed performance.
    python -P tools/lin_key_film.py
Reads unity/Data/key_song.bin (the whole song, keyed in the singing project; float32: fps, frames, channels, start, then frames x 57 controls)
and writes unity/Data/key_film.bin: the same performance, except where the film needs her for its own story -
  bars 68 - 76: the right hand off the microphone, open under the chest, palm up (the paper bird sits in it), lifted a hand's breadth when the
                bird leaves, then back to the keyed performance; the head down to the bird at first, up after it when it goes;
  bar 84.2 on:  the head lowered (the eyes are shut by the film's own script)."""
import os, numpy as np
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = os.path.join(ROOT, 'unity', 'Data', 'key_song.bin'); dst = os.path.join(ROOT, 'unity', 'Data', 'key_film.bin')
raw = np.fromfile(src, dtype=np.float32); fps, n, nch, t0 = raw[0], int(raw[1]), int(raw[2]), raw[3]; K = raw[4:4 + n * nch].reshape(n, nch).astype(np.float64).copy()
t = t0 + np.arange(n) / fps
B68, B75_2, B76_2, B84_2 = 146.030, 162.250, 164.410, 181.406          # FilmBars.At(68), (75, 2), (76, 2), (84, 2)


def ss(a, b, x):
    u = np.clip((x - a) / (b - a), 0, 1); return u * u * u * (u * (6 * u - 15) + 10)


def unit(v):
    return v / np.maximum(1e-9, np.linalg.norm(v, axis=-1, keepdims=True))


w = ss(B68 - 1.9, B68 - 0.5, t) * (1 - ss(B76_2, B76_2 + 1.6, t))          # the hand belongs to the bird
lift = ss(B75_2 - 0.08, B75_2 + 0.50, t)                                    # the bird leaves it
drift = np.stack([0.004 * np.sin(t * 1.9 + 0.4), 0.004 * np.sin(t * 1.3 + 2.1) + 0.003 * np.sin(t * 0.61), 0.003 * np.sin(t * 2.3)], 1)
settle = np.exp(-4.0 * np.maximum(0, t - B75_2 - 0.5)) * np.sin(6.0 * np.maximum(0, t - B75_2 - 0.5)) * (t > B75_2 + 0.5)
pos = np.array([0.215, 1.100, 0.300]) + lift[:, None] * np.array([0.030, 0.170, 0.080]) + drift + settle[:, None] * np.array([0, 0.012, 0])
fin = unit(np.array([-0.30, 0.04, 0.95]) + lift[:, None] * np.array([0.08, 0.18, 0.0])); palm = unit(np.array([0.0, 1.0, 0.10]) + lift[:, None] * np.array([0, 0, -0.28])); pole = unit(np.array([0.55, -0.80, -0.20]))
o = 14; W = w[:, None]
K[:, o:o + 3] = K[:, o:o + 3] * (1 - W) + pos * W
for a, tg in ((o + 3, fin), (o + 6, palm), (o + 9, np.broadcast_to(pole, (n, 3)))):
    K[:, a:a + 3] = unit(unit(K[:, a:a + 3]) * (1 - W) + tg * W)
curl = np.array([0.0, 0.12, 0.10, 0.12, 0.16]) * (1 - 0.5 * lift[:, None])
K[:, o + 12:o + 17] = K[:, o + 12:o + 17] * (1 - W) + curl * W; K[:, o + 17] = K[:, o + 17] * (1 - w) + 0.25 * w
K[:, 50] = K[:, 50] * (1 - w) + 1.0 * w; K[:, 52] *= (1 - w); K[:, 54] *= (1 - w)
# the head: down to the bird while it settles, up after it when it goes, lowered at the end
down = ss(B68 - 0.6, B68 + 0.2, t) * (1 - ss(B68 + 5.0, B68 + 6.5, t)); up = ss(B75_2, B75_2 + 0.5, t) * (1 - ss(B76_2, B76_2 + 1.6, t)); end = ss(B84_2, B84_2 + 1.8, t)
K[:, 9] = K[:, 9] * (1 - 0.7 * down) + 11.0 * 0.7 * down
K[:, 9] = K[:, 9] * (1 - up) + (-17.0) * up; K[:, 10] = K[:, 10] * (1 - 0.8 * up) + 10.0 * 0.8 * up; K[:, 6] += -3.0 * up
front = ss(175.2, 176.2, t) * (1 - ss(178.3, 179.3, t))                      # S77: she sings the last line straight at the lens, the face not hidden under the hair
K[:, 9] = K[:, 9] * (1 - front) + np.minimum(K[:, 9], 4.0) * front
K[:, 9] = K[:, 9] * (1 - end) + np.maximum(K[:, 9], 13.0) * end
K[:, 56] *= (1 - 0.6 * up) * (1 - 0.5 * end)                                 # the mouth may leave the microphone for those two moments
# S55 / S56 (the struck stage, bars 58 - 60): in the song she holds the right hand out for four seconds - on the empty stage that read as a
# frozen arm. Here the hand comes back to rest in front of the waist within the shot, the face turns a little aside, the mouth is let off the microphone.
rest = K[int((21.17 - t0) * fps), o:o + 18].copy()                              # a rest pose she already has (bar 10)
wr = ss(124.95, 126.55, t) * (1 - ss(129.3, 130.4, t)); Wr = wr[:, None]
bre = np.stack([0.005 * np.sin(t * 0.83 + 1.0), 0.006 * np.sin(t * 1.21) + 0.004 * np.sin(t * 0.47 + 2.0), 0.004 * np.sin(t * 0.67 + 0.5)], 1)
arc = np.sin(np.pi * ss(124.95, 126.55, t))[:, None] * np.array([0.035, -0.02, 0.03])   # the hand swings out a little on its way down
K[:, o:o + 3] = K[:, o:o + 3] * (1 - Wr) + (rest[0:3] + bre + arc) * Wr
for a in (o + 3, o + 6, o + 9):
    K[:, a:a + 3] = unit(unit(K[:, a:a + 3]) * (1 - Wr) + rest[a - o:a - o + 3] * Wr)
lag = ss(125.25, 126.95, t) * (1 - ss(129.3, 130.4, t))                         # the fingers arrive after the wrist
K[:, o + 12:o + 18] = K[:, o + 12:o + 18] * (1 - lag[:, None]) + rest[12:18] * lag[:, None]
aside = ss(125.3, 127.6, t) * (1 - ss(129.2, 130.4, t))
K[:, 10] = K[:, 10] * (1 - aside) + (-15.0 + 1.5 * np.sin(t * 0.9)) * aside; K[:, 9] = K[:, 9] * (1 - 0.5 * aside) + 3.0 * 0.5 * aside; K[:, 7] += -4.0 * aside
K[:, 56] *= 1 - 0.65 * ss(124.2, 125.2, t) * (1 - ss(129.3, 130.4, t))
out = np.concatenate([raw[:4], K.astype(np.float32).ravel()]); out.tofile(dst)
print('wrote', dst, n, 'frames', fps, 'fps; bird hand', round(float(t[w > 0.5][0]), 2), '-', round(float(t[w > 0.5][-1]), 2))
