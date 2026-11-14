#!/bin/bash
# The whole film from a fresh clone, in order:  bash tools/render_all.sh [out name] [cut dir] [tv dir]
#   1. the television camera's pass over the whole film (1440x1080, every frame)  -> unity/Capture/<tv>_raw
#   2. the three generated clips written into the television's first 14 seconds    -> unity/Capture/<tv>
#   3. every stage shot in play mode (2560x1440, 8 frames of handle each side)     -> unity/Capture/<cut>/Snn
#   4. the room shots, the television's picture on the screen                       -> unity/Capture/<cut>/Snn
#   5. the finish: joins, title, tears, captions (if docs/字幕.json exists), credits, -14 LUFS -> deliver/film/<out>.mp4
# Needs: the Unity editor open on unity/ (unity open ...; unity status shows "ready"), audio/song.wav, python, ffmpeg.
# Run tools/setup_paths.py once first so the paths below point at this clone.
cd /e/sucai/ikiteita || exit 1
OUT=${1:-成片}; D=${2:-cut}; TV=${3:-tv}; C=unity/Capture; PP=E:/sucai/ikiteita/unity
ready() { n=0; until unity status --project-path $PP 2>&1 | grep -q "ready" || [ $n -ge 90 ]; do sleep 4; n=$((n+1)); done; }
count() { find "$1" -name "*.jpg" 2>/dev/null | wc -l; }
[ -f audio/song.wav ] || { echo "audio/song.wav is missing (the song is not part of this repository)"; exit 1; }
echo "== 1. television pass"
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
X=$(bash tools/sets.sh SetupFilmBaker "[0,4634,1,1440,1080,\"${TV}_raw\"]" 120 2>&1 | grep -iE "error|ready" | head -2 | cut -c1-160); echo "$X"
case "$X" in *"film baker ready"*) ;; *) exit 1;; esac
unity command editor.playmode start --project-path $PP 2>&1 | tail -1 | cut -c1-100
n=0; last=-1; same=0
until [ "$(count $C/${TV}_raw)" -ge 4634 ] || [ $same -ge 30 ]; do sleep 10; c=$(count $C/${TV}_raw); if [ "$c" = "$last" ]; then same=$((same+1)); else same=0; fi; last=$c; done
echo "television frames: $(count $C/${TV}_raw) of 4634"
unity command editor.playmode stop --project-path $PP 2>&1 | tail -1 | cut -c1-80; ready
echo "== 2. the generated clips on the television"
rm -rf $C/$TV; PYTHONUTF8=1 python -P tools/tv_clips.py ${TV}_raw $TV | tail -1
echo "== 3. stage shots"
bash tools/sets.sh SetupShotBaker '["8",0,1,64,36,"_dummy"]' 60 >/dev/null 2>&1                 # takes the film baker out of the scene
bash tools/play_shots.sh "8-80" $D 2560 1440 1 8 2>&1 | tail -3
echo "== 4. room shots"
bash tools/sets.sh Open '["room"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
bash tools/sets.sh RoomFilm "[\"1-7,46,52,74,81,82\",8,1,2560,1440,\"$D\",\"$TV\"]" 3000 2>&1 | grep -E "room:|rror" | cut -c1-200
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
for s in $C/$D/S*; do echo -n "$(basename $s):$(ls $s | wc -l) "; done; echo
echo "== 5. finish"
mkdir -p deliver/film; CAPS=""; [ -f docs/字幕.json ] && CAPS="--captions docs/字幕.json"
PYTHONUTF8=1 python -P tools/finish_film.py $D "deliver/film/$OUT.mp4" --crf 16 $CAPS 2>&1 | tail -2 | cut -c1-300
ffprobe -v error -show_entries format=duration,size -of csv=p=0 "deliver/film/$OUT.mp4"
echo render-all-done
