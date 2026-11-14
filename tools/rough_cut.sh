#!/bin/bash
# a cut of the whole film:  tools/rough_cut.sh <dir> <tv frames dir> <width> <height> <out name> [extra finish args]
# room shots in edit mode, stage shots in play mode, both into unity/Capture/<dir>/Snn, then tools/finish_film.py
cd /e/sucai/ikiteita || exit 1
D=$1; TV=$2; W=$3; H=$4; OUT=$5; shift 5
rm -rf unity/Capture/$D
bash tools/sets.sh Open '["room"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
bash tools/sets.sh RoomFilm "[\"1-7,46,52,74,81,82\",8,1,$W,$H,\"$D\",\"$TV\"]" 3000 2>&1 | grep -E "room:|rror" | cut -c1-200
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
R=$(bash tools/sets.sh SetupShotBaker "[\"8-80\",8,1,$W,$H,\"$D\"]" 120 2>&1 | grep -iE "error|ready" | head -3 | cut -c1-200); echo "$R"
case "$R" in *"shot baker ready"*) ;; *) exit 1;; esac
unity command editor.playmode start --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-120
n=0; last=-1; same=0
until [ $n -ge 400 ]; do
  sleep 10; n=$((n+1)); c=$(find unity/Capture/$D -name "*.jpg" | wc -l)
  if [ "$c" = "$last" ]; then same=$((same+1)); else same=0; fi; last=$c
  if [ $same -ge 3 ]; then break; fi
done
echo "frames $last"
unity command editor.playmode stop --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-100
PYTHONUTF8=1 python -P tools/finish_film.py $D "deliver/film/$OUT.mp4" "$@" 2>&1 | tail -2 | cut -c1-300
echo cut-done
