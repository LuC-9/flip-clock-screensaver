#!/usr/bin/env bash
set -e

# Change to script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "============================================="
echo " Building FliqloClock macOS Screensaver (.saver)"
echo "============================================="

BUILD_DIR="$SCRIPT_DIR/build"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Build universal binary using xcodebuild
xcodebuild \
  -project FliqloClock.xcodeproj \
  -scheme FliqloClock \
  -configuration Release \
  -destination 'generic/platform=macOS' \
  CONFIGURATION_BUILD_DIR="$BUILD_DIR" \
  clean build

if [ ! -d "$BUILD_DIR/FliqloClock.saver" ]; then
    echo "Error: Build failed - $BUILD_DIR/FliqloClock.saver not found."
    exit 1
fi

echo "Ad-hoc code signing FliqloClock.saver..."
codesign --force --deep --sign - "$BUILD_DIR/FliqloClock.saver"

echo "Packaging release archive..."
cd "$BUILD_DIR"
zip -r -X -y FliqloClock-macOS.saver.zip FliqloClock.saver

echo "============================================="
echo " Build Succeeded!"
echo " Output:"
echo "   Bundle:  $BUILD_DIR/FliqloClock.saver"
echo "   Archive: $BUILD_DIR/FliqloClock-macOS.saver.zip"
echo "============================================="
