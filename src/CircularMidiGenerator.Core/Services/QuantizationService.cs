using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for quantization grid snapping and calculations
/// </summary>
public class QuantizationService : IQuantizationService
{
    private readonly ConcurrentDictionary<int, QuantizationSettings> _laneQuantizationSettings;

    public QuantizationService()
    {
        _laneQuantizationSettings = new ConcurrentDictionary<int, QuantizationSettings>();
    }

    /// <summary>
    /// Snap an angle to the nearest grid line based on quantization settings
    /// </summary>
    /// <param name="angle">Input angle in degrees (0-360)</param>
    /// <param name="settings">Quantization settings to use</param>
    /// <returns>Snapped angle in degrees</returns>
    public double SnapToGrid(double angle, QuantizationSettings settings)
    {
        if (settings == null || !settings.Enabled)
        {
            return NormalizeAngle(angle);
        }

        var gridAngleStep = settings.GridAngleStep;
        return GetNearestGridLine(angle, gridAngleStep);
    }

    /// <summary>
    /// Get list of grid line angles for the given quantization settings
    /// </summary>
    /// <param name="settings">Quantization settings</param>
    /// <returns>List of angles in degrees where grid lines should be drawn</returns>
    public List<double> GetGridLines(QuantizationSettings settings)
    {
        var gridLines = new List<double>();
        
        if (settings == null || !settings.Enabled)
        {
            return gridLines;
        }

        var gridAngleStep = settings.GridAngleStep;
        var gridLineCount = settings.GridLineCount;

        for (int i = 0; i < gridLineCount; i++)
        {
            var angle = i * gridAngleStep;
            gridLines.Add(angle);
        }

        return gridLines;
    }

    /// <summary>
    /// Check if quantization is enabled for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID to check</param>
    /// <returns>True if quantization is enabled for the lane</returns>
    public bool IsQuantizationEnabled(int laneId)
    {
        if (_laneQuantizationSettings.TryGetValue(laneId, out var settings))
        {
            return settings.Enabled;
        }
        
        // Default to disabled if no settings found
        return false;
    }

    /// <summary>
    /// Set quantization settings for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <param name="settings">Quantization settings to apply</param>
    public void SetQuantization(int laneId, QuantizationSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _laneQuantizationSettings.AddOrUpdate(laneId, settings, (key, oldValue) => settings);
    }

    /// <summary>
    /// Get quantization settings for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <returns>Quantization settings for the lane</returns>
    public QuantizationSettings GetQuantization(int laneId)
    {
        if (_laneQuantizationSettings.TryGetValue(laneId, out var settings))
        {
            return settings;
        }

        // Return default settings if none found
        var defaultSettings = new QuantizationSettings
        {
            Enabled = false,
            Division = "1/16"
        };
        
        _laneQuantizationSettings.TryAdd(laneId, defaultSettings);
        return defaultSettings;
    }

    /// <summary>
    /// Calculate the nearest grid line angle for snapping
    /// </summary>
    /// <param name="angle">Input angle</param>
    /// <param name="gridAngleStep">Step between grid lines</param>
    /// <returns>Nearest grid line angle</returns>
    public double GetNearestGridLine(double angle, double gridAngleStep)
    {
        if (gridAngleStep <= 0)
        {
            throw new ArgumentException("Grid angle step must be positive", nameof(gridAngleStep));
        }

        // Normalize angle to 0-360 range
        var normalizedAngle = NormalizeAngle(angle);
        
        // Calculate the nearest grid line
        var gridIndex = Math.Round(normalizedAngle / gridAngleStep);
        var snappedAngle = gridIndex * gridAngleStep;
        
        // Ensure the result is within 0-360 range
        return NormalizeAngle(snappedAngle);
    }

    /// <summary>
    /// Normalize angle to 0-360 degree range
    /// </summary>
    /// <param name="angle">Input angle</param>
    /// <returns>Normalized angle between 0 and 360 degrees</returns>
    private static double NormalizeAngle(double angle)
    {
        // Handle negative angles
        while (angle < 0)
        {
            angle += 360;
        }
        
        // Handle angles >= 360
        while (angle >= 360)
        {
            angle -= 360;
        }
        
        return angle;
    }
}