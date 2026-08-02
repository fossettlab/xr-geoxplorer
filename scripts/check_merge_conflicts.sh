#!/usr/bin/env bash
# Dry-run merge conflict check for open agent PR branches.
#
# Usage:
#   ./scripts/check_merge_conflicts.sh
#   ./scripts/check_merge_conflicts.sh origin/main cursor/hololens-deprecation-50b2
#
# Exit 0 when merge-tree reports no conflicts; exit 1 when conflicts found.
# See docs/pr-merge-guide.md for resolution hints.

set -euo pipefail

BASE="${1:-origin/main}"
BRANCH="${2:-cursor/cloud-tickets-networking-auth-50b2}"

git fetch origin "$BASE" "$BRANCH" 2>/dev/null || git fetch origin

BASE_SHA="$(git rev-parse "$BASE")"
BRANCH_SHA="$(git rev-parse "$BRANCH")"
MERGE_BASE="$(git merge-base "$BASE_SHA" "$BRANCH_SHA")"

echo "== Merge conflict dry-run =="
echo "Base:   $BASE ($BASE_SHA)"
echo "Branch: $BRANCH ($BRANCH_SHA)"
echo "Merge-base: $(git log -1 --oneline "$MERGE_BASE")"
echo

OUTPUT="$(git merge-tree "$MERGE_BASE" "$BASE_SHA" "$BRANCH_SHA")"

if echo "$OUTPUT" | rg -q '^<<<<<<<|^CONFLICT'; then
  echo "CONFLICTS DETECTED"
  echo "$OUTPUT" | rg -n 'changed in both|CONFLICT|<<<<<<<' | head -30
  exit 1
fi

# merge-tree marks conflicts with "changed in both" on overlapping hunks
if echo "$OUTPUT" | rg -q 'changed in both'; then
  echo "POSSIBLE CONFLICTS (changed in both):"
  echo "$OUTPUT" | rg -n 'changed in both' | head -20
  exit 1
fi

echo "OK — no conflicts detected between $BASE and $BRANCH"
exit 0
