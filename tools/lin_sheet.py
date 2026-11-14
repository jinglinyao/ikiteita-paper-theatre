# contact sheets of a play-mode pass:  python tools/lin_sheet.py <capture dir> <out prefix> [cols] [per sheet]
import sys, os, glob
from PIL import Image, ImageDraw
d, out = sys.argv[1], sys.argv[2]; cols = int(sys.argv[3]) if len(sys.argv) > 3 else 5; per = int(sys.argv[4]) if len(sys.argv) > 4 else 25
fs = sorted(glob.glob(os.path.join(d, 'S*', 'f_*.jpg'))); W, H = 384, 216
for k in range(0, len(fs), per):
    part = fs[k:k + per]; rows = (len(part) + cols - 1) // cols; im = Image.new('RGB', (cols * W, rows * H)); dr = ImageDraw.Draw(im)
    for i, f in enumerate(part):
        im.paste(Image.open(f).resize((W, H)), ((i % cols) * W, (i // cols) * H)); dr.text(((i % cols) * W + 4, (i // cols) * H + 3), os.path.basename(os.path.dirname(f)) + ' ' + os.path.basename(f)[2:7], fill=(255, 235, 120))
    im.save(f'{out}_{k // per}.jpg', quality=85)
print(len(fs), 'frames')
