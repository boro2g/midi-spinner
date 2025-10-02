bpm setting

size of marker is note length
- hold down right click on marker and drag up/down (velocity)
- show number, marker transparency is velocity
- hold down right click on marker and drag left/right (note length)
- show number, marker size is note length

play buttons

ability to configure which root note each lane pushes out
## Recent Fixes

- ✅ **Marker placement during rotation**: FIXED - Markers now appear exactly where clicked, even when disk is rotating. The problem was coordinate space confusion between click coordinates and rendered coordinates. Fixed by:
  1. Removing `+ DiskRotation` from `CalculateMarkerPosition` (graphics transform handles rotation)
  2. Implementing proper inverse transform in `CalculateAngleAccountingForRotation` to convert click position back to unrotated coordinate space
  3. Verified working for all scenarios: stationary disk, spinning disk, and stopped-after-rotation disk

- ✅ **Right-click functionality**: RESTORED - Right-click drag on markers to adjust velocity (vertical) and note length (horizontal)

- ✅ **Double-click to remove**: ADDED - Double-click any marker to remove it (500ms threshold)

- ✅ **Right-click text rotation**: FIXED - Velocity and note length text indicators no longer rotate with the disk. Moved drawing outside the rotation transform so text stays upright. Confirmed working!

- 🔧 **Remove "12" text**: INVESTIGATING - The "12" text at the top is not found in any XAML or C# code. May be from system overlay, debugging tools, or external library. Need to test in release build or check if it's interactive.

- ✅ **Play/Stop button consolidation**: IMPROVED - Replaced separate Play and Stop buttons with a single toggle button that shows "🎵 Play" (green) when stopped and "⏹ Stop" (red) when playing. Also updated the menu to use the same toggle command.