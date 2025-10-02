#!/bin/bash

# Build script for Circular MIDI Generator
# Usage: ./build-release.sh [version] [configuration]

set -e

VERSION=${1:-"1.0.0"}
CONFIGURATION=${2:-"Release"}
SKIP_TESTS=${SKIP_TESTS:-false}
SKIP_PACKAGING=${SKIP_PACKAGING:-false}

echo "🎵 Building Circular MIDI Generator v$VERSION"
echo "Configuration: $CONFIGURATION"

# Set up paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_PATH="$(dirname "$SCRIPT_DIR")"
SRC_PATH="$ROOT_PATH/src"
OUTPUT_PATH="$ROOT_PATH/dist"
PROJECT_PATH="$SRC_PATH/CircularMidiGenerator"

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf "$OUTPUT_PATH"
mkdir -p "$OUTPUT_PATH"

# Restore dependencies
echo "📦 Restoring NuGet packages..."
cd "$ROOT_PATH"
dotnet restore

# Run tests (unless skipped)
if [ "$SKIP_TESTS" != "true" ]; then
    echo "🧪 Running tests..."
    dotnet test --configuration "$CONFIGURATION" --no-restore --verbosity minimal
fi

# Build for each platform
declare -a PLATFORMS=(
    "win-x64:Windows"
    "osx-x64:macOS Intel"
    "osx-arm64:macOS Apple Silicon"
    "linux-x64:Linux"
)

for PLATFORM_INFO in "${PLATFORMS[@]}"; do
    IFS=':' read -r RUNTIME PLATFORM_NAME <<< "$PLATFORM_INFO"
    PLATFORM_OUTPUT="$OUTPUT_PATH/$RUNTIME"
    
    echo "🔨 Building for $PLATFORM_NAME ($RUNTIME)..."
    
    dotnet publish "$PROJECT_PATH" \
        --configuration "$CONFIGURATION" \
        --runtime "$RUNTIME" \
        --self-contained true \
        --output "$PLATFORM_OUTPUT" \
        --no-restore \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:Version="$VERSION" \
        -p:AssemblyVersion="$VERSION" \
        -p:FileVersion="$VERSION"
    
    echo "✅ Build completed for $PLATFORM_NAME"
done

# Create packages (unless skipped)
if [ "$SKIP_PACKAGING" != "true" ]; then
    echo "📦 Creating distribution packages..."
    
    # Windows ZIP
    WIN_PATH="$OUTPUT_PATH/win-x64"
    WIN_ZIP="$OUTPUT_PATH/CircularMidiGenerator-$VERSION-Windows.zip"
    cd "$WIN_PATH"
    zip -r "$WIN_ZIP" ./*
    echo "✅ Created Windows package: $(basename "$WIN_ZIP")"
    
    # macOS Intel ZIP
    MAC_INTEL_PATH="$OUTPUT_PATH/osx-x64"
    MAC_INTEL_ZIP="$OUTPUT_PATH/CircularMidiGenerator-$VERSION-macOS-Intel.zip"
    cd "$MAC_INTEL_PATH"
    zip -r "$MAC_INTEL_ZIP" ./*
    echo "✅ Created macOS Intel package: $(basename "$MAC_INTEL_ZIP")"
    
    # macOS Apple Silicon ZIP
    MAC_ARM_PATH="$OUTPUT_PATH/osx-arm64"
    MAC_ARM_ZIP="$OUTPUT_PATH/CircularMidiGenerator-$VERSION-macOS-AppleSilicon.zip"
    cd "$MAC_ARM_PATH"
    zip -r "$MAC_ARM_ZIP" ./*
    echo "✅ Created macOS Apple Silicon package: $(basename "$MAC_ARM_ZIP")"
    
    # Linux TAR.GZ
    LINUX_PATH="$OUTPUT_PATH/linux-x64"
    LINUX_TAR="$OUTPUT_PATH/CircularMidiGenerator-$VERSION-Linux.tar.gz"
    cd "$LINUX_PATH"
    tar -czf "$LINUX_TAR" ./*
    echo "✅ Created Linux package: $(basename "$LINUX_TAR")"
    
    # Create AppImage for Linux (if appimagetool is available)
    if command -v appimagetool &> /dev/null; then
        echo "📱 Creating AppImage..."
        APPIMAGE_DIR="$OUTPUT_PATH/AppImage"
        mkdir -p "$APPIMAGE_DIR"
        
        # Copy application files
        cp -r "$LINUX_PATH"/* "$APPIMAGE_DIR/"
        
        # Create desktop file
        cat > "$APPIMAGE_DIR/CircularMidiGenerator.desktop" << EOF
[Desktop Entry]
Name=Circular MIDI Generator
Exec=CircularMidiGenerator
Icon=circular-midi-generator
Type=Application
Categories=Audio;Music;
EOF
        
        # Create AppRun script
        cat > "$APPIMAGE_DIR/AppRun" << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}:${PATH}"
exec "${HERE}/CircularMidiGenerator" "$@"
EOF
        chmod +x "$APPIMAGE_DIR/AppRun"
        
        # Build AppImage
        APPIMAGE_OUTPUT="$OUTPUT_PATH/CircularMidiGenerator-$VERSION-Linux.AppImage"
        appimagetool "$APPIMAGE_DIR" "$APPIMAGE_OUTPUT"
        echo "✅ Created AppImage: $(basename "$APPIMAGE_OUTPUT")"
    fi
fi

# Generate checksums
echo "🔐 Generating checksums..."
CHECKSUM_FILE="$OUTPUT_PATH/checksums.txt"
cd "$OUTPUT_PATH"

for file in *.zip *.tar.gz *.AppImage; do
    if [ -f "$file" ]; then
        if command -v sha256sum &> /dev/null; then
            sha256sum "$file" >> "$CHECKSUM_FILE"
        elif command -v shasum &> /dev/null; then
            shasum -a 256 "$file" >> "$CHECKSUM_FILE"
        fi
    fi
done

# Build summary
echo ""
echo "🎉 Build Summary:"
echo "Version: $VERSION"
echo "Configuration: $CONFIGURATION"
echo "Output Directory: $OUTPUT_PATH"
echo ""
echo "📦 Packages created:"

for file in "$OUTPUT_PATH"/*.zip "$OUTPUT_PATH"/*.tar.gz "$OUTPUT_PATH"/*.AppImage; do
    if [ -f "$file" ]; then
        SIZE=$(du -h "$file" | cut -f1)
        echo "  $(basename "$file") ($SIZE)"
    fi
done

echo ""
echo "✅ Build completed successfully!"
echo "📁 Packages are ready in: $OUTPUT_PATH"