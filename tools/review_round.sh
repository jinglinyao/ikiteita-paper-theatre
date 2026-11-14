#!/bin/bash
# one review round: the close-up sequences, then the whole film from the television camera into Capture/<dir>, then the preview
#   tools/review_round.sh <dir> <preview name>
cd /e/sucai/ikiteita || exit 1
bash tools/review_seqs.sh | tail -3
rm -rf unity/Capture/$1
R=$(bash tools/sets.sh SetupFilmBaker "[0,4634,1,960,720,\"$1\"]" 120 2>&1 | grep -iE "error|ready" | head -3 | cut -c1-200); echo "$R"
case "$R" in *"film baker ready"*) ;; *) exit 1;; esac
unity command editor.playmode start --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-120
n=0; until [ "$(ls unity/Capture/$1 2>/dev/null | wc -l)" -ge 4634 ] || [ $n -ge 160 ]; do sleep 5; n=$((n+1)); done
ls unity/Capture/$1 | wc -l
unity command editor.playmode stop --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-100
PYTHONUTF8=1 python -P tools/film_preview.py $1 $2 2>&1 | tail -1 | cut -c1-160
echo round-done
