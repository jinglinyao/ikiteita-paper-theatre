#!/bin/bash
# the film again with the singing lin: the television's picture where she is in it, the room shots that show it, every stage shot she is in,
# then the finish:  tools/redo_lin.sh <out name>      (frames: cut3 -> cut4, tv6 -> tv7; the old frames stay)
cd /e/sucai/ikiteita || exit 1
OUT=$1; C=unity/Capture; PP=E:/sucai/ikiteita/unity
ready() { n=0; until unity status --project-path $PP 2>&1 | grep -q "ready" || [ $n -ge 60 ]; do sleep 4; n=$((n+1)); done; }
[ -d $C/cut4 ] || cp -r $C/cut3 $C/cut4
[ -d $C/tv7 ] || cp -r $C/tv6 $C/tv7
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
for R in "2500 2535" "4060 4140" "4440 4634"; do
  set -- $R; want=$(( $2 - $1 ))
  X=$(bash tools/sets.sh SetupFilmBaker "[$1,$2,1,1440,1080,\"tv_part\"]" 120 2>&1 | grep -iE "error|ready" | head -2 | cut -c1-160); echo "$X"
  case "$X" in *"film baker ready"*) ;; *) exit 1;; esac
  unity command editor.playmode start --project-path $PP 2>&1 | tail -1 | cut -c1-100
  n=0; until [ "$(ls $C/tv_part 2>/dev/null | wc -l)" -ge $want ] || [ $n -ge 120 ]; do sleep 4; n=$((n+1)); done
  echo "tv $1-$2: $(ls $C/tv_part | wc -l)"
  unity command editor.playmode stop --project-path $PP 2>&1 | tail -1 | cut -c1-80; ready
  cp $C/tv_part/*.jpg $C/tv7/
done
bash tools/sets.sh SetupShotBaker '["8",0,1,64,36,"tv_dummy"]' 60 >/dev/null 2>&1     # takes the film baker out of the scene
for s in 23 24 25 26 27 28 29 30 47 48 49 50 51 54 55 56 57 63 64 65 66 67 68 69 70 71 72 73 75 76 77 78 79 80; do rm -rf $C/cut4/S$s; done
bash tools/play_shots.sh "23-30,47-51,54-57,63-73,75-80" cut4 2560 1440 1 8 2>&1 | tail -3
bash tools/sets.sh Open '["room"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
bash tools/sets.sh RoomFilm '["46,74,81,82",8,1,2560,1440,"cut4","tv7"]' 3000 2>&1 | grep -E "room:|rror" | cut -c1-200
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
for s in $C/cut4/S*; do echo -n "$(basename $s):$(ls $s | wc -l) "; done; echo
PYTHONUTF8=1 python -P tools/finish_film.py cut4 deliver/film/$OUT.mp4 --crf 16 2>&1 | tail -1 | cut -c1-200
ffmpeg -y -loglevel error -i deliver/film/$OUT.mp4 -vf scale=1280:720 -c:v libx264 -crf 22 -preset fast -c:a copy deliver/film/${OUT}_720预览.mp4
ffprobe -v error -show_entries format=duration,size -of csv=p=0 deliver/film/$OUT.mp4
echo redo-lin-done
