"""Paper puppet from a front-on render of the character on a transparent ground: flatter 'printed' shading, a border of white card, paper grain.
usage: python make_puppet.py <cut.png> <out.png> [preview.jpg]"""
import sys
from PIL import Image, ImageFilter
import numpy as np
im = Image.open(sys.argv[1]).convert('RGBA'); a = np.asarray(im).astype(np.float32) / 255
al = a[..., 3]; rgb = a[..., :3]
rgb = np.clip((rgb - .5) * 1.05 + .52, 0, 1); q = np.round(rgb * 14) / 14; rgb = rgb * .55 + q * .45
A = Image.fromarray((al * 255).astype(np.uint8))
border = (np.asarray(A.filter(ImageFilter.MaxFilter(25)).filter(ImageFilter.GaussianBlur(2))).astype(np.float32) / 255 > .5).astype(np.float32)
card = np.array([0.93, 0.90, 0.84], np.float32)
out = card[None, None, :] * (1 - al[..., None]) + rgb * al[..., None]
rng = np.random.default_rng(3)
g8 = Image.fromarray(rng.integers(0, 256, al.shape, dtype=np.uint8)).filter(ImageFilter.GaussianBlur(1.2)); g = (np.asarray(g8).astype(np.float32) - 127.5) / 40.0
out = np.clip(out * (1 + .05 * g)[..., None], 0, 1)
res = Image.fromarray((np.dstack([out, border]) * 255).astype(np.uint8), 'RGBA')
bb = Image.fromarray((border * 255).astype(np.uint8)).getbbox(); res = res.crop((max(0, bb[0] - 6), max(0, bb[1] - 6), min(res.width, bb[2] + 6), min(res.height, bb[3] + 6)))
res.save(sys.argv[2]); print('puppet', res.size)
if len(sys.argv) > 3:
    bg = Image.new('RGBA', res.size, (60, 70, 90, 255)); bg.alpha_composite(res); bg.convert('RGB').resize((res.width // 3, res.height // 3)).save(sys.argv[3])
