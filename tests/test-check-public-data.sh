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
git config gc.auto 0
git add scripts/check-public-data.sh
# A historical contributor address is deliberately outside the new PR range.
GIT_AUTHOR_EMAIL=legacy@example.invalid GIT_COMMITTER_EMAIL=legacy@example.invalid \
  git commit -qm "base"
base="$(git rev-parse HEAD)"
git branch -M main
git update-ref refs/remotes/origin/main "$base"

run_case() {
  local bad_index="$1" count=121 i output
  git checkout -q -f -B candidate "$base"
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

run_path_case() {
  local path="$1" bad_index="$2" count=121 i output
  git checkout -q -f -B candidate "$base"
  for ((i=0; i<count; i++)); do
    if ((i == bad_index)); then
      mkdir -p "$(dirname "$path")"
      printf 'historical fixture\n' >"$path"
      git add "$path"
      git commit -qm "historical path $i"
      git rm -q "$path"
      git commit --allow-empty -qm "remove historical path $i"
    else
      git commit --allow-empty -qm "path stream $i"
    fi
  done
  output="$(mktemp)"
  if bash scripts/check-public-data.sh >"$output" 2>&1; then
    echo "historical path case unexpectedly passed: $path/$bad_index" >&2
    return 1
  fi
  if grep -Eq 'docs/(redesign\.md|SESSION-HANDOFF\.md|whitepaper-konvergenz\.pdf)' "$output"; then
    echo "prohibited path leaked in diagnostic" >&2
    return 1
  fi
  rm -f "$output"
}

run_long_path_case() {
  local path="$1" count=2401 i output old_status parent tree_a tree_b tree_without history_count
  git reset -q --hard "$base"
  git checkout -q -f -B candidate "$base"
  mkdir -p "$(dirname "$path")"
  printf 'long historical fixture A\n' >"$path"
  git add "$path"
  tree_a="$(git write-tree)"
  printf 'long historical fixture B\n' >"$path"
  git add "$path"
  tree_b="$(git write-tree)"
  git reset -q HEAD -- "$path"
  rm -f "$path"
  tree_without="$(git write-tree)"
  # Alternate two blobs so every commit is a real path change. commit-tree
  # creates the long history without spending minutes rewriting the index.
  parent="$base"
  for ((i=0; i<count; i++)); do
    if ((i % 2 == 0)); then
      parent="$(printf 'long historical path A %s\n' "$i" | git commit-tree "$tree_a" -p "$parent")"
    else
      parent="$(printf 'long historical path B %s\n' "$i" | git commit-tree "$tree_b" -p "$parent")"
    fi
  done
  parent="$(printf 'remove long historical path\n' | git commit-tree "$tree_without" -p "$parent")"
  git update-ref refs/heads/candidate "$parent"
  git reset -q --hard "$parent"

  history_count="$(git log --all --format='%H' -- "$path" | wc -l | tr -d ' ')"
  if [[ "$history_count" != "2402" ]]; then
    echo "long historical path count mismatch: $path" >&2
    return 1
  fi

  # The former short-circuiting pipeline must demonstrably hit SIGPIPE once
  # the genuine path history exceeds the pipe buffer.
  set +e
  git log --all --format='%H' -- "$path" | grep -q .
  old_status=$?
  set -e
  if ((old_status != 141)); then
    echo "old historical-path pipeline did not reproduce SIGPIPE: $path" >&2
    return 1
  fi

  output="$(mktemp)"
  if bash scripts/check-public-data.sh >"$output" 2>&1; then
    echo "long historical path unexpectedly passed: $path" >&2
    return 1
  fi
  if grep -Eq 'docs/(redesign\.md|SESSION-HANDOFF\.md|whitepaper-konvergenz\.pdf)' "$output"; then
    echo "prohibited path leaked in diagnostic" >&2
    return 1
  fi
  rm -f "$output"
}

# Exercise newest, middle, and oldest entries in a long identity stream.
run_case 120
run_case 60
run_case 0

git checkout -q -f -B candidate "$base"
for ((i=0; i<121; i++)); do
  git commit --allow-empty -qm "allowed candidate $i"
done
bash scripts/check-public-data.sh >/dev/null

# Exercise each prohibited path at the newest, middle, and oldest positions
# in a long reachable-history stream.
for path in docs/redesign.md docs/SESSION-HANDOFF.md docs/whitepaper-konvergenz.pdf; do
  run_path_case "$path" 120
  run_path_case "$path" 60
  run_path_case "$path" 0
done

# Each path also gets a genuinely large history, not merely one add/remove
# pair, so the old grep consumer really can terminate git with SIGPIPE.
for path in docs/redesign.md docs/SESSION-HANDOFF.md docs/whitepaper-konvergenz.pdf; do
  run_long_path_case "$path"
done

git checkout -q -f -B candidate "$base"
for ((i=0; i<121; i++)); do
  git commit --allow-empty -qm "clean path candidate $i"
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
