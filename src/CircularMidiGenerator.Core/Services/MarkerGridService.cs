using System;
using System.Collections.Generic;
using System.Linq;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for managing marker interactions with quantization grids
/// </summary>
public class MarkerGridService : IMarkerGridService
{
    private readonly IQuantizationService _quantizationService;

    public MarkerGridService(IQuantizationService quantizationService)
    {
        _quantizationService = quantizationService ?? throw new ArgumentNullException(nameof(quantizationService));
    }

    /// <summary>
    /// Lock a marker to the nearest grid line based on quantization settings
    /// </summary>
    /// <param name="marker">Marker to lock</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    public void LockMarkerToGrid(Marker marker, QuantizationSettings settings, double rotationAngle)
    {
        if (marker == null)
            throw new ArgumentNullException(nameof(marker));
        
        if (settings == null || !settings.Enabled)
        {
            UnlockMarkerFromGrid(marker);
            return;
        }

        // Calculate the marker's relative angle (relative to the disk, not the screen)
        var relativeAngle = marker.Angle - rotationAngle;
        relativeAngle = NormalizeAngle(relativeAngle);

        // Snap to the nearest grid line
        var snappedRelativeAngle = _quantizationService.SnapToGrid(relativeAngle, settings);
        
        // Update marker properties
        marker.IsLockedToGrid = true;
        marker.LockedGridAngle = snappedRelativeAngle;
        marker.MaintainGridAlignment = true;
        
        // Update the marker's absolute angle
        marker.Angle = NormalizeAngle(snappedRelativeAngle + rotationAngle);
    }

    /// <summary>
    /// Unlock a marker from grid alignment
    /// </summary>
    /// <param name="marker">Marker to unlock</param>
    public void UnlockMarkerFromGrid(Marker marker)
    {
        if (marker == null)
            throw new ArgumentNullException(nameof(marker));

        marker.IsLockedToGrid = false;
        marker.LockedGridAngle = null;
        marker.MaintainGridAlignment = false;
    }

    /// <summary>
    /// Update marker positions to maintain grid alignment during rotation
    /// </summary>
    /// <param name="markers">List of markers to update</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    public void UpdateMarkersForRotation(IEnumerable<Marker> markers, QuantizationSettings settings, double rotationAngle)
    {
        if (markers == null)
            return;

        foreach (var marker in markers.Where(m => m.IsLockedToGrid && m.MaintainGridAlignment))
        {
            if (marker.LockedGridAngle.HasValue)
            {
                // Update marker's absolute angle to maintain grid alignment
                marker.Angle = NormalizeAngle(marker.LockedGridAngle.Value + rotationAngle);
            }
        }
    }

    /// <summary>
    /// Check if a marker should be locked to grid based on its position and settings
    /// </summary>
    /// <param name="marker">Marker to check</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    /// <param name="snapThreshold">Distance threshold for auto-locking in degrees</param>
    /// <returns>True if marker should be locked to grid</returns>
    public bool ShouldLockToGrid(Marker marker, QuantizationSettings settings, double rotationAngle, double snapThreshold = 5.0)
    {
        if (marker == null || settings == null || !settings.Enabled)
            return false;

        // Calculate the marker's relative angle
        var relativeAngle = marker.Angle - rotationAngle;
        relativeAngle = NormalizeAngle(relativeAngle);

        // Find the nearest grid line
        var snappedAngle = _quantizationService.SnapToGrid(relativeAngle, settings);
        
        // Calculate the distance to the nearest grid line
        var distance = Math.Abs(relativeAngle - snappedAngle);
        
        // Handle wrap-around case (e.g., 359° vs 1°)
        if (distance > 180)
        {
            distance = 360 - distance;
        }

        return distance <= snapThreshold;
    }

    /// <summary>
    /// Get the visual attachment point for a grid-locked marker
    /// </summary>
    /// <param name="marker">Grid-locked marker</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    /// <returns>Angle where marker should be visually attached</returns>
    public double GetGridAttachmentAngle(Marker marker, QuantizationSettings settings, double rotationAngle)
    {
        if (marker == null)
            throw new ArgumentNullException(nameof(marker));

        if (!marker.IsLockedToGrid || !marker.LockedGridAngle.HasValue)
        {
            return marker.Angle;
        }

        // Return the absolute angle where the marker should be visually attached
        return NormalizeAngle(marker.LockedGridAngle.Value + rotationAngle);
    }

    /// <summary>
    /// Handle transition between quantized and free placement modes
    /// </summary>
    /// <param name="markers">List of markers to transition</param>
    /// <param name="fromSettings">Previous quantization settings</param>
    /// <param name="toSettings">New quantization settings</param>
    /// <param name="rotationAngle">Current disk rotation angle</param>
    public void TransitionQuantizationMode(IEnumerable<Marker> markers, QuantizationSettings fromSettings, QuantizationSettings toSettings, double rotationAngle)
    {
        if (markers == null)
            return;

        foreach (var marker in markers)
        {
            if (toSettings == null || !toSettings.Enabled)
            {
                // Transitioning to free placement mode
                UnlockMarkerFromGrid(marker);
            }
            else if (fromSettings == null || !fromSettings.Enabled)
            {
                // Transitioning from free placement to quantized mode
                if (ShouldLockToGrid(marker, toSettings, rotationAngle))
                {
                    LockMarkerToGrid(marker, toSettings, rotationAngle);
                }
            }
            else
            {
                // Transitioning between different quantization settings
                if (marker.IsLockedToGrid)
                {
                    // Re-lock to the new grid
                    LockMarkerToGrid(marker, toSettings, rotationAngle);
                }
            }
        }
    }

    /// <summary>
    /// Normalize angle to 0-360 degree range
    /// </summary>
    /// <param name="angle">Input angle</param>
    /// <returns>Normalized angle between 0 and 360 degrees</returns>
    private static double NormalizeAngle(double angle)
    {
        while (angle < 0)
        {
            angle += 360;
        }
        
        while (angle >= 360)
        {
            angle -= 360;
        }
        
        return angle;
    }
}