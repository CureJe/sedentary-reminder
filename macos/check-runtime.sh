#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
APP_DIR="$SCRIPT_DIR/.build/RuntimeChecks.app"
RESULTS="$SCRIPT_DIR/runtime-results"

if [[ "$(uname -s)" != Darwin ]]; then
    echo 'Runtime checks require a macOS GUI session.' >&2
    exit 1
fi
if [[ ! -d "$SCRIPT_DIR/dist/StandUpBuddy.app" ]]; then
    echo 'Run build-macos.sh before runtime checks.' >&2
    exit 1
fi
# A separate bundle identity and unique preferences suite isolate all test state.
mkdir -p "$APP_DIR/Contents/MacOS" "$RESULTS"
cp -R "$SCRIPT_DIR/dist/StandUpBuddy.app/Contents/Resources" "$APP_DIR/Contents/"
cp "$SCRIPT_DIR/Info.plist" "$APP_DIR/Contents/Info.plist"
/usr/libexec/PlistBuddy -c 'Set CFBundleIdentifier com.standupbuddy.runtimechecks' "$APP_DIR/Contents/Info.plist"
/usr/libexec/PlistBuddy -c 'Set CFBundleExecutable RuntimeChecks' "$APP_DIR/Contents/Info.plist"
xcrun swiftc -D RUNTIME_CHECKS -O -target "$(uname -m)-apple-macos13.0" \
    -sdk "$(xcrun --sdk macosx --show-sdk-path)" \
    -framework AppKit -framework QuartzCore -framework ServiceManagement \
    "$SCRIPT_DIR"/Sources/*.swift "$SCRIPT_DIR/Tests/RuntimeChecks.swift" \
    -o "$APP_DIR/Contents/MacOS/RuntimeChecks"
codesign --force --sign - "$APP_DIR"
codesign --verify --deep --strict "$APP_DIR"
export SOURCE_COMMIT
SOURCE_COMMIT="$(git -C "$ROOT_DIR" rev-parse HEAD)"
"$APP_DIR/Contents/MacOS/RuntimeChecks" "$RESULTS" 2>&1 | tee "$RESULTS/runtime.log"
