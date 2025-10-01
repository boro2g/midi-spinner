using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Controls;

/// <summary>
/// Custom circular canvas control for the MIDI generator interface
/// Handles marker placement, rendering, and user interactions on a spinning disk
/// </summary>
public class CircularCanvas : Control
{
    #region Styled Properties

    /// <summary>
    /// Collection of markers to display on the canvas
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Marker>?> MarkersProperty =
        AvaloniaProperty.Register<CircularCanvas, ObservableCollection<Marker>?>(nameof(Markers));

    /// <summary>
    /// Current playhead angle in degrees (0-360)
    /// </summary>
    public static readonly StyledProperty<double> PlayheadAngleProperty =
        AvaloniaProperty.Register<CircularCanvas, double>(nameof(PlayheadAngle), 0.0);

    /// <summary>
    /// Whether quantization grid should be visible
    /// </summary>
    public static readonly StyledProperty<bool> IsQuantizationEnabledProperty =
        AvaloniaProperty.Register<CircularCanvas, bool>(nameof(IsQuantizationEnabled), false);

    /// <summary>
    /// Quantization grid lines (angles in degrees)
    /// </summary>
    public static readonly StyledProperty<IList<double>?> GridLinesProperty =
        AvaloniaProperty.Register<CircularCanvas, IList<double>?>(nameof(GridLines));

    /// <summary>
    /// Disk rotation angle in degrees
    /// </summary>
    public static readonly StyledProperty<double> DiskRotationProperty =
        AvaloniaProperty.Register<CircularCanvas, double>(nameof(DiskRotation), 0.0);

    /// <summary>
    /// Currently selected marker for manipulation
    /// </summary>
    public static readonly StyledProperty<Marker?> SelectedMarkerProperty =
        AvaloniaProperty.Register<CircularCanvas, Marker?>(nameof(SelectedMarker));

    #endregion

    #region Properties

    public ObservableCollection<Marker>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    public double PlayheadAngle
    {
        get => GetValue(PlayheadAngleProperty);
        set => SetValue(PlayheadAngleProperty, value);
    }

    public bool IsQuantizationEnabled
    {
        get => GetValue(IsQuantizationEnabledProperty);
        set => SetValue(IsQuantizationEnabledProperty, value);
    }

    public IList<double>? GridLines
    {
        get => GetValue(GridLinesProperty);
        set => SetValue(GridLinesProperty, value);
    }

    public double DiskRotation
    {
        get => GetValue(DiskRotationProperty);
        set => SetValue(DiskRotationProperty, value);
    }

