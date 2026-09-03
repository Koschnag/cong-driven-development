#!/usr/bin/env bash
set -euo pipefail

# Build a bounded local static copy. This script never publishes or enables Pages.
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ $# -ne 1 ]]; then
  printf 'usage: %s OUTPUT_DIR\n' "$0" >&2
  exit 2
fi
if [[ ! -d "$1" ]]; then
  printf 'output must be an existing directory\n' >&2
  exit 2
fi
output="$(cd "$1" && pwd -P)"
case "$output" in
  "$repo_root"|"$repo_root"/*)
    printf 'output must be outside the repository\n' >&2
    exit 2
    ;;
esac
if [[ -e "$output" && -n "$(find "$output" -mindepth 1 -print -quit 2>/dev/null)" ]]; then
  printf 'output must be empty\n' >&2
  exit 2
fi
# Public-data policy is checked before copying the static projection.
bash "$repo_root/scripts/check-public-data.sh"
cp -R "$repo_root/docs/." "$output/"
printf 'built static public docs at %s\n' "$output"
