#!/bin/bash
set -euo pipefail

cd "$CLAUDE_PROJECT_DIR"

SLUG=$(DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet run --project src/Slugger.Cli -- \
  --theme-dir themes --theme '*' --oneshot 2>/dev/null | tail -n 1)

if [ -z "$SLUG" ]; then
  exit 0
fi

echo "Convention de nommage de branche (CLAUDE.md) : le nom recommande pour cette session est claude/$SLUG. Propose a l'utilisateur de basculer dessus (git switch -c claude/$SLUG) avant de commencer le travail, sauf si une branche a deja ete explicitement assignee pour cette tache."
