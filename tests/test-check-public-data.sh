#!/usr/bin/env bash
set -euo pipefail

repo="$(mktemp -d "${TMPDIR:-/tmp}/check-public-data.XXXXXX")"
trap 'rm -rf "$repo"' EXIT
mkdir -p "$repo/scripts"
cp scripts/check-public-data.sh "$repo/scripts/"
chmod +x "$repo/scripts/check-public-data.sh"
cd "$repo"
git init -q
git config user.name "Public Test"
git config user.email "12345+public-test@users.noreply.github.com"
git add scripts/check-public-data.sh
# A historical contributor address is deliberately outside the new PR range.
GIT_AUTHOR_EMAIL=legacy@example.invalid GIT_COMMITTER_EMAIL=legacy@example.invalid \
  git commit -qm "base"
base="$(git rev-parse HEAD)"
git branch -M main
git update-ref refs/remotes/origin/main "$base"

run_case() {
  local bad_index="$1" count=121 i output
  git checkout -q -B candidate "$base"
  for ((i=0; i<count; i++)); do
    if ((i == bad_index)); then
      GIT_AUTHOR_EMAIL=private@example.invalid GIT_COMMITTER_EMAIL=private@example.invalid \
        git commit --allow-empty -qm "candidate $i"
    else
      git commit --allow-empty -qm "candidate $i"
    fi
  done
  output="$(mktemp)"
  if bash scripts/check-public-data.sh >"$output" 2>&1; then
    echo "identity case unexpectedly passed: $bad_index" >&2
    return 1
  fi
  if grep -Fq 'private@example.invalid' "$output"; then
    echo "rejected identity leaked in diagnostic" >&2
    return 1
  fi
  rm -f "$output"
}

# Exercise newest, middle, and oldest entries in a long identity stream.
run_case 120
run_case 60
run_case 0

git checkout -q -B candidate "$base"
for ((i=0; i<121; i++)); do
  git commit --allow-empty -qm "allowed candidate $i"
done
bash scripts/check-public-data.sh >/dev/null
echo "check-public-data identity-range tests passed"
