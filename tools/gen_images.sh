#!/bin/bash
cd /e/sucai/ikiteita
for f in gen/image/*.txt; do id=$(basename $f .txt); [ -f gen/image/$id.png ] && continue; case $id in s*) sz="1536*1024";; *) sz="1024*1280";; esac; PYTHONUTF8=1 python -P tools/gen_ali.py image $id $f "$sz" 2>&1 | tail -1 | cut -c1-300; done; ls gen/image/*.png | wc -l; echo images-done
