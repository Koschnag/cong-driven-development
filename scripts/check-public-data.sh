#!/usr/bin/env bash
set -euo pipefail

# Deliberately narrow deterministic guard. It complements gitleaks and human review;
# it does not claim to detect every kind of personal data.
pattern='(/home/|/Users/|drive\.cong42\.de|cockpit\.cong42\.de|[Vv][Mm][[:space:]-]?12[01]|tailscale0|100\.64\.[0-9]+\.[0-9]+|192\.168\.[0-9]+\.[0-9]+|10\.[0-9]+\.[0-9]+\.[0-9]+|172\.(1[6-9]|2[0-9]|3[01])\.[0-9]+\.[0-9]+)'

if git grep -nE "$pattern" -- \
  ':(exclude)scripts/check-public-data.sh' \
  ':(exclude)PUBLICATION_POLICY.md'
then
  echo "Potential private path, route, or address found in tracked public files." >&2
  exit 1
fi

echo "Public-data deterministic pattern check passed."
