#!/bin/bash
# version 3: every stage shot again (the paper girl, the beams, the pictures), the television's frames the room shots use, the room shots,
# the finish:  tools/redo_v3.sh <out name>      (frames: cut4 -> cut5, the television in tv8)
cd /e/sucai/ikiteita || exit 1
OUT=$1; C=unity/Capture; PP=E:/sucai/ikiteita/unity
ready() { n=0; until unity status --project-path $PP 2>&1 | grep -q "ready" || [ $n -ge 60 ]; do sleep 4; n=$((n+1)); done; }
[ -d $C/cut5 ] || cp -r $C/cut4 $C/cut5
PYTHONUTF8=1 python -P tools/tv_clips.py tv7 tv8 | tail -1
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
for R in "383 477" "2433 2527" "4060 4140" "4440 4634"; do
  set -- $R; want=$(( $2 - $1 ))
  X=$(bash tools/sets.sh SetupFilmBaker "[$1,$2,1,1440,1080,\"tv_part\"]" 120 2>&1 | grep -iE "error|ready" | head -2 | cut -c1-160); echo "$X"
  case "$X" in *"film baker ready"*) ;; *) exit 1;; esac
  unity command editor.playmode start --project-path $PP 2>&1 | tail -1 | cut -c1-100
  n=0; until [ "$(ls $C/tv_part 2>/dev/null | wc -l)" -ge $want ] || [ $n -ge 120 ]; do sleep 4; n=$((n+1)); done
  echo "tv $1-$2: $(ls $C/tv_part | wc -l)"
  unity command editor.playmode stop --project-path $PP 2>&1 | tail -1 | cut -c1-80; ready
  cp $C/tv_part/*.jpg $C/tv8/
done
bash tools/sets.sh SetupShotBaker '["8",0,1,64,36,"tv_dummy"]' 60 >/dev/null 2>&1
for n in $(seq 8 80); do s=$(printf "%02d" $n); case $n in 46|52|74) ;; *) rm -rf $C/cut5/S$s;; esac; done
bash tools/play_shots.sh "8-80" cut5 2560 1440 1 8 2>&1 | tail -3
bash tools/sets.sh Open '["room"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
bash tools/sets.sh RoomFilm '["1-7,46,74,81,82",8,1,2560,1440,"cut5","tv8"]' 3000 2>&1 | grep -E "room:|rror" | cut -c1-200
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
for s in $C/cut5/S*; do echo -n "$(basename $s):$(ls $s | wc -l) "; done; echo
PYTHONUTF8=1 python -P tools/finish_film.py cut5 deliver/film/$OUT.mp4 --crf 16 --captions docs/字幕.json 2>&1 | tail -1 | cut -c1-200
ffmpeg -y -loglevel error -i deliver/film/$OUT.mp4 -vf scale=1280:720 -c:v libx264 -crf 22 -preset fast -c:a copy deliver/film/${OUT}_720预览.mp4
ffprobe -v error -show_entries format=duration,size -of csv=p=0 deliver/film/$OUT.mp4
echo redo-v3-done
