#!/usr/bin/env bash
# Compiles Reddit.Api (through native/RedditApi.Native) into libredditapi.so for Android, for ARM64
# (phones, the RP6) and x86_64 (the development emulator), and puts them in the app's jniLibs.
# .NET can only do this on Linux, so locally it runs native/publish-android.sh in the builder VM
# (G:\VMs\deaddit-linux-builder, see its README) over SSH. GitHub Actions runs that script directly.
#
# Usage (from the repo root, VM running): bash native/build-android.sh [arm64-v8a] [x86_64]
# With no ABIs named, builds both.
set -euo pipefail

VM_DIR=/g/VMs/deaddit-linux-builder
SSH=(ssh -i "$VM_DIR/builder_key" -p 2222 -o UserKnownHostsFile="$VM_DIR/known_hosts" -o BatchMode=yes builder@127.0.0.1)
REPO=$(cd "$(dirname "$0")/.." && pwd)
JNILIBS="$REPO/app/src/main/jniLibs"

echo "Copying sources to the VM"
# Synced rather than replaced, so each build keeps the last one's obj/ (restore is slow in the VM).
# Only files whose content changed are written, and they get the current time rather than their
# Windows edit time: MSBuild decides what to rebuild by timestamps, and an edit made before the
# last build finished would otherwise look older than its output and be skipped.
"${SSH[@]}" 'rm -rf ~/src-incoming && mkdir -p ~/src-incoming ~/src'
tar -C "$REPO" -cf - --exclude=bin --exclude=obj native/RedditApi.Native native/publish-android.sh Reddit.Api/Reddit.Api \
    | "${SSH[@]}" 'tar -C ~/src-incoming -xf - && rsync -rlp --checksum --delete --exclude=bin/ --exclude=obj/ ~/src-incoming/ ~/src/'

ABIS=("$@")
if [ ${#ABIS[@]} -eq 0 ]; then ABIS=(arm64-v8a x86_64); fi

for abi in "${ABIS[@]}"; do
    echo "Compiling for $abi"
    "${SSH[@]}" "bash -lc 'bash ~/src/native/publish-android.sh $abi ~/out/$abi'"
    mkdir -p "$JNILIBS/$abi"
    "${SSH[@]}" "cat ~/out/$abi/libredditapi.so" > "$JNILIBS/$abi/libredditapi.so"
    ls -la "$JNILIBS/$abi/libredditapi.so"
done
