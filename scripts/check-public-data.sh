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

if git log --all --format='%B' |
  PUBLIC_PATTERN="$pattern" perl -ne '
    BEGIN { $match = qr/$ENV{PUBLIC_PATTERN}/; $found = 0 }
    $found = 1 if $_ =~ $match;
    END { exit($found ? 0 : 1) }
  '
then
  echo "Potential private data found in Git commit messages; matching content is intentionally suppressed." >&2
  exit 1
fi

# Commit identities are public metadata but are outside git-grep and ordinary
# secret scanners. Existing public history may contain legitimate contributor
# identities, so this check is scoped to commits introduced by the PR. The
# content/path scan above intentionally remains reachable-history wide.
identity_ref='refs/remotes/origin/main'
identity_base=''
if ! git show-ref --verify --quiet "$identity_ref" 2>/dev/null ||
  ! identity_base="$(git rev-parse --verify --quiet "$identity_ref^{commit}" 2>/dev/null)" ||
  [[ -z "$identity_base" ]] ||
  ! identity_base="$(git merge-base HEAD "$identity_base" 2>/dev/null)" ||
  [[ -z "$identity_base" ]]
then
  echo "Unable to establish a safe public identity baseline; refusing to continue." >&2
  exit 1
fi
if git log "$identity_base..HEAD" --format='%ae%n%ce' |
  perl -ne '
    BEGIN { $invalid = 0 }
    chomp;
    $invalid = 1 unless /^([^@\s]+\@users\.noreply\.github\.com|noreply\@github\.com)$/;
    END { exit($invalid ? 0 : 1) }
  '
then
  echo "Potential personal e-mail found in reachable Git identity metadata; matching content is intentionally suppressed." >&2
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
