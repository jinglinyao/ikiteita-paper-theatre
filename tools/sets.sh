#!/bin/bash
# assemble AgentScripts/src/*.cs into AgentScripts/Sets.cs and run one entry point:  tools/sets.sh Entry '[args]' [timeout]
cd /e/sucai/ikiteita/unity || exit 1
cat AgentScripts/src/*.cs > AgentScripts/Sets.cs
unity command script run --file AgentScripts/Sets.cs --entry "Sets.$1" --args "${2:-[]}" --project-path E:/sucai/ikiteita/unity --timeout "${3:-300}" 2>&1 | grep -vE "^(command|parameters|target|assemblyName|compileMs|executeMs|diagnostics: \[\])" 
