#!/usr/bin/env bash
# Compiles Reddit.Api (through native/RedditApi.Native) into libredditapi.so for one Android ABI.
# Runs on Linux: in GitHub Actions directly, and locally inside the builder VM (build-android.sh).
#
# Usage: native/publish-android.sh <arm64-v8a|x86_64> <output directory>
# Needs: the .NET 10 SDK on PATH, and ANDROID_NDK_ROOT pointing at an NDK (r28c is what's used).
set -euo pipefail

abi=$1
out=$2

case $abi in
    arm64-v8a) rid=linux-bionic-arm64 ;;
    x86_64) rid=linux-bionic-x64 ;;
    *) echo "Unknown ABI '$abi' (arm64-v8a or x86_64)" >&2; exit 1 ;;
esac

: "${ANDROID_NDK_ROOT:?ANDROID_NDK_ROOT must point at an Android NDK}"
TOOLCHAIN=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64
export PATH=$TOOLCHAIN/bin:$PATH

cd "$(dirname "$0")/RedditApi.Native"

# NativeAOT for Android (bionic) is experimental in .NET: these switch it on and point it at the
# NDK's clang, lld and sysroot.
dotnet publish -c Release -r "$rid" \
    -p:DisableUnsupportedError=true \
    -p:PublishAotUsingRuntimePack=true \
    -p:CppCompilerAndLinker=clang \
    -p:ObjCopyName=llvm-objcopy \
    -p:SysRoot="$TOOLCHAIN/sysroot" \
    -p:LinkerFlavor=lld \
    -o "$out"

ls -la "$out/libredditapi.so"
