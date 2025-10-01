using System.Collections.Generic;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for managing marker interactions with quantization grids
/// </summary>
public interface IMarkerGridService
{
    /// <summary>
    /// Lock a marker to the nearest grid line based on quantization settings
    /// </summary>
    /// <param name="marker">Marker to lock</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    void LockMarkerToGrid(Marker marker, QuantizationSettings settings, double rotationAngle);
    
    /// <summary>
    /// Unlock a marker from grid alignment
    /// </summary>
    /// <param name="marker">Marker to unlock</param>
    void UnlockMarkerFromGrid(Marker marker);
    
    /// <summary>
    /// Update marker positions to maintain grid alignment during rotation
    /// </summary>
    /// <param name="markers">List of markers to update</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    void UpdateMarkersForRotation(IEnumerable<Marker> markers, QuantizationSettings settings, double rotationAngle);
    
    /// <summary>
    /// Check if a marker should be locked to grid based on its position and settings
    /// </summary>
    /// <param name="marker">Marker to check</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    /// <param name="snapThreshold">Distance threshold for auto-locking</param>
    /// <returns>True if marker should be locked to grid</returns>
    bool ShouldLockToGrid(Marker marker, QuantizationSettings settings, double rotationAngle, double snapThreshold = 5.0);
    
    /// <summary>
    /// Get the visual attachment point for a grid-locked marker
    /// </summary>
    /// <param name="marker">Grid-locked marker</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    /// <returns>Angle where marker should be visually attached</returns>
    double GetGridAttachmentAngle(Marker marker, QuantizationSettings settings, double rotationAngle);
    
    /// <summary>
    /// Handle transition between quantized and free placement modes
    /// </summary>
    /// <param name="markers">List of markers to transition</param>
    /// <param name="fromSettings">Previous quantization settings</param>
    /// <param name="toSettings">New quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    void TransitionQuantizationMode(IEnumerable<Marker> markers, QuantizationSettings fromSettings, QuantizationSettings toSettings, double rotationAngle);
}