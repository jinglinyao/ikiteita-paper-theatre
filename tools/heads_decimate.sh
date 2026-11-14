#!/bin/bash
# reduce every generated head that has no reduced copy yet
cd /e/sucai/ikiteita || exit 1
for n in hyena vulture crow wolf crocodile boar; do
  [ -f work/tripo/$n.glb ] || continue; [ -f unity/Assets/Props/heads/$n.glb ] && continue
  "/e/Blender/blender-5.2.2-windows-x64/blender.exe" -b -P tools/decimate_head.py -- work/tripo/$n.glb unity/Assets/Props/heads/$n.glb 6000 2>&1 | grep -E "^HEAD"
done
ls unity/Assets/Props/heads | grep -c "glb$"
