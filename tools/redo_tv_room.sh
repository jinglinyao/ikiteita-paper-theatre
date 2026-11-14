#!/bin/bash
# the television camera's pass again, then the room shots into an existing cut, then the finish:  tools/redo_tv_room.sh <tv dir> <cut dir> <w> <h> <out name> [finish args]
cd /e/sucai/ikiteita || exit 1
TV=$1; D=$2; W=$3; H=$4; OUT=$5; shift 5
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
rm -rf unity/Capture/$TV
R=$(bash tools/sets.sh SetupFilmBaker "[0,4634,1,1440,1080,\"$TV\"]" 120 2>&1 | grep -iE "error|ready" | head -3 | cut -c1-200); echo "$R"
case "$R" in *"film baker ready"*) ;; *) exit 1;; esac
unity command editor.playmode start --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-120
n=0; until [ "$(ls unity/Capture/$TV 2>/dev/null | wc -l)" -ge 4634 ] || [ $n -ge 300 ]; do sleep 5; n=$((n+1)); done
ls unity/Capture/$TV | wc -l
unity command editor.playmode stop --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-100
n=0; until unity status --project-path E:/sucai/ikiteita/unity 2>&1 | grep -q "ready" || [ $n -ge 60 ]; do sleep 4; n=$((n+1)); done
PYTHONUTF8=1 python -P tools/film_preview.py $TV 全片时间表预览_$TV 2>&1 | tail -1 | cut -c1-160
bash tools/sets.sh Open '["room"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
bash tools/sets.sh RoomFilm "[\"1-7,46,52,74,81,82\",8,1,$W,$H,\"$D\",\"$TV\"]" 3000 2>&1 | grep -E "room:|rror" | cut -c1-200
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
PYTHONUTF8=1 python -P tools/finish_film.py $D "deliver/film/$OUT.mp4" "$@" 2>&1 | tail -2 | cut -c1-300
echo redo-done
