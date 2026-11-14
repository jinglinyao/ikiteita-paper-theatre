#!/bin/bash
# the last repair round: room shots 5 and 52, stage shots 56-58 and 72 into cut3, then the finish, the small preview and the loudness
cd /e/sucai/ikiteita || exit 1
D=cut3
bash tools/sets.sh RoomFilm "[\"5,52\",8,1,2560,1440,\"$D\",\"tv6\"]" 3000 2>&1 | grep -E "room:|rror" | cut -c1-200
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
for s in 56 57 58 72; do rm -rf unity/Capture/$D/S$s; done
R=$(bash tools/sets.sh SetupShotBaker "[\"56-58,72\",8,1,2560,1440,\"$D\"]" 120 2>&1 | grep -iE "error|ready" | head -3 | cut -c1-200); echo "$R"
case "$R" in *"shot baker ready"*) ;; *) exit 1;; esac
unity command editor.playmode start --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-120
n=0; last=-1; same=0
until [ $n -ge 200 ]; do
  sleep 10; n=$((n+1)); c=$(find unity/Capture/$D/S56 unity/Capture/$D/S57 unity/Capture/$D/S58 unity/Capture/$D/S72 -name "*.jpg" 2>/dev/null | wc -l)
  if [ "$c" = "$last" ] && [ "$c" -gt 0 ]; then same=$((same+1)); else same=0; fi; last=$c
  if [ $same -ge 3 ]; then break; fi
done
echo "frames $last"
unity command editor.playmode stop --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-100
for s in 05 52 56 57 58 72; do echo "S$s $(ls unity/Capture/$D/S$s | wc -l)"; done
PYTHONUTF8=1 python -P tools/finish_film.py $D deliver/film/成片_v1_2560.mp4 --crf 16 2>&1 | tail -1 | cut -c1-200
ffmpeg -y -loglevel error -i deliver/film/成片_v1_2560.mp4 -vf scale=1280:720 -c:v libx264 -crf 22 -preset fast -c:a copy deliver/film/成片_v1_720预览.mp4
ffprobe -v error -show_entries format=duration,size -of csv=p=0 deliver/film/成片_v1_2560.mp4
ffmpeg -hide_banner -nostats -i deliver/film/成片_v1_2560.mp4 -af ebur128 -f null - 2>&1 | grep -E "I:|LRA:" | tail -2
echo redo-done
