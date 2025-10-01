using System;
using System.Collections.Generic;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for grid overlay rendering calculations and visual feedback
/// </summary>
public class GridRenderingService : IGridRenderingService
{
    private readonly IQuantizationService _quantizationService;
    
    /// <summary>
    /// Snap distance threshold in pixels for visual feedback
    /// </summary>
    private const double SnapThreshold = 20.0;

    public GridRenderingService(IQuantizationService quantizationService)
    {
        _quantizationService = quantizationService ?? throw new ArgumentNullException(nameof(quantizationService));
    }

    /// <summary>
    /// Calculate grid line positions for rendering on a circular canvas
    /// </summary>
    /// <param name="settings">Quantization settings</param>
    /// <param name="centerX">Center X coordinate of the circle</param>
    /// <param name="centerY">Center Y coordinate of the circle</param>
    /// <param name="radius">Radius of the circle</param>
    /// <param name="rotationAngle">Current rotation angle of the disk</param>
    /// <returns>List of grid line coordinates for rendering</returns>
    public List<GridLine> CalculateGridLines(QuantizationSettings settings, double centerX, double centerY, double radius, double rotationAngle)
    {
        var gridLines = new List<GridLine>();
        
        if (!ShouldShowGrid(settings))
        {
            return gridLines;
        }

        var gridAngles = _quantizationService.GetGridLines(settings);
        
        foreach (var gridAngle in gridAngles)
        {
            // Apply rotation to the grid angle
            var rotatedAngle = gridAngle + rotationAngle;
            var radians = DegreesToRadians(rotatedAngle);
            
            // Calculate start and end points of the grid line
            // Grid lines extend from inner radius to outer radius
            var innerRadius = radius * 0.3; // Start grid lines from 30% of radius
            var outerRadius = radius * 0.95; // End grid lines at 95% of radius
            
            var startX = centerX + Math.Cos(radians) * innerRadius;
            var startY = centerY + Math.Sin(radians) * innerRadius;
            var endX = centerX + Math.Cos(radians) * outerRadius;
            var endY = centerY + Math.Sin(radians) * outerRadius;
            
            gridLines.Add(new GridLine
            {
                StartX = startX,
                StartY = startY,
                EndX = endX,
                EndY = endY,
                Angle = gridAngle
            });
        }
        
        return gridLines;
    }

    /// <summary>
    /// Check if grid should be visible based on quantization mode
    /// </summary>
    /// <param name="settings">Quantization settings</param>
    /// <returns>True if grid should be rendered</returns>
    public bool ShouldShowGrid(QuantizationSettings settings)
    {
        return settings != null && settings.Enabled;
    }

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
    public SnapFeedback CalculateSnapFeedback(double mouseX, double mouseY, double centerX, double centerY, double radius, QuantizationSettings settings, double rotationAngle)
    {
        var feedback = new SnapFeedback();
        
        // Calculate the angle from center to mouse position
        var deltaX = mouseX - centerX;
        var deltaY = mouseY - centerY;
        var mouseAngle = RadiansToDegrees(Math.Atan2(deltaY, deltaX));
        
        // Normalize to 0-360 range
        mouseAngle = NormalizeAngle(mouseAngle);
        
        // Adjust for disk rotation (subtract rotation to get relative angle)
        var relativeAngle = mouseAngle - rotationAngle;
        relativeAngle = NormalizeAngle(relativeAngle);
        
        if (!ShouldShowGrid(settings))
        {
            // No snapping, just return the original position projected onto the circle
            var mouseDistance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            var projectedRadius = Math.Min(mouseDistance, radius);
            
            var radians = DegreesToRadians(mouseAngle);
            feedback.ShouldSnap = false;
            feedback.SnappedX = centerX + Math.Cos(radians) * projectedRadius;
            feedback.SnappedY = centerY + Math.Sin(radians) * projectedRadius;
            feedback.SnappedAngle = mouseAngle;
            feedback.SnapDistance = 0;
            
            return feedback;
        }
        
        // Calculate snapped angle
        var snappedRelativeAngle = _quantizationService.SnapToGrid(relativeAngle, settings);
        var snappedAbsoluteAngle = snappedRelativeAngle + rotationAngle;
        snappedAbsoluteAngle = NormalizeAngle(snappedAbsoluteAngle);
        
        // Calculate snapped position on the circle
        var snappedRadians = DegreesToRadians(snappedAbsoluteAngle);
        var snappedX = centerX + Math.Cos(snappedRadians) * radius;
        var snappedY = centerY + Math.Sin(snappedRadians) * radius;
        
        // Calculate distance between mouse and snapped position
        var snapDistance = Math.Sqrt(Math.Pow(mouseX - snappedX, 2) + Math.Pow(mouseY - snappedY, 2));
        
        feedback.ShouldSnap = snapDistance <= SnapThreshold;
        feedback.SnappedX = snappedX;
        feedback.SnappedY = snappedY;
        feedback.SnappedAngle = snappedAbsoluteAngle;
        feedback.SnapDistance = snapDistance;
        
        return feedback;
    }

    /// <summary>
    /// Convert degrees to radians
    /// </summary>
    /// <param name="degrees">Angle in degrees</param>
    /// <returns>Angle in radians</returns>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Convert radians to degrees
    /// </summary>
    /// <param name="radians">Angle in radians</param>
    /// <returns>Angle in degrees</returns>
    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
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