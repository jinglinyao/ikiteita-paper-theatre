#!/bin/bash
# stage shots in play mode into unity/Capture/<dir>:  tools/play_shots.sh "<spec>" <dir> <w> <h> [step] [handle]
cd /e/sucai/ikiteita || exit 1
SPEC=$1; D=$2; W=$3; H=$4; STEP=${5:-1}; HANDLE=${6:-8}
R=$(bash tools/sets.sh SetupShotBaker "[\"$SPEC\",$HANDLE,$STEP,$W,$H,\"$D\"]" 120 2>&1 | grep -iE "error|ready" | head -3 | cut -c1-200); echo "$R"
case "$R" in *"shot baker ready"*) ;; *) exit 1;; esac
unity command editor.playmode start --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-120
n=0; last=-1; same=0
until [ $n -ge 400 ]; do
  sleep 6; n=$((n+1)); c=$(find unity/Capture/$D -name "*.jpg" 2>/dev/null | wc -l)
  if [ "$c" = "$last" ] && [ "$c" -gt 0 ]; then same=$((same+1)); else same=0; fi; last=$c
  if [ $same -ge 3 ]; then break; fi
  if [ "$c" = "0" ] && [ $n -ge 15 ]; then break; fi
done
echo "frames $last"
unity command editor.playmode stop --project-path E:/sucai/ikiteita/unity 2>&1 | tail -1 | cut -c1-100
n=0; until unity status --project-path E:/sucai/ikiteita/unity 2>&1 | grep -q "ready" || [ $n -ge 60 ]; do sleep 4; n=$((n+1)); done
