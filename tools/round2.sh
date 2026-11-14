#!/bin/bash
# the television camera's pass over the whole film, then the cut:  tools/round2.sh <tv dir> <preview name> <cut dir> <cut name> <w> <h> [finish args]
cd /e/sucai/ikiteita || exit 1
TV=$1; PV=$2; D=$3; OUT=$4; W=$5; H=$6; shift 6
unity recompile --project-path E:/sucai/ikiteita/unity --timeout 180 2>&1 | grep -E "error" | head -5 | cut -c1-200
n=0; until unity status --project-path E:/sucai/ikiteita/unity 2>&1 | grep -q "ready" || [ $n -ge 60 ]; do sleep 4; n=$((n+1)); done
bash tools/sets.sh Open '["stage"]' 120 2>&1 | grep -E "opened|rror" | cut -c1-120
rm -rf unity/Capture/$TV
R=$(bash tools/sets.sh SetupFilmBaker "[0,4634,1,960,720,\"$TV\"]" 120 2>&1 | grep -iE "error|ready" | head -3 | cut -c1-200); echo "$R"
case "$R" in *"film baker ready"*) ;; *) exit 1;; esac
unity command editor.playmode start --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-120
n=0; until [ "$(ls unity/Capture/$TV 2>/dev/null | wc -l)" -ge 4634 ] || [ $n -ge 200 ]; do sleep 5; n=$((n+1)); done
ls unity/Capture/$TV | wc -l
unity command editor.playmode stop --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-100
n=0; until unity status --project-path E:/sucai/ikiteita/unity 2>&1 | grep -q "ready" || [ $n -ge 60 ]; do sleep 4; n=$((n+1)); done
PYTHONUTF8=1 python -P tools/film_preview.py $TV $PV 2>&1 | tail -1 | cut -c1-160
bash tools/rough_cut.sh $D $TV $W $H $OUT "$@"
echo round2-done
