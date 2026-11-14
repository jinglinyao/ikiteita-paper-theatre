#!/bin/bash
# the five other animal heads: an image of a paper mask (MiniMax), then a model of it (Bailian Tripo). Keys come from the environment.
cd /e/sucai/ikiteita || exit 1
export PYTHONUTF8=1
python -P tools/gen_art.py work/gen_art vulture crow wolf crocodile boar
for n in vulture crow wolf crocodile boar; do
  [ -f work/tripo/$n.glb ] && continue
  python -c "from PIL import Image; Image.open('work/gen_art/$n.jpg').convert('RGB').save('work/tripo/$n.png')" || continue
  python /e/sucai/knight3d/tripo3d.py work/tripo/$n.png work/tripo/$n 2>&1 | tail -2
done
ls -la work/tripo | awk '{print $5,$9}'
