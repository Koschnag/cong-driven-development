#!/usr/bin/env bash
set -euo pipefail

# Deliberately narrow deterministic guard. It complements gitleaks and human review;
# it does not claim to detect every kind of personal data. Generic example paths
# are allowed, but machine-specific home paths are not.
pattern='(/home/(?!user/)|/Users/(?!user/)|drive\.cong42\.de|cockpit\.cong42\.de|cdd\.cong42\.de|[Vv][Mm][[:space:]-]?12[01]|tailscale0|100\.64\.[0-9]+\.[0-9]+|192\.168\.[0-9]+\.[0-9]+|10\.[0-9]+\.[0-9]+\.[0-9]+|172\.(1[6-9]|2[0-9]|3[01])\.[0-9]+\.[0-9]+)'
excluded_paths=(
  ':(exclude)scripts/check-public-data.sh'
  ':(exclude)PUBLICATION_POLICY.md'
)

if git grep -I -qP "$pattern" -- . "${excluded_paths[@]}"
then
  echo "Potential private data found in tracked public files; matching content is intentionally suppressed." >&2
  exit 1
fi

mapfile -t commits < <(git rev-list --all)
if ((${#commits[@]} > 0)) &&
  git grep -I -qP "$pattern" "${commits[@]}" -- . "${excluded_paths[@]}"
then
  echo "Potential private data found in reachable Git history; matching content is intentionally suppressed." >&2
  exit 1
fi

if git log --all --format='%B' | grep -qP "$pattern"
then
  echo "Potential private data found in Git commit messages; matching content is intentionally suppressed." >&2
  exit 1
fi

removed_paths=(
  docs/redesign.md
  docs/SESSION-HANDOFF.md
  docs/whitepaper-konvergenz.pdf
)
for path in "${removed_paths[@]}"
do
  if git log --all --format='%H' -- "$path" | grep -q .
  then
    echo "A prohibited historical publication artifact is still reachable." >&2
    exit 1
  fi
done

echo "Public-data current-tree and reachable-history checks passed."
