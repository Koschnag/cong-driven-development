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

expect_baseline_failure() {
  local output
  output="$(mktemp)"
  if bash scripts/check-public-data.sh >"$output" 2>&1; then
    echo "unsafe identity baseline unexpectedly passed" >&2
    return 1
  fi
  if grep -Eq '(@|refs/|[0-9a-f]{7,})' "$output"; then
    echo "baseline diagnostic leaked repository details" >&2
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

# Missing, non-commit, and unrelated remote baselines fail closed.
git update-ref -d refs/remotes/origin/main
expect_baseline_failure
git update-ref refs/remotes/origin/main "$(git rev-parse HEAD^{tree})"
expect_baseline_failure
unrelated="$(printf 'unrelated\n' | git commit-tree "$(git rev-parse HEAD^{tree})")"
git update-ref refs/remotes/origin/main "$unrelated"
expect_baseline_failure

# A shorthand-shadowing branch must not affect the exact remote-tracking ref.
git update-ref refs/remotes/origin/main "$base"
git update-ref refs/heads/origin/main "$unrelated"
bash scripts/check-public-data.sh >/dev/null

echo "check-public-data identity-range tests passed"
