#!/bin/bash
# usage: run_player.sh <label> <gc> [rounds] [extra player args...]   (e.g. -researchPresettle)
B="$TEMP/ProjectL_ResearchBuild/ProjectL_Research.exe"
O="$TEMP/ProjectL_ResearchRun/$1"
rm -rf "$O"; mkdir -p "$O"
OW=$(cygpath -w "$O")
"$B" -researchAutoPlay -researchNoTutorial -researchOut "$OW" -researchGc "$2" -researchRounds "${3:-1}" "${@:4}" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile "$OW\player.log" &
powershell -NoProfile -ExecutionPolicy Bypass -File "$(dirname "$0")/front_player.ps1" -Done "$OW\done.txt" > "$O/front.txt" 2>&1
sleep 4
powershell -NoProfile -Command "Get-Process ProjectL_Research -ErrorAction SilentlyContinue | Stop-Process -Force" > /dev/null 2>&1
echo "## $1 ($2) $(cat "$O/front.txt")"
if [ -f "$O/done.txt" ]; then cat "$O/done.txt"; else echo "no done.txt"; cat "$O/lines.txt" 2>/dev/null; fi
