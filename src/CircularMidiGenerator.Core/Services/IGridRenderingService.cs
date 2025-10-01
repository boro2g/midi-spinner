using System.Collections.Generic;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for grid overlay rendering calculations and visual feedback
/// </summary>
public interface IGridRenderingService
{
    /// <summary>
    /// Calculate grid line positions for rendering on a circular canvas
    /// </summary>
    /// <param name="settings">Quantization settings</param>
    /// <param name="centerX">Center X coordinate of the circle</param>
    /// <param name="centerY">Center Y coordinate of the circle</param>
    /// <param name="radius">Radius of the circle</param>
    /// <param name="rotationAngle">Current rotation angle of the disk</param>
    /// <returns>List of grid line coordinates for rendering</returns>
    List<GridLine> CalculateGridLines(QuantizationSettings settings, double centerX, double centerY, double radius, double rotationAngle);
    
    /// <summary>
    /// Check if grid should be visible based on quantization mode
    /// </summary>
    /// <param name="settings">Quantization settings</param>
    /// <returns>True if grid should be rendered</returns>
    bool ShouldShowGrid(QuantizationSettings settings);
    
    /// <summary>
    /// Calculate snap feedback position for marker placement
    /// </summary>
    /// <param name="mouseX">Mouse X position</param>
    /// <param name="mouseY">Mouse Y position</param>
    /// <param name="centerX">Center X coordinate of the circle</param>
    /// <param name="centerY">Center Y coordinate of the circle</param>
    /// <param name="radius">Radius of the circle</param>
    /// <param name="settings">Quantization settings</param>
    /// <param name="rotationAngle">Current rotation angle of the disk</param>
    /// <returns>Snapped position for visual feedback</returns>
    SnapFeedback CalculateSnapFeedback(double mouseX, double mouseY, double centerX, double centerY, double radius, QuantizationSettings settings, double rotationAngle);
}

/// <summary>
/// Represents a grid line for rendering
/// </summary>
public class GridLine
{
    /// <summary>
    /// Start X coordinate of the grid line
    /// </summary>
    public double StartX { get; set; }
    
    /// <summary>
    /// Start Y coordinate of the grid line
    /// </summary>
    public double StartY { get; set; }
    
    /// <summary>
    /// End X coordinate of the grid line
    /// </summary>
    public double EndX { get; set; }
    
    /// <summary>
    /// End Y coordinate of the grid line
    /// </summary>
    public double EndY { get; set; }
    
    /// <summary>
    /// Angle of this grid line in degrees
    /// </summary>
    public double Angle { get; set; }
}

/// <summary>
/// Represents snap feedback for marker placement
/// </summary>
public class SnapFeedback
{
    /// <summary>
    /// Whether snapping should occur
    /// </summary>
    public bool ShouldSnap { get; set; }
    
    /// <summary>
    /// Snapped X coordinate
    /// </summary>
    public double SnappedX { get; set; }
    
    /// <summary>
    /// Snapped Y coordinate
    /// </summary>
    public double SnappedY { get; set; }
    
    /// <summary>
    /// Snapped angle in degrees
    /// </summary>
    public double SnappedAngle { get; set; }
    
    /// <summary>
    /// Distance from mouse to snap position (for visual feedback intensity)
    /// </summary>
    public double SnapDistance { get; set; }
}