    public Marker? SelectedMarker
    {
        get => GetValue(SelectedMarkerProperty);
        set => SetValue(SelectedMarkerProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a marker is placed on the canvas
    /// </summary>
    public event EventHandler<MarkerPlacedEventArgs>? MarkerPlaced;

    /// <summary>
    /// Raised when a marker is selected
    /// </summary>
    public event EventHandler<MarkerSelectedEventArgs>? MarkerSelected;

    /// <summary>
    /// Raised when a marker is moved
    /// </summary>
    public event EventHandler<MarkerMovedEventArgs>? MarkerMoved;

    /// <summary>
    /// Raised when a marker is removed
    /// </summary>
    public event EventHandler<MarkerRemovedEventArgs>? MarkerRemoved;

    #endregion

    #region Private Fields

    private Point _center;
    private double _radius;
    private bool _isDragging;
    private Marker? _draggedMarker;
    private Point _lastPointerPosition;

    // Visual styling
    private readonly IBrush _diskBrush = new SolidColorBrush(Color.FromRgb(45, 45, 55));
    private readonly IPen _diskPen = new Pen(new SolidColorBrush(Color.FromRgb(80, 80, 90)), 2);
    private readonly IPen _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1);
    private readonly IPen _playheadPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 100, 100)), 3);

    #endregion

    #region Constructor

    public CircularCanvas()
    {
        // Subscribe to property changes for invalidation
        this.GetObservable(MarkersProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(PlayheadAngleProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(IsQuantizationEnabledProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(GridLinesProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(DiskRotationProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SelectedMarkerProperty).Subscribe(_ => InvalidateVisual());

        // Set up input handling - Control doesn't have Background property
        // Input events will be handled through pointer events
    }

    #endregion

    #region Coordinate System Methods

    /// <summary>
    /// Calculates the angle from center to a point in degrees (0° = 12 o'clock, clockwise)
    /// </summary>
    private double CalculateAngle(Point point)
    {
        var deltaX = point.X - _center.X;
        var deltaY = point.Y - _center.Y;
        
        // Calculate angle in radians, then convert to degrees
        var angleRadians = Math.Atan2(deltaY, deltaX);
        var angleDegrees = angleRadians * 180.0 / Math.PI;
        
        // Adjust so 0° is at 12 o'clock (top) and increases clockwise
        angleDegrees = (angleDegrees + 90) % 360;
        if (angleDegrees < 0) angleDegrees += 360;
        
        return angleDegrees;
    }

    /// <summary>
    /// Calculates the position on the circle for a given angle
    /// </summary>
    private Point CalculatePosition(double angle, double radius)
    {
        // Convert angle to radians and adjust for 12 o'clock start
        var angleRadians = (angle - 90) * Math.PI / 180.0;
        
        var x = _center.X + radius * Math.Cos(angleRadians);
        var y = _center.Y + radius * Math.Sin(angleRadians);
        
        return new Point(x, y);
    }

    /// <summary>
    /// Calculates the distance from center to a point
    /// </summary>
    private double CalculateDistance(Point point)
    {
        var deltaX = point.X - _center.X;
        var deltaY = point.Y - _center.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    /// <summary>
    /// Checks if a point is within the disk area
    /// </summary>
    private bool IsPointInDisk(Point point)
    {
        return CalculateDistance(point) <= _radius;
    }

    /// <summary>
    /// Finds the marker at a given position (within tolerance)
    /// </summary>
    private Marker? FindMarkerAtPosition(Point position, double tolerance = 15.0)
    {
        if (Markers == null) return null;

        foreach (var marker in Markers)
        {
            var markerPosition = CalculatePosition(marker.Angle + DiskRotation, _radius * 0.85);
            var distance = Math.Sqrt(
                Math.Pow(position.X - markerPosition.X, 2) + 
                Math.Pow(position.Y - markerPosition.Y, 2)
            );
            
            if (distance <= tolerance)
                return marker;
        }
        
        return null;
    }

    /// <summary>
    /// Snaps an angle to the nearest grid line if quantization is enabled
    /// </summary>
    private double SnapToNearestGridLine(double angle)
    {
        if (GridLines == null || !GridLines.Any()) return angle;
        
        var nearestGridLine = GridLines
            .Select(gridAngle => new { GridAngle = gridAngle, Distance = Math.Abs(angle - gridAngle) })
            .OrderBy(x => x.Distance)
            .First();
            
        // Snap if within reasonable distance (e.g., 15 degrees)
        return nearestGridLine.Distance <= 15 ? nearestGridLine.GridAngle : angle;
    }

    #endregion

    #region Input Handling

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        var position = e.GetPosition(this);
        _lastPointerPosition = position;
        
        // Check if clicking on an existing marker
        var clickedMarker = FindMarkerAtPosition(position);
        
        if (clickedMarker != null)
        {
            // Enhanced marker selection and drag initiation
            SelectedMarker = clickedMarker;
            _isDragging = true;
            _draggedMarker = clickedMarker;
            
            // Visual feedback for drag start
            Cursor = new Cursor(StandardCursorType.SizeAll);
            
            MarkerSelected?.Invoke(this, new MarkerSelectedEventArgs(clickedMarker));
            
            // Capture pointer for dragging
            e.Pointer.Capture(this);
            InvalidateVisual();
        }
        else if (IsPointInDisk(position))
        {
            // Enhanced marker placement
            var angle = CalculateAngle(position);
            
            // Apply quantization to new marker placement if enabled
            if (IsQuantizationEnabled && GridLines != null)
            {
                angle = SnapToNearestGridLine(angle);
            }
            
            // Create new marker with color based on angle (chromatic mapping)
            var semitone = (int)(angle / 30) % 12; // 360° / 12 semitones = 30° per semitone
            var midiNote = 60 + semitone; // C4 + semitone
            var color = Marker.GetColorFromMidiNote(midiNote);
            var newMarker = new Marker(angle, System.Drawing.Color.FromArgb(color.R, color.G, color.B));
            
            // Set default velocity based on distance from center (closer = louder)
            var distanceFromCenter = CalculateDistance(position);
            var normalizedDistance = Math.Min(1.0, distanceFromCenter / _radius);
            var velocity = (int)(127 * (1.0 - normalizedDistance * 0.3)); // 70-127 range
            newMarker.Velocity = Math.Max(70, velocity);
            
            MarkerPlaced?.Invoke(this, new MarkerPlacedEventArgs(newMarker, position));
        }
        else
        {
            // Clicked outside disk - deselect any selected marker
            if (SelectedMarker != null)
            {
                SelectedMarker = null;
                InvalidateVisual();
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (_isDragging && _draggedMarker != null)
        {
            var currentPosition = e.GetPosition(this);
            
            // Enhanced drag behavior with visual feedback
            var isOutsideDisk = !IsPointInDisk(currentPosition);
            
            if (isOutsideDisk)
            {
                // Visual feedback for removal - could add red tint or fade effect
                // Marker removal will be handled in OnPointerReleased
                InvalidateVisual();
                return;
            }
            
            // Update marker angle based on new position
            var newAngle = CalculateAngle(currentPosition);
            var oldAngle = _draggedMarker.Angle;
            
            // Enhanced velocity adjustment with keyboard modifier support
            var verticalDelta = currentPosition.Y - _lastPointerPosition.Y;
            var horizontalDelta = currentPosition.X - _lastPointerPosition.X;
            
            // Check for keyboard modifiers
            var keyModifiers = e.KeyModifiers;
            var isShiftPressed = keyModifiers.HasFlag(KeyModifiers.Shift);
            var isCtrlPressed = keyModifiers.HasFlag(KeyModifiers.Control);
            
            var newVelocity = _draggedMarker.Velocity;
            
            // Velocity adjustment mode (Shift key or vertical movement)
            if (isShiftPressed || Math.Abs(verticalDelta) > Math.Abs(horizontalDelta))
            {
                // More sensitive velocity adjustment when in velocity mode
                var velocityChange = (int)(-verticalDelta * 0.8);
                newVelocity = Math.Max(1, Math.Min(127, _draggedMarker.Velocity + velocityChange));
                
                // Don't update angle in velocity adjust mode
                if (!isCtrlPressed)
                {
                    _draggedMarker.Velocity = newVelocity;
                    InvalidateVisual();
                    return;
                }
            }
            else
            {
                // Normal velocity adjustment for position changes
                var radialDistance = CalculateDistance(currentPosition);
                var previousRadialDistance = CalculateDistance(_lastPointerPosition);
                var radialDelta = radialDistance - previousRadialDistance;
                
                // Subtle velocity adjustment based on radial movement
                var velocityChange = (int)(radialDelta * 0.1);
                newVelocity = Math.Max(1, Math.Min(127, _draggedMarker.Velocity + velocityChange));
            }
            
            // Apply quantization if enabled
            if (IsQuantizationEnabled && GridLines != null)
            {
                newAngle = SnapToNearestGridLine(newAngle);
            }
            
            _draggedMarker.Angle = newAngle;
            _draggedMarker.Velocity = newVelocity;
            
            MarkerMoved?.Invoke(this, new MarkerMovedEventArgs(_draggedMarker, oldAngle, newAngle));
            
            _lastPointerPosition = currentPosition;
            InvalidateVisual();
        }
        else
        {
            // Hover effects for non-dragging state
            var hoveredMarker = FindMarkerAtPosition(e.GetPosition(this));
            if (hoveredMarker != null)
            {
                // Could implement hover highlighting here
                Cursor = new Cursor(StandardCursorType.Hand);
            }
            else
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        if (_isDragging && _draggedMarker != null)
        {
            var position = e.GetPosition(this);
            
            // Enhanced drag completion handling
            if (!IsPointInDisk(position))
            {
                // Marker removal with visual feedback
                MarkerRemoved?.Invoke(this, new MarkerRemovedEventArgs(_draggedMarker));
            }
            else
            {
                // Final quantization snap if enabled
                if (IsQuantizationEnabled && GridLines != null)
                {
                    var snappedAngle = SnapToNearestGridLine(_draggedMarker.Angle);
                    if (Math.Abs(snappedAngle - _draggedMarker.Angle) > 0.1)
                    {
                        var oldAngle = _draggedMarker.Angle;
                        _draggedMarker.Angle = snappedAngle;
                        MarkerMoved?.Invoke(this, new MarkerMovedEventArgs(_draggedMarker, oldAngle, snappedAngle));
                    }
                }
            }
            
            // Reset drag state
            _isDragging = false;
            _draggedMarker = null;
            Cursor = new Cursor(StandardCursorType.Arrow);
            
            // Release pointer capture
            e.Pointer.Capture(null);
            InvalidateVisual();
        }
    }

    #endregion

    #region Rendering

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateDimensions();
    }

    private void UpdateDimensions()
    {
        _center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        _radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - 20; // Leave margin
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        
        UpdateDimensions();
        
        // Apply disk rotation transform around center
        var rotationTransform = Matrix.CreateRotation(DiskRotation * Math.PI / 180.0);
        var centerTransform = Matrix.CreateTranslation(_center.X, _center.Y);
        var negCenterTransform = Matrix.CreateTranslation(-_center.X, -_center.Y);
        var combinedTransform = negCenterTransform * rotationTransform * centerTransform;
        
        using (context.PushTransform(combinedTransform))
        {
            // Draw disk background
            DrawDisk(context);
            
            // Draw quantization grid if enabled
            if (IsQuantizationEnabled && GridLines != null)
            {
                DrawQuantizationGrid(context);
            }
            
            // Draw markers
            DrawMarkers(context);
        }
        
        // Draw playhead (not rotated with disk)
        DrawPlayhead(context);
    }

    private void DrawDisk(DrawingContext context)
    {
        // Draw main disk
        context.DrawEllipse(_diskBrush, _diskPen, _center, _radius, _radius);
        
        // Draw center dot
        var centerDotRadius = 5;
        context.DrawEllipse(
            new SolidColorBrush(Color.FromRgb(200, 200, 200)), 
            null, 
            _center, 
            centerDotRadius, 
            centerDotRadius
        );
    }

    private void DrawQuantizationGrid(DrawingContext context)
    {
        if (GridLines == null) return;
        
        foreach (var gridAngle in GridLines)
        {
            var startPoint = CalculatePosition(gridAngle, _radius * 0.3);
            var endPoint = CalculatePosition(gridAngle, _radius * 0.95);
            
            context.DrawLine(_gridPen, startPoint, endPoint);
        }
    }

    private void DrawMarkers(DrawingContext context)
    {
        if (Markers == null) return;
        
        foreach (var marker in Markers)
        {
            DrawMarker(context, marker);
        }
    }

    private void DrawMarker(DrawingContext context, Marker marker)
    {
        var position = CalculatePosition(marker.Angle, _radius * 0.85);
        var baseRadius = 8;
        var markerRadius = baseRadius;
        
        // Convert System.Drawing.Color to Avalonia Color
        var avaloniaColor = Color.FromArgb(marker.Color.A, marker.Color.R, marker.Color.G, marker.Color.B);
        
        // Enhanced visual feedback based on state
        if (marker.IsActive)
        {
            // Active marker: larger size with pulsing glow effect
            markerRadius = 14;
            
            // Draw outer glow for active markers
            var glowRadius = markerRadius + 6;
            var glowBrush = new SolidColorBrush(avaloniaColor, 0.3);
            context.DrawEllipse(glowBrush, null, position, glowRadius, glowRadius);
            
            // Draw middle glow
            var midGlowRadius = markerRadius + 3;
            var midGlowBrush = new SolidColorBrush(avaloniaColor, 0.6);
            context.DrawEllipse(midGlowBrush, null, position, midGlowRadius, midGlowRadius);
        }
        
        // Selection ring
        if (marker == SelectedMarker)
        {
            var selectionRadius = markerRadius + 4;
            var selectionPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 100)), 2);
            
            // Animated selection ring (could be enhanced with actual animation)
            context.DrawEllipse(null, selectionPen, position, selectionRadius, selectionRadius);
            
            // Inner selection highlight
            var innerSelectionPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1);
            context.DrawEllipse(null, innerSelectionPen, position, markerRadius + 1, markerRadius + 1);
        }
        
        // Main marker body with velocity-based visual feedback
        var velocity = marker.Velocity;
        var velocityNormalized = velocity / 127.0;
        
        // Velocity affects both opacity and size slightly
        var velocityOpacity = Math.Max(0.4, velocityNormalized);
        var velocitySizeMultiplier = 0.7 + (velocityNormalized * 0.3); // 0.7 to 1.0 range
        var finalRadius = markerRadius * velocitySizeMultiplier;
        
        var markerBrush = new SolidColorBrush(avaloniaColor, velocityOpacity);
        
        // Enhanced border based on state
        IBrush borderBrush;
        double borderThickness;
        
        if (marker.IsActive)
        {
            borderBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            borderThickness = 2;
        }
        else if (marker == SelectedMarker)
        {
            borderBrush = new SolidColorBrush(Color.FromRgb(255, 255, 200));
            borderThickness = 1.5;
        }
        else
        {
            borderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            borderThickness = 1;
        }
        
        var markerPen = new Pen(borderBrush, borderThickness);
        context.DrawEllipse(markerBrush, markerPen, position, finalRadius, finalRadius);
        
        // Velocity visualization for selected markers
        if (marker == SelectedMarker)
        {
            DrawVelocityIndicator(context, position, velocity, finalRadius);
        }
        
        // Lane indicator (small colored dot)
        if (marker.Lane > 0)
        {
            DrawLaneIndicator(context, position, marker.Lane, finalRadius);
        }
        
        // Grid lock indicator
        if (marker.IsLockedToGrid)
        {
            DrawGridLockIndicator(context, position, finalRadius);
        }
    }
    
    private void DrawVelocityIndicator(DrawingContext context, Point markerPosition, int velocity, double markerRadius)
    {
        var velocityNormalized = velocity / 127.0;
        var indicatorHeight = velocityNormalized * 25;
        var indicatorWidth = 3;
        
        // Position the velocity bar below the marker
        var barX = markerPosition.X - indicatorWidth / 2;
        var barY = markerPosition.Y + markerRadius + 3;
        
        // Background bar (full height, dimmed)
        var backgroundRect = new Rect(barX, barY, indicatorWidth, 25);
        var backgroundBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100), 0.3);
        context.DrawRectangle(backgroundBrush, null, backgroundRect);
        
        // Velocity bar (actual velocity height)
        var velocityRect = new Rect(barX, barY + (25 - indicatorHeight), indicatorWidth, indicatorHeight);
        
        // Color-code velocity: green (low) to yellow (mid) to red (high)
        Color velocityColor;
        if (velocityNormalized < 0.5)
        {
            // Green to yellow
            var t = velocityNormalized * 2;
            velocityColor = Color.FromRgb(
                (byte)(0 + t * 255),
                255,
                0
            );
        }
        else
        {
            // Yellow to red
            var t = (velocityNormalized - 0.5) * 2;
            velocityColor = Color.FromRgb(
                255,
                (byte)(255 - t * 255),
                0
            );
        }
        
        var velocityBrush = new SolidColorBrush(velocityColor);
        context.DrawRectangle(velocityBrush, null, velocityRect);
        
        // Velocity text
        var velocityText = velocity.ToString();
        var textBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var typeface = new Typeface("Arial");
        var formattedText = new FormattedText(
            velocityText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            10,
            textBrush
        );
        
        var textPosition = new Point(
            markerPosition.X - formattedText.Width / 2,
            barY + 27
        );
        
        context.DrawText(formattedText, textPosition);
    }
    
    private void DrawLaneIndicator(DrawingContext context, Point markerPosition, int lane, double markerRadius)
    {
        // Small colored dot to indicate lane
        var laneColors = new[]
        {
            Color.FromRgb(255, 100, 100), // Lane 1 - Red
            Color.FromRgb(100, 255, 100), // Lane 2 - Green  
            Color.FromRgb(100, 100, 255), // Lane 3 - Blue
            Color.FromRgb(255, 255, 100), // Lane 4 - Yellow
            Color.FromRgb(255, 100, 255), // Lane 5 - Magenta
            Color.FromRgb(100, 255, 255), // Lane 6 - Cyan
        };
        
        var laneColor = laneColors[Math.Min(lane - 1, laneColors.Length - 1)];
        var laneBrush = new SolidColorBrush(laneColor);
        
        var indicatorRadius = 3;
        var indicatorPosition = new Point(
            markerPosition.X + markerRadius - indicatorRadius,
            markerPosition.Y - markerRadius + indicatorRadius
        );
        
        context.DrawEllipse(laneBrush, null, indicatorPosition, indicatorRadius, indicatorRadius);
    }
    
    private void DrawGridLockIndicator(DrawingContext context, Point markerPosition, double markerRadius)
    {
        // Small square to indicate grid lock
        var lockSize = 4;
        var lockPosition = new Point(
            markerPosition.X - markerRadius + 2,
            markerPosition.Y - markerRadius + 2
        );
        
        var lockRect = new Rect(lockPosition.X, lockPosition.Y, lockSize, lockSize);
        var lockBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var lockPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 0, 0)), 1);
        
        context.DrawRectangle(lockBrush, lockPen, lockRect);
    }

    private void DrawPlayhead(DrawingContext context)
    {
        // Enhanced playhead visualization at 12 o'clock (not affected by disk rotation)
        
        // Main playhead line - thicker and more prominent
        var playheadStart = new Point(_center.X, _center.Y - _radius * 0.15);
        var playheadEnd = new Point(_center.X, _center.Y - _radius * 1.15);
        
        // Draw shadow/glow effect for better visibility
        var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), 5);
        var shadowStart = new Point(playheadStart.X + 1, playheadStart.Y + 1);
        var shadowEnd = new Point(playheadEnd.X + 1, playheadEnd.Y + 1);
        context.DrawLine(shadowPen, shadowStart, shadowEnd);
        
        // Main playhead line
        context.DrawLine(_playheadPen, playheadStart, playheadEnd);
        
        // Enhanced playhead triangle/arrow
        var triangleSize = 10;
        var triangleHeight = 12;
        var arrowTip = new Point(_center.X, _center.Y - _radius * 1.15);
        var trianglePoints = new[]
        {
            arrowTip,
            new Point(_center.X - triangleSize, arrowTip.Y - triangleHeight),
            new Point(_center.X + triangleSize, arrowTip.Y - triangleHeight)
        };
        
        // Draw triangle shadow
        var shadowTrianglePoints = trianglePoints.Select(p => new Point(p.X + 1, p.Y + 1)).ToArray();
        var shadowTriangleGeometry = new PolylineGeometry(shadowTrianglePoints, true);
        context.DrawGeometry(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), null, shadowTriangleGeometry);
        
        // Draw main triangle
        var triangleGeometry = new PolylineGeometry(trianglePoints, true);
        var triangleBrush = new SolidColorBrush(Color.FromRgb(255, 120, 120));
        var trianglePen = new Pen(new SolidColorBrush(Color.FromRgb(200, 50, 50)), 2);
        context.DrawGeometry(triangleBrush, trianglePen, triangleGeometry);
        
        // Add playhead center marker
        var centerMarkerRadius = 4;
        var centerBrush = new SolidColorBrush(Color.FromRgb(255, 150, 150));
        var centerPen = new Pen(new SolidColorBrush(Color.FromRgb(200, 50, 50)), 1);
        context.DrawEllipse(centerBrush, centerPen, _center, centerMarkerRadius, centerMarkerRadius);
        
        // Optional: Add "12" text indicator
        DrawPlayheadLabel(context);
    }
    
    private void DrawPlayheadLabel(DrawingContext context)
    {
        var labelText = "12";
        var textBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
        var formattedText = new FormattedText(
            labelText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            14,
            textBrush
        );
        
        var labelPosition = new Point(
            _center.X - formattedText.Width / 2,
            _center.Y - _radius * 1.3
        );
        
        // Draw text shadow
        var shadowPosition = new Point(labelPosition.X + 1, labelPosition.Y + 1);
        var shadowText = new FormattedText(
            labelText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            14,
            new SolidColorBrush(Color.FromArgb(150, 0, 0, 0))
        );
        context.DrawText(shadowText, shadowPosition);
        
        // Draw main text
        context.DrawText(formattedText, labelPosition);
    }

    #endregion
}

#region Event Args Classes

public class MarkerPlacedEventArgs : EventArgs
{
    public Marker Marker { get; }
    public Point Position { get; }
    
    public MarkerPlacedEventArgs(Marker marker, Point position)
    {
        Marker = marker;
        Position = position;
    }
}

public class MarkerSelectedEventArgs : EventArgs
{
    public Marker Marker { get; }
    
    public MarkerSelectedEventArgs(Marker marker)
    {
        Marker = marker;
    }
}

public class MarkerMovedEventArgs : EventArgs
{
    public Marker Marker { get; }
    public double OldAngle { get; }
    public double NewAngle { get; }
    
    public MarkerMovedEventArgs(Marker marker, double oldAngle, double newAngle)
    {
        Marker = marker;
        OldAngle = oldAngle;
        NewAngle = newAngle;
    }
}

public class MarkerRemovedEventArgs : EventArgs
{
    public Marker Marker { get; }
    
    public MarkerRemovedEventArgs(Marker marker)
    {
        Marker = marker;
    }
}

#endregion