#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR="$SCRIPT_DIR/.build"
DIST_DIR="$SCRIPT_DIR/dist"
APP_NAME="StandUpBuddy"
EXECUTABLE_NAME="StandUpBuddy"
APP_DIR="$DIST_DIR/$APP_NAME.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
SIGN_IDENTITY="${SIGN_IDENTITY:--}"

if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "This script must run on macOS 13 or later." >&2
    exit 1
fi

for tool in xcrun swiftc lipo sips iconutil codesign; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "Missing build tool: $tool. Install Xcode Command Line Tools first." >&2
        exit 1
    fi
done

SDK_PATH="$(xcrun --sdk macosx --show-sdk-path)"
read -r -a ARCH_LIST <<< "${ARCHS:-arm64 x86_64}"

rm -rf "$BUILD_DIR" "$APP_DIR"
mkdir -p "$BUILD_DIR" "$MACOS_DIR" "$RESOURCES_DIR/Characters" "$RESOURCES_DIR/Audio"

SWIFT_SOURCES=("$SCRIPT_DIR"/Sources/*.swift)
BINARY_PATHS=()
for arch in "${ARCH_LIST[@]}"; do
    case "$arch" in
        arm64|x86_64) ;;
        *)
            echo "Unsupported architecture: $arch (only arm64 and x86_64 are supported)." >&2
            exit 1
            ;;
    esac

    output="$BUILD_DIR/$EXECUTABLE_NAME-$arch"
    echo "Building $arch..."
    xcrun swiftc \
        -O \
        -whole-module-optimization \
        -target "$arch-apple-macos13.0" \
        -sdk "$SDK_PATH" \
        -framework AppKit \
        -framework QuartzCore \
        -framework ServiceManagement \
        "${SWIFT_SOURCES[@]}" \
        -o "$output"
    BINARY_PATHS+=("$output")
done

if [[ "${#BINARY_PATHS[@]}" -eq 1 ]]; then
    cp "${BINARY_PATHS[0]}" "$MACOS_DIR/$EXECUTABLE_NAME"
else
    lipo -create "${BINARY_PATHS[@]}" -output "$MACOS_DIR/$EXECUTABLE_NAME"
fi
chmod +x "$MACOS_DIR/$EXECUTABLE_NAME"

cp "$SCRIPT_DIR/Info.plist" "$CONTENTS_DIR/Info.plist"

for file in cat.png corgi.png red-panda.png mech.png web-ranger.png; do
    cp "$ROOT_DIR/assets/characters/$file" "$RESOURCES_DIR/Characters/$file"
done

for file in cat.wav corgi.wav red-panda.wav mech.wav web-ranger.wav; do
    cp "$ROOT_DIR/assets/audio/$file" "$RESOURCES_DIR/Audio/$file"
done
cp "$ROOT_DIR/assets/audio/README.md" "$RESOURCES_DIR/THIRD-PARTY-NOTICES.md"

ICON_SOURCE="$ROOT_DIR/assets/app/StandUpBuddy-icon.png"
ICONSET_DIR="$BUILD_DIR/AppIcon.iconset"
mkdir -p "$ICONSET_DIR"
sips -z 16 16 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_16x16.png" >/dev/null
sips -z 32 32 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_16x16@2x.png" >/dev/null
sips -z 32 32 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_32x32.png" >/dev/null
sips -z 64 64 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_32x32@2x.png" >/dev/null
sips -z 128 128 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_128x128.png" >/dev/null
sips -z 256 256 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_128x128@2x.png" >/dev/null
sips -z 256 256 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_256x256.png" >/dev/null
sips -z 512 512 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_256x256@2x.png" >/dev/null
sips -z 512 512 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_512x512.png" >/dev/null
sips -z 1024 1024 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_512x512@2x.png" >/dev/null
iconutil -c icns "$ICONSET_DIR" -o "$RESOURCES_DIR/AppIcon.icns"

if [[ "$SIGN_IDENTITY" == "-" ]]; then
    codesign --force --sign - "$APP_DIR"
    echo "Signed with a local ad-hoc identity."
else
    codesign --force --options runtime --timestamp --sign "$SIGN_IDENTITY" "$APP_DIR"
    echo "Signed with Developer ID: $SIGN_IDENTITY"
fi

codesign --verify --deep --strict --verbose=2 "$APP_DIR"
echo
echo "Build complete: $APP_DIR"
if [[ "${#ARCH_LIST[@]}" -gt 1 ]]; then
    lipo -archs "$MACOS_DIR/$EXECUTABLE_NAME"
fi
