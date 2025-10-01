using System.Collections.Generic;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for quantization grid snapping and calculations
/// </summary>
public interface IQuantizationService
{
    /// <summary>
    /// Snap an angle to the nearest grid line based on quantization settings
    /// </summary>
    /// <param name="angle">Input angle in degrees</param>
    /// <param name="settings">Quantization settings to use</param>
    /// <returns>Snapped angle in degrees</returns>
    double SnapToGrid(double angle, QuantizationSettings settings);
    
    /// <summary>
    /// Get list of grid line angles for the given quantization settings
    /// </summary>
    /// <param name="settings">Quantization settings</param>
    /// <returns>List of angles in degrees where grid lines should be drawn</returns>
    List<double> GetGridLines(QuantizationSettings settings);
    
    /// <summary>
    /// Check if quantization is enabled for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID to check</param>
    /// <returns>True if quantization is enabled for the lane</returns>
    bool IsQuantizationEnabled(int laneId);
    
    /// <summary>
    /// Set quantization settings for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <param name="settings">Quantization settings to apply</param>
    void SetQuantization(int laneId, QuantizationSettings settings);
    
    /// <summary>
    /// Get quantization settings for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <returns>Quantization settings for the lane</returns>
    QuantizationSettings GetQuantization(int laneId);
    
    /// <summary>
    /// Calculate the nearest grid line angle for snapping
    /// </summary>
    /// <param name="angle">Input angle</param>
    /// <param name="gridAngleStep">Step between grid lines</param>
    /// <returns>Nearest grid line angle</returns>
    double GetNearestGridLine(double angle, double gridAngleStep);
}