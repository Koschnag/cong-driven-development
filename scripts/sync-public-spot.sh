#!/usr/bin/env bash
set -euo pipefail

public_spot_script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
public_spot_root=$(dirname -- "$public_spot_script_dir")
public_spot_mode=${1:-write}

if [[ "$public_spot_mode" != "write" && "$public_spot_mode" != "--check" ]]; then
  printf 'Aufruf: %s [--check]\n' "$0" >&2
  exit 2
fi

for public_spot_tool in dotnet jq cmp; do
  if ! command -v "$public_spot_tool" >/dev/null 2>&1; then
    printf 'Fehlendes Werkzeug: %s\n' "$public_spot_tool" >&2
    exit 2
  fi
done

public_spot_tmp=$(mktemp -d /tmp/cdd-public-spot.XXXXXX)
trap 'rm -rf -- "$public_spot_tmp"' EXIT

cd "$public_spot_root"
jq -s . .spot/*.json > "$public_spot_tmp/spot.json"
jq '{
  Aligned: [.[] | select(.Convergence == "Aligned")],
  Pending: [.[] | select(.Convergence == "Pending")],
  Diverged: [.[] | select(.Convergence == "Diverged")],
  Orphaned: [.[] | select(.Convergence == "Orphaned")]
}' "$public_spot_tmp/spot.json" > "$public_spot_tmp/diff.json"

dotnet run -c Release --no-build --project src/Cdd.Cli -- \
  export-context --out "$public_spot_tmp/export.md" >/dev/null

public_spot_target="$public_spot_root/docs/ide/_demo"
public_spot_files=(spot.json diff.json export.md)

if [[ "$public_spot_mode" == "--check" ]]; then
  public_spot_stale=0
  for public_spot_file in "${public_spot_files[@]}"; do
    if ! cmp -s -- "$public_spot_tmp/$public_spot_file" "$public_spot_target/$public_spot_file"; then
      printf 'Veraltete öffentliche SPOT-Projektion: %s\n' "$public_spot_file" >&2
      public_spot_stale=1
    fi
  done
  if (( public_spot_stale != 0 )); then
    printf 'scripts/sync-public-spot.sh ausführen und Ergebnisse committen.\n' >&2
    exit 1
  fi
  printf 'Oeffentliche SPOT-Projektion ist aktuell.\n'
else
  for public_spot_file in "${public_spot_files[@]}"; do
    cp -- "$public_spot_tmp/$public_spot_file" "$public_spot_target/$public_spot_file"
  done
  printf 'Oeffentliche SPOT-Projektion aktualisiert (%s Knoten).\n' \
    "$(jq 'length' "$public_spot_tmp/spot.json")"
fi
