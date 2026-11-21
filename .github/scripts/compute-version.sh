#!/usr/bin/env bash
# Prints the next version tag to push (publish.yml publishes it), from the git tags:
#   release: last release tag with the build number incremented
#   preview: the next release version plus a counter of previews already cut for it
# Fetch tags first (git fetch --tags) so the count includes every published one.
set -euo pipefail

channel="$1"

latest=$(git tag -l | grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' | sort -V | tail -n 1 || true)
if [ -z "$latest" ]; then
  echo "No X.Y.Z release tag found" >&2
  exit 1
fi

IFS=. read -r major minor build <<< "$latest"
next="$major.$minor.$((build + 1))"

case "$channel" in
  release)
    echo "$next"
    ;;
  preview)
    count=$(git tag -l "$next-preview-*" | wc -l)
    echo "$next-preview-$((count + 1))"
    ;;
  *)
    echo "Unknown channel: $channel" >&2
    exit 1
    ;;
esac
